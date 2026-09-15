using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using Colossal.UI.Binding;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
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
    private int _seenRevision = -1;

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

        AddBinding(new TriggerBinding(Group, "TogglePanel", TogglePanel));
        AddBinding(new TriggerBinding(Group, "Refresh", Refresh));
        AddBinding(new TriggerBinding<string, string, string, string>(
            Group, "CreateBridge", QueueCreate,
            new BridgeStringReader(), new BridgeStringReader(), new BridgeStringReader(), new BridgeStringReader()));
        AddBinding(new TriggerBinding<string>(Group, "ActivateBridge", QueueActivate, new BridgeStringReader()));
        AddBinding(new TriggerBinding<string, string>(
            Group, "RenameBridge", QueueRename, new BridgeStringReader(), new BridgeStringReader()));
        AddBinding(new TriggerBinding<string>(Group, "DeleteBridge", ConfirmDelete, new BridgeStringReader()));
    }

    [Preserve]
    protected override void OnUpdate()
    {
        var revision = BridgeRuntimeRequests.Revision;
        if (_seenRevision != revision)
        {
            _seenRevision = revision;
            RefreshBindings();
        }
        base.OnUpdate();
    }

    internal static void RequestRefresh() => BridgeRuntimeRequests.Touch();

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
                "正在读取道路与桥梁原型…");
        }
    }

    private static void Refresh()
    {
        BridgeRuntimeRequests.Enqueue(
            new BridgeRuntimeRequest { Action = BridgeRuntimeAction.Refresh },
            "正在刷新道路与桥梁原型…");
    }

    private static void QueueCreate(string upperDeckId, string lowerDeckId, string styleId, string registrationName)
    {
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
        {
            Action = BridgeRuntimeAction.Create,
            UpperDeckId = upperDeckId ?? string.Empty,
            LowerDeckId = lowerDeckId ?? string.Empty,
            StyleId = styleId ?? string.Empty,
            RegistrationName = registrationName ?? string.Empty,
        }, "正在创建桥梁 Prefab…");
    }

    private static void QueueActivate(string prefabName)
    {
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
        {
            Action = BridgeRuntimeAction.Activate,
            PrefabName = prefabName ?? string.Empty,
        }, "正在激活桥梁建造工具…");
    }

    private static void QueueRename(string prefabName, string registrationName)
    {
        BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
        {
            Action = BridgeRuntimeAction.Rename,
            PrefabName = prefabName ?? string.Empty,
            RegistrationName = registrationName ?? string.Empty,
        }, "正在更新显示名称…");
    }

    private static void ConfirmDelete(string prefabName)
    {
        var registration = BridgeRegistrationStore.Find(prefabName);
        if (registration == null)
        {
            BridgeRuntimeRequests.Complete("删除失败：找不到该 UUID 对应的桥梁。");
            return;
        }

        try
        {
            var dialog = new ConfirmationDialog(
                LocalizedString.Value("BridgeBuilder"),
                LocalizedString.Value($"删除“{registration.RegistrationName}”及地图中使用它建造的桥梁？桥梁构造无法恢复。"),
                LocalizedString.Value("删除"),
                LocalizedString.Value("取消"),
                Array.Empty<LocalizedString>());
            GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(dialog, result =>
            {
                if (result != 0) return;
                BridgeRuntimeRequests.Enqueue(new BridgeRuntimeRequest
                {
                    Action = BridgeRuntimeAction.Delete,
                    PrefabName = registration.PrefabName,
                }, "正在删除桥梁…");
            });
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, "Could not display the bridge deletion confirmation");
            BridgeRuntimeRequests.Complete("无法打开删除确认框，未删除任何桥梁。");
        }
    }

    private void RefreshBindings()
    {
        try
        {
            var decks = DeckCatalog.Decks.Select(deck => new BridgeDeckUiItem
            {
                Id = deck.Id,
                Name = deck.DisplayName,
                Icon = ImageSystem.GetIcon(deck.Prefab) ?? string.Empty,
                Kind = deck.Kind.ToString(),
                Width = deck.Width,
                CanBeUpper = deck.IsRoad,
            }).ToArray();

            var styles = BridgeStyleCatalog.Styles
                .Where(style => style.IsInstalled)
                .Select(style =>
                {
                    var preview = style.Variants.FirstOrDefault();
                    return new BridgeStyleUiItem
                    {
                        Id = style.Id,
                        Name = style.DisplayName,
                        Icon = preview == null ? string.Empty : ImageSystem.GetIcon(preview.Donor) ?? string.Empty,
                        Source = style.Source,
                        SupportsSingle = style.Variants.Any(variant => !variant.IsDoubleDeck),
                        SupportsDouble = style.Variants.Any(variant => variant.IsDoubleDeck),
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
            _status.Update("界面数据刷新失败；请查看 BridgeBuilder 日志。");
        }
    }
}

internal sealed class BridgeDeckUiItem : IJsonWritable
{
    internal string Id { get; set; } = string.Empty;
    internal string Name { get; set; } = string.Empty;
    internal string Icon { get; set; } = string.Empty;
    internal string Kind { get; set; } = string.Empty;
    internal float Width { get; set; }
    internal bool CanBeUpper { get; set; }

    public void Write(IJsonWriter writer)
    {
        writer.TypeBegin(nameof(BridgeDeckUiItem));
        Property(writer, "id", Id);
        Property(writer, "name", Name);
        Property(writer, "icon", Icon);
        Property(writer, "kind", Kind);
        writer.PropertyName("width"); writer.Write(Width);
        writer.PropertyName("canBeUpper"); writer.Write(CanBeUpper);
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
