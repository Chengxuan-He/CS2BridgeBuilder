using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
public static class Lift02PreparedConsumerCheck
{
 public static void Run()
 {
  var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
  var managed=Path.Combine(Arg("-gameData"),"Managed");
  AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
  var mod=Assembly.LoadFrom(Arg("-candidate"));const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
  var method=mod.GetType("BridgeBuilder.Bridges.Lift02BedAppearance",true).GetMethod("Prepared",flags);
  var surface=FormatterServices.GetUninitializedObject(method.GetParameters()[0].ParameterType);
  T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,flags).GetValue(o);
  void Need(bool yes,string why){if(!yes)throw new Exception(why);}
  Type TypeOf(string name)=>mod.GetType("BridgeBuilder.Bridges."+name,true);
  object New(Type type,params object[] values)=>Activator.CreateInstance(type,flags,null,values,null);
  Array Items(Type type,params object[] values){var array=Array.CreateInstance(type,values.Length);for(int i=0;i<values.Length;i++)array.SetValue(values[i],i);return array;}
  void Set(object item,string name,object value)=>item.GetType().GetField(name,flags).SetValue(item,value);
  using(var header=new BinaryReader(File.OpenRead(Arg("-surfaceFile"))))surface.GetType().GetMethod("ReadHeader",flags).Invoke(surface,new object[]{header});
  var vertexType=TypeOf("MovableRoadGeometry+Vertex");var triangleType=TypeOf("MovableRoadGeometry+Triangle");var boundType=TypeOf("MovableRoadGeometry+BoundTriangle");var inputType=TypeOf("MovableSurfaceAtlas+Input");
  var owner=FormatterServices.GetUninitializedObject(TypeOf("MovableRoadMaterial"));var paint=Array.CreateInstance(TypeOf("MovableNativeDecals+Region"),0);
  int vertices=0,composeCases=0;
  foreach(float delta in new[]{0f,2f,8f})
  {
   var result=method.Invoke(null,new object[]{surface,new Pose(Vector3.zero,Quaternion.identity),delta});
   var patches=Field<Array>(result,"Patches");Need(patches.Length==2,"Physical side domains");Need(ReferenceEquals(Field<object>(result,"Native"),surface),"Native finished Surface");
   float half=4.2095947265625f+delta;
   for(int side=0;side<2;side++)
   {
    var patch=patches.GetValue(side);float direction=side==0?1:-1;
    Need(Field<bool>(patch,"PreparedUnit")&&!Field<bool>(patch,"SourceAtlasRepeat")&&!Field<bool>(patch,"SourceUv"),"Continuous prepared route");
    Need(Field<bool>(patch,"RepeatU")&&!Field<bool>(patch,"RepeatV"),"Independent axes");
    var u=Field<Vector4>(patch,"U");var v=Field<Vector4>(patch,"V");
    var tangent=(Vector4)patch.GetType().GetField("Tangent",flags).GetValue(patch);
    Need(tangent==new Vector4(0,0,-1,-direction),"Measured native side frame");
    foreach(float z in new[]{-106f,-82f,-2f,0f,2f,82f,106f})foreach(float t in new[]{0f,.5f,1f})
    {
     // Native source evidence: outer x on this side is minV; inner boundary is maxV.
     var p=new Vector4(-direction*half*(1-t),0,z,1);float actualU=Vector4.Dot(u,p),actualV=Vector4.Dot(v,p);
     Need(Mathf.Abs(actualU-(.5f-z/4))<.000001f,"Original 4m phase, no frac");
     Need(Mathf.Abs(actualV-(.368896484375f+t*(.43505859375f-.368896484375f)))<.000001f,"Finite orientation and content bounds");vertices++;
    }
    foreach(float length in new[]{4f,212f,1000f})
    {
     object Vertex(float x,float z)=>New(vertexType,new Vector3(x,0,z),Vector3.up,new Vector4(1,0,0,1),new Vector4(3,5,7,9),new Color(.2f,.3f,.4f,.7f));
     var face=New(triangleType,Vertex(-direction*half,-length/2),Vertex(0,-length/2),Vertex(0,length/2),0,5,23);
     var input=New(inputType,false);Set(input,"Faces",Items(boundType,New(boundType,face,0u)));Set(input,"Materials",Items(surface.GetType(),surface));Set(input,"Native",surface);Set(input,"Patches",Items(patch.GetType(),patch));
     object[] call={Items(inputType,input),owner,paint,null};
     var outputs=(Array)TypeOf("MovableSurfaceAtlas").GetMethod("Compose",flags).Invoke(null,call);
     var faces=(Array)outputs.GetValue(0);Need(faces.Length==1,"Period count added triangle cuts");
     var mapped=faces.GetValue(0);var slots=(Array)((Array)call[3]).GetValue(0);Need(ReferenceEquals(slots.GetValue(0),surface),"Prepared Surface binding changed");
     foreach(string corner in new[]{"A","B","C"})
     {
      var sourceVertex=Field<object>(face,corner);var outputVertex=Field<object>(mapped,corner);var position=Field<Vector3>(outputVertex,"Position");var uv=Field<Vector4>(outputVertex,"UV");var point=new Vector4(position.x,position.y,position.z,1);
      Need(position==Field<Vector3>(sourceVertex,"Position"),"Receiver shape");Need(uv.z==7&&uv.w==9,"UV zw");Need(Mathf.Abs(uv.x-Vector4.Dot(u,point))<.000001f&&Mathf.Abs(uv.y-Vector4.Dot(v,point))<.000001f,"Continuous UV phase");Need(Field<Vector4>(outputVertex,"Tangent")==tangent,"Compose native tangent");Need(Field<Vector3>(outputVertex,"Normal")==Vector3.up&&Field<Color>(outputVertex,"Color")==new Color(.2f,.3f,.4f,.7f),"Original attributes");
     }
     Need(Field<int>(mapped,"Occurrence")==5&&Field<int>(mapped,"SourceTriangle")==23,"Provenance");composeCases++;
    }
   }
  }
  File.WriteAllText(Arg("-report"),"{\"passed\":true,\"sampledVertices\":"+vertices+",\"composeCases\":"+composeCases+",\"scope\":\"Actual Prepared callback, native Unity matrices and measured side/width/phase/frame assertions. Actual Compose preserves one face across 1/53/250 periods; native Surface header loaded. Full native database/rendering and cold load not covered.\"}");
  Debug.Log("PASS Lift02 actual prepared callback: "+vertices+" source-oriented points.");
 }
}
