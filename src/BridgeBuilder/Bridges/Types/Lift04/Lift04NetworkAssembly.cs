using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift04NetworkAssembly
{
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        IReadOnlyList<Lift04NetworkData.Role> roles, StaticObjectPrefab mechanism)
    {
        if (!factory.Created.Contains(mechanism))
            throw new InvalidOperationException("Lift04 network assembly requires its owned mechanism.");
        // Lift04 uses the subway placeholder, not the ordinary road pillar placeholder.
        // Preserve its identity for native spawnable-pillar reverse references.
        var pillar = factory.ResolveOriginal<ObjectPrefab>("Subway Pillar Placeholder");
        if (!pillar.isBuiltin || !pillar.Has<PlaceholderObject>())
            throw new InvalidOperationException("Lift04 requires the built-in subway pillar placeholder.");
        var sections = Lift04NetworkData.ConvertSections(target.name, target.m_Sections,
            roles, out var created);
        factory.AdoptMovableSections(created);
        target.m_Sections = sections;
        var bridge = target.AddOrGetComponent<Bridge>();
        Lift04NetworkData.ApplyBridge(bridge);
        bridge.active = true;
        var objects = target.AddOrGetComponent<NetSubObjects>();
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
            .Where(item => item == null || item.m_RequireOutsideConnection || !item.m_RequireElevated)
            .Concat(Lift04NetworkData.Placements(pillar, mechanism)).ToArray();
        objects.active = true;
    }
}
