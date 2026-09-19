using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Runtime;

/// <summary>Native lane mesh/material/curve batches for the isolated renderer.</summary>
internal sealed class BridgePreviewLaneMesh : IDisposable
{
    private readonly List<Mesh> _owned = new();

    internal bool Build(BridgePreviewLanes.Lane lane, bool leftHandTraffic, Matrix4x4 deckTransform,
        BridgePreviewRenderResources resources, List<BridgePreviewDrawList.Draw> draws)
    {
        // A missing geometry prefab is an invalid draw request, not a logical
        // lane to skip. Logical lanes are classified before constructing Lane.
        if (lane.Prefab == null) return false;
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return false;
        var prefabs = world.GetExistingSystemManaged<PrefabSystem>();
        if (prefabs == null) return false;
        foreach (var part in lane.Prefab.m_Meshes ?? Array.Empty<NetLaneMeshInfo>())
        {
            if (part?.m_Mesh == null)
                return false;
            // This is an ordinary safe straight edge, not an editor marker,
            // level crossing, track switch or intersection.
            if (part.m_RequireEditor || part.m_RequireLevelCrossing ||
                part.m_RequireTrackCrossing || (part.m_RequireLeftHandTraffic && !leftHandTraffic) ||
                (part.m_RequireRightHandTraffic && leftHandTraffic)) continue;
            var render = part.m_Mesh;
            if (!prefabs.TryGetEntity(render, out var entity) || !world.EntityManager.HasComponent<MeshData>(entity))
            {
                return false;
            }
            var data = world.EntityManager.GetComponentData<MeshData>(entity);
            var size = new float4(MathUtils.Size(data.m_Bounds), MathUtils.Center(data.m_Bounds).y);
            var curve = lane.Curve;
            if (curve.m_Length <= 0f) continue;
            if ((data.m_State & MeshFlags.Invert) != 0) curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
            var tiling = (data.m_State & MeshFlags.Tiling) != 0;
            var clipCount = 0;
            var count = tiling ? BatchDataHelpers.GetTileCount(curve, size.z, data.m_TilingCount, true, out clipCount) : 1;
            for (var tile = 0; tile < count; tile++)
            {
                var partCurve = curve;
                if (count > 1)
                {
                    // Native BatchDataSystem partitions the authored repeat
                    // count; for a non-quantized tile use equal length pieces.
                    var range = clipCount == 0 ? new float2(tile, tile + 1) / count :
                        (float2)(new int2(tile, tile + 1) * clipCount / count) / clipCount;
                    partCurve.m_Bezier = MathUtils.Cut(curve.m_Bezier, range);
                    partCurve.m_Length = curve.m_Length * (range.y - range.x);
                    if (partCurve.m_Length <= 0f) continue;
                }
                var scale = lane.Edge.HasValue ? BatchDataHelpers.BuildCurveScale(lane.Edge.Value) :
                    BatchDataHelpers.BuildCurveScale();
                var decal = (data.m_State & MeshFlags.Decal) != 0;
                var native = BatchDataHelpers.BuildTransformMatrix(partCurve, size, scale,
                    data.m_SmoothingDistance, decal, true);
                var affine = new float3x4(native.c0.xyz, native.c1.xyz, native.c2.xyz, native.c3.xyz);
                var properties = new MaterialPropertyBlock();
                properties.SetFloat("colossal_LodFade", 1f);
                properties.SetMatrix("colossal_CurveMatrix", BatchDataHelpers.BuildCurveMatrix(partCurve, affine, size, data.m_TilingCount));
                properties.SetVector("colossal_CurveScale", scale);
                properties.SetVector("colossal_CurveParams", lane.Edge.HasValue ?
                    BatchDataHelpers.BuildCurveParams(size, lane.Edge.Value) : BatchDataHelpers.BuildCurveParams(size));
                properties.SetVector("colossal_CurveDeterioration", Vector4.zero);
                properties.SetVector("colossal_MeshSize", size);
                properties.SetFloat("_SmoothingDistance", data.m_SmoothingDistance);
                properties.SetFloat("colossal_LodDistanceFactor", RenderingUtils.CalculateDistanceFactor(data.m_MinLod));
                for (sbyte channel = 0; channel < 3; channel++)
                    properties.SetColor("colossal_ColorMask" + channel,
                        render.TryGet<ColorProperties>(out var colors) && colors.active ? colors.GetColor(0, channel).linear : Color.white);
                if (render.TryGet<DecalProperties>(out var decalProperties))
                {
                    properties.SetVector("colossal_TextureArea", new float4(decalProperties.m_TextureArea.min, decalProperties.m_TextureArea.max));
                    properties.SetFloat("colossal_DecalLayerMask", math.asfloat((int)decalProperties.m_LayerMask));
                }
                if (render.TryGet<CurveProperties>(out var curveProperties) && curveProperties.m_GeometryTiling)
                    properties.SetVector("colossal_DilationParams", data.m_TilingCount != 0 && curveProperties.m_StraightTiling ?
                        new float4(1f / data.m_TilingCount, .01f, 0f, 0f) : new float4(.1f, 10f, 0f, 0f));

                var materials = resources.Materials(render, lane: true);
                var meshes = resources.Meshes(render);
                if (meshes.Length == 0 || materials.Length == 0)
                    return false;
                var materialIndex = 0;
                foreach (var source in meshes)
                {
                    if (source == null)
                        return false;
                    if (materialIndex + source.subMeshCount > materials.Length)
                        return false;
                    for (var sub = 0; sub < source.subMeshCount; sub++)
                        if (materials[materialIndex + sub] == null)
                            return false;
                    var mesh = UnityEngine.Object.Instantiate(source);
                    mesh.name = "tmp_native_lane_" + render.name;
                    mesh.hideFlags = HideFlags.HideAndDontSave;
                    _owned.Add(mesh);
                    if (!decal)
                    {
                        // Curved mesh positions are expanded by the shader. Give
                        // Unity expanded LOCAL culling bounds, not source bounds.
                        var min = math.min(math.min(partCurve.m_Bezier.a, partCurve.m_Bezier.b),
                            math.min(partCurve.m_Bezier.c, partCurve.m_Bezier.d)) - native.c3.xyz;
                        var max = math.max(math.max(partCurve.m_Bezier.a, partCurve.m_Bezier.b),
                            math.max(partCurve.m_Bezier.c, partCurve.m_Bezier.d)) - native.c3.xyz;
                        mesh.bounds = new Bounds((min + max) * .5f + new float3(0f, size.w, 0f), max - min + size.xyz);
                    }
                    var matrix = deckTransform * (Matrix4x4)native;
                    for (var sub = 0; sub < mesh.subMeshCount; sub++)
                        draws.Add(new BridgePreviewDrawList.Draw(mesh, materials[materialIndex++], sub, matrix, properties));
                }
            }
        }
        return true;
    }

    public void Dispose()
    {
        foreach (var mesh in _owned) if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
        _owned.Clear();
    }
}
