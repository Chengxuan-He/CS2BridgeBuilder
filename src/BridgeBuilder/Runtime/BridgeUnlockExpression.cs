using System;
using System.IO;
using System.Text;

namespace BridgeBuilder.Runtime;

// Value-only data, stored in the native ManualUnlockable component's name. The game can deserialize
// that native gate BEFORE IMod.OnLoad, without resolving any external prefab or custom component.
// Group edges retain native RequireAll/RequireAny flags (including their conjunction).
internal sealed class BridgeUnlockExpression
{
    internal const string Prefix = "BridgeBuilder.Unlock.v1:";
    internal byte Kind; // 0 = native group, 1 = progression leaf identity, 2 = legacy deferred prototype
    internal string Identity = string.Empty;
    internal byte[] Flags = Array.Empty<byte>();
    internal BridgeUnlockExpression[] Children = Array.Empty<BridgeUnlockExpression>();

    internal string Encode()
    {
        using var bytes = new MemoryStream();
        using (var writer = new BinaryWriter(bytes, Encoding.UTF8, true)) Write(writer);
        return Prefix + Convert.ToBase64String(bytes.ToArray());
    }
    private void Write(BinaryWriter writer)
    {
        writer.Write(Kind);
        writer.Write(Identity);
        writer.Write(Children.Length);
        for (var i = 0; i < Children.Length; i++) { writer.Write(Flags[i]); Children[i].Write(writer); }
    }
    internal static bool TryDecode(string text, out BridgeUnlockExpression result)
    {
        result = new();
        if (text == null || !text.StartsWith(Prefix, StringComparison.Ordinal) || text.Length > 1048576) return false;
        try
        {
            using var bytes = new MemoryStream(Convert.FromBase64String(text.Substring(Prefix.Length)));
            using var reader = new BinaryReader(bytes, Encoding.UTF8);
            var remaining = 4096;
            return Read(reader, 0, ref remaining, out result) && bytes.Position == bytes.Length;
        }
        catch (Exception) { return false; }
    }
    private static bool Read(BinaryReader reader, int depth, ref int remaining, out BridgeUnlockExpression result)
    {
        result = new();
        if (depth > 64 || --remaining < 0) return false;
        result.Kind = reader.ReadByte();
        result.Identity = reader.ReadString();
        var count = reader.ReadInt32();
        if (result.Kind > 2 || count < 0 || count > remaining || (result.Kind != 0 && count != 0)) return false;
        result.Flags = new byte[count];
        result.Children = new BridgeUnlockExpression[count];
        for (var i = 0; i < count; i++)
        {
            result.Flags[i] = reader.ReadByte();
            if (result.Flags[i] < 1 || result.Flags[i] > 3
                || !Read(reader, depth + 1, ref remaining, out result.Children[i])) return false;
        }
        return true;
    }

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
