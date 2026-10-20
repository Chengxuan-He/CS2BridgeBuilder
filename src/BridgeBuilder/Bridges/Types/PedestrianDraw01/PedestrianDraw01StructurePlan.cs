using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class PedestrianDraw01StructurePlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var plan = PedestrianDraw01StructureWidthPlan.Create(lod, source, indices, delta);
        PedestrianDraw01RoadTops.Remove(lod, indices, plan.RemoveTriangles[0]);
        if (lod == 2) plan = PedestrianDraw01FarRoadBoundary.Apply(plan);
        return plan;
    }
}
