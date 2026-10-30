using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;

// Appearance-domain order is Draw01RoadPartitions.MaterialRegions / ClosedRoad.Patches.
// The source near/middle road top has asphalt on the carriageway, cutout grating
// on moving sidewalks and paving on fixed sidewalks. Fixed carriageway caps use
// their separate native concrete patch. These flags select the input layer type;
// they do not classify an entire selected Section or grant wheel-track support.
internal static class Draw01RoadAppearance
{
    internal static readonly Type[] SupportedTypes =
    {
        Type.Asphalt, Type.Sidewalk, Type.Sidewalk, Type.Asphalt,
        Type.Sidewalk, Type.Asphalt, Type.Asphalt,
        Type.Asphalt, Type.Sidewalk, Type.Sidewalk, Type.Asphalt,
        Type.Sidewalk, Type.Asphalt, Type.Asphalt
    };
}
