using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw02ObjectAssembly
{
    // All render graphs and the selected-road sample must already be prepared. This stage does not
    // substitute prototype road geometry or publish a bridge with missing render inputs.
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, Func<string, ObjectPrefab> resolveOriginal, float halfWidthDelta, MovableStraightSample road)
    {
        if (mechanism == null || column == null || footing == null || content == null || !float.IsFinite(halfWidthDelta))
            throw new ArgumentException("PedestrianDraw02 object assembly requires all three render graphs, native content and a finite width change.");
        var authored = PedestrianDraw02MechanismData.AuthoredAttachments(resolveOriginal);
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var attachments = new List<ObjectSubObjectInfo>(authored.Length);
        foreach (var item in authored)
        {
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("PedestrianDraw02 requires its original rooftop attachment source.");
            // Both recorded vents belong to the fixed side control rooms. Their parent,
            // rotation, probability and vertical/longitudinal placement remain native.
            var position = item.m_Position;
            position.x += position.x < Numeric_Types_PedestrianDraw02_PedestrianDraw02ObjectAssembly_cs.Values.F32[0] ? -halfWidthDelta : position.x > Numeric_Types_PedestrianDraw02_PedestrianDraw02ObjectAssembly_cs.Values.F32[1] ? halfWidthDelta : Numeric_Types_PedestrianDraw02_PedestrianDraw02ObjectAssembly_cs.Values.F32[2];
            attachments.Add(new ObjectSubObjectInfo { m_Object = item.m_Object, m_Position = position, m_Rotation = item.m_Rotation, m_ParentMesh = item.m_ParentMesh, m_GroupIndex = item.m_GroupIndex, m_Probability = item.m_Probability });
        }

        var result = factory.CreateMovableObject(name, false, PedestrianDraw02MechanismData.Meshes(new[] { mechanism, column, footing }), attachments, target =>
        {
            PedestrianDraw02MechanismData.ApplyComponents(target, content);
            // StandingObject.Initialize copies this full span into native support geometry.
            // The native zero X gap remains zero; Y/Z size and longitudinal gap do not change.
            target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * Numeric_Types_PedestrianDraw02_PedestrianDraw02ObjectAssembly_cs.Values.F32[3];
        });
        // Bind selected-network objects after creating the final owner: native lamp
        // effects must be written to this mechanism, not the sampling object.
        var objects = road.Objects();
        var physical = PedestrianDraw02RoadPartitions.Create(halfWidthDelta);
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, physical);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        factory.AttachMovablePaint(result, road.Road.LaneGeometry.Decals, road.RoadToObject, physical, MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject));
        return result;
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02ObjectAssembly_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02ObjectAssembly_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02ObjectAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02ObjectAssembly_cs");
}
