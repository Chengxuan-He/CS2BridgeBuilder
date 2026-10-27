using System.Reflection;
using System.Text;
using System.Text.Json;
using UnityEngine;

const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(args[1],new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var dll=Assembly.LoadFrom(Path.GetFullPath(args[0]));
object Field(object o,string n)=>o.GetType().GetField(n,F)!.GetValue(o)!;
Type Type(string n)=>dll.GetType("BridgeBuilder.Bridges."+n,true)!;
float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
float[] U(Vector2 p)=>new[]{p.x,p.y};
using var reader=new BinaryReader(File.OpenRead(args[2]));
if(reader.ReadInt32()!=2)throw new Exception("Expected fixture v2");
int count=reader.ReadInt32();var results=new List<object>();
for(int ci=0;ci<count;ci++)
{
 string Text()=>Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
 string name=Text(),implementation=Text();
 int family=reader.ReadInt32(),lod=reader.ReadInt32(),mesh=reader.ReadInt32(),nv=reader.ReadInt32(),ni=reader.ReadInt32();
 var source=new Vector3[nv];var uv=new Vector2[nv];var indices=new int[ni];
 for(int i=0;i<nv;i++)source[i]=new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
 for(int i=0;i<nv;i++)uv[i]=new(reader.ReadSingle(),reader.ReadSingle());
 for(int i=0;i<ni;i++)indices[i]=reader.ReadInt32();
 if(name!="LiftBridge04 Mesh"||mesh!=0)continue;
 var recipe=Type("MovableUvDomains").GetMethod("For",F)!.Invoke(null,new object[]{name,mesh})!;
 var domains=(Array)Field(recipe,"Items");
 var method=Type(implementation).GetMethod("Create",F)!;
 foreach(float delta in new[]{-1.25f,2f,8f,24f})
 {
  var plan=method.Invoke(null,method.GetParameters().Select(p=>p.ParameterType==typeof(Vector3[])?(object)source.Clone():p.ParameterType==typeof(int[])?indices:p.ParameterType==typeof(float)?(object)delta:p.Name=="lod"?(object)lod:throw new Exception(p.Name)).ToArray())!;
  var target=(Vector3[])Field(plan,"Positions");
  foreach(int id in new[]{3,28})
  {
   var domain=domains.GetValue(id)!;var abc=new[]{(int)Field(domain,"A"),(int)Field(domain,"B"),(int)Field(domain,"C")};
   var faces=((int[])Field(domain,"_data")).Skip((int)Field(domain,"_start")).Take((int)Field(domain,"_count")).ToArray();var bounds=(Vector4)Field(domain,"Bounds");
   var frame=Activator.CreateInstance(Type("MovableUvFrame"),F,null,new object[]{source[abc[0]],source[abc[1]],source[abc[2]],uv[abc[0]],uv[abc[1]],uv[abc[2]],target[abc[0]],target[abc[1]],target[abc[2]]},null)!;
   var ids=faces.SelectMany(f=>indices.Skip(f*3).Take(3)).Distinct().ToArray();
   var rows=ids.Select(i=>{
    var mapped=(Vector2)Type("MovableUvFrame").GetMethod("Map",F)!.Invoke(frame,new object[]{source[i],target[i],uv[i]})!;
    return new{index=i,source=V(source[i]),target=V(target[i]),sourceUv=U(uv[i]),mappedUv=U(mapped),normalizedUv=new[]{(mapped.x-bounds.x)/(bounds.z-bounds.x),(mapped.y-bounds.y)/(bounds.w-bounds.y)}};
   }).ToArray();
   results.Add(new{name,implementation,family,lod,mesh,delta,domain=id,frameIndices=abc,sourceFaces=faces,indices=faces.Select(f=>indices.Skip(f*3).Take(3).ToArray()).ToArray(),bounds=new[]{bounds.x,bounds.y,bounds.z,bounds.w},vertices=rows});
  }
 }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(new{results},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Captured {results.Count} production frame cases.");
