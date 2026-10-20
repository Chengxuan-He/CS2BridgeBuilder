using System;
using System.IO;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw03ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name,
        RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content,
        float halfWidthDelta, MovableStraightSample road)
    {
        var attachments = Draw03MechanismData.StructuralAttachments(factory.ResolveOriginal<ObjectPrefab>);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var item in attachments)
        {
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("Draw03 requires its original flagpole and wall-light sources.");
            item.m_Position.x += Math.Sign(item.m_Position.x) * halfWidthDelta;
        }
        // Each of the four native stations occurs on both sides. Replicate each selected
        // lateral lamp row once per station, not once per authored left/right lamp.
        var stations = Draw03MechanismData.RoadAttachments(_ => null!)
            .Select(item => item.m_Position.z).Distinct().ToArray();
        var objects = MovableRoadLights.Place(road.Objects(), road.RoadToObject, stations);
        var result = factory.CreateMovableObject(name, false,
            Draw03MechanismData.Meshes(new[] { mechanism, column, footing }), attachments,
            target =>
            {
                Draw03MechanismData.ApplyComponents(target, content);
                target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * 2f;
            });
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        // Furniture stations are away from the domain-specific centre gaps.
        Array.Fill(partitions, Draw03RoadPartitions.Carriageway);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        factory.AttachMovablePaint(result, road.Road.LaneGeometry.Decals, road.RoadToObject,
            Draw03RoadPartitions.Carriageway, MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject));
        return result;
    }
}
