using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift04StructureWidthPlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var positions = (Vector3[])source.Clone();
        Lift04RigidParts.Apply(lod, source, positions, delta);
        Lift04Crossbeams.Apply(lod, source, positions, delta);
        Lift04DeckBase.Apply(lod, source, positions, delta);
        Lift04Machinery.Apply(lod, source, positions, delta);
        Lift04TowerFrames.Apply(lod, source, positions, delta);
        Lift04Connectors.Apply(lod, source, positions, delta);
        Lift04ColumnCapitals.Apply(lod, source, positions, delta);
        Lift04UpperShells.Apply(lod, source, positions, delta);
        Lift04FixedBodies.Apply(lod, source, positions, delta);
        if (lod < 2) Lift04MovingSideFrames.ApplyDetailed(lod, source, positions, delta);
        var endFrames = Lift04MovingEndFrames.Complete(lod, source, positions, indices, delta);
        if (lod == 2) return endFrames;

        // Both measured recipes use original vertex indices. Append only their
        // constructed vertices, remapping the upper details' fragment indices.
        var upper = Lift04UpperDetails.Complete(lod, source, positions, indices, delta);
        var offset = endFrames.AddedVertices.Length;
        var fragments = upper.Fragments.Select(f => new MovableStructureModel.Fragment(
            f.Slot, f.Triangle, f.Indices.Select(i => i < source.Length ? i : i + offset).ToArray()));
        var x = endFrames.Positions.Skip(source.Length).Select(p => p.x)
            .Concat(upper.Positions.Skip(source.Length).Select(p => p.x)).ToArray();
        return new MovableStructureModel.Plan(source, positions, new[] { indices },
            new[] { new bool[indices.Length / 3] },
            endFrames.AddedVertices.Concat(upper.AddedVertices).ToArray(),
            endFrames.Fragments.Concat(fragments).ToArray(), constructedX: x);
    }
}
