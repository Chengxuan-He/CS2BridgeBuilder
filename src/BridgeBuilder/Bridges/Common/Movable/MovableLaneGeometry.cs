using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

/// <summary>Materializes non-decal native lane meshes in the closed road frame.</summary>
internal static class MovableLaneGeometry
{
    internal sealed class Result
    {
        internal readonly MovableRoadGeometry.Triangle[] Triangles;
        internal readonly SurfaceAsset[] Surfaces;
        internal readonly MovableLaneVisuals.Batch[] Decals;
        internal Result(List<MovableRoadGeometry.Triangle> triangles, List<SurfaceAsset> surfaces,
            List<MovableLaneVisuals.Batch> decals)
        { Triangles = triangles.ToArray(); Surfaces = surfaces.ToArray(); Decals = decals.ToArray(); }
    }

    internal static Result Compile(IReadOnlyList<MovableLaneVisuals.Batch> batches,
        IReadOnlyList<MovableRoadGeometry.Triangle> receiver)
    {
        var triangles = new List<MovableRoadGeometry.Triangle>();
        var surfaces = new List<SurfaceAsset>();
        var decals = new List<MovableLaneVisuals.Batch>();
        var meshes = new Dictionary<RenderPrefab, Mesh[]>();
        try
        {
            for (var occurrence = 0; occurrence < batches.Count; occurrence++)
            {
                var batch = batches[occurrence];
                if (batch.Decal) { decals.Add(batch); continue; }
                if (!meshes.TryGetValue(batch.Source, out var parts))
                {
                    parts = PrivateGeometryReader.Read(batch.Source);
                    meshes.Add(batch.Source, parts);
                }
                if (parts.Length == 0)
                    throw new InvalidDataException("No lane geometry could be read for " + batch.Source.name);
                var firstMaterial = surfaces.Count;
                surfaces.AddRange(batch.Surfaces);
                var slot = 0;
                var sourceTriangle = 0;
                foreach (var mesh in parts)
                {
                    var vertices = ReadVertices(mesh);
                    for (var i = 0; i < vertices.Length; i++)
                        vertices[i] = MovableLaneDeformation.Evaluate(batch, vertices[i],
                            position => ReceiverHeight(receiver, position));
                    for (var submesh = 0; submesh < mesh.subMeshCount; submesh++, slot++)
                    {
                        if (slot >= batch.Surfaces.Length || mesh.GetTopology(submesh) != MeshTopology.Triangles)
                            throw new InvalidDataException("Invalid lane material/topology in " + batch.Source.name);
                        var indices = mesh.GetIndices(submesh);
                        if (indices.Length % 3 != 0) throw new InvalidDataException("Incomplete lane triangle.");
                        for (var i = 0; i < indices.Length; i += 3)
                            triangles.Add(new MovableRoadGeometry.Triangle(vertices[indices[i]],
                                vertices[indices[i + 1]], vertices[indices[i + 2]],
                                firstMaterial + slot, occurrence, sourceTriangle++));
                    }
                }
                if (slot != batch.Surfaces.Length)
                    throw new InvalidDataException("Lane surface slots do not match geometry: " + batch.Source.name);
            }
            return new Result(triangles, surfaces, decals);
        }
        finally
        {
            foreach (var parts in meshes.Values) PrivateGeometryReader.Release(parts);
        }
    }

    private static MovableRoadGeometry.Vertex[] ReadVertices(Mesh mesh)
    {
        if (!mesh.HasVertexAttribute(VertexAttribute.Normal) || !mesh.HasVertexAttribute(VertexAttribute.Tangent))
            throw new InvalidDataException("Lane mesh has no normal/tangent frame: " + mesh.name);
        using var raw = Mesh.AcquireReadOnlyMeshData(mesh);
        using var normalBytes = Attribute(mesh, raw[0], VertexAttribute.Normal);
        using var tangentBytes = Attribute(mesh, raw[0], VertexAttribute.Tangent);
        using var normals = new NativeArray<MeshNormal>(mesh.vertexCount, Allocator.Temp);
        using var tangents = new NativeArray<MeshTangent>(mesh.vertexCount, Allocator.Temp);
        MeshNormal.Unpack(new NativeSlice<byte>(normalBytes), normals, mesh.vertexCount,
            mesh.GetVertexAttributeFormat(VertexAttribute.Normal), mesh.GetVertexAttributeDimension(VertexAttribute.Normal));
        MeshTangent.Unpack(new NativeSlice<byte>(tangentBytes), tangents, mesh.vertexCount,
            mesh.GetVertexAttributeFormat(VertexAttribute.Tangent), mesh.GetVertexAttributeDimension(VertexAttribute.Tangent));
        var uv = new List<Vector4>(); mesh.GetUVs(0, uv);
        if (uv.Count != mesh.vertexCount) throw new InvalidDataException("Lane mesh has incomplete UVs: " + mesh.name);
        var positions = mesh.vertices;
        var colors = mesh.colors;
        var output = new MovableRoadGeometry.Vertex[mesh.vertexCount];
        for (var i = 0; i < output.Length; i++)
        {
            output[i] = new MovableRoadGeometry.Vertex(positions[i], normals[i].m_Normal,
                tangents[i].m_Tangent, uv[i], colors.Length == 0 ? Color.white : colors[i]);
            if (!MovableRoadGeometry.Valid(output[i]))
                throw new InvalidDataException("Invalid lane vertex in " + mesh.name);
        }
        return output;
    }

    private static NativeArray<byte> Attribute(Mesh mesh, Mesh.MeshData data, VertexAttribute attribute)
    {
        var format = mesh.GetVertexAttributeFormat(attribute);
        var element = format switch
        {
            VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32 => 4,
            VertexAttributeFormat.Float16 or VertexAttributeFormat.SNorm16 or VertexAttributeFormat.UNorm16
                or VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16 => 2,
            VertexAttributeFormat.UNorm8 or VertexAttributeFormat.SNorm8
                or VertexAttributeFormat.UInt8 or VertexAttributeFormat.SInt8 => 1,
            _ => throw new InvalidDataException("Unsupported lane vertex format: " + format)
        };
        var bytes = element * mesh.GetVertexAttributeDimension(attribute);
        var stream = mesh.GetVertexAttributeStream(attribute);
        var stride = mesh.GetVertexBufferStride(stream);
        var offset = mesh.GetVertexAttributeOffset(attribute);
        var source = data.GetVertexData<byte>(stream);
        var output = new NativeArray<byte>(mesh.vertexCount * bytes, Allocator.Temp);
        for (var i = 0; i < mesh.vertexCount; i++)
            NativeArray<byte>.Copy(source, i * stride + offset, output, i * bytes, bytes);
        return output;
    }

    // Project onto the selected road's actual closed surface, never city terrain. This is a
    // triangle/ray intersection for newly baked visual geometry, not structural-part inference.
    private static float ReceiverHeight(IReadOnlyList<MovableRoadGeometry.Triangle> receiver, Vector3 point)
    {
        var closest = float.PositiveInfinity;
        var height = 0f;
        foreach (var triangle in receiver)
        {
            var a = triangle.A.Position; var ab = triangle.B.Position - a; var ac = triangle.C.Position - a;
            var q = point - a;
            var determinant = ab.x * ac.z - ab.z * ac.x;
            if (determinant == 0f) continue;
            var u = (q.x * ac.z - q.z * ac.x) / determinant;
            var v = (ab.x * q.z - ab.z * q.x) / determinant;
            if (u < 0f || v < 0f || 1f - u - v < 0f) continue;
            var y = a.y + ab.y * u + ac.y * v;
            var distance = Mathf.Abs(y - point.y);
            if (distance < closest) { closest = distance; height = y; }
        }
        if (float.IsPositiveInfinity(closest))
            throw new InvalidDataException("Projected lane has no selected-road receiver at " + point);
        return height;
    }
}
