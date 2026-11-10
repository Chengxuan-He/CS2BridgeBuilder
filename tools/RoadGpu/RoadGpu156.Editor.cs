using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Diagnostics;using System.Reflection;using System.Runtime.InteropServices;using System.Security.Cryptography;using UnityEngine;using UnityEditor;
public static class RoadGpu156 {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 struct Step{public Vector4 plane;public uint positive;}struct Path{public uint first,count,bone;}struct Root{public uint first,count;}
 class Node{public int bone,neg,pos;public bool gap;public Matrix4x4 frame;}
 static byte[] Bytes(Array a,int size){var b=new byte[a.Length*size];var h=GCHandle.Alloc(a,GCHandleType.Pinned);try{Marshal.Copy(h.AddrOfPinnedObject(),b,0,b.Length);}finally{h.Free();}return b;}
 static Array ArrayOf(Type type,byte[] b,int stride){var a=Array.CreateInstance(type,b.Length/stride);var h=GCHandle.Alloc(a,GCHandleType.Pinned);try{Marshal.Copy(b,0,h.AddrOfPinnedObject(),b.Length);}finally{h.Free();}return a;}
 static uint[] Words(byte[] b){var u=new uint[b.Length/4];Buffer.BlockCopy(b,0,u,0,b.Length);return u;}
 static double Ms(long t)=>t*1000.0/Stopwatch.Frequency;
 static async System.Threading.Tasks.Task<object> Pump(Assembly mod, Func<System.Threading.Tasks.Task> build) {
 var t=mod.GetType("BridgeBuilder.Bridges.GenerationWork",true);var w=Activator.CreateInstance(t,F,null,new object[]{"actual BRR1 async",(Func<bool>)(()=>true)},null);
 var done=new System.Threading.Tasks.TaskCompletionSource<object>();double started=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction tick=null;
 tick=()=>{try{if(EditorApplication.timeSinceStartup-started>120)throw new Exception("async pipeline timeout");t.GetMethod("Tick",F).Invoke(w,null);if(!(bool)t.GetProperty("IsComplete",F).GetValue(w))return;
 var task=(System.Threading.Tasks.Task)t.GetProperty("Completion",F).GetValue(w);task.GetAwaiter().GetResult();var result=task.GetType().GetProperty("Result")?.GetValue(task);t.GetMethod("Report",F).Invoke(w,null);EditorApplication.update-=tick;done.SetResult(result);
 }catch(Exception e){EditorApplication.update-=tick;done.SetException(e);}};
 t.GetMethod("Start",F).Invoke(w,new object[]{build});EditorApplication.update+=tick;return await done.Task;
 }
 public static async void Run(){string report="";try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];var managed=Arg("-gameManaged");var file=Arg("-replay");var output=Arg("-report");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=System.IO.Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};var mod=Assembly.LoadFrom(Arg("-modAssembly"));var log=mod.GetType("BridgeBuilder.Mod",true).GetProperty("Log",F).GetValue(null);var ilog=log.GetType().GetInterfaces().First(t=>t.Name=="ILog");var level=ilog.GetProperty("effectivenessLevel");level.SetValue(log,level.PropertyType.GetField("Info").GetValue(null));ilog.GetProperty("redirectToDefault").SetValue(log,true);
 var snapshotType=mod.GetType("BridgeBuilder.Runtime.BridgeMaterialFileSnapshot",true);
 var fixtureRoot=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(output),"snapshot-fixture");Directory.CreateDirectory(System.IO.Path.Combine(fixtureRoot,"Probe"));
 var fixtureFile=System.IO.Path.Combine(fixtureRoot,"Probe","Fixture.Surface");File.WriteAllBytes(fixtureFile,new byte[]{1,2,3,4});
 var snapshot=snapshotType.GetMethod("Read",F).Invoke(null,new object[]{fixtureRoot,"Probe",System.Threading.CancellationToken.None});
 if(snapshotType.GetProperty("Error",F).GetValue(snapshot)!=null)throw new Exception("snapshot read failed");
 var retained=(IDisposable)snapshotType.GetMethod("Retain",F).Invoke(snapshot,null);((IDisposable)snapshot).Dispose();((IDisposable)snapshot).Dispose();
 if((int)snapshotType.GetProperty("FileCount",F).GetValue(snapshot)!=1)throw new Exception("snapshot released before async request");
 object[] lookup={fixtureFile,null,null};if(!(bool)snapshotType.GetMethod("TryGet",F).Invoke(snapshot,lookup))throw new Exception("retained snapshot unavailable");
 retained.Dispose();retained.Dispose();if((int)snapshotType.GetProperty("FileCount",F).GetValue(snapshot)!=0)throw new Exception("snapshot reference leak");
 using(var opened=File.Open(fixtureFile,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){}
 report+="PASS actual DLL prepared snapshot retained through callback return and released once after request.\n";
 var geo=mod.GetType("BridgeBuilder.Bridges.MovableRoadGeometry",true);var tri=geo.GetNestedType("Triangle",F);var partition=geo.GetNestedType("Partition",F);
 object New(Type t,params object[] a)=>Activator.CreateInstance(t,F,null,a,null);
 byte[] raw,expected;int count,bones,materials,root,expectedCount;Pose pose;bool[] planar;float[] scale;Node[] nodes;int[] occurrences=null;Dictionary<(int,int),int> faces=null;
 using(var r=new BinaryReader(File.OpenRead(file))){if(r.ReadInt32()!=0x42525231||r.ReadInt32()!=228)throw new Exception("format");count=r.ReadInt32();raw=r.ReadBytes(count*228);pose=new Pose(new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle()),new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle()));bones=r.ReadInt32();materials=r.ReadInt32();int n=r.ReadInt32();planar=new bool[n];scale=new float[n];for(int i=0;i<n;i++){planar[i]=r.ReadBoolean();scale[i]=r.ReadSingle();}n=r.ReadInt32();root=r.ReadInt32();nodes=new Node[n];for(int i=0;i<n;i++){var v=new Node{bone=r.ReadInt32(),gap=r.ReadBoolean(),neg=r.ReadInt32(),pos=r.ReadInt32()};for(int k=0;k<16;k++)v.frame[k]=r.ReadSingle();nodes[i]=v;}n=r.ReadInt32();if(n>=0){occurrences=new int[n];for(int i=0;i<n;i++)occurrences[i]=r.ReadInt32();}n=r.ReadInt32();if(n>=0){faces=new Dictionary<(int,int),int>();for(int i=0;i<n;i++)faces.Add((r.ReadInt32(),r.ReadInt32()),r.ReadInt32());}expectedCount=r.ReadInt32();expected=r.ReadBytes(32);}
 var originalRaw=(byte[])raw.Clone();
 var floats=new float[raw.Length/4];Buffer.BlockCopy(raw,0,floats,0,raw.Length);for(int i=0;i<count;i++){int slot=BitConverter.ToInt32(raw,i*228+216);if(planar[slot])for(int j=0;j<3;j++){int k=i*57+j*18;floats[k+10]=floats[k]*scale[slot];floats[k+11]=floats[k+2]*scale[slot];floats[k+12]=floats[k+13]=0;}}Buffer.BlockCopy(floats,0,raw,0,raw.Length);
 var source=ArrayOf(tri,raw,228);source=(Array)geo.GetMethod("InFrame",F).Invoke(null,new object[]{source,pose});raw=Bytes(source,228);
 var objects=new object[nodes.Length];object Build(int i){if(objects[i]!=null)return objects[i];var n=nodes[i];return objects[i]=n.gap?partition.GetField("Gap",F).GetValue(null):n.bone>=0?New(partition,n.bone):New(partition,n.frame,Build(n.neg),Build(n.pos));}
 var tree=Build(root);Array oc=null;if(occurrences!=null){oc=Array.CreateInstance(partition,occurrences.Length);for(int i=0;i<oc.Length;i++)oc.SetValue(Build(occurrences[i]),i);}IDictionary fc=null;if(faces!=null){fc=(IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(ValueTuple<int,int>),partition));foreach(var f in faces)fc.Add(f.Key,Build(f.Value));}
 var times=new List<double>();Array bound=null;for(int run=0;run<1;run++){object[] ba={source,tree,bones,materials,null,null,false,oc,fc};long t=Stopwatch.GetTimestamp();if(!(bool)geo.GetMethod("TryBind",F).Invoke(null,ba))throw new Exception("CPU bind "+ba[5]);times.Add(Ms(Stopwatch.GetTimestamp()-t));bound=(Array)ba[4];}
 var oracle=Bytes(bound,232);using(var sha=SHA256.Create())if(bound.Length!=expectedCount||!sha.ComputeHash(oracle).SequenceEqual(expected))throw new Exception("CPU replay differs from captured output");


 var gpu=mod.GetType("BridgeBuilder.Bridges.MovableRoadGpu",true);
 var old=gpu.GetMethod("TryBuild",F);var current=gpu.GetMethod("TryBuildNative",F);
 var mappingType=current.GetParameters()[12].ParameterType.GetElementType();var inputMappings=Array.CreateInstance(mappingType,planar.Length);
 for(int j=0;j<planar.Length;j++)inputMappings.SetValue(New(mappingType,planar[j],scale[j],null),j);
 var nativeSource=args.Contains("-frame")?ArrayOf(tri,originalRaw,228):source;
 var assets=mod.GetType("BridgeBuilder.Bridges.MovableRoadAssets",true);var group=assets.GetMethod("BuildMaterialMeshes",F);
 var st=current.GetParameters()[3].ParameterType.GetElementType();
 var surfaces=Array.CreateInstance(st,materials);for(int i=0;i<materials;i++)surfaces.SetValue(System.Runtime.Serialization.FormatterServices.GetUninitializedObject(st),i);
 var mt=group.ReturnType.GetElementType();
 if(args.Contains("-shared"))for(int i=1;i<materials;i+=2)surfaces.SetValue(surfaces.GetValue(i-1),i);
 object Owned()=>Activator.CreateInstance(typeof(List<>).MakeGenericType(mt));
 byte[] Channel(object model,UnityEngine.Rendering.VertexAttribute attr){var d=mt.GetMethod("GetAttributeData").Invoke(model,new object[]{attr});var a=d.GetType().GetProperty("data").GetValue(d);return (byte[])a.GetType().GetMethod("ToArray",Type.EmptyTypes).Invoke(a,null);}
 var oldTimes=new List<double>();var newTimes=new List<double>();
 for(int run=0;run<(args.Contains("-capture")?1:3);run++){
 object[] ga={source,tree,bones,materials,null,null,null,false,oc,fc};long start=Stopwatch.GetTimestamp();
 if(!(bool)old.Invoke(null,ga)||ga[5]==null)throw new Exception("old GPU fallback");
 var own=Owned();object[] ma={"baseline",ga[4],surfaces,bones,own,null,null,ga[5]};var before=(Array)group.Invoke(null,ma);double beforeMs=Ms(Stopwatch.GetTimestamp()-start);
 object[] na={nativeSource,tree,bones,surfaces,null,null,null,args.Contains("-capture"),false,oc,fc,args.Contains("-frame")?(object)pose:null,args.Contains("-frame")?inputMappings:null};start=Stopwatch.GetTimestamp();
 object[] asyncArgs={na[0],na[1],na[2],na[3],na[7],na[8],na[9],na[10],na[11],na[12]};
 var result=await Pump(mod,()=> (System.Threading.Tasks.Task)gpu.GetMethod("BuildNativeAsync",F).Invoke(null,asyncArgs));
 if(!(bool)result.GetType().GetField("Success",F).GetValue(result))throw new Exception("async build failed "+result.GetType().GetField("Error",F).GetValue(result));
 na[4]=result.GetType().GetField("Prepared",F).GetValue(result);na[5]=result.GetType().GetField("Bound",F).GetValue(result);
 var prep=na[4];var own2=Owned();var after=(Array)await Pump(mod,()=> (System.Threading.Tasks.Task)prep.GetType().GetMethod("ModelsAsync",F).Invoke(prep,new[]{(object)"candidate",own2}));double afterMs=Ms(Stopwatch.GetTimestamp()-start);
 if(args.Contains("-capture")){if(!Bytes((Array)na[5],232).SequenceEqual(Bytes((Array)ga[4],232)))throw new Exception("capture differs");}else if(((Array)na[5]).Length!=0)throw new Exception("Unexpected bound readback");
 if(before.Length!=after.Length)throw new Exception("group count");
 foreach(var attr in new[]{UnityEngine.Rendering.VertexAttribute.Position,UnityEngine.Rendering.VertexAttribute.Normal,UnityEngine.Rendering.VertexAttribute.Tangent,UnityEngine.Rendering.VertexAttribute.TexCoord0,UnityEngine.Rendering.VertexAttribute.Color,UnityEngine.Rendering.VertexAttribute.BlendIndices})
 for(int g=0;g<before.Length;g++){var x=Channel(before.GetValue(g),attr);var y=Channel(after.GetValue(g),attr);if(!x.SequenceEqual(y))throw new Exception("channel bytes differ "+attr+" group="+g+" lengths="+x.Length+"/"+y.Length);}
 for(int g=0;g<before.Length;g++){
 var bm=before.GetValue(g);var am=after.GetValue(g);
 object Member(object x,string n)=>mt.GetProperty(n,F)?.GetValue(x) ?? mt.GetField(n,F).GetValue(x);
 var bi=Member(bm,"indices");var ai=Member(am,"indices");
 var bx=(int[])bi.GetType().GetMethod("ToArray",Type.EmptyTypes).Invoke(bi,null);var ax=(int[])ai.GetType().GetMethod("ToArray",Type.EmptyTypes).Invoke(ai,null);if(!bx.SequenceEqual(ax))throw new Exception("indices differ");
 var sm=(UnityEngine.Rendering.SubMeshDescriptor[])Member(am,"subMeshes");
 var positions=Channel(am,UnityEngine.Rendering.VertexAttribute.Position);var pv=new float[positions.Length/4];Buffer.BlockCopy(positions,0,pv,0,positions.Length);
 for(int k=0;k<pv.Length;k+=3){var v=new Vector3(pv[k],pv[k+1],pv[k+2]);var b=sm[0].bounds;var epsilon=Vector3.one*0.0001f;b.Expand(epsilon);if(!b.Contains(v))throw new Exception("bounds excludes vertex");}
 }
 report+="streamBytes="+((byte[])prep.GetType().GetField("Bytes",F).GetValue(prep)).Length+"; captureBound="+((Array)na[5]).Length+"\n";
 foreach(IDisposable m in (IEnumerable)own)m.Dispose();foreach(IDisposable m in (IEnumerable)own2)m.Dispose();
 oldTimes.Add(beforeMs);newTimes.Add(afterMs);report+="round="+run+" oldMs="+beforeMs+" newMs="+afterMs+" exactChannels groups="+before.Length+"\n";
 File.WriteAllText(output,report);
 }
 var invalid=(byte[])raw.Clone();Buffer.BlockCopy(BitConverter.GetBytes(float.NaN),0,invalid,0,4);var broken=ArrayOf(tri,invalid,228);
 object[] fault={broken,tree,bones,surfaces,null,null,null,false,false,oc,fc,null,null};if((bool)current.Invoke(null,fault)||fault[4]!=null||string.IsNullOrEmpty((string)fault[6]))throw new Exception("corrupt vertex reported success");
 report+="PASS invalid vertex fails with diagnostic and no prepared output.\n";
 report+="PASS asynchronous real captured input all six native attribute streams exact versus existing GPU+CPU grouping. Includes GPU input prep/clip/encoding/checks/transfers/grouping/native model creation. New -frame path includes GPU input mapping/InFrame; old path starts with previously CPU-transformed input. Both exclude downstream native asset save.\n";File.WriteAllText(output,report);EditorApplication.Exit(0);
 }catch(Exception e){UnityEngine.Debug.LogException(e);var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-report");if(i>=0)File.AppendAllText(a[i+1],"\nFAIL "+e);EditorApplication.Exit(1);}}
}
