using Unity.Collections;using System;using System.Linq;using System.Collections.Generic;using UnityEngine.Rendering;using System.IO;using System.Reflection;using UnityEngine;using UnityEditor;
public static class Lift02Truss69NativeCheck {
[System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern IntPtr LoadLibraryW(string p);
public static void Run(){try{
const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
string root="D:/CS2BridgeBuilder",managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed",candidate=Environment.GetCommandLineArgs().SkipWhile(a=>a!="-candidate").Skip(1).First();
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
LoadLibraryW(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")));
var asm=Assembly.LoadFrom(candidate);string dir=root+"/artifacts/material-remap/Lift02/coverage54/";
object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));
Vector4[] Unpack(byte[] bytes,string type,VertexAttributeFormat format,int dimension){
var meshType=game.GetType("Game.Prefabs."+type,true);var arrayType=typeof(NativeArray<>).MakeGenericType(meshType);int count=bytes.Length/4;
object values=Activator.CreateInstance(arrayType,new object[]{count,Allocator.Temp,NativeArrayOptions.ClearMemory});using var input=new NativeArray<byte>(bytes,Allocator.Temp);
try{meshType.GetMethods(F).Single(m=>m.Name=="Unpack"&&m.GetParameters().Length==5&&m.GetParameters()[1].ParameterType==arrayType).Invoke(null,new object[]{new NativeSlice<byte>(input),values,count,format,dimension});var result=new Vector4[count];
for(int i=0;i<count;i++){var element=arrayType.GetProperty("Item").GetValue(values,new object[]{i});var v=Get(element,type=="MeshNormal"?"m_Normal":"m_Tangent");float Axis(string n)=>(float)v.GetType().GetField(n).GetValue(v);result[i]=new Vector4(Axis("x"),Axis("y"),Axis("z"),type=="MeshNormal"?0:Axis("w"));}return result;}finally{((IDisposable)values).Dispose();}}
var math=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils",true);
uint Pack(Vector4 value,bool tangent){var method=math.GetMethods(F).Single(m=>m.Name==(tangent?"TangentToOctahedral":"NormalToOctahedral")&&m.GetParameters().Length==1);var ft=method.GetParameters()[0].ParameterType;object fv=Activator.CreateInstance(ft);ft.GetField("x").SetValue(fv,value.x);ft.GetField("y").SetValue(fv,value.y);ft.GetField("z").SetValue(fv,value.z);if(tangent)ft.GetField("w").SetValue(fv,value.w);return (uint)method.Invoke(null,new[]{fv});}
Vector3 Unit(Vector3 a,Vector3 original=default){double l=Math.Sqrt((double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z);if(l==0)return original;return new Vector3((float)(a.x/l),(float)(a.y/l),(float)(a.z/l));}
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
   var args=new object[]{"Lift02CoverageCheck",source,plan,10,null,null,false};
   if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureModel",true).GetMethod("TryBuild",F).Invoke(null,args))throw new Exception("Native structure build failed: "+args[5]);model=args[4];
   var upload=model.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();output=(Mesh)upload.Invoke(model,upload.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());
   if(!output.vertices.SequenceEqual((Vector3[])Get(plan,"Positions")))throw new Exception("Uploaded positions differ from actual Plan"); using var snapshot=Mesh.AcquireReadOnlyMeshData(output);
var transforms=((KeyValuePair<int,Matrix4x4>[])Get(plan,"FrameTransforms")).ToDictionary(p=>p.Key,p=>p.Value);
var copies=(int[])Get(plan,"SourceCopies");int copyStart=((Vector3[])Get(plan,"Positions")).Length-copies.Length;
var sourceNormals=Unpack(attributes.Single(a=>a.a==1).bytes,"MeshNormal",VertexAttributeFormat.SNorm16,2);
var sourceTangents=Unpack(attributes.Single(a=>a.a==2).bytes,"MeshTangent",VertexAttributeFormat.Float32,1);
   foreach(var a in attributes.Where(a=>a.a!=0)){
    var attr=(VertexAttribute)a.a;bool promoted=a.a==4 && a.format==1 && output.GetVertexAttributeFormat(attr)==VertexAttributeFormat.Float32; if((!promoted && (int)output.GetVertexAttributeFormat(attr)!=a.format)||output.GetVertexAttributeDimension(attr)!=a.dimension)throw new Exception("Raw structural attribute layout changed: "+a.a);
    int stream=output.GetVertexAttributeStream(attr),size=a.bytes.Length/count,offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream);var actual=snapshot[0].GetVertexData<byte>(stream).ToArray();
    if(promoted){for(int i=0;i<count;i++)for(int axis=0;axis<a.dimension;axis++){float expectedUv=Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,i*size+axis*2));if(BitConverter.ToSingle(actual,i*step+offset+axis*4)!=expectedUv)throw new Exception("Original Float16 UV value changed during required Float32 promotion.");}}else for(int i=0;i<count;i++)for(int b=0;b<size;b++)if(!((a.a==1||a.a==2)&&transforms.ContainsKey(i))&&a.bytes[i*size+b]!=actual[i*step+offset+b])throw new Exception("Original packed structural channel changed: "+a.a+" vertex "+i);
   }
   foreach(var pair in transforms.Where(p=>p.Key<count||p.Key>=copyStart)){
    int original=pair.Key<count?pair.Key:copies[pair.Key-copyStart];var st=sourceTangents[original];var n=pair.Value.determinant==0f?(Vector3)sourceNormals[original]:Unit(pair.Value.inverse.transpose.MultiplyVector(sourceNormals[original]),sourceNormals[original]);var t0=pair.Value.MultiplyVector(st);var t=Unit(t0-n*Vector3.Dot(n,t0),st);
    foreach(int a in new[]{1,2}){var attr=(VertexAttribute)a;int stream=output.GetVertexAttributeStream(attr);var bytes=snapshot[0].GetVertexData<byte>(stream);int off=pair.Key*output.GetVertexBufferStride(stream)+output.GetVertexAttributeOffset(attr);uint actual=(uint)(bytes[off]|bytes[off+1]<<8|bytes[off+2]<<16|bytes[off+3]<<24);uint expectedFrame=Pack(a==1?(Vector4)n:new Vector4(t.x,t.y,t.z,pair.Value.determinant<0f?-st.w:st.w),a==2);if(actual!=expectedFrame)throw new Exception("Affine native frame mismatch vertex="+pair.Key+" attr="+a);}
   }
   foreach(var a in attributes.Where(a=>a.a==4||a.a==13)){var attr=(VertexAttribute)a.a;int stream=output.GetVertexAttributeStream(attr),offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream),size=a.bytes.Length/count;var bytes=snapshot[0].GetVertexData<byte>(stream);for(int j=0;j<copies.Length;j++){int original=copies[j],target=copyStart+j;bool promoted=a.a==4&&output.GetVertexAttributeFormat(attr)==VertexAttributeFormat.Float32;if(promoted){for(int axis=0;axis<a.dimension;axis++){byte[] q={bytes[target*step+offset+axis*4],bytes[target*step+offset+axis*4+1],bytes[target*step+offset+axis*4+2],bytes[target*step+offset+axis*4+3]};if(BitConverter.ToSingle(q,0)!=Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,original*size+axis*2)))throw new Exception("Copy UV changed");}}else for(int b=0;b<size;b++)if(bytes[target*step+offset+b]!=a.bytes[original*size+b])throw new Exception("Copy raw attribute changed "+a.a);}}
   var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];var expected=new List<int>();var replacements=new Dictionary<int,int[]>();foreach(var fragment in (Array)Get(plan,"Fragments"))replacements.Add((int)Get(fragment,"Triangle"),(int[])Get(fragment,"Indices"));for(int i=0;i<mask.Length;i++){if(!mask[i])expected.AddRange(new[]{sourceIndices[i*3],sourceIndices[i*3+1],sourceIndices[i*3+2]});if(replacements.TryGetValue(i,out var addition))expected.AddRange(addition);}
   if(!output.GetIndices(0).SequenceEqual(expected))throw new Exception("Native structural triangle order or restored membership changed.");
   if(verifyRetile){
    var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var surfaceType=native.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var identifier=native.GetType("Colossal.IO.AssetDatabase.Identifier",true);var guidType=identifier.GetField("guid").FieldType;
    var binding=File.ReadAllLines(dir+"retile-inputs.txt")[lod].Split('|');
    var cid=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{binding[2]});var surface=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(surface,Activator.CreateInstance(identifier,new[]{cid}));
    var tiles=Activator.CreateInstance(asm.GetType("BridgeBuilder.Bridges.MovableStructureTiles",true),F,null,new object[]{"Lift02"},null);var listType=typeof(List<>).MakeGenericType(model.GetType());var owned=(System.Collections.IList)Activator.CreateInstance(listType);var models=(System.Collections.IList)Activator.CreateInstance(listType);var surfaces=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
    asm.GetType("BridgeBuilder.Bridges.MovableStructureRetile",true).GetMethod("Append",F).Invoke(null,new object[]{"Restored outer/walk structure","LiftBridge02"+(lod==0?"":"_LOD"+lod)+" Mesh",0,plan,model,surface,tiles,owned,models,surfaces});
    if(owned.Count!=0||models.Count!=1||!ReferenceEquals(models[0],model)||surfaces.Count!=1||!ReferenceEquals(surfaces[0],surface))throw new Exception("Restored finite-source structure did not retain its actual native model/material through Retile.");
    retileCases++;
   }
   nativeCases++;
  }finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(model!=null)((IDisposable)model).Dispose();UnityEngine.Object.DestroyImmediate(source);}
 }
 for(int lod=0;lod<3;lod++){
using var r=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-raw.bin"));int vc=r.ReadInt32(),ic=r.ReadInt32(),stride=r.ReadInt32(),ac=r.ReadInt32();var ix=new int[ic];for(int i=0;i<ic;i++)ix[i]=stride==4?r.ReadInt32():r.ReadUInt16();var p=new Vector3[vc];
for(int j=0;j<ac;j++){int a=r.ReadInt32(),fmt=r.ReadInt32(),dim=r.ReadInt32(),size=r.ReadInt32();var b=r.ReadBytes(size);if(a==0)for(int i=0;i<vc;i++)p[i]=new Vector3(BitConverter.ToSingle(b,i*12),BitConverter.ToSingle(b,i*12+4),BitConverter.ToSingle(b,i*12+8));}
foreach(float delta in new[]{-20f,-8f,-5.2056884765625f,-5.01171875f,-5.101806640625f,-3.96502685546875f,-5.1163330078125f,-5.1700439453125f,-4.9730224609375f,0f,2f,8.5f,24f}){
var type=asm.GetType("BridgeBuilder.Bridges.Lift02StructurePlan",true);
var plan=type.GetMethod("Create",F).Invoke(null,new object[]{lod,p,ix,delta});VerifyNative(lod,plan,ix,true);Debug.Log("LIFT02_TRUSS69_PASS lod="+lod+" delta="+delta);
}}
File.WriteAllText(root+"/artifacts/material-remap/Lift02/truss69/native-result.txt","PASS native="+nativeCases+" retile="+retileCases);Debug.Log("LIFT02_TRUSS69_COMPLETE native="+nativeCases);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
