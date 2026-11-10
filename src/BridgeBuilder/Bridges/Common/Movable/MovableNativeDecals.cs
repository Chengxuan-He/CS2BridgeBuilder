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
        internal readonly SurfaceAsset? SourceColor;
        internal readonly Vector4 UvRect;
        internal bool Overlaps(MovableRoadGeometry.Triangle face)
        {
            // Separating-axis test in the actual decal frame. Merely sharing a
            // boundary with a colored lane does not protect the adjoining sidewalk.
            var center=Center; var right=Right; var forward=Forward;
            Vector2 Local(Vector3 p) => new(Vector3.Dot(p-center,right),Vector3.Dot(p-center,forward));
            var a=Local(face.A.Position); var b=Local(face.B.Position); var c=Local(face.C.Position);
            var half=Half;
            bool Separated(Vector2 axis)
            {
                if (axis.sqrMagnitude == 0) return false;
                float radius=Math.Abs(axis.x)*half.x+Math.Abs(axis.y)*half.z;
                float x=Vector2.Dot(a,axis),y=Vector2.Dot(b,axis),z=Vector2.Dot(c,axis);
                return Math.Min(x,Math.Min(y,z)) >= radius || Math.Max(x,Math.Max(y,z)) <= -radius;
            }
            Vector2 Normal(Vector2 v) => new(-v.y,v.x);
            return !Separated(Vector2.right) && !Separated(Vector2.up) && !Separated(Normal(b-a)) &&
                !Separated(Normal(c-b)) && !Separated(Normal(a-c));
        }
        internal Vector4 SourceUv(Vector3 point)
        {
            var d=point-Center;
            float u=Half.x==0 ? .5f : .5f+Vector3.Dot(d,Right)/(2*Half.x);
            float v=Half.z==0 ? .5f : .5f+Vector3.Dot(d,Forward)/(2*Half.z);
            return new Vector4(Mathf.LerpUnclamped(UvRect.x,UvRect.z,u),Mathf.LerpUnclamped(UvRect.y,UvRect.w,v),0,0);
        }
        internal Region(Vector3 center, Quaternion rotation, Vector3 half, bool yellow, SurfaceAsset? sourceColor = null, Vector4 uvRect = default)
        { Center = center; Right = rotation * Vector3.right; Up = rotation * Vector3.up;
          Forward = rotation * Vector3.forward; Half = half; Yellow = yellow; SourceColor = sourceColor; UvRect = uvRect; }
    }
    // Native protocol identity, not a source material/name/CID allowlist. Other
    // descriptors using the deterioration shader carry the same wear semantics.
    // Prototype WheelTrack road composition already owns any permitted wear.
    internal static bool IsSelectedRoadWear(MovableLaneVisuals.Batch batch)
    {
        if (!batch.Decal || batch.Surfaces.Length != 1) return false;
        var source = batch.Surfaces[0];
        source.LoadHeader();
        var library = AssetDatabase.global.resources.materialLibrary;
        var material = library.GetMaterial(source.materialTemplateHash);
        var protocol = library.GetMaterial(19); // Native deterioration protocol.
        if (material == null || material.shader == null || protocol == null || protocol.shader == null)
            throw new InvalidOperationException("Native decal shader protocol is unavailable.");
        return material.shader == protocol.shader;
    }

    internal static bool IsLaneColor(MovableLaneVisuals.Batch batch)
    {
        if (!batch.Decal || batch.Surfaces.Length != 1) return false;
        batch.Surfaces[0].LoadHeader();
        var source=batch.Surfaces[0];
        try
        {
            source.LoadProperties(true);
            var template=AssetDatabase.global.resources.materialLibrary.GetMaterial(source.materialTemplateHash);
            bool Keyword(string name)=>template.IsKeywordEnabled(name)||source.keywords.Contains(name);
            return Keyword("_MATERIAL_AFFECTS_ALBEDO") && !Keyword("_MATERIAL_AFFECTS_NORMAL") && !Keyword("_MATERIAL_AFFECTS_MASKMAP");
        }
        finally{source.UnloadProperties();}
    }

    internal static List<Region> Regions(MovableLaneVisuals.Batch batch, Pose roadToObject, float texelsPerMeter)
    {
        var result = new List<Region>();
        if (!batch.Decal || batch.Surfaces.Length != 1 || !(texelsPerMeter > 0f))
            throw new InvalidOperationException("Native marking conversion requires one decal surface and positive density.");
        var source = batch.Surfaces[0];
        source.LoadHeader();
        bool bounded = false;
        var curve = batch.CurveMatrix;
        var segments = new List<(Vector4 A, Vector4 D)>();
        Split(curve.GetColumn(0), curve.GetColumn(1), curve.GetColumn(2), curve.GetColumn(3), 0);
        var original = batch.Source.GetComponent<DecalProperties>();
        if(bounded)Mod.Log.Warn("Projected source color uses bounded curve sampling (at most 256 segments); local texture resolution retained.");
        // Every native decal uses its own complete source texture interval. Pixel
        // coverage is alpha, not a name-dependent white/yellow geometry mask.
        var strokes = segments.Select(s => (A:s.A,D:s.D));
        foreach (var segment in strokes)
        {
            var direction = (Vector3)(segment.D - segment.A);
            var length = direction.magnitude;
            if (!float.IsFinite(length)) throw new InvalidOperationException("Native marking curve is non-finite.");
            var rotation = length == 0f ? batch.Transform.rotation : Quaternion.LookRotation(direction, Vector3.up);
            var inverse = Quaternion.Inverse(rotation);
            var progress = curve.GetColumn(3).w == 0f ? 0f : (segment.A.w + segment.D.w) * .5f / curve.GetColumn(3).w;
            var width = batch.Size.x * Mathf.Lerp(batch.Scale.x, batch.Scale.z, progress);
            var height = batch.Size.y;
            if (!float.IsFinite(width) || !float.IsFinite(height)) throw new InvalidOperationException("Non-finite native decal dimensions.");
            var center = (Vector3)batch.Transform.GetColumn(3)
                + (Vector3)(segment.A + segment.D) * .5f + Vector3.up * batch.Size.w
               ;
            result.Add(new Region(roadToObject.position + roadToObject.rotation * center,
                roadToObject.rotation * rotation, new Vector3(Mathf.Abs(width), Mathf.Abs(height), length) * .5f, false, source,
                new Vector4(original.m_TextureArea.min.x,
                    Mathf.LerpUnclamped(original.m_TextureArea.min.y,original.m_TextureArea.max.y,segment.A.w),
                    original.m_TextureArea.max.x,
                    Mathf.LerpUnclamped(original.m_TextureArea.min.y,original.m_TextureArea.max.y,segment.D.w))));
        }
        return result;

        void Split(Vector4 a, Vector4 b, Vector4 c, Vector4 d, int depth)
        {
            var chord = (Vector3)(d - a);
            var length = chord.magnitude;
            var error = length > 0f ? Mathf.Max(Vector3.Cross(chord, (Vector3)(b - a)).magnitude,
                Vector3.Cross(chord, (Vector3)(c - a)).magnitude) / length : float.PositiveInfinity;
            var widthChange = Mathf.Abs(batch.Size.x * (batch.Scale.z - batch.Scale.x)
                * (curve.GetColumn(3).w == 0f ? 0f : (d.w - a.w) / curve.GetColumn(3).w));
            // Spatial and width approximation are bounded by the existing road sampling
            // density. This does not allocate textures or classify mechanism structure.
            if (error <= 1f / texelsPerMeter && widthChange <= 1f / texelsPerMeter)
            { segments.Add((a, d)); return; }
            if (depth == 8 || (a.Equals(b) && b.Equals(c) && c.Equals(d))) { if(depth==8)bounded=true; segments.Add((a,d)); return; }
            var ab = (a + b) * .5f; var bc = (b + c) * .5f; var cd = (c + d) * .5f;
            var abc = (ab + bc) * .5f; var bcd = (bc + cd) * .5f; var mid = (abc + bcd) * .5f;
            Split(a, ab, abc, mid, depth + 1); Split(mid, bcd, cd, d, depth + 1);
        }
    }

}
