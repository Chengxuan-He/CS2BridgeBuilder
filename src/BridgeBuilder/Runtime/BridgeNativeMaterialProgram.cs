using System;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;

namespace BridgeBuilder.Runtime;

// The offline producer records native scalar/string field locations for these exact embedded
// graphs. No shader bytes, lengths, object references or property arrays change at runtime.
internal static class BridgeNativeMaterialProgram
{
    internal static string Identity(byte[] package)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(package)).Replace("-", "");
    }

    internal const string QueueProperty = "BridgeBuilder_RenderQueue";
    // Internal family selector, not the native Default material template hash.
    internal const int NetworkPalette = 0;

    internal static string ProgramName(int family) => family == NetworkPalette ? "NetworkPalette" :
        family == 17 ? "WorldLayeredPbr" : "BoneDecal";

    internal static int DefaultQueue(int family) => family == NetworkPalette ?
        BridgeNativePaletteLayout.DefaultQueue : BridgeNativeProgramLayout.DefaultQueue(family);
    internal static int HashOffset(int family) => family == NetworkPalette ?
        BridgeNativePaletteLayout.HashOffset : BridgeNativeProgramLayout.HashOffset(family);
    internal static int QueueOffset(int family) => family == NetworkPalette ?
        BridgeNativePaletteLayout.QueueOffset : BridgeNativeProgramLayout.QueueOffset(family);
    internal static int[] IdentityOffsets(int family) => family == NetworkPalette ?
        BridgeNativePaletteLayout.IdentityOffsets : BridgeNativeProgramLayout.IdentityOffsets(family);

    internal static string LogicalIdentity(int sourceTemplate, int queue) =>
        "BridgeBuilder/" + ProgramName(sourceTemplate)
        + ":queue=" + queue.ToString(CultureInfo.InvariantCulture);

    internal static int TemplateId(int sourceTemplate, int? renderQueue = null)
    {
        string identity = LogicalIdentity(sourceTemplate, renderQueue ?? DefaultQueue(sourceTemplate));
        uint hash = 2166136261;
        foreach (char c in identity) hash = unchecked((hash ^ c) * 16777619);
        return (int)(hash & 0x1fffffff) | 0x20000000;
    }

    internal static bool TryIdentify(int template, int queue, out int sourceTemplate)
    {
        foreach (int candidate in new[] { NetworkPalette, 17, 18 })
            if (TemplateId(candidate, queue) == template) { sourceTemplate = candidate; return true; }
        sourceTemplate = 0; return false;
    }

    internal static byte[] Configure(byte[] source, int sourceTemplate, int? renderQueue)
    {
        int queue = renderQueue ?? DefaultQueue(sourceTemplate);
        string identity = LogicalIdentity(sourceTemplate, queue);
        int template = TemplateId(sourceTemplate, queue);
        var bytes = (byte[])source.Clone();
        Put(bytes, HashOffset(sourceTemplate), template);
        Put(bytes, QueueOffset(sourceTemplate), queue);
        string bundleIdentity = Identity(Encoding.UTF8.GetBytes(identity)).Substring(0, 32).ToLowerInvariant();
        var name = Encoding.ASCII.GetBytes(bundleIdentity);
        foreach (int offset in IdentityOffsets(sourceTemplate))
            Buffer.BlockCopy(name, 0, bytes, offset, name.Length);
        return bytes;
    }

    private static void Put(byte[] bytes, int offset, int value)
    {
        for (int i = 0; i < 4; i++) bytes[offset + i] = (byte)((uint)value >> (8 * i));
    }
}
