using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using Game.Modding;
using Game.Prefabs;
using Game.Settings;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBuilder.Settings;

/// <summary>
/// The options page.
///
/// One bridge is built at a time, from two explicitly chosen decks. That is not a simplification of a
/// batch flow, it is the shape the job actually has: a bridge is a pairing - this deck on top, that
/// one underneath, in this style - and there is no sensible way to apply one pairing to a list of
/// roads. The road exporter next door is the batch tool; this one is a bench.
/// </summary>
[FileLocation("ModsSettings/BridgeBuilder/BridgeBuilder")]
[SettingsUITabOrder(BridgeTab, OptionsTab)]
[SettingsUIGroupOrder(StatusGroup, DeckGroup, StyleGroup, LowerDeckGroup, ActionsGroup, ExportGroup, MaintenanceGroup)]
[SettingsUIShowGroupName(DeckGroup, StyleGroup, LowerDeckGroup, ActionsGroup, ExportGroup, MaintenanceGroup)]
public sealed class BridgeSetting : ModSetting
{
    internal const string BridgeTab = "Bridge";
    internal const string OptionsTab = "Options";

    internal const string StatusGroup = "Status";
    internal const string DeckGroup = "Deck";
    internal const string StyleGroup = "Style";
    internal const string LowerDeckGroup = "LowerDeck";
    internal const string ActionsGroup = "Actions";
    internal const string ExportGroup = "Export";
    internal const string MaintenanceGroup = "Maintenance";

    /// <summary>The dropdown entry that means "do not override what the style itself says".</summary>
    internal const string DonorBuildStyle = "";

    /// <summary>The lower deck entry that means "none": a single deck bridge.</summary>
    internal const string NoLowerDeck = "";

    public BridgeSetting(IMod mod) : base(mod)
    {
    }

    [SettingsUISection(BridgeTab, StatusGroup)]
    [SettingsUIMultilineText("")]
    public string StatusText
    {
        get
        {
            RoadSelectionModel.NotePageViewed();
            return RoadSelectionModel.Describe();
        }
    }

    /// <summary>
    /// What the generated bridge is called.
    ///
    /// Shows a generated name by default, and that default is regenerated whenever the configuration
    /// changes - the deck, the style, the second deck. A name left alone therefore always describes
    /// the bridge that will be produced, rather than one two settings ago, which is the failure mode a
    /// remembered name has: it stops matching what it names and nothing says so.
    ///
    /// Typing over it makes it the player's, until the configuration changes again. That is the same
    /// rule, and it is the honest one: the field says what the export will be called, and the export
    /// is what the configuration says it is.
    /// </summary>
    [SettingsUISection(BridgeTab, DeckGroup)]
    [SettingsUITextInput]
    public string BridgeName { get; set; } = string.Empty;

    /// <summary>
    /// Puts the generated name back in the field, because what it named has changed.
    ///
    /// Called from every setter that takes part in the name. Not from the constructor: the catalogues
    /// are not populated before a world is scanned, so a name generated then would be built out of
    /// blanks and would then look like a name the player had chosen.
    /// </summary>
    private void RegenerateName()
    {
        var upper = DeckCatalog.Find(UpperDeckId);
        if (upper == null) return;

        BridgeName = BridgeNaming.BaseName(
            upper, DeckCatalog.Find(LowerDeckId), BridgeStyleCatalog.Find(BridgeStyleId));
    }

    [SettingsUISection(BridgeTab, DeckGroup)]
    [SettingsUIDropdown(typeof(BridgeSetting), nameof(GetDecks))]
    public string UpperDeckId
    {
        get => _upperDeckId;
        set
        {
            if (string.Equals(_upperDeckId, value, StringComparison.Ordinal)) return;
            _upperDeckId = value ?? string.Empty;
            RegenerateName();
        }
    }

    private string _upperDeckId = string.Empty;

    [SettingsUISection(BridgeTab, StyleGroup)]
    [SettingsUIDropdown(typeof(BridgeSetting), nameof(GetBridgeStyles))]
    public string BridgeStyleId
    {
        get => _bridgeStyleId;
        set
        {
            if (string.Equals(_bridgeStyleId, value, StringComparison.Ordinal)) return;
            _bridgeStyleId = value ?? BridgeStyleDefinitions.Default;
            RegenerateName();
        }
    }

    private string _bridgeStyleId = BridgeStyleDefinitions.Default;

    [SettingsUISection(BridgeTab, StyleGroup)]
    [SettingsUIDropdown(typeof(BridgeSetting), nameof(GetBuildStyles))]
    public string BuildStyleOverride { get; set; } = DonorBuildStyle;

    [SettingsUISection(BridgeTab, LowerDeckGroup)]
    [SettingsUIDropdown(typeof(BridgeSetting), nameof(GetLowerDecks))]
    public string LowerDeckId
    {
        get => _lowerDeckId;
        set
        {
            if (string.Equals(_lowerDeckId, value, StringComparison.Ordinal)) return;
            _lowerDeckId = value ?? NoLowerDeck;
            RegenerateName();
        }
    }

    private string _lowerDeckId = NoLowerDeck;

    /// <summary>
    /// Which way the second deck runs. A choice, unlike the separation.
    ///
    /// The separation is geometry - the structure is drawn around two decks at one distance and any
    /// other distance puts one through the other - while direction is traffic. Nothing about the
    /// bridge is drawn differently for a lower deck running the other way, so this is the player's.
    /// </summary>
    [SettingsUISection(BridgeTab, LowerDeckGroup)]
    [SettingsUIDisableByCondition(typeof(BridgeSetting), nameof(NoLowerDeckChosen))]
    public bool LowerDeckOpposite { get; set; } = true;

    /// <summary>
    /// How far the second deck runs from the first is the archetype's, not the player's. The towers,
    /// portals and cables are drawn around two decks at one separation; every other value puts the
    /// second deck through geometry modelled to clear it.
    ///
    /// It was a slider from four to twenty-four metres. It is gone rather than disabled, because a
    /// control that cannot change anything is worse than no control - it says the value is a choice.
    /// <c>BridgeStyleVariant.LowerDeck</c> is where the separation is read from now.
    /// </summary>
    [SettingsUIHidden]
    public float DeckSpacing { get; set; } = 8f;

    [SettingsUISection(BridgeTab, ActionsGroup)]
    [SettingsUIButton]
    public bool RescanRoads
    {
        set => RoadSelectionModel.Request(ExporterRequest.Refresh);
    }

    [SettingsUISection(BridgeTab, ActionsGroup)]
    [SettingsUIButton]
    [SettingsUIDisableByCondition(typeof(BridgeSetting), nameof(CannotExport))]
    public bool ExportSelected
    {
        set => RoadSelectionModel.Request(ExporterRequest.ExportSelected);
    }

    [SettingsUISection(BridgeTab, ActionsGroup)]
    public bool ArmRemoval { get; set; }

    [SettingsUISection(BridgeTab, ActionsGroup)]
    [SettingsUIButton]
    [SettingsUIDisableByCondition(typeof(BridgeSetting), nameof(CannotRemove))]
    public bool RemoveSelected
    {
        set
        {
            RoadSelectionModel.Request(ExporterRequest.RemoveSelected);
            ArmRemoval = false;
            ApplyAndSave();
        }
    }

    [SettingsUISection(OptionsTab, ExportGroup)]
    public bool OverwriteExisting { get; set; } = true;

    [SettingsUISection(OptionsTab, ExportGroup)]
    public bool EmbedIcons { get; set; }

    [SettingsUISection(OptionsTab, ExportGroup)]
    public bool AllowGameplayExport { get; set; }

    [SettingsUISection(OptionsTab, MaintenanceGroup)]
    public bool RemoveUnusedDependencies { get; set; } = true;

    public override void SetDefaults()
    {
        UpperDeckId = string.Empty;
        BridgeStyleId = BridgeStyleDefinitions.Default;
        BuildStyleOverride = DonorBuildStyle;
        LowerDeckId = NoLowerDeck;
        BridgeName = string.Empty;
        LowerDeckOpposite = true;
        DeckSpacing = 8f;
        OverwriteExisting = true;
        EmbedIcons = false;
        AllowGameplayExport = false;
        RemoveUnusedDependencies = true;
        ArmRemoval = false;
    }

    public bool NoLowerDeckChosen() => string.IsNullOrEmpty(LowerDeckId);

    /// <summary>
    /// Exporting needs a deck to convert and a style to copy a look from. Both conditions are only
    /// meaningful once a world has been scanned; before that the button stays enabled rather than
    /// telling the player their perfectly valid choice is invalid.
    /// </summary>
    public bool CannotExport()
    {
        if (!DeckCatalog.Scanned) return false;
        if (DeckCatalog.Find(UpperDeckId) == null) return true;
        return BridgeStyleCatalog.Scanned && BridgeStyleCatalog.Resolve(BridgeStyleId)?.IsInstalled != true;
    }

    public bool CannotRemove() => !ArmRemoval || DeckCatalog.Find(UpperDeckId) == null;

    /// <summary>The bridge options as the export system needs them.</summary>
    internal BridgeOptions ToBridgeOptions() => new()
    {
        BuildStyle = ParseBuildStyle(BuildStyleOverride),
        // The second deck is enabled by choosing one, not by a separate switch. One control that can
        // disagree with another is one control too many.
        DoubleDeck = !string.IsNullOrEmpty(LowerDeckId),
        LowerDeckId = string.IsNullOrEmpty(LowerDeckId) ? null : LowerDeckId,
        LowerDeckOpposite = LowerDeckOpposite,
        DeckSpacing = DeckSpacing,
    };

    private static BridgeBuildStyle? ParseBuildStyle(string value)
    {
        return Enum.TryParse(value, out BridgeBuildStyle parsed) ? parsed : null;
    }

    /// <summary>
    /// Everything that can be the upper deck: Road Builder roads, the roads already registered -
    /// including ones exported earlier - and the train, subway and tram tracks.
    /// </summary>
    public static DropdownItem<string>[] GetDecks()
    {
        UiStrings text = UiStringCatalog.Current;
        var items = new List<DropdownItem<string>>
        {
            new()
            {
                value = string.Empty,
                displayName = LocalizedString.Value(text.OptionNoDeckChosen),
            },
        };

        items.AddRange(DeckCatalog.Decks.Select(deck => new DropdownItem<string>
        {
            value = deck.Id,
            displayName = LocalizedString.Value(Label(text, deck)),
        }));

        return items.ToArray();
    }

    public static DropdownItem<string>[] GetLowerDecks()
    {
        UiStrings text = UiStringCatalog.Current;
        var items = new List<DropdownItem<string>>
        {
            new()
            {
                value = NoLowerDeck,
                displayName = LocalizedString.Value(text.OptionLowerDeckNone),
            },
        };

        items.AddRange(DeckCatalog.Decks.Select(deck => new DropdownItem<string>
        {
            value = deck.Id,
            displayName = LocalizedString.Value(Label(text, deck)),
        }));

        return items.ToArray();
    }

    /// <summary>
    /// "Road Builder - My Road (18 m)". The kind is spelled out because the same list mixes roads and
    /// tracks, and the width because it is what decides which variant of a style will fit.
    /// </summary>
    private static string Label(UiStrings text, Deck deck)
    {
        var kind = text.DeckKindName(deck.Kind.ToString());
        var width = deck.Width > 0f
            ? $" ({deck.Width.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} m)"
            : string.Empty;
        return $"{kind} · {deck.DisplayName}{width}";
    }

    /// <summary>
    /// The bridge styles, always as a list of named styles.
    ///
    /// The list does not depend on anything having been scanned. This page is built when the mod
    /// loads, long before a world exists, so a list assembled from discovered prefabs would be empty
    /// exactly when the player first opens it. A style that genuinely has nothing behind it is still
    /// listed and marked, rather than missing.
    /// </summary>
    public static DropdownItem<string>[] GetBridgeStyles()
    {
        UiStrings text = UiStringCatalog.Current;
        var scanned = BridgeStyleCatalog.Scanned;
        return BridgeStyleCatalog.Styles.Select(style => new DropdownItem<string>
        {
            value = style.Id,
            displayName = LocalizedString.Value(
                !scanned || style.IsInstalled
                    ? style.DisplayName
                    : string.Format(text.StyleNotAvailable, style.DisplayName)),
        }).ToArray();
    }

    public static DropdownItem<string>[] GetBuildStyles()
    {
        var items = new List<DropdownItem<string>>
        {
            new()
            {
                value = DonorBuildStyle,
                displayName = LocalizedString.Value(UiStringCatalog.Current.OptionDonorBuildStyle),
            },
        };

        items.AddRange(Enum.GetNames(typeof(BridgeBuildStyle)).Select(name => new DropdownItem<string>
        {
            value = name,
            displayName = LocalizedString.Value(name),
        }));

        return items.ToArray();
    }

    public override AutomaticSettings.SettingPageData GetPageData(string prefix, bool addPrefix)
    {
        AutomaticSettings.SettingPageData page = base.GetPageData(prefix, addPrefix);
        try
        {
            ApplyOptionText(page, UiStringCatalog.Current);
        }
        catch (Exception exception)
        {
            ModHost.Log.Error(exception, "Unable to build the bridge settings page");
        }

        return page;
    }

    /// <summary>
    /// Writes the label and description of every option straight onto its widget. The dictionary
    /// source covers the same text, but registering it can fail; without this the page would then
    /// show raw locale ids for every entry.
    /// </summary>
    private static void ApplyOptionText(AutomaticSettings.SettingPageData page, UiStrings text)
    {
        foreach (var tab in page.tabs)
        {
            foreach (var item in tab.items)
            {
                if (!text.TryGetOption(item.property?.name, out var option)) continue;
                item.displayName = LocalizedString.Value(option.Label);
                item.description = LocalizedString.Value(option.Description);
            }
        }
    }
}
