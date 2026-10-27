using System;
using System.Collections.Generic;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Actual lane/gap appearance boundaries, not texture-period subdivision. The
// common road clipper preserves attributes; original physical trees own bones.
internal static class Draw03RoadWearDomains
{
    internal static Patch[] Apply(Patch[] source, ref Partition carriageway, ref Partition sidewalk)
    {
        var patches = new List<Patch>(source);
        var domains = new Dictionary<int, Partition>();
        Partition Expand(Partition node)
        {
            if (node.IsGap)
                return node;
            if (node.Bone < Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[0])
                return new Partition(node.ObjectToSeam, Expand(node.Negative!), Expand(node.Positive!));
            var patch = source[node.Bone];
            if (patch.Bands == null)
                return node;
            if (domains.TryGetValue(node.Bone, out var existing))
                return existing;
            var bands = patch.Bands;
            var first = patches.Count;
            float previous = Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.F32[0];
            for (int i = Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[1]; i < bands.Length; i++)
            {
                if (bands[i].Start != previous || bands[i].End <= previous)
                    throw new InvalidOperationException("Draw03 appearance bands must be contiguous.");
                patches.Add(Draw03RoadWearUnits.ForBand(patch, i));
                previous = bands[i].End;
            }

            if (bands.Length == Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[2] || previous != Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.F32[1])
                throw new InvalidOperationException("Draw03 appearance bands must cover the selected surface.");
            Partition Build(int start, int end)
            {
                if (end - start == Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[3])
                    return new Partition(first + start);
                int middle = start + (end - start) / Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[4];
                var frame = Matrix4x4.identity;
                // Source patches and appearance trees are both in selected-road
                // coordinates. The seam is exactly V(position)-band.End == 0.
                frame.SetRow(Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[5], patch.U);
                var normal = Vector3.Cross((Vector3)patch.V, (Vector3)patch.U);
                frame.SetRow(Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[6], new Vector4(normal.x, normal.y, normal.z, Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.F32[2]));
                var seam = patch.V;
                seam.w -= bands[middle - Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[7]].End;
                frame.SetRow(Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[8], seam);
                return new Partition(frame, Build(start, middle), Build(middle, end));
            }

            var result = Build(Numeric_Types_Draw03_Draw03RoadWearDomains_cs.Values.I32[9], bands.Length);
            domains.Add(node.Bone, result);
            return result;
        }

        carriageway = Expand(carriageway);
        sidewalk = Expand(sidewalk);
        return patches.ToArray();
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03RoadWearDomains_cs
internal static class Numeric_Types_Draw03_Draw03RoadWearDomains_cs
{
    static Numeric_Types_Draw03_Draw03RoadWearDomains_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03RoadWearDomains_cs");
}
