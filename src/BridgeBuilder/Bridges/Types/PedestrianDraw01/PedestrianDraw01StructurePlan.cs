using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw01StructurePlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var plan = PedestrianDraw01StructureWidthPlan.Create(lod, source, indices, delta);
        PedestrianDraw01RoadTops.Remove(lod, indices, plan.RemoveTriangles[Numeric_Types_PedestrianDraw01_PedestrianDraw01StructurePlan_cs.Values.I32[0]]);
        if (lod == Numeric_Types_PedestrianDraw01_PedestrianDraw01StructurePlan_cs.Values.I32[1])
            plan = PedestrianDraw01FarRoadBoundary.Apply(plan);
        return plan;
    }
}

// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01StructurePlan_cs
internal static class Numeric_Types_PedestrianDraw01_PedestrianDraw01StructurePlan_cs
{
    static Numeric_Types_PedestrianDraw01_PedestrianDraw01StructurePlan_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PedestrianDraw01StructurePlan_cs");
}
