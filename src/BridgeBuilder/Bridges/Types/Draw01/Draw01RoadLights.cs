using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Preserve Draw01's recorded mounting stations and native selected-road producers.
internal static class Draw01RoadLights
{
    internal static MovableNodeObjects.Result Place(MovableNodeObjects.Result source, Pose roadToObject)
        => MovableRoadLights.Place(source, roadToObject,
            Draw01MechanismData.RoadAttachments(_ => null!).Select(item => item.m_Position.z).ToArray());
}