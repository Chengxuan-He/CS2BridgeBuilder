using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BridgeBuilder.Runtime;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    private float _nextLift03Inspection;
    private readonly Dictionary<Entity, string> _lift03Snapshots = new();
    private readonly Dictionary<Entity, int> _lift03Reports = new();

    // Bounded read-only city evidence. Source skeleton identity identifies Lift03;
    // no UUID, animation override or generated-asset mutation controls behavior.
    private void InspectPlacedLift03()
    {
        if (_settling || UnityEngine.Time.realtimeSinceStartup < _nextLift03Inspection) return;
        _nextLift03Inspection = UnityEngine.Time.realtimeSinceStartup + 5f;
        try
        {
            using var query = EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<PointOfInterest>(), ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<Game.Objects.Transform>() },
                None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabData>() }
            });
            EntityManager.CompleteAllTrackedJobs();
            using var instances = query.ToEntityArray(Allocator.Temp);
            foreach (var instance in instances)
            {
                var reference = EntityManager.GetComponentData<PrefabRef>(instance);
                if (!_prefabSystem.TryGetPrefab<StaticObjectPrefab>(reference, out var prefab) || prefab == null) continue;
                var separator = prefab.name.IndexOf(' ');
                if (separator <= 0 || !BridgeAssetInfo.IsPrefabName(prefab.name.Substring(0, separator))) continue;
                var skeleton = prefab.m_Meshes?.FirstOrDefault()?.m_Mesh?.GetComponent<ProceduralAnimationProperties>();
                if (skeleton?.m_Bones == null || skeleton.m_Bones.Length == 0 ||
                    skeleton.m_Bones[0].name != "LiftBridge03_Root") continue;
                if (_lift03Reports.TryGetValue(instance, out var count) && count >= 24) continue;
                if (!_lift03Reports.ContainsKey(instance) && _lift03Reports.Count >= 4) continue;
                var text = new StringBuilder($"owner={prefab.name}; instance={instance};");
                MotionComponent<Game.Objects.Transform>(text, instance);
                MotionComponent<Game.Objects.NetObject>(text, instance);
                MotionSkin(text, instance, reference.m_Prefab);
                var seen = new HashSet<Entity>();
                void Visit(Entity entity, int depth)
                {
                    if (entity == Entity.Null || !EntityManager.Exists(entity) || depth > 3 || seen.Count >= 48 || !seen.Add(entity)) return;
                    text.Append($" entity={entity}[");
                    MotionComponent<Owner>(text, entity);
                    MotionComponent<Node>(text, entity);
                    MotionComponent<Edge>(text, entity);
                    MotionComponent<Curve>(text, entity);
                    if (EntityManager.HasComponent<PrefabRef>(entity))
                    {
                        var pr = EntityManager.GetComponentData<PrefabRef>(entity);
                        if (_prefabSystem.TryGetPrefab<PrefabBase>(pr, out var named)) text.Append($" prefab={named?.name};");
                        MotionComponent<NetData>(text, pr.m_Prefab);
                        MotionComponent<NetGeometryData>(text, pr.m_Prefab);
                    }
                    if (EntityManager.HasComponent<Composition>(entity))
                    {
                        var composition = EntityManager.GetComponentData<Composition>(entity);
                        text.Append($" edgeComposition={composition.m_Edge}; startComposition={composition.m_StartNode}; endComposition={composition.m_EndNode};");
                        MotionComponent<NetCompositionData>(text, composition.m_Edge);
                        MotionComponent<NetCompositionData>(text, composition.m_StartNode);
                        MotionComponent<NetCompositionData>(text, composition.m_EndNode);
                        foreach (var item in new[] { composition.m_Edge, composition.m_StartNode, composition.m_EndNode })
                            if (EntityManager.HasComponent<NetCompositionData>(item))
                            {
                                var flags = EntityManager.GetComponentData<NetCompositionData>(item).m_Flags;
                                text.Append($" compositionFlags={item}:{flags.m_General}/{flags.m_Left}/{flags.m_Right};");
                            }
                    }
                    if (EntityManager.HasBuffer<Game.Objects.SubObject>(entity))
                        foreach (var item in EntityManager.GetBuffer<Game.Objects.SubObject>(entity, true))
                        {
                            text.Append($" subobject={item.m_SubObject};");
                            if (EntityManager.HasComponent<PrefabRef>(item.m_SubObject))
                            {
                                var pr = EntityManager.GetComponentData<PrefabRef>(item.m_SubObject);
                                MotionComponent<NetObjectData>(text, pr.m_Prefab);
                                MotionComponent<PlaceableObjectData>(text, pr.m_Prefab);
                                if (EntityManager.HasComponent<NetObjectData>(pr.m_Prefab))
                                    text.Append($" subobjectGeneralFlags={EntityManager.GetComponentData<NetObjectData>(pr.m_Prefab).m_CompositionFlags.m_General};");
                            }
                        }
                    text.Append("]; ");
                    if (EntityManager.HasComponent<Owner>(entity)) Visit(EntityManager.GetComponentData<Owner>(entity).m_Owner, depth + 1);
                    if (EntityManager.HasComponent<Edge>(entity))
                    {
                        var edge = EntityManager.GetComponentData<Edge>(entity);
                        Visit(edge.m_Start, depth + 1); Visit(edge.m_End, depth + 1);
                    }
                    if (EntityManager.HasBuffer<Game.Net.SubNet>(entity))
                        foreach (var child in EntityManager.GetBuffer<Game.Net.SubNet>(entity, true)) Visit(child.m_SubNet, depth + 1);
                    if (EntityManager.HasBuffer<ConnectedEdge>(entity))
                        foreach (var edge in EntityManager.GetBuffer<ConnectedEdge>(entity, true)) Visit(edge.m_Edge, depth + 1);
                }
                if (EntityManager.HasComponent<Owner>(instance))
                    Visit(EntityManager.GetComponentData<Owner>(instance).m_Owner, 0);
                Visit(instance, 0);
                if (EntityManager.HasComponent<Game.Objects.Attached>(instance))
                    Visit(EntityManager.GetComponentData<Game.Objects.Attached>(instance).m_Parent, 0);
                var snapshot = text.ToString();
                var signature = System.Text.RegularExpressions.Regex.Replace(snapshot, @"m_Timer=[^;]*;|updated=[^;]*;", "");
                if (_lift03Snapshots.TryGetValue(instance, out var previous) && previous == signature) continue;
                _lift03Snapshots[instance] = signature;
                _lift03Reports[instance] = count + 1;
                Mod.Log.Info("Placed Lift03 deck diagnostic: " + snapshot);
            }
        }
        catch (Exception exception)
        {
            _nextLift03Inspection = float.PositiveInfinity;
            Mod.Log.Warn("Placed Lift03 diagnostic stopped: " + exception);
        }
    }
}
