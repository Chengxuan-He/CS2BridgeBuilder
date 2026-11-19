using System;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Programs belong to the mod. Generation publishes only native Surface references.
internal static class MovableCustomShaderAsset
{
    private static byte[] Package(int sourceTemplate) => GenerationParameters.Get("ModMaterialProgram:" + sourceTemplate, () =>
    {
        string name = BridgeNativeMaterialProgram.ProgramName(sourceTemplate);
        using var input = typeof(MovableCustomShaderAsset).Assembly.GetManifestResourceStream("BridgeBuilder.MaterialProgram." + name + ".bundle");
        using var bytes = new MemoryStream(); input!.CopyTo(bytes); return bytes.ToArray();
    });

    internal static bool TryRegisterCurrent(out string? error)
    {
        error = null;
        foreach (int source in new[] { BridgeNativeMaterialProgram.NetworkPalette, 17, 18 })
            if (!TryPrepare(source, BridgeNativeMaterialProgram.DefaultQueue(source), out _, out var cause))
                error = (error == null ? "" : error + "\n") + cause;
        return error == null;
    }

    internal static bool TryPrepare(int sourceTemplate, int queue, out int template, out string? error)
    {
        template = -1; error = null;
        try
        {
            return BridgeCustomShaderRegistry.TryRegisterPackage(AssetDatabase.global.resources.materialLibrary,
                BridgeNativeMaterialProgram.Configure(Package(sourceTemplate), sourceTemplate, queue),
                BridgeNativeMaterialProgram.LogicalIdentity(sourceTemplate, queue), out template, out error);
        }
        catch (Exception exception)
        { error = "Mod material program registration failed: " + exception.Message; return false; }
    }
}
