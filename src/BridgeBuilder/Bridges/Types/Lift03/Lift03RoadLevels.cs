using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Level = BridgeBuilder.Bridges.MovableRoadAssets.Level;
using Triangle = BridgeBuilder.Bridges.MovableRoadGeometry.Triangle;

namespace BridgeBuilder.Bridges;

// Appearance is already evaluated independently in each selected deck's frame.
// Only geometry changes frame here; UV density and native surface references stay intact.
internal static class Lift03RoadLevels
{
    internal static Level[] Combine(IReadOnlyList<Level> main, IReadOnlyList<Level> auxiliary)
    {
        if (main.Count != 3 || auxiliary.Count != 3)
            throw new ArgumentException("Lift03 requires both decks at all three LODs.");
        var result = new Level[3];
        for (var lod = 0; lod < result.Length; lod++)
        {
            var upper = main[lod];
            var lower = auxiliary[lod];
            Validate(upper);
            Validate(lower);
            if (!ReferenceEquals(upper.Source, lower.Source) ||
                upper.ClosedWorld != lower.ClosedWorld)
                throw new InvalidOperationException("Lift03 decks must share their mechanism source and closed transform.");

            var triangles = new List<Triangle>();
            Append(triangles, upper.Triangles, upper.RoadToObject, 0, 0);
            Append(triangles, lower.Triangles, lower.RoadToObject,
                upper.Surfaces.Length, upper.RoadPartitions!.Length);

            // Lane occurrence indices refer to visual batches, not road pieces.
            var lanes = new List<Triangle>();
            Append(lanes, upper.Lanes!.Triangles, upper.RoadToObject, 0, 0);
            Append(lanes, lower.Lanes!.Triangles, lower.RoadToObject,
                upper.Lanes.Surfaces.Length, upper.LanePartitions!.Length);
            var laneResult = new MovableLaneGeometry.Result(lanes,
                upper.Lanes.Surfaces.Concat(lower.Lanes.Surfaces).ToList(),
                new List<MovableLaneVisuals.Batch>());
            var bounds = upper.RenderBounds;
            bounds.min = math.min(bounds.min, lower.RenderBounds.min);
            bounds.max = math.max(bounds.max, lower.RenderBounds.max);
            result[lod] = new Level(upper.Source, triangles,
                upper.Surfaces.Concat(lower.Surfaces).ToArray(), upper.Partition!, bounds,
                new Pose(Vector3.zero, Quaternion.identity), skeleton: upper.Skeleton,
                lanes: laneResult, closedWorld: upper.ClosedWorld,
                appearancePageSize: upper.AppearancePageSize, texelsPerMeter: upper.TexelsPerMeter,
                roadPartitions: upper.RoadPartitions.Concat(lower.RoadPartitions!).ToArray(),
                lanePartitions: upper.LanePartitions.Concat(lower.LanePartitions!).ToArray());
        }
        return result;
    }

    private static void Append(List<Triangle> target, IReadOnlyList<Triangle> source,
        Pose pose, int materialOffset, int occurrenceOffset)
    {
        foreach (var triangle in MovableRoadGeometry.InFrame(source, pose))
            target.Add(new Triangle(triangle.A, triangle.B, triangle.C,
                triangle.Material + materialOffset, triangle.Occurrence + occurrenceOffset,
                triangle.SourceTriangle));
    }

    private static void Validate(Level level)
    {
        // Accept the output of PrepareDeck before structural plans or paint are attached.
        // Dropping an attached plan or a triangle-specific binding would corrupt the asset.
        if (!level.HasRoad || level.Partition == null || level.RoadPartitions == null ||
            level.LanePartitions == null || level.Lanes == null || level.Lanes.Decals.Length != 0 ||
            level.Structure != null || level.ExtraStructure != null || level.RoadTrianglePartitions != null)
            throw new InvalidOperationException("Lift03 deck composition requires prepared road and lane partitions without structure or projectors.");
    }
}
