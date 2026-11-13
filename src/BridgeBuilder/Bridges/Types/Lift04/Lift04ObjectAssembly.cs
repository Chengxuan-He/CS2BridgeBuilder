using System;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift04ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float halfWidthDelta, MovableStraightSample road) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, owner, name, mechanism, column, footing, content, halfWidthDelta, road));

    internal static async System.Threading.Tasks.Task<StaticObjectPrefab> CreateAsync(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float halfWidthDelta, MovableStraightSample road)
    {
        await GenerationWork.Next("support Lift04ObjectAssembly.Create");
        // Recorded fixed-tower attachments: two wall lights, two vents and forty ladder pieces.
        // All retain native parent connection 0, rotation, group and probability.
        var attachments = Lift04MechanismData.AuthoredAttachments(factory.ResolveOriginal<ObjectPrefab>);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var item in attachments)
        {
            item.m_Position.x += Math.Sign(item.m_Position.x) * halfWidthDelta;
        }

        var result = factory.CreateMovableObject(name, false, Lift04MechanismData.Meshes(new[] { mechanism, column, footing }), attachments, target =>
        {
            Lift04MechanismData.ApplyComponents(target, content);
            target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * Numeric_Types_Lift04_Lift04ObjectAssembly_cs.Values.F32[0];
        });
        var objects = road.Objects();
        var physical = Lift04RoadPartitions.Carriageway;
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, physical);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        return result;
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04ObjectAssembly_cs
internal static class Numeric_Types_Lift04_Lift04ObjectAssembly_cs
{
    static Numeric_Types_Lift04_Lift04ObjectAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift04_Lift04ObjectAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Lift04_Lift04ObjectAssembly_cs"));
}
