using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;
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
    internal static float PrototypeSectionWidth => Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.F32[0];
    internal static float PrototypePierWidth => Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.F32[1];
    internal static float PrototypeBaseWidth => Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.F32[2];

    private static IReadOnlyDictionary<string, PortalMap> SectionMaps => GenerationParameters.Get<IReadOnlyDictionary<string, PortalMap>>("BridgeBuilder.Bridges.TrussArch01Geometry.SectionMaps", () => TrussArch01SectionData.Maps);
    private static IReadOnlyDictionary<string, PortalMap> PierMaps => GenerationParameters.Get<IReadOnlyDictionary<string, PortalMap>>("BridgeBuilder.Bridges.TrussArch01Geometry.PierMaps", () => TrussArch01PierData.Maps);
    internal static float PierExtraForSection(float sectionExtra) => sectionExtra + PrototypeSectionWidth - PrototypePierWidth;
    internal static bool TryWidenSection(string meshName, float3[] source, float extra, out float3[] moved) => TryApply(SectionMaps, meshName, source, extra, out moved);
    internal static bool TryWidenPier(string meshName, float3[] source, float extra, out float3[] moved) => TryApply(PierMaps, meshName, source, extra, out moved);
    private static bool TryApply(IReadOnlyDictionary<string, PortalMap> maps, string meshName, float3[] source, float extra, out float3[] moved)
    {
        moved = new float3[source.Length];
        Array.Copy(source, moved, source.Length);
        if (!maps.TryGetValue(meshName, out var map) || !map.Matches(source.Length))
            return false;
        if (Math.Abs(extra) < TowerWidening.CentreEpsilon)
            return true;
        var stretch = new List<int>();
        var positive = new List<int>();
        var negative = new List<int>();
        for (int index = 0; index < source.Length; index++)
        {
            if (map.Stretches(index))
                stretch.Add(index);
            else if (source[index].x > TowerWidening.CentreEpsilon)
                positive.Add(index);
            else if (!(Math.Abs(source[index].x) <= TowerWidening.CentreEpsilon))
                negative.Add(index);
        }

        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.HalfWidthTable, new[] { source, moved }, new[] { map.Coefficients }, new[] { extra }, count: stretch.Count, indices: new[] { stretch.ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.PositiveHalfX, new[] { source, moved }, Array.Empty<float[]>(), new[] { extra }, count: positive.Count, indices: new[] { positive.ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.NegativeHalfX, new[] { source, moved }, Array.Empty<float[]>(), new[] { extra }, count: negative.Count, indices: new[] { negative.ToArray() });
        return true;
    }

    internal sealed class PortalMap
    {
        private readonly int _vertices;
        private readonly float[] _vertexCoefficients;
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
                while (remaining != Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[0])
                {
                    _markedVertices += remaining & Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[1];
                    remaining >>= Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[2];
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

        internal bool Matches(int vertices) => vertices == _vertices && _membership.Length == (vertices + Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[3]) / Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[4] && _coefficients.Length == _markedVertices * sizeof(float);
        internal bool Stretches(int vertex) => (_membership[vertex >> Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[5]] & (Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[6] << (vertex & Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[7]))) != Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[8];
        internal float Coefficient(int index) => BitConverter.ToSingle(_coefficients, index * sizeof(float));
    }
}

// Embedded numerical arrays: Types_TrussArch01_TrussArch01Geometry_cs
internal static class Numeric_Types_TrussArch01_TrussArch01Geometry_cs
{
    static Numeric_Types_TrussArch01_TrussArch01Geometry_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values", () => HardcodedNumbers.Load("Types_TrussArch01_TrussArch01Geometry_cs"));
}
