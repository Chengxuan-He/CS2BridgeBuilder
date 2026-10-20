using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift01ApproachStructure
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var positions = (Vector3[])source.Clone();
        Lift01ApproachParts.Apply(lod, source, positions, delta);
        if (lod == 2)
            return Lift01ApproachFarShell.Complete(source, positions, indices, delta);
        Lift01ApproachBases.Apply(lod, source, positions, delta);
        return Lift01ApproachFrames.Complete(lod, source, positions, indices, delta);
    }
}
