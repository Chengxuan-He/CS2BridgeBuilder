using BridgeBuilder.Bridges;
using UnityEngine;

var source = new RenderTexture(16, 8, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
var shader = new ComputeShader();
using (var a = new MovableAtlasChannel(shader, source, 0, false))
using (var b = new MovableAtlasChannel(shader, source, 1, true))
{
    if (ComputeBuffer.Live != 10) throw new Exception("Expected two complete mip chains.");
    a.Read(); b.Read();
    if (a.Texture.Writes != 5 || b.Texture.Writes != 5) throw new Exception("Missing mip copy.");
    if (a.Texture.Linear || !b.Texture.Linear || b.Texture.wrapMode != TextureWrapMode.Repeat)
        throw new Exception("Channel settings changed.");
}
if (ComputeBuffer.Live != 0 || source.Released) throw new Exception("Resource ownership changed.");
UnityEngine.Rendering.AsyncGPUReadback.FailRequestAt = UnityEngine.Rendering.AsyncGPUReadback.Count + 2;
try { using var bad = new MovableAtlasChannel(shader, source, 2, false); throw new Exception("Expected submission failure."); }
catch (InjectedFailure) { }
if (ComputeBuffer.Live != 0 || source.Released) throw new Exception("Partial submission leaked.");
UnityEngine.Rendering.AsyncGPUReadback.FailRequestAt = -1;
using (var abandoned = new MovableAtlasChannel(shader, source, 3, false)) { }
if (ComputeBuffer.Live != 0 || source.Released) throw new Exception("Abandoned readback leaked.");
Console.WriteLine("PASS two channels, mip copy order, color/wrap settings, partial submission and abandoned readback cleanup. GPU output requires game validation.");
public class InjectedFailure : Exception { }

namespace UnityEngine
{
 public class Object { public static void Destroy(object o) { } }
 public enum TextureFormat { BC7 }
 public enum TextureWrapMode { Repeat, Clamp }
 public enum RenderTextureFormat { ARGB32 }
 public enum RenderTextureReadWrite { Linear }
 public class Texture2D
 {
  public int mipmapCount = 5, anisoLevel, Writes; public bool Linear; public TextureWrapMode wrapMode;
  public Texture2D(int w,int h,TextureFormat f,bool mip,bool linear) { Linear=linear; }
  public void SetPixelData<T>(T[] data,int mip) { if(mip!=Writes++) throw new Exception("Mip order changed."); }
 }
 public class RenderTexture
 {
  public int width,height; public bool enableRandomWrite,Released;
  public RenderTexture(int w,int h,int d,RenderTextureFormat f,RenderTextureReadWrite r) {width=w;height=h;}
  public void Create() { } public void Release() {Released=true;}
 }
 public class ComputeBuffer : IDisposable
 {
  public static int Live; public UnityEngine.Rendering.RequestState State;
  public ComputeBuffer(int n,int stride) {Live++;}
  public void Dispose() { if(State!=null && !State.Done) throw new Exception("Buffer released before readback."); Live--; }
 }
 public class ComputeShader
 {
  public int FindKernel(string s)=>0;
  public void SetInt(string s,int v) { }
  public void SetInts(string s,params int[] v) { }
  public void SetTexture(int k,string s,RenderTexture t) { }
  public void SetBuffer(int k,string s,ComputeBuffer b) { }
  public void Dispatch(int k,int x,int y,int z) { if(x<1 || y<1) throw new Exception("Empty dispatch."); }
 }
}
namespace UnityEngine.Rendering
{
 public class RequestState { public bool Done; }
 public struct AsyncGPUReadbackRequest
 {
  public RequestState State; public bool done=>State.Done; public bool hasError=>false;
  public void WaitForCompletion() {State.Done=true;}
  public T[] GetData<T>() { if(!done)throw new Exception("Early read.");return Array.Empty<T>(); }
 }
 public static class AsyncGPUReadback
 {
  public static int Count,FailRequestAt=-1;
  public static AsyncGPUReadbackRequest Request(UnityEngine.ComputeBuffer b)
  { if(++Count==FailRequestAt)throw new InjectedFailure();b.State=new();return new(){State=b.State}; }
 }
}
