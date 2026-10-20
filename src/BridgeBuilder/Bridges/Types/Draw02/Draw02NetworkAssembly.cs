using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Draw02NetworkAssembly
{
    // Apply to the private selected-road clone after road sampling and support creation.
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        StaticObjectPrefab pillar, StaticObjectPrefab mechanism)
    {
        if (!factory.Created.Contains(pillar) || !factory.Created.Contains(mechanism))
            throw new InvalidOperationException("Draw02 network assembly requires owned pillar and mechanism objects.");
        var sections = Draw02NetworkData.ConvertSections(target.name, target.m_Sections, out var created);
        // Native Draw02 Sidewalk 3.5 resolves Sidewalk Piece 3.5 - NoTram,
        // whose LevelCrossing producer uses this same bascule barrier placeholder.
        MovableBarrierRules.Apply(target.name, sections, ref created,
            factory.ResolveOriginal<ObjectPrefab>("BasculeBridgeBarrier01 Placeholder"));
        factory.AdoptMovableSections(created);
        target.m_Sections = sections;
        var bridge = target.AddOrGetComponent<Bridge>();
        Draw02NetworkData.ApplyBridge(bridge);
        bridge.active = true;
        Draw02NetworkData.AddMechanismStates(target);
        var objects = target.AddOrGetComponent<NetSubObjects>();
        // Outside connections belong to the selected road. Keep their complete rules
        // rather than replacing them with the prototype's road-specific connection.
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
            .Where(item => item == null || item.m_RequireOutsideConnection || !item.m_RequireElevated)
            .Concat(Draw02NetworkData.StructurePlacements(pillar, mechanism)).ToArray();
        objects.active = true;
    }
}
