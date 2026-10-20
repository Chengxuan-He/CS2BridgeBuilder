using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Only width deformation. The road replacement stage owns track/top removal.
// All measured memberships are disjoint; original channels and topology survive.
internal static class Lift02StructureWidthPlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var faceCount = lod switch
        {
            0 => 134080,
            1 => 80439,
            2 => 2600,
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
        if (indices.Length != faceCount * 3)
            throw new InvalidOperationException("Lift02 structure requires its native LOD topology.");
        var positions = (Vector3[])source.Clone();
        Lift02RigidParts.Apply(lod, source, positions, delta);
        Lift02CounterweightGeometry.Apply(lod, source, positions, delta);
        Lift02RoofBraceGeometry.Apply(lod, source, positions, delta);
        Lift02TowerBeamGeometry.Apply(lod, source, positions, delta);
        Lift02TowerDiagonalGeometry.Apply(lod, source, positions, delta);
        Lift02MovingFrameGeometry.Apply(lod, source, positions, delta);
        Lift02MovingBaseGeometry.Apply(lod, source, positions, delta);
        Lift02TowerShellGeometry.Apply(lod, source, positions, delta);
        Lift02PlatformDeckGeometry.Apply(lod, source, positions, delta);
        Lift02PlatformSupportGeometry.Apply(lod, source, positions, delta);
        Lift02PlatformFrameGeometry.Apply(lod, source, positions, delta);
        Lift02RoadbedGeometry.Apply(lod, source, positions, delta);
        Lift02TransverseWebGeometry.Apply(lod, source, positions, delta);
        Lift02EndFrameGeometry.Apply(lod, source, positions, delta);
        Lift02FixedBasesGeometry.Apply(lod, source, positions, delta);
        Lift02VerticalBraceGeometry.Apply(lod, source, positions, delta);
        Lift02CrossMemberGeometry.Apply(lod, source, positions, delta);
        Lift02MachineryGeometry.Apply(lod, source, positions, delta);
        Lift02JointPlateGeometry.Apply(lod, source, positions, delta);
        Lift02DeckCrossMemberGeometry.Apply(lod, source, positions, delta);
        Lift02HalfDiagonalGeometry.Apply(lod, source, positions, delta);
        Lift02CentralHardwareGeometry.Apply(lod, source, positions, delta);
        return new MovableStructureModel.Plan(source, positions, new[] { indices },
            new[] { new bool[faceCount] });
    }
}
