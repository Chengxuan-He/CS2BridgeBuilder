using BridgeBuilder.Runtime;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// MaterialRegions identifies physical parts before any material substitution.
// All LODs use the same original-source density; the fixed concrete band is not
// an asphalt override merely because it receives selected carriageway geometry.
internal static class Draw01PartRepeats
{
    internal static MovableExistingRoadSurface.Patch[] Apply(MovableExistingRoadSurface.Patch[] source, Pose roadToObject)
    {
        var result = Draw01AsphaltRepeat.Apply(source, Draw01RoadAppearance.SupportedTypes, roadToObject);
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).transpose;
        for (int i = Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[0]; i < result.Length; i++)
        {
            int part = i % Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[1];
            if (part == Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[2] || part == Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[3])
                continue;
            bool grating = part == Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[4] || part == Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[5];
            bool platform = part == Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[6];
            string key = grating ? "2c21be8e861d7f307eafd18a7f6563365ad81e386bc5f76f80fcd4b70f866902" : platform ? "646ce47f8b33867ff5d17c39bd5f298a5f903bde264dfd658f2a7885d90e7acc" : "abd357e2483159049ec72564fef01ecb06fc1fdcc8dcad501a5506c65fa37e98";
            float periodX = grating ? Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[0] : platform ? Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[1] : Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[2];
            // Two exact native four-metre panels make the 914-row source BC7-aligned.
            float periodZ = grating ? Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[3] : Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[4];
            var u = frame * new Vector4((grating ? -Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[5] : Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[6]) / periodX, Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[7], Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[8], Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[9]);
            var v = frame * new Vector4(Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[10], Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[11], -Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[7] / periodZ, Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[12]);
            // MaterialRegions orders the positive leaf before the mirrored negative leaf.
            // Preserve the authored panel edge phase; width changes never stretch the wire.
            if (grating)
                v = frame * new Vector4(0f, 0f, (i < Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.I32[1] ? -1f : 1f) / periodZ, Numeric_Types_Draw01_Draw01PartRepeats_cs.Values.F32[8] / periodZ);
            result[i] = new MovableExistingRoadSurface.Patch(Vector2.zero, Vector2.one, u, v, BridgePreparedMaterials.RepeatUnit("Draw01", key), preparedUnit: true, repeatU: true, repeatV: true, projectTangent: true);
        }

        return result;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01PartRepeats_cs
internal static class Numeric_Types_Draw01_Draw01PartRepeats_cs
{
    static Numeric_Types_Draw01_Draw01PartRepeats_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01PartRepeats_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01PartRepeats_cs"));
}
