using System;
using System.Linq;
using System.Text;
namespace BridgeBuilder.Runtime;
// Historical interpretation is private to the self-check migration call chain.
internal static class BridgeInspectionMetadata
{
    internal static bool DecodeHistorical(string value, out BridgeAssetInfo entry)
    {
        entry = null!;
        if (!value.StartsWith(BridgeAssetMetadata.Prefix, StringComparison.Ordinal)) return false;
        try
        {
            var v = Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(BridgeAssetMetadata.Prefix.Length))).Split('\n')
                .Select(x => Encoding.UTF8.GetString(Convert.FromBase64String(x))).ToArray();
            if (v.Length < 1 || v.Length > 7 || !BridgeAssetInfo.IsPrefabName(v[0])) return false;
            string Get(int index) => index < v.Length ? v[index] : "";
            entry = new BridgeAssetInfo(v[0], string.IsNullOrWhiteSpace(Get(1)) ? v[0] : Get(1),
                Get(2), Get(3), Get(4), Get(5), Get(6) == "1"); return true;
        }
        catch (Exception) { return false; }
    }
    private const string TypeName = "BridgeBuilder.Bridges.BridgeConstructionCost, BridgeBuilder";

    internal static bool TryReadHistorical(string text, out BridgeAssetInfo entry)
    {
        entry = null!;
        if (!BridgeSerializedReferences.TryRead(text, out var document)
            || !BridgeAssetInfo.IsPrefabName(document.Name)) return false;
        var native = document.Component(BridgeAssetMetadata.NativeType);
        if (native != null && native.Fields.TryGetValue("name", out var encoded)
            && DecodeHistorical(encoded.Text, out entry) && entry.PrefabName == document.Name) return true;
        var component = document.Component(TypeName);
        if (component == null) return false;
        string Get(string key) => component.Fields.TryGetValue(key, out var value) && value.Text != "null" ? value.Text : "";
        var label = Get("m_BridgeDisplayName");
        entry = new BridgeAssetInfo(document.Name, string.IsNullOrWhiteSpace(label) ? document.Name : label,
            Get("m_BridgeUpperDeckId"), Get("m_BridgeLowerDeckId"), Get("m_BridgeStyleId"),
            Get("m_BridgeCreatedUtc"), Get("m_BridgeCreationPending") == "true");
        return true;
    }

}
