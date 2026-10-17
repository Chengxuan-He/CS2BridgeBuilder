using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Extracts actual primary/secondary lane visuals in the closed road's centred frame. Curved
/// meshes and decal volumes remain explicitly different inputs to visual conversion: exporting
/// a decal's box as opaque triangles would not export the marking projected by that box.
/// </summary>
internal static class MovableLaneVisuals
{
    internal sealed class Batch
    {
        internal readonly int LaneIndex, PartIndex, TileIndex;
        // Preserve native lane semantics separately from the shared render asset.
        // Several lane types may reuse a render; LaneIndex is not a road-piece index.
        internal readonly NetLaneGeometryPrefab SourceLane;
        internal readonly RenderPrefab Source;
        internal readonly SurfaceAsset[] Surfaces;
        internal readonly Curve Curve;
        internal readonly bool Decal;
        internal readonly Matrix4x4 Transform, CurveMatrix;
        internal readonly Vector4 Size, Scale, Parameters;
        internal readonly float SmoothingDistance;
        internal readonly bool GeometryTiling, HangingSwaying;
        internal readonly Vector4 Dilation;
        internal readonly bool Dilated;

        internal Batch(int laneIndex, int partIndex, int tileIndex, NetLaneGeometryPrefab sourceLane, RenderPrefab source,
            SurfaceAsset[] surfaces, Curve curve, MeshData data, float4 size, float4 scale,
            float4 parameters)
        {
            LaneIndex = laneIndex; PartIndex = partIndex; TileIndex = tileIndex;
            SourceLane = sourceLane;
            Source = source; Surfaces = surfaces; Curve = curve;
            Decal = (data.m_State & MeshFlags.Decal) != 0;
            Size = size; Scale = scale; Parameters = parameters;
            SmoothingDistance = data.m_SmoothingDistance;
            var properties = source.GetComponent<CurveProperties>();
            GeometryTiling = properties?.m_GeometryTiling == true;
            HangingSwaying = properties?.m_HangingSwaying == true;
            Dilation = data.m_TilingCount != 0 && properties?.m_StraightTiling == true
                ? new Vector4(1f / data.m_TilingCount, .01f, 0f, 0f) : new Vector4(.1f, 10f, 0f, 0f);
            var dilation = source.GetComponent<DilationProperties>();
            Dilated = dilation != null;
            if (dilation != null)
                Dilation = new Vector4(dilation.m_InfoviewOnly ? math.max(size.x, size.y) : dilation.m_MinSize,
                    1f / math.max(1e-5f, math.max(size.x, size.y)), dilation.m_InfoviewFactor * 2.5f,
                    dilation.m_InfoviewOnly ? 0f : 2.5f);
            var transform = BatchDataHelpers.BuildTransformMatrix(curve, size, scale,
                SmoothingDistance, Decal, true);
            Transform = transform;
            CurveMatrix = BatchDataHelpers.BuildCurveMatrix(curve,
                new float3x4(transform.c0.xyz, transform.c1.xyz, transform.c2.xyz, transform.c3.xyz),
                size, data.m_TilingCount);
        }
    }

    internal static bool TryExtract(BridgePreviewComposition.Piece[] pieces, NetCompositionData data,
        float span, out Batch[] batches, out string? error)
    {
        batches = Array.Empty<Batch>(); error = null;
        if (!math.isfinite(span) || span <= 0f)
        { error = "Lane visual extraction requires a finite positive span."; return false; }
        try
        {
            using var composition = new BridgePreviewLanes();
            if (!composition.Build(pieces, data, span, out var lanes, captureRenderState: true))
            { error = "Native primary/secondary lane generation failed for the selected road."; return false; }
            return TryExtract(lanes, new float3(0f, 0f, span * .5f), null, out batches, out error);
        }
        catch (Exception exception)
        { error = "Lane visual extraction failed: " + exception.Message; return false; }
    }

    internal static bool TryExtract(World context, IReadOnlyList<Entity> owners, MovableNetworkPrefabs imported,
        bool leftHandTraffic, float3 origin, out Batch[] batches, out string? error)
    {
        batches = Array.Empty<Batch>(); error = null;
        if (context == World.DefaultGameObjectInjectionWorld)
            throw new InvalidOperationException("Moving lane extraction requires a private network context.");
        try
        {
            var manager = context.EntityManager;
            manager.CompleteAllTrackedJobs();
            var lanes = new List<BridgePreviewLanes.Lane>();
            var visited = new HashSet<Entity>();
            foreach (var owner in owners)
                foreach (var entry in manager.GetBuffer<Game.Net.SubLane>(owner, true))
                {
                    var entity = entry.m_SubLane;
                    if (!visited.Add(entity) || manager.HasComponent<Game.Common.Deleted>(entity) ||
                        manager.HasComponent<Game.Net.MasterLane>(entity) || manager.HasComponent<Game.Tools.Hidden>(entity)) continue;
                    var original = imported.Original(manager.GetComponentData<PrefabRef>(entity).m_Prefab);
                    imported.CheckSource(original);
                    if (original is not NetLanePrefab lane)
                        throw new InvalidOperationException("A generated lane references a non-lane prefab.");
                    if (lane is not NetLaneGeometryPrefab geometry)
                    {
                        var native = World.DefaultGameObjectInjectionWorld;
                        var system = native.GetExistingSystemManaged<PrefabSystem>();
                        if (!system.TryGetEntity(lane, out var source))
                            throw new InvalidOperationException("A generated lane has no original prefab entity.");
                        if (native.EntityManager.HasBuffer<SubMesh>(source) &&
                            native.EntityManager.GetBuffer<SubMesh>(source, true).Length != 0)
                            throw new InvalidOperationException("A non-geometry lane has render data requiring conversion: " + lane.name);
                        continue;
                    }
                    lanes.Add(new BridgePreviewLanes.Lane(geometry, manager.GetComponentData<Curve>(entity),
                        manager.HasComponent<EdgeLane>(entity) ? manager.GetComponentData<EdgeLane>(entity) : null,
                        BridgePreviewLanes.RejectedSubMeshes(manager, entity, leftHandTraffic)));
                }
            return TryExtract(lanes.ToArray(), origin, imported, out batches, out error);
        }
        catch (Exception exception)
        { error = "Network lane visual extraction failed: " + exception.Message; return false; }
    }

    private static bool TryExtract(BridgePreviewLanes.Lane[] lanes, float3 offset,
        MovableNetworkPrefabs? imported, out Batch[] batches, out string? error)
    {
        batches = Array.Empty<Batch>(); error = null;
        try
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var prefabs = world?.GetExistingSystemManaged<PrefabSystem>();
            if (world == null || !world.IsCreated || prefabs == null)
            { error = "The native prefab world is unavailable for lane visual extraction."; return false; }
            var output = new List<Batch>();
            for (var laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
            {
                var lane = lanes[laneIndex];
                var parts = lane.Prefab.m_Meshes ?? Array.Empty<NetLaneMeshInfo>();
                for (var partIndex = 0; partIndex < parts.Length; partIndex++)
                {
                    var part = parts[partIndex];
                    if (part?.m_Mesh == null)
                    { error = $"Lane {laneIndex} part {partIndex} has no render Prefab."; return false; }
                    if ((Requirements(part) & lane.RejectedSubMeshes) != 0) continue;
                    var source = part.m_Mesh;
                    imported?.CheckSource(source);
                    // Native MeshSystem puts these meshes on Pipeline/SubPipeline,
                    // not the visible road layer. Keep utility lanes in the native
                    // network; do not bake their underground-view representation
                    // into a permanently visible, bone-bound road object.
                    var underground = source.GetComponent<UndergroundMesh>();
                    if (underground != null && (underground.m_IsPipeline || underground.m_IsSubPipeline))
                        continue;
                    if (!prefabs.TryGetEntity(source, out var entity) || !world.EntityManager.HasComponent<MeshData>(entity))
                    { error = $"Lane visual '{source.name}' has no native mesh data."; return false; }
                    var meshData = world.EntityManager.GetComponentData<MeshData>(entity);
                    var size = new float4(MathUtils.Size(meshData.m_Bounds), MathUtils.Center(meshData.m_Bounds).y);
                    var curve = lane.Curve;
                    // The caller supplies the common closed-pose origin for node and edge lanes.
                    curve.m_Bezier.a -= offset; curve.m_Bezier.b -= offset;
                    curve.m_Bezier.c -= offset; curve.m_Bezier.d -= offset;
                    if ((meshData.m_State & MeshFlags.Invert) != 0)
                        curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
                    if (!math.isfinite(curve.m_Length) || curve.m_Length <= 0f ||
                        !math.all(math.isfinite(size)) || size.z <= 0f)
                    { error = $"Lane visual '{source.name}' has invalid native curve/mesh dimensions."; return false; }
                    var surfaces = source.surfaceAssets?.ToArray() ?? Array.Empty<SurfaceAsset>();
                    if (surfaces.Length == 0 || surfaces.Any(surface => surface == null))
                    { error = $"Lane visual '{source.name}' has unresolved surface references."; return false; }
                    var scale = lane.Edge.HasValue ? BatchDataHelpers.BuildCurveScale(lane.Edge.Value)
                        : BatchDataHelpers.BuildCurveScale();
                    var parameters = lane.Edge.HasValue ? BatchDataHelpers.BuildCurveParams(size, lane.Edge.Value)
                        : BatchDataHelpers.BuildCurveParams(size);
                    var count = BatchDataHelpers.GetTileCount(curve, size.z, meshData.m_TilingCount,
                        (meshData.m_State & MeshFlags.Tiling) != 0, out var clipCount);
                    for (var tile = 0; tile < count; tile++)
                    {
                        var segment = curve;
                        if (count > 1)
                        {
                            var range = clipCount == 0 ? new float2(tile, tile + 1) / count :
                                (float2)(new int2(tile, tile + 1) * clipCount / count) / clipCount;
                            segment.m_Bezier = MathUtils.Cut(curve.m_Bezier, range);
                            segment.m_Length = curve.m_Length * (range.y - range.x);
                            if (segment.m_Length <= 0f) continue;
                        }
                        output.Add(new Batch(laneIndex, partIndex, tile, lane.Prefab, source, surfaces, segment,
                            meshData, size, scale, parameters));
                    }
                }
            }
            batches = output.ToArray(); return true;
        }
        catch (Exception exception)
        { error = "Lane visual extraction failed: " + exception.Message; return false; }
    }

    private static SubMeshFlags Requirements(NetLaneMeshInfo part)
    {
        SubMeshFlags flags = 0;
        if (part.m_RequireSafe) flags |= SubMeshFlags.RequireSafe;
        if (part.m_RequireLevelCrossing) flags |= SubMeshFlags.RequireLevelCrossing;
        if (part.m_RequireEditor) flags |= SubMeshFlags.RequireEditor;
        if (part.m_RequireTrackCrossing) flags |= SubMeshFlags.RequireTrack;
        if (part.m_RequireClear) flags |= SubMeshFlags.RequireClear;
        if (part.m_RequireLeftHandTraffic) flags |= SubMeshFlags.RequireLeftHandTraffic;
        if (part.m_RequireRightHandTraffic) flags |= SubMeshFlags.RequireRightHandTraffic;
        return flags;
    }
}
