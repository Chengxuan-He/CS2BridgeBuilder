using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw03DetailedStructure
{
    // Near and middle main mesh only. The merged far mesh has its own authored parts.
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        if (lod != 0 && lod != 1) throw new ArgumentOutOfRangeException(nameof(lod));
        var positions = (Vector3[])source.Clone();
        Draw03RigidParts.Apply(lod, source, positions, delta);
        Draw03Shafts.Apply(lod, source, positions, delta);
        Draw03CentreMembers.Apply(lod, source, positions, delta);
        Draw03Bases.Apply(lod, source, positions, delta);
        Draw03EndStrips.Apply(lod, source, positions, delta);
        var structure = Draw03FixedPlatforms.Complete(lod, source, positions, indices, delta);
        return Draw03RoadReplacement.Apply(lod, structure, delta);
    }
}
