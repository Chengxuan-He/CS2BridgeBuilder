using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01DeckTrusses
{
    private sealed class Recipe
    {
        internal readonly float HalfSpan, YHalfSpan, HigherHalfSpan, EndHalfSpan;
        internal readonly HashSet<int> YMembers, HigherMembers, EndMembers;
        internal readonly int[] Center, Edge, Copies, AddedCopies;
        internal readonly (int Face, int[] Indices)[] Fragments;
        internal Recipe(string key)
        {
            var data = HardcodedNumbers.Load(key);
            HalfSpan = data.F32[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[0]];
            YHalfSpan = data.F32[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[1]];
            HigherHalfSpan = data.F32[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[2]];
            EndHalfSpan = data.F32[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[3]];
            var values = data.I32;
            var offset = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[4]; // Recipe header: four vertex lists, fragment count and complete Y membership.
            int[] Read(int count)
            {
                var result = new int[count];
                Array.Copy(values, offset, result, Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[5], count);
                offset += count;
                return result;
            }

            Center = Read(values[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[6]]);
            Edge = Read(values[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[7]]);
            Copies = Read(values[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[8]]);
            AddedCopies = Read(values[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[9]]);
            Fragments = new (int, int[])[values[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[10]]];
            for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[11]; i < Fragments.Length; i++)
            {
                var face = values[offset++];
                var count = values[offset++];
                Fragments[i] = (face, Read(count));
            }

            YMembers = new HashSet<int>(Read(values[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[12]]));
            HigherMembers = new HashSet<int>(Read(values[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[13]]));
            EndMembers = new HashSet<int>(Read(values[Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[14]]));
            if (offset != values.Length)
                throw new InvalidOperationException("Lift01 deck truss recipe length mismatch.");
        }
    }

    private static class Near
    {
        internal static Recipe Value => GenerationParameters.Get<Recipe>("BridgeBuilder.Bridges.Lift01DeckTrusses.Near.Value", () => new("Types_Lift01_DeckTruss0"));
    }

    private static class Middle
    {
        internal static Recipe Value => GenerationParameters.Get<Recipe>("BridgeBuilder.Bridges.Lift01DeckTrusses.Middle.Value", () => new("Types_Lift01_DeckTruss1"));
    }

    internal static MovableStructureModel.Plan Complete(MovableStructureModel.Plan plan, int lod, float delta)
    {
        var recipe = lod switch
        {
            0 => Near.Value,
            1 => Middle.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
        if (plan.InteriorVertices.Length != Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[15] || plan.SourceCopies.Length != Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[16])
            throw new InvalidOperationException("Lift01 deck trusses require the original edge-construction plan.");
        var source = plan.SourcePositions;
        var positions = plan.Positions.Take(source.Length).ToArray();
        var scale = (recipe.HalfSpan + delta) / recipe.HalfSpan;
        var yScale = (recipe.YHalfSpan + delta) / recipe.YHalfSpan;
        var higherScale = (recipe.HigherHalfSpan + delta) / recipe.HigherHalfSpan;
        var endScale = (recipe.EndHalfSpan + delta) / recipe.EndHalfSpan;
        float Scale(int i) => recipe.EndMembers.Contains(i) ? endScale : recipe.YMembers.Contains(i) ? yScale : recipe.HigherMembers.Contains(i) ? higherScale : scale;
        var centerScales = Array.ConvertAll(recipe.Center, Scale);
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.RowScaleX, new[] { source, positions }, new[] { centerScales }, Array.Empty<float>(), count: recipe.Center.Length, indices: new[] { recipe.Center });
        var edgeSides = Array.ConvertAll(recipe.Edge, i => (float)Math.Sign(source[i].x));
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.RowDelta, new[] { source, positions }, new[] { edgeSides }, new[] { delta }, count: recipe.Edge.Length, indices: new[] { recipe.Edge });
        var added = plan.AddedVertices.Concat(recipe.AddedCopies.Select(i => plan.AddedVertices[i])).ToArray();
        var constructed = new float[added.Length + recipe.Copies.Length];
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[17]; i < plan.AddedVertices.Length; i++)
            constructed[i] = plan.Positions[source.Length + i].x;
        var a = new int[recipe.AddedCopies.Length];
        var b = new int[a.Length];
        var outputs = new int[a.Length];
        var fractions = new float[a.Length];
        var scales = new float[a.Length];
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[18]; i < recipe.AddedCopies.Length; i++)
        {
            var edge = plan.AddedVertices[recipe.AddedCopies[i]];
            a[i] = edge.A;
            b[i] = edge.B;
            fractions[i] = edge.Fraction;
            outputs[i] = plan.AddedVertices.Length + i;
            scales[i] = Scale(source.Length + recipe.AddedCopies[i]);
        }

        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.EdgeScaledScalar, new[] { source }, new[] { fractions, scales, constructed }, Array.Empty<float>(), count: a.Length, indices: new[] { a, b, outputs });
        var copyOutputs = new int[recipe.Copies.Length];
        var copyScales = new float[recipe.Copies.Length];
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[19]; i < recipe.Copies.Length; i++)
        {
            copyOutputs[i] = added.Length + i;
            copyScales[i] = Scale(recipe.Copies[i]);
        }

        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.ScaledScalar, new[] { source }, new[] { copyScales, constructed }, Array.Empty<float>(), count: recipe.Copies.Length, indices: new[] { recipe.Copies, copyOutputs });
        var replaced = new HashSet<int>(recipe.Fragments.Select(f => f.Face));
        var fragments = plan.Fragments.Where(f => !replaced.Contains(f.Triangle)).ToList();
        foreach (var fragment in recipe.Fragments)
        {
            var indices = fragment.Indices.Select(i => i >= Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[20] ? i : -i - Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[21] < recipe.Copies.Length ? source.Length + added.Length - i - Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[22] : source.Length + plan.AddedVertices.Length - i - Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[23] - recipe.Copies.Length).ToArray();
            fragments.Add(new MovableStructureModel.Fragment(Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[24], fragment.Face, indices));
        }

        Matrix4x4 Frame(int i)
        {
            var matrix = Matrix4x4.identity;
            matrix.m00 = Scale(i);
            return matrix;
        }

        var frames = new List<KeyValuePair<int, Matrix4x4>>(plan.FrameTransforms);
        foreach (var i in recipe.Center)
            frames.Add(new(i, Frame(i)));
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[25]; i < recipe.AddedCopies.Length; i++)
            frames.Add(new(source.Length + plan.AddedVertices.Length + i, Frame(source.Length + recipe.AddedCopies[i])));
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[26]; i < recipe.Copies.Length; i++)
            frames.Add(new(source.Length + added.Length + i, Frame(recipe.Copies[i])));
        return new MovableStructureModel.Plan(source, positions, plan.SourceIndices, plan.RemoveTriangles, added, fragments.ToArray(), constructedX: constructed, reconstructsAuthoredPositions: plan.ReconstructsAuthoredPositions, uvCorrections: plan.UvCorrections, preparedDomains: plan.PreparedDomains, frameTransforms: frames.ToArray(), sourceCopies: recipe.Copies);
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01DeckTrusses_cs
internal static class Numeric_Types_Lift01_Lift01DeckTrusses_cs
{
    static Numeric_Types_Lift01_Lift01DeckTrusses_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values", () => HardcodedNumbers.Load("Types_Lift01_Lift01DeckTrusses_cs"));
}
