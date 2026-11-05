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
        internal static readonly Recipe Value = new("Types_Lift01_DeckTruss0");
    }

    private static class Middle
    {
        internal static readonly Recipe Value = new("Types_Lift01_DeckTruss1");
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
        AffineGeometry.For(Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[17], recipe.Center.Length, j =>
        {
            var i = recipe.Center[j];
            positions[i].x = source[i].x * Scale(i);
        });
        AffineGeometry.For(Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[18], recipe.Edge.Length, j =>
        {
            var i = recipe.Edge[j];
            positions[i].x = source[i].x + Math.Sign(source[i].x) * delta;
        });
        var added = plan.AddedVertices.Concat(recipe.AddedCopies.Select(i => plan.AddedVertices[i])).ToArray();
        var constructed = new float[added.Length + recipe.Copies.Length];
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[19]; i < plan.AddedVertices.Length; i++)
            constructed[i] = plan.Positions[source.Length + i].x;
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[20]; i < recipe.AddedCopies.Length; i++)
        {
            var edge = plan.AddedVertices[recipe.AddedCopies[i]];
            constructed[plan.AddedVertices.Length + i] = Mathf.LerpUnclamped(source[edge.A].x, source[edge.B].x, edge.Fraction) * Scale(source.Length + recipe.AddedCopies[i]);
        }

        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[21]; i < recipe.Copies.Length; i++)
            constructed[added.Length + i] = source[recipe.Copies[i]].x * Scale(recipe.Copies[i]);
        var replaced = new HashSet<int>(recipe.Fragments.Select(f => f.Face));
        var fragments = plan.Fragments.Where(f => !replaced.Contains(f.Triangle)).ToList();
        foreach (var fragment in recipe.Fragments)
        {
            var indices = fragment.Indices.Select(i => i >= Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[22] ? i : -i - Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[23] < recipe.Copies.Length ? source.Length + added.Length - i - Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[24] : source.Length + plan.AddedVertices.Length - i - Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[25] - recipe.Copies.Length).ToArray();
            fragments.Add(new MovableStructureModel.Fragment(Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[26], fragment.Face, indices));
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
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[27]; i < recipe.AddedCopies.Length; i++)
            frames.Add(new(source.Length + plan.AddedVertices.Length + i, Frame(source.Length + recipe.AddedCopies[i])));
        for (var i = Numeric_Types_Lift01_Lift01DeckTrusses_cs.Values.I32[28]; i < recipe.Copies.Length; i++)
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

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift01_Lift01DeckTrusses_cs");
}
