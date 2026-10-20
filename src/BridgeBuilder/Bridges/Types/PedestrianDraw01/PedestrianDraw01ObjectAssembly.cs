using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class PedestrianDraw01ObjectAssembly
{
    // All render graphs and the selected-road sample must already be prepared. This stage does not
    // substitute prototype road geometry or publish a bridge with missing render inputs.
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name,
        RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content,
        Func<string, ObjectPrefab> resolveOriginal, float halfWidthDelta,
        MovableStraightSample road)
    {
        if (mechanism == null || column == null || footing == null || content == null ||
            !float.IsFinite(halfWidthDelta))
            throw new ArgumentException("PedestrianDraw01 object assembly requires all three render graphs, native content and a finite width change.");
        var authored = PedestrianDraw01MechanismData.AuthoredAttachments(resolveOriginal);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var attachments = new List<ObjectSubObjectInfo>(authored.Length);
        foreach (var item in authored)
        {
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("PedestrianDraw01 requires its original rooftop attachment source.");
            // The recorded vent belongs to the fixed side control room. Their parent,
            // rotation, probability and vertical/longitudinal placement remain native.
            var position = item.m_Position;
            position.x += position.x < 0f ? -halfWidthDelta : position.x > 0f ? halfWidthDelta : 0f;
            attachments.Add(new ObjectSubObjectInfo
            {
                m_Object = item.m_Object, m_Position = position, m_Rotation = item.m_Rotation,
                m_ParentMesh = item.m_ParentMesh, m_GroupIndex = item.m_GroupIndex,
                m_Probability = item.m_Probability
            });
        }
        var result = factory.CreateMovableObject(name, false,
            PedestrianDraw01MechanismData.Meshes(new[] { mechanism, column, footing }), attachments,
            target =>
            {
                PedestrianDraw01MechanismData.ApplyComponents(target, content);
                // StandingObject.Initialize copies this full span into native support geometry.
                // The native zero X gap remains zero; Y/Z size and longitudinal gap do not change.
                target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * 2f;
            });
        // Bind selected-network objects after creating the final owner: native lamp
        // effects must be written to this mechanism, not the sampling object.
        var objects = road.Objects();
        var physical = PedestrianDraw01RoadPartitions.Carriageway;
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, physical);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        factory.AttachMovablePaint(result, road.Road.LaneGeometry.Decals, road.RoadToObject,
            physical, MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject));
        return result;
    }
}
