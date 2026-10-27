using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Native detailed Lift02 bed-top bone boundary; fixed ends retain bone 0.
// Far simplified boundaries must follow this detailed motion domain.
internal static class Lift02RoadPartitions
{
    internal static float MovingEnd => Numeric_Types_Lift02_Lift02RoadPartitions_cs.Values.F32[0];
    internal static float FixedEnd => Numeric_Types_Lift02_Lift02RoadPartitions_cs.Values.F32[1];

    // Bone array index 1 is TrainTrack (connection 100); bone 4 is counterweight.
    // Original detailed top faces meet at +/-82 without an authored top-surface gap.
    internal static readonly Partition Carriageway = Cut(-FixedEnd, Partition.Gap, Cut(-MovingEnd, new Partition(Numeric_Types_Lift02_Lift02RoadPartitions_cs.Values.I32[0]), Cut(MovingEnd, new Partition(Numeric_Types_Lift02_Lift02RoadPartitions_cs.Values.I32[1]), Cut(FixedEnd, new Partition(Numeric_Types_Lift02_Lift02RoadPartitions_cs.Values.I32[2]), Partition.Gap))));
    private static Partition Cut(float station, Partition negative, Partition positive)
    {
        var frame = Matrix4x4.identity;
        frame.m23 = -station;
        return new Partition(frame, negative, positive);
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02RoadPartitions_cs
internal static class Numeric_Types_Lift02_Lift02RoadPartitions_cs
{
    static Numeric_Types_Lift02_Lift02RoadPartitions_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift02_Lift02RoadPartitions_cs");
}
