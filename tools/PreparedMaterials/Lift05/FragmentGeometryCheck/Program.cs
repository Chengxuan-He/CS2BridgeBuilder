using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using UnityEngine;
const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
var managed=Path.GetFullPath(args[1]);AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,flags)!.GetValue(o)!;
float[] P(Vector3 p)=>new[]{p.x,p.y,p.z};float[] U(Vector2 p)=>new[]{p.x,p.y};
using var reader=new BinaryReader(File.OpenRead(args[2]));if(reader.ReadInt32()!=2)throw new Exception("Fixture version");int count=reader.ReadInt32();
var reports=new List<object>();
for(int entry=0;entry<count;entry++)
{
 string Text()=>Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
 string name=Text(),implementation=Text();int family=reader.ReadInt32(),lod=reader.ReadInt32(),mesh=reader.ReadInt32(),n=reader.ReadInt32(),ni=reader.ReadInt32();
 var source=new Vector3[n];var uv=new Vector2[n];var indices=new int[ni];
 for(int i=0;i<n;i++)source[i]=new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
 for(int i=0;i<n;i++)uv[i]=new(reader.ReadSingle(),reader.ReadSingle());for(int i=0;i<ni;i++)indices[i]=reader.ReadInt32();
 if(name!="LiftBridge05_LOD2 Mesh"||mesh!=0)continue;
 var recipe=mod.GetType("BridgeBuilder.Bridges.MovableUvDomains")!.GetMethod("For",flags)!.Invoke(null,new object[]{name,mesh})!;
 var domain=Field<Array>(recipe,"Items").GetValue(83)!;var anchors=new[]{Field<int>(domain,"A"),Field<int>(domain,"B"),Field<int>(domain,"C")};
 foreach(float delta in new[]{-1.25f,-.25f,0,2,8,24})
 {
  object Plan(string type)=>mod.GetType("BridgeBuilder.Bridges."+type)!.GetMethod("Create",flags)!.Invoke(null,new object[]{lod,(Vector3[])source.Clone(),indices,delta})!;
  var plan=Plan("Lift05StructurePlan");var widthPlan=Plan("Lift05StructureWidthPlan");var target=Field<Vector3[]>(plan,"Positions");var src=Field<Vector3[]>(plan,"SourcePositions");
  var construct=plan.GetType().GetMethod("Construction",flags)!;
  object C(int i)=>construct.Invoke(plan,new object[]{i-src.Length})!;
  Vector3 Source(int i){if(i<src.Length)return src[i];var c=C(i);int a=Field<int>(c,"A"),b=Field<int>(c,"B"),cc=Field<int>(c,"C");return src[a]+(src[b]-src[a])*Field<float>(c,"WeightB")+(src[cc]-src[a])*Field<float>(c,"WeightC");}
  Vector2 UV(int i){if(i<src.Length)return uv[i];var c=C(i);int a=Field<int>(c,"A"),b=Field<int>(c,"B"),cc=Field<int>(c,"C");return UV(a)+(UV(b)-UV(a))*Field<float>(c,"WeightB")+(UV(cc)-UV(a))*Field<float>(c,"WeightC");}
  var fragment=Field<Array>(plan,"Fragments").Cast<object>().Single(f=>Field<int>(f,"Triangle")==85);var ids=Field<int[]>(fragment,"Indices").Take(3).ToArray();
  var frame=mod.GetType("BridgeBuilder.Bridges.MovableUvFrame")!;
  int pivot=(int)frame.GetMethod("TrianglePivot",flags)!.Invoke(null,ids.Select(i=>(object)Source(i)).ToArray())!;
  double[] D(Vector3 a,Vector3 b)=>Enumerable.Range(0,3).Select(i=>(double)a[i]-b[i]).ToArray();
  double Dot(double[] a,double[] b)=>a.Zip(b,(x,y)=>x*y).Sum();
  double[] Cross(double[] a,double[] b)=>new[]{a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};
  double[] Norm(double[] a)=>a.Select(v=>v/Math.Sqrt(Dot(a,a))).ToArray();
  var order=new[]{pivot,(pivot+1)%3,(pivot+2)%3};
  var se=D(Source(ids[order[1]]),Source(ids[order[0]]));var so=D(Source(ids[order[2]]),Source(ids[order[0]]));
  var te=D(target[ids[order[1]]],target[ids[order[0]]]);var to=D(target[ids[order[2]]],target[ids[order[0]]]);
  double sl=Math.Sqrt(Dot(se,se)),sx=Dot(so,se)/sl,sy=Math.Sqrt(Dot(Cross(se,so),Cross(se,so)))/sl;
  var tx=Norm(te);var ty=Cross(Norm(Cross(te,to)),tx);
  object[] rotationArgs={sl,sx,sy,Math.Sqrt(Dot(te,te)),Dot(to,tx),Dot(to,ty),0d,0d};
  frame.GetMethod("PolarRotation",flags)!.Invoke(null,rotationArgs);double cosine=(double)rotationArgs[6],sine=(double)rotationArgs[7];
  var sampleX=tx.Zip(ty,(x,y)=>x*cosine+y*sine).ToArray();var sampleY=ty.Zip(tx,(y,x)=>y*cosine-x*sine).ToArray();
  var projection=mod.GetType("BridgeBuilder.Bridges.MovableParentUv")!.GetMethod("Map",flags)!.Invoke(null,new object[]{ids.Select(Source).ToArray(),ids.Select(UV).ToArray(),ids.Select(i=>target[i]).ToArray(),anchors.Select(Source).ToArray(),anchors.Select(UV).ToArray(),Field<bool>(domain,"Triangle"),Field<Vector4>(domain,"Bounds")})!;
  reports.Add(new{name,implementation,lod,mesh,delta,domain=83,sourceFace=85,fragmentIndex=0,anchors,pivot,
   projectionBasis=new{order,sourceEdge=se,sourceOther=so,sourceLength=sl,sourceCx=sx,sourceCy=sy,targetEdge=te,targetOther=to,targetX=tx,targetY=ty,cosine,sine,sampleX,sampleY},
   parent=anchors.Select(i=>new{id=i,source=P(Source(i)),target=P(target[i]),uv=U(UV(i))}),
   corners=ids.Select(i=>new{id=i,source=P(Source(i)),target=P(target[i]),displacement=P(target[i]-Source(i)),uv=U(UV(i)),construction=i<src.Length?null:C(i).GetType().GetFields(flags).ToDictionary(f=>f.Name,f=>f.GetValue(C(i)))}),
   widthStageFragment=Field<Array>(widthPlan,"Fragments").Cast<object>().Where(f=>Field<int>(f,"Triangle")==85).Select(f=>Field<int[]>(f,"Indices")),
   widthStageOriginalParent=anchors.Select(i=>P(Field<Vector3[]>(widthPlan,"Positions")[i])),
   exactProjection=Field<double[][]>(projection,"Exact"),offsetX=Field<double>(projection,"OffsetX"),offsetY=Field<double>(projection,"OffsetY")});
 }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(new{assemblySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))),fixtureSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[2]))),reports},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Exported {reports.Count} exact fragment geometry records.");
