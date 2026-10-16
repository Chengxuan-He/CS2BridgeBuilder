using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class PedestrianDraw02NetworkAssembly
{
    // Apply after sampling the original visible road and preparing all structure objects.
    // The target is the private selected-network clone owned by the normal generation path.
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        ObjectPrefab pillar, StaticObjectPrefab mechanism)
    {
        if (!factory.Created.Contains(pillar) || !factory.Created.Contains(mechanism))
            throw new InvalidOperationException("PedestrianDraw02 network assembly requires its prepared owned pillar and mechanism.");

        var sections = PedestrianDraw02NetworkData.ConvertSections(target.name,
            target.m_Sections, out var created);
        // Include object-producer clones as well as wrappers. Otherwise a cold load would
        // refer to transient pieces even though the root network itself was saved.
        factory.AdoptMovableSections(created);
        target.m_Sections = sections;

        var bridge = target.AddOrGetComponent<Bridge>();
        PedestrianDraw02NetworkData.ApplyBridge(bridge);
        bridge.active = true;
        PedestrianDraw02NetworkData.AddMechanismStates(target);

        var objects = target.AddOrGetComponent<NetSubObjects>();
        // Match the existing generation path's selected-road ownership: replace its
        // ordinary elevated supports, retain its ground/outside-connection objects.
        // The native mechanism placement itself is deliberately NOT elevated-only.
        var existing = objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>();
        objects.m_SubObjects = existing.Where(item => item == null || !item.m_RequireElevated)
            .Concat(PedestrianDraw02NetworkData.Placements(pillar, mechanism)).ToArray();
        objects.active = true;
    }
}
