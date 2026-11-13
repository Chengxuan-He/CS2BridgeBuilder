using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Exact original central longitudinal girder and outer deck underside/closure faces.
// The far native shell represents these parts together and has no narrow girder.
internal static class Draw02Underside
{
    internal static MovableStructureModel.Plan Apply(int lod, MovableStructureModel.Plan plan,
        MovableSelectedRoadLayout layout)
    {
        if (lod == 2) return plan;
        var data = Numeric_Types_Draw02_Draw02Underside_cs.Values;
        var h = lod * 16;
        var positions = new Vector3[plan.SourcePositions.Length];
        Array.Copy(plan.Positions, positions, positions.Length);
        var removed = (bool[])plan.RemoveTriangles[0].Clone();
        // This is one narrow longitudinal section, not two independently moving
        // halves of the full under-deck assembly or a transverse width member.
        // Its adjoining original panel edges share this anchor. The panels'
        // outer edges still follow the displaced ribs, closing the original joint
        // without widening either the central girder or the longitudinal ribs.
        for (int i = 0; i < data.I32[h + 3]; i++)
        {
            int vertex = data.I32[data.I32[h + 2] + i];
            positions[vertex].x = plan.SourcePositions[vertex].x;
        }
        for (int side = 0; side < 2; side++)
        {
            var span = side == 0 ? layout.LeftSidewalk : layout.RightSidewalk;
            int row = h + 4 + side * 5;
            bool present = span.y > span.x;
            for (int i = 0; i < data.I32[row + 1]; i++)
                removed[data.I32[data.I32[row] + i]] = false;
            // These measured faces contain no upward road surface. They close
            // the outer deck even when its selected surface is a shoulder rather
            // than a sidewalk. Without a sidewalk retain the body's signed map;
            // a real sidewalk still supplies its independently measured span.
            if (!present) continue;
            float shift = span.x - data.F32[side + 1];
            float widthChange = span.y - span.x - data.F32[0];
            int count = data.I32[row + 3];
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.OriginDelta,
                new[] { plan.SourcePositions, positions },
                new[] { StructureRows.Slice(data.F32, data.I32[row + 4], count) },
                new[] { shift, widthChange }, count: count,
                indices: new[] { StructureRows.Slice(data.I32, data.I32[row + 2], count) });
        }
        // The original outer wall and stepped railing seat are structural,
        // including their upward faces. Road replacement must not cut them at
        // the road height or make their presence depend on a sidewalk.
        var restoredOuter = new HashSet<int>();
        var frames = new List<KeyValuePair<int, Matrix4x4>>(plan.FrameTransforms);
        var edges = new List<MovableStructureModel.EdgeVertex>(plan.AddedVertices);
        var replacements = new List<MovableStructureModel.Fragment>();
        var constructedX = new List<float>();
        for (int i = 0; i < plan.ConstructedCount; i++)
            constructedX.Add(plan.Positions[positions.Length + i].x);
        Draw02RailFootClosure.Apply(lod, plan, layout, positions, removed, restoredOuter, frames,
            edges, replacements, constructedX);
        for (int i = 0; i < data.I32[h + 15]; i++)
        {
            int face = data.I32[data.I32[h + 14] + i];
            removed[face] = false;
            restoredOuter.Add(face);
        }
        var fragments = new List<MovableStructureModel.Fragment>();
        foreach (var fragment in plan.Fragments)
            if (!restoredOuter.Contains(fragment.Triangle)) fragments.Add(fragment);
        fragments.AddRange(replacements);
        return new MovableStructureModel.Plan(plan.SourcePositions, positions, plan.SourceIndices,
            new[] { removed }, edges.ToArray(), fragments.ToArray(), plan.InteriorVertices, constructedX.ToArray(),
            uvCorrections: plan.UvCorrections, preparedDomains: plan.PreparedDomains,
            frameTransforms: frames.ToArray());
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02Underside_cs
internal static class Numeric_Types_Draw02_Draw02Underside_cs
{
    static Numeric_Types_Draw02_Draw02Underside_cs() { }
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02Underside_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02Underside_cs"));
}
