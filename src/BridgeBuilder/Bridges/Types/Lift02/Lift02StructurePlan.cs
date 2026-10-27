using UnityEngine;

namespace BridgeBuilder.Bridges;
// Consume only together with prepared selected-road geometry. Detailed native
// rail/sleeper/fastener geometry is replaced; the supporting roadbed remains.
internal static class Lift02StructurePlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var plan = Lift02StructureWidthPlan.Create(lod, source, indices, delta);
        if (lod == Numeric_Types_Lift02_Lift02StructurePlan_cs.Values.I32[0])
        {
            Lift02FarRoadTops.Remove(indices, plan.RemoveTriangles[Numeric_Types_Lift02_Lift02StructurePlan_cs.Values.I32[1]]);
            plan = Lift02FarRoadBoundary.Apply(plan);
        }
        else
            Lift02TrackRemoval.RemoveDetailed(lod, indices, plan.RemoveTriangles[Numeric_Types_Lift02_Lift02StructurePlan_cs.Values.I32[2]]);
        return plan;
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02StructurePlan_cs
internal static class Numeric_Types_Lift02_Lift02StructurePlan_cs
{
    static Numeric_Types_Lift02_Lift02StructurePlan_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift02_Lift02StructurePlan_cs");
}
