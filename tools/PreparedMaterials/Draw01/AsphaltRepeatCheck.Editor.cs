using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

// Uses actual owner triangles, the candidate's production Apply/Compose, and native mesh upload.
public static class Draw01AsphaltRepeatCheck
{
 const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object F(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
 static void S(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,Flags,null,a,null);
 static string Arg(string n){var a=Environment.GetCommandLineArgs();return a[Array.IndexOf(a,n)+1];}
 static Array Arr(Type t,IEnumerable<object> v){var a=v.ToArray();var r=Array.CreateInstance(t,a.Length);for(int i=0;i<a.Length;i++)r.SetValue(a[i],i);return r;}
 public static void Run(){try{
  string dll=Arg("-candidate"),managed=Arg("-gameManaged");using var sha=SHA256.Create();string hash=string.Concat(sha.ComputeHash(File.ReadAllBytes(dll)).Select(b=>b.ToString("X2")));
  if(hash!=Arg("-candidateHash"))throw new Exception("Candidate hash changed");
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{foreach(var dir in new[]{Path.GetDirectoryName(dll),managed}){var p=Path.Combine(dir,new AssemblyName(e.Name).Name+".dll");if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
  var dbType=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll")).GetType("Colossal.IO.AssetDatabase.AssetDatabase",true);var db=FormatterServices.GetUninitializedObject(dbType);var databases=dbType.GetField("m_Databases",Flags);databases.SetValue(db,Activator.CreateInstance(databases.FieldType));dbType.GetField("s_Instance",Flags).SetValue(null,db);
  var asm=Assembly.LoadFrom(dll);Type T(string n)=>asm.GetType("BridgeBuilder.Bridges."+n,true);
  var surfaceType=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll")).GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
  var surface=FormatterServices.GetUninitializedObject(surfaceType);
  const string key="87b16f614d85671f0ad2e65f113b218460b330780cbb8d6003a089d600139a16";
  using(var r=new BinaryReader(File.OpenRead(Path.Combine(Arg("-assetRoot"),"Materials/Draw01/RepeatUnits/"+key+"/Repeat.Surface"))))surfaceType.GetMethod("ReadHeader",Flags).Invoke(surface,new object[]{r});
  var catalogue=asm.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);catalogue.GetMethod("Initialize",Flags).Invoke(null,new object[]{Arg("-assetRoot")});
  ((System.Collections.IDictionary)catalogue.GetField("Surfaces",Flags).GetValue(null))["Draw01/RepeatUnits/"+key+"/Repeat.Surface"]=surface;
  var patchType=T("MovableExistingRoadSurface+Patch");var appearance=T("MovableRoadAppearance+Type");var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var bound=T("MovableRoadGeometry+BoundTriangle");var inputType=T("MovableSurfaceAtlas+Input");
  var apply=T("Draw01AsphaltRepeat").GetMethod("Apply",Flags);var compose=T("MovableSurfaceAtlas").GetMethod("Compose",Flags);
  int checks=0,poseChecks=0,scenarios=0,totalTriangles=0;double maxError=0;
  using var reader=new BinaryReader(File.OpenRead(Arg("-fixture")));int levels=reader.ReadInt32();
  for(int lod=0;lod<levels;lod++){
   int count=reader.ReadInt32();var records=new (int slot,Vector3[] p,Vector4[] uv)[count];
   for(int i=0;i<count;i++){int slot=reader.ReadInt32();var p=new Vector3[3];var uv=new Vector4[3];for(int j=0;j<3;j++){p[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());uv[j]=new Vector4(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());}records[i]=(slot,p,uv);}totalTriangles+=count;
   foreach(float width in new[]{1f,1.7f})foreach(var pose in new[]{Pose.identity}){
    var original=Arr(patchType,Enumerable.Range(0,8).Select(i=>New(patchType,Vector2.zero,Vector2.one)));
    var types=Arr(appearance,Enumerable.Range(0,8).Select(i=>Enum.ToObject(appearance,i==7?2:1)));
    var patches=(Array)apply.Invoke(null,new object[]{original,types,pose});
    if(!patches.GetValue(7).Equals(original.GetValue(7)))throw new Exception("Non-asphalt patch changed");
    for(int i=0;i<7;i++){var p=patches.GetValue(i);if(!(bool)F(p,"PreparedUnit")||!(bool)F(p,"RepeatU")||!(bool)F(p,"RepeatV")||!ReferenceEquals(F(p,"Surface"),surface))throw new Exception("Asphalt domain bypass");if(!F(p,"U").Equals(F(patches.GetValue(0),"U"))||!F(p,"V").Equals(F(patches.GetValue(0),"V")))throw new Exception("Domain phase reset");}
    var tiltedPose=new Pose(new Vector3(2,3,-4),Quaternion.Euler(0,37,0));
    var tiltedPatches=(Array)apply.Invoke(null,new object[]{original,types,tiltedPose});
    var projected=T("MovableRoadProjection").GetMethod("Prepared",Flags);
    var faces=new List<object>();var inverse=Quaternion.Inverse(pose.rotation);
    for(int i=0;i<count;i++){var record=records[i];var v=new object[3];for(int j=0;j<3;j++){var q=record.p[j];q.x*=width;var p=inverse*(q-pose.position);v[j]=New(vertex,p,inverse*Vector3.up,new Vector4(1,0,0,1),record.uv[j],Color.white);}faces.Add(New(bound,New(triangle,v[0],v[1],v[2],0,record.slot,i),(uint)record.slot));}
    var input=New(inputType,false);S(input,"Faces",Arr(bound,faces));S(input,"Materials",Arr(surfaceType,new[]{surface}));S(input,"Native",surface);S(input,"Patches",patches);S(input,"SupportedTypes",types);
    var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
    var output=(Array)((Array)compose.Invoke(null,new object[]{Arr(inputType,new[]{input}),owner,paint,null})).GetValue(0);
    if(output.Length!=count)throw new Exception("Compose changed topology");
    var positions=new List<Vector3>();var uvs=new List<Vector4>();
    for(int i=0;i<count;i++)foreach(var corner in new[]{"A","B","C"}){
     var before=F(F(faces[i],"Geometry"),corner);var after=F(output.GetValue(i),corner);
     foreach(var field in new[]{"Position","Normal","Color"})if(!F(before,field).Equals(F(after,field)))throw new Exception(field+" changed");
     var p=(Vector3)F(after,"Position");var objectP=pose.position+pose.rotation*p;var uv=(Vector4)F(after,"UV");var prior=(Vector4)F(before,"UV");if(uv.z!=prior.z||uv.w!=prior.w)throw new Exception("UV zw changed");
     double u=objectP.x*(769.5625/3.36602783203125/512)+(820.5-128)/512;
     double v=objectP.z/4-73.9609375/4;
     double error=Math.Max(Math.Abs(uv.x-u)*512,Math.Abs(uv.y-v)*732);maxError=Math.Max(maxError,error);if(error>.02)throw new Exception("Physical density/phase differs: "+error);
     var tiltedPosition=Quaternion.Inverse(tiltedPose.rotation)*(objectP-tiltedPose.position);
     var tiltedVertex=New(vertex,tiltedPosition,Quaternion.Inverse(tiltedPose.rotation)*Vector3.up,new Vector4(1,0,0,1),prior,Color.white);
     var tilted=projected.Invoke(null,new[]{tiltedVertex,tiltedPatches.GetValue(records[i].slot)});
     var tiltedUv=(Vector4)F(tilted,"UV");double pe=Math.Max(Math.Abs(tiltedUv.x-u)*512,Math.Abs(tiltedUv.y-v)*732);
     maxError=Math.Max(maxError,pe);if(pe>.02)throw new Exception("Posed projection density/phase differs: "+pe);poseChecks++;
     var tangent=(Vector4)F(after,"Tangent");var pt=(Vector4)F(tilted,"Tangent");var td=Quaternion.Inverse(tiltedPose.rotation)*Vector3.right;
     if((new Vector3(tangent.x,tangent.y,tangent.z)-Vector3.right).sqrMagnitude>1e-10f||tangent.w!=-1||
        (new Vector3(pt.x,pt.y,pt.z)-td).sqrMagnitude>1e-10f||pt.w!=-1)throw new Exception("Normal tangent frame differs");
     positions.Add(p);uvs.Add(uv);checks++;
    }
    var mesh=new Mesh();try{mesh.SetVertices(positions);mesh.SetUVs(0,uvs);mesh.SetTriangles(Enumerable.Range(0,positions.Count).ToArray(),0);mesh.UploadMeshData(false);var read=new List<Vector4>();mesh.GetUVs(0,read);if(!read.SequenceEqual(uvs)||!mesh.vertices.SequenceEqual(positions))throw new Exception("Native upload differs");}finally{UnityEngine.Object.DestroyImmediate(mesh);}scenarios++;
   }
  }
  File.WriteAllText(Arg("-report"),"{\"status\":\"PASS\",\"candidateSha256\":\""+hash+"\",\"lods\":"+levels+",\"actualOwnerTriangles\":"+totalTriangles+",\"scenarios\":"+scenarios+",\"vertexChecks\":"+checks+",\"posedPreparedChecks\":"+poseChecks+",\"maxSourcePixelFloat32Error\":"+maxError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"actualApplyComposeAndMeshUpload\":true,\"nativeVisualAcceptance\":false}");Debug.Log("PASS Draw01 actual asphalt repeat: "+checks+" vertices");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
