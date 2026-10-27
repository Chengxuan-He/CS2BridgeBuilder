using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01ApproachStructure
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var positions = (Vector3[])source.Clone();
        Lift01ApproachParts.Apply(lod, source, positions, delta);
        if (lod == Numeric_Types_Lift01_Lift01ApproachStructure_cs.Values.I32[0])
            return Lift01ApproachFarShell.Complete(source, positions, indices, delta);
        Lift01ApproachBases.Apply(lod, source, positions, delta);
        return Lift01ApproachFrames.Complete(lod, source, positions, indices, delta);
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01ApproachStructure_cs
internal static class Numeric_Types_Lift01_Lift01ApproachStructure_cs
{
    static Numeric_Types_Lift01_Lift01ApproachStructure_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01ApproachStructure_cs");
}
