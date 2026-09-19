using BridgeBuilder.Bridges;
using Game;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

/// <summary>Replaces native base cost after initialization, without adding any charge asset.</summary>
public partial class BridgePriceSystem : GameSystemBase
{
    private EntityQuery _networks;
    private EntityQuery _compositions;
    private PrefabSystem _prefabs = null!;

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
        _networks = GetEntityQuery(ComponentType.ReadOnly<BridgeConstructionCostData>(),
            ComponentType.ReadWrite<PlaceableNetData>());
        _compositions = GetEntityQuery(ComponentType.ReadOnly<BridgeCostComposition>(),
            ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PlaceableNetComposition>());
    }

    protected override void OnUpdate()
    {
        // Direct EntityManager access synchronizes native producer jobs. Assignment is idempotent:
        // reload/reinitialization cannot accumulate the charge or restore the original piece price.
        using var networks = _networks.ToEntityArray(Allocator.Temp);
        foreach (var entity in networks)
        {
            var value = EntityManager.GetComponentData<BridgeConstructionCostData>(entity).Value;
            var native = EntityManager.GetComponentData<PlaceableNetData>(entity);
            if (native.m_DefaultConstructionCost == value) continue;
            native.m_DefaultConstructionCost = value;
            EntityManager.SetComponentData(entity, native);
        }
        using var compositions = _compositions.ToEntityArray(Allocator.Temp);
        foreach (var entity in compositions)
        {
            var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            if (!EntityManager.HasComponent<BridgeConstructionCostData>(prefab)) continue;
            var value = EntityManager.GetComponentData<BridgeConstructionCostData>(prefab).Value;
            var native = EntityManager.GetComponentData<PlaceableNetComposition>(entity);
            if (native.m_ConstructionCost == value) continue;
            native.m_ConstructionCost = value;
            EntityManager.SetComponentData(entity, native);
        }
    }

    internal bool TryReadSource(NetGeometryPrefab road, out long ground, out long elevated)
    {
        ground = elevated = 0;
        if (!_prefabs.TryGetEntity(road, out var entity)
            || !EntityManager.HasComponent<PlaceableNetData>(entity)
            || !EntityManager.HasBuffer<NetGeometrySection>(entity)) return false;
        ground = EntityManager.GetComponentData<PlaceableNetData>(entity).m_DefaultConstructionCost;
        using var pieces = new NativeList<NetCompositionPiece>(Allocator.Temp);
        var sections = GetBufferLookup<NetSubSection>(true);
        var sectionPieces = GetBufferLookup<NetSectionPiece>(true);
        var costs = GetComponentLookup<PlaceableNetPieceData>(true);
        EntityManager.CompleteAllTrackedJobs();
        NetCompositionHelpers.GetCompositionPieces(pieces,
            EntityManager.GetBuffer<NetGeometrySection>(entity, true).AsNativeArray(),
            new CompositionFlags(CompositionFlags.General.Elevated, 0, 0), sections, sectionPieces);
        if (pieces.Length == 0) return false;
        var data = default(PlaceableNetComposition);
        NetCompositionHelpers.CalculatePlaceableData(ref data, pieces.AsArray(), costs);
        elevated = data.m_ConstructionCost;
        return true;
    }
}
