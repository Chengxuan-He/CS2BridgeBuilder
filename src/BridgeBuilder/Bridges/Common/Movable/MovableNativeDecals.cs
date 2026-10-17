using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Evaluates paint footprints without allocating any decal object. Curve subdivision and
// endpoint-fade differences were explicitly accepted by the user on 2026-10-16.
internal static class MovableNativeDecals
{
    internal readonly struct Region
    {
        internal readonly Vector3 Center, Right, Up, Forward, Half;
        internal readonly bool Yellow;
        internal Region(Vector3 center, Quaternion rotation, Vector3 half, bool yellow)
        { Center = center; Right = rotation * Vector3.right; Up = rotation * Vector3.up;
          Forward = rotation * Vector3.forward; Half = half; Yellow = yellow; }
    }
    internal static List<Region> Regions(MovableLaneVisuals.Batch batch, Pose roadToObject, float texelsPerMeter)
    {
        var result = new List<Region>();
        if (!batch.Decal || batch.Surfaces.Length != 1 || !(texelsPerMeter > 0f))
            throw new InvalidOperationException("Native marking conversion requires one decal surface and positive density.");
        var source = batch.Surfaces[0];
        source.LoadHeader();
        if (source.materialTemplateHash is not (19 or 20 or 32))
            throw new NotSupportedException($"Unmeasured native decal template {source.materialTemplateHash}: {source.name}.");
        // Deterioration and pavement/rail-bed blends belong to road appearance. The
        // selected existing bridge surface now supplies that appearance directly;
        // they are not paint and must not become full-width white decal ribbons.
        if (source.materialTemplateHash == 19 || source.name.StartsWith("RoadTransition", StringComparison.Ordinal)
            || source.name.StartsWith("TiledTransition", StringComparison.Ordinal)
            || source.name.StartsWith("GravelLane", StringComparison.Ordinal)
            || source.name.StartsWith("TrainRails", StringComparison.Ordinal)
            || source.name.StartsWith("TramRailsBase", StringComparison.Ordinal)
            || source.name.StartsWith("SubwayRailsBase", StringComparison.Ordinal)) return result;
        var curve = batch.CurveMatrix;
        var segments = new List<(Vector4 A, Vector4 D)>();
        Split(curve.GetColumn(0), curve.GetColumn(1), curve.GetColumn(2), curve.GetColumn(3), 0);
        var original = batch.Source.GetComponent<DecalProperties>();
        foreach (var segment in segments.SelectMany(s => MovableExistingMarkings.Strokes(source, s.A, s.D, original.m_TextureArea)))
        {
            var direction = (Vector3)(segment.D - segment.A);
            var length = direction.magnitude;
            if (!(length > 0f)) throw new InvalidOperationException("Native marking curve has a zero-length segment.");
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            var inverse = Quaternion.Inverse(rotation);
            var progress = (segment.A.w + segment.D.w) * .5f / curve.GetColumn(3).w;
            var width = batch.Size.x * Mathf.Lerp(batch.Scale.x, batch.Scale.z, progress);
            var fullWidth = width;
            width *= segment.Width;
            var height = batch.Size.y;
            if (!(width > 0f) || !(height > 0f)) throw new InvalidOperationException("Invalid native decal dimensions.");
            var center = (Vector3)batch.Transform.GetColumn(3)
                + (Vector3)(segment.A + segment.D) * .5f + Vector3.up * batch.Size.w
                + rotation * Vector3.right * (fullWidth * segment.Offset);
            result.Add(new Region(roadToObject.position + roadToObject.rotation * center,
                roadToObject.rotation * rotation, new Vector3(width, height, length) * .5f, segment.Yellow));
        }
        return result;

        void Split(Vector4 a, Vector4 b, Vector4 c, Vector4 d, int depth)
        {
            var chord = (Vector3)(d - a);
            var length = chord.magnitude;
            var error = length > 0f ? Mathf.Max(Vector3.Cross(chord, (Vector3)(b - a)).magnitude,
                Vector3.Cross(chord, (Vector3)(c - a)).magnitude) / length : float.PositiveInfinity;
            var widthChange = Mathf.Abs(batch.Size.x * (batch.Scale.z - batch.Scale.x)
                * (d.w - a.w) / curve.GetColumn(3).w);
            // Spatial and width approximation are bounded by the existing road sampling
            // density. This does not allocate textures or classify mechanism structure.
            if (error <= 1f / texelsPerMeter && widthChange <= 1f / texelsPerMeter)
            { segments.Add((a, d)); return; }
            if (depth == 16) throw new InvalidOperationException("Native marking curve cannot be subdivided to the requested density.");
            var ab = (a + b) * .5f; var bc = (b + c) * .5f; var cd = (c + d) * .5f;
            var abc = (ab + bc) * .5f; var bcd = (bc + cd) * .5f; var mid = (abc + bcd) * .5f;
            Split(a, ab, abc, mid, depth + 1); Split(mid, bcd, cd, d, depth + 1);
        }
    }

}
