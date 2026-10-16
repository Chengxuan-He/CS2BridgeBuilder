using System;
using System.Collections.Generic;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native DrawBridge01 main mesh 0, game 1.6.2f1, decoded binary32 boundaries.
// Geometry SHA-256: f9cc32a1e42568aa26b3248ad2cdb6fee7de23d3dfeaa0b56e7714c51e0d1abe
// These two logical surface roles are supplied by the selected-section mapping.
// Do not choose them by a runtime X/Y threshold, prefab name, or material similarity.
// Every generated road LOD uses the same highest-detail logical seams; the native
// far mesh's flattened proxy is not a different road or a different bone assignment.
internal static class Draw01RoadPartitions
{
    internal static IReadOnlyDictionary<(int Occurrence, int SourceTriangle), Partition> ForRoad(
        MovableClosedRoad.Result road)
    {
        var sources = road.SourceFaces ?? throw new InvalidOperationException("Draw01 road sampling did not retain native source-face correspondence.");
        var result = new Dictionary<(int, int), Partition>();
        foreach (var triangle in road.Triangles)
        {
            var key = (triangle.Occurrence, triangle.SourceTriangle);
            if (result.ContainsKey(key)) continue;
            var source = sources[triangle.Occurrence][triangle.SourceTriangle];
            // Each occurrence was compiled individually; the enclosing occurrence
            // retains the selected-road identity across combined node contributions.
            if (source.Piece != 0)
                throw new InvalidOperationException("Draw01 source-face correspondence has an invalid local piece index.");
            var piece = road.Occurrences[triangle.Occurrence].Prefab;
            if (!MovableNativeRoadRoles.TryGet(piece, source, out var role))
                throw new InvalidOperationException($"No measured road-surface role for '{piece.name}', geometry '{piece.geometryAsset?.id.guid}', mesh {source.Mesh}, submesh {source.Submesh}, face {source.Triangle}.");
            result.Add(key, role == MovableNativeRoadRoles.Role.Carriageway ? Carriageway : Sidewalk);
        }
        return result;
    }

    internal static readonly Partition Carriageway = Create(
        -83.740234375f, -83.58984375f, -0.05267906188964844f,
        0.05267906188964844f, 83.58984375f, 83.740234375f);

    internal static readonly Partition Sidewalk = Create(
        -78.150390625f, -78.05078125f, -0.03000020980834961f,
        0.03000020980834961f, 78.05078125f, 78.150390625f);

    // In increasing Z: fixed, end gap, negative leaf, central gap, positive leaf,
    // end gap, fixed. Bone ARRAY indices 1/2 correspond to native connections 100/101.
    // Sampling supplies the finite node extent; these trees do not fabricate approaches.
    private static Partition Create(float negativeFixedEnd, float negativeLeafStart,
        float negativeLeafEnd, float positiveLeafStart, float positiveLeafEnd,
        float positiveFixedStart)
    {
        var fixedRoad = new Partition(0);
        return Cut(negativeFixedEnd, fixedRoad,
            Cut(negativeLeafStart, Partition.Gap,
                Cut(negativeLeafEnd, new Partition(1),
                    Cut(positiveLeafStart, Partition.Gap,
                        Cut(positiveLeafEnd, new Partition(2),
                            Cut(positiveFixedStart, Partition.Gap, fixedRoad))))));
    }

    private static Partition Cut(float seam, Partition negative, Partition positive)
    {
        var objectToSeam = Matrix4x4.identity;
        objectToSeam.m23 = -seam;
        return new Partition(objectToSeam, negative, positive);
    }
}
