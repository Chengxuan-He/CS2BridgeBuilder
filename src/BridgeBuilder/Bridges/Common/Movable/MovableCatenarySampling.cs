using Game.Net;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

// A movable span is sampled as an unusually long straight node. Native train/tram
// auxiliary wires normally exist on edges and special nodes only. Permit their
// topology-only selector on this private straight-node sample as well; all other
// requirements, native dimensions and selected-track eligibility remain intact.
internal static class MovableCatenarySampling
{
    internal static AuxiliaryNetLane ForNode(AuxiliaryNetLane lane, UtilityTypes utilities)
    {
        const CompositionFlags.General topology = CompositionFlags.General.Edge |
            CompositionFlags.General.Node | CompositionFlags.General.Intersection |
            CompositionFlags.General.DeadEnd | CompositionFlags.General.LevelCrossing;
        var any = lane.m_CompositionAny;
        // Native train contact wire is LowVoltageLine; its messenger and tram
        // contact wires are Catenary. Caller restricts this to track auxiliaries.
        if ((utilities & (UtilityTypes.Catenary | UtilityTypes.LowVoltageLine)) != 0 &&
            (any.m_General & CompositionFlags.General.Edge) != 0 &&
            (any.m_General & ~topology) == 0 && any.m_Left == 0 && any.m_Right == 0)
        {
            any.m_General |= CompositionFlags.General.Node;
            lane.m_CompositionAny = any;
        }
        return lane;
    }
}
