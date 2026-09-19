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
            BridgeRuntimeRequest? last = null;
            foreach (var queued in Requests) last = queued;
            if (request.Action == BridgeRuntimeAction.Rename && last?.Action == BridgeRuntimeAction.Rename
                && last.PrefabName == request.PrefabName)
                last.RegistrationName = request.RegistrationName;
            else
                Requests.Enqueue(request);
            _status = new RuntimeUiMessage(statusKey);
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
            _status = new RuntimeUiMessage(statusKey, arguments);
            _revision++;
        }
    }

    internal static void Touch()
    {
        lock (Gate) _revision++;
    }
}
