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
/// Every top transverse truss stretches against its logical assembly's recorded span, including
/// separate diagonal, splice-plate, and end-fitting islands. Inner side arches and pier columns
/// translate rigidly. Outer section vertices use x' = x + sign(x) * delta, preserving the shape of
/// the deck base and railings while moving them to the complete visible road width. Both pillar
/// meshes belong to the inner bridge-pier assembly and receive the same displacement as the inner
/// arch. The mesh named <c>NetPillarBase</c> is a pier footing, not the deck base described by the
/// project contract.
/// </summary>
internal static class TrussArch02Geometry
{
    // Exact lateral spans measured from the shipped full-detail archetype by GeometryMetaprogram.
    internal static float PrototypeSectionOuterWidth => Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.F32[0];
    internal static float PrototypeSectionInnerLeft => Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.F32[1];
    internal static float PrototypeSectionInnerRight => Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.F32[2];
    internal static float PrototypeSectionInnerWidth => Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.F32[3];
    internal static float PrototypePillarInnerWidth => Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.F32[4];
    internal static float PrototypePillarOuterWidth => Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.F32[5];

    private static readonly IReadOnlyDictionary<string, TransformMap> Maps = TrussArch02GeometryData.Maps;
    internal static bool IsRecorded(string? styleId, string? meshName) => string.Equals(styleId, "TrussArch02", StringComparison.Ordinal) && !string.IsNullOrEmpty(meshName) && Maps.ContainsKey(meshName);
    internal static bool TryWidenSection(string meshName, float3[] source, float targetOuterWidth, float targetInnerLeft, float targetInnerRight, bool removeLeftOuterRailing, bool removeRightOuterRailing, out float3[] moved, out TransformFacts facts, out bool[]? dropped)
    {
        var outerDelta = (targetOuterWidth - PrototypeSectionOuterWidth) * Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.F32[6];
        var innerLeftDelta = targetInnerLeft - PrototypeSectionInnerLeft;
        var innerRightDelta = targetInnerRight - PrototypeSectionInnerRight;
        return TryApply(meshName, source, outerDelta, outerDelta, innerLeftDelta, innerRightDelta, removeLeftOuterRailing, removeRightOuterRailing, out moved, out facts, out dropped);
    }

    internal static bool TryWidenTowerPart(string meshName, float3[] source, float leftDelta, float rightDelta, out float3[] moved, out TransformFacts facts) => TryApply(meshName, source, leftDelta, rightDelta, leftDelta, rightDelta, removeLeftOuterRailing: false, removeRightOuterRailing: false, out moved, out facts, out _);
    private static bool TryApply(string meshName, float3[] source, float outerLeftDelta, float outerRightDelta, float innerLeftDelta, float innerRightDelta, bool removeLeftOuterRailing, bool removeRightOuterRailing, out float3[] moved, out TransformFacts facts, out bool[]? dropped)
    {
        moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        facts = default;
        dropped = null;
        if (!Maps.TryGetValue(meshName, out var map) || !map.Matches(source.Length))
            return false;
        var innerVertices = 0;
        var droppedRailingVertices = 0;
        for (int index = 0; index < source.Length; index++)
        {
            if (map.IsInner(index)) innerVertices++;
            if (map.IsOuterRailing(index) && ((source[index].x < 0f && removeLeftOuterRailing) ||
                (source[index].x > 0f && removeRightOuterRailing)))
            {
                dropped ??= new bool[source.Length];
                dropped[index] = true;
                droppedRailingVertices++;
            }
        }
        var output = moved;
        AffineGeometry.For(0, output.Length, index =>
        {
            bool inner = map.IsInner(index);
            float left = inner ? innerLeftDelta : outerLeftDelta;
            float right = inner ? innerRightDelta : outerRightDelta;
            float x = source[index].x;
            if (map.Stretches(index))
            {
                float coefficient = map.VertexCoefficient(index);
                output[index].x = AffineGeometry.Translate(x,
                    coefficient < 0f ? coefficient * left : coefficient > 0f ? coefficient * right : 0f);
            }
            else output[index].x = x < 0f ? AffineGeometry.Translate(x, -left) : x > 0f ? AffineGeometry.Translate(x, right) : x;
        });
        int stretchedVertices = map.CoefficientCount;
        facts = new TransformFacts(innerVertices, source.Length - innerVertices, stretchedVertices,
            source.Length - stretchedVertices, innerLeftDelta, innerRightDelta, outerLeftDelta, outerRightDelta, droppedRailingVertices);
        return true;
    }

    internal readonly struct TransformFacts
    {
        internal TransformFacts(int innerVertices, int outerVertices, int stretchingVertices, int rigidVertices, float innerLeftDelta, float innerRightDelta, float outerLeftDelta, float outerRightDelta, int droppedRailingVertices)
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
        private readonly float[] _vertexCoefficients;
        private readonly byte[] _innerLayer;
        private readonly byte[] _stretching;
        private readonly byte[] _outerRailing;
        private readonly byte[] _coefficients;
        private readonly int _markedVertices;
        internal TransformMap(int vertices, string innerLayer, string stretching, string outerRailing, string coefficients)
        {
            _vertices = vertices;
            _innerLayer = Convert.FromBase64String(innerLayer);
            _stretching = Convert.FromBase64String(stretching);
            _outerRailing = Convert.FromBase64String(outerRailing);
            _coefficients = Convert.FromBase64String(coefficients);
            foreach (var bits in _stretching)
            {
                var remaining = bits;
                while (remaining != Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[5])
                {
                    _markedVertices += remaining & Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[6];
                    remaining >>= Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[7];
                }
            }
            _vertexCoefficients = new float[vertices];
            if (Matches(vertices))
            {
                int coefficient = 0;
                for (int vertex = 0; vertex < vertices; vertex++)
                    if (Stretches(vertex)) _vertexCoefficients[vertex] = Coefficient(coefficient++);
            }
        }

        internal float VertexCoefficient(int index) => _vertexCoefficients[index];
        internal int CoefficientCount => _coefficients.Length / sizeof(float);

        internal bool Matches(int vertices) => vertices == _vertices && _innerLayer.Length == (vertices + Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[8]) / Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[9] && _stretching.Length == (vertices + Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[10]) / Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[11] && _outerRailing.Length == (vertices + Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[12]) / Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[13] && _coefficients.Length == _markedVertices * sizeof(float);
        internal bool IsInner(int vertex) => (_innerLayer[vertex >> Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[14]] & (Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[15] << (vertex & Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[16]))) != Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[17];
        internal bool Stretches(int vertex) => (_stretching[vertex >> Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[18]] & (Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[19] << (vertex & Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[20]))) != Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[21];
        internal bool IsOuterRailing(int vertex) => (_outerRailing[vertex >> Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[22]] & (Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[23] << (vertex & Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[24]))) != Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[25];
        internal float Coefficient(int index) => BitConverter.ToSingle(_coefficients, index * sizeof(float));
    }
}

// Embedded numerical arrays: Types_TrussArch02_TrussArch02Geometry_cs
internal static class Numeric_Types_TrussArch02_TrussArch02Geometry_cs
{
    static Numeric_Types_TrussArch02_TrussArch02Geometry_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_TrussArch02_TrussArch02Geometry_cs");
}
