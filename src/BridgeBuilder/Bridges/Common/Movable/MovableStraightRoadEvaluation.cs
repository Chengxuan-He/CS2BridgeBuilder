using System;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Evaluates a native straight-edge composition mesh before binding it to mechanism bones.
/// The selectors and UV rule follow the installed NetCompositionMeshLit vertex program;
/// no composition matrix or matrix-selector color is required by the resulting road vertices.
/// Material conversion is a separate mandatory step, not established by this evaluation.
/// </summary>
internal static class MovableStraightRoadEvaluation
{
    internal static bool TryEvaluate(Mesh mesh, float width, float length, int occurrence,
        out MovableRoadGeometry.Triangle[] triangles, out string? error)
        => TryEvaluate(mesh, width, length, occurrence, null, out triangles, out error);

    internal static bool TryEvaluate(Mesh mesh, BatchDataHelpers.CompositionParameters parameters,
        float3 origin, int occurrence, out MovableRoadGeometry.Triangle[] triangles, out string? error)
        => TryEvaluate(mesh, 0f, 0f, occurrence, new MovableCompositionDeformation(parameters, origin),
            out triangles, out error);

    private static bool TryEvaluate(Mesh mesh, float width, float length, int occurrence,
        MovableCompositionDeformation? deformation, out MovableRoadGeometry.Triangle[] triangles, out string? error)
    {
        triangles = Array.Empty<MovableRoadGeometry.Triangle>(); error = null;
        if (mesh == null || mesh.vertexCount == 0 || occurrence < 0 ||
            (deformation == null && (!Finite(width) || !Finite(length) || width <= 0f || length <= 0f)))
        { error = "Straight road evaluation requires a native mesh, positive finite span and source occurrence."; return false; }
        if (!Layout(mesh, VertexAttribute.Position, VertexAttributeFormat.Float32, 3) ||
            !Layout(mesh, VertexAttribute.Normal, VertexAttributeFormat.SNorm16, 2) ||
            !Layout(mesh, VertexAttribute.Tangent, VertexAttributeFormat.Float32, 1) ||
            !Layout(mesh, VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4) ||
            !Layout(mesh, VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4))
        { error = "The road mesh does not match the measured native composition vertex layout."; return false; }
        try
        {
            using var raw = Mesh.AcquireReadOnlyMeshData(mesh);
            using var normalBytes = Attribute(mesh, raw[0], VertexAttribute.Normal, 4);
            using var tangentBytes = Attribute(mesh, raw[0], VertexAttribute.Tangent, 4);
            using var normals = new NativeArray<MeshNormal>(mesh.vertexCount, Allocator.Temp);
            using var tangents = new NativeArray<MeshTangent>(mesh.vertexCount, Allocator.Temp);
            MeshNormal.Unpack(new NativeSlice<byte>(normalBytes), normals, mesh.vertexCount,
                VertexAttributeFormat.SNorm16, 2);
            MeshTangent.Unpack(new NativeSlice<byte>(tangentBytes), tangents, mesh.vertexCount,
                VertexAttributeFormat.Float32, 1);
            var positions = mesh.vertices; var selectors = mesh.colors32;
            var uv = new List<Vector4>(); mesh.GetUVs(0, uv);
            if (positions.Length != mesh.vertexCount || selectors.Length != mesh.vertexCount || uv.Count != mesh.vertexCount)
            { error = "The road composition mesh has incomplete vertex channels."; return false; }
            var vertices = new MovableRoadGeometry.Vertex[mesh.vertexCount];
            for (var i = 0; i < vertices.Length; i++)
            {
                var selector = selectors[i]; var p = positions[i]; var texture = uv[i];
                if (deformation != null)
                {
                    vertices[i] = deformation.Evaluate(p, normals[i].m_Normal,
                        tangents[i].m_Tangent, texture, selector);
                    if (!MovableRoadGeometry.Valid(vertices[i]))
                    {
                        var v = vertices[i];
                        error = $"Road vertex {i} produced invalid native composition attributes: " +
                            $"position={v.Position}; normal={v.Normal}; tangent={v.Tangent}; UV={v.UV}; " +
                            $"packed position={p}; packed UV={texture}; selectors={selector}.";
                        return false;
                    }
                    continue;
                }
                // Straight edges use matrices (0,2) or (1,3). Node/divider and terrain-driven
                // vertex paths need their own evaluator, rather than a plausible flat fallback.
                if (!((selector.r == 0 && selector.g == 2) || (selector.r == 1 && selector.g == 3)) ||
                    selector.b != 0 || selector.a != 0)
                { error = $"Road vertex {i} requires a node, divider or terrain-dependent deformation path."; return false; }
                if (!Finite(p.x) || !Finite(p.y) || !Finite(p.z) || p.x < 0f || p.x > 1f || p.z < 0f || p.z > 1f)
                { error = $"Road vertex {i} has invalid packed composition coordinates."; return false; }
                var distance = (p.z + selector.r) * (length * .5f);
                p = new Vector3((p.x - .5f) * width, p.y, distance - length * .5f);
                // Native vertex shader: nonnegative V is a repetition rate; negative V is
                // authored non-tiling UV, recovered with frac. UV.w is retained for material
                // conversion because the network fragment shader can consume it separately.
                texture.y = texture.y >= 0f ? distance * texture.y : texture.y - Mathf.Floor(texture.y);
                texture.z = 0f;
                vertices[i] = new MovableRoadGeometry.Vertex(p, normals[i].m_Normal,
                    tangents[i].m_Tangent, texture, Color.white);
                if (!MovableRoadGeometry.Valid(vertices[i]))
                { error = $"Road vertex {i} produced invalid evaluated attributes."; return false; }
            }
            var result = new List<MovableRoadGeometry.Triangle>();
            var sourceTriangle = 0;
            for (var slot = 0; slot < mesh.subMeshCount; slot++)
            {
                if (mesh.GetTopology(slot) != MeshTopology.Triangles)
                { error = $"Road material slot {slot} uses unsupported primitive topology."; return false; }
                var indices = mesh.GetIndices(slot);
                if (indices.Length % 3 != 0)
                { error = $"Road material slot {slot} has an incomplete triangle."; return false; }
                foreach (var index in indices)
                    if (index < 0 || index >= vertices.Length)
                    { error = $"Road material slot {slot} has an invalid vertex index."; return false; }
                for (var i = 0; i < indices.Length; i += 3)
                    result.Add(new MovableRoadGeometry.Triangle(vertices[indices[i]], vertices[indices[i + 1]],
                        vertices[indices[i + 2]], slot, occurrence, sourceTriangle++));
            }
            if (result.Count == 0) { error = "The evaluated road contains no triangles."; return false; }
            triangles = result.ToArray(); return true;
        }
        catch (Exception exception)
        { error = "Straight road evaluation failed: " + exception.Message; return false; }
    }

    private static bool Layout(Mesh mesh, VertexAttribute attribute, VertexAttributeFormat format, int dimension)
        => mesh.HasVertexAttribute(attribute) && mesh.GetVertexAttributeFormat(attribute) == format &&
            mesh.GetVertexAttributeDimension(attribute) == dimension;

    private static NativeArray<byte> Attribute(Mesh mesh, Mesh.MeshData data, VertexAttribute attribute, int bytes)
    {
        var stream = mesh.GetVertexAttributeStream(attribute);
        var stride = mesh.GetVertexBufferStride(stream);
        var offset = mesh.GetVertexAttributeOffset(attribute);
        var source = data.GetVertexData<byte>(stream);
        var output = new NativeArray<byte>(mesh.vertexCount * bytes, Allocator.Temp);
        for (var i = 0; i < mesh.vertexCount; i++)
            NativeArray<byte>.Copy(source, i * stride + offset, output, i * bytes, bytes);
        return output;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
