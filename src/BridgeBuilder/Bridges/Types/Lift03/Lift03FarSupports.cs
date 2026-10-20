using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift03FarSupports
{
    // Middle-source supports and independent road bases for the native Supplement path.
    // Original fixed/moving bone indices and source Surface are retained. The far assembler
    // must remove the corresponding 32 native far faces using SupportFaces.
    internal static MovableStructureModel.Plan CreateSupplement(Vector3[] source, int[] indices, float delta,
        float upperDelta, float lowerDelta)
    {
        var positions = (Vector3[])source.Clone();
        Lift03DeckBases.Apply(1, source, positions, upperDelta, lowerDelta);
        var tall = Lift03TallSupports.Complete(1, source, positions, indices, delta);
        var slopes = Lift03SlopedSupports.Complete(1, source, positions, indices, delta);
        var removed = new bool[indices.Length / 3];
        Lift03SupportFaces.SelectMiddle(indices, removed);
        Lift03FarDeckBases.Include(indices, removed);
        return Lift03DetailedStructure.Combine(source, positions, indices, removed, tall, slopes);
    }
}
