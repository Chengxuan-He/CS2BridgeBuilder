using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Applies the reviewed output of the TrussArchBridge02 geometry metaprogram.
///
/// The white bridge has two independent lateral envelopes in one section mesh. The arch and pier
/// belong to the inner envelope; the deck base immediately below the road and the railings belong to
/// the outer envelope. Full detail determines that membership once, and both LODs inherit it. Runtime
/// performs only an exact mesh-name lookup and reads the stored per-vertex map.
///
/// Inner centre-crossing members stretch against their own recorded span. Inner side arches and pier
/// columns translate rigidly. Outer section vertices use x' = x + sign(x) * delta, preserving the
/// shape of the deck base and railings while moving them to the complete visible road width. Both
/// pillar meshes belong to the inner bridge-pier assembly and receive the same displacement as the
/// inner arch. The mesh named <c>NetPillarBase</c> is a pier footing, not the deck base described by
/// the project contract.
/// </summary>
internal static class TrussArch02Geometry
{
    // Exact lateral spans measured from the shipped full-detail archetype by GeometryMetaprogram.
    internal const float PrototypeSectionOuterWidth = 20.79248f;
    internal const float PrototypeSectionInnerLeft = 8.918701f;
    internal const float PrototypeSectionInnerRight = 8.818848f;
    internal const float PrototypeSectionInnerWidth = 17.737549f;
    internal const float PrototypePillarInnerWidth = 15.699708f;
    internal const float PrototypePillarOuterWidth = 30.15966f;

    private static readonly IReadOnlyDictionary<string, TransformMap> Maps =
        TrussArch02GeometryData.Maps;

    internal static bool IsRecorded(string? styleId, string? meshName) =>
        string.Equals(styleId, "TrussArch02", StringComparison.Ordinal)
        && !string.IsNullOrEmpty(meshName)
        && Maps.ContainsKey(meshName);

    internal static bool TryWidenSection(
        string meshName,
        float3[] source,
        float targetOuterWidth,
        float targetInnerLeft,
        float targetInnerRight,
        bool removeLeftOuterRailing,
        bool removeRightOuterRailing,
        out float3[] moved,
        out TransformFacts facts,
        out bool[]? dropped)
    {
        var outerDelta = (targetOuterWidth - PrototypeSectionOuterWidth) * 0.5f;
        var innerLeftDelta = targetInnerLeft - PrototypeSectionInnerLeft;
        var innerRightDelta = targetInnerRight - PrototypeSectionInnerRight;
        return TryApply(
            meshName,
            source,
            outerDelta,
            outerDelta,
            innerLeftDelta,
            innerRightDelta,
            removeLeftOuterRailing,
            removeRightOuterRailing,
            out moved,
            out facts,
            out dropped);
    }

    internal static bool TryWidenTowerPart(
        string meshName,
        float3[] source,
        float leftDelta,
        float rightDelta,
        out float3[] moved,
        out TransformFacts facts) =>
        TryApply(
            meshName,
            source,
            leftDelta,
            rightDelta,
            leftDelta,
            rightDelta,
            removeLeftOuterRailing: false,
            removeRightOuterRailing: false,
            out moved,
            out facts,
            out _);

    private static bool TryApply(
        string meshName,
        float3[] source,
        float outerLeftDelta,
        float outerRightDelta,
        float innerLeftDelta,
        float innerRightDelta,
        bool removeLeftOuterRailing,
        bool removeRightOuterRailing,
        out float3[] moved,
        out TransformFacts facts,
        out bool[]? dropped)
    {
        moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        facts = default;
        dropped = null;
        if (!Maps.TryGetValue(meshName, out var map) || !map.Matches(source.Length)) return false;

        var coefficientOffset = 0;
        var innerVertices = 0;
        var stretchedVertices = 0;
        var droppedRailingVertices = 0;
        for (var index = 0; index < moved.Length; index++)
        {
            var inner = map.IsInner(index);
            var leftDelta = inner ? innerLeftDelta : outerLeftDelta;
            var rightDelta = inner ? innerRightDelta : outerRightDelta;
            if (inner) innerVertices++;

            if (map.Stretches(index))
            {
                var coefficient = map.Coefficient(coefficientOffset++);
                moved[index].x = source[index].x + (coefficient < 0f
                    ? coefficient * leftDelta
                    : coefficient > 0f
                        ? coefficient * rightDelta
                        : 0f);
                stretchedVertices++;
            }
            else
            {
                moved[index].x = source[index].x < 0f
                    ? source[index].x - leftDelta
                    : source[index].x > 0f
                        ? source[index].x + rightDelta
                        : source[index].x;
            }

            if (map.IsOuterRailing(index)
                && ((source[index].x < 0f && removeLeftOuterRailing)
                    || (source[index].x > 0f && removeRightOuterRailing)))
            {
                dropped ??= new bool[source.Length];
                dropped[index] = true;
                droppedRailingVertices++;
            }
        }

        facts = new TransformFacts(
            innerVertices,
            source.Length - innerVertices,
            stretchedVertices,
            source.Length - stretchedVertices,
            innerLeftDelta,
            innerRightDelta,
            outerLeftDelta,
            outerRightDelta,
            droppedRailingVertices);
        return coefficientOffset == map.CoefficientCount;
    }

    internal readonly struct TransformFacts
    {
        internal TransformFacts(
            int innerVertices,
            int outerVertices,
            int stretchingVertices,
            int rigidVertices,
            float innerLeftDelta,
            float innerRightDelta,
            float outerLeftDelta,
            float outerRightDelta,
            int droppedRailingVertices)
        {
            InnerVertices = innerVertices;
            OuterVertices = outerVertices;
            StretchingVertices = stretchingVertices;
            RigidVertices = rigidVertices;
            InnerLeftDelta = innerLeftDelta;
            InnerRightDelta = innerRightDelta;
            OuterLeftDelta = outerLeftDelta;
            OuterRightDelta = outerRightDelta;
            DroppedRailingVertices = droppedRailingVertices;
        }

        internal int InnerVertices { get; }
        internal int OuterVertices { get; }
        internal int StretchingVertices { get; }
        internal int RigidVertices { get; }
        internal float InnerLeftDelta { get; }
        internal float InnerRightDelta { get; }
        internal float OuterLeftDelta { get; }
        internal float OuterRightDelta { get; }
        internal int DroppedRailingVertices { get; }
    }

    internal sealed class TransformMap
    {
        private readonly int _vertices;
        private readonly byte[] _innerLayer;
        private readonly byte[] _stretching;
        private readonly byte[] _outerRailing;
        private readonly byte[] _coefficients;
        private readonly int _markedVertices;

        internal TransformMap(
            int vertices,
            string innerLayer,
            string stretching,
            string outerRailing,
            string coefficients)
        {
            _vertices = vertices;
            _innerLayer = Convert.FromBase64String(innerLayer);
            _stretching = Convert.FromBase64String(stretching);
            _outerRailing = Convert.FromBase64String(outerRailing);
            _coefficients = Convert.FromBase64String(coefficients);
            foreach (var bits in _stretching)
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
            && _innerLayer.Length == (vertices + 7) / 8
            && _stretching.Length == (vertices + 7) / 8
            && _outerRailing.Length == (vertices + 7) / 8
            && _coefficients.Length == _markedVertices * sizeof(float);

        internal bool IsInner(int vertex) =>
            (_innerLayer[vertex >> 3] & (1 << (vertex & 7))) != 0;

        internal bool Stretches(int vertex) =>
            (_stretching[vertex >> 3] & (1 << (vertex & 7))) != 0;

        internal bool IsOuterRailing(int vertex) =>
            (_outerRailing[vertex >> 3] & (1 << (vertex & 7))) != 0;

        internal float Coefficient(int index) =>
            BitConverter.ToSingle(_coefficients, index * sizeof(float));
    }
}
