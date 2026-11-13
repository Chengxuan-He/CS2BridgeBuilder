using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// One pending channel. All methods are called on the main thread; GPU work is async.
internal sealed class MovableAtlasChannel : IDisposable
{
    internal int Channel { get; private set; }
    internal Texture2D Texture { get; private set; } = null!;
    private readonly List<RenderTexture> _mips = new();
    private readonly List<ComputeBuffer> _buffers = new();
    private readonly List<AsyncGPUReadbackRequest> _requests = new();
    private bool _disposed;

    private MovableAtlasChannel() { }
    internal static bool TryCreate(ComputeShader program,RenderTexture source,int channel,bool tiled,out MovableAtlasChannel result,out string? error)
    {
        result=null!;error=null;var candidate=new MovableAtlasChannel{Channel=channel};
        try
        {
            candidate.Texture=new Texture2D(source.width,source.height,TextureFormat.BC7,true,channel!=0);
            candidate.Texture.wrapMode=tiled?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            candidate.Texture.anisoLevel=8;
            if(!candidate.Submit(program,source,out error))return false;
            result=candidate;return true;
        }
        catch(Exception exception){error="Atlas channel creation: "+exception.Message;return false;}
        finally{if(result==null && !candidate.TryDispose(out var cleanup))error=(error??"Atlas creation failed")+"; cleanup: "+cleanup;}
    }

    private bool Submit(ComputeShader program,RenderTexture source,out string? error)
    {
        error=null;
        try
        {
            var previous = source;
            int kernel = program.FindKernel("AtlasMip");
            program.SetInt("MipChannel", Channel);
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
        catch(Exception exception){error="Atlas channel submission: "+exception.Message;return false;}
        return true;
    }

    internal bool TryRead(out string? error)
    {
        error=null;
        if(_disposed){error="Atlas channel is already disposed.";return false;}
        try
        {
            // Validate every request before exposing any copied mip through a success result.
            for(int mip=0;mip<_requests.Count;mip++)
            {
                var request=_requests[mip];request.WaitForCompletion();
                if(request.hasError){error="Moving-road GPU atlas readback failed at mip "+mip;return false;}
            }
            for(int mip=0;mip<_requests.Count;mip++)Texture.SetPixelData(_requests[mip].GetData<byte>(),mip);
            return true;
        }
        catch(Exception exception){error="Atlas channel readback: "+exception.Message;return false;}
    }

    internal bool TryDispose(out string? error)
    {
        error=null;if(_disposed)return true;
        var failures=new List<string>();
        for(int i=_requests.Count-1;i>=0;i--)
        {
            try {var request=_requests[i];if(!request.done)request.WaitForCompletion();_requests.RemoveAt(i);}
            catch(Exception exception){failures.Add("readback wait: "+exception.Message);}
        }
        // A request whose completion could not be established can still read these buffers.
        // Retain ownership for retry instead of releasing GPU inputs under an active request.
        if(_requests.Count==0)
        {
            for(int i=_buffers.Count-1;i>=0;i--)
                try{_buffers[i].Dispose();_buffers.RemoveAt(i);}catch(Exception exception){failures.Add("buffer release: "+exception.Message);}
            for(int i=_mips.Count-1;i>=0;i--)
            {
                bool released=true;
                try{_mips[i].Release();}catch(Exception exception){released=false;failures.Add("mip release: "+exception.Message);}
                if(released)try{UnityEngine.Object.Destroy(_mips[i]);_mips.RemoveAt(i);}catch(Exception exception){failures.Add("mip destruction: "+exception.Message);}
            }
            if(Texture!=null)try{UnityEngine.Object.Destroy(Texture);Texture=null!;}catch(Exception exception){failures.Add("texture destruction: "+exception.Message);}
        }
        _disposed=_requests.Count==0&&_buffers.Count==0&&_mips.Count==0&&Texture==null;
        if(failures.Count!=0)error=string.Join("; ",failures);
        return _disposed&&error==null;
    }

    public void Dispose()=>TryDispose(out _);
}
