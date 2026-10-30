using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using UnityEngine;
if(args.Length!=3)throw new ArgumentException("candidate, game Managed, report");
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(args[1],new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true)!;
object New(Type type,params object?[] values)=>type.GetConstructors(F).Single(c=>c.GetParameters().Length==values.Length).Invoke(values);
V Get<V>(object o,string n)=>(V)o.GetType().GetField(n,F)!.GetValue(o)!;
void Need(bool v,string why){if(!v)throw new InvalidDataException(why);}
var vt=T("MovableRoadGeometry+Vertex");var tt=T("MovableRoadGeometry+Triangle");var pt=T("MovableExistingRoadSurface+Patch");
var ctor=pt.GetConstructors(F).Single(c=>c.GetParameters().Length>3);var surface=RuntimeHelpers.GetUninitializedObject(ctor.GetParameters().Single(p=>p.Name=="surface").ParameterType);
var run=T("MovableRoadAtlasWrap").GetMethod("Apply",F)!;
var records=new List<object>();
foreach(float delta in new[]{0f,2f,8f})foreach(int side in new[]{-1,1})
{
 float half=4.2095947265625f+delta;var min=new Vector2(.92041015625f,.046112060546875f);var max=new Vector2(.9990234375f,.05438232421875f);
 var tangent=new Vector4(0,0,-1,side);var u=new Vector4(0,0,-(max.x-min.x)/4,min.x+(max.x-min.x)*.5f);var v=new Vector4(-side*(max.y-min.y)/half,0,0,max.y);
 var values=ctor.GetParameters().Select(p=>p.Name switch {"min"=>(object)min,"max"=>max,"u"=>u,"v"=>v,"surface"=>surface,"sourceUv"=>true,"repeatU"=>true,"repeatV"=>false,"sourceAtlasRepeat"=>true,"tangent"=>tangent,_=>p.DefaultValue}).ToArray();var patch=ctor.Invoke(values);
 object Vertex(float x,float z)=>New(vt,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(0,0,x,z),new Color(x/half,z/212,0.4f,1));
 var a=Vertex(0,-106);var b=Vertex(side*half,-106);var c=Vertex(side*half,106);var d=Vertex(0,106);
 var sources=new[]{New(tt,a,b,c,7,11,13),New(tt,a,c,d,7,11,14)};
 int count=0;double area=0;float worst=0;
 foreach(var source in sources)foreach(var triangle in (Array)run.Invoke(null,new[]{source,patch})!)
 {
  count++;Need(Get<int>(triangle,"Material")==7&&Get<int>(triangle,"Occurrence")==11&&Get<int>(triangle,"SourceTriangle")==Get<int>(source,"SourceTriangle"),"provenance");
  var points=new List<Vector3>();
  foreach(var key in new[]{"A","B","C"})
  {
   var vertex=Get<object>(triangle,key);var p=Get<Vector3>(vertex,"Position");var uv=Get<Vector4>(vertex,"UV");var color=Get<Color>(vertex,"Color");points.Add(p);
   Need(Get<Vector4>(vertex,"Tangent").Equals(tangent),"native tangent");Need(Get<Vector3>(vertex,"Normal").Equals(Vector3.up),"normal");
   Need(Math.Abs(uv.z-p.x)<.00003&&Math.Abs(uv.w-p.z)<.00003,"UV zw interpolation");Need(Math.Abs(color.r-p.x/half)<.00001&&Math.Abs(color.g-p.z/212)<.00001&&Math.Abs(color.a-1)<.00001,"color interpolation");
   Need(uv.x>=min.x-.000001&&uv.x<=max.x+.000001,"repeat region");
   float expectedV=max.y-side*p.x/half*(max.y-min.y);worst=Math.Max(worst,Math.Abs(uv.y-expectedV));Need(worst<.000001,"finite V");
   float q=.5f-p.z/4,actual=(uv.x-min.x)/(max.x-min.x);Need(Math.Abs((q-actual)-MathF.Round(q-actual))<.00001,"phase");
  }
  area+=Vector3.Cross(points[1]-points[0],points[2]-points[0]).magnitude/2;
  Need(points.Max(p=>p.z)-points.Min(p=>p.z)<=4.00001,"only one period per face");
 }
 Need(Math.Abs(area-half*212)<.003,"shape area");records.Add(new{delta,side,triangles=count,area,finiteVMaxError=worst});
}
File.WriteAllText(args[2],JsonSerializer.Serialize(new{candidateSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))),cases=records,scope="Actual candidate wrap helper; measured Lift02 mapping; synthetic planar selected receivers. Not ForLod callback, bone upload, rendering or cold-load acceptance."},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("PASS actual Lift02 atlas wrap helper: six width/side cases.");
