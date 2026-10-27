using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw03DetailedStructure
{
    // Near and middle main mesh only. The merged far mesh has its own authored parts.
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        if (lod != Numeric_Types_Draw03_Draw03DetailedStructure_cs.Values.I32[0] && lod != Numeric_Types_Draw03_Draw03DetailedStructure_cs.Values.I32[1])
            throw new ArgumentOutOfRangeException(nameof(lod));
        var positions = (Vector3[])source.Clone();
        Draw03RigidParts.Apply(lod, source, positions, delta);
        Draw03Shafts.Apply(lod, source, positions, delta);
        Draw03CentreMembers.Apply(lod, source, positions, delta);
        Draw03Bases.Apply(lod, source, positions, delta);
        // Side frames and their centre member belong to one full-width truss.
        Draw03Truss.Apply(lod, source, positions, delta);
        Draw03EndStrips.Apply(lod, source, positions, delta);
        var structure = Draw03FixedPlatforms.Complete(lod, source, positions, indices, delta);
        return Draw03RoadReplacement.Apply(lod, structure, delta);
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03DetailedStructure_cs
internal static class Numeric_Types_Draw03_Draw03DetailedStructure_cs
{
    static Numeric_Types_Draw03_Draw03DetailedStructure_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03DetailedStructure_cs");
}
