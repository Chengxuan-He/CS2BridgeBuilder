using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01FarStructure
{
    internal static MovableStructureModel.Plan Create(Vector3[] source, int[] indices, float delta)
    {
        var positions = (Vector3[])source.Clone();
        Lift01FarSideParts.Apply(source, positions, delta);
        Lift01FarBases.Apply(source, positions, delta);
        Lift01FarMovingParts.Apply(source, positions, delta);
        var plan = Lift01FarJoinedParts.Complete(source, positions, indices, delta);
        Lift01RoadReplacement.RemoveNativeRoad(Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[0], plan);
        // Native Geometry a634d30d56089487f0a4fbd1d6f7dfbe3644c99f5b447b4a6658e4db282fd925.
        // Fixed component 0: 72 exclusive vertices, 36 faces. The detailed-derived
        // tower supplement replaces these baked lateral/transverse proxy panels.
        // Independent diagonal supports, rooms, stairs and moving parts remain native.
        foreach (var fragment in plan.Fragments)
            if (IsTowerProxy(fragment.Triangle))
                throw new InvalidOperationException("Lift01 tower replacement overlaps a platform fragment.");
        for (var face = Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[1]; face < Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[2]; face++)
            plan.RemoveTriangles[Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[3]][face] = true;
        for (var face = Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[4]; face < Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[5]; face++)
            plan.RemoveTriangles[Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[6]][face] = true;
        return Lift01FarBaseProfile.Apply(plan, delta);
    }

    private static bool IsTowerProxy(int face) => face >= Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[7] && face < Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[8] || face >= Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[9] && face < Numeric_Types_Lift01_Lift01FarStructure_cs.Values.I32[10];
}

// Embedded numerical arrays: Types_Lift01_Lift01FarStructure_cs
internal static class Numeric_Types_Lift01_Lift01FarStructure_cs
{
    static Numeric_Types_Lift01_Lift01FarStructure_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01FarStructure_cs");
}
