using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift02RoadPreparation
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "Lift02");
        var appearance = Lift02RoadAppearance.Regions(sample.RoadToObject);
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material, Lift02RoadPartitions.Carriageway, Lift02RoadPartitions.Carriageway, appearance, appearance, Lift02RoadAppearance.Patches(sample.RoadToObject), Numeric_Types_Lift02_Lift02RoadPreparation_cs.Values.I32[0], layout,
            appearanceForLod: lod => Lift02RoadAppearance.ForLod(lod, nativeLevels[lod], sample.RoadToObject, halfWidthDelta));
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02RoadPreparation_cs
internal static class Numeric_Types_Lift02_Lift02RoadPreparation_cs
{
    static Numeric_Types_Lift02_Lift02RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift02_Lift02RoadPreparation_cs");
}
