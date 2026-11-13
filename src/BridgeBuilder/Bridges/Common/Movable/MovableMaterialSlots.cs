using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBuilder.Bridges;

// Multiple road occurrences/decks may carry separate slots for the exact same
// borrowed Surface. Use its first slot for batching, without comparing names,
// texture contents or CIDs and without changing triangle occurrence/bone data.
internal static class MovableMaterialSlots
{
    // Independent native renderers select their own LOD. Keep the entire selected
    // road (including sidewalks) together when its complete graph fits, rather
    // than moving a road part between renderers as structural slot counts change.
    internal static int[][][]? RoadRenderGroups(IReadOnlyList<int> totals, IReadOnlyList<int> roadStarts)
    {
        if (totals.Sum() <= 16 || roadStarts.Sum() > 16 || totals.Sum() - roadStarts.Sum() > 16 ||
            Enumerable.Range(0, totals.Count).Any(i => roadStarts[i] == 0 || roadStarts[i] == totals[i]))
            return null;
        return new[] {
            roadStarts.Select(count => Enumerable.Range(0, count).ToArray()).ToArray(),
            Enumerable.Range(0, totals.Count).Select(i => Enumerable.Range(roadStarts[i], totals[i]-roadStarts[i]).ToArray()).ToArray()
        };
    }

    // A far structural supplement can replace geometry inside the near main mesh.
    // Keep the complete structural prefix together even when the road needs more
    // than one renderer. Supplemental slot ordinals are not physical part identities.
    internal static int[][][]? StructureRoadRenderGroups(int[][] surfaces, IReadOnlyList<int> roadStarts)
    {
        if (surfaces.Sum(x => x.Length) <= 16 || roadStarts.Sum() > 16 ||
            Enumerable.Range(0, surfaces.Length).Any(i => roadStarts[i] == 0 || roadStarts[i] == surfaces[i].Length))
            return null;
        var roles = surfaces.Select((row, lod) => Enumerable.Range(0, row.Length)
            .Select(slot => slot < roadStarts[lod] ? 0 : 1).ToArray()).ToArray();
        return StableRoleGroups(roles, surfaces);
    }

    // Role identifiers come from assembly provenance, not slot positions or material
    // names: original structure, each supplemental occurrence, selected road and lanes.
    // A shared Surface only groups slots within a role; it never merges their geometry.
    internal static int[][][]? StableRoleGroups(int[][] roles, int[][] surfaces)
    {
        var bundles = new List<int[][]>();
        foreach (int role in roles.SelectMany(x => x).Distinct().OrderBy(x => x))
        {
            var slots = roles.Select(r => Enumerable.Range(0, r.Length).Where(i => r[i] == role).ToArray()).ToArray();
            if (slots.Any(x => x.Length == 0)) return null;
            if (slots.Sum(x => x.Length) <= 16) { bundles.Add(slots); continue; }
            var common = slots[0].Select(i => surfaces[0][i]).Distinct()
                .Where(key => Enumerable.Range(1, roles.Length - 1).All(lod => slots[lod].Any(i => surfaces[lod][i] == key))).ToArray();
            var residual = slots.Select((row, lod) => row.Where(i => !common.Contains(surfaces[lod][i])).ToArray()).ToArray();
            var complete = common.Select(key => slots.Select((row, lod) => row.Where(i => surfaces[lod][i] == key).ToArray()).ToArray()).ToList();
            if (complete.Any(b => b.Sum(x => x.Length) > 16)) return null;
            if (residual.Any(x => x.Length != 0))
            {
                // Do not fabricate empty native LODs. Add complete bundles to a
                // residual with missing levels, while retaining each slot once.
                while (residual.Any(x => x.Length == 0) && complete.Count != 0)
                {
                    var next = complete[0]; complete.RemoveAt(0);
                    residual = residual.Select((row, lod) => row.Concat(next[lod]).ToArray()).ToArray();
                }
                if (residual.Any(x => x.Length == 0) || residual.Sum(x => x.Length) > 16) return null;
                bundles.Add(residual);
            }
            bundles.AddRange(complete);
        }
        // Pack whole complete semantic bundles. Never slice one bundle differently
        // per LOD as the old balanced-count fallback does.
        var groups = new List<int[][]>();
        foreach (var bundle in bundles)
        {
            int size = bundle.Sum(x => x.Length);
            var target = groups.FirstOrDefault(g => g.Sum(x => x.Length) + size <= 16);
            if (target == null) groups.Add(bundle.Select(x => x.ToArray()).ToArray());
            else for (int lod = 0; lod < roles.Length; lod++) target[lod] = target[lod].Concat(bundle[lod]).ToArray();
        }
        return groups.ToArray();
    }

    // Supply one nonempty native stream per LOD/group. The margin accounts for
    // independent ceiling in each LOD's balanced group slices.
    internal static int MinimumLodSlots(IReadOnlyList<int> totals)
    {
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

    internal static int[] Canonical<T>(IReadOnlyList<T> surfaces, IEnumerable<int>? usedSlots = null) where T : class
    {
        var result = Enumerable.Repeat(-1, surfaces.Count).ToArray();
        var slots = (usedSlots ?? Enumerable.Range(0, surfaces.Count)).Distinct().OrderBy(slot => slot).ToArray();
        foreach (var slot in slots)
        {
            result[slot] = slot;
            for (var previous = 0; previous < slot; previous++)
                if (result[previous] >= 0 && ReferenceEquals(surfaces[slot], surfaces[previous]))
                {
                    result[slot] = result[previous];
                    break;
                }
        }
        return result;
    }
}
