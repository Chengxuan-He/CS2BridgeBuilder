using System;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;
// The original DrawBridge02 network carries Car Drive Piece 3 and Sidewalk
// Piece 3.5 - NoTram. The mechanism has separate running-surface and walkway
// domains. Wheel-track appearance has no measured prototype counterpart.
// These flags select layers; they never classify an entire mixed road material.
internal static class Draw02AppearanceSupport
{
    internal static Type[] ForLod(int lod)
    {
        if (lod < Numeric_Types_Draw02_Draw02AppearanceSupport_cs.Values.I32[0] || lod >= Numeric_Types_Draw02_Draw02RenderAssembly_cs.Values.I32[Numeric_Types_Draw02_Draw02AppearanceSupport_cs.Values.I32[1]])
            throw new ArgumentOutOfRangeException(nameof(lod));
        // Same physical roles at every LOD. Mapping and Surface selection are
        // independent: this does not authorize near-LOD UVs on the far atlas.
        return new[]
        {
            Type.Asphalt,
            Type.Asphalt,
            Type.Asphalt | Type.Sidewalk,
            Type.Asphalt | Type.Sidewalk,
            Type.Asphalt,
            Type.Asphalt,
            Type.Asphalt | Type.Sidewalk,
            Type.Asphalt | Type.Sidewalk,
            Type.Asphalt,
            Type.Asphalt,
            Type.Asphalt | Type.Sidewalk,
            Type.Asphalt | Type.Sidewalk,
            Type.Asphalt,
            Type.Asphalt,
            Type.Asphalt | Type.Sidewalk,
            Type.Asphalt | Type.Sidewalk
        };
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02AppearanceSupport_cs
internal static class Numeric_Types_Draw02_Draw02AppearanceSupport_cs
{
    static Numeric_Types_Draw02_Draw02AppearanceSupport_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02AppearanceSupport_cs");
}
