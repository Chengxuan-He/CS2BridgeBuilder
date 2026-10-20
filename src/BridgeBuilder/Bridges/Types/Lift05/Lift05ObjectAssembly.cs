using System;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift05ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name,
        RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content,
        float halfWidthDelta, MovableStraightSample road, MovableRoadGeometry.Partition physical)
    {
        // Recorded native attachments: twelve wall lights and sixteen roof vents.
        // All retain parent connection 0, rotations, groups and probabilities.
        var attachments = Lift05MechanismData.AuthoredAttachments(factory.ResolveOriginal<ObjectPrefab>);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var item in attachments)
        {
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("Lift05 requires its original wall-light and roof-vent sources.");
            // Contract attachment rule: nonzero X follows its parent's side displacement.
            item.m_Position.x += Math.Sign(item.m_Position.x) * halfWidthDelta;
        }
        var result = factory.CreateMovableObject(name, false,
            Lift05MechanismData.Meshes(new[] { mechanism, column, footing }), attachments,
            target =>
            {
                Lift05MechanismData.ApplyComponents(target, content);
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
