
using BridgeBuilder.Settings;
using BridgeBuilder.Systems;
using BridgeBuilder.UI;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.Logging;
using Colossal.UI;
using CS2Mods.Shared;
using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using Game.UI.Menu;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;

namespace BridgeBuilder;

public sealed class Mod : IMod
{
    public const string Id = nameof(BridgeBuilder);

    internal static ILog Log { get; } = LogManager.GetLogger(Id).SetShowsErrorsInUI(true);

    internal static BridgeSetting? Setting { get; private set; }

    // Static for the same reason as in the road exporter: the game does not construct the mod class
    // the ordinary way, so its instance field initializers never run and any instance state read at
    // load time comes back null.
    private static readonly Dictionary<string, BridgeLocaleSource> LocaleSources =
        new(StringComparer.OrdinalIgnoreCase);

    private static Action? _onSupportedLocalesChanged;

    public void OnLoad(UpdateSystem updateSystem)
    {
        ModHost.Initialize(Id, "BridgeBuilder", Log);
        ModHost.PageRebuilder = RebuildOptionsPage;
        RoadSelectionModel.Text = new BridgeSelectionText();
        // A bridge made from a road must not collide with that same road exported by the road
        // exporter, and its generated dependencies must not collide either.
        ModHost.DefaultNamePrefix = "RBBridge";
        BridgeBuilder.Runtime.BridgeRailSeamPatch.Start();

        Log.Info($"{Id} loaded (build {BuildStamp()})");
        try
        {
            RoadBuilderIconExporter.RegisterHost();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Unable to register the exported bridge icon directory");
        }

        RegisterUiHost(this);
        BridgeRecoveryLocation.MigrateLegacy();

        try
        {
            RegisterSettings(this);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Unable to register the settings page. Road selection will not be available.");
        }

        // Register new prefabs before the native pipeline, and publish the result only after it.
        // Re-entering PrefabSystem/PrefabInitializeSystem from inside PrefabUpdate duplicates
        // UIGroupElement entries (UIObject.LateInitialize appends without a uniqueness check).
        updateSystem.UpdateBefore<BridgeGenerationSystem>(SystemUpdatePhase.PrefabUpdate);
        updateSystem.UpdateAfter<BridgePublicationSystem>(SystemUpdatePhase.PrefabUpdate);
        updateSystem.UpdateAt<BridgeBuilderUISystem>(SystemUpdatePhase.UIUpdate);
        // Optional manager setting; persisted native conditions work without this system.
        updateSystem.UpdateAt<BridgeUnlockSystem>(SystemUpdatePhase.UIUpdate);
        // Self-check waits for native/PDX asset batches and mod initialization at a ready main menu.
        updateSystem.UpdateAt<BridgeStartupAssetSystem>(SystemUpdatePhase.UIUpdate);
        updateSystem.UpdateAt<BridgeRailSeamAuditSystem>(SystemUpdatePhase.UIUpdate);
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<BridgeStartupAssetSystem>().ScheduleInspectionAfterLoad();
    }

    public void OnDispose()
    {
        Runtime.BridgeBulkRemoval.Stop();
        BridgeStartupAssetSystem.StopInspection();
        BridgeBuilder.Runtime.BridgeStartupRecovery.Stop();
        BridgeBuilder.Runtime.BridgeRailSeamPatch.Stop();
        BridgeBuilder.Runtime.BridgeSessionState.Stop();
        try
        {
            RoadBuilderIconExporter.UnregisterHost();
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Unable to unregister the exported bridge icon directory");
        }

        try
        {
            UIManager.defaultUISystem.RemoveHostLocation("bridgebuilderui");
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Unable to unregister the BridgeBuilder UI asset directory");
        }
        try
        {
            UnregisterSettings();
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Unable to unregister the settings page");
        }

        Log.Info($"{Id} disposed");
    }

    private static string BuildStamp()
    {
        try
        {
            return typeof(Mod).Assembly.ManifestModule.ModuleVersionId.ToString("N").Substring(0, 12);
        }
        catch (Exception)
        {
            return "unknown";
        }
    }

    /// <summary>
    /// Makes the options UI ask for the page again. A setting's page is built once, at assetInfo,
    /// long before a world exists, so neither the road checkboxes nor the discovered bridge styles
    /// would ever appear without this.
    /// </summary>
    internal static void RebuildOptionsPage()
    {
        var setting = Setting;
        if (setting == null) return;

        var wasOnScreen = RoadSelectionModel.PageOnScreen;
        try
        {
            setting.UnregisterInOptionsUI();
            setting.RegisterInOptionsUI();
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not rebuild the options page. Close and reopen the options menu to refresh it.");
            return;
        }

        if (wasOnScreen) ReopenOwnPage(setting);
    }

    private static void ReopenOwnPage(BridgeSetting setting)
    {
        try
        {
            World.DefaultGameObjectInjectionWorld?
                .GetExistingSystemManaged<OptionsUISystem>()?
                .OpenPage(setting.id, BridgeSetting.OptionsTab, false);
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not reopen the exporter's options page after refreshing it.");
        }
    }

    internal static void ShowRecoveryMessage(string message)
    {
        try
        {
            var dialog = new ConfirmationDialog(
                LocalizedString.Value(UiStringCatalog.Current.Title),
                LocalizedString.Value(message),
                LocalizedString.Value(RuntimeUiText.Get("RecoveryOpenLabel")),
                null,
                LocalizedString.Value(RuntimeUiText.Get("RecoveryRestartLabel")));
            GameManager.instance?.userInterface?.appBindings?.ShowConfirmationDialog(dialog, result =>
            {
                if (result == 0) BridgeRecoveryLocation.Open();
                // Native close/Esc returns 1 as well as the cancel button. Restart must be
                // an otherAction (2), never cancelAction, so dismissing cannot quit the game.
                else if (result == 2) Runtime.BridgeGameRestart.Run();
            });
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not show bridge recovery dialog");
        }
    }

    internal static void ShowMessage(string title, string message)
    {
        try
        {
            var dialog = new MessageDialog(
                LocalizedString.Value(title == "Bridge Builder" ? UiStringCatalog.Current.Title : title),
                LocalizedString.Value(message),
                LocalizedString.Value(RuntimeUiText.Get("OK")));
            GameManager.instance?.userInterface?.appBindings?.ShowMessageDialog(dialog, _ => { });
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not show the result dialog");
        }
    }

    internal static void ReloadActiveLocale()
    {
        try
        {
            GameManager.instance?.localizationManager?.ReloadActiveLocale();
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not refresh generated bridge display names");
        }
    }

    private static void RegisterUiHost(Mod mod)
    {
        try
        {
            ExecutableAsset executable = null!;
            if (!GameManager.instance.modManager.TryGetExecutableAsset(mod, out executable))
            {
                Log.Error("Unable to locate BridgeBuilder UI assets");
                return;
            }

            var directory = Path.GetDirectoryName(executable.path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                Log.Error("BridgeBuilder executable directory is empty");
                return;
            }
            UIManager.defaultUISystem.AddHostLocation("bridgebuilderui", directory, false, 0);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Unable to register the BridgeBuilder UI asset directory");
        }
    }

    private static void RegisterSettings(Mod mod)
    {
        var setting = new BridgeSetting(mod);
        setting.RegisterInOptionsUI();

        AssetDatabase.global.LoadSettings(Id, setting, new BridgeSetting(mod));
        setting.RegisterKeyBindings();
        Setting = setting;

        AddLocaleSources(setting);

        _onSupportedLocalesChanged = () => AddLocaleSources(setting);
        GameManager.instance.localizationManager.onSupportedLocalesChanged += _onSupportedLocalesChanged;
    }

    private static void AddLocaleSources(BridgeSetting setting)
    {
        LocalizationManager? manager = GameManager.instance?.localizationManager;
        if (manager == null)
        {
            Log.Warn("No localization manager yet; option labels will be registered once locales are known.");
            return;
        }

        foreach (var localeId in UiStringCatalog.LocaleIds)
        {
            if (LocaleSources.ContainsKey(localeId)) continue;
            try
            {
                var source = new BridgeLocaleSource(setting, UiStringCatalog.ForLocale(localeId));
                manager.AddSource(localeId, source);
                LocaleSources[localeId] = source;
            }
            catch (Exception exception)
            {
                Log.Warn(exception, $"Could not register option labels for locale '{localeId}'");
            }
        }
    }

    private static void UnregisterSettings()
    {
        LocalizationManager? manager = null;
        try
        {
            manager = GameManager.instance?.localizationManager;
        }
        catch (Exception)
        {
            manager = null;
        }

        if (_onSupportedLocalesChanged != null)
        {
            if (manager != null) manager.onSupportedLocalesChanged -= _onSupportedLocalesChanged;
            _onSupportedLocalesChanged = null;
        }

        if (manager != null)
        {
            foreach (var entry in LocaleSources)
            {
                try
                {
                    manager.RemoveSource(entry.Key, entry.Value);
                }
                catch (Exception exception)
                {
                    Log.Warn(exception, $"Could not unregister option labels for locale '{entry.Key}'");
                }
            }
        }

        LocaleSources.Clear();

        try
        {
            Setting?.UnregisterInOptionsUI();
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not unregister the options page");
        }

        Setting = null;
    }
}
