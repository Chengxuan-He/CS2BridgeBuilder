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
        if (main.Count != Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[0] || auxiliary.Count != Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[1])
            throw new ArgumentException("Lift03 requires both decks at all three LODs.");
        var decalGroups = new Dictionary<(IReadOnlyList<MovablePaintDeck>, IReadOnlyList<MovablePaintDeck>), MovablePaintDeck[]>();
        var result = new Level[Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[2]];
        for (var lod = Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[3]; lod < result.Length; lod++)
        {
            var upper = main[lod];
            var lower = auxiliary[lod];
            var decalKey = (upper.Decals, lower.Decals);
            if (!decalGroups.TryGetValue(decalKey, out var decals))
                decalGroups.Add(decalKey, decals = upper.Decals.Concat(lower.Decals).ToArray());
            Validate(upper);
            Validate(lower);
            if (!ReferenceEquals(upper.Source, lower.Source) || upper.ClosedWorld != lower.ClosedWorld)
                throw new InvalidOperationException("Lift03 decks must share their mechanism source and closed transform.");
            var triangles = new List<Triangle>();
            Append(triangles, upper.Triangles, upper.RoadToObject, Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[4], Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[5]);
            int offset = upper.Triangles.Count == 0 ? 0 : checked(upper.Triangles.Max(t => t.Occurrence) + 1);
            Append(triangles, lower.Triangles, lower.RoadToObject, upper.Surfaces.Length, offset);
            var motion = new Dictionary<(int,int),MovableRoadGeometry.Partition>();
            Bind(upper, 0);
            Bind(lower, offset);
            void Bind(Level deck, int first)
            {
                foreach (var face in deck.Triangles)
                {
                    var key = (face.Occurrence, face.SourceTriangle);
                    motion[(first+face.Occurrence,face.SourceTriangle)] = deck.RoadTrianglePartitions != null
                        ? deck.RoadTrianglePartitions[key] : deck.RoadPartitions![face.Occurrence];
                }
            }
            // Lane occurrence indices refer to visual batches, not road pieces.
            var lanes = new List<Triangle>();
            Append(lanes, upper.Lanes!.Triangles, upper.RoadToObject, Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[6], Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[7]);
            Append(lanes, lower.Lanes!.Triangles, lower.RoadToObject, upper.Lanes.Surfaces.Length, upper.LanePartitions!.Length);
            var laneResult = new MovableLaneGeometry.Result(lanes, upper.Lanes.Surfaces.Concat(lower.Lanes.Surfaces).ToList(), new List<MovableLaneVisuals.Batch>());
            var bounds = upper.RenderBounds;
            bounds.min = math.min(bounds.min, lower.RenderBounds.min);
            bounds.max = math.max(bounds.max, lower.RenderBounds.max);
            result[lod] = new Level(upper.Source, triangles, upper.Surfaces.Concat(lower.Surfaces).ToArray(), upper.Partition!, bounds, new Pose(Vector3.zero, Quaternion.identity), skeleton: upper.Skeleton, lanes: laneResult, closedWorld: upper.ClosedWorld, appearancePageSize: upper.AppearancePageSize, texelsPerMeter: upper.TexelsPerMeter, roadTrianglePartitions: motion, lanePartitions: upper.LanePartitions.Concat(lower.LanePartitions!).ToArray(), additionalStructures: upper.AdditionalStructures.Concat(lower.AdditionalStructures).ToArray(), decals: decals);
        }

        return result;
    }

    private static void Append(List<Triangle> target, IReadOnlyList<Triangle> source, Pose pose, int materialOffset, int occurrenceOffset)
    {
        foreach (var triangle in MovableRoadGeometry.InFrame(source, pose))
            target.Add(new Triangle(triangle.A, triangle.B, triangle.C, triangle.Material + materialOffset, triangle.Occurrence + occurrenceOffset, triangle.SourceTriangle));
    }

    private static void Validate(Level level)
    {
        // Accept the output of PrepareDeck before structural plans or paint are attached.
        // Dropping an attached plan or a triangle-specific binding would corrupt the asset.
        if (!level.HasRoad || level.Partition == null || (level.RoadPartitions == null && level.RoadTrianglePartitions == null) || level.LanePartitions == null || level.Lanes == null || level.Lanes.Decals.Length != Numeric_Types_Lift03_Lift03RoadLevels_cs.Values.I32[8] || level.Structure != null || level.ExtraStructure != null )
            throw new InvalidOperationException("Lift03 deck composition requires prepared road and lane partitions without structure or projectors.");
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03RoadLevels_cs
internal static class Numeric_Types_Lift03_Lift03RoadLevels_cs
{
    static Numeric_Types_Lift03_Lift03RoadLevels_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift03_Lift03RoadLevels_cs.Values", () => HardcodedNumbers.Load("Types_Lift03_Lift03RoadLevels_cs"));
}
