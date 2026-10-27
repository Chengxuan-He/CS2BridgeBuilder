using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// One pending channel. All methods are called on the main thread; GPU work is async.
internal sealed class MovableAtlasChannel : IDisposable
{
    internal int Channel { get; }
    internal Texture2D Texture { get; }
    private readonly List<RenderTexture> _mips = new();
    private readonly List<ComputeBuffer> _buffers = new();
    private readonly List<AsyncGPUReadbackRequest> _requests = new();
    private bool _disposed;

    internal MovableAtlasChannel(ComputeShader program, RenderTexture source, int channel, bool tiled)
    {
        Channel = channel;
        Texture = new Texture2D(source.width, source.height, TextureFormat.BC7, true, channel != 0)
        { wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, anisoLevel = 8 };
        try
        {
            var previous = source;
            int kernel = program.FindKernel("AtlasMip");
            program.SetInt("MipChannel", channel);
            for (int mip = 1; mip < Texture.mipmapCount; mip++)
            {
                int width = Math.Max(1, source.width >> mip), height = Math.Max(1, source.height >> mip);
                var target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                { enableRandomWrite = true };
                _mips.Add(target);
                target.Create();
                program.SetTexture(kernel, "MipSource", previous);
                program.SetTexture(kernel, "MipTarget", target);
                program.SetInts("MipExtent", width, height);
                program.Dispatch(kernel, (width + 7) / 8, (height + 7) / 8, 1);
                previous = target;
            }
            kernel = program.FindKernel("CompressAtlas");
            for (int mip = 0; mip < Texture.mipmapCount; mip++)
            {
                int x = (Math.Max(1, source.width >> mip) + 3) / 4;
                int y = (Math.Max(1, source.height >> mip) + 3) / 4;
                var buffer = new ComputeBuffer(x * y, 16);
                _buffers.Add(buffer);
                program.SetTexture(kernel, "MipSource", mip == 0 ? source : _mips[mip - 1]);
                program.SetBuffer(kernel, "CompressedAtlas", buffer);
                program.Dispatch(kernel, (x + 7) / 8, (y + 7) / 8, 1);
                _requests.Add(AsyncGPUReadback.Request(buffer));
            }
        }
        catch { Dispose(); throw; }
    }

    internal void Read()
    {
        for (int mip = 0; mip < _requests.Count; mip++)
        {
            var request = _requests[mip];
            request.WaitForCompletion();
            if (request.hasError) throw new InvalidOperationException("Moving-road GPU atlas readback failed.");
            Texture.SetPixelData(request.GetData<byte>(), mip);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Also covers queued channels abandoned after another channel's save fails.
        try
        {
            foreach (var request in _requests)
                if (!request.done) request.WaitForCompletion();
        }
        finally
        {
            foreach (var buffer in _buffers) buffer.Dispose();
            foreach (var mip in _mips) { mip.Release(); UnityEngine.Object.Destroy(mip); }
            UnityEngine.Object.Destroy(Texture);
        }
    }
}
