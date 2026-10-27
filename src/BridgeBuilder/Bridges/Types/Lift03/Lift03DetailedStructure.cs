using System;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift03DetailedStructure
{
    // Detailed measured families only. Selected-sidewalk inclusion must be
    // completed before this is used by the final render assembler.
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float structureDelta, float upperDelta, float lowerDelta)
    {
        if (lod != Numeric_Types_Lift03_Lift03DetailedStructure_cs.Values.I32[0] && lod != Numeric_Types_Lift03_Lift03DetailedStructure_cs.Values.I32[1])
            throw new ArgumentOutOfRangeException(nameof(lod));
        var positions = (Vector3[])source.Clone();
        Lift03RigidParts.Apply(lod, source, positions, structureDelta);
        Lift03DeckBases.Apply(lod, source, positions, upperDelta, lowerDelta);
        Lift03DeckRailings.Apply(lod, source, positions, upperDelta, lowerDelta);
        Lift03CrossingMembers.Apply(lod, source, positions, upperDelta, lowerDelta);
        var parts = new[]
        {
            Lift03Frames.Complete(lod, source, positions, indices, structureDelta),
            Lift03EndBeams.Complete(lod, source, positions, indices, structureDelta),
            Lift03JoinedShells.Complete(lod, source, positions, indices, structureDelta),
            Lift03TallSupports.Complete(lod, source, positions, indices, structureDelta),
            Lift03SlopedSupports.Complete(lod, source, positions, indices, structureDelta)
        };
        var removed = new bool[indices.Length / Numeric_Types_Lift03_Lift03DetailedStructure_cs.Values.I32[2]];
        Lift03RoadTops.RemoveDetailed(lod, indices, removed);
        Lift03TrackRemoval.RemoveDetailed(lod, indices, removed);
        return Combine(source, positions, indices, removed, parts);
    }

    internal static MovableStructureModel.Plan Combine(Vector3[] source, Vector3[] positions, int[] indices, bool[] removed, params MovableStructureModel.Plan[] parts)
    {
        var edges = parts.SelectMany(part => part.AddedVertices).ToArray();
        var interiors = parts.SelectMany(part => part.InteriorVertices).ToArray();
        var fragments = new System.Collections.Generic.List<MovableStructureModel.Fragment>();
        var constructedX = new float[edges.Length + interiors.Length];
        var edgeOffset = Numeric_Types_Lift03_Lift03DetailedStructure_cs.Values.I32[3];
        var interiorOffset = Numeric_Types_Lift03_Lift03DetailedStructure_cs.Values.I32[4];
        foreach (var part in parts)
        {
            // Each input plan numbers its edges then interiors. The combined plan
            // must put ALL edges first, so a single concatenation offset is wrong.
            int Remap(int index)
            {
                if (index < source.Length)
                    return index;
                var local = index - source.Length;
                return source.Length + (local < part.AddedVertices.Length ? edgeOffset + local : edges.Length + interiorOffset + local - part.AddedVertices.Length);
            }

            for (var i = Numeric_Types_Lift03_Lift03DetailedStructure_cs.Values.I32[5]; i < part.ConstructedCount; i++)
                constructedX[Remap(source.Length + i) - source.Length] = part.Positions[source.Length + i].x;
            foreach (var fragment in part.Fragments)
                fragments.Add(new MovableStructureModel.Fragment(fragment.Slot, fragment.Triangle, fragment.Indices.Select(Remap).ToArray()));
            edgeOffset += part.AddedVertices.Length;
            interiorOffset += part.InteriorVertices.Length;
        }

        foreach (var fragment in fragments)
            if (removed[fragment.Triangle])
                throw new InvalidOperationException("Lift03 structural fragments overlap an excluded source face.");
        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { removed }, addedVertices: edges, fragments: fragments.ToArray(), interiorVertices: interiors, constructedX: constructedX);
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03DetailedStructure_cs
internal static class Numeric_Types_Lift03_Lift03DetailedStructure_cs
{
    static Numeric_Types_Lift03_Lift03DetailedStructure_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03DetailedStructure_cs");
}
