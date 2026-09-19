using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Infrastructure;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.UI.InGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;

namespace BridgeBuilder.Systems;

/// <summary>
/// Registers a bridge without driving any game system recursively. Scheduled after the native
/// PrefabUpdate pipeline; only then may the bridge be offered to the placement tool.
/// </summary>
public partial class BridgePublicationSystem : GameSystemBase
{
    private PrefabSystem _prefabs = null!;
    private PrefabBase[] _targets = Array.Empty<PrefabBase>();
    private readonly Dictionary<PrefabBase, Entity> _replaced =
        new(ReferenceEqualityComparer<PrefabBase>.Instance);
    private ExportReport? _report;
    private Action<bool>? _completed;
    private bool _awaitedReplacementPass;

    internal bool IsPending => _completed != null;

    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabs = World.GetOrCreateSystemManaged<PrefabSystem>();
        Enabled = false;
    }

    internal bool Publish(IReadOnlyList<PrefabCloneNode> nodes, ExportReport report, Action<bool> completed)
    {
        if (IsPending)
        {
            report.Failed("Bridge publication", new InvalidOperationException(
                "The previous bridge is still being initialized."));
            return false;
        }

        var pending = nodes.Where(node => node.NeedsSave).ToArray();
        var targets = pending.Where(node => !node.IsRoot).Reverse()
            .Concat(pending.Where(node => node.IsRoot))
            .Select(node => node.Target)
            .Distinct(ReferenceEqualityComparer<PrefabBase>.Instance).ToArray();
        _replaced.Clear();
        try
        {
            foreach (var target in targets)
            {
                if (_prefabs.TryGetEntity(target, out var oldEntity))
                    _replaced[target] = oldEntity;
                _prefabs.AddOrUpdatePrefab(target);
                // AddOrUpdatePrefab returns void and AddPrefab may reject without throwing.
                if (!_prefabs.TryGetEntity(target, out _))
                {
                    report.Failed(target.name, new InvalidOperationException("Prefab registration was rejected."));
                    return false;
                }
            }
        }
        catch (Exception exception)
        {
            report.Failed("Bridge publication", exception);
            return false;
        }

        _targets = targets;
        _report = report;
        _completed = completed;
        _awaitedReplacementPass = false;
        Enabled = true;
        report.Note($"Registered {targets.Length} prefabs; waiting for the scheduled native prefab pipeline.");
        return true;
    }

    protected override void OnUpdate()
    {
        var completed = _completed;
        var report = _report;
        if (completed == null || report == null) { Enabled = false; return; }

        // UpdatePrefab is applied by the outer PrefabSystem at the start of the NEXT main loop.
        // Never force it here: a newly added prefab has already run the native pipeline once.
        var ready = false;
        try
        {
            var waiting = _replaced.Keys.FirstOrDefault(target =>
                _prefabs.TryGetEntity(target, out var entity) && entity == _replaced[target]);
            if (waiting != null && !_awaitedReplacementPass)
            {
                _awaitedReplacementPass = true;
                return;
            }
            ready = waiting == null
                ? ValidateInitialized(report)
                : NotReady(waiting, "the native prefab update did not replace its old entity", report);
        }
        catch (Exception exception)
        {
            report.Failed("Bridge initialization", exception);
        }

        // Clear before calling user code. Success, failure and UI refresh each happen only once.
        _completed = null;
        _report = null;
        _targets = Array.Empty<PrefabBase>();
        _replaced.Clear();
        Enabled = false;
        try
        {
            completed(ready);
        }
        catch (Exception)
        {
            // Generation diagnostics are silent; retain the external API exception boundary.
        }
        if (ready) RefreshMenus(report);
    }

    private bool ValidateInitialized(ExportReport report)
    {
        foreach (var target in _targets)
        {
            if (!_prefabs.TryGetEntity(target, out var entity)
                || !EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity))
                return NotReady(target, "its live prefab entity is missing", report);

            if (target is NetPrefab)
            {
                if (!EntityManager.HasComponent<NetData>(entity))
                    return NotReady(target, "network data is missing", report);
                var net = EntityManager.GetComponentData<NetData>(entity);
                if (!net.m_NodeArchetype.Valid || !net.m_EdgeArchetype.Valid)
                    return NotReady(target, "network instance archetypes are not initialized", report);
            }

            if (target is NetGeometryPrefab geometry && geometry.m_Sections?.Length > 0)
            {
                if (!EntityManager.HasBuffer<NetGeometrySection>(entity)
                    || EntityManager.GetBuffer<NetGeometrySection>(entity, true).Length == 0)
                    return NotReady(target, "network sections are not initialized", report);
            }

            var ui = target.components.OfType<UIObject>().FirstOrDefault();
            if (ui?.m_Group == null || ui.m_IsDebugObject) continue;
            if (!EntityManager.HasComponent<UIObjectData>(entity)
                || !_prefabs.TryGetEntity(ui.m_Group, out var group)
                || EntityManager.GetComponentData<UIObjectData>(entity).m_Group != group
                || !EntityManager.HasBuffer<UIGroupElement>(group))
                return NotReady(target, "its menu category is not initialized", report);

            var entries = EntityManager.GetBuffer<UIGroupElement>(group, true);
            var count = 0;
            for (var index = 0; index < entries.Length; index++)
                if (entries[index].m_Prefab == entity) count++;
            if (count != 1)
                return NotReady(target, $"its menu category contains {count} entries instead of one", report);
        }
        return true;
    }

    private static bool NotReady(PrefabBase target, string reason, ExportReport report)
    {
        report.Failed(target.name, new InvalidOperationException(
            $"Bridge placement was not activated because {reason}. Existing assets were retained."));
        return false;
    }

    internal void RefreshMenus(ExportReport report)
    {
        // Toolbar bindings cache category contents. Refresh the data, not the prefab initializer,
        // so creating another bridge in the already-open road category also updates its list.
        try
        {
            var toolbar = World.GetExistingSystemManaged<ToolbarUISystem>();
            if (toolbar != null)
            {
                RefreshBinding(toolbar, "m_ToolbarGroupsBinding", "Update");
                RefreshBinding(toolbar, "m_AssetMenuCategoriesBinding", "UpdateAll");
                RefreshBinding(toolbar, "m_AssetsBinding", "UpdateAll");
            }
            var panelType = Type.GetType("Game.UI.Editor.PrefabToolPanelSystem, Game", false);
            var panel = panelType == null ? null : World.GetExistingSystemManaged(panelType);
            if (panel != null)
                panelType!.GetMethod("UpdatePrefabs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null)?.Invoke(panel, null);
        }
        catch (Exception)
        {
            report.Warning("The bridge is initialized, but the open menu could not be refreshed.");
        }
    }

    private static void RefreshBinding(ToolbarUISystem toolbar, string fieldName, string methodName)
    {
        var binding = typeof(ToolbarUISystem).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(toolbar);
        binding?.GetType().GetMethod(methodName, Type.EmptyTypes)?.Invoke(binding, null);
    }
}
