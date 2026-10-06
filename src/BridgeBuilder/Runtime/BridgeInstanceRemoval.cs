using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Runtime;

/// <summary>Only placed network topology is owned by a bridge deletion, never composition caches.</summary>
internal sealed class BridgeInstanceRemoval
{
    internal readonly HashSet<Entity> DeletedEntities = new();
    internal readonly HashSet<Entity> UpdatedEntities = new();
    private readonly Dictionary<Entity, PrefabRef> _survivingNodePrefabs = new();
    private readonly HashSet<Entity> _targetPrefabs = new();
    private readonly HashSet<Entity> _appliedUpdates = new();

    internal static bool HasPlacedReferences(EntityManager manager, HashSet<Entity> prefabs)
        => CountPlacedReferences(manager, prefabs) != 0;

    internal static int CountPlacedReferences(EntityManager manager, HashSet<Entity> prefabs)
    {
        var count = 0;
        using var query = manager.CreateEntityQuery(NetworkQuery());
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
            if (prefabs.Contains(manager.GetComponentData<PrefabRef>(entity).m_Prefab)) count++;
        return count;
    }

    private static EntityQueryDesc NetworkQuery() => new()
    {
        All = new[] { ComponentType.ReadOnly<PrefabRef>() },
        Any = new[] { ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Node>() },
        None = new[] { ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabData>(),
            ComponentType.ReadOnly<NetCompositionData>() },
    };

    internal static BridgeInstanceRemoval Collect(EntityManager manager, HashSet<Entity> prefabs)
    {
        var plan = new BridgeInstanceRemoval();
        plan._targetPrefabs.UnionWith(prefabs);
        var nodes = new HashSet<Entity>();
        // CompositionSelectSystem.CreateComposition also writes PrefabRef(roadPrefab).
        // A query for PrefabRef alone includes those shared render/composition entities!
        using var query = manager.CreateEntityQuery(NetworkQuery());
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            if (!prefabs.Contains(manager.GetComponentData<PrefabRef>(entity).m_Prefab)) continue;
            if (manager.HasComponent<Edge>(entity))
            {
                plan.DeletedEntities.Add(entity);
                var edge = manager.GetComponentData<Edge>(entity);
                nodes.Add(edge.m_Start);
                nodes.Add(edge.m_End);
            }
            else nodes.Add(entity);
        }

        foreach (var node in nodes)
        {
            if (!manager.Exists(node) || !manager.HasComponent<Node>(node)
                || manager.HasComponent<Temp>(node)) continue;
            var hasSurvivingEdge = false;
            var survivorPrefab = Entity.Null;
            if (manager.HasBuffer<ConnectedEdge>(node))
            {
                foreach (var connection in manager.GetBuffer<ConnectedEdge>(node, true))
                {
                    var edge = connection.m_Edge;
                    if (!manager.Exists(edge) || manager.HasComponent<Deleted>(edge)
                        || plan.DeletedEntities.Contains(edge) || !Connects(manager, edge, node)) continue;
                    hasSurvivingEdge = true;
                    plan.UpdatedEntities.Add(edge);
                    if (survivorPrefab == Entity.Null && manager.HasComponent<PrefabRef>(edge))
                    {
                        var candidate = manager.GetComponentData<PrefabRef>(edge).m_Prefab;
                        if (manager.HasComponent<PrefabData>(candidate)
                            && manager.GetComponentData<PrefabData>(candidate).m_Index >= 0
                            && !prefabs.Contains(candidate))
                            survivorPrefab = candidate;
                    }
                }
            }
            // A junction shared with another road must survive. Let the normal network update
            // rebuild it and its remaining edges, instead of severing somebody else's road.
            if (hasSurvivingEdge)
            {
                plan.UpdatedEntities.Add(node);
                // Updated rebuilds geometry; it does not transfer this node's prefab ownership.
                // Keep the junction and its surviving roads, but release the deleted bridge's
                // PrefabRef using an actual connected surviving road (never a guessed source).
                if (survivorPrefab != Entity.Null && manager.HasComponent<PrefabRef>(node)
                    && prefabs.Contains(manager.GetComponentData<PrefabRef>(node).m_Prefab))
                    plan._survivingNodePrefabs[node] = new PrefabRef { m_Prefab = survivorPrefab };
            }
            else plan.DeletedEntities.Add(node);
        }
        return plan;
    }

    internal bool CanApply(EntityManager manager, out string reason)
    {
        foreach (var node in UpdatedEntities)
        {
            if (!manager.Exists(node) || manager.HasComponent<Deleted>(node)
                || !manager.HasComponent<Node>(node) || !manager.HasComponent<PrefabRef>(node)
                || !_targetPrefabs.Contains(manager.GetComponentData<PrefabRef>(node).m_Prefab)) continue;
            // A missing neighbouring road cannot take over this shared junction. Do not
            // delete half a bridge and then wait forever for this protected reference.
            if (_survivingNodePrefabs.TryGetValue(node, out var replacement)
                && !_targetPrefabs.Contains(replacement.m_Prefab)
                && manager.HasComponent<PrefabData>(replacement.m_Prefab)
                && manager.GetComponentData<PrefabData>(replacement.m_Prefab).m_Index >= 0) continue;
            reason = $"Shared junction {node} still references a retiring bridge, but no initialized "
                + "connected surviving road/track prefab can take it over. Restore missing network dependencies first.";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    internal void Apply(EntityManager manager)
    {
        if (!CanApply(manager, out _)) return;
        // Collect first, mutate second: AddComponent invalidates DynamicBuffer enumerators.
        // Native sub-element/object/lane systems own cascading cleanup of these real edges/nodes.
        foreach (var item in _survivingNodePrefabs)
            if (manager.Exists(item.Key) && !manager.HasComponent<Deleted>(item.Key))
                manager.SetComponentData(item.Key, item.Value);
        foreach (var entity in UpdatedEntities)
            if (manager.Exists(entity) && !manager.HasComponent<Deleted>(entity))
            {
                if (manager.HasComponent<Node>(entity) && manager.HasComponent<Owner>(entity)
                    && DeletedEntities.Contains(manager.GetComponentData<Owner>(entity).m_Owner))
                    manager.RemoveComponent<Owner>(entity);
                // Native cleanup consumes Updated. Re-adding it every frame continually
                // rebuilds surviving roads and their children while we wait for retirement.
                if (_appliedUpdates.Add(entity) && !manager.HasComponent<Updated>(entity))
                    manager.AddComponent<Updated>(entity);
            }
        foreach (var entity in DeletedEntities)
            if (manager.Exists(entity))
            {
                // Match SubElementDeleteSystem: deleted entities must not also be processed
                // as applied/created/updated geometry while awaiting native destruction.
                manager.RemoveComponent<Applied>(entity);
                manager.RemoveComponent<Created>(entity);
                manager.RemoveComponent<Updated>(entity);
                if (!manager.HasComponent<Deleted>(entity))
                manager.AddComponent<Deleted>(entity);
            }
    }

    private static bool Connects(EntityManager manager, Entity edge, Entity node)
    {
        if (!manager.HasComponent<Edge>(edge)) return false;
        var connection = manager.GetComponentData<Edge>(edge);
        return connection.m_Start == node || connection.m_End == node;
    }

    internal bool IsComplete(EntityManager manager)
        => RemainingEntityCount(manager) == 0;

    internal int RemainingEntityCount(EntityManager manager)
    {
        var count = 0;
        foreach (var entity in DeletedEntities)
            if (manager.Exists(entity)) count++;
        return count;
    }
}
