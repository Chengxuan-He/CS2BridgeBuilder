using System.Reflection;
using System.Text.Json;
using UnityEngine;
var managed=Path.GetFullPath(args[1]);
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
var create=mod.GetType("BridgeBuilder.Bridges.PedestrianDraw02RenderAssembly")!.GetMethod("Structure",BindingFlags.Static|BindingFlags.NonPublic)!;
const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
T Field<T>(object p,string n)=>(T)p.GetType().GetField(n,flags)!.GetValue(p)!;
bool Equal32(float a,float b)=>BitConverter.ToInt32(BitConverter.GetBytes(a),0)==BitConverter.ToInt32(BitConverter.GetBytes(b),0);
using var input=JsonDocument.Parse(File.ReadAllText(args[2]));int cases=0,faceChecks=0,frameChecks=0;
foreach(var level in input.RootElement.EnumerateArray()){
 int lod=level.GetProperty("lod").GetInt32();
 var source=level.GetProperty("positions").EnumerateArray().Select(p=>new Vector3(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle())).ToArray();
 var indices=level.GetProperty("indices").EnumerateArray().Select(p=>p.GetInt32()).ToArray();
 var assembly=level.GetProperty("assembly").EnumerateArray().Select(p=>p.GetInt32()).ToHashSet();float h=level.GetProperty("halfspan").GetSingle();
 var upper=level.GetProperty("upper").EnumerateArray().Select(p=>p.GetInt32()).ToHashSet();float uh=level.GetProperty("upperHalfspan").GetSingle();
 foreach(float delta in new[]{-6f,-h,-uh,-1f,0f,2f,8f,24f}){
  var plan=create.Invoke(null,new object[]{lod,0,source,indices,delta})!;
  var target=Field<Vector3[]>(plan,"Positions");var removed=Field<bool[][]>(plan,"RemoveTriangles");var frames=Field<Array>(plan,"FrameTransforms");var scale32=new float[]{(h+delta)/h};float k=scale32[0];var uscale32=new float[]{(uh+delta)/uh};var selected=new HashSet<int>(assembly);selected.UnionWith(upper);var sf=new HashSet<int>();
  foreach(int i in assembly)if(!Equal32(target[i].x,new float[]{source[i].x*scale32[0]}[0])||target[i].y!=source[i].y||target[i].z!=source[i].z)throw new Exception("Assembly mapping mismatch");
  foreach(int i in upper)if(!Equal32(target[i].x,new float[]{source[i].x*uscale32[0]}[0])||target[i].y!=source[i].y||target[i].z!=source[i].z)throw new Exception($"Upper assembly mapping mismatch LOD{lod} delta{delta} vertex{i}");
  foreach(var member in level.GetProperty("members").EnumerateArray()){
   float c=member.GetProperty("centre").GetSingle();var ids=member.GetProperty("vertices").EnumerateArray().Select(p=>p.GetInt32()).ToArray();
   foreach(int i in ids){selected.Add(i);if(!Equal32(target[i].x,new float[]{source[i].x+c/4f*delta}[0])||target[i].y!=source[i].y||target[i].z!=source[i].z)throw new Exception("Rib mapping mismatch");}
   if(Math.Abs((ids.Max(i=>target[i].x)-ids.Min(i=>target[i].x))-(ids.Max(i=>source[i].x)-ids.Min(i=>source[i].x)))>4e-6f)throw new Exception("Rib widened");
   foreach(var x in member.GetProperty("faces").EnumerateArray())sf.Add(x.GetInt32());
  }
  foreach(var x in level.GetProperty("assemblyFaces").EnumerateArray())sf.Add(x.GetInt32());
  foreach(var x in level.GetProperty("upperFaces").EnumerateArray())sf.Add(x.GetInt32());
  foreach(int f in sf){if(removed[0][f])throw new Exception("Structural face removed");faceChecks++;}
  foreach(var f in Field<Array>(plan,"Fragments"))if(sf.Contains(Field<int>(f!,"Triangle")))throw new Exception("Structural face fragmented");
  var keys=new HashSet<int>();foreach(var p in frames){int i=(int)p!.GetType().GetProperty("Key")!.GetValue(p)!;var m=(Matrix4x4)p.GetType().GetProperty("Value")!.GetValue(p)!;if(!keys.Add(i)||!(assembly.Contains(i)||upper.Contains(i))||!Equal32(m.m00,upper.Contains(i)?uscale32[0]:scale32[0])||m.m11!=1||m.m22!=1)throw new Exception("Frame mismatch");frameChecks++;}
  var expectedFrames=new HashSet<int>(assembly);expectedFrames.UnionWith(upper);if(!keys.SetEquals(expectedFrames))throw new Exception("Incomplete frames");
  foreach(var e in Field<Array>(plan,"AddedVertices"))if(selected.Contains(Field<int>(e!,"A"))||selected.Contains(Field<int>(e!,"B")))throw new Exception("Selected construction edge not handled");
  cases++;
 }
}
Console.WriteLine($"PASS {cases} actual final plans; {faceChecks} complete source-face checks; {frameChecks} unique assembly Jacobians; rigid rib profiles; Y/Z preserved; no selected constructed edges.");
