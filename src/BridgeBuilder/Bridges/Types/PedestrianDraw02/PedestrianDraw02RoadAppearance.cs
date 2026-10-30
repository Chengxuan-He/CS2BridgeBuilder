using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Native PedestrianDrawBridge02 mesh 0, game 1.6.2f1.
// Geometry SHA-256: 55b8fb530c4f5d80a211feecb2e82b6deb12cc8af5d76bdc3abef78b192494be.
// Fixed unit: faces 6708/6709, X=2.6348876953125..3.68646240234375, Z=68..72.
// Moving unit: faces 8556/8557, X=1.9114990234375..3.68646240234375,
// Z=2.2608642578125..9.05517578125. These legacy pathway crop projections
// are not the complete original finite-half mapping: fixed 4 m repetition is
// unproved and the moving transverse axis must be finite. The native-panel
// authoring work replaces these only after its filtered-mip checks pass.
internal static class PedestrianDraw02RoadAppearance
{
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        // Original packed MeshTangent frames for the measured source patches:
        // +Z fixed = +X, +Z moving = -X, both with handedness -1.
        // The opposite leaf rotates both directions. UV mirrors do not define
        // these frames; use the original native attributes, transformed to road space.
        Vector4 Tangent(float direction)
        {
            var axis = Quaternion.Inverse(roadToObject.rotation) * (Vector3.right * direction);
            return new Vector4(axis.x, axis.y, axis.z,
                -Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[27]);
        }
        float fixedWidth = Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[0] - Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[1];
        float movingWidth = Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[2] - Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[3];
        float movingLength = Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[4] - Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[5];
        Patch Fixed(float direction) => new(new Vector2(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[6], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[7]), new Vector2(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[8], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[9]), frame * new Vector4(direction / fixedWidth, Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[10], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[11], -Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[12] / fixedWidth), frame * new Vector4(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[13], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[14], direction / Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[15], -Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[16]), repeat: true, tangent: Tangent(direction));
        Patch Moving(float direction) => new(new Vector2(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[17], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[18]), new Vector2(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[19], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[20]), frame * new Vector4(-direction / movingWidth, Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[21], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[22], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[23] / movingWidth), frame * new Vector4(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[24], Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[25], -direction / movingLength, Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[26] / movingLength), repeat: true, tangent: Tangent(-direction));
        return new[]
        {
            Fixed(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[27]),
            Fixed(-Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[28]),
            Moving(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[29]),
            Moving(-Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.F32[30])
        };
    }

    // Physical leaves carry original bone indices; appearance leaves carry patch
    // indices. Transform every seam into sampled-road space before the atlas pass.
    internal static Partition Regions(Partition physical, Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            if (node.Bone == Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.I32[0])
                return new Partition(pose, new Partition(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.I32[1]), new Partition(Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.I32[2]));
            return new Partition(node.Bone + Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs.Values.I32[3]);
        }

        return Convert(physical);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02RoadAppearance_cs");
}
