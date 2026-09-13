using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Applies the reviewed full-detail layer assignment of the Grand Bridge cable and support meshes.
///
/// The archetype has two independent lateral planes. Its outer longitudinal edge follows the road
/// surface edge; its inner suspension cable, hangers, and inner longitudinal edge follow the boundary
/// between the outermost Sidewalk section and the road interior. The metaprogram identifies every
/// vertex belonging to the inner plane once. Runtime performs only an exact mesh-name lookup and
/// applies those immutable maps; it never tries to rediscover a layer from coordinates or topology.
/// </summary>
internal static class GrandBridgeGeometry
{
    // Authored lateral centre lines measured from the Grand Bridge full-detail prototype. The donor
    // road is 19 m wide, so its outer edge is 9.5 m from x=0. The inner cable/hanger plane is 6.5 m
    // from x=0, exactly the donor's Sidewalk inner boundary.
    private const float PrototypeOuter = 9.5f;
    private const float PrototypeInner = 6.5f;
    private const float PrototypeSupportInner = 4f;

    private static readonly IReadOnlyDictionary<string, LayerMap> CableMaps =
        GrandBridgeGeometryData.CableMaps;
    private static readonly IReadOnlyDictionary<string, CorrectionMap> SupportMaps =
        GrandBridgeGeometryData.SupportMaps;

    internal static bool IsRecordedCable(string? styleId, string? meshName) =>
        string.Equals(styleId, "Grand", StringComparison.Ordinal)
        && !string.IsNullOrEmpty(meshName)
        && CableMaps.ContainsKey(meshName);

    internal static bool IsRecordedSupport(string? styleId, string? meshName) =>
        string.Equals(styleId, "Grand", StringComparison.Ordinal)
        && !string.IsNullOrEmpty(meshName)
        && SupportMaps.ContainsKey(meshName);

    internal static bool TryAlignCableLayers(
        string meshName,
        float3[] source,
        float targetOuterLeft,
        float targetOuterRight,
        float targetInnerLeft,
        float targetInnerRight,
        out float3[] moved,
        out TransformFacts facts)
    {
        moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        facts = default;
        if (!CableMaps.TryGetValue(meshName, out var map) || !map.Matches(source.Length)) return false;

        var outerLeftDelta = targetOuterLeft - PrototypeOuter;
        var outerRightDelta = targetOuterRight - PrototypeOuter;
        var innerLeftDelta = targetInnerLeft - PrototypeInner;
        var innerRightDelta = targetInnerRight - PrototypeInner;
        var innerVertices = 0;
        for (var index = 0; index < moved.Length; index++)
        {
            var inner = map.IsInner(index);
            var leftDelta = inner ? innerLeftDelta : outerLeftDelta;
            var rightDelta = inner ? innerRightDelta : outerRightDelta;
            if (inner) innerVertices++;

            moved[index].x = source[index].x < 0f
                ? source[index].x - leftDelta
                : source[index].x > 0f
                    ? source[index].x + rightDelta
                    : source[index].x;
        }

        facts = new TransformFacts(
            innerVertices,
            source.Length - innerVertices,
            innerLeftDelta,
            innerRightDelta,
            outerLeftDelta,
            outerRightDelta);
        return true;
    }

    internal static bool TryAlignSupportInnerEdge(
        string meshName,
        float3[] source,
        float3[] genericallyMoved,
        float targetOuterLeft,
        float targetOuterRight,
        float targetInnerLeft,
        float targetInnerRight,
        out float3[] moved,
        out TransformFacts facts)
    {
        moved = new float3[genericallyMoved.Length];
        Array.Copy(genericallyMoved, moved, genericallyMoved.Length);
        facts = default;
        if (source.Length != genericallyMoved.Length
            || !SupportMaps.TryGetValue(meshName, out var map)
            || !map.Matches(source.Length))
            return false;

        var outerLeftDelta = targetOuterLeft - PrototypeOuter;
        var outerRightDelta = targetOuterRight - PrototypeOuter;
        var innerLeftCorrection = targetInnerLeft - (PrototypeSupportInner + outerLeftDelta);
        var innerRightCorrection = targetInnerRight - (PrototypeSupportInner + outerRightDelta);
        var correctedVertices = 0;
        var coefficientOffset = 0;
        for (var index = 0; index < moved.Length; index++)
        {
            if (!map.IsCorrected(index)) continue;
            var coefficient = map.Coefficient(coefficientOffset++);
            moved[index].x += source[index].x < 0f
                ? -coefficient * innerLeftCorrection
                : source[index].x > 0f
                    ? coefficient * innerRightCorrection
                    : 0f;
            correctedVertices++;
        }

        facts = new TransformFacts(
            correctedVertices,
            source.Length - correctedVertices,
            innerLeftCorrection,
            innerRightCorrection,
            outerLeftDelta,
            outerRightDelta);
        return coefficientOffset == map.CoefficientCount;
    }

    internal readonly struct TransformFacts
    {
        internal TransformFacts(
            int innerVertices,
            int outerVertices,
            float innerLeftDelta,
            float innerRightDelta,
            float outerLeftDelta,
            float outerRightDelta)
        {
            InnerVertices = innerVertices;
            OuterVertices = outerVertices;
            InnerLeftDelta = innerLeftDelta;
            InnerRightDelta = innerRightDelta;
            OuterLeftDelta = outerLeftDelta;
            OuterRightDelta = outerRightDelta;
        }

        internal int InnerVertices { get; }
        internal int OuterVertices { get; }
        internal float InnerLeftDelta { get; }
        internal float InnerRightDelta { get; }
        internal float OuterLeftDelta { get; }
        internal float OuterRightDelta { get; }
    }

    internal sealed class LayerMap
    {
        private readonly int _vertices;
        private readonly byte[] _innerLayer;

        internal LayerMap(int vertices, string innerLayer)
        {
            _vertices = vertices;
            _innerLayer = Convert.FromBase64String(innerLayer);
        }

        internal bool Matches(int vertices) =>
            vertices == _vertices && _innerLayer.Length == (vertices + 7) / 8;

        internal bool IsInner(int vertex) =>
            (_innerLayer[vertex >> 3] & (1 << (vertex & 7))) != 0;
    }

    internal sealed class CorrectionMap
    {
        private readonly int _vertices;
        private readonly byte[] _membership;
        private readonly byte[] _coefficients;
        private readonly int _markedVertices;

        internal CorrectionMap(int vertices, string membership, string coefficients)
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

        internal bool IsCorrected(int vertex) =>
            (_membership[vertex >> 3] & (1 << (vertex & 7))) != 0;

        internal float Coefficient(int index) =>
            BitConverter.ToSingle(_coefficients, index * sizeof(float));
    }
}
