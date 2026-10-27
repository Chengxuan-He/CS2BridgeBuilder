using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Native Lift05 carriageway seams, measured independently from near/middle Geometry.
// Sidewalk/structural domains require separate preparation; this is motion ownership.
internal static class Lift05RoadPartitions
{
    internal static float MovingEnd => Numeric_Types_Lift05_Lift05RoadPartitions_cs.Values.F32[0];
    internal static float FixedStart => Numeric_Types_Lift05_Lift05RoadPartitions_cs.Values.F32[1];
    internal static float FixedEnd => Numeric_Types_Lift05_Lift05RoadPartitions_cs.Values.F32[2];

    // Original road bone array index 2, connection 100. Index 1 is counterweight.
    // Far fixed shells must adapt to these detailed seams, not redefine them.
    internal static readonly Partition Carriageway = Cut(-FixedEnd, Partition.Gap, Cut(-FixedStart, new Partition(Numeric_Types_Lift05_Lift05RoadPartitions_cs.Values.I32[0]), Cut(-MovingEnd, Partition.Gap, Cut(MovingEnd, new Partition(Numeric_Types_Lift05_Lift05RoadPartitions_cs.Values.I32[1]), Cut(FixedStart, Partition.Gap, Cut(FixedEnd, new Partition(Numeric_Types_Lift05_Lift05RoadPartitions_cs.Values.I32[2]), Partition.Gap))))));
    private static Partition Cut(float station, Partition negative, Partition positive)
    {
        var frame = Matrix4x4.identity;
        frame.m23 = -station;
        return new Partition(frame, negative, positive);
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05RoadPartitions_cs
internal static class Numeric_Types_Lift05_Lift05RoadPartitions_cs
{
    static Numeric_Types_Lift05_Lift05RoadPartitions_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05RoadPartitions_cs");
}
