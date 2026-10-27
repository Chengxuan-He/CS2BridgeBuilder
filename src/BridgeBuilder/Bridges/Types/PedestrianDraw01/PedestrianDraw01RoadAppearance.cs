using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Native PedestrianDrawBridge01 mesh 0, Geometry SHA-256:
// 491d8dae6420e0b5b00fbf0014ff74a8aff44c45a1bd48ab40708978888d7f05.
// Fixed units: faces 1035/1036 and 13243/13244. Moving: 1091/1092 and 13299/13300.
// Both leaves retain the same negative longitudinal UV gradient; their origins differ.
internal static class PedestrianDraw01RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        float movingWidth = Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[0];
        float movingLength = Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[1] - Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[2];
        Patch Fixed(float minV, float end) => new(new Vector2(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[3], minV), new Vector2(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[4], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[5]), frame * new Vector4(-Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[6], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[7], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[8], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[9]), frame * new Vector4(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[10], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[11], -Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[12], end / Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[13]), repeat: true);
        Patch Moving(float end) => new(new Vector2(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[14], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[15]), new Vector2(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[16], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[17]), frame * new Vector4(-Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[18] / movingWidth, Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[19], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[20], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[21]), frame * new Vector4(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[22], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[23], -Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[24] / movingLength, end / movingLength), repeat: true);
        return new[]
        {
            Fixed(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[25], Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[26]),
            Fixed(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[27], -Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[28]),
            Moving(Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[29]),
            Moving(-Numeric_Types_PedestrianDraw01_PedestrianDraw01RoadAppearance_cs.Values.F32[30])
        };
    }

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
