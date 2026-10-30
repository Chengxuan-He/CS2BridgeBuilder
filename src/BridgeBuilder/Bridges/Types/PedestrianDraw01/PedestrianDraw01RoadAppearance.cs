using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Original physical appearance domains; native road cells add only evidenced UV reset boundaries.
internal static class PedestrianDraw01RoadAppearance
{
    internal static Partition Regions(Partition physical, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            if (node.Bone == Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.I32[0])
                return new Partition(pose, new Partition(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.I32[1]), new Partition(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.I32[2]));
            return new Partition(node.Bone + Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.I32[3]);
        }

        return Convert(physical);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs
internal static class Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs
{
    static Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs");
}
