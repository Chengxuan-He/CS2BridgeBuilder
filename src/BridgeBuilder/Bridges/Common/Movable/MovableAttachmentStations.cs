using System;
using System.Collections.Generic;
using UnityEngine;
using Partition=BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
namespace BridgeBuilder.Bridges;

// Classify the original anchor against the measured road partition. Never move
// furniture to a nearby station: wires and their poles must keep one native layout.
internal static class MovableAttachmentStations
{
    // Intersect a longitudinal mounting row with the recorded partition planes.
    // This consumes authored motion domains; it does not infer seams from meshes.
    internal static float[] Ends(Vector3 row, Partition partition, Func<int, bool> moving, float clearance)
    {
        if (!float.IsFinite(clearance) || clearance <= 0f)
            throw new ArgumentException("Support clearance must be finite and positive.");
        var ranges = new List<(float Min, float Max)>();
        void Visit(Partition region, float min, float max)
        {
            if (min >= max || region.IsGap) return;
            if (region.Bone >= 0)
            {
                if (moving(region.Bone)) ranges.Add((min, max));
                return;
            }
            var origin = new Vector3(row.x, row.y, 0f);
            var offset = region.ObjectToSeam.MultiplyPoint3x4(origin).z;
            var slope = region.ObjectToSeam.m22;
            if (!float.IsFinite(offset) || !float.IsFinite(slope))
                throw new InvalidOperationException("Invalid support partition frame.");
            if (slope == 0f)
            {
                Visit(offset < 0f ? region.Negative! : region.Positive!, min, max);
                return;
            }
            var boundary = -offset / slope;
            Visit(slope > 0f ? region.Negative! : region.Positive!, min, Math.Min(max, boundary));
            Visit(slope > 0f ? region.Positive! : region.Negative!, Math.Max(min, boundary), max);
        }
        Visit(partition, float.NegativeInfinity, float.PositiveInfinity);
        if (ranges.Count == 0) return Array.Empty<float>();
        ranges.Sort((a, b) => a.Min.CompareTo(b.Min));
        var first = ranges[0];
        var last = ranges[ranges.Count - 1];
        var start = first.Min + clearance;
        var end = last.Max - clearance;
        if (!float.IsFinite(start) || !float.IsFinite(end) || start >= first.Max ||
            end <= last.Min || start >= end)
            throw new InvalidOperationException("Recorded moving span cannot contain end supports.");
        return new[] { start, end };
    }

    internal static bool Supported(Vector3 point, Partition partition)
    {
        var region=partition;
        while(region.Bone<0 && !region.IsGap)
        {
            var distance=region.ObjectToSeam.MultiplyPoint3x4(point).z;
            if(!float.IsFinite(distance)) throw new InvalidOperationException("Attachment mounting frame is not finite.");
            region=distance<0f?region.Negative!:region.Positive!;
        }
        return !region.IsGap;
    }
}
