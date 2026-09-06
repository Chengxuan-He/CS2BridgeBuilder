using BridgeBuilder.Bridges;
using BridgeBuilder.Settings;
using BridgeBuilder.Systems;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.Logging;
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

        Log.Info($"{Id} loaded (build {BuildStamp()})");
        try
        {
            RoadBuilderIconExporter.RegisterHost();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Unable to register the exported bridge icon directory");
        }

        try
        {
            RegisterSettings(this);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Unable to register the settings page. Road selection will not be available.");
        }

        updateSystem.UpdateAt<BridgeGenerationSystem>(SystemUpdatePhase.PrefabUpdate);
    }

    public void OnDispose()
    {
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
    /// Makes the options UI ask for the page again. A setting's page is built once, at registration,
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
                .OpenPage(setting.id, BridgeSetting.BridgeTab, false);
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not reopen the exporter's options page after refreshing it.");
        }
    }

    internal static void ShowMessage(string title, string message)
    {
        try
        {
            var dialog = new MessageDialog(
                LocalizedString.Value(title),
                LocalizedString.Value(message),
                LocalizedString.Value("OK"));
            GameManager.instance?.userInterface?.appBindings?.ShowMessageDialog(dialog, _ => { });
        }
        catch (Exception exception)
        {
            Log.Warn(exception, "Could not show the result dialog");
        }
    }

    private static void RegisterSettings(Mod mod)
    {
        var setting = new BridgeSetting(mod);
        setting.RegisterInOptionsUI();

        AssetDatabase.global.LoadSettings(Id, setting, new BridgeSetting(mod));
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
