using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift01DetailedStructure
{
    // Main mechanism mesh, LOD0/1. Every surviving authored part is handled by
    // a measured table; joined parts finish the plan before road replacement.
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var positions = (Vector3[])source.Clone();
        Lift01RigidParts.Apply(lod, source, positions, delta);
        Lift01Bases.Apply(lod, source, positions, delta);
        Lift01TransverseParts.Apply(lod, source, positions, delta);
        Lift01Connectors.Apply(lod, source, positions, delta);
        Lift01TowerFrames.Apply(lod, source, positions, delta);
        Lift01TowerBodies.Apply(lod, source, positions, delta);
        var plan = Lift01JoinedFrames.Complete(lod, source, positions, indices, delta);
        Lift01RoadReplacement.RemoveNativeRoad(lod, plan);
        return plan;
    }
}
