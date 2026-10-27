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
        var mainRoles = Lift03SectionRoles.ResolveMain(main.m_Sections);
        var auxiliaryRoles = Lift03SectionRoles.ResolveAuxiliary(auxiliary.m_Sections);
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
        auxiliaryObjects.m_SubObjects = RetainedObjects(auxiliaryObjects).ToArray();
        auxiliaryObjects.active = true;
        var link = main.AddOrGetComponent<AuxiliaryNets>();
        Lift03NetworkData.ApplyAuxiliary(link, auxiliary);
        link.m_AuxiliaryNets[Numeric_Types_Lift03_Lift03NetworkAssembly_cs.Values.I32[0]].m_InvertWhen = auxiliaryOpposite ? NetInvertMode.Always : NetInvertMode.Never;
        link.active = true;
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
