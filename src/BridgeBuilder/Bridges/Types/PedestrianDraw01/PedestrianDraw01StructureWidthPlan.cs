using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>Combines the disjoint prototype-specific main-body width maps.</summary>
internal static class PedestrianDraw01StructureWidthPlan
{
    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)
    {
        var frame = PedestrianDraw01Frames.Create(lod, source, indices, delta);
        var positions = new Vector3[source.Length];
        Array.Copy(frame.Positions, positions, positions.Length);
        PedestrianDraw01RigidParts.Apply(lod, source, positions, delta);
        PedestrianDraw01CrossbeamGeometry.Apply(lod, source, positions, delta);
        PedestrianDraw01EndConnectorGeometry.Apply(lod, source, positions, delta);
        PedestrianDraw01MachineryGeometry.Apply(lod, source, positions, delta);
        PedestrianDraw01DeckBases.Apply(lod, source, positions, delta);
        PedestrianDraw01ControlHousing.Apply(lod, source, positions, delta);
        PedestrianDraw01Ropes.Apply(lod, source, positions, delta);
        var constructedX = new float[frame.ConstructedCount];
        for (var i = 0; i < constructedX.Length; i++)
            constructedX[i] = frame.Positions[source.Length + i].x;
        // Road-sheet replacement is a separate assembly stage; retain its native
        // triangles here so the caller can combine the exact removal masks.
        return new MovableStructureModel.Plan(source, positions, new[] { indices }, frame.RemoveTriangles,
            addedVertices: frame.AddedVertices, interiorVertices: frame.InteriorVertices,
            fragments: frame.Fragments, constructedX: constructedX);
    }
}
