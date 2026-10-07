using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

/// <summary>Read legacy assets during additive migration; never persist this type in new assets.</summary>
[Serializable]
public sealed class BridgeConstructionCost : ComponentBase
{
    // Kept only to deserialize old assets; never used to calculate or override native prices.
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

    // Legacy deserialization only. New assets never contain this component.
    public override void GetPrefabComponents(HashSet<ComponentType> components) { }
    public override void GetArchetypeComponents(HashSet<ComponentType> components) { }
}
