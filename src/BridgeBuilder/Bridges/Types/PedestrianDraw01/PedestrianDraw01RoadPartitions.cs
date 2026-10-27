using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Detailed native walking-surface stations, measured at Y=10 on bones 0/1/2.
// Far merged surfaces must be reconciled to these detailed motion domains.
internal static class PedestrianDraw01RoadPartitions
{
    internal static float CentreGapHalf => Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.Values.F32[0];
    internal static float MovingEnd => Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.Values.F32[1];
    internal static float FixedStart => Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.Values.F32[2];
    internal static float FixedEnd => Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.Values.F32[3];

    internal static readonly Partition Carriageway = Cut(-FixedEnd, Partition.Gap, Cut(-FixedStart, new Partition(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.Values.I32[0]), Cut(-MovingEnd, Partition.Gap, Cut(-CentreGapHalf, new Partition(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.Values.I32[1]), Cut(CentreGapHalf, Partition.Gap, Cut(MovingEnd, new Partition(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.Values.I32[2]), Cut(FixedStart, Partition.Gap, Cut(FixedEnd, new Partition(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.Values.I32[3]), Partition.Gap))))))));
    private static Partition Cut(float station, Partition negative, Partition positive)
    {
        var frame = Matrix4x4.identity;
        frame.m23 = -station;
        return new Partition(frame, negative, positive);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs
internal static class Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs
{
    static Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs");
}
