using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native Lift05 carriageway seams, measured independently from near/middle Geometry.
// Sidewalk/structural domains require separate preparation; this is motion ownership.
internal static class Lift05RoadPartitions
{
    internal const float MovingEnd = 62.4609375f;
    internal const float FixedStart = 62.580078125f;
    internal const float FixedEnd = 84f;

    // Original road bone array index 2, connection 100. Index 1 is counterweight.
    // Far fixed shells must adapt to these detailed seams, not redefine them.
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
