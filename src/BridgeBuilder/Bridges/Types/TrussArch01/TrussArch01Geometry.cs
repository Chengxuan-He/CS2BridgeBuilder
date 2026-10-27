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

    private static readonly IReadOnlyDictionary<string, PortalMap> SectionMaps = TrussArch01SectionData.Maps;
    private static readonly IReadOnlyDictionary<string, PortalMap> PierMaps = TrussArch01PierData.Maps;
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
        var shift = extra * Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.F32[3];
        var output = moved;
        AffineGeometry.For(0, output.Length, index =>
        {
            output[index].x = map.Stretches(index)
                ? AffineGeometry.AddScaled(source[index].x, map.VertexCoefficient(index), shift)
                : TowerWidening.Spread(source[index].x, extra);
        });
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
                while (remaining != Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[2])
                {
                    _markedVertices += remaining & Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[3];
                    remaining >>= Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[4];
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

        internal bool Matches(int vertices) => vertices == _vertices && _membership.Length == (vertices + Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[5]) / Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[6] && _coefficients.Length == _markedVertices * sizeof(float);
        internal bool Stretches(int vertex) => (_membership[vertex >> Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[7]] & (Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[8] << (vertex & Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[9]))) != Numeric_Types_TrussArch01_TrussArch01Geometry_cs.Values.I32[10];
        internal float Coefficient(int index) => BitConverter.ToSingle(_coefficients, index * sizeof(float));
    }
}

// Embedded numerical arrays: Types_TrussArch01_TrussArch01Geometry_cs
internal static class Numeric_Types_TrussArch01_TrussArch01Geometry_cs
{
    static Numeric_Types_TrussArch01_TrussArch01Geometry_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_TrussArch01_TrussArch01Geometry_cs");
}
