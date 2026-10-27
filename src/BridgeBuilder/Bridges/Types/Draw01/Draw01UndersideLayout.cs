using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Lateral extents of the evaluated selected road, not a prototype-width sidewalk assumption.
internal sealed class Draw01UndersideLayout
{
    // Central solid, left grating, right grating.
    internal readonly Vector2[] Spans;
    internal readonly int[] OccurrenceRoles;
    internal Draw01UndersideLayout(MovableClosedRoad.Result road, Pose roadToObject)
    {
        var ranges = new Vector2[road.Occurrences.Length];
        OccurrenceRoles = new int[ranges.Length];
        for (var i = Numeric_Types_Draw01_Draw01UndersideLayout_cs.Values.I32[0]; i < ranges.Length; i++)
            ranges[i] = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        foreach (var triangle in road.Triangles)
        {
            Add(triangle.Occurrence, triangle.A.Position);
            Add(triangle.Occurrence, triangle.B.Position);
            Add(triangle.Occurrence, triangle.C.Position);
        }

        var total = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        var left = total;
        var right = total;
        for (var i = Numeric_Types_Draw01_Draw01UndersideLayout_cs.Values.I32[1]; i < ranges.Length; i++)
        {
            var span = ranges[i];
            if (!(span.y > span.x))
                continue;
            Extend(ref total, span);
            var section = road.Occurrences[i].RootSection ?? throw new InvalidOperationException("Draw01 underside requires section provenance.");
            if (section.name != "Sidewalk" && !section.name.StartsWith("Sidewalk ", StringComparison.Ordinal))
                continue;
            OccurrenceRoles[i] = span.x + span.y < Numeric_Types_Draw01_Draw01UndersideLayout_cs.Values.F32[0] ? Numeric_Types_Draw01_Draw01UndersideLayout_cs.Values.I32[2] : Numeric_Types_Draw01_Draw01UndersideLayout_cs.Values.I32[3];
            if (span.x + span.y < Numeric_Types_Draw01_Draw01UndersideLayout_cs.Values.F32[1])
                Extend(ref left, span);
            else
                Extend(ref right, span);
        }

        var hasLeft = left.y > left.x;
        var hasRight = right.y > right.x;
        var solid = new Vector2(hasLeft ? left.y : total.x, hasRight ? right.x : total.y);
        if (!float.IsFinite(solid.x) || !float.IsFinite(solid.y) || solid.y <= solid.x)
            throw new InvalidOperationException("Draw01 underside has no valid non-sidewalk road span.");
        Spans = new[]
        {
            solid,
            hasLeft ? left : Vector2.zero,
            hasRight ? right : Vector2.zero
        };
        void Add(int occurrence, Vector3 position)
        {
            var x = (roadToObject.rotation * position + roadToObject.position).x;
            ranges[occurrence].x = Mathf.Min(ranges[occurrence].x, x);
            ranges[occurrence].y = Mathf.Max(ranges[occurrence].y, x);
        }
    }

    private static void Extend(ref Vector2 target, Vector2 span)
    {
        target.x = Mathf.Min(target.x, span.x);
        target.y = Mathf.Max(target.y, span.y);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01UndersideLayout_cs
internal static class Numeric_Types_Draw01_Draw01UndersideLayout_cs
{
    static Numeric_Types_Draw01_Draw01UndersideLayout_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01UndersideLayout_cs");
}
