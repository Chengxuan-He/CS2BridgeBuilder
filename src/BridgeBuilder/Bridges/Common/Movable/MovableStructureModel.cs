using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.AssetPipeline.Importers;
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

    internal sealed class Plan
    {
        internal readonly Vector3[] SourcePositions, Positions;
        internal readonly int[][] SourceIndices;
        internal readonly bool[][] RemoveTriangles;
        internal readonly EdgeVertex[] AddedVertices;
        internal readonly TriangleVertex[] InteriorVertices;
        internal readonly Fragment[] Fragments;
        // Only measured LOD reconstruction opts in; ordinary width plans preserve Y/Z.
        internal readonly bool ReconstructsAuthoredPositions;
        internal int ConstructedCount => AddedVertices.Length + InteriorVertices.Length;

        internal TriangleVertex Construction(int index)
        {
            if (index >= AddedVertices.Length) return InteriorVertices[index - AddedVertices.Length];
            var edge = AddedVertices[index];
            return new TriangleVertex(edge.A, edge.B, edge.A, edge.Fraction, 0f);
        }

        internal Plan(Vector3[] sourcePositions, Vector3[] positions, int[][] sourceIndices,
            bool[][] removeTriangles, EdgeVertex[]? addedVertices = null, Fragment[]? fragments = null,
            TriangleVertex[]? interiorVertices = null, float[]? constructedX = null,
            bool reconstructsAuthoredPositions = false)
        {
            ReconstructsAuthoredPositions = reconstructsAuthoredPositions;
            SourcePositions = (Vector3[])sourcePositions.Clone();
            SourceIndices = sourceIndices.Select(indices => (int[])indices.Clone()).ToArray();
            RemoveTriangles = removeTriangles.Select(mask => (bool[])mask.Clone()).ToArray();
            AddedVertices = addedVertices == null ? Array.Empty<EdgeVertex>() : (EdgeVertex[])addedVertices.Clone();
            InteriorVertices = interiorVertices == null ? Array.Empty<TriangleVertex>() : (TriangleVertex[])interiorVertices.Clone();
            Fragments = fragments?.Select(f => new Fragment(f.Slot, f.Triangle, f.Indices)).ToArray()
                ?? Array.Empty<Fragment>();
            if (positions.Length != sourcePositions.Length)
                throw new ArgumentException("A structural plan must preserve every original vertex.");
            Positions = new Vector3[checked(positions.Length + ConstructedCount)];
            Array.Copy(positions, Positions, positions.Length);
            for (var i = 0; i < AddedVertices.Length; i++)
            {
                var edge = AddedVertices[i];
                if (edge.A < 0 || edge.A >= positions.Length || edge.B < 0 || edge.B >= positions.Length ||
                    !float.IsFinite(edge.Fraction) || edge.Fraction <= 0f || edge.Fraction >= 1f)
                    throw new ArgumentException($"Constructed structural vertex {i} has an invalid measured edge.");
                Positions[positions.Length + i] = Vector3.LerpUnclamped(positions[edge.A], positions[edge.B], edge.Fraction);
            }
            for (var i = 0; i < InteriorVertices.Length; i++)
            {
                var vertex = InteriorVertices[i];
                if (vertex.A < 0 || vertex.A >= positions.Length || vertex.B < 0 || vertex.B >= positions.Length ||
                    vertex.C < 0 || vertex.C >= positions.Length || vertex.A == vertex.B || vertex.A == vertex.C || vertex.B == vertex.C ||
                    !float.IsFinite(vertex.WeightB) || !float.IsFinite(vertex.WeightC) ||
                    vertex.WeightB <= 0f || vertex.WeightC <= 0f || vertex.WeightB + vertex.WeightC >= 1f)
                    throw new ArgumentException($"Interior structural vertex {i} has invalid measured barycentric data.");
                var a = positions[vertex.A];
                Positions[positions.Length + AddedVertices.Length + i] = a +
                    (positions[vertex.B] - a) * vertex.WeightB + (positions[vertex.C] - a) * vertex.WeightC;
            }
            var replaced = new HashSet<(int, int)>();
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
                    fragment.Indices.Length == 0 || fragment.Indices.Length % 3 != 0 ||
                    !replaced.Add((fragment.Slot, fragment.Triangle)))
                    throw new ArgumentException("A measured road fragment has an invalid or duplicate source triangle.");
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
                if (!plan.ReconstructsAuthoredPositions &&
                    (plan.Positions[i].y != positions[i].y || plan.Positions[i].z != positions[i].z))
                { error = $"Structural vertex {i} changes an authored Y/Z coordinate."; return false; }
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
            foreach (var attribute in declared)
            {
                var size = Size(attribute.format) * attribute.dimension;
                var bytes = new byte[checked(size * outputCount)];
                if (attribute.attribute == VertexAttribute.Position)
                {
                    var values = new float[outputCount * 3];
                    for (var i = 0; i < outputCount; i++)
                    {
                        values[i * 3] = plan.Positions[i].x;
                        values[i * 3 + 1] = i < positions.Length ? positions[i].y : plan.Positions[i].y;
                        values[i * 3 + 2] = i < positions.Length ? positions[i].z : plan.Positions[i].z;
                    }
                    Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
                }
                else
                {
                    var raw = read[0].GetVertexData<byte>(attribute.stream);
                    var stride = source.GetVertexBufferStride(attribute.stream);
                    var offset = source.GetVertexAttributeOffset(attribute.attribute);
                    if (offset < 0 || stride < offset + size ||
                        (long)(positions.Length - 1) * stride + offset + size > raw.Length)
                    { error = $"Structural channel {attribute.attribute} has an incomplete raw buffer."; return false; }
                    for (var i = 0; i < positions.Length; i++)
                        NativeArray<byte>.Copy(raw, i * stride + offset, bytes, i * size, size);
                    for (var i = 0; i < plan.ConstructedCount; i++)
                    {
                        var vertex = plan.Construction(i);
                        var a = vertex.A * size; var b = vertex.B * size; var c = vertex.C * size;
                        var destination = (positions.Length + i) * size;
                        var identical = true;
                        for (var k = 0; k < size; k++)
                            identical &= bytes[a + k] == bytes[b + k] && bytes[a + k] == bytes[c + k];
                        if (identical) { Buffer.BlockCopy(bytes, a, bytes, destination, size); continue; }
                        if (MovableStructureAttributes.TryInterpolate(attribute, bytes,
                            a, b, c, destination, vertex.WeightB, vertex.WeightC)) continue;
                        // UVs retain the source format. Integer bone identities and unknown
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
