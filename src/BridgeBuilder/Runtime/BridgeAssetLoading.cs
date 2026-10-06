using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace BridgeBuilder.Runtime;

/// <summary>Observe native batch completion and also read state when the mod loads after the event.</summary>
internal static class BridgeAssetLoading
{
    private static readonly FieldInfo? Caching = typeof(GameManager).GetField("m_CachingPdxDatabaseTask", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo? Cached = typeof(FileSystemDataSource).GetMethod("IsDataCached", BindingFlags.NonPublic | BindingFlags.Instance);
    private static ParadoxModsDataSource? _source;
    private static Action? _wake;
    private static bool _batch;

    internal static void Start(Action wake)
    {
        Stop();
        _wake = wake;
        Connect();
    }
    private static void Connect()
    {
        var manager = GameManager.instance;
        if (manager == null || manager.configuration.disablePDXSDK || manager.configuration.disableModding) return;
        if (AssetDatabase<ParadoxMods>.instance.dataSource is not ParadoxModsDataSource source || ReferenceEquals(source, _source)) return;
        if (_source != null) Unsubscribe();
        _source = source;
        _source.onEntryIsInActivePlaysetChanged += Batch;
        _source.onAfterActivePlaysetOrModStatusChanged += Completed;
    }
    private static Task Batch(IReadOnlyCollection<Colossal.Hash128> ids, bool active)
    {
        _batch = true;
        _wake?.Invoke();
        return Task.CompletedTask;
    }
    private static void Completed() { _batch = false; _wake?.Invoke(); }
    internal static bool Ready
    {
        get
        {
            var manager = GameManager.instance;
            if (manager == null || manager.modManager?.isInitialized != true) return false;
            if (manager.configuration.disablePDXSDK || manager.configuration.disableModding) return true;
            Connect();
            // Unknown native state fails closed. A completed title screen alone is insufficient.
            return !_batch && _source != null && Caching?.GetValue(manager) is Task task
                && task.Status == TaskStatus.RanToCompletion && Cached != null
                && Cached.Invoke(_source, new object[] { true }) is true
                && Cached.Invoke(_source, new object[] { false }) is true;
        }
    }
    private static void Unsubscribe()
    {
        _source!.onEntryIsInActivePlaysetChanged -= Batch;
        _source.onAfterActivePlaysetOrModStatusChanged -= Completed;
    }
    internal static void Stop()
    {
        if (_source != null) Unsubscribe();
        _source = null; _wake = null; _batch = false;
    }
}
