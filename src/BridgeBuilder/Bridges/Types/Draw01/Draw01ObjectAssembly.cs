using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw01ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name,
        RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content,
        Func<string, ObjectPrefab> resolveOriginal, float halfWidthDelta,
        Draw01ClosedRoad road)
    {
        if (mechanism == null || column == null || footing == null || content == null ||
            !float.IsFinite(halfWidthDelta))
            throw new ArgumentException("Draw01 requires three prepared render graphs, native content and a finite width change.");

        // Resolve once from the same live sampling context that supplied the road
        // geometry. Keep native selection order and each winning producer's identity.
        var roadObjects = Draw01RoadLights.Place(road.Objects(), road.RoadToObject);
        var attachmentPartitions = Draw01RoadPartitions.ForObjects(roadObjects);

        // The four authored streetlights are road furniture. The selected-road producer
        // supplies their replacements in its closed-road frame. Binding must wait
        // until the final root exists because native lamp effects belong to it.
        // Resolve only the eight structural attachments; never require the old streetlights.
        var authored = Draw01MechanismData.StructuralAttachments(resolveOriginal);
        var attachments = new List<ObjectSubObjectInfo>(authored.Length);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        foreach (var item in authored)
        {
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("Draw01 requires its original structural light and rooftop attachment sources.");
            // Both recorded control buildings are on positive X (Draw01ControlBuildingData).
            // Their six lights and two vents follow that same rigid displacement.
            var position = item.m_Position;
            position.x += halfWidthDelta;
            attachments.Add(new ObjectSubObjectInfo
            {
                m_Object = item.m_Object, m_Position = position, m_Rotation = item.m_Rotation,
                m_ParentMesh = item.m_ParentMesh, m_GroupIndex = item.m_GroupIndex,
                m_Probability = item.m_Probability
            });
        }
        var result = factory.CreateMovableObject(name, false,
            Draw01MechanismData.Meshes(new[] { mechanism, column, footing }), attachments,
            target =>
            {
                Draw01MechanismData.ApplyComponents(target, content);
                // StandingObject uses the complete support span; preserve its native Y/Z
                // extents, longitudinal gap, movement parameters and network node offset.
                target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * 2f;
            });
        factory.AttachMovableRoadObjects(result, roadObjects.Placements, road.RoadToObject, attachmentPartitions);
        return result;
    }
}
