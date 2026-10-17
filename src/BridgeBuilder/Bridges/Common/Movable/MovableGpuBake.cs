using System;
using System.IO;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Retains the vendor-neutral compute loader for preview skin bounds only.
internal static class MovableGpuBake
{
    private static ComputeShader? _template;
    internal static ComputeShader CreateProgram()
    {
        if (_template == null)
        {
            using var stream = typeof(MovableGpuBake).Assembly.GetManifestResourceStream("BridgeBuilder.GpuBake")
                ?? throw new InvalidOperationException("The GPU geometry bundle is missing from Bridge Builder.");
            using var bytes = new MemoryStream(); stream.CopyTo(bytes);
            var bundle = AssetBundle.LoadFromMemory(bytes.ToArray());
            if (bundle == null) throw new InvalidOperationException("The GPU geometry bundle could not be loaded.");
            try { _template = bundle.LoadAsset<ComputeShader>("Assets/BridgeBake.compute"); }
            finally { bundle.Unload(false); }
            if (_template == null) throw new InvalidOperationException("The GPU geometry compute program is missing.");
            Mod.Log.Info($"Moving road GPU geometry: {SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}.");
        }
        return UnityEngine.Object.Instantiate(_template);
    }

}
