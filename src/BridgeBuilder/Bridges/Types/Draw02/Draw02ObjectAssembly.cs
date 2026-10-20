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
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name,
        RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content,
        Func<string, ObjectPrefab> resolveOriginal, float halfWidthDelta, MovableStraightSample road)
    {
        var authored = Draw02MechanismData.StructuralAttachments(resolveOriginal);
        var attachments = new List<ObjectSubObjectInfo>(authored.Length);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var item in authored)
        {
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("Draw02 requires its original control-room attachment sources.");
            // All three recorded vents/lights belong to the negative-X fixed control room.
            var position = item.m_Position;
            position.x -= halfWidthDelta;
            attachments.Add(new ObjectSubObjectInfo
            {
                m_Object = item.m_Object, m_Position = position, m_Rotation = item.m_Rotation,
                m_ParentMesh = item.m_ParentMesh, m_GroupIndex = item.m_GroupIndex,
                m_Probability = item.m_Probability
            });
        }
        var objects = MovableRoadLights.Place(road.Objects(), road.RoadToObject,
            Draw02MechanismData.RoadAttachments(_ => null!).Select(item => item.m_Position.z).ToArray());
        var result = factory.CreateMovableObject(name, false,
            Draw02MechanismData.Meshes(new[] { mechanism, column, footing }), attachments,
            target =>
            {
                Draw02MechanismData.ApplyComponents(target, content);
                target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * 2f;
            });
        // Both Draw02 domains have the same leaf ownership. Their distinct centre
        // gaps remain relevant to road geometry, but no authored lamp is at the seam.
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, Draw02RoadPartitions.Carriageway);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        factory.AttachMovablePaint(result, road.Road.LaneGeometry.Decals, road.RoadToObject,
            Draw02RoadPartitions.Carriageway, MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject));
        return result;
    }
}
