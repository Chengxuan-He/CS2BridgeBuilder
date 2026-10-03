using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal static partial class TowerWidening
{
    /// <summary>
    /// Rebuilds the golden top's centred vertical spoke as a constant-width rectangle.
    ///
    /// The golden bridge's top fan is openwork. At some sampled heights the centre spoke stands by
    /// itself, at others it stands beside ribs or the arch, so a height-by-height span gives its two
    /// sides different scale factors and turns it into an hourglass or a pair of broad diamonds.
    ///
    /// It is a column on the centre line: widening the bridge changes neither its centre nor its
    /// thickness. The member is therefore found as topology, not as every vertex that happens to pass
    /// through a narrow x band. That distinction matters at the tips of the neighbouring fan ribs:
    /// their vertices enter the same band, but their triangles run far outside it. Only the narrow,
    /// centred connected component with the greatest vertical reach is rebuilt, and every one of its
    /// non-centre vertices is put on one of two parallel sides at the component's authored half-width.
    /// </summary>
    internal static int RectangularizeCentralSpoke(
        float3[] source,
        float3[] moved,
        IReadOnlyList<int>? triangles,
        out float halfWidth,
        out float scale)
    {
        halfWidth = 0f;
        scale = 1f;
        if (source.Length == 0 || source.Length != moved.Length || triangles == null) return 0;

        var outer = 0f;
        var low = float.MaxValue;
        var high = float.MinValue;
        foreach (var vertex in source)
        {
            outer = Math.Max(outer, Math.Abs(vertex.x));
            low = Math.Min(low, vertex.y);
            high = Math.Max(high, vertex.y);
        }

        if (outer <= CentreEpsilon || high <= low) return 0;

        var limit = outer * 0.1f;
        var parent = new int[source.Length];
        var used = new bool[source.Length];
        for (var index = 0; index < parent.Length; index++) parent[index] = index;

        int Root(int of)
        {
            while (parent[of] != of)
            {
                parent[of] = parent[parent[of]];
                of = parent[of];
            }

            return of;
        }

        void Join(int one, int two)
        {
            var first = Root(one);
            var second = Root(two);
            if (first != second) parent[first] = second;
        }

        // Build topology only from triangles wholly inside the centre band. A fan rib's tip can sit
        // inside this band, but the other two corners of its triangle plainly identify it as a rib.
        // Joining the whole mesh first joins the spoke to the arch at their shared bottom edge and
        // makes the complete fan look like one wide component.
        for (var corner = 0; corner + 2 < triangles.Count; corner += 3)
        {
            var a = triangles[corner];
            var b = triangles[corner + 1];
            var c = triangles[corner + 2];
            if (a < 0 || b < 0 || c < 0) continue;
            if (a >= source.Length || b >= source.Length || c >= source.Length) continue;
            if (Math.Abs(source[a].x) > limit
                || Math.Abs(source[b].x) > limit
                || Math.Abs(source[c].x) > limit)
                continue;

            used[a] = true;
            used[b] = true;
            used[c] = true;
            Join(a, b);
            Join(b, c);
        }

        // A rendered mesh normally duplicates a position at hard edges. Weld only copies which
        // belong to the centre-band faces; a duplicate belonging solely to a fan rib stays out.
        var welded = new Dictionary<long, int>();
        for (var index = 0; index < source.Length; index++)
        {
            if (!used[index]) continue;
            var key = WeldKey(source[index]);
            if (welded.TryGetValue(key, out var first)) Join(index, first);
            else welded[key] = index;
        }

        var components = new Dictionary<int, (float Low, float High, int Signs)>();
        for (var index = 0; index < source.Length; index++)
        {
            if (!used[index]) continue;
            var root = Root(index);
            var vertex = source[index];
            if (!components.TryGetValue(root, out var component))
                component = (vertex.y, vertex.y, 0);

            component.Low = Math.Min(component.Low, vertex.y);
            component.High = Math.Max(component.High, vertex.y);
            component.Signs |= vertex.x < -CentreEpsilon ? 1
                : vertex.x > CentreEpsilon ? 2
                : 4;
            components[root] = component;
        }

        var chosen = -1;
        var greatestSpan = 0f;
        foreach (var entry in components)
        {
            var component = entry.Value;
            var crossesCentre = (component.Signs & 3) == 3 || (component.Signs & 4) != 0;
            if (!crossesCentre) continue;

            var verticalSpan = component.High - component.Low;
            if (verticalSpan <= greatestSpan) continue;

            chosen = entry.Key;
            greatestSpan = verticalSpan;
        }

        if (chosen < 0 || greatestSpan < Math.Max(1f, (high - low) * 0.05f)) return 0;

        // The longest pair of parallel vertical edges within the chosen centre component are the
        // spoke's sides. Short runs at the bottom belong to the arch which the spoke lands on.
        const float bucket = 0.05f;
        var candidates = new Dictionary<int, (float Low, float High, int Signs)>();
        for (var index = 0; index < source.Length; index++)
        {
            if (!used[index] || Root(index) != chosen) continue;
            var vertex = source[index];
            var distance = Math.Abs(vertex.x);
            if (distance <= bucket) continue;

            var slot = (int)Math.Round(distance / bucket);
            candidates.TryGetValue(slot, out var candidate);
            if (candidate.Signs == 0) candidate = (vertex.y, vertex.y, 0);
            candidate.Low = Math.Min(candidate.Low, vertex.y);
            candidate.High = Math.Max(candidate.High, vertex.y);
            candidate.Signs |= vertex.x < 0f ? 1 : 2;
            candidates[slot] = candidate;
        }

        var memberLow = 0f;
        var memberHigh = 0f;
        var memberSpan = 0f;
        var chosenSide = -1;
        foreach (var entry in candidates)
        {
            if (entry.Value.Signs != 3) continue;
            var verticalSpan = entry.Value.High - entry.Value.Low;
            if (verticalSpan < memberSpan - bucket) continue;
            if (verticalSpan > memberSpan + bucket || entry.Key > chosenSide)
            {
                chosenSide = entry.Key;
                memberLow = entry.Value.Low;
                memberHigh = entry.Value.High;
                memberSpan = verticalSpan;
            }
        }

        if (chosenSide < 0 || memberSpan < Math.Max(1f, greatestSpan * 0.5f)) return 0;
        halfWidth = chosenSide * bucket;
        var memberLimit = halfWidth + Math.Max(bucket, halfWidth * 0.15f);

        // No scale is deliberate. This is the thickness of a vertical column, not the span between
        // the tower's legs. Widening the tower must not turn a 2.7 m column into the 12.9 m member the
        // previous pass reported on a 64 m road.
        scale = 1f;
        var changed = 0;
        for (var index = 0; index < source.Length; index++)
        {
            var vertex = source[index];
            if (!used[index] || Root(index) != chosen) continue;
            if (vertex.y < memberLow - bucket || vertex.y > memberHigh + bucket) continue;
            if (Math.Abs(vertex.x) <= CentreEpsilon || Math.Abs(vertex.x) > memberLimit) continue;

            var corrected = vertex.x < 0f ? -halfWidth : halfWidth;
            if (Math.Abs(moved[index].x - corrected) <= CentreEpsilon) continue;
            moved[index].x = corrected;
            changed++;
        }

        return changed;
    }

}
