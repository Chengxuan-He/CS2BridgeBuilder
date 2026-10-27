using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBuilder.Bridges;

// Multiple road occurrences/decks may carry separate slots for the exact same
// borrowed Surface. Use its first slot for batching, without comparing names,
// texture contents or CIDs and without changing triangle occurrence/bone data.
internal static class MovableMaterialSlots
{
    // Supply one nonempty native stream per LOD/group. The margin accounts for
    // independent ceiling in each LOD's balanced group slices.
    internal static int MinimumLodSlots(IReadOnlyList<int> totals)
    {
        if(totals.Count==0 || totals.Count>16 || totals.Any(n=>n<=0))
            throw new ArgumentException("Native LOD grouping requires 1..16 nonempty levels.");
        if(totals.Sum()<=16)return 1;
        for(int groups=(totals.Sum()+15)/16;groups<=totals.Max();groups++)
        {
            bool valid=true;
            for(int group=0;group<groups && valid;group++)
                valid=totals.Sum(count=>
                {
                    long n=Math.Max(count,groups);
                    return (int)(n*(group+1)/groups-n*group/groups);
                })<=16;
            if(valid)return groups;
        }
        throw new InvalidOperationException("No finite complete native LOD group partition.");
    }
    // Each entry is a complete native LOD group; each LOD lists original model
    // slots. Nothing is merged, dropped, resampled or duplicated by this plan.
    internal static int[][][] RenderGroups(IReadOnlyList<int> totals, IReadOnlyList<int> laneStarts)
    {
        if (totals.Count == 0 || totals.Count != laneStarts.Count ||
            totals.Any(count => count <= 0) ||
            Enumerable.Range(0, totals.Count).Any(i => laneStarts[i] < 0 || laneStarts[i] > totals[i]))
            throw new ArgumentException("Batch planning requires nonempty complete LODs and valid lane boundaries.");
        int total = totals.Sum();
        if (total <= 16)
            return new[] { totals.Select(count => Enumerable.Range(0, count).ToArray()).ToArray() };
        int structure = laneStarts.Sum();
        // Preserve the established structure/track split wherever it fits.
        if (structure <= 16 && total - structure <= 16 &&
            Enumerable.Range(0, totals.Count).All(i => laneStarts[i] > 0 && laneStarts[i] < totals[i]))
            return new[] {
                laneStarts.Select(count => Enumerable.Range(0, count).ToArray()).ToArray(),
                Enumerable.Range(0, totals.Count).Select(i => Enumerable.Range(laneStarts[i], totals[i]-laneStarts[i]).ToArray()).ToArray()
            };
        // More appearance units need more native groups, not smaller textures.
        // Balanced contiguous slices retain every original slot exactly once.
        // Empty LOD payloads have not been validated in the native renderer, so
        // do not fabricate them or copy a near mesh into a missing far level.
        for (int groups = (total + 15) / 16; groups <= totals.Min(); groups++)
        {
            var result = Enumerable.Range(0, groups).Select(group => totals.Select(count =>
            {
                int first = (int)((long)count * group / groups);
                int end = (int)((long)count * (group + 1) / groups);
                return Enumerable.Range(first, end - first).ToArray();
            }).ToArray()).ToArray();
            if (result.All(group => group.Sum(slots => slots.Length) <= 16)) return result;
        }
        throw new InvalidOperationException($"Cannot distribute {total} material batches across complete nonempty native LOD groups of at most 16 without changing mesh topology.");
    }

    internal static int[] Canonical<T>(IReadOnlyList<T> surfaces) where T : class
    {
        var result = new int[surfaces.Count];
        for (var slot = 0; slot < surfaces.Count; slot++)
        {
            if (surfaces[slot] == null)
                throw new ArgumentException("Moving geometry has an unresolved surface slot.", nameof(surfaces));
            result[slot] = slot;
            for (var previous = 0; previous < slot; previous++)
                if (ReferenceEquals(surfaces[slot], surfaces[previous]))
                {
                    result[slot] = result[previous];
                    break;
                }
        }
        return result;
    }
}
