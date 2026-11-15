using System;

namespace BridgeBuilder.Runtime;

// Current in-memory native unlock tree used during generation.
// Group edges retain native RequireAll/RequireAny flags (including their conjunction).
internal sealed class BridgeUnlockExpression
{
    internal byte Kind; // 0 = native group, 1 = progression leaf identity
    internal string Identity = string.Empty;
    internal byte[] Flags = Array.Empty<byte>();
    internal BridgeUnlockExpression[] Children = Array.Empty<BridgeUnlockExpression>();

    // null means unresolved: no guessed unlock, no deletion. Evaluate every child before returning.
    internal bool? Evaluate(Func<string, bool?> leaf)
    {
        if (Kind == 1) return leaf(Identity);
        if (Kind != 0) return null;
        var all = true;
        var any = false;
        var hasAny = false;
        for (var i = 0; i < Children.Length; i++)
        {
            var value = Children[i].Evaluate(leaf);
            if (!value.HasValue) return null;
            if ((Flags[i] & 1) != 0) all &= value.Value;
            if ((Flags[i] & 2) != 0) { hasAny = true; any |= value.Value; }
        }
        return all && (!hasAny || any);
    }
}
