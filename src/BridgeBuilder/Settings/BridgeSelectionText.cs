using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Settings;

/// <summary>
/// Feeds the shared selection model the few lines it prints, taken from whichever locale is active
/// at the moment of reading. Resolving per call is deliberate: the player can change language while
/// the options page is open, and the status text is rebuilt on every poll anyway.
/// </summary>
internal sealed class BridgeSelectionText : ISelectionText
{
    public string Ready => UiStringCatalog.Current.StateReady;
    public string Selected => UiStringCatalog.Current.StateSelected;
    public string PageIndicator => UiStringCatalog.Current.StatePageIndicator;
    public string RestartHint => UiStringCatalog.Current.StateRestartHint;
    public string ReportHint => UiStringCatalog.Current.StateReportHint;
}
