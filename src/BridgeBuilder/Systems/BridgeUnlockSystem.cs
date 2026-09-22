using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using Game;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

/// <summary>Keep generated networks buildable from native menus too, including while paused.</summary>
public partial class BridgeUnlockSystem : GameSystemBase
{
    private EntityQuery _networks;
    private PrefabSystem _prefabs = null!;

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
        _networks = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(),
            ComponentType.ReadOnly<PlaceableNetData>());
    }

    protected override void OnUpdate()
    {
        using var entities = _networks.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            if (!_prefabs.TryGetPrefab<NetGeometryPrefab>(entity, out var bridge) || bridge == null
                || !BridgeRegistration.IsPrefabName(bridge.name)) continue;
            BridgeUnlockPolicy.TryPrepareBuild(bridge, _prefabs, EntityManager, out _);
        }
    }
}
