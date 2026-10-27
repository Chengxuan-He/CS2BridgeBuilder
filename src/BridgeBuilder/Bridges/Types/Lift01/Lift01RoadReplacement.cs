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
            0 => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[0],
            1 => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[1],
            2 => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[2],
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
        var triangleCount = lod switch
        {
            0 => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[3],
            1 => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[4],
            _ => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[5]
        };
        if (structure.SourcePositions.Length != vertexCount || structure.SourceIndices.Length != Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[6] || structure.SourceIndices[Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[7]].Length != triangleCount * Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[8] || structure.RemoveTriangles.Length != Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[9] || structure.RemoveTriangles[Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[10]].Length != triangleCount)
            throw new InvalidOperationException("Lift01 road replacement requires its recorded main mesh LOD.");
        var removed = new bool[triangleCount];
        Lift01RoadTops.Remove(lod, structure.SourceIndices[Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[11]], removed);
        // Native LOD2 has no separate rail, sleeper or catenary geometry. Its
        // road is represented by the eight measured fixed/moving top faces.
        if (lod < Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[12])
        {
            Lift01SleeperRemoval.Remove(lod, removed);
            Lift01RailRemoval.Remove(lod, removed);
            Lift01CatenaryRemoval.Remove(lod, removed);
        }

        // A replacement fragment is appended even when its original face is removed.
        // Reject overlapping ownership before changing the supplied structural plan.
        foreach (var fragment in structure.Fragments)
            if (fragment.Slot == Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[13] && removed[fragment.Triangle])
                throw new InvalidOperationException("Lift01 structural fragments overlap native road replacement.");
        for (var i = Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[14]; i < removed.Length; i++)
            structure.RemoveTriangles[Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[15]][i] |= removed[i];
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01RoadReplacement_cs
internal static class Numeric_Types_Lift01_Lift01RoadReplacement_cs
{
    static Numeric_Types_Lift01_Lift01RoadReplacement_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01RoadReplacement_cs");
}
