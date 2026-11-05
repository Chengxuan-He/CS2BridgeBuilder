using System.Reflection;
using System.Text;
using System.Text.Json;
using UnityEngine;
const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>Assembly.LoadFrom(Path.Combine(args[1],new AssemblyName(e.Name).Name+".dll"));
var dll=Assembly.LoadFrom(args[0]);
using var reader=new BinaryReader(File.OpenRead(args[2]));
if(reader.ReadInt32()!=2)throw new Exception("fixture schema");
int count=reader.ReadInt32();var rows=new List<object>();
for(int ci=0;ci<count;ci++){
 string Text()=>Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
 string name=Text(),implementation=Text();
 int family=reader.ReadInt32(),lod=reader.ReadInt32(),mesh=reader.ReadInt32(),nv=reader.ReadInt32(),ni=reader.ReadInt32();
 var source=new Vector3[nv];var uv=new Vector2[nv];var indices=new int[ni];
 for(int i=0;i<nv;i++)source[i]=new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
 for(int i=0;i<nv;i++)uv[i]=new(reader.ReadSingle(),reader.ReadSingle());
 for(int i=0;i<ni;i++)indices[i]=reader.ReadInt32();
 if(!name.StartsWith("LiftBridge04")||name.Contains("Pillar")||mesh!=0)continue;
 var type=dll.GetType("BridgeBuilder.Bridges."+implementation,true)!;var method=type.GetMethod("Create",F)!;
 foreach(float delta in new[]{-4f,-1f,0f,2f,10.25f,24f}){
 var plan=method.Invoke(null,method.GetParameters().Select(p=>p.ParameterType==typeof(Vector3[])?(object)source.Clone():p.ParameterType==typeof(int[])?indices:p.ParameterType==typeof(float)?(object)delta:p.Name=="lod"?(object)lod:throw new Exception(p.Name)).ToArray())!;
 var target=(Vector3[])plan.GetType().GetField("Positions",F)!.GetValue(plan)!;
 using var measured=JsonDocument.Parse(File.ReadAllText(args[4]));
 var selected=measured.RootElement[lod].GetProperty("entries").EnumerateArray().Select(e=>e[0].GetInt32()).ToHashSet();
 int checkedFaces=0;
 var frameType=dll.GetType("BridgeBuilder.Bridges.MovableUvFrame",true)!;
 for(int face=0;face<indices.Length;face+=3){
 int a=indices[face],b=indices[face+1],c=indices[face+2];
 if(!selected.Contains(a)&&!selected.Contains(b)&&!selected.Contains(c))continue;
 if(!selected.Contains(a)||!selected.Contains(b)||!selected.Contains(c))throw new Exception("Partial rib face");
 var frame=Activator.CreateInstance(frameType,F,null,new object[]{source[a],source[b],source[c],uv[a],uv[b],uv[c],target[a],target[b],target[c]},null)!;
 foreach(int i in new[]{a,b,c}){
 var mapped=(Vector2)frameType.GetMethod("Map",F)!.Invoke(frame,new object[]{source[i],target[i],uv[i]})!;
 if((mapped-uv[i]).sqrMagnitude>1e-8f)throw new Exception("Rigid rib UV phase changed");
 }
 checkedFaces++;
 }
 rows.Add(new {name,lod,delta,checkedUvFaces=checkedFaces,positions=target.Select(p=>new[]{p.x,p.y,p.z}).ToArray()});
 }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(rows));Console.WriteLine($"Captured {rows.Count} actual plan cases");
