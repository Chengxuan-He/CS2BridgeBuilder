using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw02ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, Func<string, ObjectPrefab> resolveOriginal, float halfWidthDelta, MovableStraightSample road) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, owner, name, mechanism, column, footing, content, resolveOriginal, halfWidthDelta, road));

    internal static async System.Threading.Tasks.Task<StaticObjectPrefab> CreateAsync(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, Func<string, ObjectPrefab> resolveOriginal, float halfWidthDelta, MovableStraightSample road)
    {
        await GenerationWork.Next("support Draw02ObjectAssembly.Create");
        var authored = Draw02MechanismData.StructuralAttachments(resolveOriginal);
        var attachments = new List<ObjectSubObjectInfo>(authored.Length);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var item in authored)
        {
            // All three recorded vents/lights belong to the negative-X fixed control room.
            var position = item.m_Position;
            position.x -= halfWidthDelta;
            attachments.Add(new ObjectSubObjectInfo { m_Object = item.m_Object, m_Position = position, m_Rotation = item.m_Rotation, m_ParentMesh = item.m_ParentMesh, m_GroupIndex = item.m_GroupIndex, m_Probability = item.m_Probability });
        }

        var objects = MovableRoadLights.Place(road.Objects(), road.RoadToObject, Draw02MechanismData.RoadAttachments(_ => null !).Select(item => item.m_Position.z).ToArray());
        var result = factory.CreateMovableObject(name, false, Draw02MechanismData.Meshes(new[] { mechanism, column, footing }), attachments, target =>
        {
            Draw02MechanismData.ApplyComponents(target, content);
            target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * Numeric_Types_Draw02_Draw02ObjectAssembly_cs.Values.F32[0];
        });
        // Both Draw02 domains have the same leaf ownership. Their distinct centre
        // gaps remain relevant to road geometry, but no authored lamp is at the seam.
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, Draw02RoadPartitions.Carriageway);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        return result;
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02ObjectAssembly_cs
internal static class Numeric_Types_Draw02_Draw02ObjectAssembly_cs
{
    static Numeric_Types_Draw02_Draw02ObjectAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02ObjectAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02ObjectAssembly_cs"));
}
