using System;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift01ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float halfWidthDelta, MovableStraightSample road, MovableRoadGeometry.Partition physical) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, owner, name, mechanism, column, footing, content, halfWidthDelta, road, physical));

    internal static async System.Threading.Tasks.Task<StaticObjectPrefab> CreateAsync(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float halfWidthDelta, MovableStraightSample road, MovableRoadGeometry.Partition physical)
    {
        await GenerationWork.Next("support Lift01ObjectAssembly.Create");
        var attachments = Lift01MechanismData.AuthoredAttachments(factory.ResolveOriginal<ObjectPrefab>);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        // Audited order: positive-X control-room wall light, negative-X control-room
        // wall light. Both belong to the fixed root and retain their native rotations.
        for (var i = Numeric_Types_Lift01_Lift01ObjectAssembly_cs.Values.I32[0]; i < attachments.Length; i++)
        {
            var item = attachments[i];
            if (item.m_Object == null)
                throw new InvalidOperationException("Lift01 requires its original control-room light sources.");
            item.m_Position.x += i == Numeric_Types_Lift01_Lift01ObjectAssembly_cs.Values.I32[1] ? halfWidthDelta : -halfWidthDelta;
        }

        var result = factory.CreateMovableObject(name, false, Lift01MechanismData.Meshes(new[] { mechanism, column, footing }), attachments, target =>
        {
            Lift01MechanismData.ApplyComponents(target, content);
            target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * Numeric_Types_Lift01_Lift01ObjectAssembly_cs.Values.F32[0];
        });
        var objects = road.Objects();
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, physical);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        return result;
    }
}

// Embedded numerical arrays: Types_Lift01_Lift01ObjectAssembly_cs
internal static class Numeric_Types_Lift01_Lift01ObjectAssembly_cs
{
    static Numeric_Types_Lift01_Lift01ObjectAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift01_Lift01ObjectAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Lift01_Lift01ObjectAssembly_cs"));
}
