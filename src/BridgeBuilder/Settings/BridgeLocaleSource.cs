using Colossal;
using System.Collections.Generic;

namespace BridgeBuilder.Settings;

/// <summary>
/// Supplies the page title, tab names and group names, which the widget builder resolves through the
/// game's dictionary and which cannot be set on the widgets directly. Option labels are also included
/// so the page reads correctly in the options search, but <see cref="BridgeSetting"/> writes those
/// onto the widgets as well, so a missing dictionary never leaves raw locale ids on screen.
/// </summary>
internal sealed class BridgeLocaleSource : IDictionarySource
{
    private readonly BridgeSetting _setting;
    private readonly UiStrings _text;

    internal BridgeLocaleSource(BridgeSetting setting, UiStrings text)
    {
        _setting = setting;
        _text = text;
    }

    public IEnumerable<KeyValuePair<string, string>> ReadEntries(
        IList<IDictionaryEntryError> errors,
        Dictionary<string, int> indexCounts)
    {
        var entries = new Dictionary<string, string>
        {
            { _setting.GetSettingsLocaleID(), _text.Title },
            { _setting.GetOptionTabLocaleID(BridgeSetting.BridgeTab), _text.TabBridge },
            { _setting.GetOptionTabLocaleID(BridgeSetting.OptionsTab), _text.TabOptions },
            { _setting.GetOptionGroupLocaleID(BridgeSetting.StatusGroup), _text.GroupStatus },
            { _setting.GetOptionGroupLocaleID(BridgeSetting.DeckGroup), _text.GroupDeck },
            { _setting.GetOptionGroupLocaleID(BridgeSetting.StyleGroup), _text.GroupStyle },
            { _setting.GetOptionGroupLocaleID(BridgeSetting.LowerDeckGroup), _text.GroupLowerDeck },
            { _setting.GetOptionGroupLocaleID(BridgeSetting.ActionsGroup), _text.GroupActions },
            { _setting.GetOptionGroupLocaleID(BridgeSetting.ExportGroup), _text.GroupExport },
            { _setting.GetOptionGroupLocaleID(BridgeSetting.MaintenanceGroup), _text.GroupMaintenance },
        };

        foreach (var option in _text.Options)
        {
            entries[_setting.GetOptionLabelLocaleID(option.Key)] = option.Value.Label;
            entries[_setting.GetOptionDescLocaleID(option.Key)] = option.Value.Description;
        }

        return entries;
    }

    public void Unload()
    {
    }
}
