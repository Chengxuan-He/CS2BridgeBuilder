using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift01NetworkAssembly
{
    // Roles must come from selected-network composition, not native train-section names.
    // All appearance graphs are prepared before mutating this private network clone.
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory,
        IReadOnlyList<Lift01NetworkData.Role> roles, StaticObjectPrefab pillar,
        StaticObjectPrefab mechanism)
    {
        if (!factory.Created.Contains(pillar) || !factory.Created.Contains(mechanism))
            throw new InvalidOperationException("Lift01 network assembly requires owned pillar and mechanism objects.");
        var sections = Lift01NetworkData.ConvertSections(target.name, target.m_Sections,
            roles, out var created);
        MovableBarrierRules.Apply(target.name, sections, ref created,
            factory.ResolveOriginal<ObjectPrefab>("BasculeBridgeBarrier01 Placeholder"));
        factory.AdoptMovableSections(created);
        target.m_Sections = sections;
        var bridge = target.AddOrGetComponent<Bridge>();
        Lift01NetworkData.ApplyBridge(bridge);
        bridge.active = true;
        // The audited Lift01 root has no extra edge/node state rules. In particular,
        // do not import bascule road requirements into this lift mechanism.
        var objects = target.AddOrGetComponent<NetSubObjects>();
        objects.m_SubObjects = (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
            .Where(item => item == null || item.m_RequireOutsideConnection || !item.m_RequireElevated)
            .Concat(Lift01NetworkData.StructurePlacements(pillar, mechanism)).ToArray();
        // Retain the selected network's complete outside-connection rules. The native
        // train connection's 8 m anchor must not replace another selected network's data.
        objects.active = true;
    }
}
