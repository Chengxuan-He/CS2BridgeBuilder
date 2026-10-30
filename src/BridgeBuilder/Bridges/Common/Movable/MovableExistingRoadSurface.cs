using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Measured UV projection inputs for original finite domains and completed offline units.
internal static class MovableExistingRoadSurface
{
    // Generation-only texture strips inside one repeating unit. Destination is a
    // normalized U interval (V when BandRows is set); source rectangles retain native pixels.
    internal readonly struct RepeatBand
    {
        internal readonly float Start, End, RepeatY;
        internal readonly Vector2 Min, Max;
        internal RepeatBand(float start, float end, Vector2 min, Vector2 max, float repeatY = 1f)
        { RepeatY = repeatY; Start = start; End = end; Min = min; Max = max; }
    }

    internal readonly struct Patch
    {
        internal readonly Vector2 Min, Max;
        internal readonly Vector2 FiniteSamplingMin, FiniteSamplingMax;
        internal readonly Vector4 U, V;
        internal readonly bool Project, Repeat, BandRows;
        // Absolute original-atlas projection, with no region-local frac or crop.
        // Used only for an evidenced finite source domain in this specific LOD.
        internal readonly bool SourceUv, SourceAtlasRepeat, ProjectTangent;
        // A completed offline unit; U/V already address its final native texture.
        // Its finite axis uses an interior guard while its repeat axis may exceed 1.
        internal readonly bool PreparedUnit, RepeatU, RepeatV;
        internal readonly Vector4? Tangent;
        internal readonly Matrix4x4? SourceTangent;
        internal readonly SurfaceAsset? Surface;
        internal readonly RepeatBand[]? Bands;
        internal Patch(Vector2 min, Vector2 max) { Min = FiniteSamplingMin = min; Max = FiniteSamplingMax = max; U = V = default; Project = false; Repeat = false; BandRows = false; SourceUv = SourceAtlasRepeat = ProjectTangent = false; PreparedUnit = RepeatU = RepeatV = false; Tangent = null; SourceTangent = null; Surface = null; Bands = null; }
        internal Patch(Vector2 min, Vector2 max, Vector4 u, Vector4 v, SurfaceAsset? surface = null, bool repeat = false,
            RepeatBand[]? bands = null, bool bandRows = false, bool sourceUv = false,
            bool preparedUnit = false, bool repeatU = false, bool repeatV = false, Vector4? tangent = null, bool sourceAtlasRepeat = false, bool projectTangent = false)
        {
            if (preparedUnit && (surface == null || sourceUv || bands != null))
                throw new System.ArgumentException("An offline unit requires its final Surface and explicit repeat axes.");
            if (sourceAtlasRepeat && (!sourceUv || surface == null || preparedUnit || bands != null ||
                (!repeatU && !repeatV) || !(max.x > min.x && max.y > min.y)))
                throw new System.ArgumentException("Original-atlas repetition requires one evidenced region, original Surface and explicit repeat axes.");
            if (projectTangent && ((!sourceUv && !preparedUnit) || tangent.HasValue))
                throw new System.ArgumentException("Projected tangent requires an explicit source or prepared mapping without a constant tangent override.");
            ProjectTangent = projectTangent;
            SourceTangent = null;
            SourceAtlasRepeat = sourceAtlasRepeat;
            Min = FiniteSamplingMin = min; Max = FiniteSamplingMax = max; U = u; V = v; Project = true; Surface = surface; Repeat = repeat;
            Bands = bands; BandRows = bandRows; SourceUv = sourceUv;
            PreparedUnit = preparedUnit; RepeatU = repeatU; RepeatV = repeatV;
            Tangent = tangent;
        }
        // Applied only after the input appearance gate accepts this source chart.
        // Rows multiply (Compose Vertex.Position.xyz, 1) in its object-local
        // coordinate frame, not world coordinates. Callers transform source rows
        // into that exact frame before supplying them.
        // Native packing retains direction, not magnitude; interior equivalence is
        // a separate source-specific validation requirement.
        internal Patch WithSourceTangentField(Vector4 x,Vector4 y,Vector4 z,float handedness)
            =>new Patch(this,x,y,z,handedness);
        private Patch(Patch source,Vector4 x,Vector4 y,Vector4 z,float handedness)
        {
            if (!source.SourceUv || source.SourceAtlasRepeat || source.PreparedUnit ||
                source.Tangent.HasValue || source.ProjectTangent || (handedness!=1f && handedness!=-1f))
                throw new System.ArgumentException("Source tangent field requires a finite source mapping, fixed handedness and no other tangent override.");
            for(int i=0;i<4;i++)
                if(!float.IsFinite(x[i])||!float.IsFinite(y[i])||!float.IsFinite(z[i]))
                    throw new System.ArgumentException("Source tangent field coefficients must be finite.");
            var field=new Matrix4x4();field.SetRow(0,x);field.SetRow(1,y);field.SetRow(2,z);
            field.SetRow(3,new Vector4(0,0,0,handedness));
            this=source;SourceTangent=field;
        }
        // Content bounds define the mapping window; an offline producer may also
        // have authored valid finite guard pixels outside that window.
        internal Patch WithFiniteSampling(Vector2 min,Vector2 max)=>new Patch(this,min,max);
        private Patch(Patch source,Vector2 min,Vector2 max)
        {
            if(!source.PreparedUnit)throw new System.ArgumentException("Finite sampling guards require a completed offline material.");
            for(int axis=0;axis<2;axis++)
                if(!float.IsFinite(source.Min[axis])||!float.IsFinite(source.Max[axis])||source.Min[axis]>=source.Max[axis]||
                    !float.IsFinite(min[axis])||!float.IsFinite(max[axis])||min[axis]<0||max[axis]>1||
                    min[axis]>=max[axis]||min[axis]>source.Min[axis]||max[axis]<source.Max[axis])
                    throw new System.ArgumentException("Offline finite sampling bounds must contain the content window inside the native texture.");
            this=source;FiniteSamplingMin=min;FiniteSamplingMax=max;
        }

    }

}
