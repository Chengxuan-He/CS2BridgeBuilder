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
            if (manager.HasBuffer<ConnectedEdge>(node))
            {
                foreach (var connection in manager.GetBuffer<ConnectedEdge>(node, true))
                {
                    var edge = connection.m_Edge;
                    if (!manager.Exists(edge) || manager.HasComponent<Deleted>(edge)
                        || plan.DeletedEntities.Contains(edge)) continue;
                    hasSurvivingEdge = true;
                    plan.UpdatedEntities.Add(edge);
                }
            }
            // A junction shared with another road must survive. Let the normal network update
            // rebuild it and its remaining edges, instead of severing somebody else's road.
            if (hasSurvivingEdge) plan.UpdatedEntities.Add(node);
            else plan.DeletedEntities.Add(node);
        }
        return plan;
    }

    internal void Apply(EntityManager manager)
    {
        // Collect first, mutate second: AddComponent invalidates DynamicBuffer enumerators.
        // Native sub-element/object/lane systems own cascading cleanup of these real edges/nodes.
        foreach (var entity in UpdatedEntities)
            if (manager.Exists(entity) && !manager.HasComponent<Deleted>(entity)
                && !manager.HasComponent<Updated>(entity)) manager.AddComponent<Updated>(entity);
        foreach (var entity in DeletedEntities)
            if (manager.Exists(entity) && !manager.HasComponent<Deleted>(entity))
                manager.AddComponent<Deleted>(entity);
    }

    internal bool IsComplete(EntityManager manager)
    {
        foreach (var entity in DeletedEntities)
            if (manager.Exists(entity)) return false;
        return true;
    }
}
