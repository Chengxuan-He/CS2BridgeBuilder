using System.Collections.Generic;
using BridgeBuilder.Settings;

namespace BridgeBuilder.Runtime;

internal enum BridgeRuntimeAction
{
    Refresh,
    Create,
    Activate,
    Rename,
    Delete,
}

internal sealed class BridgeRuntimeRequest
{
    internal BridgeRuntimeAction Action { get; set; }
    internal bool BuildAfterCreate { get; set; }
    internal bool LowerDeckOpposite { get; set; } = true;
    internal string PrefabName { get; set; } = string.Empty;
    internal string RegistrationName { get; set; } = string.Empty;
    internal string UpperDeckId { get; set; } = string.Empty;
    internal string LowerDeckId { get; set; } = string.Empty;
    internal string StyleId { get; set; } = string.Empty;
}

/// <summary>Moves UI requests onto the PrefabUpdate system without exposing mutable prefab state to JS.</summary>
internal static class BridgeRuntimeRequests
{
    private static readonly object Gate = new();
    private static readonly Queue<BridgeRuntimeRequest> Requests = new();
    private static RuntimeUiMessage _status = new(string.Empty);
    private static int _revision;
    private static bool _catalogLoading = true;

    internal static bool CatalogLoading
    {
        get { lock (Gate) return _catalogLoading; }
    }

    internal static void BeginCatalogLoad()
    {
        lock (Gate)
        {
            _catalogLoading = true;
            _revision++;
        }
    }

    internal static void EndCatalogLoad()
    {
        lock (Gate)
        {
            _catalogLoading = false;
            foreach (var request in Requests)
                if (request.Action == BridgeRuntimeAction.Refresh) _catalogLoading = true;
            _revision++;
        }
    }

    internal static int Revision
    {
        get { lock (Gate) return _revision; }
    }

    internal static string Status
    {
        get { lock (Gate) return _status.Text; }
    }

    internal static void Enqueue(BridgeRuntimeRequest request, string statusKey)
    {
        lock (Gate)
        {
            // Coalesce adjacent typing events only; other actions are ordering barriers.
            if (request.Action == BridgeRuntimeAction.Refresh) _catalogLoading = true;
            BridgeRuntimeRequest? last = null;
            foreach (var queued in Requests) last = queued;
            if (request.Action == BridgeRuntimeAction.Rename && last?.Action == BridgeRuntimeAction.Rename
                && last.PrefabName == request.PrefabName)
                last.RegistrationName = request.RegistrationName;
            else
                Requests.Enqueue(request);
            // Routine progress/success is not a persistent footer notification.
            _status = new RuntimeUiMessage(string.Empty);
            _revision++;
        }
    }

    internal static bool TryTake(out BridgeRuntimeRequest? request)
    {
        lock (Gate)
        {
            request = Requests.Count > 0 ? Requests.Dequeue() : null;
            return request != null;
        }
    }

    internal static void Complete(string statusKey, params object[] arguments)
    {
        lock (Gate)
        {
            _status = new RuntimeUiMessage(statusKey is "CreatedActive" or "CreatedManage"
                or "CreatedLocked" or "Activated" or "ActivateLocked" or "Renamed" or "Deleted"
                ? string.Empty : statusKey, arguments);
            _revision++;
        }
    }

    internal static void Touch()
    {
        lock (Gate) _revision++;
    }
}
