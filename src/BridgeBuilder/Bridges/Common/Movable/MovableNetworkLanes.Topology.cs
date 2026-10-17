using System.Collections.Generic;
using System;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Transform = Game.Objects.Transform;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkLanes
{
    internal Entity CreateNode(Entity prefab, Node node, PseudoRandomSeed seed, Elevation? elevation)
    {
        RequirePrivateWorld();
        var entity = CreateNativeEntity(EntityManager.GetComponentData<NetData>(prefab).m_NodeArchetype);
        EntityManager.SetComponentData(entity, new PrefabRef(prefab));
        EntityManager.SetComponentData(entity, node);
        EntityManager.SetComponentData(entity, seed);
        if (elevation.HasValue) EntityManager.AddComponentData(entity, elevation.Value);
        return entity;
    }

    internal Entity CreateEdge(Entity prefab, Entity start, Entity end, Bezier4x3 curve,
        Composition composition, PseudoRandomSeed seed, Elevation? elevation)
    {
        RequirePrivateWorld();
        var entity = CreateNativeEntity(EntityManager.GetComponentData<NetData>(prefab).m_EdgeArchetype);
        EntityManager.SetComponentData(entity, new PrefabRef(prefab));
        EntityManager.SetComponentData(entity, new Edge { m_Start = start, m_End = end });
        EntityManager.SetComponentData(entity, new Curve { m_Bezier = curve, m_Length = MathUtils.Length(curve) });
        EntityManager.SetComponentData(entity, composition);
        EntityManager.SetComponentData(entity, seed);
        if (elevation.HasValue) EntityManager.AddComponentData(entity, elevation.Value);
        AppendUnique(start, new ConnectedEdge(entity));
        AppendUnique(end, new ConnectedEdge(entity));
        return entity;
    }

    internal void ConnectMiddle(Entity edge, Entity node, float curvePosition)
    {
        RequirePrivateWorld();
        AppendUnique(edge, new ConnectedNode(node, curvePosition));
        AppendUnique(node, new ConnectedEdge(edge));
    }

    internal void SetNetworkOwner(Entity child, Entity owner)
    {
        RequirePrivateWorld();
        EntityManager.AddComponentData(child, new Owner(owner));
        if (!EntityManager.HasBuffer<Game.Net.SubNet>(owner)) EntityManager.AddBuffer<Game.Net.SubNet>(owner);
        AppendUnique(owner, new Game.Net.SubNet(child));
    }

    // Use the actual mechanism prefab archetype: MoveableBridge supplies PointOfInterest and the
    // imported MoveableBridgeData is discovered by native signal initialization through PrefabRef.
    internal Entity CreateMechanism(Entity prefab, Entity owner, Transform ownerTransform,
        NetSubObjectInfo placement, TrafficLights signalState, PseudoRandomSeed seed)
    {
        RequirePrivateWorld();
        // All ten measured B&P mechanisms are aligned Standalone pillars. Native
        // SubObjectSystem enables AnchorTop for these even when m_AnchorTop is false.
        // Use initialized object bounds (including StandingObject), not raw mesh bounds
        // or a universal road-height subtraction. This is instance placement only.
        if (EntityManager.GetComponentData<PillarData>(prefab).m_Type != PillarType.Standalone)
            throw new InvalidOperationException("The measured mechanism placement requires a standalone pillar.");
        var transform = Game.Objects.ObjectUtils.LocalToWorld(ownerTransform,
            placement.m_Position, placement.m_Rotation);
        var geometry = EntityManager.GetComponentData<ObjectGeometryData>(prefab);
        var placeable = EntityManager.GetComponentData<PlaceableObjectData>(prefab);
        transform.m_Position.y -= geometry.m_Bounds.max.y - placeable.m_PlacementOffset.y;
        // Keep the native signal-to-target convention in the same anchored world frame.
        // No animation loop or manager component is introduced into the saved object.
        var point = default(PointOfInterest);
        Game.Simulation.TrafficLightSystem.UpdateMoveableBridge(signalState, transform,
            EntityManager.GetComponentData<MoveableBridgeData>(prefab), ref point);
        var entity = CreateNativeEntity(EntityManager.GetComponentData<ObjectData>(prefab).m_Archetype);
        EntityManager.SetComponentData(entity, new PrefabRef(prefab));
        EntityManager.SetComponentData(entity, transform);
        EntityManager.SetComponentData(entity, point);
        EntityManager.SetComponentData(entity, seed);
        EntityManager.AddComponentData(entity, new Owner(owner));
        EntityManager.GetBuffer<Game.Objects.SubObject>(owner).Add(new Game.Objects.SubObject(entity));
        return entity;
    }

    private Entity CreateNativeEntity(EntityArchetype archetype)
        => (Entity)typeof(EntityManager).GetMethod("CreateEntity", new[] { typeof(EntityArchetype) })!
            .Invoke(EntityManager, new object[] { archetype });

    private void AppendUnique<T>(Entity owner, T value) where T : unmanaged, IBufferElementData, IEquatable<T>
    {
        var buffer = EntityManager.GetBuffer<T>(owner);
        foreach (var item in buffer) if (item.Equals(value)) return;
        buffer.Add(value);
    }

    private void InitializeNetworkComponents(bool loaded)
    {
        using var query = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = loaded ? System.Array.Empty<ComponentType>() : new[] { ComponentType.ReadOnly<Updated>() },
            Any = new[] { ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Edge>() }
        });
        Run(typeof(NetComponentsSystem), "CheckNodeComponentsJob", "Execute", query,
            new Dictionary<string, object>());
    }
}
