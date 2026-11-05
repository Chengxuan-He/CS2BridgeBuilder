using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkContext
{
    // Snapshot values only: no temporary-world entity survives CompileRoad.
    internal readonly struct RoadLane
    {
        internal readonly Vector3 Start, Control1, Control2, End;
        internal readonly float Width, StartWidth, EndWidth;
        internal readonly LaneFlags Flags;
        internal readonly string Prefab;
        internal readonly bool Straight;
        internal RoadLane(Vector3 start, Vector3 control1, Vector3 control2, Vector3 end, float width, float startWidth, float endWidth, LaneFlags flags, bool straight, string prefab)
        { Start = start; Control1=control1; Control2=control2; End = end; Width = width; StartWidth = startWidth; EndWidth = endWidth; Flags = flags; Straight = straight; Prefab=prefab; }

        internal MovableRoadLaneLayout.Kind Type
        {
            get
            {
                var kind=MovableRoadLaneLayout.Kind.NonLaneGap;
                if((Flags & LaneFlags.BicyclesOnly)!=0)kind|=MovableRoadLaneLayout.Kind.Bicycle;
                if((Flags & LaneFlags.Pedestrian)!=0)kind|=MovableRoadLaneLayout.Kind.Pedestrian;
                if((Flags & LaneFlags.Parking)!=0)kind|=MovableRoadLaneLayout.Kind.Parking;
                if((Flags & LaneFlags.Track)!=0)kind|=MovableRoadLaneLayout.Kind.Track;
                if((Flags & LaneFlags.Road)!=0 && (Flags & (LaneFlags.BicyclesOnly|LaneFlags.Parking))==0)
                    kind|=MovableRoadLaneLayout.Kind.Car;
                return kind==MovableRoadLaneLayout.Kind.NonLaneGap?MovableRoadLaneLayout.Kind.Other:kind;
            }
        }
    }
}
