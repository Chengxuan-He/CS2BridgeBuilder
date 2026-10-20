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
        Lift01RoadReplacement.RemoveNativeRoad(2, plan);

        // Native Geometry a634d30d56089487f0a4fbd1d6f7dfbe3644c99f5b447b4a6658e4db282fd925.
        // Fixed component 0: 72 exclusive vertices, 36 faces. The detailed-derived
        // tower supplement replaces these baked lateral/transverse proxy panels.
        // Independent diagonal supports, rooms, stairs and moving parts remain native.
        foreach (var fragment in plan.Fragments)
            if (IsTowerProxy(fragment.Triangle))
                throw new InvalidOperationException("Lift01 tower replacement overlaps a platform fragment.");
        for (var face = 0; face < 18; face++) plan.RemoveTriangles[0][face] = true;
        for (var face = 388; face < 406; face++) plan.RemoveTriangles[0][face] = true;
        return Lift01FarBaseProfile.Apply(plan, delta);
    }

    private static bool IsTowerProxy(int face) => face >= 0 && face < 18 || face >= 388 && face < 406;
}
