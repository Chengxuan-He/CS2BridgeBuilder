using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Applies the immutable output of the TrussArchBridge03 section metaprogram.
///
/// Full detail is classified once from the shipped archetype. Every island wholly on one side of
/// x=0, including the small deck-height wing fittings, translates rigidly. Every logical transverse
/// assembly that crosses x=0 stretches. LOD1 and LOD2 inherit the full-detail decision by
/// recorded vertex maps, so moving the camera cannot change the transform rule.
///
/// A stored coefficient describes x' = x + coefficient * (extra / 2). Unmarked vertices use
/// sign(x), the rigid-side rule. Runtime does not inspect bounds, topology or coordinate bands.
/// </summary>
internal static class TrussArch03Geometry
{
    private static readonly IReadOnlyDictionary<string, PortalMap> SectionMaps =
        TrussArch03SectionData.Maps;

    internal static bool IsRecorded(string? styleId, string? meshName) =>
        string.Equals(styleId, "TrussArch03", StringComparison.Ordinal)
        && !string.IsNullOrEmpty(meshName)
        && SectionMaps.ContainsKey(meshName);

    internal static bool TryWidenSection(
        string meshName,
        float3[] source,
        float extra,
        out float3[] moved,
        out int rigidVertices,
        out int stretchingVertices)
    {
        moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        rigidVertices = 0;
        stretchingVertices = 0;

        if (!SectionMaps.TryGetValue(meshName, out var map) || !map.Matches(source.Length))
            return false;

        rigidVertices = source.Length - map.CoefficientCount;
        stretchingVertices = map.CoefficientCount;
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

        internal float Coefficient(int index) =>
            BitConverter.ToSingle(_coefficients, index * sizeof(float));
    }
}
