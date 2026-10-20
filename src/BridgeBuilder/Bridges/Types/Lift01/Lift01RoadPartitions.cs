using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Measured main mesh-0 horizontal road surfaces, native game 1.6.2f1.
// Near and middle geometry independently agree on these binary32 coordinates.
// This is motion ownership only, not a structural face-removal or sidewalk mask.
internal static class Lift01RoadPartitions
{
    internal const float MovingEnd = 79.9921875f;
    internal const float FixedStart = 80.02734375f;
    internal const float FixedEnd = 95.99609375f;

    // Bone array index 4 is TrainTracks, connection 100. Bone 3 is the
    // counterweight and must never own selected-road surfaces or paint.
    // Keep both physical gaps at every LOD, even though the original far shell
    // has different top heights and broader extents.
    internal static readonly Partition Carriageway = Cut(-FixedEnd, Partition.Gap,
        Cut(-FixedStart, new Partition(0),
            Cut(-MovingEnd, Partition.Gap,
                Cut(MovingEnd, new Partition(4),
                    Cut(FixedStart, Partition.Gap,
                        Cut(FixedEnd, new Partition(0), Partition.Gap))))));

    private static Partition Cut(float station, Partition negative, Partition positive)
    {
        var frame = Matrix4x4.identity;
        frame.m23 = -station;
        return new Partition(frame, negative, positive);
    }
}
