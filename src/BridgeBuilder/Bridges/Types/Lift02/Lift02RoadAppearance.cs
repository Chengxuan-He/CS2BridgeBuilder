using UnityEngine;
using Game.Prefabs;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Measured LiftBridge02 mesh 0 roadbed top: 212 faces, starting at 83248.
// Original 4096px atlas, 4m longitudinal unit and 0.42156982421875m lateral strip.
internal static class Lift02RoadAppearance
{
    // The native TrainTrack deck has rails/sleepers and two narrow structural
    // roadbed strips, not an asphalt, sidewalk or wheel-track appearance. A
    // roadbed atlas rectangle is not permission to replace a selected road type.
    // Verified TrainSubwayTrack bed receivers use Ballast in both input layers;
    // the prototype supplies its own bed appearance. Rails/sleepers are separate.
    internal static MovableRoadAppearance.Level ForLod(int lod, RenderPrefab source, Pose roadToObject, float halfDelta)
    {
        if (lod == 2)
            return Lift02BedAppearance.Far(source, roadToObject, halfDelta);
        return Lift02BedAppearance.Detailed(source,roadToObject,halfDelta);
    }

    // Retained measured legacy recipe; the explicit support map above does not
    // authorize its transverse repeat for selected-road composition.
    internal static Patch[] Patches(Pose roadToObject)
    {
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        var min = new Vector2(Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[0], Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[1]);
        var max = new Vector2(Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[2], Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[3]);
        var result = new Patch[Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.I32[0]];
        for (var side = Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.I32[1]; side < Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.I32[2]; side++)
        {
            var sign = side == Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.I32[3] ? Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[4] : -Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[5];
            result[side] = new Patch(min, max, frame * new Vector4(Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[6], Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[7], -Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[8], Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[9]), frame * new Vector4(sign / Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[10], Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[11], Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[12], Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[13] / Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[14]), repeat: true);
        }

        return result;
    }

    internal static Partition Regions(Pose roadToObject)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var side = Matrix4x4.identity;
        side.m00 = Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[15];
        side.m02 = -Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[16];
        side.m20 = Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[17];
        side.m22 = Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.F32[18];
        Partition Convert(Partition node)
        {
            if (node.IsGap)
                return Partition.Gap;
            if (node.Negative != null && node.Positive != null)
                return new Partition(node.ObjectToSeam * pose, Convert(node.Negative), Convert(node.Positive));
            return new Partition(side * pose, new Partition(Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.I32[4]), new Partition(Numeric_Types_Lift02_Lift02RoadAppearance_cs.Values.I32[5]));
        }

        return Convert(Lift02RoadPartitions.Carriageway);
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02RoadAppearance_cs
internal static class Numeric_Types_Lift02_Lift02RoadAppearance_cs
{
    static Numeric_Types_Lift02_Lift02RoadAppearance_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift02_Lift02RoadAppearance_cs");
}
