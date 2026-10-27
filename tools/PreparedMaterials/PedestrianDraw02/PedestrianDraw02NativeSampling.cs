using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// A complete affine receiver contained in one unreflected source period can
// retain the original Surface and every native VT/mip reference. No pixel copy,
// vertex modulo, triangle cuts or custom material is required.
internal static class PedestrianDraw02NativeSampling
{
    internal static bool TrySourcePeriod(MovableStructurePacking.Unit unit,
        out MovableStructurePacking.Unit original, out Vector2 phase)
    {
        original = null!;
        phase = default;
        if (unit.Points.Count == 0 || unit.Points.Count % 3 != 0) return false;
        var first = unit.Points[0];
        if (!float.IsFinite(first.x) || !float.IsFinite(first.y)) return false;
        phase = new Vector2(MathF.Floor(first.x), MathF.Floor(first.y));
        // Points are the coordinates actually emitted after ParentUv.Select.
        // Never restore its previous integer OffsetX/Y: doing so would apply
        // conditioning twice. A conditioned unit can already occupy period zero.
        if (phase == Vector2.zero && !unit.RepeatOnly) return false;
        var candidate = new MovableStructurePacking.Unit
        {
            Id = unit.Id, Triangle = unit.Triangle, Bounds = unit.Bounds,
            A = unit.A, B = unit.B, C = unit.C
        };
        foreach (var p in unit.Points)
        {
            // Use the same constant translation for every corner. Strict upper
            // bounds exclude wrap discontinuities; convex interpolation then
            // stays in the same native source region for the whole face.
            double x = (double)p.x - phase.x, y = (double)p.y - phase.y;
            if (!double.IsFinite(x) || !double.IsFinite(y) || x < 0 || y < 0 || x >= 1 || y >= 1 ||
                (unit.Triangle && x + y >= 1)) return false;
            var local = new Vector2((float)x, (float)y);
            // Do not erase a gradient while translating large Float32 phases.
            if ((double)local.x != x || (double)local.y != y) return false;
            candidate.Points.Add(local);
        }
        // Existing independent two-axis Float32 gradient verification applies
        // after returning to absolute atlas UVs, including tiny source faces.
        if (!candidate.CanUseSource) return false;
        original = candidate;
        return true;
    }
}
