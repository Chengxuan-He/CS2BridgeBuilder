using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class PedestrianDraw01NetworkAssembly
{
    // Apply after sampling the original visible road and preparing all structure objects.
    // The target is the private selected-network clone owned by the normal generation path.
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        StaticObjectPrefab mechanism)
    {
        if (!factory.Created.Contains(mechanism))
            throw new InvalidOperationException("PedestrianDraw01 network assembly requires its prepared owned mechanism.");

        // Native SpawnableObject.m_Placeholders references populate this exact object's
        // replacement buffer in ObjectInitializeSystem. A private clone would have no
        // reverse references and therefore could not select the native pillar variants.
        // This is a generic built-in pathway placeholder, not the bridge prototype.
        var pillar = factory.ResolveOriginal<ObjectPrefab>("Pillar Pedestrian Placeholder");
        if (!pillar.isBuiltin || !pillar.Has<PlaceholderObject>())
            throw new InvalidOperationException("PedestrianDraw01 requires the built-in pedestrian pillar placeholder.");

        var sections = PedestrianDraw01NetworkData.ConvertSections(target.name,
            target.m_Sections, out var created);
        // Include object-producer clones as well as wrappers. Otherwise a cold load would
        // refer to transient pieces even though the root network itself was saved.
        factory.AdoptMovableSections(created);
        target.m_Sections = sections;

        var bridge = target.AddOrGetComponent<Bridge>();
        PedestrianDraw01NetworkData.ApplyBridge(bridge);
        bridge.active = true;
        PedestrianDraw01NetworkData.AddMechanismStates(target);

        var objects = target.AddOrGetComponent<NetSubObjects>();
        // Match the existing generation path's selected-road ownership: replace its
        // ordinary elevated supports, retain its ground/outside-connection objects.
        // The native mechanism placement itself is deliberately NOT elevated-only.
        var existing = objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>();
        objects.m_SubObjects = existing.Where(item => item == null || !item.m_RequireElevated)
            .Concat(PedestrianDraw01NetworkData.Placements(pillar, mechanism)).ToArray();
        objects.active = true;
    }
}
