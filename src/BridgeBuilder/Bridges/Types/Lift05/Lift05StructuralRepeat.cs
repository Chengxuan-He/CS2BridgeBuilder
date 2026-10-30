using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Continuous coordinates on existing faces; never creates a period boundary.
internal static class Lift05StructuralRepeat
{
    private static class Bounds
    {
        static Bounds()
        {
        }

        internal static readonly double[] Values = HardcodedNumbers.Load("Types_Lift05_ProfileRegion").F64;
    }

    internal static MovableStructureModel.Plan Apply(int lod, MovableStructureModel.Plan plan, SurfaceAsset? finishedSurface, Vector2 contentMin, Vector2 contentMax)
    {
        if (lod == Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[0])
            return plan;
        var data = Lift05StructuralWrapData.For(lod);
        int count = plan.SourcePositions.Length;
        if (count != data.VertexCount || plan.SourceIndices.Length != Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[1] || plan.SourceIndices[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[2]].Length != data.IndexCount || plan.PreparedDomains.Length != Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[4])
            throw new InvalidOperationException("Lift05 continuous repeat requires its measured original topology plan.");
        double Displacement(int v) => (double)plan.Positions[v].x - plan.SourcePositions[v].x;
        if (data.Faces.All(f => f.Indices.All(v => Displacement(v) == Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[5])))
            return plan;
        if (finishedSurface == null)
            throw new InvalidOperationException("Lift05 offline structural repeat dependency has not yet been bound.");
        if (contentMin.x != Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[6] || contentMax.x != Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[7] || !(contentMin.y >= Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[8] && contentMax.y <= Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[9] && contentMax.y > contentMin.y))
            throw new InvalidOperationException("Lift05 repeat requires one U period and measured finite V content bounds.");
        var offsets = new double[data.Profiles.Length];
        for (int i = Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[10]; i < offsets.Length; i++)
        {
            var p = data.Profiles[i];
            offsets[i] = p.Parent < Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[11] ? -p.Rate * Displacement(p.Anchor) : offsets[p.Parent] + data.Profiles[p.Parent].Rate * Displacement(p.ParentVertex) - p.Rate * Displacement(p.Vertex);
        }

        var recipe = MovableUvDomains.For(lod == Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[12] ? "LiftBridge05 Mesh" : "LiftBridge05_LOD1 Mesh", Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[13]) ?? throw new InvalidOperationException("Lift05 source domain metadata is missing.");
        var faces = data.Faces.ToDictionary(f => f.Triangle);
        var fragments = plan.Fragments.ToDictionary(f => f.Triangle);
        var prepared = new List<MovableStructureModel.PreparedDomain>();
        var bounds = Bounds.Values;
        foreach (int domain in data.Domains)
        {
            var values = new Dictionary<int, Vector2>();
            foreach (int triangle in recipe.Items[domain].Faces)
            {
                if (!faces.TryGetValue(triangle, out var face))
                    throw new InvalidOperationException("Lift05 derivative domain extends beyond measured repeating profiles.");
                var profile = data.Profiles[face.Profile];
                if (profile.MinU != bounds[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[14]] || profile.MaxU != bounds[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[15]])
                    throw new InvalidOperationException("Lift05 prepared U period differs from the native profile.");
                for (int k = Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[16]; k < Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[17]; k++)
                    if (plan.SourceIndices[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[18]][triangle * Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[19] + k] != face.Indices[k])
                        throw new InvalidOperationException("Lift05 source face identity changed.");
                var ids = fragments.TryGetValue(triangle, out var fragment) ? fragment.Indices : plan.RemoveTriangles[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[20]][triangle] ? Array.Empty<int>() : face.Indices;
                foreach (int vertex in ids)
                {
                    var weights = new double[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[21]];
                    void Add(int source, double weight)
                    {
                        if (weight == Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[22])
                            return;
                        int local = Array.IndexOf(face.Indices, source);
                        if (local < Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[23])
                            throw new InvalidOperationException("Lift05 existing fragment leaves its original source face.");
                        weights[local] += weight;
                    }

                    if (vertex < count)
                        Add(vertex, Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[24]);
                    else
                    {
                        var c = plan.Construction(vertex - count);
                        Add(c.A, Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.F64[0] - c.WeightB - c.WeightC);
                        Add(c.B, c.WeightB);
                        Add(c.C, c.WeightC);
                    }

                    double u = Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[25], v = Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[26], x = Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[27];
                    for (int k = Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[28]; k < Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[29]; k++)
                    {
                        u += weights[k] * face.UV[k].x;
                        v += weights[k] * face.UV[k].y;
                        x += weights[k] * plan.SourcePositions[face.Indices[k]].x;
                    }

                    var uv = new Vector2((float)((u - profile.MinU) / (profile.MaxU - profile.MinU) + profile.Rate * (plan.Positions[vertex].x - x) + offsets[face.Profile]), (float)(contentMin.y + (v - bounds[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[30]]) / (bounds[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[31]] - bounds[Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[32]]) * (contentMax.y - contentMin.y)));
                    if (values.TryGetValue(vertex, out var previous) && !previous.Equals(uv))
                        throw new InvalidOperationException("Lift05 shared domain vertex has inconsistent continuous phase.");
                    values[vertex] = uv;
                }
            }

            if (values.Count != Numeric_Types_Lift05_Lift05StructuralRepeat_cs.Values.I32[33])
                prepared.Add(new MovableStructureModel.PreparedDomain(domain, finishedSurface, values.OrderBy(p => p.Key).ToArray()));
        }

        return new MovableStructureModel.Plan(plan.SourcePositions, plan.Positions.Take(count).ToArray(), plan.SourceIndices, plan.RemoveTriangles, plan.AddedVertices, plan.Fragments, plan.InteriorVertices, plan.Positions.Skip(count).Select(p => p.x).ToArray(), plan.ReconstructsAuthoredPositions, plan.UvCorrections, preparedDomains: prepared.ToArray());
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05StructuralRepeat_cs
internal static class Numeric_Types_Lift05_Lift05StructuralRepeat_cs
{
    static Numeric_Types_Lift05_Lift05StructuralRepeat_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05StructuralRepeat_cs");
}
