using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Draw01NetworkAssembly
{
    // Run after sampling the visible selected road, on its private generation clone.
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        StaticObjectPrefab pillar, StaticObjectPrefab mechanism)
    {
        if (!factory.Created.Contains(pillar) || !factory.Created.Contains(mechanism))
            throw new InvalidOperationException("Draw01 network assembly requires its owned pillar and mechanism objects.");

        var sections = Draw01NetworkData.ConvertSections(target.name, target.m_Sections, out var created);
        factory.AdoptMovableSections(created);
        target.m_Sections = sections;

        var bridge = target.AddOrGetComponent<Bridge>();
        Draw01NetworkData.ApplyBridge(bridge);
        bridge.active = true;
        Draw01NetworkData.AddMechanismStates(target);

        var objects = target.AddOrGetComponent<NetSubObjects>();
        var existing = objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>();
        // Replace elevated supports, preserving the selected road's outside connections
        // and ground attachments. Do not duplicate the prototype's road-specific sign.
        // The mechanism's recorded placement deliberately has RequireElevated=false.
        objects.m_SubObjects = existing.Where(item => item == null ||
                item.m_RequireOutsideConnection || !item.m_RequireElevated)
            .Concat(Draw01NetworkData.StructurePlacements(pillar, mechanism)).ToArray();
        objects.active = true;
    }
}
