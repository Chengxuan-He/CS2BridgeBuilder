using System;
using System.Collections.Generic;

namespace BridgeBuilder.Runtime;

/// <summary>Require the same current failure on separate engine frames, never a historical log alone.</summary>
internal sealed class BridgeCleanupConfirmation
{
    private readonly Dictionary<string, string> _previous = new(StringComparer.Ordinal);
    private int _frame = -1;

    internal HashSet<string> Observe(IReadOnlyDictionary<string, string> failures, int frame)
    {
        var confirmed = new HashSet<string>(StringComparer.Ordinal);
        if (frame == _frame) return confirmed;
        foreach (var failure in failures)
            if (_previous.TryGetValue(failure.Key, out var reason) && reason == failure.Value)
                confirmed.Add(failure.Key);
        _previous.Clear();
        foreach (var failure in failures) _previous.Add(failure.Key, failure.Value);
        _frame = frame;
        return confirmed;
    }

    internal void Clear() { _previous.Clear(); _frame = -1; }
}
