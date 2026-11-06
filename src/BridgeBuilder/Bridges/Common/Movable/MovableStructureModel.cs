using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Applies an explicit, measured road-removal plan to an authored structural mesh. Triangle
/// membership is never inferred from materials, bones, normals, bounds or connected islands.
/// Original channels remain raw. New subdivision vertices interpolate supported native attributes.
/// </summary>
internal static class MovableStructureModel
{
    // An offline-measured road/structure boundary on an original triangle edge. This
    // constructs a new vertex; it never lowers or otherwise rewrites an original vertex.
    internal readonly struct EdgeVertex
    {
        internal readonly int A, B;
        internal readonly float Fraction;
        internal EdgeVertex(int a, int b, float fraction) { A = a; B = b; Fraction = fraction; }
    }

    // A measured boundary turn inside a source triangle, expressed without a runtime
    // intersection or part classifier. WeightA is 1 - WeightB - WeightC.
    internal readonly struct TriangleVertex
    {
        internal readonly int A, B, C;
        internal readonly float WeightB, WeightC;
        internal TriangleVertex(int a, int b, int c, float weightB, float weightC)
        { A = a; B = b; C = c; WeightB = weightB; WeightC = weightC; }
    }

    internal sealed class Fragment
    {
        internal readonly int Slot, Triangle;
        internal readonly int[] Indices;
        // Appended vertices are addressed after the original source vertex count.
        internal Fragment(int slot, int triangle, params int[] indices)
        { Slot = slot; Triangle = triangle; Indices = (int[])indices.Clone(); }
    }

    internal readonly struct ProjectedUvFrame
    {
        // Gradient rows in final Plan.Positions coordinates. The offset components
        // do not affect tangent direction; source geometry and normals stay intact.
        internal readonly Vector4 U, V;
        internal ProjectedUvFrame(Vector4 u, Vector4 v) { U = u; V = v; }
    }

    // Final UVs address a resolved native material. Measured domain membership
    // and phase are supplied by the style; this record adds no geometry.
    internal sealed class PreparedDomain
    {
        internal readonly int Domain;
        internal readonly SurfaceAsset Surface;
        internal readonly IReadOnlyDictionary<int, Vector2> Uvs;
        internal readonly IReadOnlyDictionary<int, ProjectedUvFrame> TangentProjections;
        internal PreparedDomain(int domain, SurfaceAsset surface, KeyValuePair<int, Vector2>[] uvs,
            KeyValuePair<int, ProjectedUvFrame>[]? tangentProjections = null)
        {
            if (domain < 0 || surface == null) throw new ArgumentException("Invalid prepared structural domain.");
            Domain = domain; Surface = surface;
            var values = new Dictionary<int, Vector2>();
            foreach (var pair in uvs)
            {
                if (pair.Key < 0 || !float.IsFinite(pair.Value.x) || !float.IsFinite(pair.Value.y))
                    throw new ArgumentException("Invalid prepared structural UV.");
                values.Add(pair.Key, pair.Value);
            }
            Uvs = values;
            var projections = new Dictionary<int, ProjectedUvFrame>();
            if (tangentProjections != null)
                foreach (var pair in tangentProjections)
                {
                    if (!values.ContainsKey(pair.Key))
                        throw new ArgumentException("Prepared tangent projection requires its vertex UV.");
                    for (int axis = 0; axis < 4; axis++)
                        if (!float.IsFinite(pair.Value.U[axis]) || !float.IsFinite(pair.Value.V[axis]))
                            throw new ArgumentException("Prepared tangent projection is non-finite.");
                    projections.Add(pair.Key, pair.Value);
                }
            TangentProjections = projections;
        }
    }

    internal sealed class Plan
    {
        internal readonly Vector3[] SourcePositions, Positions;
        internal readonly int[][] SourceIndices;
        internal readonly bool[][] RemoveTriangles;
        internal readonly EdgeVertex[] AddedVertices;
        internal readonly TriangleVertex[] InteriorVertices;
        // Explicit exact source-attribute duplicates, appended after edge/interior vertices.
        internal readonly int[] SourceCopies;
        internal readonly Fragment[] Fragments;
        internal readonly KeyValuePair<int, Vector2>[] UvCorrections;
        // Measured, orientation-preserving direction Jacobians in the same coordinates
        // as SourcePositions/Positions. Indices address final Plan.Positions, including
        // explicitly constructed vertices; their raw source attributes interpolate first.
        internal readonly KeyValuePair<int, Matrix4x4>[] FrameTransforms;
        internal readonly PreparedDomain[] PreparedDomains;
        // Only measured LOD reconstruction opts in; ordinary width plans preserve Y/Z.
        internal readonly bool ReconstructsAuthoredPositions;
        internal int ConstructedCount => AddedVertices.Length + InteriorVertices.Length + SourceCopies.Length;

        internal TriangleVertex Construction(int index)
        {
            if (index >= AddedVertices.Length + InteriorVertices.Length)
            {
                int source = SourceCopies[index - AddedVertices.Length - InteriorVertices.Length];
                return new TriangleVertex(source, source, source, 0f, 0f);
            }
            if (index >= AddedVertices.Length) return InteriorVertices[index - AddedVertices.Length];
            var edge = AddedVertices[index];
            return new TriangleVertex(edge.A, edge.B, edge.A, edge.Fraction, 0f);
        }

        internal Plan(Vector3[] sourcePositions, Vector3[] positions, int[][] sourceIndices,
            bool[][] removeTriangles, EdgeVertex[]? addedVertices = null, Fragment[]? fragments = null,
            TriangleVertex[]? interiorVertices = null, float[]? constructedX = null,
            bool reconstructsAuthoredPositions = false, KeyValuePair<int, Vector2>[]? uvCorrections = null,
            PreparedDomain[]? preparedDomains = null,
            KeyValuePair<int, Matrix4x4>[]? frameTransforms = null, int[]? sourceCopies = null)
        {
            SourceCopies = sourceCopies == null ? Array.Empty<int>() : (int[])sourceCopies.Clone();
            foreach (int source in SourceCopies)
                if (source < 0 || source >= sourcePositions.Length)
                    throw new ArgumentException("Structural source copy must reference an original source vertex.");
            FrameTransforms = frameTransforms == null ? Array.Empty<KeyValuePair<int, Matrix4x4>>() :
                (KeyValuePair<int, Matrix4x4>[])frameTransforms.Clone();
            MovableStructureFrames.Validate(FrameTransforms, checked(sourcePositions.Length +
                (addedVertices?.Length ?? 0) + (interiorVertices?.Length ?? 0) + SourceCopies.Length));
            PreparedDomains = preparedDomains == null ? Array.Empty<PreparedDomain>() : (PreparedDomain[])preparedDomains.Clone();
            if (PreparedDomains.Select(d => d.Domain).Distinct().Count() != PreparedDomains.Length)
                throw new ArgumentException("Duplicate prepared structural domain.");
            UvCorrections = uvCorrections == null ? Array.Empty<KeyValuePair<int, Vector2>>() : (KeyValuePair<int, Vector2>[])uvCorrections.Clone();
            foreach (var correction in UvCorrections)
                if (correction.Key < 0 || correction.Key >= sourcePositions.Length ||
                    !float.IsFinite(correction.Value.x) || !float.IsFinite(correction.Value.y))
                    throw new ArgumentException("Invalid measured structural UV correction.");
            ReconstructsAuthoredPositions = reconstructsAuthoredPositions;
            SourcePositions = (Vector3[])sourcePositions.Clone();
            SourceIndices = sourceIndices.Select(indices => (int[])indices.Clone()).ToArray();
            RemoveTriangles = removeTriangles.Select(mask => (bool[])mask.Clone()).ToArray();
            AddedVertices = addedVertices == null ? Array.Empty<EdgeVertex>() : (EdgeVertex[])addedVertices.Clone();
            InteriorVertices = interiorVertices == null ? Array.Empty<TriangleVertex>() : (TriangleVertex[])interiorVertices.Clone();
            Fragments = fragments?.GroupBy(f => (f.Slot, f.Triangle))
                .Select(group => new Fragment(group.Key.Slot, group.Key.Triangle,
                    group.SelectMany(f => f.Indices).ToArray())).ToArray()
                ?? Array.Empty<Fragment>();
            if (positions.Length != sourcePositions.Length)
                throw new ArgumentException("A structural plan must preserve every original vertex.");
            Positions = new Vector3[checked(positions.Length + ConstructedCount)];
            Array.Copy(positions, Positions, positions.Length);
            var edgeA = new int[AddedVertices.Length];
            var edgeB = new int[AddedVertices.Length];
            var edgeOutput = new int[AddedVertices.Length];
            var edgeWeight = new float[AddedVertices.Length];
            for (var i = 0; i < AddedVertices.Length; i++)
            {
                var edge = AddedVertices[i];
                if (edge.A < 0 || edge.A >= positions.Length || edge.B < 0 || edge.B >= positions.Length ||
                    !float.IsFinite(edge.Fraction) || edge.Fraction <= 0f || edge.Fraction >= 1f)
                    throw new ArgumentException($"Constructed structural vertex {i} has an invalid measured edge.");
                edgeA[i] = edge.A; edgeB[i] = edge.B;
                edgeOutput[i] = positions.Length + i; edgeWeight[i] = edge.Fraction;
            }
            if (AddedVertices.Length != 0)
                AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.EdgeXyz, new[] { positions, Positions },
                    new[] { edgeWeight }, Array.Empty<float>(), count: AddedVertices.Length,
                    indices: new[] { edgeA, edgeB, edgeOutput });
            var interiorA = new int[InteriorVertices.Length];
            var interiorB = new int[InteriorVertices.Length];
            var interiorC = new int[InteriorVertices.Length];
            var interiorOutput = new int[InteriorVertices.Length];
            var weightB = new float[InteriorVertices.Length];
            var weightC = new float[InteriorVertices.Length];
            for (var i = 0; i < InteriorVertices.Length; i++)
            {
                var vertex = InteriorVertices[i];
                if (vertex.A < 0 || vertex.A >= positions.Length || vertex.B < 0 || vertex.B >= positions.Length ||
                    vertex.C < 0 || vertex.C >= positions.Length || vertex.A == vertex.B || vertex.A == vertex.C || vertex.B == vertex.C ||
                    !float.IsFinite(vertex.WeightB) || !float.IsFinite(vertex.WeightC) ||
                    vertex.WeightB <= 0f || vertex.WeightC <= 0f || vertex.WeightB + vertex.WeightC >= 1f)
                    throw new ArgumentException($"Interior structural vertex {i} has invalid measured barycentric data.");
                interiorA[i] = vertex.A; interiorB[i] = vertex.B; interiorC[i] = vertex.C;
                interiorOutput[i] = positions.Length + AddedVertices.Length + i;
                weightB[i] = vertex.WeightB; weightC[i] = vertex.WeightC;
            }
            if (InteriorVertices.Length != 0)
                AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.InteriorXyz, new[] { positions, Positions },
                    new[] { weightB, weightC }, Array.Empty<float>(), count: InteriorVertices.Length,
                    indices: new[] { interiorA, interiorB, interiorC, interiorOutput });
            for (int i = 0; i < SourceCopies.Length; i++)
                Positions[positions.Length + AddedVertices.Length + InteriorVertices.Length + i] = positions[SourceCopies[i]];
            // A merged LOD face may contain independently transformed authored parts.
            // Their new boundary X values come from offline part data, not interpolation
            // of the old face's widened corners. Original vertices and new Y/Z stay intact.
            if (constructedX != null)
            {
                if (constructedX.Length != ConstructedCount)
                    throw new ArgumentException("Every subdivided structural vertex requires its measured X output.");
                for (var i = 0; i < constructedX.Length; i++)
                {
                    if (!float.IsFinite(constructedX[i]))
                        throw new ArgumentException("A subdivided structural X coordinate is non-finite.");
                    Positions[positions.Length + i].x = constructedX[i];
                }
            }
            foreach (var fragment in Fragments)
            {
                if (fragment.Slot < 0 || fragment.Slot >= SourceIndices.Length || fragment.Slot >= RemoveTriangles.Length ||
                    fragment.Triangle < 0 || fragment.Triangle >= RemoveTriangles[fragment.Slot].Length ||
                    fragment.Triangle >= SourceIndices[fragment.Slot].Length / 3 ||
                    fragment.Indices.Length % 3 != 0)
                    throw new ArgumentException("A measured road fragment has an invalid source triangle or incomplete index stream.");
                var original = SourceIndices[fragment.Slot];
                bool OnTriangle(int index) => index == original[fragment.Triangle * 3] ||
                    index == original[fragment.Triangle * 3 + 1] || index == original[fragment.Triangle * 3 + 2];
                foreach (var index in fragment.Indices)
                {
                    if (index < 0 || index >= Positions.Length)
                        throw new ArgumentException("A measured road fragment references a missing vertex.");
                    if (index < positions.Length)
                    {
                        if (!OnTriangle(index)) throw new ArgumentException("A road fragment references another source triangle.");
                    }
                    else
                    {
                        var vertex = Construction(index - positions.Length);
                        if (!OnTriangle(vertex.A) || !OnTriangle(vertex.B) || !OnTriangle(vertex.C))
                            throw new ArgumentException("A road fragment boundary is not on its measured source triangle.");
                    }
                }
                RemoveTriangles[fragment.Slot][fragment.Triangle] = true;
            }
        }
    }

    internal static bool TryBuild(string name, Mesh source, Plan plan, int boneCount,
        out ModelImporter.Model? model, out string? error, bool compact = false)
    {
        model = null; error = null;
        // Native fixed supports have no skinning channel and legitimately use zero bones.
        // Existing BlendIndices, when present, are still checked against this count below.
        if (source == null || plan == null || source.vertexCount == 0 || boneCount < 0 ||
            plan.SourcePositions.Length != source.vertexCount ||
            plan.Positions.Length != source.vertexCount + plan.ConstructedCount ||
            plan.SourceIndices.Length != source.subMeshCount || plan.RemoveTriangles.Length != source.subMeshCount)
        { error = "Structure replacement requires complete per-mesh, per-vertex and per-slot measurements."; return false; }
        var attributes = new List<ModelImporter.Model.VertexData>();
        NativeArray<int> nativeIndices = default;
        try
        {
            var positions = source.vertices;
            var outputCount = plan.Positions.Length;
            if (positions.Length != plan.SourcePositions.Length)
            { error = "The structural position channel does not match the measured vertex count."; return false; }
            for (var i = 0; i < positions.Length; i++)
            {
                // Exact component comparisons, not Unity's approximate Vector3 equality. These
                // are identity checks against recorded input, not spatial part classification.
                for (var axis = 0; axis < 3; axis++)
                    if (positions[i][axis] != plan.SourcePositions[i][axis] || !float.IsFinite(plan.Positions[i][axis]))
                    { error = $"Structural vertex {i} does not match its measured source or has invalid output."; return false; }
            }
            for (var i = positions.Length; i < outputCount; i++)
                if (!float.IsFinite(plan.Positions[i].x) || !float.IsFinite(plan.Positions[i].y) ||
                    !float.IsFinite(plan.Positions[i].z))
                { error = $"Constructed structural vertex {i} is non-finite."; return false; }
            var indices = new List<int>();
            var submeshes = new SubMeshDescriptor[source.subMeshCount];
            for (var slot = 0; slot < source.subMeshCount; slot++)
            {
                var recorded = plan.SourceIndices[slot];
                var removed = plan.RemoveTriangles[slot];
                var actual = source.GetIndices(slot);
                if (source.GetTopology(slot) != MeshTopology.Triangles || actual.Length % 3 != 0 ||
                    removed.Length != actual.Length / 3 || !actual.SequenceEqual(recorded))
                { error = $"Structural slot {slot} does not match its measured triangle stream."; return false; }
                var start = indices.Count;
                var bounds = new Bounds(); var initialized = false;
                var first = outputCount; var last = -1;
                var fragments = plan.Fragments.Where(f => f.Slot == slot).ToDictionary(f => f.Triangle);
                void Append(int index)
                {
                    indices.Add(index);
                    first = Math.Min(first, index); last = Math.Max(last, index);
                    if (!initialized) { bounds = new Bounds(plan.Positions[index], Vector3.zero); initialized = true; }
                    else bounds.Encapsulate(plan.Positions[index]);
                }
                for (var triangle = 0; triangle < removed.Length; triangle++)
                {
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var index = actual[triangle * 3 + corner];
                        if (index < 0 || index >= positions.Length)
                        { error = $"Structural slot {slot} has an invalid vertex index."; return false; }
                        if (removed[triangle]) continue;
                        Append(index);
                    }
                    if (fragments.TryGetValue(triangle, out var fragment))
                        foreach (var index in fragment.Indices) Append(index);
                }
                // Empty slots remain in place, keeping every subsequent surface index stable.
                submeshes[slot] = new SubMeshDescriptor(start, indices.Count - start, MeshTopology.Triangles)
                { bounds = bounds, baseVertex = 0, firstVertex = initialized ? first : 0,
                    vertexCount = initialized ? last - first + 1 : 0 };
            }
            // Supplemental LOD parts carry only their referenced vertices; preserve every
            // raw attribute byte while removing unused source geometry from the payload.
            var used = compact ? indices.Distinct().OrderBy(index => index).ToArray() : null;
            if (used != null)
            {
                var remap = new Dictionary<int, int>();
                for (var i = 0; i < used.Length; i++) remap.Add(used[i], i);
                for (var i = 0; i < indices.Count; i++) indices[i] = remap[indices[i]];
                for (var slot = 0; slot < submeshes.Length; slot++)
                {
                    var descriptor = submeshes[slot];
                    descriptor.firstVertex = 0;
                    descriptor.vertexCount = used.Length;
                    submeshes[slot] = descriptor;
                }
            }
            var declared = source.GetVertexAttributes();
            if (!declared.Any(a => a.attribute == VertexAttribute.Position))
            { error = "The structural mesh has no position channel."; return false; }
            foreach (var attribute in declared)
                if (Size(attribute.format) == 0 || attribute.dimension <= 0 ||
                    (attribute.attribute == VertexAttribute.Position &&
                     (attribute.format != VertexAttributeFormat.Float32 || attribute.dimension != 3)) ||
                    (attribute.attribute == VertexAttribute.BlendIndices &&
                     (attribute.format != VertexAttributeFormat.UInt32 || attribute.dimension != 1)))
                { error = $"Unsupported structural attribute {attribute.attribute}:{attribute.format}x{attribute.dimension}."; return false; }

            using var read = Mesh.AcquireReadOnlyMeshData(source);
            foreach (var declaration in declared)
            {
                // Constructed UVs need sub-texel interpolation; quantizing them
                // back to Half can erase the gradient on narrow authored fragments.
                var attribute=declaration;
                bool promote=(plan.AddedVertices.Length + plan.InteriorVertices.Length)>0 && attribute.attribute==VertexAttribute.TexCoord0 &&
                    attribute.format==VertexAttributeFormat.Float16;
                if(promote)attribute=new VertexAttributeDescriptor(attribute.attribute,VertexAttributeFormat.Float32,attribute.dimension,attribute.stream);
                var size = Size(attribute.format) * attribute.dimension;
                var bytes = new byte[checked(size * outputCount)];
                if (attribute.attribute == VertexAttribute.Position)
                {
                    if (Marshal.SizeOf<Vector3>() != 12 || bytes.Length != checked(outputCount * 12))
                        throw new InvalidOperationException("Position output requires native packed Float32 XYZ.");
                    MemoryMarshal.AsBytes(plan.Positions.AsSpan(0, outputCount)).CopyTo(bytes);
                }
                else
                {
                    var raw = read[0].GetVertexData<byte>(attribute.stream);
                    var stride = source.GetVertexBufferStride(attribute.stream);
                    var offset = source.GetVertexAttributeOffset(attribute.attribute);
                    var sourceSize=Size(declaration.format)*declaration.dimension;
                    if (offset < 0 || stride < offset + sourceSize ||
                        (long)(positions.Length - 1) * stride + offset + sourceSize > raw.Length)
                    { error = $"Structural channel {attribute.attribute} has an incomplete raw buffer."; return false; }
                    for (var i = 0; i < positions.Length; i++)
                    {
                        if(!promote){NativeArray<byte>.Copy(raw,i*stride+offset,bytes,i*size,size);continue;}
                        for(int axis=0;axis<attribute.dimension;axis++)
                        {
                            int at=i*stride+offset+axis*2;
                            float value=(float)new half {value=(ushort)(raw[at]|raw[at+1]<<8)};
                            Buffer.BlockCopy(BitConverter.GetBytes(value),0,bytes,i*size+axis*4,4);
                        }
                    }
                    if (attribute.attribute == VertexAttribute.TexCoord0 && plan.UvCorrections.Length != 0)
                    {
                        if (attribute.dimension < 2 || (attribute.format != VertexAttributeFormat.Float32 && attribute.format != VertexAttributeFormat.Float16))
                            throw new InvalidOperationException("Measured UV correction requires a floating-point UV0 channel.");
                        var width = Size(attribute.format);
                        foreach (var correction in plan.UvCorrections.Where(c => c.Key < positions.Length))
                            for (var axis = 0; axis < 2; axis++)
                            {
                                var value = axis == 0 ? correction.Value.x : correction.Value.y;
                                var encoded = width == 4 ? BitConverter.GetBytes(value) : BitConverter.GetBytes(new half(value).value);
                                Buffer.BlockCopy(encoded, 0, bytes, correction.Key * size + axis * width, width);
                            }
                    }
                    for (var i = 0; i < plan.ConstructedCount; i++)
                    {
                        var vertex = plan.Construction(i);
                        var a = vertex.A * size; var b = vertex.B * size; var c = vertex.C * size;
                        var destination = (positions.Length + i) * size;
                        if (vertex.WeightB == 0f && vertex.WeightC == 0f)
                        { Buffer.BlockCopy(bytes, a, bytes, destination, size); continue; }
                        if (vertex.WeightB == 1f && vertex.WeightC == 0f)
                        { Buffer.BlockCopy(bytes, b, bytes, destination, size); continue; }
                        var identical = true;
                        for (var k = 0; k < size; k++)
                            identical &= bytes[a + k] == bytes[b + k] && bytes[a + k] == bytes[c + k];
                        if (identical) { Buffer.BlockCopy(bytes, a, bytes, destination, size); continue; }
                        if (MovableStructureAttributes.TryInterpolate(attribute, bytes,
                            a, b, c, destination, vertex.WeightB, vertex.WeightC)) continue;
                        // Other UVs retain the source format. Integer bone identities and unknown
                        // encodings are never blended into a guessed native value.
                        if (attribute.attribute < VertexAttribute.TexCoord0 || attribute.attribute > VertexAttribute.TexCoord7 ||
                            (attribute.format != VertexAttributeFormat.Float16 && attribute.format != VertexAttributeFormat.Float32))
                        { error = $"Constructed structural vertex {i} requires an explicit conversion for {attribute.attribute}."; return false; }
                        var width = Size(attribute.format);
                        for (var k = 0; k < attribute.dimension; k++)
                        {
                            var start = attribute.format == VertexAttributeFormat.Float32 ? BitConverter.ToSingle(bytes, a + k * width)
                                : (float)new half { value = BitConverter.ToUInt16(bytes, a + k * width) };
                            var end = attribute.format == VertexAttributeFormat.Float32 ? BitConverter.ToSingle(bytes, b + k * width)
                                : (float)new half { value = BitConverter.ToUInt16(bytes, b + k * width) };
                            var value = start + (end - start) * vertex.WeightB;
                            if (vertex.WeightC != 0f)
                            {
                                var third = attribute.format == VertexAttributeFormat.Float32 ? BitConverter.ToSingle(bytes, c + k * width)
                                    : (float)new half { value = BitConverter.ToUInt16(bytes, c + k * width) };
                                value += (third - start) * vertex.WeightC;
                            }
                            if (!float.IsFinite(value))
                            { error = $"Constructed structural vertex {i} has non-finite {attribute.attribute}."; return false; }
                            var encoded = attribute.format == VertexAttributeFormat.Float32 ? BitConverter.GetBytes(value)
                                : BitConverter.GetBytes(new half(value).value);
                            Buffer.BlockCopy(encoded, 0, bytes, destination + k * width, width);
                        }
                    }
                }
                // Seam twins share interpolated raw attributes, but have independent UV0.
                if (attribute.attribute == VertexAttribute.TexCoord0)
                    foreach (var correction in plan.UvCorrections.Where(c => c.Key >= positions.Length))
                    {
                        if (attribute.dimension < 2 || attribute.format is not (VertexAttributeFormat.Float32 or VertexAttributeFormat.Float16))
                            throw new InvalidOperationException("Lift05 seam UV requires floating-point UV0.");
                        int width = Size(attribute.format);
                        for (int axis = 0; axis < 2; axis++)
                        {
                            float value = correction.Value[axis];
                            var encoded = width == 4 ? BitConverter.GetBytes(value) : BitConverter.GetBytes(new half(value).value);
                            Buffer.BlockCopy(encoded, 0, bytes, correction.Key * size + axis * width, width);
                        }
                    }
                if (attribute.attribute == VertexAttribute.BlendIndices)
                    for (var i = 0; i < outputCount; i++)
                        if (BitConverter.ToUInt32(bytes, i * 4) >= boneCount)
                        { error = $"Structural vertex {i} references a missing procedural bone."; return false; }
                if (used != null)
                {
                    var packed = new byte[checked(used.Length * size)];
                    for (var i = 0; i < used.Length; i++)
                        Buffer.BlockCopy(bytes, used[i] * size, packed, i * size, size);
                    bytes = packed;
                }
                attributes.Add(new ModelImporter.Model.VertexData(attribute.attribute, attribute.format,
                    attribute.dimension, new NativeArray<byte>(bytes, Allocator.Persistent), false));
            }
            MovableStructureFrames.Apply(attributes, plan.FrameTransforms, used, used?.Length ?? outputCount);
            nativeIndices = new NativeArray<int>(indices.ToArray(), Allocator.Persistent);
            model = new ModelImporter.Model(name, Matrix4x4.identity, used?.Length ?? outputCount, nativeIndices,
                attributes.ToArray(), submeshes, -1, Array.Empty<ModelImporter.Model.BoneInfo>());
            return true;
        }
        catch (Exception exception)
        { error = "Measured structure replacement failed: " + exception.Message; return false; }
        finally
        {
            if (model == null)
            {
                if (nativeIndices.IsCreated) nativeIndices.Dispose();
                foreach (var attribute in attributes) attribute.Dispose();
            }
        }
    }

    private static int Size(VertexAttributeFormat format) => format switch
    {
        VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32 => 4,
        VertexAttributeFormat.Float16 or VertexAttributeFormat.UNorm16 or VertexAttributeFormat.SNorm16 or
            VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16 => 2,
        VertexAttributeFormat.UNorm8 or VertexAttributeFormat.SNorm8 or VertexAttributeFormat.UInt8 or
            VertexAttributeFormat.SInt8 => 1,
        _ => 0
    };
}
