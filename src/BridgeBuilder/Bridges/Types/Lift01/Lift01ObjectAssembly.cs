using System;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift01ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name,
        RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content,
        float halfWidthDelta, MovableStraightSample road, MovableRoadGeometry.Partition physical)
    {
        var attachments = Lift01MechanismData.AuthoredAttachments(factory.ResolveOriginal<ObjectPrefab>);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        // Audited order: positive-X control-room wall light, negative-X control-room
        // wall light. Both belong to the fixed root and retain their native rotations.
        for (var i = 0; i < attachments.Length; i++)
        {
            var item = attachments[i];
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("Lift01 requires its original control-room light sources.");
            item.m_Position.x += i == 0 ? halfWidthDelta : -halfWidthDelta;
        }
        var result = factory.CreateMovableObject(name, false,
            Lift01MechanismData.Meshes(new[] { mechanism, column, footing }), attachments,
            target =>
            {
                Lift01MechanismData.ApplyComponents(target, content);
                target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * 2f;
            });
        var objects = road.Objects();
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, physical);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        factory.AttachMovablePaint(result, road.Road.LaneGeometry.Decals, road.RoadToObject,
            physical, MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject));
        return result;
    }
}
