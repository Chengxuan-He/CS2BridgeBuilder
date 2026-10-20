using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift05StructurePlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var plan = Lift05StructureWidthPlan.Create(lod, source, indices, delta);
        if (lod == 2)
            plan = Lift05FarRoadBoundary.Apply(plan, delta);
        else
            Lift05RoadTops.RemoveDetailedFixed(lod, indices, plan.RemoveTriangles[0]);
        Lift05RoadTops.RemoveMoving(lod, indices, plan.RemoveTriangles[0]);
        return plan;
    }
}
