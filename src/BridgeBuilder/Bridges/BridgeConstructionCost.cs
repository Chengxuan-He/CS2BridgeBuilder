using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

/// <summary>Serialized on the existing bridge asset; deliberately has no prefab dependencies.</summary>
[Serializable]
public sealed class BridgeConstructionCost : ComponentBase
{
    public uint m_BaseConstructionCost;

    // Asset-local metadata. Missing fields in old prefabs retain these defaults.
    // Strings are values, never dependencies on source roads or bridge prototypes.
    public string m_BridgeDisplayName = "";
    public string m_BridgeUpperDeckId = "";
    public string m_BridgeLowerDeckId = "";
    public string m_BridgeStyleId = "";
    public string m_BridgeCreatedUtc = "";
    public bool m_BridgeCreationPending;
    public int m_BridgePersistenceVersion;

    protected override void OnEnable()
    {
        base.OnEnable();
        // Imported components are instantiated by the native Odin formatter before IMod.OnLoad.
        // Install assetInfo validation here as well; OnLoad alone is too late for startup assets.
        // No asset deletion, ECS mutation or forced asset loading from this callback.
        try
        {
            BridgeBuilder.Runtime.BridgeStartupRecovery.Start();
        }
        catch (Exception exception)
        {
            // Dependency loading/JIT can fail before the patch method's own try block is entered.
            // Never let that escape a native asset-deserialization callback.
            BridgeBuilder.Runtime.BridgeSessionState.RequireRestart();
            Mod.Log.Warn("Early bridge recovery unavailable; assets retained, restart required: " + exception.Message);
        }
    }

    public override void GetPrefabComponents(HashSet<ComponentType> components) =>
        components.Add(ComponentType.ReadWrite<BridgeConstructionCostData>());

    public override void GetArchetypeComponents(HashSet<ComponentType> components)
    {
        if (components.Contains(ComponentType.ReadWrite<NetCompositionData>()))
            components.Add(ComponentType.ReadWrite<BridgeCostComposition>());
    }

    public override void Initialize(EntityManager manager, Entity entity) =>
        manager.SetComponentData(entity, new BridgeConstructionCostData { Value = m_BaseConstructionCost });
}

public struct BridgeConstructionCostData : IComponentData
{
    public uint Value;
}

public struct BridgeCostComposition : IComponentData { }
