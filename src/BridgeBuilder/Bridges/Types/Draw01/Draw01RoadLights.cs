using System;
using System.Linq;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Preserve Draw01's recorded mounting stations and native selected-road producers.
internal static class Draw01RoadLights
{
    internal static MovableNodeObjects.Result Place(MovableNodeObjects.Result source, Pose roadToObject, float halfWidthDelta)
    {
        var stations = Draw01MechanismData.RoadAttachments(_ => null !).Select(item => item.m_Position.z).ToArray();
        var lights = MovableRoadLights.Place(source, roadToObject, stations);
        // Only streetlights use the authored lamp stations. Moving a catenary pole
        // here detaches it from the native wire anchors and breaks longitudinal
        // symmetry. The common binder omits gap poles without relocating them.
        return lights;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01RoadLights_cs
internal static class Numeric_Types_Draw01_Draw01RoadLights_cs
{
    static Numeric_Types_Draw01_Draw01RoadLights_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01RoadLights_cs");
}
