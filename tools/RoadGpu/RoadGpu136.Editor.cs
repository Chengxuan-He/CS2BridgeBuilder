using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Diagnostics;using System.Reflection;using System.Runtime.InteropServices;using System.Security.Cryptography;using UnityEngine;using UnityEditor;
public static class RoadGpu136 {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 struct Step{public Vector4 plane;public uint positive;}struct Path{public uint first,count,bone;}struct Root{public uint first,count;}
 class Node{public int bone,neg,pos;public bool gap;public Matrix4x4 frame;}
 static byte[] Bytes(Array a,int size){var b=new byte[a.Length*size];var h=GCHandle.Alloc(a,GCHandleType.Pinned);try{Marshal.Copy(h.AddrOfPinnedObject(),b,0,b.Length);}finally{h.Free();}return b;}
 static Array ArrayOf(Type type,byte[] b,int stride){var a=Array.CreateInstance(type,b.Length/stride);var h=GCHandle.Alloc(a,GCHandleType.Pinned);try{Marshal.Copy(b,0,h.AddrOfPinnedObject(),b.Length);}finally{h.Free();}return a;}
 static uint[] Words(byte[] b){var u=new uint[b.Length/4];Buffer.BlockCopy(b,0,u,0,b.Length);return u;}
 static double Ms(long t)=>t*1000.0/Stopwatch.Frequency;
 public static void Run(){string report="";try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");var file=Arg("-replay");var output=Arg("-report");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=System.IO.Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};var mod=Assembly.LoadFrom(Arg("-modAssembly"));var geo=mod.GetType("BridgeBuilder.Bridges.MovableRoadGeometry",true);var tri=geo.GetNestedType("Triangle",F);var partition=geo.GetNestedType("Partition",F);
 object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 byte[] raw,expected;int count,bones,materials,root,expectedCount;Pose pose;bool[] planar;float[] scale;Node[] nodes;int[] occurrences=null;Dictionary<(int,int),int> faces=null;
 using(var r=new BinaryReader(File.OpenRead(file))){if(r.ReadInt32()!=0x42525231||r.ReadInt32()!=228)throw new Exception("format");count=r.ReadInt32();raw=r.ReadBytes(count*228);pose=new Pose(new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle()),new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle()));bones=r.ReadInt32();materials=r.ReadInt32();int n=r.ReadInt32();planar=new bool[n];scale=new float[n];for(int i=0;i<n;i++){planar[i]=r.ReadBoolean();scale[i]=r.ReadSingle();}n=r.ReadInt32();root=r.ReadInt32();nodes=new Node[n];for(int i=0;i<n;i++){var v=new Node{bone=r.ReadInt32(),gap=r.ReadBoolean(),neg=r.ReadInt32(),pos=r.ReadInt32()};for(int k=0;k<16;k++)v.frame[k]=r.ReadSingle();nodes[i]=v;}n=r.ReadInt32();if(n>=0){occurrences=new int[n];for(int i=0;i<n;i++)occurrences[i]=r.ReadInt32();}n=r.ReadInt32();if(n>=0){faces=new Dictionary<(int,int),int>();for(int i=0;i<n;i++)faces.Add((r.ReadInt32(),r.ReadInt32()),r.ReadInt32());}expectedCount=r.ReadInt32();expected=r.ReadBytes(32);}
 var floats=new float[raw.Length/4];Buffer.BlockCopy(raw,0,floats,0,raw.Length);for(int i=0;i<count;i++){int slot=BitConverter.ToInt32(raw,i*228+216);if(planar[slot])for(int j=0;j<3;j++){int k=i*57+j*18;floats[k+10]=floats[k]*scale[slot];floats[k+11]=floats[k+2]*scale[slot];floats[k+12]=floats[k+13]=0;}}Buffer.BlockCopy(floats,0,raw,0,raw.Length);
 var source=ArrayOf(tri,raw,228);source=(Array)geo.GetMethod("InFrame",F).Invoke(null,new object[]{source,pose});raw=Bytes(source,228);
 var objects=new object[nodes.Length];object Build(int i){if(objects[i]!=null)return objects[i];var n=nodes[i];return objects[i]=n.gap?partition.GetField("Gap",F).GetValue(null):n.bone>=0?New(partition,n.bone):New(partition,n.frame,Build(n.neg),Build(n.pos));}
 var tree=Build(root);Array oc=null;if(occurrences!=null){oc=Array.CreateInstance(partition,occurrences.Length);for(int i=0;i<oc.Length;i++)oc.SetValue(Build(occurrences[i]),i);}IDictionary fc=null;if(faces!=null){fc=(IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(ValueTuple<int,int>),partition));foreach(var f in faces)fc.Add(f.Key,Build(f.Value));}
 var times=new List<double>();Array bound=null;for(int run=0;run<8;run++){object[] ba={source,tree,bones,materials,null,null,false,oc,fc};long t=Stopwatch.GetTimestamp();if(!(bool)geo.GetMethod("TryBind",F).Invoke(null,ba))throw new Exception("CPU bind "+ba[5]);times.Add(Ms(Stopwatch.GetTimestamp()-t));bound=(Array)ba[4];}
 var oracle=Bytes(bound,232);using(var sha=SHA256.Create())if(bound.Length!=expectedCount||!sha.ComputeHash(oracle).SequenceEqual(expected))throw new Exception("CPU replay differs from captured output");

 var gpu=mod.GetType("BridgeBuilder.Bridges.MovableRoadGpu",true);var method=gpu.GetMethod("TryBuild",F);
 report="GPU="+SystemInfo.graphicsDeviceName+" API="+SystemInfo.graphicsDeviceType+" Unity="+Application.unityVersion+" assembly="+mod.Location+"\nsource="+file+" sourceTriangles="+count+" boundTriangles="+bound.Length+" CPUbindMs="+string.Join(",",times)+"\n";
 var measurements=new List<double>();Array actualBound=null;uint[] packed=null;
 for(int run=0;run<8;run++){
  object[] ga={source,tree,bones,materials,null,null,null,false,oc,fc};long t=Stopwatch.GetTimestamp();
  if(!(bool)method.Invoke(null,ga))throw new Exception("production GPU result "+ga[6]);measurements.Add(Ms(Stopwatch.GetTimestamp()-t));
  actualBound=(Array)ga[4];packed=(uint[])ga[5];if(packed==null)throw new Exception("Unexpected CPU fallback; GPU path was not exercised");
  var actual=Words(Bytes(actualBound,232));var reference=Words(oracle);if(actual.Length!=reference.Length)throw new Exception("Topology count mismatch");
  var errorsByField=new long[7];var max=new double[5];
  for(int i=0;i<actual.Length;i++){if(actual[i]==reference[i])continue;int slot=i%58,v=slot%18,g=slot>=54?5+(slot==57?1:0):v<3?0:v<6?1:v<10?2:v<14?3:4;errorsByField[g]++;if(g==0||g>=4)throw new Exception("Position/color/provenance/bone mismatch word "+i);float a=BitConverter.ToSingle(BitConverter.GetBytes(actual[i]),0),b=BitConverter.ToSingle(BitConverter.GetBytes(reference[i]),0);max[g]=Math.Max(max[g],Math.Abs((double)a-b));}
  if(run==1)report+="bound differences position/normal/tangent/UV/color/provenance/bone="+string.Join(",",errorsByField)+" maxAbs="+string.Join(",",max)+"\n";
 }
 report+="ProductionConnectedGPUms="+string.Join(",",measurements)+"; includes input validation, partition preparation, upload, GPU clip+encode, both readbacks, output validation, temporary buffer cleanup\n";File.WriteAllText(output,report);
 var modelType=mod.GetType("BridgeBuilder.Bridges.MovableRoadModel",true);object[] ma={"connected GPU native publication",actualBound,materials,bones,packed,null,null};long ms=Stopwatch.GetTimestamp();
 if(!(bool)modelType.GetMethod("TryBuildPacked",F).Invoke(null,ma))throw new Exception("Native model "+ma[6]);var model=ma[5];
 report+="NativePackedModelMs="+Ms(Stopwatch.GetTimestamp()-ms)+"\n";
 var mesh=(Mesh)model.GetType().GetMethod("ToUnityMesh").Invoke(model,new object[]{true});
 if(mesh.vertexCount!=actualBound.Length*3||mesh.triangles.Length!=mesh.vertexCount)throw new Exception("Native publication shape mismatch");
 var channelBytes=new Dictionary<UnityEngine.Rendering.VertexAttribute,byte[]>();
 foreach(UnityEngine.Rendering.VertexAttribute attr in new[]{UnityEngine.Rendering.VertexAttribute.Position,UnityEngine.Rendering.VertexAttribute.Normal,UnityEngine.Rendering.VertexAttribute.Tangent,UnityEngine.Rendering.VertexAttribute.TexCoord0,UnityEngine.Rendering.VertexAttribute.Color,UnityEngine.Rendering.VertexAttribute.BlendIndices}){
  var data=model.GetType().GetMethod("GetAttributeData").Invoke(model,new object[]{attr});var value=data.GetType().GetProperty("data").GetValue(data);var rawChannel=(byte[])value.GetType().GetMethod("ToArray",Type.EmptyTypes).Invoke(value,null);
  channelBytes.Add(attr,rawChannel);int stride=rawChannel.Length/mesh.vertexCount,offset=attr==UnityEngine.Rendering.VertexAttribute.Position?0:attr==UnityEngine.Rendering.VertexAttribute.Normal?3:attr==UnityEngine.Rendering.VertexAttribute.Tangent?4:attr==UnityEngine.Rendering.VertexAttribute.TexCoord0?5:attr==UnityEngine.Rendering.VertexAttribute.Color?9:10;
  var ww=Words(rawChannel);for(int i=0;i<mesh.vertexCount;i++)for(int k=0;k<stride/4;k++)if(ww[i*stride/4+k]!=packed[i*11+offset+k])throw new Exception("Native channel mismatch "+attr);
  report+="NativeChannel "+attr+" stride="+stride+" exact packed match\n";
 }
 try{report+=RoadGpu136Persistence.Check(model,managed,output+".Geometry",channelBytes);}
 catch(Exception gpuSaveError){
  if(!gpuSaveError.ToString().Contains("NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr"))throw;
  object[] ca={"CPU persistence control",bound,materials,bones,null,null};if(!(bool)modelType.GetMethod("TryBuild",F).Invoke(null,ca))throw new Exception("CPU model control failed");
  bool same=false;try{RoadGpu136Persistence.Check(ca[4],managed,output+".cpu.Geometry",channelBytes);}catch(Exception cpuSaveError){same=cpuSaveError.ToString().Contains("NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr");}finally{((IDisposable)ca[4]).Dispose();}
  if(!same)throw new Exception("Native persistence failure was not reproduced in CPU control",gpuSaveError);
  report+="PENDING native save/reload: both unchanged CPU model and GPU model fail at the editor host NativeList.AsArray/NativeArrayUnsafeUtility safety-handle boundary in CreateDataFromGeometry. No saved-asset acceptance claimed.\n";
 }
 ((IDisposable)model).Dispose();if(mesh.vertexCount!=actualBound.Length*3)throw new Exception("Published mesh borrowed disposed model");UnityEngine.Object.DestroyImmediate(mesh);
 report+="PASS connected production GPU output; native model all channels exact, publication remains alive after source model disposal.\n";File.WriteAllText(output,report);UnityEngine.Debug.Log(report);EditorApplication.Exit(0);
 }catch(Exception e){UnityEngine.Debug.LogException(e);var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-report");if(i>=0)File.AppendAllText(a[i+1],"\nFAIL "+e);EditorApplication.Exit(1);}}
}
