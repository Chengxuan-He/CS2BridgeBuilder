using System;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;

namespace BridgeBuilder.Bridges;
// Consumer for the four continuous half-leaf domains. The caller must supply
// validated offline products and their measured output bounds/projections.
// This helper creates no assets and performs no atlas-period subdivision.
internal static class PedestrianDraw02RepeatAppearance
{
    internal static Patch Create(SurfaceAsset finished, Vector2 contentMin, Vector2 contentMax, Vector4 finiteU, Vector4 continuousV, Vector4 measuredTangent)
    {
        if (finished == null || !Valid(contentMin.x) || !Valid(contentMax.x) || !Valid(contentMin.y) || !Valid(contentMax.y) || contentMin.x < Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs.Values.I32[0] || contentMax.x > Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs.Values.I32[1] || contentMin.x >= contentMax.x || contentMin.y < Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs.Values.I32[2] || contentMax.y > Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs.Values.I32[3] || contentMin.y >= contentMax.y || !Valid(finiteU) || !Valid(continuousV) || !Valid(measuredTangent))
            throw new InvalidOperationException(nameof(PedestrianDraw02RepeatAppearance));
        // The completed /3 products include explicitly evidenced outer guard
        // panels and passed coverage at every output mip. Content bounds remain
        // distinct from the full texture's valid finite sampling domain.
        return new Patch(contentMin, contentMax, finiteU, continuousV, surface: finished, preparedUnit: true, repeatU: false, repeatV: true, tangent: measuredTangent)
            .WithFiniteSampling(Vector2.zero, Vector2.one);
    }

    private static bool Valid(float value) => float.IsFinite(value);
    private static bool Valid(Vector4 value) => Valid(value.x) && Valid(value.y) && Valid(value.z) && Valid(value.w);
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs");
}
