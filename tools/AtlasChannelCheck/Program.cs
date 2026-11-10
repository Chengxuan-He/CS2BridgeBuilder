using BridgeBuilder.Bridges;
using UnityEngine;
using UnityEngine.Rendering;
var source=new RenderTexture(16,8,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
var shader=new ComputeShader();
void Check(bool value,string text){if(!value)throw new Exception(text);}
MovableAtlasChannel Create(int kind=0,bool tiled=false)
{Check(MovableAtlasChannel.TryCreate(shader,source,kind,tiled,out var channel,out var error),error??"Creation failed");return channel;}
using(var a=Create())using(var b=Create(1,true))
{
 Check(ComputeBuffer.Live==10,"Expected two mip chains");
 Check(a.TryRead(out _)&&b.TryRead(out _),"Successful read rejected");
 Check(a.Texture.Writes==5&&b.Texture.Writes==5,"Missing mip copy");
 Check(!a.Texture.Linear&&b.Texture.Linear&&b.Texture.wrapMode==TextureWrapMode.Repeat,"Settings changed");
}
Check(ComputeBuffer.Live==0&&!source.Released,"Ownership changed");
AsyncGPUReadback.FailRequestAt=AsyncGPUReadback.Count+2;
Check(!MovableAtlasChannel.TryCreate(shader,source,2,false,out var failed,out var cause)&&failed==null&&cause!=null,"Submission must return failure");
Check(ComputeBuffer.Live==0,"Partial submission leaked");AsyncGPUReadback.FailRequestAt=-1;
using(var read=Create())
{
 AsyncGPUReadback.Error=true;Check(!read.TryRead(out cause)&&cause!=null&&read.Texture.Writes==0,"Failed mip exposed copied texture");AsyncGPUReadback.Error=false;
}
using(var read=Create())
{
 Texture2D.FailWrite=2;Check(!read.TryRead(out cause)&&cause!=null,"SetPixelData fault escaped or became success");Texture2D.FailWrite=-1;
}
var retry=Create();AsyncGPUReadback.FailWaitOnce=true;
Check(!retry.TryDispose(out cause)&&cause!=null&&ComputeBuffer.Live==5,"Unconfirmed GPU request input released");
Check(retry.TryDispose(out cause)&&cause==null&&ComputeBuffer.Live==0,"Wait retry lost ownership");
retry=Create();ComputeBuffer.FailDisposeOnce=true;RenderTexture.FailReleaseOnce=true;UnityEngine.Object.FailDestroyOnce=true;
Check(!retry.TryDispose(out cause)&&cause.Contains("buffer release")&&cause.Contains("mip release")&&cause.Contains("destruction"),"Independent cleanup failures were masked");
Check(ComputeBuffer.Live==1,"One release failure prevented all other buffer cleanup");
Check(retry.TryDispose(out cause)&&cause==null&&ComputeBuffer.Live==0,"Cleanup retry failed");
Check(retry.TryDispose(out _)&&!retry.TryRead(out _),"Disposed state is inconsistent");
Check(!MovableAtlasChannel.TryCreate(null!,source,0,false,out failed,out cause)&&failed==null&&cause!=null,"Invalid source accepted");
Check(!source.Released,"Borrowed source destroyed");
Console.WriteLine("PASS result channel success, failed submission/readback/mip copy, all-mip prevalidation, independent cleanup faults, safe pending-request retention/retry, idempotence and borrowed source ownership. CPU fault-injection seam; native GPU output separately tested.");
public class InjectedFailure : Exception { }

namespace UnityEngine
{
 public class Object { public static bool FailDestroyOnce; public static void Destroy(object o) {if(FailDestroyOnce){FailDestroyOnce=false;throw new InjectedFailure();}} }
 public enum TextureFormat { BC7 }
 public enum TextureWrapMode { Repeat, Clamp }
 public enum RenderTextureFormat { ARGB32 }
 public enum RenderTextureReadWrite { Linear }
 public class Texture2D
 {
  public static int FailWrite=-1; public int mipmapCount = 5, anisoLevel, Writes; public bool Linear; public TextureWrapMode wrapMode;
  public Texture2D(int w,int h,TextureFormat f,bool mip,bool linear) { Linear=linear; }
  public void SetPixelData<T>(T[] data,int mip) { if(mip==FailWrite)throw new InjectedFailure(); if(mip!=Writes++) throw new Exception("Mip order changed."); }
 }
 public class RenderTexture
 {
  public static bool FailReleaseOnce; public int width,height; public bool enableRandomWrite,Released;
  public RenderTexture(int w,int h,int d,RenderTextureFormat f,RenderTextureReadWrite r) {width=w;height=h;}
  public void Create() { } public void Release() {if(FailReleaseOnce){FailReleaseOnce=false;throw new InjectedFailure();}Released=true;}
 }
 public class ComputeBuffer : IDisposable
 {
  public static int Live; public static bool FailDisposeOnce; public UnityEngine.Rendering.RequestState State;
  public ComputeBuffer(int n,int stride) {Live++;}
  public void Dispose() {if(FailDisposeOnce){FailDisposeOnce=false;throw new InjectedFailure();} if(State!=null && !State.Done) throw new Exception("Buffer released before readback."); Live--; }
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
  public RequestState State; public bool done=>State.Done; public bool hasError=>AsyncGPUReadback.Error;
  public void WaitForCompletion() {if(AsyncGPUReadback.FailWaitOnce){AsyncGPUReadback.FailWaitOnce=false;throw new InjectedFailure();}State.Done=true;}
  public T[] GetData<T>() { if(!done)throw new Exception("Early read.");return Array.Empty<T>(); }
 }
 public static class AsyncGPUReadback
 {
  public static int Count,FailRequestAt=-1; public static bool Error,FailWaitOnce;
  public static AsyncGPUReadbackRequest Request(UnityEngine.ComputeBuffer b)
  { if(++Count==FailRequestAt)throw new InjectedFailure();b.State=new();return new(){State=b.State}; }
 }
}
