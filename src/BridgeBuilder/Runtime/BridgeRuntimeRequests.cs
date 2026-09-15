using System.Collections.Generic;

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
    private static string _status = string.Empty;
    private static int _revision;

    internal static int Revision
    {
        get { lock (Gate) return _revision; }
    }

    internal static string Status
    {
        get { lock (Gate) return _status; }
    }

    internal static void Enqueue(BridgeRuntimeRequest request, string status)
    {
        lock (Gate)
        {
            Requests.Enqueue(request);
            _status = status;
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

    internal static void Complete(string status)
    {
        lock (Gate)
        {
            _status = status;
            _revision++;
        }
    }

    internal static void Touch()
    {
        lock (Gate) _revision++;
    }
}
