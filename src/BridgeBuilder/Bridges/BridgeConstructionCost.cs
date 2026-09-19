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
