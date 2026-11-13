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
            Domain = domain; Surface = surface;
            var values = new Dictionary<int, Vector2>();
            foreach (var pair in uvs)
            {
                values.Add(pair.Key, pair.Value);
            }
            Uvs = values;
            var projections = new Dictionary<int, ProjectedUvFrame>();
            if (tangentProjections != null)
                foreach (var pair in tangentProjections) projections.Add(pair.Key, pair.Value);
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
        internal readonly bool NativeBoneFrames;
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
            KeyValuePair<int, Matrix4x4>[]? frameTransforms = null, int[]? sourceCopies = null,
            bool nativeBoneFrames = false)
        {
            NativeBoneFrames = nativeBoneFrames;
            SourceCopies = sourceCopies == null ? Array.Empty<int>() : (int[])sourceCopies.Clone();
            FrameTransforms = frameTransforms == null ? Array.Empty<KeyValuePair<int, Matrix4x4>>() :
                (KeyValuePair<int, Matrix4x4>[])frameTransforms.Clone();
            PreparedDomains = preparedDomains == null ? Array.Empty<PreparedDomain>() : (PreparedDomain[])preparedDomains.Clone();
            UvCorrections = uvCorrections == null ? Array.Empty<KeyValuePair<int, Vector2>>() : (KeyValuePair<int, Vector2>[])uvCorrections.Clone();
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
            Positions = new Vector3[checked(positions.Length + ConstructedCount)];
            Array.Copy(positions, Positions, positions.Length);
            var edgeA = new int[AddedVertices.Length];
            var edgeB = new int[AddedVertices.Length];
            var edgeOutput = new int[AddedVertices.Length];
            var edgeWeight = new float[AddedVertices.Length];
            for (var i = 0; i < AddedVertices.Length; i++)
            {
                var edge = AddedVertices[i];
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
                for (var i = 0; i < constructedX.Length; i++)
                {
                    Positions[positions.Length + i].x = constructedX[i];
                }
            }
            foreach (var fragment in Fragments)
            {
                RemoveTriangles[fragment.Slot][fragment.Triangle] = true;
            }
        }
    }

    internal static bool TryBuild(string name, Mesh source, Plan plan, int boneCount,
        out ModelImporter.Model? model, out string? error, bool compact = false, int? rigidBone = null, int? sourceSubMesh = null)
    {
        model = null; error = null;
        var attributes = new List<ModelImporter.Model.VertexData>();
        NativeArray<int> nativeIndices = default;
        try
        {
            var positions = source.vertices;
            var outputCount = plan.Positions.Length;
            var indices = new List<int>();
            var submeshes = new SubMeshDescriptor[plan.SourceIndices.Length];
            for (var slot = 0; slot < plan.SourceIndices.Length; slot++)
            {
                var removed = plan.RemoveTriangles[slot];
                var sourceSlot = sourceSubMesh ?? slot;
                var actual = source.GetIndices(sourceSlot);
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

            using var read = Mesh.AcquireReadOnlyMeshData(source);
            foreach (var declaration in declared)
            {
                if (rigidBone.HasValue && (declaration.attribute == VertexAttribute.BlendIndices ||
                    declaration.attribute == VertexAttribute.BlendWeight)) continue;
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
                    MemoryMarshal.AsBytes(plan.Positions.AsSpan(0, outputCount)).CopyTo(bytes);
                }
                else
                {
                    var raw = read[0].GetVertexData<byte>(attribute.stream);
                    var stride = source.GetVertexBufferStride(attribute.stream);
                    var offset = source.GetVertexAttributeOffset(attribute.attribute);
                    var sourceSize=Size(declaration.format)*declaration.dimension;
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
                        int width = Size(attribute.format);
                        for (int axis = 0; axis < 2; axis++)
                        {
                            float value = correction.Value[axis];
                            var encoded = width == 4 ? BitConverter.GetBytes(value) : BitConverter.GetBytes(new half(value).value);
                            Buffer.BlockCopy(encoded, 0, bytes, correction.Key * size + axis * width, width);
                        }
                    }
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
            if (rigidBone.HasValue)
            {
                // Native rigid procedural skinning uses one UInt32 index, with implicit
                // weight one. This is the existing mechanism skeleton, not a child pose.
                var bound = new uint[used?.Length ?? outputCount];
                for (int i = 0; i < bound.Length; i++) bound[i] = (uint)rigidBone.Value;
                var bytes = new byte[checked(bound.Length * sizeof(uint))];
                Buffer.BlockCopy(bound, 0, bytes, 0, bytes.Length);
                attributes.Add(new ModelImporter.Model.VertexData(VertexAttribute.BlendIndices,
                    VertexAttributeFormat.UInt32, 1, new NativeArray<byte>(bytes, Allocator.Persistent), false));
            }
            MovableStructureFrames.Apply(attributes, plan.FrameTransforms, used, used?.Length ?? outputCount, plan.NativeBoneFrames);
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
