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
    internal static Patch Create(MovableExistingRoadSurface.SurfaceResult finished, Vector2 contentMin, Vector2 contentMax, Vector4 finiteU, Vector4 continuousV, Vector4 measuredTangent)
    {
        if(finished.Error!=null)return new Patch(finished.Error);
        // The completed /3 products include explicitly evidenced outer guard
        // panels and passed coverage at every output mip. Content bounds remain
        // distinct from the full texture's valid finite sampling domain.
        return new Patch(contentMin, contentMax, finiteU, continuousV, surface: finished, preparedUnit: true, repeatU: false, repeatV: true, tangent: measuredTangent)
            .WithFiniteSampling(Vector2.zero, Vector2.one);
    }

}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs.Values", () => HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02RepeatAppearance_cs"));
}
