using UnityEngine;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;
internal static class Lift05StructurePlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta,
        SurfaceAsset? finishedSurface = null, Vector2 contentMin = default, Vector2 contentMax = default)
    {
        var plan = Lift05StructureWidthPlan.Create(lod, source, indices, delta);
        if (lod == Numeric_Types_Lift05_Lift05StructurePlan_cs.Values.I32[0])
            plan = Lift05FarRoadBoundary.Apply(plan, delta);
        else
            Lift05RoadTops.RemoveDetailedFixed(lod, indices, plan.RemoveTriangles[Numeric_Types_Lift05_Lift05StructurePlan_cs.Values.I32[1]]);
        Lift05RoadTops.RemoveMoving(lod, indices, plan.RemoveTriangles[Numeric_Types_Lift05_Lift05StructurePlan_cs.Values.I32[2]]);
        return Lift05StructuralRepeat.Apply(lod, plan, finishedSurface, contentMin, contentMax);
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05StructurePlan_cs
internal static class Numeric_Types_Lift05_Lift05StructurePlan_cs
{
    static Numeric_Types_Lift05_Lift05StructurePlan_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05StructurePlan_cs");
}
