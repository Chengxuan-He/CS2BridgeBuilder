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

    private static IReadOnlyDictionary<string, TransformMap> Maps => GenerationParameters.Get<IReadOnlyDictionary<string, TransformMap>>("BridgeBuilder.Bridges.TrussArch02Geometry.Maps", () => TrussArch02GeometryData.Maps);
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
            if (map.IsInner(index))
                innerVertices++;
            if (map.IsOuterRailing(index) && ((source[index].x < 0f && removeLeftOuterRailing) || (source[index].x > 0f && removeRightOuterRailing)))
            {
                dropped ??= new bool[source.Length];
                dropped[index] = true;
                droppedRailingVertices++;
            }
        }

        var groups = new List<int>[9];
        for (int group = 0; group < groups.Length; group++) groups[group] = new List<int>();
        for (int index = 0; index < moved.Length; index++)
        {
            bool inner = map.IsInner(index);
            float x = source[index].x;
            if (map.Stretches(index))
            {
                float coefficient = map.VertexCoefficient(index);
                if (coefficient < 0f) groups[inner ? 0 : 2].Add(index);
                else if (coefficient > 0f) groups[inner ? 1 : 3].Add(index);
                else groups[8].Add(index);
            }
            else
            {
                if (x < 0f) groups[inner ? 4 : 6].Add(index);
                else if (x > 0f) groups[inner ? 5 : 7].Add(index);
            }
        }
        var deltas = new[] { innerLeftDelta, innerRightDelta, outerLeftDelta, outerRightDelta };
        for (int group = 0; group < 4; group++)
            AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.DeltaTable, new[] { source, moved },
                new[] { map.Coefficients }, new[] { deltas[group] }, count: groups[group].Count, indices: new[] { groups[group].ToArray() });
        for (int group = 4; group < 8; group++)
            AffineGeometry.Execute(StructurePrograms.Data, group % 2 == 0 ? StructurePrograms.NegativeTranslateX : StructurePrograms.TranslateX,
                new[] { source, moved }, Array.Empty<float[]>(), new[] { deltas[group - 4] },
                count: groups[group].Count, indices: new[] { groups[group].ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.TranslateX, new[] { source, moved },
            Array.Empty<float[]>(), new[] { 0f }, count: groups[8].Count, indices: new[] { groups[8].ToArray() });
        int stretchedVertices = map.CoefficientCount;
        facts = new TransformFacts(innerVertices, source.Length - innerVertices, stretchedVertices, source.Length - stretchedVertices, innerLeftDelta, innerRightDelta, outerLeftDelta, outerRightDelta, droppedRailingVertices);
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
                while (remaining != Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[0])
                {
                    _markedVertices += remaining & Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[1];
                    remaining >>= Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[2];
                }
            }

            _vertexCoefficients = new float[vertices];
            if (Matches(vertices))
            {
                int coefficient = 0;
                for (int vertex = 0; vertex < vertices; vertex++)
                    if (Stretches(vertex))
                        _vertexCoefficients[vertex] = Coefficient(coefficient++);
            }
        }

        internal float VertexCoefficient(int index) => _vertexCoefficients[index];
        internal float[] Coefficients => _vertexCoefficients;
        internal int CoefficientCount => _coefficients.Length / sizeof(float);

        internal bool Matches(int vertices) => vertices == _vertices && _innerLayer.Length == (vertices + Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[3]) / Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[4] && _stretching.Length == (vertices + Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[5]) / Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[6] && _outerRailing.Length == (vertices + Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[7]) / Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[8] && _coefficients.Length == _markedVertices * sizeof(float);
        internal bool IsInner(int vertex) => (_innerLayer[vertex >> Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[9]] & (Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[10] << (vertex & Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[11]))) != Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[12];
        internal bool Stretches(int vertex) => (_stretching[vertex >> Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[13]] & (Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[14] << (vertex & Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[15]))) != Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[16];
        internal bool IsOuterRailing(int vertex) => (_outerRailing[vertex >> Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[17]] & (Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[18] << (vertex & Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[19]))) != Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values.I32[20];
        internal float Coefficient(int index) => BitConverter.ToSingle(_coefficients, index * sizeof(float));
    }
}

// Embedded numerical arrays: Types_TrussArch02_TrussArch02Geometry_cs
internal static class Numeric_Types_TrussArch02_TrussArch02Geometry_cs
{
    static Numeric_Types_TrussArch02_TrussArch02Geometry_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_TrussArch02_TrussArch02Geometry_cs.Values", () => HardcodedNumbers.Load("Types_TrussArch02_TrussArch02Geometry_cs"));
}
