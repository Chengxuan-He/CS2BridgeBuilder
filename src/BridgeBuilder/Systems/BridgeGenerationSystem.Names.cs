using BridgeBuilder.Runtime;
using Colossal.Localization;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    private LocalizationManager? _nameLocalization;
    private bool _nativeNamesPending;

    private void InitializeNativeNames()
    {
        _nameLocalization = GameManager.instance.localizationManager;
        _nameLocalization.onActiveDictionaryChanged += QueueNativeNames;
    }

    private void ReleaseNativeNames()
    {
        if (_nameLocalization != null)
            _nameLocalization.onActiveDictionaryChanged -= QueueNativeNames;
        _nameLocalization = null;
        _nativeNamesPending = false;
    }

    private void QueueNativeNames() => _nativeNamesPending = true;

    private void RefreshNativeNames()
    {
        if (!_nativeNamesPending) return;
        _nativeNamesPending = false;
        // Dictionary publication happens after initial prefab publication and on rename.
        // Notify native prefab consumers on the next scheduled pass, without replacing
        // entities or re-running Created-only component/UIGroup initialization.
        foreach (var prefab in PrefabCatalog.GetAll(_prefabSystem))
        {
            if (prefab is not NetGeometryPrefab) continue;
            var name = prefab.name;
            var owner = name.EndsWith("_Upper", System.StringComparison.Ordinal)
                || name.EndsWith("_Lower", System.StringComparison.Ordinal)
                ? name.Substring(0, name.Length - 6) : name;
            if (!BridgeAssetInfo.IsPrefabName(owner)
                || prefab.asset == null || !BridgeAssetInfo.MatchesOwner(prefab.asset.path, owner)
                || !_prefabSystem.TryGetEntity(prefab, out var entity)
                || EntityManager.HasComponent<Deleted>(entity)) continue;
            EntityManager.AddComponent<Updated>(entity);
        }
    }
}
