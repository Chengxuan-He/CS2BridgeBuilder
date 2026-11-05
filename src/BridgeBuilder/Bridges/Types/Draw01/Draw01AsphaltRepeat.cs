using BridgeBuilder.Runtime;
using UnityEngine;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;
// Authored pure prototype asphalt, shared by every running-surface domain/LOD.
// Physical partitions still control coverage and motion; they never reset grain.
internal static class Draw01AsphaltRepeat
{
    private static readonly float[] Data = HardcodedNumbers.Load("Types_Draw01_AsphaltRepeatMeasurements").F32;
    internal static float LongitudinalDensity => Data[1];
    internal static Vector4 NativeU(float direction, float offset)
        => new Vector4(direction * Data[0], 0, 0, offset * Data[0] + Data[2]);
    internal static MovableExistingRoadSurface.Patch[] Apply(MovableExistingRoadSurface.Patch[] patches, Type[] types, Pose roadToObject)
    {
        var result = (MovableExistingRoadSurface.Patch[])patches.Clone();
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var u = frame.transpose * new Vector4(Data[Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[0]], Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[1], Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[2], Data[Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[3]]);
        var v = frame.transpose * new Vector4(Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[4], Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[5], Data[Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[6]], Data[Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[7]]);
        for (int i = Numeric_Types_Draw01_Draw01AsphaltRepeat_cs.Values.I32[8]; i < result.Length; i++)
        {
            if (types[i] != Type.Asphalt)
                continue;
            result[i] = new MovableExistingRoadSurface.Patch(Vector2.zero, Vector2.one, u, v, BridgePreparedMaterials.RepeatUnit("Draw01", "87b16f614d85671f0ad2e65f113b218460b330780cbb8d6003a089d600139a16"), preparedUnit: true, repeatU: true, repeatV: true, projectTangent: true);
        }

        return result;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01AsphaltRepeat_cs
internal static class Numeric_Types_Draw01_Draw01AsphaltRepeat_cs
{
    static Numeric_Types_Draw01_Draw01AsphaltRepeat_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01AsphaltRepeat_cs");
}
