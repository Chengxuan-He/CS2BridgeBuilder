using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift03ObjectAssembly
{
    // Native authored order: four warning lights and twenty fixed-tower wall lights.
    private static readonly int[] AttachmentSides = ReadPackedAttachmentSides();
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float structureHalfDelta, Lift03RoadSampling roads)
    {
        var attachments = Lift03MechanismData.AuthoredAttachments(factory.ResolveOriginal<ObjectPrefab>);
        if (attachments.Length != AttachmentSides.Length)
            throw new InvalidOperationException("Lift03 requires its recorded 24 tower attachments.");
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        for (var i = Numeric_Types_Lift03_Lift03ObjectAssembly_cs.Values.I32[24]; i < attachments.Length; i++)
        {
            var item = attachments[i];
            if (item.m_Object == null || BridgeAssetInfo.IsOtherBridgePrefab(item.m_Object.asset?.path, owner, imported))
                throw new InvalidOperationException("Lift03 requires its original tower light sources.");
            item.m_Position.x += AttachmentSides[i] * structureHalfDelta;
        }

        var result = factory.CreateMovableObject(name, false, Lift03MechanismData.Meshes(new[] { mechanism, column, footing }), attachments, target =>
        {
            Lift03MechanismData.ApplyComponents(target, content);
            target.GetComponent<StandingObject>().m_LegSize.x += Numeric_Types_Lift03_Lift03ObjectAssembly_cs.Values.F32[0] * structureHalfDelta;
        });
        // Bind once: the shared lamp converter allocates names per invocation.
        // Bring both selected placement lists into the same closed mechanism frame first.
        var objects = new List<ObjectSubObjectInfo>();
        AddObjects(Lift03RoadSampling.Deck.Main, roads.MainToObject);
        AddObjects(Lift03RoadSampling.Deck.Auxiliary, roads.AuxiliaryToObject);
        var partitions = new MovableRoadGeometry.Partition[objects.Count];
        Array.Fill(partitions, Lift03RoadPartitions.Carriageway);
        factory.AttachMovableRoadObjects(result, objects, Pose.identity, partitions);
        // Each deck supplies only its own receivers. Merge the resulting paint once,
        // preserving shared bone 3 and fixed bone 0 without duplicate resource names.
        factory.AttachMovablePaint(result, new[] { new MovablePaintDeck(roads.MainRoad.LaneGeometry.Decals, roads.MainToObject, Lift03RoadPartitions.Carriageway, MovableRoadGeometry.InFrame(roads.MainRoad.Triangles, roads.MainToObject)), new MovablePaintDeck(roads.AuxiliaryRoad.LaneGeometry.Decals, roads.AuxiliaryToObject, Lift03RoadPartitions.Carriageway, MovableRoadGeometry.InFrame(roads.AuxiliaryRoad.Triangles, roads.AuxiliaryToObject)) });
        return result;
        void AddObjects(Lift03RoadSampling.Deck deck, Pose pose)
        {
            foreach (var item in roads.Objects(deck).Placements)
                objects.Add(new ObjectSubObjectInfo { m_Object = item.m_Object, m_Position = pose.rotation * (Vector3)item.m_Position + pose.position, m_Rotation = pose.rotation * (Quaternion)item.m_Rotation, m_ParentMesh = item.m_ParentMesh, m_GroupIndex = item.m_GroupIndex, m_Probability = item.m_Probability });
        }
    }

    private static int[] ReadPackedAttachmentSides()
    {
        var d = Numeric_Types_Lift03_Lift03ObjectAssembly_cs.Values;
        var values = new int[24];
        for (int i = 0; i < 1; i++)
            values[0 + i] = -d.I32[0 + i * 1];
        for (int i = 0; i < 2; i++)
            values[1 + i] = d.I32[1 + i * 1];
        for (int i = 0; i < 5; i++)
            values[3 + i] = -d.I32[3 + i * 1];
        for (int i = 0; i < 4; i++)
            values[8 + i] = d.I32[8 + i * 1];
        for (int i = 0; i < 2; i++)
            values[12 + i] = -d.I32[12 + i * 1];
        for (int i = 0; i < 2; i++)
            values[14 + i] = d.I32[14 + i * 1];
        for (int i = 0; i < 1; i++)
            values[16 + i] = -d.I32[16 + i * 1];
        for (int i = 0; i < 1; i++)
            values[17 + i] = d.I32[17 + i * 1];
        for (int i = 0; i < 2; i++)
            values[18 + i] = -d.I32[18 + i * 1];
        for (int i = 0; i < 3; i++)
            values[20 + i] = d.I32[20 + i * 1];
        for (int i = 0; i < 1; i++)
            values[23 + i] = -d.I32[23 + i * 1];
        return values;
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03ObjectAssembly_cs
internal static class Numeric_Types_Lift03_Lift03ObjectAssembly_cs
{
    static Numeric_Types_Lift03_Lift03ObjectAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03ObjectAssembly_cs");
}
