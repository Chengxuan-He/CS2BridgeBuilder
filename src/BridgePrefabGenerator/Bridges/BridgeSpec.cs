using System;
using System.Collections.Generic;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// What a bridge prefab is, apart from the parts it references - as numbers, read out of the game once.
///
/// The same rule the tower follows, applied to the bridge itself: a generated bridge is held to its
/// archetype, and the archetype is written down rather than copied from a prefab at generation time.
/// The bridge a style is derived from can be absent - a road is convertible with none of the style's
/// content installed - and the generator still has to produce something that behaves like a bridge.
///
/// Read from <c>Suspension Bridge - Highway Oneway - 5 Lanes</c> and confirmed against the 4-lane one,
/// which holds every value identically. That is what makes these family constants rather than one
/// bridge's settings:
///
///     Bridge        m_SegmentLength=256 m_Hanging=0 m_ElevationOnWater=10 m_CanCurve=False
///                   m_AllowMinimalLength=False m_WaterFlow=Any m_BuildStyle=Elevated m_FixedSegments=[]
///     PlaceableNet  m_ElevationRange=(0,200) m_AllowParallelMode=True m_XPReward=4
///     RoadPrefab    m_MaxSlopeSteepness=0.2 m_AggregateType=Bridge
///     [subobject]   at (0, 77.9, 0) placement=EdgeMiddle fixedIndex=0 spacing=0
///                   anchorTop=False anchorCenter=False requireElevated=False
///     [overhead]    at (0,0,0) median
///
/// Only the suspension family has been measured. A style with no entry here keeps the donor's own
/// values, which is a deviation from the rule and is reported as one - not silently, because a bridge
/// built from unmeasured numbers is exactly as wrong as one built from invented ones, and the only
/// difference is whether anybody knows. <c>AssetAnatomy</c> is what fills the gaps in.
/// </summary>
internal static class BridgeSpec
{
    /// <summary>The archetype of one bridge type, as far as it has been measured.</summary>
    internal readonly struct Archetype
    {
        internal Archetype(
            string measuredFrom,
            float segmentLength,
            float hanging,
            float elevationOnWater,
            bool canCurve,
            bool allowMinimalLength,
            float towerHeightAboveOrigin,
            float elevationMin,
            float elevationMax,
            bool allowParallelMode,
            int xpReward)
        {
            MeasuredFrom = measuredFrom;
            SegmentLength = segmentLength;
            Hanging = hanging;
            ElevationOnWater = elevationOnWater;
            CanCurve = canCurve;
            AllowMinimalLength = allowMinimalLength;
            TowerHeightAboveOrigin = towerHeightAboveOrigin;
            ElevationMin = elevationMin;
            ElevationMax = elevationMax;
            AllowParallelMode = allowParallelMode;
            XpReward = xpReward;
        }

        /// <summary>The bridge these numbers were read from, so a wrong one can be traced.</summary>
        internal string MeasuredFrom { get; }

        /// <summary>How far apart the towers stand.</summary>
        internal float SegmentLength { get; }

        internal float Hanging { get; }

        internal float ElevationOnWater { get; }

        internal bool CanCurve { get; }

        internal bool AllowMinimalLength { get; }

        /// <summary>
        /// Where the deck meets the tower, measured up from the tower's own origin - the y of the
        /// bridge's tower sub object.
        ///
        /// Not a free choice. The tower is a fixed height and the game places it by comparing that
        /// height against the gap between deck and ground, so this number decides where the tower sits
        /// relative to the road rather than describing it.
        /// </summary>
        internal float TowerHeightAboveOrigin { get; }

        /// <summary>
        /// How high the bridge may be built, and how low.
        ///
        /// The archetype allows nothing below ground and up to two hundred metres above it. A road's
        /// own range is not that - it runs from a hundred below to a hundred above, because a road may
        /// be a tunnel - and a bridge that keeps the road's range can be placed at heights its tower
        /// was never drawn for.
        /// </summary>
        internal float ElevationMin { get; }

        internal float ElevationMax { get; }

        internal bool AllowParallelMode { get; }

        internal int XpReward { get; }
    }

    private static readonly Dictionary<string, Archetype> Table =
        new(StringComparer.Ordinal)
        {
            ["Suspension"] = new Archetype(
                "Suspension Bridge - Highway Oneway - 5 Lanes",
                segmentLength: 256f,
                hanging: 0f,
                elevationOnWater: 10f,
                canCurve: false,
                allowMinimalLength: false,
                towerHeightAboveOrigin: 77.9f,
                elevationMin: 0f,
                elevationMax: 200f,
                allowParallelMode: true,
                xpReward: 4),
        };

    /// <summary>The archetype for a bridge type, or null when it has not been measured yet.</summary>
    internal static Archetype? For(string styleId)
    {
        return Table.TryGetValue(styleId, out var archetype) ? archetype : null;
    }

    /// <summary>Every type with a recorded archetype, for the tests to walk.</summary>
    internal static IEnumerable<string> Styles => Table.Keys;

    /// <summary>
    /// How a bridge anchors its tower, field by field, keyed by the field's own name.
    ///
    /// A map rather than a set of properties, because the caller applies it by walking every field of
    /// <c>NetSubObjectInfo</c> and looking each one up. A field this does not answer for is reported as
    /// unrecorded instead of quietly taking whatever a default-constructed entry holds - which is the
    /// only way an omission here becomes visible, and omissions here are what put a tower at the wrong
    /// height. Listing the fields by hand is the habit that produced them.
    ///
    /// Values are engine-free so the tests can read them: an enum is its number, and the caller
    /// converts. Identical on both suspension bridges measured.
    ///
    /// <c>m_Object</c> and <c>m_Position</c> are absent on purpose - the first is the tower being
    /// placed and the second depends on which tower it is, so both are supplied by the caller.
    /// </summary>
    internal static IReadOnlyDictionary<string, object> TowerBinding { get; } =
        new Dictionary<string, object>(StringComparer.Ordinal)
        {
            // quaternion.identity, as four components. The archetype places its tower unrotated.
            ["m_Rotation"] = new[] { 0f, 0f, 0f, 1f },

            // NetObjectPlacement.EdgeMiddle: one tower at the middle of each span.
            ["m_Placement"] = PlacementEdgeMiddle,

            ["m_FixedIndex"] = 0,

            // Zero, because EdgeMiddle places one per edge rather than repeating along it. A bridge's
            // span length is what puts the towers apart, not this.
            ["m_Spacing"] = 0f,

            ["m_AnchorTop"] = false,
            ["m_AnchorCenter"] = false,

            // Clear on the archetype. Setting it was a deviation that also changed how the road's own
            // props were sorted, because the same flag is what tells a bridge's structure from a
            // road's default pillars.
            ["m_RequireElevated"] = false,

            ["m_RequireOutsideConnection"] = false,
            ["m_RequireDeadEnd"] = false,
            ["m_RequireOrphan"] = false,
        };

    /// <summary><c>NetObjectPlacement.EdgeMiddle</c>, as its number so this file needs nothing from the game.</summary>
    internal const int PlacementEdgeMiddle = 2;

    /// <summary>Where a bridge draws its cables: centred, and marked as a median section.</summary>
    internal static class CablePlacement
    {
        internal const float Offset = 0f;

        internal const bool Median = true;
    }
}
