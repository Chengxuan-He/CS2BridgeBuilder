namespace CS2Mods.Shared.Infrastructure;

/// <summary>
/// The handful of format strings <see cref="RoadSelectionModel.Describe"/> needs. Supplied by the
/// host so the shared model stays out of the localization tables, which differ per mod.
/// </summary>
internal interface ISelectionText
{
    /// <summary>{0} total, {1} exported, {2} pending, {3} outdated.</summary>
    string Ready { get; }

    /// <summary>{0} selected.</summary>
    string Selected { get; }

    /// <summary>{0} page, {1} pages, {2} first shown, {3} last shown, {4} total.</summary>
    string PageIndicator { get; }

    string RestartHint { get; }

    string ReportHint { get; }
}
