using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift05NetworkAssembly
{
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        IReadOnlyList<Lift05NetworkData.Role> roles, StaticObjectPrefab mechanism)
    {
        if (!factory.Created.Contains(mechanism))
            throw new InvalidOperationException("Lift05 network assembly requires its owned mechanism.");
        // Native spawnable pillars register against this exact placeholder identity.
        // Cloning it would sever the game's reverse replacement references.
        var pillar = factory.ResolveOriginal<ObjectPrefab>("Pillar Placeholder");
        if (!pillar.isBuiltin || !pillar.Has<PlaceholderObject>())
            throw new InvalidOperationException("Lift05 requires the built-in pillar placeholder.");

        var sections = Lift05NetworkData.ConvertSections(target.name, target.m_Sections,
            roles, out var created);
        factory.AdoptMovableSections(created);
        target.m_Sections = sections;
        var bridge = target.AddOrGetComponent<Bridge>();
        Lift05NetworkData.ApplyBridge(bridge);
        bridge.active = true;

        // No additional edge/node states exist on the recorded Lift05 network.
        // Replace elevated supports, preserving the selected network's ground and
        // outside-connection objects. The mechanism itself is not elevated-only.
        var objects = target.AddOrGetComponent<NetSubObjects>();
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
            .Where(item => item == null || item.m_RequireOutsideConnection || !item.m_RequireElevated)
            .Concat(Lift05NetworkData.Placements(pillar, mechanism)).ToArray();
        objects.active = true;
    }
}
