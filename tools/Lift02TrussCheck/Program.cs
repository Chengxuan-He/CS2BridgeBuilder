using System.Reflection;
using System.Text.Json;
using UnityEngine;
const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(args[1],new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true)!;
object Call(string n,string m,params object[] a)=>T(n).GetMethod(m,F)!.Invoke(null,a)!;
V Field<V>(object p,string n)=>(V)p.GetType().GetField(n,F)!.GetValue(p)!;
using var data=new BinaryReader(File.OpenRead(args[3]));
data.ReadInt32();data.ReadInt32();int ni=data.ReadInt32();for(int i=0;i<3;i++)data.ReadInt32();int nf=data.ReadInt32();data.ReadInt32();
var ints=Enumerable.Range(0,ni).Select(_=>data.ReadInt32()).ToArray();var spans=Enumerable.Range(0,nf).Select(_=>data.ReadSingle()).ToArray();var results=new List<object>();
for(int lod=0;lod<3;lod++){
 using var input=new BinaryReader(File.OpenRead(Path.Combine(args[2],$"lift02-widthcheck-{lod}.bin")));int count=input.ReadInt32(),ic=input.ReadInt32();var source=new Vector3[count];for(int i=0;i<count;i++)source[i]=new(input.ReadSingle(),input.ReadSingle(),input.ReadSingle());var indices=Enumerable.Range(0,ic).Select(_=>input.ReadInt32()).ToArray();
 foreach(float delta in new[]{-20f,-8f,0f,2f,8.5f,24f}.Concat(spans.Select(h=>-h)).Distinct()){
 var parent=Call("Lift02StructureWidthPlan","Create",lod,source,indices,delta);
 if(lod==2){Call("Lift02FarRoadTops","Remove",indices,Field<bool[][]>(parent,"RemoveTriangles")[0]);parent=Call("Lift02FarRoadBoundary","Apply",parent);}else Call("Lift02TrackRemoval","RemoveDetailed",lod,indices,Field<bool[][]>(parent,"RemoveTriangles")[0]);
 var actual=Call("Lift02StructurePlan","Create",lod,source,indices,delta);var before=Field<Vector3[]>(parent,"Positions");var after=Field<Vector3[]>(actual,"Positions");var frames=Field<KeyValuePair<int,Matrix4x4>[]>(actual,"FrameTransforms").ToDictionary(x=>x.Key,x=>x.Value);var selected=new HashSet<int>();int at=3*spans.Length+ints.Take(lod*spans.Length).Sum();
 for(int g=0;g<spans.Length;g++){float k=(spans[g]+delta)/spans[g];for(int n=0;n<ints[lod*spans.Length+g];n++){int i=ints[at++];selected.Add(i);if(BitConverter.SingleToInt32Bits(after[i].x)!=BitConverter.SingleToInt32Bits(source[i].x*k)||BitConverter.SingleToInt32Bits(frames[i].m00)!=BitConverter.SingleToInt32Bits(k))throw new Exception("Central map/frame mismatch");}}
 for(int i=0;i<count;i++)if(after[i].y!=before[i].y||after[i].z!=before[i].z||(!selected.Contains(i)&&after[i].x!=before[i].x))throw new Exception("Unselected or YZ changed");
 if(!Field<int[][]>(actual,"SourceIndices")[0].SequenceEqual(indices)||!Field<bool[][]>(actual,"RemoveTriangles")[0].SequenceEqual(Field<bool[][]>(parent,"RemoveTriangles")[0]))throw new Exception("Topology changed");
 results.Add(new{lod,delta,vertices=count,central=selected.Count,frameCount=frames.Count,pass=true});
 }
}
File.WriteAllText(args[4],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS {results.Count} actual production plan cases");
