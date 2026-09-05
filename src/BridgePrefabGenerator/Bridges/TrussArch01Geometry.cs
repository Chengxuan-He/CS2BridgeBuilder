using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// Applies the immutable output of the TrussArchBridge01 geometry metaprogram.
///
/// The full-detail and LOD1 pier meshes contain separate authored islands. The metaprogram marks only
/// islands which cross x=0 as transverse beams; every island wholly on x&lt;0 or x&gt;0 is a column or side
/// fitting and translates rigidly. LOD2 welds those parts into one mesh, so it inherits a fixed vertex
/// map from the full-detail prototype instead of making another runtime geometry decision.
///
/// A stored coefficient describes x' = x + coefficient * (extra / 2). Unmarked vertices use sign(x),
/// the rigid-column rule. Runtime never measures a non-zero boundary.
/// </summary>
internal static class TrussArch01Geometry
{
    // These are immutable measurements of the shipped TrussArchBridge01 archetype. They are produced
    // by the geometry metaprogram, not rediscovered from a generated mesh while the game is running.
    internal const float PrototypeSectionOuter = 7.699951f;
    internal const float PrototypePierOuter = 7.600708f;
    internal const float PrototypeBaseWidth = 18.419433f;

    private static readonly IReadOnlyDictionary<string, PortalMap> SectionMaps =
        TrussArch01SectionData.Maps;

    private static readonly IReadOnlyDictionary<string, PortalMap> PierMaps =
        TrussArch01PierData.Maps;

    internal static float PierExtraForSection(float sectionExtra) =>
        sectionExtra + 2f * (PrototypeSectionOuter - PrototypePierOuter);

    internal static float SectionExtraForPier(float pierExtra) =>
        pierExtra - 2f * (PrototypeSectionOuter - PrototypePierOuter);

    internal static bool TryWidenSection(
        string meshName, float3[] source, float extra, out float3[] moved) =>
        TryApply(SectionMaps, meshName, source, extra, out moved);

    internal static bool TryWidenPier(
        string meshName, float3[] source, float extra, out float3[] moved) =>
        TryApply(PierMaps, meshName, source, extra, out moved);

    private static bool TryApply(
        IReadOnlyDictionary<string, PortalMap> maps,
        string meshName,
        float3[] source,
        float extra,
        out float3[] moved)
    {
        moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        if (!maps.TryGetValue(meshName, out var map) || !map.Matches(source.Length)) return false;
        if (Math.Abs(extra) < TowerWidening.CentreEpsilon) return true;

        var shift = extra * 0.5f;
        var coefficientOffset = 0;
        for (var index = 0; index < moved.Length; index++)
        {
            if (!map.Stretches(index))
            {
                moved[index].x = TowerWidening.Spread(source[index].x, extra);
                continue;
            }

            moved[index].x = source[index].x + map.Coefficient(coefficientOffset++) * shift;
        }
        return coefficientOffset == map.CoefficientCount;
    }

    internal sealed class PortalMap
    {
        private readonly int _vertices;
        private readonly byte[] _membership;
        private readonly byte[] _coefficients;
        private readonly int _markedVertices;

        internal PortalMap(int vertices, string membership, string coefficients)
        {
            _vertices = vertices;
            _membership = Convert.FromBase64String(membership);
            _coefficients = Convert.FromBase64String(coefficients);
            foreach (var bits in _membership)
            {
                var remaining = bits;
                while (remaining != 0)
                {
                    _markedVertices += remaining & 1;
                    remaining >>= 1;
                }
            }
        }

        internal int CoefficientCount => _coefficients.Length / sizeof(float);

        internal bool Matches(int vertices) =>
            vertices == _vertices
            && _membership.Length == (vertices + 7) / 8
            && _coefficients.Length == _markedVertices * sizeof(float);

        internal bool Stretches(int vertex) =>
            (_membership[vertex >> 3] & (1 << (vertex & 7))) != 0;

        internal float Coefficient(int index) => BitConverter.ToSingle(_coefficients, index * sizeof(float));
    }
}
