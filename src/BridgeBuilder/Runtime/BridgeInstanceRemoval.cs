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

    internal static bool HasPlacedReferences(EntityManager manager, HashSet<Entity> prefabs)
    {
        using var query = manager.CreateEntityQuery(NetworkQuery());
        using var entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
            if (prefabs.Contains(manager.GetComponentData<PrefabRef>(entity).m_Prefab)) return true;
        return false;
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

    internal void Apply(EntityManager manager)
    {
        // Collect first, mutate second: AddComponent invalidates DynamicBuffer enumerators.
        // Native sub-element/object/lane systems own cascading cleanup of these real edges/nodes.
        foreach (var item in _survivingNodePrefabs)
            if (manager.Exists(item.Key) && !manager.HasComponent<Deleted>(item.Key))
                manager.SetComponentData(item.Key, item.Value);
        foreach (var entity in UpdatedEntities)
            if (manager.Exists(entity) && !manager.HasComponent<Deleted>(entity)
                && !manager.HasComponent<Updated>(entity)) manager.AddComponent<Updated>(entity);
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

    internal void IncludeOwnedEntities(EntityManager manager)
    {
        manager.CompleteAllTrackedJobs();
        var children = new Dictionary<Entity, List<Entity>>();
        using (var query = manager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Owner>() },
            None = new[] { ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<Temp>(),
                ComponentType.ReadOnly<NetCompositionData>() },
        }))
        using (var entities = query.ToEntityArray(Allocator.Temp))
            foreach (var entity in entities)
            {
                var owner = manager.GetComponentData<Owner>(entity).m_Owner;
                if (!children.TryGetValue(owner, out var list)) children[owner] = list = new List<Entity>();
                list.Add(entity);
            }

        var pending = new Queue<Entity>(DeletedEntities);
        var visited = new HashSet<Entity>();
        while (pending.Count != 0)
        {
            var entity = pending.Dequeue();
            if (!visited.Add(entity)) continue;
            foreach (var child in Children(entity)) Add(child);
        }

        // Native SubElementDeleteSystem preserves subnetwork junctions with external edges.
        // Protect their entire owned subtree BEFORE marking anything Deleted. Protecting only
        // the node after expansion leaves its lanes/objects in the deletion set. Repeat to a
        // fixed point: a rescued owned edge may keep another junction alive too.
        var protectedEntities = new HashSet<Entity>();
        foreach (var entity in UpdatedEntities) Protect(entity);
        bool rescued;
        do
        {
            rescued = false;
            foreach (var entity in new List<Entity>(DeletedEntities))
            {
                if (!manager.Exists(entity) || !manager.HasComponent<Node>(entity)
                    || !manager.HasBuffer<ConnectedEdge>(entity)) continue;
                foreach (var connection in manager.GetBuffer<ConnectedEdge>(entity, true))
                {
                    var edge = connection.m_Edge;
                    if (!manager.Exists(edge) || DeletedEntities.Contains(edge)
                        || manager.HasComponent<Deleted>(edge) || !Connects(manager, edge, entity)) continue;
                    Protect(entity);
                    Protect(edge);
                    UpdatedEntities.Add(entity);
                    UpdatedEntities.Add(edge);
                    rescued = true;
                    if (manager.HasComponent<PrefabRef>(edge))
                    {
                        var replacement = manager.GetComponentData<PrefabRef>(edge);
                        if (manager.HasComponent<PrefabData>(replacement.m_Prefab)
                            && manager.GetComponentData<PrefabData>(replacement.m_Prefab).m_Index >= 0)
                            _survivingNodePrefabs[entity] = replacement;
                    }
                }
            }
        } while (rescued);
        foreach (var node in UpdatedEntities)
            if (manager.Exists(node) && manager.HasComponent<Node>(node)
                && manager.HasComponent<Owner>(node)
                && DeletedEntities.Contains(manager.GetComponentData<Owner>(node).m_Owner))
                manager.RemoveComponent<Owner>(node);

        IEnumerable<Entity> Children(Entity parent)
        {
            if (children.TryGetValue(parent, out var owned))
                foreach (var child in owned) if (Eligible(child, parent)) yield return child;
            if (!manager.Exists(parent)) yield break;
            if (manager.HasBuffer<Game.Objects.SubObject>(parent))
                foreach (var child in manager.GetBuffer<Game.Objects.SubObject>(parent, true))
                    if (Eligible(child.m_SubObject, parent)) yield return child.m_SubObject;
            if (manager.HasBuffer<Game.Net.SubLane>(parent))
                foreach (var child in manager.GetBuffer<Game.Net.SubLane>(parent, true))
                    if (Eligible(child.m_SubLane, parent)) yield return child.m_SubLane;
            if (manager.HasBuffer<Game.Net.SubNet>(parent))
                foreach (var child in manager.GetBuffer<Game.Net.SubNet>(parent, true))
                    if (Eligible(child.m_SubNet, parent)) yield return child.m_SubNet;
        }

        bool Eligible(Entity child, Entity parent) => manager.Exists(child)
            && !manager.HasComponent<PrefabData>(child) && !manager.HasComponent<NetCompositionData>(child)
            && !manager.HasComponent<Temp>(child)
            // Native LaneSystem and SubObjectSystem deliberately skip secondary elements.
            // Their reference-counting systems decide when the shared instance can die.
            && !manager.HasComponent<Game.Net.SecondaryLane>(child)
            && !manager.HasComponent<Game.Objects.Secondary>(child)
            // A stale sub-element buffer must not override a child's current owner.
            && (!manager.HasComponent<Owner>(child)
                || manager.GetComponentData<Owner>(child).m_Owner == parent);

        void Protect(Entity root)
        {
            var keep = new Queue<Entity>();
            keep.Enqueue(root);
            while (keep.Count != 0)
            {
                var entity = keep.Dequeue();
                if (!protectedEntities.Add(entity)) continue;
                DeletedEntities.Remove(entity);
                foreach (var child in Children(entity)) keep.Enqueue(child);
            }
        }

        void Add(Entity child)
        {
            if (!manager.Exists(child) || manager.HasComponent<PrefabData>(child)
                || manager.HasComponent<NetCompositionData>(child) || manager.HasComponent<Temp>(child)
                || UpdatedEntities.Contains(child)) return;
            if (DeletedEntities.Add(child)) pending.Enqueue(child);
        }
    }

    private static bool Connects(EntityManager manager, Entity edge, Entity node)
    {
        if (!manager.HasComponent<Edge>(edge)) return false;
        var connection = manager.GetComponentData<Edge>(edge);
        return connection.m_Start == node || connection.m_End == node;
    }

    internal bool IsComplete(EntityManager manager)
    {
        foreach (var entity in DeletedEntities)
            if (manager.Exists(entity)) return false;
        return true;
    }
}
