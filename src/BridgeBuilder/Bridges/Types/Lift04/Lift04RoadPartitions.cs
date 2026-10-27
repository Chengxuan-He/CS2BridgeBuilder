using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Native LiftBridge04 detailed road-bed boundaries, in the original mesh frame.
// Moving road is bone 2; the counterweight must never receive road geometry.
internal static class Lift04RoadPartitions
{
    internal static float MovingEnd => Numeric_Types_Lift04_Lift04RoadPartitions_cs.Values.F32[0];
    internal static float FixedStart => Numeric_Types_Lift04_Lift04RoadPartitions_cs.Values.F32[1];
    internal static float FixedEnd => Numeric_Types_Lift04_Lift04RoadPartitions_cs.Values.F32[2];

    internal static readonly Partition Carriageway = Cut(-FixedEnd, Partition.Gap, Cut(-FixedStart, new Partition(Numeric_Types_Lift04_Lift04RoadPartitions_cs.Values.I32[0]), Cut(-MovingEnd, Partition.Gap, Cut(MovingEnd, new Partition(Numeric_Types_Lift04_Lift04RoadPartitions_cs.Values.I32[1]), Cut(FixedStart, Partition.Gap, Cut(FixedEnd, new Partition(Numeric_Types_Lift04_Lift04RoadPartitions_cs.Values.I32[2]), Partition.Gap))))));
    private static Partition Cut(float station, Partition negative, Partition positive)
    {
        var frame = Matrix4x4.identity;
        frame.m23 = -station;
        return new Partition(frame, negative, positive);
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04RoadPartitions_cs
internal static class Numeric_Types_Lift04_Lift04RoadPartitions_cs
{
    static Numeric_Types_Lift04_Lift04RoadPartitions_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04RoadPartitions_cs");
}
