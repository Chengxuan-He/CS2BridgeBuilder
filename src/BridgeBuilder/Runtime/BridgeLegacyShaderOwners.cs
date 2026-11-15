using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace BridgeBuilder.Runtime;

// Self-check only. Positive evidence selects an owner; unfamiliar templates do not.
internal static class BridgeLegacyShaderOwners
{
    // Identical package digests in published 26.11.12 (51d1455), 12a (db18800), 14 (9352ef5).
    internal const string PublishedWorld = "349D959D0823C298A291C3CDAD28143956698D7A99812DE3EB68B7E50F3684CF";
    internal const string PublishedDecal = "5793989F8FBB85176C16F2887DD2C7EC0D9524A34AF15B46D9C69E9DE30C2384";
    // Recorded development UnityFS graph packages before per-owner persistence was removed.
    internal const string DevelopmentWorld = "B9B779A87DF09FD4DF8041D002CC6B70F918BE3D9D8330273E5902084FC21755";
    internal const string DevelopmentDecal = "C8948092F1825D6247C4C0873585B86E397669F40B9F22F20267D5E8D5FC7689";

    internal static bool Identify(string owner, IEnumerable<string> paths, out string? evidence)
    {
        evidence = null;
        foreach (var path in paths)
        {
            if (!BridgeAssetInfo.MatchesOwner(path, owner)) continue;
            if (string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "MaterialPrograms", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(path);
                // A missing published payload can leave its exact original program/CID filename.
                // Generic orphan sidecars and unknown program hashes are not evidence.
                foreach (var program in new[] { "Program 17 " + PublishedWorld + ".txt.cid", "Program 18 " + PublishedDecal + ".txt.cid" })
                    if (string.Equals(fileName, program, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fileName, owner + " " + program, StringComparison.OrdinalIgnoreCase))
                    { evidence = path; return true; }
                var extension = Path.GetExtension(path);
                if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) || extension.Equals(".bundle", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = BridgeFileAccess.ReadBytes(path);
                    if (!ProgramEvidence(bytes, extension)) continue;
                    if (extension.Equals(".bundle", StringComparison.OrdinalIgnoreCase))
                    {
                        using var sha = SHA256.Create();
                        var expected = "Program " + BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "") + ".bundle";
                        if (!string.Equals(fileName, expected, StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(fileName, owner + " " + expected, StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    evidence = path; return true;
                }
            }
            // Do not classify borrowed third-party snapshots by arbitrary template integers.
            if (!Path.GetExtension(path).Equals(".Surface", StringComparison.OrdinalIgnoreCase)
                || BridgeInspectionPaths.IsDependencyPath(path, owner)) continue;
            if (SurfaceEvidence(BridgeFileAccess.ReadBytes(path))) { evidence = path; return true; }
        }
        return false;
    }

    internal static bool ProgramEvidence(byte[] bytes, string extension)
    {
        var prefix = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 64)).TrimStart('\uFEFF');
        if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return prefix.StartsWith("BridgeBuilder.MaterialProgram/1\n", StringComparison.Ordinal)
                || prefix.StartsWith("BridgeBuilder.MaterialProgram/1\r\n", StringComparison.Ordinal)
                || prefix.StartsWith("BridgeBuilder.MaterialProgram/2\n", StringComparison.Ordinal)
                || prefix.StartsWith("BridgeBuilder.MaterialProgram/2\r\n", StringComparison.Ordinal);
        return extension.Equals(".bundle", StringComparison.OrdinalIgnoreCase) && prefix.StartsWith("UnityFS\0", StringComparison.Ordinal);
    }

    internal static int HistoricalTemplate(string digest, int source, int? queue, bool published)
    {
        if (published)
        {
            var hyphenated = new StringBuilder();
            for (int i = 0; i < digest.Length; i += 2) { if (i != 0) hyphenated.Append('-'); hyphenated.Append(digest, i, 2); }
            digest = hyphenated.ToString();
        }
        string identity = digest + ":" + source.ToString(CultureInfo.InvariantCulture)
            + (queue.HasValue ? ":queue=" + queue.Value.ToString(CultureInfo.InvariantCulture) : "");
        uint value = 2166136261;
        foreach (char c in identity) value = unchecked((value ^ c) * 16777619);
        return (int)(value & 0x3fffffff) | 0x40000000;
    }

    internal static bool SurfaceEvidence(byte[] bytes)
    {
        if (!BridgeCurrentProgramSurface.TryReadSurfaceIdentity(bytes, out var template,
            out var currentQueue, out var drawOrder, out _)) return false;
        if (currentQueue.HasValue && BridgeNativeMaterialProgram.TryIdentify(template, currentQueue.Value, out _)) return false;
        if (template == HistoricalTemplate(PublishedWorld, 17, null, true)
            || template == HistoricalTemplate(PublishedDecal, 18, null, true)
            || template == HistoricalTemplate(DevelopmentWorld, 17, 2225, false)) return true;
        // Outside float's consecutive integer domain the old writer loses priority information.
        // Do not infer a queue from a rounded value or an unknown/absent property.
        if (!drawOrder.HasValue || float.IsNaN(drawOrder.Value) || Math.Abs((double)drawOrder.Value) >= 16777216
            || drawOrder.Value != Math.Truncate(drawOrder.Value)) return false;
        int queue = checked(2000 + (int)drawOrder.Value);
        return template == HistoricalTemplate(PublishedDecal, 18, queue, true)
            || template == HistoricalTemplate(DevelopmentDecal, 18, queue, false);
    }
}
