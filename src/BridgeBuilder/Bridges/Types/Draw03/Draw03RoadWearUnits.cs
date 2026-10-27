using System;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;

namespace BridgeBuilder.Bridges;
// Reduces only an existing triangle wholly inside one authored wear band.
// It changes the affine sampling map, never positions, indices or bone ownership.
internal static class Draw03RoadWearUnits
{
    internal static bool TryReduce(Patch patch, Vector3 a, Vector3 b, Vector3 c, out Patch reduced)
    {
        reduced = patch;
        if (!patch.Project || !patch.Repeat || !patch.BandRows || patch.Bands == null)
            return false;
        float Sample(Vector3 point) => Vector3.Dot(point, patch.V) + patch.V.w;
        float va = Sample(a), vb = Sample(b), vc = Sample(c);
        if (!float.IsFinite(va) || !float.IsFinite(vb) || !float.IsFinite(vc))
            throw new InvalidOperationException("Draw03 wear projection must be finite.");
        float low = Mathf.Min(va, Mathf.Min(vb, vc));
        float high = Mathf.Max(va, Mathf.Max(vb, vc));
        float period = Mathf.Floor(low);
        // Do not extrapolate through frac(), or assign a boundary vertex to an
        // adjacent band. The common path handles those triangles unchanged.
        if (high >= period + Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.F32[0])
            return false;
        low -= period;
        high -= period;
        for (int i = 0; i < patch.Bands.Length; i++)
        {
            var band = patch.Bands[i];
            if (low < band.Start || high >= band.End)
                continue;
            return TryBand(patch, i, period, out reduced);
        }
        return false;
    }

    // Domain ownership is already established by the appearance partition. Its
    // shared boundary vertices must not be reclassified into an adjacent band.
    internal static Patch ForBand(Patch patch, int index, float period = 0f)
    {
        if (!TryBand(patch, index, period, out var reduced))
            throw new InvalidOperationException("Draw03 wear domain is not a measured lane or gap unit.");
        return reduced;
    }

    private static bool TryBand(Patch patch, int index, float period, out Patch reduced)
    {
        reduced = patch;
        if (!patch.Project || !patch.Repeat || !patch.BandRows || patch.Bands == null)
            return false;
        var f = Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32;
        var laneMin = new Vector2(f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[0]] / f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[1]], f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[2]] / f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[3]]);
        var laneMax = new Vector2(f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[4]] / f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[5]], f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[6]] / f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[7]]);
        float gapHeight = f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[8]] / f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[9]];
        float gapBottom = laneMin.y - gapHeight * f[Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.I32[10]];
        if (!patch.Min.Equals(laneMin) || !patch.Max.Equals(laneMax))
            return false;
        var band = patch.Bands[index];
        if (band.Min.x != laneMin.x || band.Max.x != laneMax.x)
            return false;
        Vector2 min = laneMin, max = laneMax;
        float scale, offset;
        if (band.Min.y == gapBottom && band.Max.y == gapBottom + gapHeight)
        {
            min.y = gapBottom;
            max.y = gapBottom + gapHeight;
            scale = band.RepeatY / (band.End - band.Start);
            offset = Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.F32[1];
        }
        else if (band.RepeatY == Numeric_Types_Draw03_Draw03RoadWearUnits_cs.Values.F32[2] && band.Min.y >= laneMin.y && band.Max.y <= laneMax.y)
        {
            scale = (band.Max.y - band.Min.y) / (laneMax.y - laneMin.y) / (band.End - band.Start);
            offset = (band.Min.y - laneMin.y) / (laneMax.y - laneMin.y);
        }
        else
            return false;
        var v = patch.V * scale;
        v.w += offset - (period + band.Start) * scale;
        reduced = new Patch(min, max, patch.U, v, patch.Surface, repeat: true);
        return true;
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03RoadWearUnits_cs
internal static class Numeric_Types_Draw03_Draw03RoadWearUnits_cs
{
    static Numeric_Types_Draw03_Draw03RoadWearUnits_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03RoadWearUnits_cs");
}
