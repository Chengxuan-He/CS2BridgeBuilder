using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Receivers remain deck-local even when the resulting paint shares one native mesh.
internal sealed class MovablePaintDeck
{
    internal readonly IReadOnlyList<MovableLaneVisuals.Batch> Batches;
    internal readonly Pose RoadToObject;
    internal readonly MovableRoadGeometry.Partition Partition;
    internal readonly IReadOnlyList<MovableRoadGeometry.Triangle> Receivers;

    internal MovablePaintDeck(IReadOnlyList<MovableLaneVisuals.Batch> batches, Pose roadToObject,
        MovableRoadGeometry.Partition partition, IReadOnlyList<MovableRoadGeometry.Triangle> receivers)
    {
        Batches = batches;
        RoadToObject = roadToObject;
        Partition = partition;
        Receivers = receivers;
    }
}
