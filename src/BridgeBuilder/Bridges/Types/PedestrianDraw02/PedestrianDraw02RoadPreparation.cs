using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw02RoadPreparation
{
    internal static MovableRoadAssets.Level[] Prepare(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material)
    {
        var physical = PedestrianDraw02LocalAppearance.Physical(halfWidthDelta);
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "PedestrianDraw02");
        var levels = new MovableRoadAppearance.Level[nativeLevels.Length];
        for (int lod = 0; lod < levels.Length; lod++)
            levels[lod] = PedestrianDraw02AtlasAppearance.Create(nativeLevels[lod].GetSurfaceAsset(0),
                sample.RoadToObject, halfWidthDelta);
        var first = levels[0];
        // Both measured mechanism LODs carry the native paved pathway. This does
        // not authorize substituting asphalt, sidewalks or wheel-track layers.
        // Resolve the Surface from this LOD even though these two original LODs
        // currently share its CID; never inherit a different LOD's descriptor.
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld,
            material, physical, physical, first.Carriageway!, first.Sidewalk!, first.Patches,
            Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs.Values.I32[0], layout,
            appearanceForLod: lod => levels[lod], combinePartitions: true);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02RoadPreparation_cs");
}
