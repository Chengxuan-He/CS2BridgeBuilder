using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using BridgeBuilder.Settings;
using Colossal.UI.Binding;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine.Scripting;

namespace BridgeBuilder.UI;

public partial class BridgeBuilderUISystem : UISystemBase
{
    private const string Group = "BridgeBuilder";
    private PrefabSystem _prefabSystem = null!;
    private ValueBinding<string> _title = null!;
    private ValueBinding<bool> _panelOpen = null!;
    private ValueBinding<BridgeDeckUiItem[]> _decks = null!;
    private ValueBinding<BridgeStyleUiItem[]> _styles = null!;
    private ValueBinding<BridgeRegistrationUiItem[]> _bridges = null!;
    private ValueBinding<string> _status = null!;
    private ValueBinding<IReadOnlyDictionary<string, string>> _texts = null!;
    private string? _seenLocaleId;
    private int _seenRevision = -1;
    private int _seenPreviewRevision = -1;
    private ValueBinding<string> _previewImage = null!;
    private ValueBinding<string> _previewStatus = null!;
    private ValueBinding<string> _previewKey = null!;
    private ValueBinding<bool> _previewLoading = null!;

    // The runtime builder owns loaded-save prefabs and resolves icons from a settled gameplay
    // PrefabSystem. UISystemBase applies this mask during OnGamePreload, so the editor never runs
    // RefreshBindings while its prefab database is still being constructed.
    public override GameMode gameMode => GameMode.Game;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

        _title = AddValue("Title", BridgeBuilder.Settings.UiStringCatalog.Current.Title, new BridgeStringWriter());
        _panelOpen = AddValue("PanelOpen", false, new BridgeBoolWriter());
        _decks = AddValue("Decks", Array.Empty<BridgeDeckUiItem>(), new BridgeWritableArrayWriter<BridgeDeckUiItem>());
        _styles = AddValue("Styles", Array.Empty<BridgeStyleUiItem>(), new BridgeWritableArrayWriter<BridgeStyleUiItem>());
        _bridges = AddValue("Bridges", Array.Empty<BridgeRegistrationUiItem>(), new BridgeWritableArrayWriter<BridgeRegistrationUiItem>());
        _status = AddValue("Status", string.Empty, new BridgeStringWriter());
        _texts = AddValue("Texts", RuntimeUiText.ForLocale(UiStringCatalog.Current.LocaleId), new BridgeTextWriter());
        _previewImage = AddValue("PreviewImage", string.Empty, new BridgeStringWriter());
        _previewStatus = AddValue("PreviewStatus", string.Empty, new BridgeStringWriter());
        _previewKey = AddValue("PreviewKey", string.Empty, new BridgeStringWriter());
        _previewLoading = AddValue("PreviewLoading", false, new BridgeBoolWriter());
        AddBinding(new TriggerBinding<BridgeRuntimeRequest>(Group, "PreviewBridge", PreviewBridge,
            new BridgeRecipeReader()));
        AddBinding(new TriggerBinding<string>(Group, "PreviewExistingBridge", PreviewExistingBridge,
            new BridgeStringReader()));
        AddBinding(new TriggerBinding(Group, "ClearPreview", BridgePreviewState.Clear));

        AddBinding(new TriggerBinding(Group, "TogglePanel", TogglePanel));
        AddBinding(new TriggerBinding<BridgeRuntimeRequest>(Group, "CreateBridge",
            request => QueueCreate(request, false), new BridgeRecipeReader()));
        AddBinding(new TriggerBinding<BridgeRuntimeRequest>(Group, "CreateAndBuildBridge",
            request => QueueCreate(request, true), new BridgeRecipeReader()));
        AddBinding(new TriggerBinding<string>(Group, "ActivateBridge", QueueActivate, new BridgeStringReader()));
        AddBinding(new TriggerBinding<string, string>(
            Group, "RenameBridge", QueueRename, new BridgeStringReader(), new BridgeStringReader()));
        AddBinding(new TriggerBinding<string>(Group, "DeleteBridge", ConfirmDelete, new BridgeStringReader()));

        // OnGamePreload enables the system only when the GameMode.Game mask above matches. Keeping
        // it disabled until that callback also closes the short startup window before a mode exists.
        Enabled = false;
    }

    [Preserve]
    protected override void OnUpdate()
    {
        var localeId = GameManager.instance?.localizationManager?.activeLocaleId ?? "en-US";
        var localeChanged = !string.Equals(_seenLocaleId, localeId, StringComparison.Ordinal);
        if (localeChanged)
        {
            _seenLocaleId = localeId;
            _texts.Update(RuntimeUiText.ForLocale(localeId));
        }
        // Translate existing results without recreating their temporary geometry.
        if (localeChanged || _seenPreviewRevision != BridgePreviewState.ResultRevision)
        {
            _seenPreviewRevision = BridgePreviewState.ResultRevision;
            _previewImage.Update(BridgePreviewState.Image);
            _previewStatus.Update(BridgePreviewState.Status);
            _previewKey.Update(BridgePreviewState.Key);
            _previewLoading.Update(BridgePreviewState.Loading);
        }
        var revision = BridgeRuntimeRequests.Revision;
        if (localeChanged || _seenRevision != revision)
        {
            _seenRevision = revision;
            RefreshBindings();
        }
        base.OnUpdate();
    }

    internal static void RequestRefresh() => BridgeRuntimeRequests.Touch();

    internal void CloseForBuild()
    {
        // Called only after the native tool accepted the formal prefab. Never toggle:
        // an asynchronous publication may finish after the player already closed the panel.
        _panelOpen.Update(false);
        BridgePreviewState.Clear();
    }

    private ValueBinding<T> AddValue<T>(string key, T initial, IWriter<T> writer)
    {
        var binding = new ValueBinding<T>(Group, key, initial, writer, null);
        AddBinding(binding);
        return binding;
    }

    private void TogglePanel()
    {
        var opening = !_panelOpen.value;
        _panelOpen.Update(opening);
        if (opening)
        {
            BridgeRuntimeRequests.Enqueue(
                new BridgeRuntimeRequest { Action = BridgeRuntimeAction.Refresh },
                "Scanning");
        }
        else BridgePreviewState.Clear();
    }

    private void PreviewBridge(BridgeRuntimeRequest request)
    {
        if (!_panelOpen.value) return;
        BridgePreviewState.Select(request.UpperDeckId, request.LowerDeckId, request.StyleId,
            request.LowerDeckOpposite);
    }

    private void PreviewExistingBridge(string prefabName)
    {
        if (!_panelOpen.value) return;
        BridgePreviewState.SelectExisting(prefabName ?? string.Empty);
    }

    private static void QueueCreate(BridgeRuntimeRequest request, bool buildAfterCreate)
    {
        request.Action = BridgeRuntimeAction.Create;
        request.BuildAfterCreate = buildAfterCreate;
        BridgeRuntimeRequests.Enqueue(request, "Creating");
    }

    private static void QueueActivate(string prefabName)
    {
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
        {
            Action = BridgeRuntimeAction.Activate,
            PrefabName = prefabName ?? string.Empty,
        }, "Activating");
    }

    private static void QueueRename(string prefabName, string registrationName)
    {
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
        {
            Action = BridgeRuntimeAction.Rename,
            PrefabName = prefabName ?? string.Empty,
            RegistrationName = registrationName ?? string.Empty,
        }, "Renaming");
    }

    private static void ConfirmDelete(string prefabName)
    {
        var registration = BridgeRegistrationStore.Find(prefabName);
        if (registration == null)
        {
            BridgeRuntimeRequests.Complete("DeleteMissing");
            return;
        }

        try
        {
            var dialog = new ConfirmationDialog(
                LocalizedString.Value(UiStringCatalog.Current.Title),
                LocalizedString.Value(RuntimeUiText.Get("ConfirmDelete", registration.RegistrationName)),
                LocalizedString.Value(RuntimeUiText.Get("Delete")),
                LocalizedString.Value(RuntimeUiText.Get("Cancel")),
                Array.Empty<LocalizedString>());
            GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(dialog, result =>
            {
                if (result != 0) return;
                BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
                {
                    Action = BridgeRuntimeAction.Delete,
                    PrefabName = registration.PrefabName,
                }, "Deleting");
            });
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not display the bridge deletion confirmation");
            BridgeRuntimeRequests.Complete("DeleteDialogFailed");
        }
    }

    private void RefreshBindings()
    {
        try
        {
            var decks = DeckCatalog.Decks.Select(deck => new BridgeDeckUiItem
            {
                Id = deck.Id,
                // Built-in asset names follow the current dictionary, not the
                // language cached at catalogue scan time. Custom names stay intact.
                Name = deck.Road?.Name ?? DeckCatalog.DisplayNameOf(deck.Prefab),
                Icon = ImageSystem.GetIcon(deck.Prefab) ?? string.Empty,
                Kind = UiStringCatalog.Current.DeckKindName(deck.Kind.ToString()),
                NetworkType = deck.Prefab is RoadPrefab road
                    ? (road.m_HighwayRules ? "Highway" : road.m_RoadType == RoadType.PublicTransport ? "PublicTransport" : "Road")
                    : deck.Kind.ToString(),
                Width = deck.Width,
            }).ToArray();

            var styles = BridgeStyleCatalog.Styles
                .Where(style => style.IsInstalled &&
                    BridgeStyleDefinitions.CanGenerate(style.Id) &&
                    style.Variants.Any(variant => variant.IsAvailable))
                .Select(style =>
                {
                    var preview = style.Variants.FirstOrDefault(variant => variant.IsAvailable);
                    return new BridgeStyleUiItem
                    {
                        Id = style.Id,
                        Name = style.DisplayName,
                        Icon = preview == null ? string.Empty : ImageSystem.GetIcon(preview.Donor) ?? string.Empty,
                        Source = preview?.Source.LocalizedLabel ?? string.Empty,
                        SupportsSingle = style.Variants.Any(variant => variant.IsAvailable && !variant.IsDoubleDeck),
                        SupportsDouble = style.Variants.Any(variant => variant.IsAvailable && variant.IsDoubleDeck),
                    };
                }).ToArray();

            var loaded = PrefabCatalog.GetAll(_prefabSystem)
                .OfType<NetGeometryPrefab>()
                .GroupBy(prefab => prefab.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var bridges = BridgeRegistrationStore.Load().Select(registration =>
            {
                loaded.TryGetValue(registration.PrefabName, out var prefab);
                return new BridgeRegistrationUiItem
                {
                    PrefabName = registration.PrefabName,
                    RegistrationName = registration.RegistrationName,
                    UpperDeckId = registration.UpperDeckId,
                    LowerDeckId = registration.LowerDeckId ?? string.Empty,
                    StyleId = registration.StyleId,
                    IsDoubleDeck = registration.IsDoubleDeck,
                    Available = prefab != null,
                    Icon = prefab == null ? string.Empty : ImageSystem.GetIcon(prefab) ?? string.Empty,
                };
            }).ToArray();

            _decks.Update(decks);
            _styles.Update(styles);
            _bridges.Update(bridges);
            _title.Update(BridgeBuilder.Settings.UiStringCatalog.Current.Title);
            _status.Update(BridgeRuntimeRequests.Status);
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not update the BridgeBuilder runtime UI");
            _status.Update(RuntimeUiText.Get("UiRefreshFailed"));
        }
    }
}

internal sealed class BridgeDeckUiItem : IJsonWritable
{
    internal string Id { get; set; } = string.Empty;
    internal string Name { get; set; } = string.Empty;
    internal string Icon { get; set; } = string.Empty;
    internal string Kind { get; set; } = string.Empty;
    internal string NetworkType { get; set; } = string.Empty;
    internal float Width { get; set; }

    public void Write(IJsonWriter writer)
    {
        writer.TypeBegin(nameof(BridgeDeckUiItem));
        Property(writer, "id", Id);
        Property(writer, "name", Name);
        Property(writer, "icon", Icon);
        Property(writer, "kind", Kind);
        Property(writer, "networkType", NetworkType);
        writer.PropertyName("width"); writer.Write(Width);
        writer.TypeEnd();
    }

    private static void Property(IJsonWriter writer, string name, string value)
    {
        writer.PropertyName(name); writer.Write(value);
    }
}

internal sealed class BridgeStyleUiItem : IJsonWritable
{
    internal string Id { get; set; } = string.Empty;
    internal string Name { get; set; } = string.Empty;
    internal string Icon { get; set; } = string.Empty;
    internal string Source { get; set; } = string.Empty;
    internal bool SupportsSingle { get; set; }
    internal bool SupportsDouble { get; set; }

    public void Write(IJsonWriter writer)
    {
        writer.TypeBegin(nameof(BridgeStyleUiItem));
        Property(writer, "id", Id);
        Property(writer, "name", Name);
        Property(writer, "icon", Icon);
        Property(writer, "source", Source);
        writer.PropertyName("supportsSingle"); writer.Write(SupportsSingle);
        writer.PropertyName("supportsDouble"); writer.Write(SupportsDouble);
        writer.TypeEnd();
    }

    private static void Property(IJsonWriter writer, string name, string value)
    {
        writer.PropertyName(name); writer.Write(value);
    }
}

internal sealed class BridgeRegistrationUiItem : IJsonWritable
{
    internal string PrefabName { get; set; } = string.Empty;
    internal string RegistrationName { get; set; } = string.Empty;
    internal string UpperDeckId { get; set; } = string.Empty;
    internal string LowerDeckId { get; set; } = string.Empty;
    internal string StyleId { get; set; } = string.Empty;
    internal string Icon { get; set; } = string.Empty;
    internal bool IsDoubleDeck { get; set; }
    internal bool Available { get; set; }

    public void Write(IJsonWriter writer)
    {
        writer.TypeBegin(nameof(BridgeRegistrationUiItem));
        Property(writer, "prefabName", PrefabName);
        Property(writer, "registrationName", RegistrationName);
        Property(writer, "upperDeckId", UpperDeckId);
        Property(writer, "lowerDeckId", LowerDeckId);
        Property(writer, "styleId", StyleId);
        Property(writer, "icon", Icon);
        writer.PropertyName("isDoubleDeck"); writer.Write(IsDoubleDeck);
        writer.PropertyName("available"); writer.Write(Available);
        writer.TypeEnd();
    }

    private static void Property(IJsonWriter writer, string name, string value)
    {
        writer.PropertyName(name); writer.Write(value);
    }
}

internal sealed class BridgeWritableArrayWriter<T> : IWriter<T[]> where T : IJsonWritable
{
    public void Write(IJsonWriter writer, T[] value)
    {
        JsonWriterExtensions.ArrayBegin(writer, value?.Length ?? 0);
        if (value != null)
            foreach (var item in value) item.Write(writer);
        writer.ArrayEnd();
    }
}

internal sealed class BridgeStringWriter : IWriter<string>
{
    public void Write(IJsonWriter writer, string value) => writer.Write(value ?? string.Empty);
}

internal sealed class BridgeTextWriter : IWriter<IReadOnlyDictionary<string, string>>
{
    public void Write(IJsonWriter writer, IReadOnlyDictionary<string, string> value)
    {
        writer.TypeBegin("BridgeUiTexts");
        foreach (var entry in value)
        {
            writer.PropertyName(entry.Key);
            writer.Write(entry.Value);
        }
        writer.TypeEnd();
    }
}

internal sealed class BridgeBoolWriter : IWriter<bool>
{
    public void Write(IJsonWriter writer, bool value) => writer.Write(value);
}

internal sealed class BridgeStringReader : IReader<string>
{
    public void Read(IJsonReader reader, out string value)
    {
        value = string.Empty;
        reader.Read(out value);
        value ??= string.Empty;
    }
}

// One recipe snapshot is used for both preview and creation. Direction is not
// mutable global UI state that could change while a queued creation is pending.
internal sealed class BridgeRecipeReader : IReader<BridgeRuntimeRequest>
{
    public void Read(IJsonReader reader, out BridgeRuntimeRequest value)
    {
        reader.ReadMapBegin();
        reader.ReadProperty("upperDeckId");
        reader.Read(out string upper);
        reader.ReadProperty("lowerDeckId");
        reader.Read(out string lower);
        reader.ReadProperty("styleId");
        reader.Read(out string style);
        reader.ReadProperty("registrationName");
        reader.Read(out string name);
        reader.ReadProperty("lowerDeckOpposite");
        reader.Read(out bool opposite);
        reader.ReadMapEnd();
        value = new BridgeRuntimeRequest
        {
            UpperDeckId = upper ?? string.Empty, LowerDeckId = lower ?? string.Empty,
            StyleId = style ?? string.Empty, RegistrationName = name ?? string.Empty,
            LowerDeckOpposite = opposite
        };
    }
}
