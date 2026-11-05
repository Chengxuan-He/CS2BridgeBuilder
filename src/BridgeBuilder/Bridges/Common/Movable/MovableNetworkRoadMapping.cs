using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Native network input, not evaluated/skinned road geometry. The caller supplies
// measured face membership and an occurrence-specific transverse projection.
// Network batching derives longitudinal density from source UV edge differences;
// its mapping origin is native, so this API cannot encode a longitudinal phase.
internal sealed class MovableNetworkRoadMapping
{
    internal readonly SurfaceAsset Surface;
    internal readonly IReadOnlyDictionary<int, int[]> Faces;
    internal readonly Vector4 U;
    internal readonly float LongitudinalDensity, TangentSign;

    internal MovableNetworkRoadMapping(SurfaceAsset surface,
        IReadOnlyDictionary<int, int[]> faces, Vector4 u, float longitudinalDensity,
        float tangentSign)
    {
        if (surface == null || faces == null || !float.IsFinite(longitudinalDensity) ||
            longitudinalDensity <= 0 || (tangentSign != 1 && tangentSign != -1) ||
            !float.IsFinite(u.x) || !float.IsFinite(u.y) || !float.IsFinite(u.z) || !float.IsFinite(u.w))
            throw new ArgumentException("Invalid native network road projection.");
        Surface = surface; Faces = faces.ToDictionary(p => p.Key, p => (int[])p.Value.Clone());
        U = u; LongitudinalDensity = longitudinalDensity; TangentSign = tangentSign;
    }

    internal void Append(string name, RenderPrefab source, List<ModelImporter.Model> owned,
        List<ModelImporter.Model> batch, List<SurfaceAsset> surfaces)
    {
        Surface.LoadHeader();
        if (Surface.materialTemplateHash != 17 || Surface.hasVTSurfaceAsset)
            throw new InvalidOperationException("Native network road replacement requires its non-VT template 17 descriptor.");
        var piece = source as NetPiecePrefab;
        if (piece?.GetComponent<NetPieceTiling>()?.m_DisableTextureTiling == true)
            throw new InvalidOperationException("Native longitudinal road projection requires texture tiling enabled.");
        var loaded = PrivateGeometryReader.Read(source);
        try
        {
            if (loaded.Length == 0 || Faces.Keys.Any(i => i < 0 || i >= loaded.Length))
                throw new InvalidOperationException("Native network road membership has an invalid mesh.");
            int material = 0;
            for (int meshIndex = 0; meshIndex < loaded.Length; meshIndex++)
            {
                var mesh = loaded[meshIndex];
                if (mesh.subMeshCount != 1)
                    throw new InvalidOperationException("Measured native road input requires one original surface per mesh.");
                var original = source.GetSurfaceAsset(material++);
                AppendMesh(name, mesh, meshIndex, original, owned, batch, surfaces);
            }
        }
        finally { PrivateGeometryReader.Release(loaded); }
    }

    internal void AppendMesh(string name, Mesh mesh, int meshIndex, SurfaceAsset original,
        List<ModelImporter.Model> owned, List<ModelImporter.Model> batch, List<SurfaceAsset> surfaces)
    {
    var points = mesh.vertices;
    var indices = mesh.GetIndices(0);
    var selected = new bool[indices.Length / 3];
    if (Faces.TryGetValue(meshIndex, out var measured))
        foreach (var face in measured)
        {
            if (face < 0 || face >= selected.Length || selected[face])
                throw new InvalidOperationException("Native road membership contains an invalid or duplicate face.");
            selected[face] = true;
        }
    if (selected.Any(v => v))
    {
        var minimum = points.Min(p => p.z);
        var corrections = points.Select((p, i) => new KeyValuePair<int, Vector2>(i,
            new Vector2(Vector4.Dot(new Vector4(p.x, p.y, p.z, 1), U),
                (p.z - minimum) * LongitudinalDensity))).ToArray();
        Add(selected.Select(v => !v).ToArray(), corrections, Surface, true);
    }
    if (selected.Any(v => !v)) Add(selected, null, original, false);

    void Add(bool[] remove, KeyValuePair<int, Vector2>[]? uv, SurfaceAsset surface, bool mapped)
    {
        var plan = new MovableStructureModel.Plan(points, points, new[] { indices },
            new[] { remove }, uvCorrections: uv);
        if (!MovableStructureModel.TryBuild(name + " Network Material " + batch.Count,
            mesh, plan, 0, out var model, out var error))
            throw new InvalidOperationException(error);
        owned.Add(model!);
        if (mapped && TangentSign < 0) ReverseUFrame(model!);
        batch.Add(model!); surfaces.Add(surface);
    }
    }

    // Reversing U reverses T and its handedness, retaining the original V basis.
    // Native batching subsequently applies the original section flip/invert rules.
    private static void ReverseUFrame(ModelImporter.Model model)
    {
        var attribute = model.FirstOrDefault(a => a.attribute == VertexAttribute.Tangent)
            ?? throw new InvalidOperationException("Native road input has no tangent channel.");
        using var tangents = new NativeArray<MeshTangent>(model.vertexCount, Allocator.Temp);
        MeshTangent.Unpack(new NativeSlice<byte>(attribute.data), tangents,
            model.vertexCount, attribute.format, attribute.dimension);
        if (attribute.format != VertexAttributeFormat.Float32 || attribute.dimension != 1)
            throw new InvalidOperationException("Native road input has an unsupported packed tangent layout.");
        var data = attribute.data;
        for (int i = 0; i < model.vertexCount; i++)
        {
            var value = -tangents[i].m_Tangent;
            var bytes = BitConverter.GetBytes(MathUtils.TangentToOctahedral(value));
            for (int j = 0; j < bytes.Length; j++) data[i * 4 + j] = bytes[j];
        }
    }
}
