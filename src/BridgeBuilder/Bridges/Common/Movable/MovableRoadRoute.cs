using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Selection is total and does no asset I/O. Reading a required source is a separate
// realization step: corrupt descriptors are not disguised as optional no-matches.
internal static class MovableRoadRoute
{
    internal enum Kind { Original, Prototype, Corresponding }
    // Offline verified source sampling, not a new repeat or a target atlas domain.
    internal readonly struct SourceDomain
    {
        internal readonly float MinU, MaxU;
        internal readonly MovableRoadAppearance.Type Type;
        internal bool RepeatU => false;
        internal bool RepeatV => true;
        internal SourceDomain(float minU, float maxU, MovableRoadAppearance.Type type)
        { MinU=minU; MaxU=maxU; Type=type; }
        internal bool Contains(Vector2 uv) => float.IsFinite(uv.x) && float.IsFinite(uv.y) &&
            uv.x>=MinU && uv.x<=MaxU;
    }
    internal readonly struct Source
    {
        internal readonly string Surface, Color, Normal, Mask;
        internal readonly bool Builtin;
        internal readonly MovableRoadAppearance.Type Worldspace;
        internal Source(string surface, bool builtin, string color, string normal, string mask,
            MovableRoadAppearance.Type worldspace)
        { Surface=surface; Builtin=builtin; Color=color; Normal=normal; Mask=mask; Worldspace=worldspace; }

        internal SourceDomain? MeasuredDomain(MovableRoadAppearance.Type receiver)
        {
            float min=MovableRoadLayerIdentity.SidewalkMinU, max=MovableRoadLayerIdentity.SidewalkMaxU;
            // Exact PBR identities are checked by the same classifier used for receivers.
            // sidewalk-source-pbr.json records all rows and all three native textures:
            // wrap=Repeat (0), Trilinear (2), anisotropy=16. U stays within the paving
            // band; only V may repeat over the original full texture period. Coarse
            // mip network blending differences remain those of the OriginalBase route.
            return receiver==MovableRoadAppearance.Type.Sidewalk &&
                MovableRoadLayerIdentity.WholeReceiver(Surface,Builtin,Color,Normal,Mask,Worldspace,min,max)==receiver
                ? new SourceDomain(min,max,receiver) : null;
        }

        internal bool IsMeasuredBase(MovableRoadGeometry.Triangle triangle)
        {
            if (!Builtin || Surface!="1d7dad9f64c9fe55a809f1080c39fb92" ||
                Color!="9a6506aaede935752b5c4c0362c95f6e" ||
                Normal!="a142c18b42577de52815d1afd054660b" || Mask!="37ceea0e8c2bd24599e237236e6ae338") return false;
            // Exact RoadEU base-region columns 546..2047, measured over every source row.
            // Coarse mips contain network blending: this is the authorized corresponding
            // base appearance, not a claim of full network-shader equivalence.
            bool Inside(MovableRoadGeometry.Vertex vertex) => float.IsFinite(vertex.UV.x) &&
                float.IsFinite(vertex.UV.y) && vertex.UV.x>=547f/2048f && vertex.UV.x<=1f;
            return Inside(triangle.A)&&Inside(triangle.B)&&Inside(triangle.C);
        }

        internal MovableRoadAppearance.Type Receiver(MovableRoadGeometry.Triangle triangle)
        {
            float a=triangle.A.UV.x,b=triangle.B.UV.x,c=triangle.C.UV.x;
            if (!float.IsFinite(a)||!float.IsFinite(b)||!float.IsFinite(c)) return MovableRoadAppearance.Type.None;
            return MovableRoadLayerIdentity.WholeReceiver(Surface,Builtin,Color,Normal,Mask,Worldspace,
                Math.Min(a,Math.Min(b,c)),Math.Max(a,Math.Max(b,c)));
        }
    }

    internal static Kind Select(Source source, MovableRoadGeometry.Triangle triangle,
        MovableRoadAppearance.Type supported, bool hasPrototype, bool hasCorresponding)
    {
        var actual=source.Receiver(triangle);
        if (actual is MovableRoadAppearance.Type.None or MovableRoadAppearance.Type.Curb) return Kind.Original;
        if (hasPrototype && (supported & actual)!=0) return Kind.Prototype;
        return hasCorresponding ? Kind.Corresponding : Kind.Original;
    }

    internal static Source Read(SurfaceAsset source)
    {
        source.LoadProperties(true);
        try
        {
            string Id(string key)=>source.TryGetTexture(key,out var texture)&&texture!=null
                ? texture.id.guid.ToString() : string.Empty;
            string cid=source.id.guid.ToString();
            return new Source(cid,source.isBuiltin,Id("_BaseColorMap"),Id("_NormalMap"),Id("_MaskMap"),
                MovableRoadLayerIdentity.Worldspace(cid,source.isBuiltin,Id("_WorldspaceAlbedo"),
                    Id("_WorldspaceNormalMap"),Id("_WorldspaceMaskMap"),string.Empty));
        }
        finally { source.UnloadProperties(); }
    }
}
