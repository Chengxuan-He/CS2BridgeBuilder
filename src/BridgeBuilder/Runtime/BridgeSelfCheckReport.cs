using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Settings;

namespace BridgeBuilder.Runtime;

/// <summary>Final owner outcomes for one inspection, separate from session stale-reference guards.</summary>
internal sealed class BridgeSelfCheckReport
{
    internal enum Outcome { Migrated, RemovedBackedUp, RemovedWithoutBackup, Unfinished }
    private readonly Dictionary<string, Outcome> _owners = new(StringComparer.Ordinal);
    internal bool Interrupted { get; set; }
    internal int Total => _owners.Count;
    internal bool HasNotice => Total != 0 || Interrupted;
    internal void Set(string owner, Outcome outcome) => _owners[owner] = outcome;
    internal void Healthy(string owner) => _owners.Remove(owner);
    internal int Count(Outcome outcome) => _owners.Values.Count(value => value == outcome);

    internal string Message()
    {
        var lines = new List<string>();
        Add(Outcome.Migrated, "BridgeSelfCheckMigrated");
        Add(Outcome.RemovedBackedUp, "BridgeSelfCheckRemoved");
        Add(Outcome.RemovedWithoutBackup, "BridgeSelfCheckRemovedWithoutBackup");
        Add(Outcome.Unfinished, "BridgeSelfCheckIncomplete");
        if (Interrupted) lines.Add(RuntimeUiText.Get("BridgeSelfCheckInterrupted"));
        if (lines.Count != 0) lines.Add(RuntimeUiText.Get("BridgeSelfCheckRestart"));
        return string.Join("\n\n", lines);

        void Add(Outcome outcome, string key)
        {
            var count = Count(outcome);
            if (count != 0) lines.Add(RuntimeUiText.Get(key, count));
        }
    }
}
