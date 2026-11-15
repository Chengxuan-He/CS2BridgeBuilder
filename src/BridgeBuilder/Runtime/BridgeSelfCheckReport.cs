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
    internal int Total => _owners.Count;
    private bool _changed;
    internal void Changed() => _changed = true;
    internal bool HasNotice => _changed || _owners.Values.Any(value => value != Outcome.Unfinished);
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
        if (lines.Count != 0)
        {
            var restart = RuntimeUiText.Get("BridgeSelfCheckRestart");
            if (Count(Outcome.Unfinished) > 0)
                restart += " " + RuntimeUiText.Get("BridgeSelfCheckUnfinishedWarning");
            lines.Add(restart);
        }
        return string.Join("\n\n", lines);

        void Add(Outcome outcome, string key)
        {
            var count = Count(outcome);
            if (count != 0) lines.Add(RuntimeUiText.Get(key, count));
        }
    }
}
