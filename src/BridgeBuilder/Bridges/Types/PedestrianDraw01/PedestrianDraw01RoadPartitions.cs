using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Detailed native walking-surface stations, measured at Y=10 on bones 0/1/2.
// Far merged surfaces must be reconciled to these detailed motion domains.
internal static class PedestrianDraw01RoadPartitions
{
    internal const float CentreGapHalf = .029998779296875f;
    internal const float MovingEnd = 60.28515625f;
    internal const float FixedStart = 60.3857421875f;
    internal const float FixedEnd = 76f;

    internal static readonly Partition Carriageway = Cut(-FixedEnd, Partition.Gap,
        Cut(-FixedStart, new Partition(0),
            Cut(-MovingEnd, Partition.Gap,
                Cut(-CentreGapHalf, new Partition(2),
                    Cut(CentreGapHalf, Partition.Gap,
                        Cut(MovingEnd, new Partition(1),
                            Cut(FixedStart, Partition.Gap,
                                Cut(FixedEnd, new Partition(0), Partition.Gap))))))));

    private static Partition Cut(float station, Partition negative, Partition positive)
    {
        var frame = Matrix4x4.identity;
        frame.m23 = -station;
        return new Partition(frame, negative, positive);
    }
}
