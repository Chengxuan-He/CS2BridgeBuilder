using System;using System.Linq;using System.Collections.Generic;using UnityEngine.Rendering;using System.IO;using System.Reflection;using UnityEngine;using UnityEditor;
public static class Lift01Supplement54Check {
[System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern IntPtr LoadLibraryW(string p);
public static void Run(){try{
const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
string root="E:/CS2BridgeBuilder",managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed",candidate=Environment.GetEnvironmentVariable("LIFT01_COVERAGE_DLL");
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
LoadLibraryW(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")));
var asm=Assembly.LoadFrom(candidate);string dir=root+"/artifacts/material-remap/Lift01/coverage54/";
object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
int nativeCases=0,retileCases=0; void VerifyNative(int lod,object plan,int[] sourceIndices,bool verifyRetile=false) {
  using var raw=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-raw.bin"));int count=raw.ReadInt32(),indexCount=raw.ReadInt32(),indexStride=raw.ReadInt32(),attributeCount=raw.ReadInt32();raw.ReadBytes(indexCount*indexStride);
  var attributes=new List<(int a,int format,int dimension,byte[] bytes)>();
  for(int i=0;i<attributeCount;i++){int a=raw.ReadInt32(),format=raw.ReadInt32(),dimension=raw.ReadInt32(),size=raw.ReadInt32();attributes.Add((a,format,dimension,raw.ReadBytes(size)));}
  var source=new Mesh();source.indexFormat=IndexFormat.UInt32;Mesh output=null;object model=null;
  try {
   source.SetVertexBufferParams(count,attributes.Select(a=>new VertexAttributeDescriptor((VertexAttribute)a.a,(VertexAttributeFormat)a.format,a.dimension,a.a==0?0:1)).ToArray());
   source.SetVertexBufferData(attributes[0].bytes,0,0,attributes[0].bytes.Length,0);
   int stride=source.GetVertexBufferStride(1);var buffer=new byte[count*stride];
   foreach(var a in attributes.Where(a=>a.a!=0)){int offset=source.GetVertexAttributeOffset((VertexAttribute)a.a),size=a.bytes.Length/count;for(int i=0;i<count;i++)Array.Copy(a.bytes,i*size,buffer,i*stride+offset,size);}
   source.SetVertexBufferData(buffer,0,0,buffer.Length,1);source.SetIndices(sourceIndices,MeshTopology.Triangles,0);
   var args=new object[]{"Lift01CoverageCheck",source,plan,5,null,null,false};
   if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureModel",true).GetMethod("TryBuild",F).Invoke(null,args))throw new Exception("Native structure build failed: "+args[5]);model=args[4];
   var upload=model.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();output=(Mesh)upload.Invoke(model,upload.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());
   using var snapshot=Mesh.AcquireReadOnlyMeshData(output);
   foreach(var a in attributes.Where(a=>a.a!=0)){
    var attr=(VertexAttribute)a.a;bool promoted=a.a==4 && a.format==1 && output.GetVertexAttributeFormat(attr)==VertexAttributeFormat.Float32; if((!promoted && (int)output.GetVertexAttributeFormat(attr)!=a.format)||output.GetVertexAttributeDimension(attr)!=a.dimension)throw new Exception("Raw structural attribute layout changed: "+a.a);
    int stream=output.GetVertexAttributeStream(attr),size=a.bytes.Length/count,offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream);var actual=snapshot[0].GetVertexData<byte>(stream).ToArray();
    if(promoted){for(int i=0;i<count;i++)for(int axis=0;axis<a.dimension;axis++){float expectedUv=Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,i*size+axis*2));if(BitConverter.ToSingle(actual,i*step+offset+axis*4)!=expectedUv)throw new Exception("Original Float16 UV value changed during required Float32 promotion.");}}else for(int i=0;i<count;i++)for(int b=0;b<size;b++)if(a.bytes[i*size+b]!=actual[i*step+offset+b])throw new Exception("Original packed structural channel changed: "+a.a+" vertex "+i);
   }
   var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];var expected=new List<int>();var replacements=new Dictionary<int,int[]>();foreach(var fragment in (Array)Get(plan,"Fragments"))replacements.Add((int)Get(fragment,"Triangle"),(int[])Get(fragment,"Indices"));for(int i=0;i<mask.Length;i++){if(!mask[i])expected.AddRange(new[]{sourceIndices[i*3],sourceIndices[i*3+1],sourceIndices[i*3+2]});if(replacements.TryGetValue(i,out var addition))expected.AddRange(addition);}
   if(!output.GetIndices(0).SequenceEqual(expected))throw new Exception("Native structural triangle order or restored membership changed.");
   if(verifyRetile){
    var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var surfaceType=native.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var identifier=native.GetType("Colossal.IO.AssetDatabase.Identifier",true);var guidType=identifier.GetField("guid").FieldType;
    var cid=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{lod==2?"e0101c8ca7e9dc14016d187d012704a4":"bcdd054871e3bcb50f793ccefc50304d"});var surface=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(surface,Activator.CreateInstance(identifier,new[]{cid}));
    using(var header=new BinaryReader(File.OpenRead(root+"/artifacts/material-remap/Lift01/"+(lod==2?"far-finite/Original.Surface":"half-floor/inputs/Original.Surface"))))surfaceType.GetMethod("ReadHeader",F).Invoke(surface,new object[]{header});
    var tiles=Activator.CreateInstance(asm.GetType("BridgeBuilder.Bridges.MovableStructureTiles",true),F,null,new object[]{"Lift01"},null);var listType=typeof(List<>).MakeGenericType(model.GetType());var owned=(System.Collections.IList)Activator.CreateInstance(listType);var models=(System.Collections.IList)Activator.CreateInstance(listType);var surfaces=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
    var retileArgs=new object[]{"Restored outer/walk structure","LiftBridge01"+(lod==0?"":"_LOD"+lod)+" Mesh",0,plan,model,surface,tiles,owned,models,surfaces,null};if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureRetile",true).GetMethod("TryAppend",F).Invoke(null,retileArgs))throw new Exception((string)retileArgs[retileArgs.Length-1]);
    if(owned.Count!=0||models.Count!=1||!ReferenceEquals(models[0],model)||surfaces.Count!=1||!ReferenceEquals(surfaces[0],surface))throw new Exception("Restored finite-source structure did not retain its actual native model/material through Retile.");
    retileCases++;
   }
   nativeCases++;
  }finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(model!=null)((IDisposable)model).Dispose();UnityEngine.Object.DestroyImmediate(source);}
 }
 for(int lod=1;lod<2;lod++){
using var r=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-raw.bin"));int vc=r.ReadInt32(),ic=r.ReadInt32(),stride=r.ReadInt32(),ac=r.ReadInt32();var ix=new int[ic];for(int i=0;i<ic;i++)ix[i]=stride==4?r.ReadInt32():r.ReadUInt16();var p=new Vector3[vc];
for(int j=0;j<ac;j++){int a=r.ReadInt32(),fmt=r.ReadInt32(),dim=r.ReadInt32(),size=r.ReadInt32();var b=r.ReadBytes(size);if(a==0)for(int i=0;i<vc;i++)p[i]=new Vector3(BitConverter.ToSingle(b,i*12),BitConverter.ToSingle(b,i*12+4),BitConverter.ToSingle(b,i*12+8));}
foreach(float delta in new[]{-1.25f,0f,2f,8f}){
var type=asm.GetType("BridgeBuilder.Bridges.Lift01FarTowerCore",true);
var plan=type.GetMethod("Create",F).Invoke(null,new object[]{p,ix,delta}); plan=asm.GetType("BridgeBuilder.Bridges.Lift01FarEndLips",true).GetMethod("Apply",F).Invoke(null,new object[]{plan,delta});VerifyNative(lod,plan,ix,true);Debug.Log("LIFT01_COVERAGE_PASS lod="+lod+" delta="+delta);
}}
Debug.Log("LIFT01_COVERAGE_COMPLETE native="+nativeCases);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
