using Game.Prefabs;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;

namespace BridgeBuilder.Bridges;
// Seven existing finite LOD2 bed charts, measured by BuildFarAppearance.py.
internal static class Lift04FarAppearance
{
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_Lift04_FarAppearance");
    internal static MovableRoadAppearance.Level Create(RenderPrefab native, Pose roadToObject, float halfDelta)
    {
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var patches = new Patch[Data.I32[Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[0]]];
        var support = new MovableRoadAppearance.Type[patches.Length];
        var regions = Partition.Gap;
        Partition Cut(float station, Partition negative, Partition positive)
        {
            var seam = Matrix4x4.identity;
            seam.m23 = -station;
            return new Partition(seam * pose, negative, positive);
        }

        for (var i = patches.Length - Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[1]; i >= Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[2]; i--)
        {
            var offset = i * Data.I32[Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[3]];
            float F(int n) => Data.F32[offset + n];
            // The authored full-width quad stretches between the same widened
            // shoulder boundaries. Longitudinal finite coordinates stay intact.
            var scale = F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[4]) / (F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[5]) + halfDelta);
            var u = pose.transpose * new Vector4(F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[6]) * scale, Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.F32[0], F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[7]), F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[8]));
            var v = pose.transpose * new Vector4(F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[9]) * scale, Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.F32[1], F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[10]), F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[11]));
            var tangent = Quaternion.Inverse(roadToObject.rotation) * new Vector3(F(13), F(14), F(15));
            patches[i] = new Patch(new Vector2(F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[12]), F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[13])), new Vector2(F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[14]), F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[15])), u, v, native.GetSurfaceAsset(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[16]), sourceUv: true,
                tangent: new Vector4(tangent.x, tangent.y, tangent.z, F(16)));
            support[i] = MovableRoadAppearance.Type.Ballast;
            regions = Cut(F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[17]), Partition.Gap, Cut(F(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[18]), new Partition(i), regions));
        }

        return new MovableRoadAppearance.Level(native.GetSurfaceAsset(Numeric_Types_Lift04_Lift04FarAppearance_cs.Values.I32[19]), patches, support, regions, regions);
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04FarAppearance_cs
internal static class Numeric_Types_Lift04_Lift04FarAppearance_cs
{
    static Numeric_Types_Lift04_Lift04FarAppearance_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04FarAppearance_cs");
}
