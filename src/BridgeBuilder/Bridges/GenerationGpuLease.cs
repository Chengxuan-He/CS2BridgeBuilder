using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// A cancelled preview relinquishes publication immediately. Native resources stay leased
// until callbacks finish; cancellation/world disposal never WaitForCompletion/WaitAllRequests.
internal sealed class GenerationGpuLease : IDisposable
{
    internal readonly List<ComputeBuffer> Buffers = new();
    internal Action? ReleaseShaders;
    private int _pending;
    private bool _dispose, _released;
    internal async Task Read<T>(ComputeBuffer buffer,T[] destination) where T:struct
    {
        if(GenerationWork.Current==null){buffer.GetData(destination);return;}
        bool complete=false;Exception? failure=null;
        _pending++;
        try
        {
            AsyncGPUReadback.Request(buffer,request=>
            {
                try
                {
                    if(!_dispose)
                    {
                        if(request.hasError)failure=new InvalidOperationException("Generation GPU readback failed.");
                        else request.GetData<T>().CopyTo(destination);
                    }
                }
                catch(Exception error){failure=error;}
                finally{_pending--;complete=true;ReleaseIfReady();}
            });
        }
        catch{_pending--;ReleaseIfReady();throw;}
        while(!complete)await GenerationWork.NextFrame("GPU readback completion");
        if(failure!=null)throw failure;
    }
    public void Dispose(){_dispose=true;ReleaseIfReady();}
    private void ReleaseIfReady()
    {
        if(!_dispose||_pending!=0||_released)return;
        _released=true;
        foreach(var buffer in Buffers)try{buffer.Dispose();}catch(Exception error){Mod.Log.Warn("Generation GPU resource release: "+error.Message);}
        Buffers.Clear();
        try{ReleaseShaders?.Invoke();}catch(Exception error){Mod.Log.Warn("Generation shader release: "+error.Message);}
        ReleaseShaders=null;
    }
}
