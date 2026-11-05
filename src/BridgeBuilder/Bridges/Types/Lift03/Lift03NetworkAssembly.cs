using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Lift03NetworkAssembly
{
    // Both inputs are private selected-network clones. Direction is expressed by
    // the native auxiliary link and must match the coordinated sampling direction.
    // The main network owns the sole mechanism; the auxiliary carries no duplicate supports.
    internal static void Apply(NetGeometryPrefab main, NetGeometryPrefab auxiliary, TowerFactory factory, StaticObjectPrefab pillar, StaticObjectPrefab mechanism, bool auxiliaryOpposite = false)
    {
        if (ReferenceEquals(main, auxiliary))
            throw new InvalidOperationException("Lift03 requires distinct main and auxiliary networks.");
        if (!factory.Created.Contains(pillar) || !factory.Created.Contains(mechanism))
            throw new InvalidOperationException("Lift03 network assembly requires owned pillar and mechanism objects.");
        var mainRoles = Lift03SectionRoles.ResolveMain(main.m_Sections, BridgeRoadGeneration.Find(main)
            ?? throw new InvalidOperationException("Lift03 main network has no elevated road context."));
        var auxiliaryRoles = Lift03SectionRoles.ResolveAuxiliary(auxiliary.m_Sections, BridgeRoadGeneration.Find(auxiliary)
            ?? throw new InvalidOperationException("Lift03 auxiliary network has no elevated road context."));
        var mainSections = Lift03NetworkData.ConvertMainSections(main.name, main.m_Sections, mainRoles, out var mainCreated);
        factory.AdoptMovableSections(mainCreated);
        var auxiliarySections = Lift03NetworkData.ConvertAuxiliarySections(auxiliary.name, auxiliary.m_Sections, auxiliaryRoles, out var auxiliaryCreated);
        factory.AdoptMovableSections(auxiliaryCreated);
        main.m_Sections = mainSections;
        auxiliary.m_Sections = auxiliarySections;
        var mainBridge = main.AddOrGetComponent<Bridge>();
        Lift03NetworkData.ApplyMainBridge(mainBridge);
        mainBridge.active = true;
        var auxiliaryBridge = auxiliary.AddOrGetComponent<Bridge>();
        Lift03NetworkData.ApplyAuxiliaryBridge(auxiliaryBridge);
        auxiliaryBridge.active = true;
        Lift03NetworkData.AddMainStates(main);
        var mainObjects = main.AddOrGetComponent<NetSubObjects>();
        mainObjects.m_SubObjects = RetainedObjects(mainObjects).Concat(Lift03NetworkData.StructurePlacements(pillar, mechanism)).ToArray();
        mainObjects.active = true;
        var auxiliaryObjects = auxiliary.AddOrGetComponent<NetSubObjects>();
        auxiliaryObjects.m_SubObjects = AuxiliaryObjects(auxiliaryObjects, factory, mechanism, auxiliary.name, auxiliaryOpposite);
        auxiliaryObjects.active = true;
        var link = main.AddOrGetComponent<AuxiliaryNets>();
        Lift03NetworkData.ApplyAuxiliary(link, auxiliary);
        link.m_AuxiliaryNets[Numeric_Types_Lift03_Lift03NetworkAssembly_cs.Values.I32[0]].m_InvertWhen = auxiliaryOpposite ? NetInvertMode.Always : NetInvertMode.Never;
        link.active = true;
    }

    private static NetSubObjectInfo[] AuxiliaryObjects(NetSubObjects objects, TowerFactory factory, StaticObjectPrefab mechanism, string name, bool opposite)
    {
        var retained = RetainedObjects(objects).ToArray();
        if (!opposite) return retained;

        // Native fixed-offset calculation follows the parent's endpoint without
        // reversing it. Let the opposite child read its own node offset instead.
        // Node alone creates RoadNode data; only the already-fixed centre applies
        // that offset. Do not mark ordinary outer nodes as FixedNodeSize.
        var source = mechanism.GetComponent<NetObject>();
        var carrier = factory.CreateMovableObject(name + " Auxiliary Node Offset", false,
            Array.Empty<ObjectMeshInfo>(), Array.Empty<ObjectSubObjectInfo>(), target =>
            {
                var net = target.AddComponent<NetObject>();
                net.m_SetCompositionState = new[] { NetPieceRequirements.Node };
                net.m_RequireRoad = source.m_RequireRoad;
                net.m_RoadPassThrough = source.m_RoadPassThrough;
                net.m_TrackPassThrough = source.m_TrackPassThrough;
                net.m_NodeOffset = source.m_NodeOffset;
                net.m_Attached = false;
                net.m_RequirePedestrian = source.m_RequirePedestrian;
            });
        var placement = Lift03NetworkData.MechanismPlacement(carrier);
        placement.m_Placement = NetObjectPlacement.Node;
        return retained.Concat(new[] { placement }).ToArray();
    }

    // Outside connections and ground objects belong to each selected network. Preserve
    // their complete rules, rather than imposing the prototype road/train connection.
    private static IEnumerable<NetSubObjectInfo> RetainedObjects(NetSubObjects objects) => (objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>()).Where(item => item == null || item.m_RequireOutsideConnection || !item.m_RequireElevated);
}

// Embedded numerical arrays: Types_Lift03_Lift03NetworkAssembly_cs
internal static class Numeric_Types_Lift03_Lift03NetworkAssembly_cs
{
    static Numeric_Types_Lift03_Lift03NetworkAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03NetworkAssembly_cs");
}
