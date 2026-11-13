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
            2 => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[2]};
        var triangleCount = lod switch
        {
            0 => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[3],
            1 => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[4],
            _ => Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[5]
        };
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
        for (var i = Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[14]; i < removed.Length; i++)
            structure.RemoveTriangles[Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values.I32[15]][i] |= removed[i];
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01RoadReplacement_cs
internal static class Numeric_Types_Lift01_Lift01RoadReplacement_cs
{
    static Numeric_Types_Lift01_Lift01RoadReplacement_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift01_Lift01RoadReplacement_cs.Values", () => HardcodedNumbers.Load("Types_Lift01_Lift01RoadReplacement_cs"));
}
