using System;
using System.Collections.Generic;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Band = BridgeBuilder.Bridges.MovableExistingRoadSurface.RepeatBand;

namespace BridgeBuilder.Bridges;
// DrawBridge03 native albedo: lateral lane-period minima at rows 818 and 1194.
// UV origin is bottom-left. Columns 3084..3380 exclude the authored white dash.
// Native face 58054 spans 9.399904251098633 m over V=.10833740234375..385986328125.
internal static class Draw03RoadWear
{
    internal static Patch[] Apply(Patch[] patches, float[] centers, Matrix4x4 frame, Vector2 surfaceSpan)
    {
        if (centers.Length == Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[0])
            return patches;
        float period = Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[0];
        var min = new Vector2(Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[1] / Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[2], Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[3] / Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[4]);
        var max = new Vector2(Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[5] / Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[6], Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[7] / Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[8]);
        var left = centers[Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[1]] - period;
        var right = centers[centers.Length - Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[2]] + period;
        left = Mathf.Min(left,surfaceSpan.x);
        right = Mathf.Max(right,surfaceSpan.y);
        var width = right - left;
        var bands = new List<Band>();
        float cursor = left;
        void Gap(float end)
        {
            if (end <= cursor)
                return;
            // Use the measured dark asphalt interior (rows 802..834), not a
            // four-row sliver: repeating that sliver produces correlated streaks.
            // Keep the same native texels/metre and crop the final repetition.
            var crop = Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[9] / Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[10];
            var repeats = (end-cursor)*(max.y-min.y)/(period*crop);
            var bottom = min.y-crop*Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[11];
            // The final domain ends at the exact normalized boundary. Dividing
            // independently rounded world coordinates can otherwise produce
            // 0.9999999 and leave a false uncovered strip at an asymmetric edge.
            var normalizedEnd = end == right ? Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[17] : (end-left)/width;
            bands.Add(new Band((cursor-left)/width,normalizedEnd,new Vector2(min.x,bottom),
                new Vector2(max.x,bottom+crop),repeatY:repeats));
            cursor = end;
        }

        for (int i = Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[3]; i < centers.Length; i++)
        {
            float start = centers[i] - period * Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[11], end = centers[i] + period * Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[12];
            // Adjacent narrower lanes clip a native-width sample, never rescale it.
            if (i > Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[4])
                start = Mathf.Max(start, (centers[i - Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[5]] + centers[i]) * Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[13]);
            if (i + Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[6] < centers.Length)
                end = Mathf.Min(end, (centers[i] + centers[i + Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[7]]) * Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[14]);
            Gap(start);
            var lo = min.y + (start - (centers[i] - period * Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[15])) / period * (max.y - min.y);
            var hi = min.y + (end - (centers[i] - period * Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[16])) / period * (max.y - min.y);
            bands.Add(new Band((start - left) / width, (end - left) / width, new Vector2(min.x, lo), new Vector2(max.x, hi)));
            cursor = end;
        }

        Gap(right);
        var rows = bands.ToArray();
        for (int i = Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[8]; i < patches.Length; i++)
        {
            if (i % Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[9] >= Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[10])
                continue; // Authored sidewalk domains keep their existing material.
            var p = patches[i];
            var u = p.U * ((p.Max.x - p.Min.x) / (max.x - min.x));
            u.w += (p.Min.x - min.x) / (max.x - min.x);
            var v = frame * new Vector4(Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32[17] / width, Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[11], Numeric_Types_Draw03_Draw03RoadWear_cs.Values.I32[12], -left / width);
            patches[i] = new Patch(min, max, u, v, p.Surface, p.Repeat, rows, bandRows: true);
        }

        return patches;
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03RoadWear_cs
internal static class Numeric_Types_Draw03_Draw03RoadWear_cs
{
    static Numeric_Types_Draw03_Draw03RoadWear_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03RoadWear_cs");
}
