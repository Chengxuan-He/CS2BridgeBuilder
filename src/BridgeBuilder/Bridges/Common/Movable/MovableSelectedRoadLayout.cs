using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Evaluated selected-road dimensions in mechanism space; never prototype dimensions.
internal sealed class MovableSelectedRoadLayout
{
    internal readonly bool[] SidewalkOccurrences;
    internal readonly Vector2 LeftSidewalk, RightSidewalk;

    internal MovableSelectedRoadLayout(MovableClosedRoad.Result road, Pose roadToObject)
    {
        SidewalkOccurrences = new bool[road.Occurrences.Length];
        var spans = new Vector2[road.Occurrences.Length];
        for (var i = 0; i < spans.Length; i++)
        {
            var section = road.Occurrences[i].RootSection
                ?? throw new InvalidOperationException("Moving road layout requires source section provenance.");
            SidewalkOccurrences[i] = section.name == "Sidewalk" ||
                section.name.StartsWith("Sidewalk ", StringComparison.Ordinal);
            spans[i] = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        }
        foreach (var triangle in road.Triangles)
        {
            Add(triangle.Occurrence, triangle.A.Position);
            Add(triangle.Occurrence, triangle.B.Position);
            Add(triangle.Occurrence, triangle.C.Position);
        }
        var left = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        var right = left;
        for (var i = 0; i < spans.Length; i++)
        {
            var span = spans[i];
            if (!SidewalkOccurrences[i] || !(span.y > span.x)) continue;
            if (span.x + span.y < 0f) Extend(ref left, span);
            else Extend(ref right, span);
        }
        LeftSidewalk = left.y > left.x ? left : Vector2.zero;
        RightSidewalk = right.y > right.x ? right : Vector2.zero;

        void Add(int occurrence, Vector3 position)
        {
            var x = (roadToObject.rotation * position + roadToObject.position).x;
            spans[occurrence].x = Mathf.Min(spans[occurrence].x, x);
            spans[occurrence].y = Mathf.Max(spans[occurrence].y, x);
        }
    }

    private static void Extend(ref Vector2 target, Vector2 span)
    { target.x = Mathf.Min(target.x, span.x); target.y = Mathf.Max(target.y, span.y); }
}
