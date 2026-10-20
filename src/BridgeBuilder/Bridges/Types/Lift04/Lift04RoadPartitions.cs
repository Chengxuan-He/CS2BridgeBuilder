using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native LiftBridge04 detailed road-bed boundaries, in the original mesh frame.
// Moving road is bone 2; the counterweight must never receive road geometry.
internal static class Lift04RoadPartitions
{
    internal const float MovingEnd = 79.849609375f;
    internal const float FixedStart = 80f;
    internal const float FixedEnd = 96f;

    internal static readonly Partition Carriageway = Cut(-FixedEnd, Partition.Gap,
        Cut(-FixedStart, new Partition(0),
            Cut(-MovingEnd, Partition.Gap,
                Cut(MovingEnd, new Partition(2),
                    Cut(FixedStart, Partition.Gap,
                        Cut(FixedEnd, new Partition(0), Partition.Gap))))));

    private static Partition Cut(float station, Partition negative, Partition positive)
    {
        var frame = Matrix4x4.identity;
        frame.m23 = -station;
        return new Partition(frame, negative, positive);
    }
}
