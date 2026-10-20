using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift04StructurePlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var plan = Lift04StructureWidthPlan.Create(lod, source, indices, delta);
        if (lod == 2) return Lift04FarShoulders.Apply(plan, delta);
        Lift04RoadTops.RemoveDetailed(lod, indices, plan.RemoveTriangles[0]);
        Lift04TrackRemoval.RemoveDetailed(lod, indices, plan.RemoveTriangles[0]);
        return plan;
    }
}
