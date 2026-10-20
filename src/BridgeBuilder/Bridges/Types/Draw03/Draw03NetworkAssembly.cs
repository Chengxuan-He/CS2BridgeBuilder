using System;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Draw03NetworkAssembly
{
    // Called once on a private selected-network clone after preparing the render graphs.
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        StaticObjectPrefab pillar, StaticObjectPrefab mechanism)
    {
        if (!factory.Created.Contains(pillar) || !factory.Created.Contains(mechanism))
            throw new InvalidOperationException("Draw03 network assembly requires owned pillar and mechanism objects.");
        var sections = Draw03NetworkData.ConvertSections(target.name, target.m_Sections, out var created);
        // Recorded sidewalk 118596 uses native piece 120789. Its LevelCrossing
        // producer (72428) selects this bascule barrier, with selected-road placement.
        MovableBarrierRules.Apply(target.name, sections, ref created,
            factory.ResolveOriginal<ObjectPrefab>("BasculeBridgeBarrier01 Placeholder"));
        factory.AdoptMovableSections(created);
        target.m_Sections = sections;
        var bridge = target.AddOrGetComponent<Bridge>();
        Draw03NetworkData.ApplyBridge(bridge);
        bridge.active = true;
        Draw03NetworkData.AddMechanismStates(target);
        var objects = target.AddOrGetComponent<NetSubObjects>();
        // Preserve the selected network's outside-connection object, anchor and conditions.
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
            .Where(item => item == null || item.m_RequireOutsideConnection || !item.m_RequireElevated)
            .Concat(Draw03NetworkData.StructurePlacements(pillar, mechanism)).ToArray();
        objects.active = true;
    }
}
