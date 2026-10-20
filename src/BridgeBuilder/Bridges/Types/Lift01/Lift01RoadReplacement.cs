using System;

namespace BridgeBuilder.Bridges;

internal static class Lift01RoadReplacement
{
    // Apply only after structural construction. Keep its explicit edge positions and
    // fragments intact instead of rebuilding the plan with default interpolation.
    internal static void RemoveNativeRoad(int lod, MovableStructureModel.Plan structure)
    {
        var vertexCount = lod switch
        {
            0 => 472209,
            1 => 351968,
            2 => 1110,
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
        var triangleCount = lod switch { 0 => 380478, 1 => 228254, _ => 776 };
        if (structure.SourcePositions.Length != vertexCount || structure.SourceIndices.Length != 1 ||
            structure.SourceIndices[0].Length != triangleCount * 3 || structure.RemoveTriangles.Length != 1 ||
            structure.RemoveTriangles[0].Length != triangleCount)
            throw new InvalidOperationException("Lift01 road replacement requires its recorded main mesh LOD.");

        var removed = new bool[triangleCount];
        Lift01RoadTops.Remove(lod, structure.SourceIndices[0], removed);
        // Native LOD2 has no separate rail, sleeper or catenary geometry. Its
        // road is represented by the eight measured fixed/moving top faces.
        if (lod < 2)
        {
            Lift01SleeperRemoval.Remove(lod, removed);
            Lift01RailRemoval.Remove(lod, removed);
            Lift01CatenaryRemoval.Remove(lod, removed);
        }
        // A replacement fragment is appended even when its original face is removed.
        // Reject overlapping ownership before changing the supplied structural plan.
        foreach (var fragment in structure.Fragments)
            if (fragment.Slot == 0 && removed[fragment.Triangle])
                throw new InvalidOperationException("Lift01 structural fragments overlap native road replacement.");
        for (var i = 0; i < removed.Length; i++)
            structure.RemoveTriangles[0][i] |= removed[i];
    }
}
