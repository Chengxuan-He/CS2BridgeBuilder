using UnityEngine;

namespace BridgeBuilder.Bridges;

// Consume only together with prepared selected-road geometry. Detailed native
// rail/sleeper/fastener geometry is replaced; the supporting roadbed remains.
internal static class Lift02StructurePlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var plan = Lift02StructureWidthPlan.Create(lod, source, indices, delta);
        if (lod == 2)
        {
            Lift02FarRoadTops.Remove(indices, plan.RemoveTriangles[0]);
            plan = Lift02FarRoadBoundary.Apply(plan);
        }
        else
            Lift02TrackRemoval.RemoveDetailed(lod, indices, plan.RemoveTriangles[0]);
        return plan;
    }
}

