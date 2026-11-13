using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Measured UV projection inputs for original finite domains and completed offline units.
internal static class MovableExistingRoadSurface
{
    internal readonly struct SurfaceResult
    {
        internal readonly SurfaceAsset? Value;
        internal readonly string? Error;
        internal bool Success=>Error==null;
        public static implicit operator SurfaceResult(SurfaceAsset value)=>new SurfaceResult(value);
        internal SurfaceResult(SurfaceAsset? value,string? error=null){Value=error==null?value:null;Error=error;}
    }

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
        internal readonly string? Error;
        internal Patch(string error){this=default;Error=error;}
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
        internal Patch(Vector2 min, Vector2 max) { Error=null; Min = FiniteSamplingMin = min; Max = FiniteSamplingMax = max; U = V = default; Project = false; Repeat = false; BandRows = false; SourceUv = SourceAtlasRepeat = ProjectTangent = false; PreparedUnit = RepeatU = RepeatV = false; Tangent = null; SourceTangent = null; Surface = null; Bands = null; }
        internal Patch(Vector2 min, Vector2 max, Vector4 u, Vector4 v, SurfaceAsset? surface = null, bool repeat = false,
            RepeatBand[]? bands = null, bool bandRows = false, bool sourceUv = false,
            bool preparedUnit = false, bool repeatU = false, bool repeatV = false, Vector4? tangent = null, bool sourceAtlasRepeat = false, bool projectTangent = false)
        {
            this=default;
            ProjectTangent = projectTangent;
            SourceTangent = null;
            SourceAtlasRepeat = sourceAtlasRepeat;
            Min = FiniteSamplingMin = min; Max = FiniteSamplingMax = max; U = u; V = v; Project = true; Surface = surface; Repeat = repeat;
            Bands = bands; BandRows = bandRows; SourceUv = sourceUv;
            PreparedUnit = preparedUnit; RepeatU = repeatU; RepeatV = repeatV;
            Tangent = tangent;
        }
        internal Patch(Vector2 min,Vector2 max,Vector4 u,Vector4 v,SurfaceResult surface,bool repeat=false,
            RepeatBand[]? bands=null,bool bandRows=false,bool sourceUv=false,bool preparedUnit=false,bool repeatU=false,bool repeatV=false,
            Vector4? tangent=null,bool sourceAtlasRepeat=false,bool projectTangent=false)
            :this(min,max,u,v,surface.Value,repeat,bands,bandRows,sourceUv,preparedUnit,repeatU,repeatV,tangent,sourceAtlasRepeat,projectTangent)
        {Error=surface.Error??Error;}

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
            this=source;if(Error!=null)return;
            var field=new Matrix4x4();field.SetRow(0,x);field.SetRow(1,y);field.SetRow(2,z);
            field.SetRow(3,new Vector4(0,0,0,handedness));
            this=source;SourceTangent=field;
        }
        // Content bounds define the mapping window; an offline producer may also
        // have authored valid finite guard pixels outside that window.
        internal Patch WithFiniteSampling(Vector2 min,Vector2 max)=>new Patch(this,min,max);
        private Patch(Patch source,Vector2 min,Vector2 max)
        {
            this=source;if(Error!=null)return;
            this=source;FiniteSamplingMin=min;FiniteSamplingMax=max;
        }

    }

}
