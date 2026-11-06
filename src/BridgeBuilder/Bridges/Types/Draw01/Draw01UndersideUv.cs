using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Offline whole-island correspondence preserves UV seams within each slab panel.
internal static class Draw01UndersideUv
{
    internal static KeyValuePair<int, Vector2>[] ForLod(int lod) => lod == Numeric_Types_Draw01_Draw01UndersideUv_cs.Values.I32[0] ? Near : lod == Numeric_Types_Draw01_Draw01UndersideUv_cs.Values.I32[1] ? Middle : Array.Empty<KeyValuePair<int, Vector2>>();
    private static KeyValuePair<int, Vector2>[] Near => GenerationParameters.Get<KeyValuePair<int, Vector2>[]>("BridgeBuilder.Bridges.Draw01UndersideUv.Near", () => ReadPackedNear());
    private static KeyValuePair<int, Vector2>[] Middle => GenerationParameters.Get<KeyValuePair<int, Vector2>[]>("BridgeBuilder.Bridges.Draw01UndersideUv.Middle", () => ReadPackedMiddle());
    private static KeyValuePair<int, Vector2>[] ReadPackedNear()
    {
        var d = Numeric_Types_Draw01_Draw01UndersideUv_cs.Values;
        var values = new KeyValuePair<int, Vector2>[176];
        for (int i = 0; i < 176; i++)
            values[0 + i] = new(d.I32[2 + i * 1], new Vector2(d.F32[0 + i * 2], d.F32[1 + i * 2]));
        return values;
    }

    private static KeyValuePair<int, Vector2>[] ReadPackedMiddle()
    {
        var d = Numeric_Types_Draw01_Draw01UndersideUv_cs.Values;
        var values = new KeyValuePair<int, Vector2>[158];
        for (int i = 0; i < 158; i++)
            values[0 + i] = new(d.I32[178 + i * 1], new Vector2(d.F32[352 + i * 2], d.F32[353 + i * 2]));
        return values;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01UndersideUv_cs
internal static class Numeric_Types_Draw01_Draw01UndersideUv_cs
{
    static Numeric_Types_Draw01_Draw01UndersideUv_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01UndersideUv_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01UndersideUv_cs"));
}
