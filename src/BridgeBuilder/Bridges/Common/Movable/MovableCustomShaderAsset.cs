using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

internal static class MovableCustomShaderAsset
{
    private static byte[] WorldPackage() => Package("WorldLayeredPbr");
    private static byte[] DecalPackage() => Package("BoneDecal");
    private static byte[] Package(string program) => GenerationParameters.Get("GeneratedMaterialProgram:" + program, () =>
    {
        using var input = typeof(MovableCustomShaderAsset).Assembly.GetManifestResourceStream("BridgeBuilder.MaterialProgram." + program + ".bbs1")
            ?? throw new InvalidDataException("Generated layered material package is missing.");
        using var bytes = new MemoryStream(); input.CopyTo(bytes); return bytes.ToArray();
    });

    internal static bool TryRegisterCurrent(out string? error)
    {
        error = null;
        foreach (var program in new[] { (Name: "WorldLayeredPbr", Template: 17), (Name: "BoneDecal", Template: 18) })
        {
            string? cause;
            try
            {
                if (BridgeCustomShaderRegistry.TryRegisterPackage(AssetDatabase.global.resources.materialLibrary,
                    program.Template, Package(program.Name), out _, out cause)) continue;
            }
            catch (Exception exception) { cause = exception.Message; }
            error = (error == null ? "" : error + "\n") + "Generated material initialization: " + program.Name + "; " + cause;
        }
        return error == null;
    }

    internal static bool TryPrepareBoneDecal(string owner, PreviewGeometry? preview, List<AssetData> assets, int priority,
        out int template, out string? error)
    {
        try { return TryPrepare(DecalPackage(), 18, owner, preview, assets, out template, out error, checked(2000 + priority)); }
        catch (Exception exception) { template = -1; error = "Generated bone decal source failed: " + exception.Message; return false; }
    }

    internal static bool TryPrepareWorldLayered(string owner, PreviewGeometry? preview, List<AssetData> assets,
        out int template, out string? error)
    {
        try { return TryPrepare(WorldPackage(), 17, owner, preview, assets, out template, out error); }
        catch (Exception exception) { template = -1; error = "Generated layered material source failed: " + exception.Message; return false; }
    }

    // Full producer operation: compile/register, then add one real program asset to
    // this request's owner transaction and dependency roots. No Surface CID inference.
    internal static bool TryPrepare(byte[] package, int sourceTemplate, string owner,
        PreviewGeometry? preview, List<AssetData> assets, out int template, out string? error, int? renderQueue = null)
    {
        template = -1; error = null; FileAsset? pending = null;
        byte[]? originalEnvelope = null;
        try
        {
            var dependencies = BridgeRoadGeneration.CurrentDependencies;
            if (dependencies == null)
            { error = "Generated material program has no creation dependency scope."; return false; }
            string digest;
            using (var hash = SHA256.Create()) digest = BitConverter.ToString(hash.ComputeHash(package)).Replace("-", "");
            // The dependency scope already belongs to exactly one owner/request.
            // Surface names contain per-group suffixes and must not duplicate programs.
            string programIdentity = "program:" + sourceTemplate + ":" + digest;
            string identity = programIdentity + (renderQueue.HasValue ? ":queue=" + renderQueue.Value : ":native");
            if (dependencies.TryProgram(identity, out var previous, out var reused))
            {
                if (!ReferenceEquals(previous.database, preview?.NativeAssets ?? AssetDatabase.user))
                { error = "Generated material program belongs to another asset transaction."; return false; }
                template = reused; return true;
            }
            if (!BridgeCustomShaderRegistry.TryRegisterConfiguredPackage(AssetDatabase.global.resources.materialLibrary,
                sourceTemplate, package, renderQueue, out var registered, out error)) return false;
            var database = preview?.NativeAssets ?? AssetDatabase.user;
            int[] queues = Array.Empty<int>();
            if (dependencies.TryProgram(programIdentity, out var existing, out _))
            {
                if (!ReferenceEquals(existing.database, database))
                { error = "Generated program payload belongs to another asset transaction."; return false; }
                using (var input = existing.GetReadStream())
                using (var copy = new MemoryStream())
                { input.CopyTo(copy); originalEnvelope = copy.ToArray(); }
                using (var input = new MemoryStream(originalEnvelope, writable: false))
                    if (!BridgeCustomShaderPackage.TryReadConfigured(input, out var original, out _, out queues, out error) || original != sourceTemplate)
                    { error ??= "Generated program source template changed within the request."; return false; }
                pending = existing;
            }
            else
            {
                var name = BridgePrefabPersistence.ResourcePath(owner + " Program " + sourceTemplate + " " + digest, "MaterialPrograms");
                pending = database.AddAsset<FileAsset>(AssetDataPath.Create(name.subPath, name.assetName + ".txt", true, EscapeStrategy.None));
                assets.Add(pending);
            }
            if (renderQueue.HasValue) queues = queues.Concat(new[] { renderQueue.Value }).Distinct().ToArray();
            using (var encoded = new MemoryStream())
            {
                if (!BridgeCustomShaderPackage.TryWriteConfigured(encoded, sourceTemplate, package, queues, out error)) return false;
                encoded.Position = 0;
                using var output = pending.GetWriteStream();
                encoded.CopyTo(output);
            }
            dependencies.Program(programIdentity, pending, -1);
            dependencies.Program(identity, pending, registered);
            pending = null; template = registered; return true;
        }
        catch (Exception exception)
        { error = "Generated material program asset failed: " + exception.GetType().Name + ": " + exception.Message; return false; }
        finally
        {
            if (pending != null)
                try
                {
                    if (originalEnvelope != null)
                    {
                        using var output = pending.GetWriteStream();
                        output.Write(originalEnvelope, 0, originalEnvelope.Length);
                    }
                    else { pending.database.DeleteAsset(pending); assets.Remove(pending); }
                }
                catch (Exception exception) { error = (error ?? "Generated material program failed.") + " Asset rollback: " + exception.Message; }
        }
    }
}
