using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkPrefabs
{
    private readonly Dictionary<RenderPrefab, Entity> _renders = new();

    // Metadata for generated render graphs, including previews with no GeometryAsset.
    // No materials, GPU batches or city prefab registrations are created here.
    internal Entity StageRender(RenderPrefab prefab)
    {
        CheckGraph(prefab);
        return StageRenderCore(prefab);
    }

    private Entity StageRenderCore(RenderPrefab prefab)
    {
        CheckSource(prefab);
        if (_renders.TryGetValue(prefab, out var existing)) return existing;
        var components = new List<ComponentBase>();
        prefab.GetComponents(components);
        var types = new HashSet<ComponentType>();
        foreach (var component in components) component.GetPrefabComponents(types);
        var hasLods = types.Contains(ComponentType.ReadWrite<LodMesh>());
        // ObjectInitializeSystem resolves these through its own world's PrefabSystem.
        var registry = _target.World.GetOrCreateSystemManaged<PrefabSystem>();
        if (!registry.AddPrefab(prefab))
            throw new InvalidOperationException($"Could not stage render prefab '{prefab.name}' in the private registry.");
        var entity = registry.GetEntity(prefab);
        _renders.Add(prefab, entity);
        _originals.Add(entity, prefab);
        _target.SetComponentData(entity, RenderMetadata(prefab, hasLods));
        if (hasLods)
        {
            var lods = prefab.GetComponent<LodProperties>().m_LodMeshes;
            var records = new List<LodMesh>();
            if (lods != null)
            {
                // Preserve native index-count order. Check ownership before reading each child.
                var ordered = new List<RenderPrefab>();
                foreach (var lod in lods)
                {
                    CheckSource(lod);
                    var index = ordered.Count;
                    while (index > 0 && lod.indexCount > ordered[index - 1].indexCount) index--;
                    ordered.Insert(index, lod);
                }
                foreach (var lod in ordered) records.Add(new LodMesh { m_LodMesh = StageRenderCore(lod) });
            }
            // Recursive staging can move chunks; acquire the buffer only afterwards.
            var buffer = _target.GetBuffer<LodMesh>(entity);
            foreach (var record in records) buffer.Add(record);
        }
        return entity;
    }

    // Scalar MeshData projection of Game.Prefabs.MeshSystem.OnUpdate, verified against the
    // installed Game.dll AAEE15C4...F86A. Object initialization subsequently supplies object LOD
    // and base flags. Material indices, procedural buffers and renderer batches are separate.
    private static MeshData RenderMetadata(RenderPrefab prefab, bool hasLods)
    {
        var data = new MeshData
        {
            m_Bounds = prefab.bounds, m_SubMeshCount = prefab.meshCount,
            m_IndexCount = prefab.indexCount, m_SmoothingDistance = 0.001f, m_ShadowBias = 0.5f
        };
        if (prefab.isImpostor) data.m_State |= MeshFlags.Impostor;
        if (hasLods)
        {
            var lod = prefab.GetComponent<LodProperties>();
            data.m_LodBias = lod.m_Bias;
            data.m_ShadowBias += lod.m_Bias + lod.m_ShadowBias;
        }
        else
        {
            var size = math.cmax(MathUtils.Size(data.m_Bounds));
            // Native rendering thresholds, not authored-part classification.
            if (size > 0f && size <= 5f)
            {
                data.m_LodBias = 1.2f * math.pow(size / 5f, 3f);
                data.m_ShadowBias = math.max(data.m_ShadowBias, 1.5f * data.m_LodBias);
            }
        }
        if (prefab.surfaceArea > 0f)
        {
            var size = data.m_Bounds.max - data.m_Bounds.min;
            var areaBias = math.log2(math.sqrt(math.clamp(prefab.surfaceArea /
                (math.csum(size * size.yzx) * 2f), 1E-06f, 1f)));
            var shapeBias = math.log2(math.sqrt(math.clamp(math.cmax(math.min(size, size.yzx)) *
                3f / math.csum(size), 1E-06f, 1f)));
            data.m_LodBias -= areaBias;
            data.m_ShadowBias -= 1.5f * areaBias + shapeBias;
        }
        var underground = prefab.GetComponent<UndergroundMesh>();
        if (underground != null)
        {
            if (underground.m_IsTunnel) data.m_DefaultLayers |= MeshLayer.Tunnel;
            if (underground.m_IsPipeline) data.m_DefaultLayers |= MeshLayer.Pipeline;
            if (underground.m_IsSubPipeline) data.m_DefaultLayers |= MeshLayer.SubPipeline;
        }
        var overlay = prefab.GetComponent<OverlayProperties>();
        if (overlay != null && overlay.m_IsWaterway) data.m_DefaultLayers |= MeshLayer.Waterway;
        if (prefab.GetComponent<DecalProperties>() != null) data.m_State |= MeshFlags.Decal;
        var stack = prefab.GetComponent<StackProperties>();
        if (stack != null)
        {
            switch (stack.m_Direction)
            {
                case StackDirection.Right: data.m_State |= MeshFlags.StackX; break;
                case StackDirection.Up: data.m_State |= MeshFlags.StackY; break;
                case StackDirection.Forward: data.m_State |= MeshFlags.StackZ; break;
            }
        }
        if (prefab.GetComponent<ProceduralAnimationProperties>() != null) data.m_State |= MeshFlags.Skeleton;
        var curve = prefab.GetComponent<CurveProperties>();
        if (curve != null)
        {
            data.m_TilingCount = curve.m_TilingCount;
            if (curve.m_OverrideLength != 0f)
            {
                data.m_Bounds.min.z = curve.m_OverrideLength * -0.5f;
                data.m_Bounds.max.z = curve.m_OverrideLength * 0.5f;
            }
            if (curve.m_SmoothingDistance > data.m_SmoothingDistance)
                data.m_SmoothingDistance = curve.m_SmoothingDistance;
            if (curve.m_GeometryTiling) data.m_State |= MeshFlags.Tiling;
            if (curve.m_InvertCurve) data.m_State |= MeshFlags.Invert;
        }
        var baseProperties = prefab.GetComponent<BaseProperties>();
        if (baseProperties != null && baseProperties.m_BaseType != null)
        {
            data.m_State |= MeshFlags.Base;
            if (baseProperties.m_UseMinBounds) data.m_State |= MeshFlags.MinBounds;
        }
        if (prefab.Has<DefaultMesh>())
        {
            var size = RenderingUtils.GetRenderingSize(MathUtils.Size(data.m_Bounds));
            data.m_State |= MeshFlags.Default;
            data.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(size, data.m_LodBias);
            data.m_ShadowLod = (byte)RenderingUtils.CalculateLodLimit(size, data.m_ShadowBias);
        }
        return data;
    }
}
