using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.IO.LowLevel.Unsafe;
using System.Runtime.Serialization;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;

// Invoked by the isolated Unity Editor with the installed game assemblies and a sandboxed fixture.
public static class GameMonoProbe
{
    static readonly BindingFlags Fields=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
    static object Blank(Type t)=>FormatterServices.GetUninitializedObject(t);
    static FieldInfo F(object o,string n)=>o.GetType().GetField(n,Fields)??throw new MissingFieldException(n);
    static object Init(object o,string n){var f=F(o,n);var v=Activator.CreateInstance(f.FieldType);f.SetValue(o,v);return v;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static unsafe void CheckReadFence(object stream, Action disconnect)
    {
        string path=Path.GetTempFileName(); var buffer=Marshal.AllocHGlobal(1024*1024);
        var data=EnumerableBytes(); ReadHandle read=default;
        try {
            File.WriteAllBytes(path,data);
            var command=new ReadCommand { Offset=0, Size=data.Length, Buffer=buffer.ToPointer() };
            read=AsyncReadManager.Read(path,&command,1);
            var busy=(IList)F(stream,"m_BusyRequests").GetValue(stream);
            var req=Blank(busy.GetType().GetGenericArguments()[0]);
            ((IList)Init(req,"m_ReadHandles")).Add(read);
            Init(req,"m_ReadAndDecompressionHandles");busy.Add(req);
            disconnect();
            Check(read.IsValid() && read.Status==ReadStatus.Complete && Marshal.ReadByte(buffer,data.Length-1)==97,"live read fence failed or disposed native handle");
            busy.Clear();
        } finally { if(read.IsValid()){read.JobHandle.Complete();read.Dispose();} Marshal.FreeHGlobal(buffer);File.Delete(path); }
    }
    static byte[] EnumerableBytes(){var bytes=new byte[1024*1024];bytes[bytes.Length-1]=97;return bytes;}
    public static void Run()
    {
        var report=Environment.GetEnvironmentVariable("BB_AFFINE_REPORT");
        try {
            var mod=Assembly.Load(File.ReadAllBytes(Environment.GetEnvironmentVariable("BB_AFFINE_CANDIDATE")));
            var disconnect=mod.GetType("BridgeBuilder.Runtime.BridgeVirtualTextureRemoval",true).GetMethod("Disconnect",BindingFlags.NonPublic|BindingFlags.Static);
            var stream=(TextureStreamingSystem)Blank(typeof(TextureStreamingSystem));
            var data=(VTDatabase)Blank(typeof(VTDatabase));F(stream,"m_VtDatabase").SetValue(stream,data);
            var atlas=(AtlasMaterialsDatabase)Blank(typeof(AtlasMaterialsDatabase));F(stream,"m_AtlasMaterialsDatabase").SetValue(stream,atlas);
            var groups=(List<AtlasMaterialsGrouper>)Init(atlas,"m_AtlasMaterialsGroupers");
            var registered=(HashSet<AtlasMaterialsGrouper>)Init(atlas,"m_RegisteredAtlasMaterialsGroupers");
            var tiles=(IDictionary)Init(atlas,"m_PreprocessedMidMipTilesInfo");
            var cells=(IDictionary)Activator.CreateInstance(tiles.GetType().GetGenericArguments()[1]);tiles.Add(0,cells);
            var paths=(IDictionary)Init(data,"m_VTTexturePaths");var channels=(IList)Init(data,"m_TextureGuidsPerIndex");
            var stacks=(IList)Init(data,"m_VTSurfaceTileInfo");
            var slots=(IDictionary)Activator.CreateInstance(stacks.GetType().GetGenericArguments()[0]);stacks.Add(slots);
            var layerType=channels.GetType().GetGenericArguments()[0].GetElementType();
            var layers=Array.CreateInstance(layerType,1);var layer=(IDictionary)Activator.CreateInstance(layerType);layers.SetValue(layer,0);channels.Add(layers);
            Init(stream,"m_TexturesAsyncLoaders"); Init(stream,"m_BusyRequests");
            Init(data,"m_LoadingData");Init(data,"m_TexturesData");var registry=(IDictionary)Init(data,"m_RegisteredTextures");
            var queue=(Queue<SurfaceAsset>)Init(stream,"m_VTAssetsToInit");var shallow=(Queue<SurfaceAsset>)Init(stream,"m_VTAssetsShallowInit");
            var target=(SurfaceAsset)Blank(typeof(SurfaceAsset));var other=(SurfaceAsset)Blank(typeof(SurfaceAsset));
            var material=Blank(F(stream,"m_WorkingSetLodBiasMat").FieldType);F(stream,"m_WorkingSetLodBiasMat").SetValue(stream,material);
            for(int i=0;i<2;i++){
                var path=i==0?"target/file":"missing-foreign/file";
                var id=Colossal.Hash128.Parse(i==0?"00000000000000000000000000000001":"00000000000000000000000000000002");
                var group=(AtlasMaterialsGrouper)Blank(typeof(AtlasMaterialsGrouper));F(group,"m_MidMipPath").SetValue(group,path);Init(group,"m_PreprocessedTiles");
                groups.Add(group);registered.Add(group);
                var cell=Blank(cells.GetType().GetGenericArguments()[1]);F(cell,"m_Grouper").SetValue(cell,group);cells.Add(i,cell);
                Check(data.RegisterTextureData(id,8+i),"register staging");data.DoneLoading(id);
                var tex=Blank(paths.GetType().GetGenericArguments()[1]);F(tex,"path").SetValue(tex,path);paths.Add(id,tex);layer.Add(i,id);
                var info=Blank(typeof(VTDatabase.VTSurfaceInfo));F(info,"path").SetValue(info,path);slots.Add(i,info);
                queue.Enqueue(i==0?target:other);shallow.Enqueue(i==0?other:target);
            }
            var args=new object[]{stream,(Func<string,bool>)(p=>p!=null&&p.StartsWith("target/")),(Func<SurfaceAsset,bool>)(a=>ReferenceEquals(a,target))};
            CheckReadFence(stream,()=>disconnect.Invoke(null,args));disconnect.Invoke(null,args);
            Check(data.dataSize==9 && registry.Count==1,"target staging freed and foreign allocation preserved");
            foreach(IDisposable allocation in ((IDictionary)F(data,"m_TexturesData").GetValue(data)).Values)allocation.Dispose();
            Check(ReferenceEquals(material,F(stream,"m_WorkingSetLodBiasMat").GetValue(stream)),"global material changed");
            Check(ReferenceEquals(data,F(stream,"m_VtDatabase").GetValue(stream)),"global database changed");
            Check(data.GetVTSurfaceTileInfo(0,0)==null&&data.GetVTSurfaceTileInfo(0,1)!=null,"per-surface route");
            Check(!data.IsVTTextureData(0,0,0)&&data.IsVTTextureData(0,0,1)&&paths.Count==1,"per-layer route");
            Check(!atlas.TryGetMidMipMask(0,0,out _)&&atlas.TryGetMidMipMask(0,1,out _)&&groups.Count==1&&registered.Count==1,"MidMip route");
            Check(queue.Count==1&&ReferenceEquals(queue.Peek(),other)&&shallow.Count==1&&ReferenceEquals(shallow.Peek(),other),"foreign queue");
            File.WriteAllText(report,"PASS native stream/database identity, mip-bias material, missing unrelated asset, owned/foreign VTSurface and VTTexture routes, MidMip tile owners, two queues, completed live native file read without disposal, and retry.");
        } catch(Exception e){File.WriteAllText(report,e.ToString());throw;}
    }
}
