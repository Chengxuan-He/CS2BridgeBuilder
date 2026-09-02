using System;
using System.Collections.Generic;

namespace BridgePrefabGenerator.Settings;

internal readonly struct OptionText
{
    internal OptionText(string label, string description)
    {
        Label = label;
        Description = description;
    }

    internal string Label { get; }
    internal string Description { get; }
}

/// <summary>
/// One locale's worth of user visible text.
/// Two consumers: <see cref="BridgeLocaleSource"/> feeds the page title, tabs and groups into the
/// game's dictionary, and <see cref="BridgeSetting"/> writes the option labels straight onto the
/// widgets. The second path exists on purpose - if the dictionary is ever missing, the page still
/// reads as text instead of raw locale ids.
/// </summary>
internal sealed class UiStrings
{
    internal string Title = string.Empty;
    internal string TabRoads = string.Empty;
    internal string TabBridge = string.Empty;
    internal string GroupDeck = string.Empty;
    internal string GroupStyle = string.Empty;
    internal string GroupLowerDeck = string.Empty;
    internal string TabOptions = string.Empty;
    internal string GroupStatus = string.Empty;
    internal string GroupSelection = string.Empty;
    internal string GroupActions = string.Empty;
    internal string GroupRoads = string.Empty;
    internal string DetailSummary = string.Empty;
    internal string DetailLastExport = string.Empty;
    internal string GroupExport = string.Empty;
    internal string GroupMaintenance = string.Empty;

    internal string StatusNotExported = string.Empty;
    internal string StatusExported = string.Empty;
    internal string StatusOutdated = string.Empty;
    internal string StatusExportedPendingRestart = string.Empty;
    internal string StatusRemovedPendingRestart = string.Empty;
    internal string StateNameConflicts = string.Empty;
    internal string StatePageIndicator = string.Empty;

    internal string StateNoWorld = string.Empty;
    internal string StateGameplayBlocked = string.Empty;
    internal string StateScanning = string.Empty;
    internal string StateNoRoads = string.Empty;
    internal string StateBrokenRoads = string.Empty;
    internal string StateReady = string.Empty;
    internal string StateSelected = string.Empty;
    internal string StateRestartHint = string.Empty;
    internal string StateReportHint = string.Empty;
    internal string OperationSummary = string.Empty;
    internal string GroupBridge = string.Empty;
    internal string StateNoStyles = string.Empty;
    internal string StateStyleSource = string.Empty;
    internal string StateDoubleDeckExperimental = string.Empty;
    internal string OptionDonorBuildStyle = string.Empty;
    internal string OptionLowerDeckNone = string.Empty;
    internal string StyleNotAvailable = string.Empty;
    internal string StateStyleNotInstalled = string.Empty;
    internal string StateNoUpperDeck = string.Empty;
    internal string StateUpperDeck = string.Empty;
    internal string StateStyleFit = string.Empty;
    internal string StateLowerDeck = string.Empty;
    internal string StateDirectionOpposite = string.Empty;
    internal string StateDirectionSame = string.Empty;
    internal string StateExportName = string.Empty;
    internal string OptionNoDeckChosen = string.Empty;
    internal string NothingSelected = string.Empty;



    private readonly Dictionary<string, string> _deckKinds = new(StringComparer.Ordinal);

    /// <summary>Names one deck kind - road, train, subway, tram - keyed by its enum name.</summary>
    internal UiStrings DeckKind(string id, string name)
    {
        _deckKinds[id] = name;
        return this;
    }

    internal string DeckKindName(string id) => _deckKinds.TryGetValue(id, out var name) ? name : id;

    private readonly Dictionary<string, string> _styleNames = new(StringComparer.Ordinal);

    /// <summary>Names one bridge style, keyed by its stable id.</summary>
    internal UiStrings Style(string id, string name)
    {
        _styleNames[id] = name;
        return this;
    }

    /// <summary>
    /// The style's name in this locale. Falls back to the id, which is readable English rather than a
    /// locale key, so a style added without a translation still reads as a name.
    /// </summary>
    internal string StyleName(string id) =>
        _styleNames.TryGetValue(id, out var name) ? name : id;

    private readonly Dictionary<string, OptionText> _options = new();

    /// <summary>Adds the label and optional description of one option, keyed by its property name.</summary>
    internal UiStrings Option(string property, string label, string description = "")
    {
        _options[property] = new OptionText(label, description);
        return this;
    }

    internal bool TryGetOption(string? property, out OptionText text)
    {
        if (property != null) return _options.TryGetValue(property, out text);
        text = default;
        return false;
    }

    internal IEnumerable<KeyValuePair<string, OptionText>> Options => _options;
}
