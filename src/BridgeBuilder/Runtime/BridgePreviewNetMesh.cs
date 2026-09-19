using System;
using System.Collections.Generic;
using System.Reflection;
using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Runtime;

/// <summary>
/// Native network mesh generation in a private ECS world. A Net surface shader
/// does not consume an ordinary stretched MeshFilter: it needs the native packed
/// composition vertices AND the four Bezier composition matrices together.
/// </summary>
internal sealed class BridgePreviewNetMesh : IDisposable
{
    private readonly World _world = new("BridgeBuilder preview network meshes");
    private readonly List<Mesh> _meshes = new();
    private bool _disposed;

    internal bool Build(BridgePreviewComposition.Piece[] pieces, NetCompositionData data,
        BridgePreviewRenderResources resources, float length, out Mesh mesh,
        out Material[] materials, out MaterialPropertyBlock properties)
    {
        mesh = null!;
        materials = Array.Empty<Material>();
        properties = new MaterialPropertyBlock();
        if (_disposed || data.m_Width <= 0f || length <= 0f)
        {
            return false;
        }
        var manager = _world.EntityManager;
        var target = manager.CreateEntity();
        manager.AddBuffer<NetCompositionPiece>(target);
        manager.AddBuffer<MeshMaterial>(target);
        var input = new List<NetCompositionPiece>();
        var sourceMaterials = new List<Material>();
        var sources = new List<Entity>();
        var pending = Mesh.AllocateWritableMeshData(1);
        var consumed = false;
        try
        {
            foreach (var entry in pieces)
            {
                var piece = entry.Composition;
                if (!Visible(piece)) continue;
                var materialIndex = 0;
                var partMaterials = resources.Materials(entry.Prefab, true);
                var partMeshes = resources.Meshes(entry.Prefab);
                if (partMeshes.Length == 0)
                {
                    return false;
                }
                foreach (var source in partMeshes)
                {
                    if (source == null) continue;
                    if (materialIndex + source.subMeshCount > partMaterials.Length)
                    {
                        return false;
                    }
                    var entity = manager.CreateEntity();
                    sources.Add(entity);
                    Cache(source, entity, manager, sourceMaterials.Count);
                    for (var sub = 0; sub < source.subMeshCount; sub++)
                        sourceMaterials.Add(partMaterials[materialIndex++]);
                    piece.m_Piece = entity;
                    input.Add(piece);
                }
            }
            if (input.Count == 0)
            {
                return false;
            }
            var buffer = manager.GetBuffer<NetCompositionPiece>(target);
            foreach (var piece in input) buffer.Add(piece);
            var nativeData = new NetCompositionMeshData
            {
                m_Flags = data.m_Flags, m_Width = data.m_Width,
                m_MiddleOffset = data.m_MiddleOffset, m_HeightRange = data.m_HeightRange
            };
            if (!_world.GetOrCreateSystemManaged<MeshSystem>().Generate(nativeData, target, pending[0]))
                return false;
            var materialBuffer = manager.GetBuffer<MeshMaterial>(target);
            materials = new Material[materialBuffer.Length];
            for (var i = 0; i < materials.Length; i++)
                materials[i] = sourceMaterials[materialBuffer[i].m_MaterialIndex];
            mesh = new Mesh { name = "tmp_native_bridge_edge", hideFlags = HideFlags.HideAndDontSave };
            _meshes.Add(mesh);
            Mesh.ApplyAndDisposeWritableMeshData(pending, mesh, MeshUpdateFlags.DontRecalculateBounds);
            consumed = true;

            // The shader expands normalized composition coordinates to this edge.
            // Culling and the orthographic camera must use the expanded bounds.
            var bounds = new Bounds(new Vector3(0f, (data.m_HeightRange.min + data.m_HeightRange.max) * .5f, 0f),
                new Vector3(data.m_Width, data.m_HeightRange.max - data.m_HeightRange.min, length));
            mesh.bounds = bounds;
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                var sub = mesh.GetSubMesh(i);
                sub.bounds = bounds;
                mesh.SetSubMesh(i, sub, MeshUpdateFlags.DontRecalculateBounds);
            }
            var half = length * .5f;
            var edge = new EdgeGeometry
            {
                m_Bounds = new Bounds3(new float3(-data.m_Width * .5f, 0f, -half),
                    new float3(data.m_Width * .5f, 0f, half)),
                m_Start = Segment(data.m_Width, -half, 0f),
                m_End = Segment(data.m_Width, 0f, half)
            };
            BatchDataHelpers.CalculateEdgeParameters(edge, false, out var parameters);
            properties.SetMatrix("colossal_CompositionMatrix0", parameters.m_CompositionMatrix0);
            properties.SetMatrix("colossal_CompositionMatrix1", parameters.m_CompositionMatrix1);
            properties.SetMatrix("colossal_CompositionMatrix2", parameters.m_CompositionMatrix2);
            properties.SetMatrix("colossal_CompositionMatrix3", parameters.m_CompositionMatrix3);
            properties.SetFloat("colossal_LodFade", 1f);
            if (mesh.vertexCount != 0 && materials.Length != 0) return true;

            return false;
        }
        finally
        {
            if (!consumed) pending.Dispose();
            foreach (var source in sources) manager.DestroyEntity(source);
            manager.DestroyEntity(target);
        }
    }

    private static Segment Segment(float width, float start, float end) => new()
    {
        m_Left = Line(-width * .5f, start, end), m_Right = Line(width * .5f, start, end),
        m_Length = new float2(end - start)
    };

    private static Bezier4x3 Line(float x, float start, float end) => new(
        new float3(x, 0f, start), new float3(x, 0f, math.lerp(start, end, 1f / 3f)),
        new float3(x, 0f, math.lerp(start, end, 2f / 3f)), new float3(x, 0f, end));

    internal static bool Visible(NetCompositionPiece p)
        => (p.m_PieceFlags & NetPieceFlags.HasMesh) != 0 &&
        (p.m_SectionFlags & NetSectionFlags.Hidden) == 0 &&
        !((p.m_PieceFlags & NetPieceFlags.Surface) != 0 && (p.m_SectionFlags & NetSectionFlags.HiddenSurface) != 0) &&
        !((p.m_PieceFlags & NetPieceFlags.Bottom) != 0 && (p.m_SectionFlags & NetSectionFlags.HiddenBottom) != 0) &&
        !((p.m_PieceFlags & NetPieceFlags.Top) != 0 && (p.m_SectionFlags & NetSectionFlags.HiddenTop) != 0) &&
        !((p.m_PieceFlags & NetPieceFlags.Side) != 0 && (p.m_SectionFlags & NetSectionFlags.HiddenSide) != 0);

    private static void Cache(Mesh mesh, Entity entity, EntityManager manager, int firstMaterial)
    {
        manager.AddBuffer<MeshVertex>(entity);
        manager.AddBuffer<MeshNormal>(entity);
        manager.AddBuffer<MeshTangent>(entity);
        manager.AddBuffer<MeshUV0>(entity);
        manager.AddBuffer<MeshIndex>(entity);
        manager.AddBuffer<MeshMaterial>(entity);
        using var raw = Mesh.AcquireReadOnlyMeshData(mesh);
        // Copy interleaved attributes into contiguous buffers before calling the
        // game's decoders. GetNormals/GetTangents do not decode octahedral data.
        using var positions = Attribute(mesh, raw[0], VertexAttribute.Position);
        using var normals = Attribute(mesh, raw[0], VertexAttribute.Normal);
        using var tangents = Attribute(mesh, raw[0], VertexAttribute.Tangent);
        using var uv = Attribute(mesh, raw[0], VertexAttribute.TexCoord0);
        MeshVertex.Unpack(new NativeSlice<byte>(positions), manager.GetBuffer<MeshVertex>(entity), mesh.vertexCount,
            mesh.GetVertexAttributeFormat(VertexAttribute.Position), mesh.GetVertexAttributeDimension(VertexAttribute.Position));
        MeshNormal.Unpack(new NativeSlice<byte>(normals), manager.GetBuffer<MeshNormal>(entity), mesh.vertexCount,
            mesh.GetVertexAttributeFormat(VertexAttribute.Normal), mesh.GetVertexAttributeDimension(VertexAttribute.Normal));
        MeshTangent.Unpack(new NativeSlice<byte>(tangents), manager.GetBuffer<MeshTangent>(entity), mesh.vertexCount,
            mesh.GetVertexAttributeFormat(VertexAttribute.Tangent), mesh.GetVertexAttributeDimension(VertexAttribute.Tangent));
        MeshUV0.Unpack(new NativeSlice<byte>(uv), manager.GetBuffer<MeshUV0>(entity), mesh.vertexCount,
            mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord0), mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0));
        var indices = manager.GetBuffer<MeshIndex>(entity);
        var materials = manager.GetBuffer<MeshMaterial>(entity);
        for (var sub = 0; sub < mesh.subMeshCount; sub++)
        {
            var descriptor = mesh.GetSubMesh(sub);
            var values = mesh.GetIndices(sub);
            materials.Add(new MeshMaterial(indices.Length, values.Length, descriptor.firstVertex,
                descriptor.vertexCount, firstMaterial + sub));
            foreach (var index in values) indices.Add(new MeshIndex { m_Index = index });
        }
    }

    private static NativeArray<byte> Attribute(Mesh mesh, Mesh.MeshData data, VertexAttribute attribute)
    {
        var format = mesh.GetVertexAttributeFormat(attribute);
        var bytes = format == VertexAttributeFormat.Float32 || format == VertexAttributeFormat.UInt32 ||
            format == VertexAttributeFormat.SInt32 ? 4 :
            format == VertexAttributeFormat.Float16 || format == VertexAttributeFormat.SNorm16 ||
            format == VertexAttributeFormat.UNorm16 || format == VertexAttributeFormat.UInt16 ||
            format == VertexAttributeFormat.SInt16 ? 2 : 1;
        bytes *= mesh.GetVertexAttributeDimension(attribute);
        var stream = mesh.GetVertexAttributeStream(attribute);
        var stride = mesh.GetVertexBufferStride(stream);
        var offset = mesh.GetVertexAttributeOffset(attribute);
        var source = data.GetVertexData<byte>(stream);
        var output = new NativeArray<byte>(mesh.vertexCount * bytes, Allocator.Temp);
        for (var vertex = 0; vertex < mesh.vertexCount; vertex++)
            NativeArray<byte>.Copy(source, vertex * stride + offset, output, vertex * bytes, bytes);
        return output;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var mesh in _meshes) UnityEngine.Object.DestroyImmediate(mesh);
        _meshes.Clear();
        _world.Dispose();
    }

    [DisableAutoCreation]
    internal sealed partial class MeshSystem : SystemBase
    {
        private static readonly Type? JobType = typeof(BatchMeshHelpers).GetNestedType("GenerateBatchMeshJob", BindingFlags.NonPublic);
        private static readonly MethodInfo? GenerateMesh = JobType?.GetMethod("GenerateCompositionMesh", BindingFlags.Instance | BindingFlags.NonPublic);
        protected override void OnUpdate() { }
        internal bool Generate(NetCompositionMeshData data, Entity target, Mesh.MeshData output)
        {
            if (JobType == null || GenerateMesh == null)
            {
                return false;
            }
            var job = Activator.CreateInstance(JobType);
            JobType.GetField("m_MeshVertices")!.SetValue(job, GetBufferLookup<MeshVertex>(true));
            JobType.GetField("m_MeshNormals")!.SetValue(job, GetBufferLookup<MeshNormal>(true));
            JobType.GetField("m_MeshTangents")!.SetValue(job, GetBufferLookup<MeshTangent>(true));
            JobType.GetField("m_MeshUV0s")!.SetValue(job, GetBufferLookup<MeshUV0>(true));
            JobType.GetField("m_MeshIndices")!.SetValue(job, GetBufferLookup<MeshIndex>(true));
            JobType.GetField("m_MeshMaterials")!.SetValue(job, GetBufferLookup<MeshMaterial>());
            GenerateMesh.Invoke(job, new object[] { data, EntityManager.GetBuffer<NetCompositionPiece>(target),
                EntityManager.GetBuffer<MeshMaterial>(target), output });
            return true;
        }
    }
}
