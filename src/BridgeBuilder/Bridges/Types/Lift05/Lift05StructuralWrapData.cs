using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Offline measured source profiles and adjacency; runtime never discovers parts.
// Producer: tools/PreparedMaterials/Lift05/BuildStructuralWrapData.py.
internal static class Lift05StructuralWrapData
{
    internal sealed class Profile
    {
        internal int Parent, Anchor, ParentVertex, Vertex;
        internal double MinU, MaxU, Rate;
    }
    internal sealed class Face
    {
        internal int Triangle, Profile;
        internal int[] Indices = null!;
        internal Vector2[] UV = null!;
    }
    internal sealed class Level
    {
        internal int VertexCount, IndexCount;
        internal int[] Domains = null!;
        internal Profile[] Profiles = null!;
        internal Face[] Faces = null!;
    }
    internal static Level For(int lod) => Data.Levels[lod];
    private static class Data
    {
        static Data() { }
        internal static readonly Level[] Levels = Read();
        private static Level[] Read()
        {
            var d = Numeric_Types_Lift05_StructuralWrap.Values;
            int at = 0, fp = 0, dp = 0;
            if (d.I32[at++] != 1 || d.I32[at++] != 2)
                throw new InvalidOperationException("Invalid Lift05 structural wrap schema.");
            var levels = new Level[2];
            for (int lod = 0; lod < levels.Length; lod++)
            {
                var level = levels[lod] = new Level { VertexCount = d.I32[at++], IndexCount = d.I32[at++] };
                level.Domains = new int[d.I32[at++]];
                for (int i = 0; i < level.Domains.Length; i++) level.Domains[i] = d.I32[at++];
                level.Profiles = new Profile[d.I32[at++]];
                for (int i = 0; i < level.Profiles.Length; i++)
                    level.Profiles[i] = new Profile { Parent = d.I32[at++], Anchor = d.I32[at++],
                        ParentVertex = d.I32[at++], Vertex = d.I32[at++],
                        MinU = d.F64[dp++], MaxU = d.F64[dp++], Rate = d.F64[dp++] };
                level.Faces = new Face[d.I32[at++]];
                for (int i = 0; i < level.Faces.Length; i++)
                {
                    var face = level.Faces[i] = new Face { Triangle = d.I32[at++], Profile = d.I32[at++],
                        Indices = new int[3], UV = new Vector2[3] };
                    for (int k = 0; k < 3; k++) face.Indices[k] = d.I32[at++];
                    for (int k = 0; k < 3; k++) face.UV[k] = new Vector2(d.F32[fp++], d.F32[fp++]);
                }
            }
            if (at != d.I32.Length || fp != d.F32.Length || dp != d.F64.Length)
                throw new InvalidOperationException("Invalid Lift05 structural wrap payload length.");
            return levels;
        }
    }
}

// Embedded numerical arrays: Types_Lift05_StructuralWrap
internal static class Numeric_Types_Lift05_StructuralWrap
{
    static Numeric_Types_Lift05_StructuralWrap() { }
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_StructuralWrap");
}
