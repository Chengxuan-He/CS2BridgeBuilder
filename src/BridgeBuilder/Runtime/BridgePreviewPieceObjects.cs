using System;
using System.Collections.Generic;
using BridgeBuilder.Bridges;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Runtime;

// Stages authoring rules in a private world so newly generated, unregistered pieces
// take the same native composition path as installed pieces.
internal static class BridgePreviewPieceObjects
{
    internal static MovableRoadObjects.Rule[] Compose(BridgePreviewComposition.Piece[] pieces, NetCompositionData data)
    {
        using var world = new World("BridgeBuilder preview piece objects");
        var manager = world.EntityManager;
        var originals = new Dictionary<Entity, ObjectPrefab>();
        var imported = new Dictionary<ObjectPrefab, Entity>();
        var sourceWorld = World.DefaultGameObjectInjectionWorld;
        var source = sourceWorld.EntityManager;
        var prefabs = sourceWorld.GetExistingSystemManaged<PrefabSystem>();
        void Copy<T>(Entity from, Entity to) where T : unmanaged, IComponentData
        { if (source.HasComponent<T>(from)) manager.AddComponentData(to, source.GetComponentData<T>(from)); }
        Entity Import(ObjectPrefab prefab)
        {
            if (imported.TryGetValue(prefab, out var cached)) return cached;
            var entity = manager.CreateEntity();
            imported.Add(prefab, entity); originals.Add(entity, prefab);
            if (prefabs.TryGetEntity(prefab, out var native))
            {
                Copy<StreetLightData>(native, entity); Copy<LaneDirectionData>(native, entity);
                Copy<TrafficSignData>(native, entity); Copy<UtilityObjectData>(native, entity);
            }
            else
            {
                var components = new List<ComponentBase>();
                prefab.GetComponents(components);
                foreach (var component in components)
                {
                    var types = new HashSet<ComponentType>();
                    component.GetPrefabComponents(types);
                    if (!types.Contains(ComponentType.ReadWrite<StreetLightData>()) &&
                        !types.Contains(ComponentType.ReadWrite<LaneDirectionData>()) &&
                        !types.Contains(ComponentType.ReadWrite<TrafficSignData>()) &&
                        !types.Contains(ComponentType.ReadWrite<UtilityObjectData>())) continue;
                    foreach (var type in types) manager.AddComponent(entity, type);
                    component.Initialize(manager, entity);
                }
            }
            return entity;
        }
        var composition = manager.CreateEntity();
        manager.AddBuffer<NetCompositionPiece>(composition);
        manager.AddBuffer<NetCompositionObject>(composition);
        foreach (var occurrence in pieces)
        {
            var piece = occurrence.Composition;
            piece.m_Piece = manager.CreateEntity();
            var rules = new List<NetPieceObject>();
            var component = occurrence.Prefab.GetComponent<NetPieceObjects>();
            if (component != null && component.active)
                foreach (var info in component.m_PieceObjects)
                {
                    var rule = new NetPieceObject { m_Prefab = Import(info.m_Object), m_Position = info.m_Position,
                        m_Offset = info.m_Offset, m_Rotation = info.m_Rotation, m_Spacing = info.m_Spacing,
                        m_UseCurveRotation = info.m_UseCurveRotation, m_MinLength = info.m_MinLength,
                        m_CurveOffsetRange = info.m_CurveOffsetRange, m_Probability = info.m_Probability == 0 ? 100 : info.m_Probability };
                    NetCompositionHelpers.GetRequirementFlags(info.m_RequireAll, out rule.m_CompositionAll, out rule.m_SectionAll);
                    NetCompositionHelpers.GetRequirementFlags(info.m_RequireAny, out rule.m_CompositionAny, out rule.m_SectionAny);
                    NetCompositionHelpers.GetRequirementFlags(info.m_RequireNone, out rule.m_CompositionNone, out rule.m_SectionNone);
                    if (info.m_FlipWhenInverted) rule.m_Flags |= SubObjectFlags.FlipInverted;
                    if (info.m_EvenSpacing) rule.m_Flags |= SubObjectFlags.EvenSpacing;
                    if (info.m_SpacingOverride) rule.m_Flags |= SubObjectFlags.SpacingOverride;
                    rules.Add(rule);
                }
            var buffer = manager.AddBuffer<NetPieceObject>(piece.m_Piece);
            foreach (var rule in rules) buffer.Add(rule);
            manager.GetBuffer<NetCompositionPiece>(composition).Add(piece);
        }
        var composer = world.GetOrCreateSystemManaged<MovableRoadObjects.ObjectCompositionSystem>();
        composer.Compose(composition, data);
        var origins = composer.TraceOrigins(composition, data);
        var placed = manager.GetBuffer<NetCompositionObject>(composition, true);
        var output = new MovableRoadObjects.Rule[placed.Length];
        for (var i = 0; i < output.Length; i++)
            output[i] = new MovableRoadObjects.Rule(originals[placed[i].m_Prefab], placed[i],
                pieces[origins[i].Occurrence].Prefab, origins[i].Occurrence, origins[i].RuleIndex);
        return output;
    }
}
