using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Main geometry mesh 0 only. Structural widening is supplied by its separate measured plan.
internal static class Draw01RoadReplacement
{
    internal static MovableStructureModel.Plan CreateMainMeshPlan(int lod, Vector3[] sourcePositions,
        Vector3[] widenedPositions, int[][] sourceIndices)
    {
        var vertexCount = lod switch
        {
            0 => 29840,
            1 => 18772,
            2 => 1118,
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
        var triangleCount = Draw01MovingRoadData.SourceTriangleCount(lod);
        if (sourcePositions.Length != vertexCount || sourceIndices.Length != 1 ||
            sourceIndices[0].Length != triangleCount * 3)
            throw new ArgumentException("Draw01 road replacement requires the measured main mesh 0 topology.");

        var removed = new bool[triangleCount];
        foreach (var face in Draw01MovingRoadData.RemovalFaces(lod)) removed[face] = true;
        foreach (var face in Draw01FixedRoadData.RemovalFaces(lod)) removed[face] = true;

        var edges = new List<MovableStructureModel.EdgeVertex>();
        var moving = vertexCount + edges.Count;
        edges.AddRange(Draw01RoadFragmentData.MovingVertices(lod));
        var rear = vertexCount + edges.Count;
        edges.AddRange(Draw01FixedRearFragmentData.Vertices(lod));
        var rearSlope = vertexCount + edges.Count;
        edges.AddRange(Draw01FixedRearSlopeData.Edges(lod));
        var front = vertexCount + edges.Count;
        edges.AddRange(Draw01FixedFrontFragmentData.Vertices(lod));
        var inner = vertexCount + edges.Count;
        edges.AddRange(Draw01FixedInnerFragmentData.Vertices(lod));
        var sidewalk = vertexCount + edges.Count;
        edges.AddRange(Draw01SidewalkFrontFragmentData.Edges(lod));

        // Interior vertices must follow ALL edge tables, including tables added later.
        var interior = new List<MovableStructureModel.TriangleVertex>();
        var rearSlopeInterior = vertexCount + edges.Count;
        interior.AddRange(Draw01FixedRearSlopeData.Interior(lod));
        var sidewalkInterior = vertexCount + edges.Count + interior.Count;
        interior.AddRange(Draw01SidewalkFrontFragmentData.Interior(lod));

        var fragments = new List<MovableStructureModel.Fragment>();
        fragments.AddRange(Draw01RoadFragmentData.MovingFaces(lod, moving));
        fragments.AddRange(Draw01FixedRearFragmentData.Faces(lod, rear));
        fragments.AddRange(Draw01FixedRearSlopeData.Faces(lod, rearSlope, rearSlopeInterior));
        fragments.AddRange(Draw01FixedFrontFragmentData.Faces(lod, front));
        fragments.AddRange(Draw01FixedInnerFragmentData.Faces(lod, inner));
        fragments.AddRange(Draw01SidewalkFrontFragmentData.Faces(lod, sidewalk, sidewalkInterior));
        return new MovableStructureModel.Plan(sourcePositions, widenedPositions, sourceIndices,
            new[] { removed }, edges.ToArray(), fragments.ToArray(), interior.ToArray());
    }
}
