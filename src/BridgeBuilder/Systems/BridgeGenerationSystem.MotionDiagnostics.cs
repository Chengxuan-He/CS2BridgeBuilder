using BridgeBuilder.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    private float _nextMotionInspection;
    private int _motionReports;
    private readonly Dictionary<Entity, string> _motionSnapshots = new();

    // Read-only evidence for the reported generated Draw02 mechanism. This never
    // selects a road branch, drives animation, changes signals or repairs saved assets.
    private void InspectPlacedMotion()
    {
        InspectPlacedLift03();
        if (_settling || _motionReports >= 24 || UnityEngine.Time.realtimeSinceStartup < _nextMotionInspection) return;
        _nextMotionInspection = UnityEngine.Time.realtimeSinceStartup + 5f;
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
            foreach (var entity in instances)
            {
                var reference = EntityManager.GetComponentData<PrefabRef>(entity);
                if (!_prefabSystem.TryGetPrefab<StaticObjectPrefab>(reference, out var prefab) || prefab == null ||
                    prefab.name != "b6ce7c1b5-3156-4d96-b860-c8f42041117e Moving Object" ||
                    !BridgeAssetInfo.IsPrefabName(prefab.name.Substring(0, prefab.name.Length - " Moving Object".Length))) continue;
                var skeleton = prefab.m_Meshes?.FirstOrDefault()?.m_Mesh?.GetComponent<ProceduralAnimationProperties>();
                if (skeleton?.m_Bones == null || skeleton.m_Bones.Length == 0 || skeleton.m_Bones[0].name != "DrawBridge02_Root") continue;
                var text = new StringBuilder();
                text.Append($"owner={prefab.name}; instance={entity}; prefab={reference.m_Prefab};");
                MotionComponent<PointOfInterest>(text, entity);
                MotionComponent<Game.Objects.Transform>(text, entity);
                MotionComponent<Game.Objects.NetObject>(text, entity);
                MotionComponent<MoveableBridgeData>(text, reference.m_Prefab);
                MotionSkin(text, entity, reference.m_Prefab);
                var nodes = new HashSet<Entity>();
                var parents = new HashSet<Entity>();
                void Parent(Entity parent)
                {
                    if (parent == Entity.Null || !EntityManager.Exists(parent) || !parents.Add(parent)) return;
                    text.Append($" parent={parent};");
                    MotionComponent<Game.Objects.Transform>(text, parent);
                    MotionComponent<Node>(text, parent);
                    if (EntityManager.HasComponent<PrefabRef>(parent))
                    {
                        var pr = EntityManager.GetComponentData<PrefabRef>(parent);
                        if (_prefabSystem.TryGetPrefab<PrefabBase>(pr, out var pp)) text.Append($" parentPrefab={pp?.name};");
                    }
                    if (EntityManager.HasComponent<Node>(parent)) nodes.Add(parent);
                    if (EntityManager.HasComponent<Edge>(parent))
                    {
                        var edge = EntityManager.GetComponentData<Edge>(parent);
                        text.Append($" ends={edge.m_Start}/{edge.m_End};");
                        MotionComponent<Curve>(text, parent);
                        nodes.Add(edge.m_Start); nodes.Add(edge.m_End);
                    }
                    MotionComponent<Owner>(text, parent);
                    MotionComponent<Game.Objects.Attached>(text, parent);
                }
                Parent(entity);
                var current = entity;
                for (var depth = 0; depth < 4 && EntityManager.HasComponent<Owner>(current); depth++)
                {
                    current = EntityManager.GetComponentData<Owner>(current).m_Owner;
                    Parent(current);
                }
                if (EntityManager.HasComponent<Game.Objects.Attached>(entity))
                    Parent(EntityManager.GetComponentData<Game.Objects.Attached>(entity).m_Parent);
                foreach (var node in nodes.OrderBy(n => n.Index))
                {
                    text.Append($" node={node}[");
                    MotionComponent<Node>(text, node);
                    MotionComponent<TrafficLights>(text, node);
                    if (EntityManager.HasBuffer<ConnectedEdge>(node))
                        foreach (var edge in EntityManager.GetBuffer<ConnectedEdge>(node, true))
                        {
                            text.Append($" edge={edge.m_Edge}");
                            MotionComponent<Edge>(text, edge.m_Edge);
                            MotionComponent<Curve>(text, edge.m_Edge);
                            MotionComponent<Composition>(text, edge.m_Edge);
                            if (EntityManager.HasComponent<PrefabRef>(edge.m_Edge))
                            {
                                var ep = EntityManager.GetComponentData<PrefabRef>(edge.m_Edge);
                                if (_prefabSystem.TryGetPrefab<NetPrefab>(ep, out var network)) text.Append($" network={network?.name}");
                                MotionComponent<NetGeometryData>(text, ep.m_Prefab);
                            }
                        }
                    if (EntityManager.HasBuffer<Game.Net.SubLane>(node))
                    {
                        var lanes = EntityManager.GetBuffer<Game.Net.SubLane>(node, true);
                        text.Append($" lanes={lanes.Length};");
                        for (var i = 0; i < lanes.Length && i < 64; i++)
                        {
                            var lane = lanes[i].m_SubLane;
                            text.Append($" lane={lane} track={EntityManager.HasComponent<Game.Net.TrackLane>(lane)} water={EntityManager.HasComponent<Game.Net.Waterway>(lane)};");
                            MotionComponent<LaneSignal>(text, lane);
                        }
                    }
                    text.Append("]; ");
                }
                var snapshot = text.ToString();
                // Native timers and renderer dirty bits change while idle; they must not
                // consume the bounded capture budget before the reported ship arrives.
                var signature = System.Text.RegularExpressions.Regex.Replace(snapshot, @"m_Timer=[^;]*;|updated=[^;]*;", "");
                if (_motionSnapshots.TryGetValue(entity, out var previous) && previous == signature) continue;
                _motionSnapshots[entity] = signature;
                Mod.Log.Info("Placed Draw02 motion: " + snapshot);
                if (++_motionReports >= 24) break;
            }
        }
        catch (Exception exception)
        {
            // Diagnostics must not affect creation or simulation; report once, then stop.
            _motionReports = 24;
            Mod.Log.Warn("Placed Draw02 motion diagnostic failed: " + exception);
        }
    }

    private void MotionSkin(StringBuilder text, Entity instance, Entity prefab)
    {
        if (!EntityManager.HasBuffer<Game.Rendering.Bone>(instance) ||
            !EntityManager.HasBuffer<Game.Rendering.Skeleton>(instance) || !EntityManager.HasBuffer<SubMesh>(prefab))
        { text.Append(" liveSkinBuffers=absent;"); return; }
        var bones = EntityManager.GetBuffer<Game.Rendering.Bone>(instance, true);
        var skeletons = EntityManager.GetBuffer<Game.Rendering.Skeleton>(instance, true);
        var meshes = EntityManager.GetBuffer<SubMesh>(prefab, true);
        text.Append($" liveBones={bones.Length}; liveSkeletons={skeletons.Length}; GPUReadback=notCaptured;");
        for (var i = 0; i < skeletons.Length && i < meshes.Length && i < 12; i++)
        {
            var sk = skeletons[i]; var mesh = meshes[i];
            text.Append($" skeleton={i},mesh={mesh.m_SubMesh},boneOffset={sk.m_BoneOffset},allocation={sk.m_BufferAllocation},updated={sk.m_CurrentUpdated};");
            if (!EntityManager.HasBuffer<ProceduralBone>(mesh.m_SubMesh)) continue;
            var procedural = EntityManager.GetBuffer<ProceduralBone>(mesh.m_SubMesh, true);
            using var matrices = new NativeList<float4x4>(procedural.Length * 2, Allocator.Temp);
            matrices.ResizeUninitialized(procedural.Length * 2);
            Game.Rendering.ProceduralSkeletonSystem.GetSkinMatrices(sk, in procedural, in bones, matrices);
            for (var b = 0; b < procedural.Length; b++)
            {
                var bone = bones[sk.m_BoneOffset + b];
                text.Append($" bone={b} localP={bone.m_Position} localQ={bone.m_Rotation} localS={bone.m_Scale} cpuSkin={matrices[procedural.Length + procedural[b].m_BindIndex]};");
            }
        }
    }

    private void MotionComponent<T>(StringBuilder text, Entity entity) where T : unmanaged, IComponentData
    {
        if (!EntityManager.HasComponent<T>(entity)) { text.Append($" {typeof(T).Name}=absent;"); return; }
        var value = EntityManager.GetComponentData<T>(entity);
        text.Append(" " + typeof(T).Name + "={");
        foreach (var field in typeof(T).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            text.Append(field.Name + "=" + field.GetValue(value) + ";");
        text.Append("};");
    }
}
