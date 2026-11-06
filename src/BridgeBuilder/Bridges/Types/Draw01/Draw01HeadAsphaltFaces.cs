using System;
using BridgeBuilder.Runtime;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
// Exact native source face membership, measured in MeasureHeadAsphalt.py.
// Unknown source/LOD identities receive no inferred asphalt membership.
internal static class Draw01HeadAsphaltFaces
{
    internal static bool TryGet(RenderPrefab source, out int[] triangles)
    {
        triangles = Array.Empty<int>();
        int record = source.geometryAsset?.id.guid.ToString() switch
        {
            "65b7e0cbd9c16168c9f6c306369b88f6" => Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[0],
            "812aeb2cabadb5ad8383e31856a75f0e" => Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[1],
            "82feda9919b429b4fdef3e95aa176aea" => Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[2],
            "92eb25687f4a6fcd79cf82a761ee2e23" => Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[3],
            "b309a1ae9095ba3370b82099cf26f6ef" => Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[4],
            "b7d954c9539c359c8a51d154b4c6df53" => Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[5],
            "c6b3897dbc66e186de5bb56ed777273b" => Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[6],
            _ => -Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[7]
        };
        if (record < Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[8])
            return false;
        var material = source.GetSurfaceAsset(Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[9]);
        if (material == null || material.id.guid.ToString() != "1d7dad9f64c9fe55a809f1080c39fb92")
            return false;
        var values = Data.Values.I32;
        int at = Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[10];
        for (int i = Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[11]; i < record; i++)
            at = checked(at + Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[12] + values[at + Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[13]]);
        int count = values[at + Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[14]];
        triangles = new int[count];
        Array.Copy(values, at + Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[15], triangles, Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values.I32[16], count);
        return true;
    }

    private static class Data
    {
        internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Draw01HeadAsphaltFaces.Data.Values", () => HardcodedNumbers.Load("Types_Draw01_HeadAsphaltFaces"));
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01HeadAsphaltFaces_cs
internal static class Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs
{
    static Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01HeadAsphaltFaces_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01HeadAsphaltFaces_cs"));
}
