using BridgeBuilder.Bridges;
using BridgeBuilder.Settings;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BridgeBuilder.Systems;

/// <summary>
/// A file-request boundary for bridge sample creation.
///
/// The caller writes one request and leaves all prefab work to the mod. This deliberately does not
/// start the game, press options-page buttons, or try to construct game prefabs in a PowerShell
/// process. The request is consumed when BridgeBuilder next has a loaded prefab world.
/// </summary>
internal sealed class BridgeSampleRequest
{
    private const string OperationName = "create-width-samples";

    private BridgeSampleRequest(string requestId, string roadId, IReadOnlyList<string> styleIds)
    {
        RequestId = requestId;
        RoadId = roadId;
        StyleIds = styleIds;
    }

    internal string RequestId { get; }

    /// <summary>Empty means that the mod selects any registered road with a measurable width.</summary>
    internal string RoadId { get; }

    internal IReadOnlyList<string> StyleIds { get; }

    internal static bool TryTake(out BridgeSampleRequest? request)
    {
        request = null;
        if (!File.Exists(ExportPaths.RequestFile)) return false;

        string requestId = "rejected";
        try
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in File.ReadAllLines(ExportPaths.RequestFile))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                var separator = line.IndexOf('=');
                if (separator <= 0) continue;
                fields[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
            }

            if (fields.TryGetValue("requestId", out var statedId) && statedId.Length > 0)
                requestId = SafePart(statedId);

            if (!fields.TryGetValue("operation", out var operation)
                || !string.Equals(operation, OperationName, StringComparison.Ordinal))
            {
                WriteStatus(requestId, "rejected", "Unsupported or missing operation.");
                return false;
            }

            if (!fields.TryGetValue("styles", out var statedStyles)) statedStyles = string.Empty;
            var styles = statedStyles
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (styles.Length == 0)
            {
                WriteStatus(requestId, "rejected", "No bridge style ids were supplied.");
                return false;
            }

            fields.TryGetValue("road", out var roadId);
            request = new BridgeSampleRequest(requestId, roadId ?? string.Empty, styles);
            return true;
        }
        catch (Exception exception)
        {
            ModHost.Log.Error(exception, "Could not read the BridgeBuilder API request");
            WriteStatus(requestId, "rejected", exception.GetType().Name + ": " + exception.Message);
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(ExportPaths.RequestFile)) File.Delete(ExportPaths.RequestFile);
            }
            catch (Exception exception)
            {
                ModHost.Log.Warn(exception, "Could not remove the consumed BridgeBuilder API request");
            }
        }
    }

    internal static void WriteStatus(string requestId, string state, params string[] details)
    {
        try
        {
            ExportPaths.EnsureDataDirectory();
            var lines = new List<string>
            {
                "UTC=" + DateTime.UtcNow.ToString("O"),
                "requestId=" + SafePart(requestId),
                "state=" + state,
            };
            lines.AddRange(details.Select(detail => "detail=" + detail));
            File.WriteAllLines(Path.Combine(ExportPaths.DataDirectory, "sample-api-status.txt"), lines);
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(exception, "Could not write the BridgeBuilder API status");
        }
    }

    internal static string SafePart(string? value)
    {
        var safe = NameSanitizer.MakeFileSystemSafe(value).Replace('.', '_').Trim();
        return safe.Length == 0 ? "request" : safe;
    }
}

/// <summary>State retained while one API request advances by one bridge per update.</summary>
internal sealed class BridgeSampleBatch
{
    private readonly List<string> _outcomes = new();
    private readonly BridgeSettingSnapshot _settingSnapshot;

    private BridgeSampleBatch(
        BridgeSampleRequest request,
        IReadOnlyList<Deck> roads,
        BridgeSetting setting,
        string resultsDirectory)
    {
        Request = request;
        Roads = roads;
        ResultsDirectory = resultsDirectory;
        _settingSnapshot = new BridgeSettingSnapshot(setting);
    }

    internal BridgeSampleRequest Request { get; }

    internal IReadOnlyList<Deck> Roads { get; }

    internal string ResultsDirectory { get; }

    internal int Index { get; private set; }

    internal int RoadIndex { get; private set; }

    internal bool IsComplete => Index >= Request.StyleIds.Count;

    internal string CurrentStyleId => IsComplete ? string.Empty : Request.StyleIds[Index];

    internal Deck CurrentRoad => Roads[RoadIndex];

    internal static BridgeSampleBatch? Start(
        BridgeSampleRequest request,
        IReadOnlyList<Deck> roads,
        BridgeSetting setting)
    {
        try
        {
            var directory = Path.Combine(
                ExportPaths.DataDirectory,
                "sample-api-results",
                BridgeSampleRequest.SafePart(request.RequestId));
            Directory.CreateDirectory(directory);
            var batch = new BridgeSampleBatch(request, roads, setting, directory);
            batch.WriteManifest("processing");
            BridgeSampleRequest.WriteStatus(
                request.RequestId,
                "processing",
                "roads=" + string.Join(",", roads.Select(road => road.Id)),
                "styles=" + string.Join(",", request.StyleIds),
                "results=" + directory);
            return batch;
        }
        catch (Exception exception)
        {
            ModHost.Log.Error(exception, "Could not start the BridgeBuilder sample request");
            BridgeSampleRequest.WriteStatus(
                request.RequestId,
                "failed",
                exception.GetType().Name + ": " + exception.Message);
            return null;
        }
    }

    internal void Record(string outcome)
    {
        _outcomes.Add(CurrentStyleId + ": " + outcome);
        WriteManifest("processing");
    }

    internal void ArchiveCurrentReport()
    {
        var prefix = (Index + 1).ToString("00") + "_"
            + BridgeSampleRequest.SafePart(CurrentStyleId) + "_attempt_"
            + (RoadIndex + 1).ToString("00") + "_" + BridgeSampleRequest.SafePart(CurrentRoad.Id);
        CopyEvidence(ExportPaths.ReportFile, Path.Combine(ResultsDirectory, prefix + "_export-report.txt"));
    }

    internal void AdvanceStyle()
    {
        Index++;
        RoadIndex = 0;
    }

    internal void AdvanceRoadOrStyle()
    {
        RoadIndex++;
        if (RoadIndex < Roads.Count) return;

        _outcomes.Add(CurrentStyleId + ": no measurable registered road produced a sample");
        AdvanceStyle();
        WriteManifest("processing");
    }

    internal void RestoreSetting(BridgeSetting setting) => _settingSnapshot.Restore(setting);

    internal void Complete()
    {
        CopyEvidence(
            Path.Combine(ExportPaths.DataDirectory, "asset-anatomy.txt"),
            Path.Combine(ResultsDirectory, "asset-anatomy.txt"));
        CopyEvidence(
            ExportPaths.MeasurementsFile,
            Path.Combine(ResultsDirectory, "tower-measurements.txt"));
        WriteManifest("completed");
        BridgeSampleRequest.WriteStatus(
            Request.RequestId,
            "completed",
            "roadsTriedFrom=" + Roads.Count,
            "completed=" + Index,
            "results=" + ResultsDirectory);
    }

    private void CopyEvidence(string source, string destination)
    {
        try
        {
            if (File.Exists(source))
            {
                File.Copy(source, destination, true);
                return;
            }

            _outcomes.Add(Path.GetFileName(destination) + ": source evidence file was not present");
        }
        catch (Exception exception)
        {
            _outcomes.Add(Path.GetFileName(destination) + ": "
                + exception.GetType().Name + ": " + exception.Message);
            ModHost.Log.Warn(exception, "Could not archive BridgeBuilder sample evidence");
        }
    }

    private void WriteManifest(string state)
    {
        try
        {
            var lines = new List<string>
            {
                "BridgeBuilder sample API result",
                "UTC=" + DateTime.UtcNow.ToString("O"),
                "requestId=" + Request.RequestId,
                "state=" + state,
                "availableRoads=" + Roads.Count,
                "currentRoad=" + (IsComplete ? string.Empty : CurrentRoad.Id),
                "styles=" + string.Join(",", Request.StyleIds),
                "completed=" + Index,
                string.Empty,
            };
            lines.AddRange(_outcomes);
            File.WriteAllLines(Path.Combine(ResultsDirectory, "manifest.txt"), lines);
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(exception, "Could not write the BridgeBuilder sample manifest");
        }
    }

    private sealed class BridgeSettingSnapshot
    {
        private readonly string _upperDeckId;
        private readonly string _bridgeStyleId;
        private readonly string _buildStyleOverride;
        private readonly string _lowerDeckId;
        private readonly string _bridgeName;
        private readonly bool _lowerDeckOpposite;
        private readonly bool _overwriteExisting;

        internal BridgeSettingSnapshot(BridgeSetting setting)
        {
            _upperDeckId = setting.UpperDeckId;
            _bridgeStyleId = setting.BridgeStyleId;
            _buildStyleOverride = setting.BuildStyleOverride;
            _lowerDeckId = setting.LowerDeckId;
            _bridgeName = setting.BridgeName;
            _lowerDeckOpposite = setting.LowerDeckOpposite;
            _overwriteExisting = setting.OverwriteExisting;
        }

        internal void Restore(BridgeSetting setting)
        {
            setting.UpperDeckId = _upperDeckId;
            setting.BridgeStyleId = _bridgeStyleId;
            setting.BuildStyleOverride = _buildStyleOverride;
            setting.LowerDeckId = _lowerDeckId;
            setting.LowerDeckOpposite = _lowerDeckOpposite;
            setting.OverwriteExisting = _overwriteExisting;
            setting.BridgeName = _bridgeName;
        }
    }
}
