using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;

namespace CS2Mods.Shared.Export;

/// <summary>
/// Makes freshly written prefabs usable in the running session.
///
/// Writing the asset is only the first of three steps, and stopping after the first two is what makes
/// a newly created prefab crash the moment it is placed.
///
///   1. Register the prefab. <see cref="PrefabSystem.AddOrUpdatePrefab"/> takes the ScriptableObject
///      and queues it; the prefab now exists as far as the asset menu is concerned, which is why it
///      can be picked and placed at all.
///   2. Build its entity. The queue is applied, and the prefab's components are turned into ECS data
///      by the prefab initialisation system. Until that has run the prefab entity exists but is
///      missing the data the placement jobs read - and those jobs are compiled without bounds or null
///      checks, so what follows is not an exception but a hard crash inside command buffer playback.
///   3. Refresh what is listing it, so the editor's panel shows the new entry rather than a stale one.
///
/// Step 2 normally happens on some later frame of its own accord, which is why a restart always
/// "fixed" it: after a restart the prefab is loaded through the ordinary path with initialisation
/// included. Placing something in the window between step 1 and step 2 is the crash. So the steps are
/// driven here, in order, before the export reports success.
/// </summary>
internal static class WorldRegistration
{
    /// <summary>
    /// Registers <paramref name="nodes"/> and drives the prefab pipeline until they are placeable.
    /// Dependencies are registered before the roots that reference them.
    /// </summary>
    internal static void Publish(World world, PrefabSystem prefabSystem, IReadOnlyList<PrefabCloneNode> nodes, ExportReport report)
    {
        var pending = nodes.Where(node => node.NeedsSave).ToList();
        var order = pending.Where(node => !node.IsRoot).Reverse().Concat(pending.Where(node => node.IsRoot));

        var registered = 0;
        foreach (var node in order)
        {
            try
            {
                prefabSystem.AddOrUpdatePrefab(node.Target);
                registered++;
            }
            catch (Exception exception)
            {
                report.Failed(node.Target.name, exception);
            }
        }

        report.Note($"Registered {registered} of {pending.Count} prefabs in the running session.");
        if (registered == 0) return;

        var initialised = Initialize(world, prefabSystem, report);
        RefreshEditor(world, report);

        if (!initialised)
        {
            report.Warning(
                "The new prefabs are registered but their entity data could not be built in this "
                + "session. Restart the game before placing them: placing a prefab whose entity is "
                + "incomplete crashes the game rather than failing safely.");
        }
    }

    /// <summary>
    /// Applies the queued prefabs and builds their entity data, then re-evaluates availability.
    /// Returns false if any step could not be driven, which is the caller's cue to warn rather than
    /// let the player place something half-built.
    /// </summary>
    private static bool Initialize(World world, PrefabSystem prefabSystem, ExportReport report)
    {
        var ok = true;

        // Applies whatever AddOrUpdatePrefab queued.
        ok &= Run(world, typeof(PrefabSystem), report);

        // Turns the prefab's components into the ECS data the placement jobs read. This is the step
        // whose absence produced the crash.
        ok &= Run(world, typeof(PrefabInitializeSystem), report);

        try
        {
            prefabSystem.UpdateAvailable();
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(exception, "Could not refresh prefab availability after registering the export");
            ok = false;
        }

        return ok;
    }

    /// <summary>Runs one system immediately, out of its usual phase.</summary>
    private static bool Run(World world, Type systemType, ExportReport report)
    {
        try
        {
            var system = world.GetExistingSystemManaged(systemType);
            if (system == null)
            {
                report.Note($"{systemType.Name} is not present in this world; skipped.");
                return false;
            }

            system.Update();
            return true;
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(exception, $"Could not run {systemType.Name} after registering the export");
            return false;
        }
    }

    /// <summary>
    /// Asks the editor's asset panel to rebuild its list.
    ///
    /// Reached by reflection because the method is not public, and treated as optional: it only
    /// decides whether the new entry appears without reopening the panel. Failing here leaves a
    /// cosmetic problem, not an unsafe one, so it never fails the export.
    /// </summary>
    private static void RefreshEditor(World world, ExportReport report)
    {
        try
        {
            var panelType = Type.GetType("Game.UI.Editor.PrefabToolPanelSystem, Game", false);
            if (panelType == null) return;

            var panel = world.GetExistingSystemManaged(panelType);
            if (panel == null) return;

            var update = panelType.GetMethod(
                "UpdatePrefabs",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            if (update == null) return;

            update.Invoke(panel, null);
            report.Note("Refreshed the editor's prefab panel.");
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(exception, "Could not refresh the editor's prefab panel; reopen it to see the new entry.");
        }
    }
}
