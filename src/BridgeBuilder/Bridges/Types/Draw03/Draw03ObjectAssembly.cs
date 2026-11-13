using System;
using System.IO;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw03ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float halfWidthDelta, MovableStraightSample road) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, owner, name, mechanism, column, footing, content, halfWidthDelta, road));

    internal static async System.Threading.Tasks.Task<StaticObjectPrefab> CreateAsync(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float halfWidthDelta, MovableStraightSample road)
    {
        await GenerationWork.Next("support Draw03ObjectAssembly.Create");
        var attachments = Draw03MechanismData.StructuralAttachments(factory.ResolveOriginal<ObjectPrefab>);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var item in attachments)
        {
            item.m_Position.x += Math.Sign(item.m_Position.x) * halfWidthDelta;
        }

        // Each of the four native stations occurs on both sides. Replicate each selected
        // lateral lamp row once per station, not once per authored left/right lamp.
        var stations = Draw03MechanismData.RoadAttachments(_ => null !).Select(item => item.m_Position.z).Distinct().ToArray();
        var objects = MovableRoadLights.Place(road.Objects(), road.RoadToObject, stations);
        var result = factory.CreateMovableObject(name, false, Draw03MechanismData.Meshes(new[] { mechanism, column, footing }), attachments, target =>
        {
            Draw03MechanismData.ApplyComponents(target, content);
            target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * Numeric_Types_Draw03_Draw03ObjectAssembly_cs.Values.F32[0];
        });
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        // Furniture stations are away from the domain-specific centre gaps.
        Array.Fill(partitions, Draw03RoadPartitions.Carriageway);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        return result;
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03ObjectAssembly_cs
internal static class Numeric_Types_Draw03_Draw03ObjectAssembly_cs
{
    static Numeric_Types_Draw03_Draw03ObjectAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw03_Draw03ObjectAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Draw03_Draw03ObjectAssembly_cs"));
}
