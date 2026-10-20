using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>Combines the disjoint measured Lift05 main-mesh width plans.</summary>
internal static class Lift05StructureWidthPlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var stages = new[]
        {
            Lift05RoofRings.Create(lod, source, indices, delta),
            Lift05MovingShell.Create(lod, source, indices, delta),
            Lift05TowerShells.Create(lod, source, indices, delta)
        };
        var positions = (Vector3[])source.Clone();
        var edges = new List<MovableStructureModel.EdgeVertex>();
        var constructedX = new List<float>();
        var fragments = new List<MovableStructureModel.Fragment>();
        var removed = new bool[indices.Length / 3];
        foreach (var stage in stages)
        {
            // These measured plans only add edge vertices. Their source-vertex and
            // source-face memberships are disjoint, verified by the offline audit.
            if (stage.InteriorVertices.Length != 0)
                throw new InvalidOperationException("Lift05 width composition requires edge-only structural plans.");
            for (var i = 0; i < source.Length; i++)
                if (stage.Positions[i].x != source[i].x)
                    positions[i].x = stage.Positions[i].x;
            var offset = edges.Count;
            edges.AddRange(stage.AddedVertices);
            for (var i = 0; i < stage.ConstructedCount; i++)
                constructedX.Add(stage.Positions[source.Length + i].x);
            foreach (var fragment in stage.Fragments)
            {
                var remapped = (int[])fragment.Indices.Clone();
                for (var i = 0; i < remapped.Length; i++)
                    if (remapped[i] >= source.Length) remapped[i] += offset;
                fragments.Add(new MovableStructureModel.Fragment(fragment.Slot, fragment.Triangle, remapped));
            }
            for (var i = 0; i < removed.Length; i++)
                removed[i] |= stage.RemoveTriangles[0][i];
        }
        Lift05RigidParts.Apply(lod, source, positions, delta);
        Lift05CounterweightGeometry.Apply(lod, source, positions, delta);
        Lift05TransverseParts.Apply(lod, source, positions, delta);
        Lift05VBraces.Apply(lod, source, positions, delta);
        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { removed },
            addedVertices: edges.ToArray(), fragments: fragments.ToArray(), constructedX: constructedX.ToArray());
    }
}
