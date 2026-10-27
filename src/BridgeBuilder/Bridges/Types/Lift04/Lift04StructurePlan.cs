using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift04StructurePlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var plan = Lift04StructureWidthPlan.Create(lod, source, indices, delta);
        if (lod == Numeric_Types_Lift04_Lift04StructurePlan_cs.Values.I32[0])
            return Lift04FarShoulders.Apply(plan, delta);
        Lift04RoadTops.RemoveDetailed(lod, indices, plan.RemoveTriangles[Numeric_Types_Lift04_Lift04StructurePlan_cs.Values.I32[1]]);
        Lift04TrackRemoval.RemoveDetailed(lod, indices, plan.RemoveTriangles[Numeric_Types_Lift04_Lift04StructurePlan_cs.Values.I32[2]]);
        return plan;
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04StructurePlan_cs
internal static class Numeric_Types_Lift04_Lift04StructurePlan_cs
{
    static Numeric_Types_Lift04_Lift04StructurePlan_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04StructurePlan_cs");
}
