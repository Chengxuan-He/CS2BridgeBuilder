using System;
using System.IO;
using System.Text;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Unity.Entities;
namespace BridgeBuilder.Runtime;
// Resolves published historical ManualUnlockable records only during self-check.
internal static class BridgeInspectionUnlock
{
    internal const string Prefix = "BridgeBuilder.Unlock.v1:";
    internal static bool Read(NetGeometryPrefab bridge, PrefabSystem prefabs, EntityManager manager,
        out BridgeUnlockExpression rule)
    {
        rule = new();
        var gate = bridge.GetComponent<ManualUnlockable>();
        if (gate == null || !TryDecode(gate.name, out rule)) return false;
        if (rule.Kind != 2) return true;
        if (bridge.isBuiltin || bridge.isReadOnly) return false;
        // Resolve only registered, initialized instances. Never call PrefabAsset.Load or GetEntity(null).
        PrefabBase? prototype = null;
        if (rule.Identity.StartsWith("CID:", StringComparison.Ordinal))
        {
            if (AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(rule.Identity.Substring(4)),
                out PrefabAsset asset)) prototype = asset.GetInstance<PrefabBase>();
        }
        else if (rule.Identity.StartsWith("UnityGUID:", StringComparison.Ordinal)
            && AssetDatabase.global.resources.prefabsMap.TryGetObject(rule.Identity.Substring(10), out var value))
            prototype = value as PrefabBase;
        else if (rule.Identity.StartsWith("ID:", StringComparison.Ordinal))
        {
            var parts = rule.Identity.Substring(3).Split('/');
            if (parts.Length < 2 || parts.Length > 3) return false;
            prefabs.TryGetPrefab(new PrefabID(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]),
                parts.Length == 3 ? Colossal.Hash128.Parse(parts[2]) : default), out prototype);
        }
        if (prototype == null || !BridgeUnlockSnapshot.Capture(prototype, prefabs, manager, out var snapshot)) return false;
        rule = snapshot;
        return true;
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

}
