using Game.Prefabs;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Native LiftBridge04 mesh 0, Geometry SHA-256:
// 869158c6d06c9a9717851460adf922838d45c294c4b1e692b7b96e4585422980.
// Repeating units measured at faces 4198/4199, 28426/28427,
// 10581/10582 and 33328/33329. Preserve the prototype bed's own appearance.
internal static class Lift04RoadAppearance
{
    internal static MovableRoadAppearance.Level ForLod(int lod, RenderPrefab nativeLevel, Pose roadToObject, float halfDelta)
    {
        if (lod == 2)
            return Lift04FarAppearance.Create(nativeLevel, roadToObject, halfDelta);
        return Lift04NearAppearance.Create(lod, nativeLevel, roadToObject);
    }

    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var min = new Vector2(Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[0], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[1]);
        var max = new Vector2(Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[2], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[3]);
        float movingStart = Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[4];
        float movingLength = Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[5] - movingStart;
        Patch Unit(float direction, float length, float start, float lateral) => new(min, max, frame * new Vector4(Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[6], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[7], direction / length, -start / length), frame * new Vector4(lateral / Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[8], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[9], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[10], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[11]), repeat: true);
        return new[]
        {
            Unit(-Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[12], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[13], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[14], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[15]),
            Unit(Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[16], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[17], Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[18], -Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[19]),
            Unit(-Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[20], movingLength, movingStart, -Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[21]),
            Unit(Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[22], movingLength, movingStart, -Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32[23])
        };
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            // Appearance indices are independent of the unchanged physical bones 0/2.
            var first = node.Bone == Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.I32[0] ? Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.I32[1] : Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.I32[2];
            return new Partition(pose, new Partition(first), new Partition(first + Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.I32[3]));
        }

        return Convert(Lift04RoadPartitions.Carriageway);
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04RoadAppearance_cs
internal static class Numeric_Types_Lift04_Lift04RoadAppearance_cs
{
    static Numeric_Types_Lift04_Lift04RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04RoadAppearance_cs");
}
