using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw01RoadPreparation
{
    internal static MovableRoadAssets.Level[] Prepare(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material)
    {
        var physical = PedestrianDraw01RoadPartitions.Carriageway;
        var originalAppearance = PedestrianDraw01RoadAppearance.Regions(physical, sample.RoadToObject);
        var appearance = PedestrianDraw01PreparedRoadMapping.Regions(originalAppearance, sample.RoadToObject);
        var layout = new MovableSelectedRoadLayout(sample.Road, sample.RoadToObject, "PedestrianDraw01");
        var detailed = PedestrianDraw01PreparedRoadMapping.Patches(sample.RoadToObject, halfWidthDelta);
        return MovableRoadPreparation.Prepare(sample, nativeLevels, halfWidthDelta, closedWorld, material,
            physical, physical, appearance, appearance, detailed,
            Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs.Values.I32[0], layout,
            appearanceForLod: lod =>
            {
                var native = nativeLevels[lod].GetSurfaceAsset(0);
                var patches = lod == 2
                    ? PedestrianDraw01FarRoadMapping.Patches(native, sample.RoadToObject, halfWidthDelta)
                    : PedestrianDraw01PreparedRoadMapping.Patches(sample.RoadToObject, halfWidthDelta);
                var regions = lod == 2 ? PedestrianDraw01FarRoadMapping.Regions(physical, sample.RoadToObject)
                    : PedestrianDraw01PreparedRoadMapping.Regions(originalAppearance, sample.RoadToObject);
                var supported = new MovableRoadAppearance.Type[patches.Length];
                // Native Pavement Path Section 8 carries PedestrianLane geometry.
                // This is pathway surfacing, not a Sidewalk layout occurrence and
                // not evidence of asphalt, wheel-track or vehicle-road support.
                for (var region = 0; region < supported.Length; region++)
                    supported[region] = MovableRoadAppearance.Type.Pathway;
                return new MovableRoadAppearance.Level(native, patches, supported,
                    carriageway: regions, sidewalk: regions);
            });
    }
}

// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs
internal static class Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs
{
    static Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PedestrianDraw01RoadPreparation_cs");
}
