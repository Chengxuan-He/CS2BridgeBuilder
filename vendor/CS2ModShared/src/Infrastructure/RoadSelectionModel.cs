using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CS2Mods.Shared.Infrastructure;

internal enum RoadExportStatus
{
    NotExported,
    Exported,
    Outdated,
    ExportedThisSession,
    RemovedThisSession,
}

internal enum ExporterRequest
{
    None,
    Refresh,
    ExportSelected,
    RemoveSelected,
}

/// <summary>
/// One road as the settings page sees it. Deliberately holds no prefab reference: the options page
/// outlives the world, and a destroyed Unity object would still look non-null through a stale field.
/// </summary>
internal sealed class RoadListItem
{
    internal RoadListItem(string name, RoadExportStatus status, string detail)
    {
        Name = name;
        Status = status;
        Detail = detail;
    }

    /// <summary>Identity and label in one: the road's name is also its exported asset's name.</summary>
    internal string Name { get; }

    internal RoadExportStatus Status { get; }

    /// <summary>Shown on the right when the row is hovered: width, lane layout and last export.</summary>
    internal string Detail { get; }

    internal bool IsPending => Status is RoadExportStatus.NotExported or RoadExportStatus.Outdated or RoadExportStatus.RemovedThisSession;
}

/// <summary>
/// The only thing the settings page and the export system share. The page runs whenever the options
/// menu is open, the system only exists inside a loaded world, so neither may reach into the other.
/// Everything here is touched from the main thread only, but the lock keeps that assumption cheap to hold.
/// </summary>
internal static class RoadSelectionModel
{
    private static readonly object Gate = new();
    private static readonly HashSet<string> SelectedNames = new(StringComparer.Ordinal);
    private static IReadOnlyList<RoadListItem> _roads = Array.Empty<RoadListItem>();
    private static ExporterRequest _request = ExporterRequest.None;
    private static string _message = string.Empty;
    private static string _lastOperation = string.Empty;
    private static int _selectionVersion;
    private static object? _owner;
    private static string _renderedSignature = string.Empty;
    private static int _pageIndex;
    private static bool _pageViewPending;
    private static bool _pageOnScreen;

    /// <summary>
    /// Bumped on every selection change. The options page hands this to each checkbox so a bulk
    /// action such as "select all" tells the already rendered widgets to re-read their value.
    /// </summary>
    internal static int SelectionVersion
    {
        get { lock (Gate) return _selectionVersion; }
    }

    /// <summary>How many roads one page of the options list shows.</summary>
    internal const int PageSize = 8;

    internal static IReadOnlyList<RoadListItem> Roads
    {
        get { lock (Gate) return _roads; }
    }

    /// <summary>
    /// Pending roads first, then the exported ones, each alphabetically. Paging follows this order, so
    /// a page keeps meaning the same thing between rebuilds.
    /// </summary>
    private static List<RoadListItem> Ordered() =>
        _roads.OrderBy(road => road.IsPending ? 0 : 1)
            .ThenBy(road => road.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    internal static int PageCount
    {
        get { lock (Gate) return Math.Max(1, (_roads.Count + PageSize - 1) / PageSize); }
    }

    internal static int PageNumber
    {
        get { lock (Gate) return ClampPage() + 1; }
    }

    /// <summary>The roads of the current page, and the 1-based range they occupy in the whole list.</summary>
    internal static IReadOnlyList<RoadListItem> VisibleRoads(out int firstIndex, out int lastIndex)
    {
        lock (Gate)
        {
            var page = ClampPage();
            var visible = Ordered().Skip(page * PageSize).Take(PageSize).ToList();
            firstIndex = visible.Count == 0 ? 0 : page * PageSize + 1;
            lastIndex = page * PageSize + visible.Count;
            return visible;
        }
    }

    /// <summary>The full list in display order. The page is applied by hiding widgets, not by rebuilding.</summary>
    internal static IReadOnlyList<RoadListItem> OrderedRoads()
    {
        lock (Gate) return Ordered();
    }

    /// <summary>The hover text of one road, read live so a status change needs no page rebuild.</summary>
    internal static string DetailOf(string name)
    {
        lock (Gate)
        {
            foreach (var road in _roads)
                if (string.Equals(road.Name, name, StringComparison.Ordinal)) return road.Detail;
            return string.Empty;
        }
    }

    internal static bool IsOnCurrentPage(int index)
    {
        lock (Gate)
        {
            var page = ClampPage();
            return index >= page * PageSize && index < (page + 1) * PageSize;
        }
    }

    /// <summary>How many roads the current page actually holds - the last page is usually short.</summary>
    internal static int VisibleRoadCount
    {
        get
        {
            lock (Gate)
            {
                var page = ClampPage();
                return Math.Max(0, Math.Min(PageSize, _roads.Count - page * PageSize));
            }
        }
    }

    internal static void NextPage() => MovePage(1);

    internal static void PreviousPage() => MovePage(-1);

    /// <summary>
    /// Only moves the window. Every road already has a widget; the page decides which are visible, so
    /// turning a page never rebuilds the options page - a rebuild would drop the menu back to its first page.
    /// </summary>
    private static void MovePage(int delta)
    {
        lock (Gate)
        {
            var count = Math.Max(1, (_roads.Count + PageSize - 1) / PageSize);
            // Wrapping keeps both buttons useful at either end of a short list.
            _pageIndex = ((ClampPage() + delta) % count + count) % count;
            _selectionVersion++;
        }
    }

    private static int ClampPage()
    {
        var count = Math.Max(1, (_roads.Count + PageSize - 1) / PageSize);
        if (_pageIndex >= count) _pageIndex = count - 1;
        if (_pageIndex < 0) _pageIndex = 0;
        return _pageIndex;
    }

    /// <summary>
    /// Called while the options page is on screen. Roads can be edited with Road Builder in the same
    /// Editor session, so the list has to be re-read when the player comes back to look at it.
    /// </summary>
    internal static void NotePageViewed()
    {
        lock (Gate)
        {
            _pageViewPending = true;
            _pageOnScreen = true;
        }
    }

    /// <summary>True while the options menu is showing this mod's page.</summary>
    internal static bool PageOnScreen
    {
        get { lock (Gate) return _pageOnScreen; }
    }

    internal static void SetPageOnScreen(bool onScreen)
    {
        lock (Gate) _pageOnScreen = onScreen;
    }

    internal static bool TakePageViewed()
    {
        lock (Gate)
        {
            var pending = _pageViewPending;
            _pageViewPending = false;
            return pending;
        }
    }

    internal static bool HasRoads
    {
        get { lock (Gate) return _roads.Count > 0; }
    }

    internal static bool IsSelected(string name)
    {
        lock (Gate) return SelectedNames.Contains(name);
    }

    internal static void SetSelected(string name, bool selected)
    {
        lock (Gate)
        {
            if (selected) SelectedNames.Add(name);
            else SelectedNames.Remove(name);
            _selectionVersion++;
        }
    }

    internal static void SelectAll() => ReplaceSelection(road => true);

    internal static void SelectNone() => ReplaceSelection(road => false);

    internal static void SelectPending() => ReplaceSelection(road => road.IsPending);

    internal static void InvertSelection() => ReplaceSelection(road => !SelectedNames.Contains(road.Name));

    private static void ReplaceSelection(Func<RoadListItem, bool> predicate)
    {
        lock (Gate)
        {
            var keep = _roads.Where(predicate).Select(road => road.Name).ToList();
            // Names not in the current list belong to another playset and are kept as-is.
            foreach (var road in _roads) SelectedNames.Remove(road.Name);
            foreach (var id in keep) SelectedNames.Add(id);
            _selectionVersion++;
        }
    }

    /// <summary>Names that are both selected and currently present, in list order.</summary>
    internal static IReadOnlyList<string> SelectedRoadNames()
    {
        lock (Gate)
            return _roads.Where(road => SelectedNames.Contains(road.Name)).Select(road => road.Name).ToList();
    }

    internal static void PublishRoads(object owner, IReadOnlyList<RoadListItem> roads, string message)
    {
        bool changed;
        lock (Gate)
        {
            _owner = owner;
            _roads = roads;
            _message = message;
            changed = ContentChanged();
        }

        if (changed) ModHost.RebuildOptionsPage();
    }

    internal static void PublishMessage(object owner, string message)
    {
        bool changed;
        lock (Gate)
        {
            _owner = owner;
            _roads = Array.Empty<RoadListItem>();
            _message = message;
            changed = ContentChanged();
        }

        if (changed) ModHost.RebuildOptionsPage();
    }

    /// <summary>
    /// True when what the page would render differs from what it last rendered. The options UI only
    /// builds a page when the setting is registered, so a change has to trigger a rebuild - and a
    /// rebuild that changes nothing would disturb the menu for no reason.
    /// </summary>
    private static bool ContentChanged()
    {
        var builder = new StringBuilder(_message);
        builder.Append('|').Append(_lastOperation);
        // Only the set of names matters: statuses and details are read live by the widgets, and a
        // rebuild would throw the player out of the page they are looking at.
        foreach (var road in _roads) builder.Append('|').Append(road.Name);

        var signature = builder.ToString();
        if (string.Equals(signature, _renderedSignature, StringComparison.Ordinal)) return false;
        _renderedSignature = signature;
        return true;
    }

    /// <summary>
    /// Clears the list only if <paramref name="owner"/> is still the system that published it. A world
    /// teardown that lands after the next world already published must not wipe the fresh list.
    /// </summary>
    internal static void ReleaseIfOwner(object owner, string message)
    {
        bool changed;
        lock (Gate)
        {
            if (!ReferenceEquals(_owner, owner)) return;
            _owner = null;
            _roads = Array.Empty<RoadListItem>();
            _message = message;
            changed = ContentChanged();
        }

        if (changed) ModHost.RebuildOptionsPage();
    }

    internal static void PublishOperationResult(string summary)
    {
        bool changed;
        lock (Gate)
        {
            _lastOperation = summary;
            changed = ContentChanged();
        }

        if (changed) ModHost.RebuildOptionsPage();
    }

    internal static void Request(ExporterRequest request)
    {
        lock (Gate) _request = request;
    }

    internal static ExporterRequest TakeRequest()
    {
        lock (Gate)
        {
            var request = _request;
            _request = ExporterRequest.None;
            return request;
        }
    }

    /// <summary>Supplied by the host at load. Until then <see cref="Describe"/> returns nothing.</summary>
    internal static ISelectionText? Text { get; set; }

    internal static string Describe()
    {
        ISelectionText? text = Text;
        if (text == null) return string.Empty;
        var builder = new StringBuilder();
        List<RoadListItem> roads;
        string message;
        string lastOperation;
        int selected;
        lock (Gate)
        {
            roads = _roads.ToList();
            message = _message;
            lastOperation = _lastOperation;
            selected = roads.Count(road => SelectedNames.Contains(road.Name));
        }

        if (message.Length > 0) builder.AppendLine(message);
        if (roads.Count > 0)
        {
            var exported = roads.Count(road =>
                road.Status is RoadExportStatus.Exported or RoadExportStatus.ExportedThisSession);
            var outdated = roads.Count(road => road.Status == RoadExportStatus.Outdated);
            builder.AppendLine(string.Format(
                text.Ready, roads.Count, exported, roads.Count - exported - outdated, outdated));
            builder.AppendLine(string.Format(text.Selected, selected));

            if (roads.Count > PageSize)
            {
                VisibleRoads(out var first, out var last);
                builder.AppendLine(string.Format(
                    text.PageIndicator, PageNumber, PageCount, first, last, roads.Count));
            }
        }

        if (lastOperation.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine(lastOperation);
            builder.AppendLine(text.RestartHint);
            builder.AppendLine(text.ReportHint);
        }

        return builder.ToString().TrimEnd();
    }
}
