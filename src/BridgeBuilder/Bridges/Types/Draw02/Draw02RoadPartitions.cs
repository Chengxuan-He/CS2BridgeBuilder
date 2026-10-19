using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// DrawBridge02 main mesh 0, game 1.6.2f1. These are longitudinal motion
// boundaries, not a structural triangle-removal mask or material classification.
// Original Geometry SHA-256:
// 1f0ff1a2c0a8318e6537bd347ae064d1bb55bfa48f3cf3c2375601adcd05dd18
internal static class Draw02RoadPartitions
{
    // Fixed road faces 10733/10819 meet the positive hinge opening here.
    // Moving road faces 2544/2526 end at the other side of that opening.
    // The negative leaf has the corresponding mirrored authored boundary.
    private const float FixedStart = 78.7421875f;
    private const float LeafEnd = 78.69921875f;

    // Binary32 carriageway seam from faces 4434/15392. The sidewalk's
    // independently recorded seam contains a tiny transverse skew. Its maximum
    // extent defines the conservative planar gap envelope (16.3 micrometres
    // larger than its minimum), rather than borrowing Draw01's sidewalk gap.
    internal static readonly Partition Carriageway = Create(0.030005931854248047f);
    internal static readonly Partition Sidewalk = Create(0.030022144317626953f);

    // Native bone ARRAY index 1 is the positive-Z leaf (connection 101),
    // index 2 is the negative-Z leaf (connection 100), and index 0 is fixed.
    // All generated LODs share these physical seams; the coarse original
    // visual shell does not define a different skeleton or traffic height.
    private static Partition Create(float centreHalfGap)
    {
        var fixedRoad = new Partition(0);
        return Cut(-FixedStart, fixedRoad,
            Cut(-LeafEnd, Partition.Gap,
                Cut(-centreHalfGap, new Partition(2),
                    Cut(centreHalfGap, Partition.Gap,
                        Cut(LeafEnd, new Partition(1),
                            Cut(FixedStart, Partition.Gap, fixedRoad))))));
    }

    private static Partition Cut(float seam, Partition negative, Partition positive)
    {
        var frame = Matrix4x4.identity;
        frame.m23 = -seam;
        return new Partition(frame, negative, positive);
    }
}
