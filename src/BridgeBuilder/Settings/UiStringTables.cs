namespace BridgeBuilder.Settings;

/// <summary>
/// The shipped translations, one factory per locale. Split across a few files by language family so
/// each stays readable; every table sets exactly the same keys.
/// </summary>
internal static partial class UiStringTables
{
    internal static UiStrings English() => new UiStrings
    {
        Title = "Road Prefab Exporter",
        TabRoads = "Roads",
        TabOptions = "Options",
        GroupStatus = "Status",
        GroupSelection = "Selection",
        GroupActions = "Actions",
        GroupRoads = "Road list",
        DetailSummary = "Width ~{0} m - speed limit {1}",
        DetailLastExport = "Last export: {0}",
        GroupExport = "Export",
        GroupMaintenance = "Maintenance",

        StatusNotExported = "not exported",
        StatusExported = "exported",
        StatusOutdated = "changed since the last export",
        StatusExportedPendingRestart = "exported just now",
        StatusRemovedPendingRestart = "removed now - restart required",

        StateNoWorld = "No world loaded. Open the Editor to list Road Builder roads.",
        StateGameplayBlocked = "Exporting outside the Editor is off. Open the Editor, or turn on \"Allow exporting outside the Editor\".",
        StateScanning = "Waiting for Road Builder to finish generating its roads...",
        StateNoRoads = "No Road Builder roads found. Check that Road Builder is enabled in this playset.",
        StateBrokenRoads = "{0} road(s) skipped: Road Builder could not generate them (configuration missing).",
        StateNameConflicts = "{0} road(s) skipped: the name is not unique. Rename them in Road Builder.",
        StatePageIndicator = "Page {0} of {1} - showing {2}-{3} of {4}.",
        StateReady = "{0} roads: {1} exported, {2} not exported, {3} changed since export.",
        StateSelected = "{0} ticked.",
        StateRestartHint = "Exported roads are registered immediately; no restart needed.",
        StateReportHint = "Full report: ModsData\\RoadPrefabExporter\\last-export-report.txt",
        OperationSummary = "Last run: {0} exported, {1} removed, {2} skipped, {3} failed.",
        NothingSelected = "Nothing to do: no road is ticked.",
    }
        .Option(nameof(BridgeSetting.StatusText), "Current state",
            "Roads are listed here while a world containing Road Builder roads is loaded.")
        .Option(nameof(BridgeSetting.RescanRoads), "Rescan roads",
            "Read the road list and the export state again.")
        .Option(nameof(BridgeSetting.ExportSelected), "Export ticked roads",
            "Converts every ticked road into a native RoadPrefab asset. Restart the game before using the results.")
        .Option(nameof(BridgeSetting.ArmRemoval), "Allow removal",
            "Safety catch. Removal deletes asset files and cannot be undone, so the removal button stays disabled until this is on.")
        .Option(nameof(BridgeSetting.RemoveSelected), "Remove exports of ticked roads",
            "Deletes the exported assets of every ticked road. Roads already placed in a city will break.")
        .Option(nameof(BridgeSetting.OverwriteExisting), "Overwrite existing exports",
            "Export a road again even when its asset already exists.")
        .Option(nameof(BridgeSetting.AllowGameplayExport), "Allow exporting outside the Editor",
            "Off by default: writing user assets from a city save is riskier than doing it in the Editor.")
        .Option(nameof(BridgeSetting.RemoveUnusedDependencies), "Also remove unused dependencies",
            "After a removal, delete exported net sections and pieces that no remaining exported road references.")
        .Option(nameof(BridgeSetting.EmbedIcons), "Embed thumbnails into the assets",
            "Makes an exported road self-contained, so its thumbnail still works when the asset is shared or this mod is disabled. Costs roughly 20-70 KB per road. When off, thumbnails are served from this mod's folder and only work on your own machine.");
}
