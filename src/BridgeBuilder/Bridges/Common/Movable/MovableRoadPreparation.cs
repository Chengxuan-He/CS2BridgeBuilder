using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Converts evaluated native road LODs using style-owned motion and appearance data.
// Structure plans and skinned paint are attached by the mechanism assembly afterwards.
internal static class MovableRoadPreparation
{
    internal static MovableRoadAssets.Level[] Prepare(MovableStraightSample sample,
        IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, Partition carriageway, Partition sidewalk,
        Partition carriagewayAppearance, Partition sidewalkAppearance,
        MovableExistingRoadSurface.Patch[] patches, int boneCount, MovableSelectedRoadLayout layout)
        => Prepare(sample.Road, sample.RoadToObject, sample.CompileLod, sources, halfDelta,
            closedWorld, material, carriageway, sidewalk, carriagewayAppearance,
            sidewalkAppearance, patches, boneCount, layout);

    // A coordinated dual-network sample supplies a separate road frame and LOD
    // compiler for each deck. Never reuse the main deck's frame for the auxiliary.
    internal static MovableRoadAssets.Level[] Prepare(MovableClosedRoad.Result road,
        Pose pose, Func<int, MovableClosedRoad.Result> compileLod,
        IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, Partition carriageway, Partition sidewalk,
        Partition carriagewayAppearance, Partition sidewalkAppearance,
        MovableExistingRoadSurface.Patch[] patches, int boneCount, MovableSelectedRoadLayout layout)
    {
        var physical = new Partition[road.Occurrences.Length];
        var appearance = new Partition[physical.Length];
        for (var i = 0; i < physical.Length; i++)
        {
            var walk = layout.SidewalkOccurrences[i];
            physical[i] = walk ? sidewalk : carriageway;
            appearance[i] = walk ? sidewalkAppearance : carriagewayAppearance;
        }
        var lanePartitions = road.LaneVisuals.Select(lane =>
            lane.SourceLane.Has<PedestrianLane>() ? sidewalk : carriageway).ToArray();
        var rotation = Quaternion.Inverse(pose.rotation);
        var inverse = new Pose(rotation * -pose.position, rotation);
        var inputs = new MovableSurfaceAtlas.Input[sources.Count];
        var lanes = new MovableLaneGeometry.Result[sources.Count];
        var bounds = new Bounds3[sources.Count];
        for (var lod = 0; lod < sources.Count; lod++)
        {
            var source = compileLod(lod);
            lanes[lod] = new MovableLaneGeometry.Result(source.LaneGeometry.Triangles.ToList(),
                source.LaneGeometry.Surfaces.ToList(), new List<MovableLaneVisuals.Batch>());
            var geometry = MovableRoadGeometry.InFrame(source.Triangles, pose);
            if (!MovableRoadGeometry.TryBind(geometry, carriageway, boneCount, source.Surfaces.Length,
                out var receivers, out var error, occurrencePartitions: physical))
                throw new InvalidOperationException(error);
            var converted = new bool[source.Surfaces.Length];
            for (var i = 0; i < converted.Length; i++)
            {
                source.Surfaces[i].LoadHeader();
                converted[i] = !MovableSurfaceAssets.IsObjectSurface(source.Surfaces[i]);
            }
            var prepared = MovableRoadTemplate.Prepare(receivers, converted);
            var selected = MovableRoadGeometry.InFrame(prepared, inverse);
            if (!MovableRoadGeometry.TryBind(selected, carriagewayAppearance, patches.Length,
                source.Surfaces.Length, out var assigned, out error, occurrencePartitions: appearance))
                throw new InvalidOperationException(error);
            inputs[lod] = new MovableSurfaceAtlas.Input
            {
                Faces = assigned, Materials = source.Surfaces, Patches = patches,
                Native = sources[0].GetSurfaceAsset(0)
            };
            bounds[lod] = sources[lod].bounds;
            bounds[lod].min.x -= halfDelta;
            bounds[lod].max.x += halfDelta;
            Include(geometry, ref bounds[lod]);
            Include(MovableRoadGeometry.InFrame(source.LaneGeometry.Triangles, pose), ref bounds[lod]);
        }
        var visible = MovableSurfaceAtlas.Compose(inputs, material,
            Array.Empty<MovableNativeDecals.Region>(), out var surfaces);
        var result = new MovableRoadAssets.Level[sources.Count];
        for (var lod = 0; lod < result.Length; lod++)
            result[lod] = new MovableRoadAssets.Level(sources[lod], visible[lod], surfaces[lod],
                carriageway, bounds[lod], pose, lanes: lanes[lod], closedWorld: closedWorld,
                roadPartitions: physical, lanePartitions: lanePartitions);
        return result;
    }

    private static void Include(IReadOnlyList<MovableRoadGeometry.Triangle> triangles, ref Bounds3 bounds)
    {
        foreach (var triangle in triangles)
        {
            bounds.min = math.min(bounds.min, math.min((float3)triangle.A.Position,
                math.min((float3)triangle.B.Position, (float3)triangle.C.Position)));
            bounds.max = math.max(bounds.max, math.max((float3)triangle.A.Position,
                math.max((float3)triangle.B.Position, (float3)triangle.C.Position)));
        }
    }
}
