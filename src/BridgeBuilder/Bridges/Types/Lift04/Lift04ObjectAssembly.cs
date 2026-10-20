using System;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift04ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name,
        RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content,
        float halfWidthDelta, MovableStraightSample road)
    {
        // Recorded fixed-tower attachments: two wall lights, two vents and forty ladder pieces.
        // All retain native parent connection 0, rotation, group and probability.
        var attachments = Lift04MechanismData.AuthoredAttachments(factory.ResolveOriginal<ObjectPrefab>);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var item in attachments)
        {
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("Lift04 requires its original tower attachment sources.");
            item.m_Position.x += Math.Sign(item.m_Position.x) * halfWidthDelta;
        }
        var result = factory.CreateMovableObject(name, false,
            Lift04MechanismData.Meshes(new[] { mechanism, column, footing }), attachments,
            target =>
            {
                Lift04MechanismData.ApplyComponents(target, content);
                target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * 2f;
            });
        var objects = road.Objects();
        var physical = Lift04RoadPartitions.Carriageway;
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, physical);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        factory.AttachMovablePaint(result, road.Road.LaneGeometry.Decals, road.RoadToObject,
            physical, MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject));
        return result;
    }
}
