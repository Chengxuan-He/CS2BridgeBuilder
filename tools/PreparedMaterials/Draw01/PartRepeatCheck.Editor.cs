using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

// Actual owner positions/UVs, controlled domain assignment across all 14 patches.
// Normals/tangents/color are synthetic. This does not execute game composition or partition binding.
public static class Draw01PartRepeatCheck
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
  var catalogue=asm.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);catalogue.GetMethod("Initialize",Flags).Invoke(null,new object[]{Arg("-assetRoot")});
  var keys=new[]{"87b16f614d85671f0ad2e65f113b218460b330780cbb8d6003a089d600139a16","2c21be8e861d7f307eafd18a7f6563365ad81e386bc5f76f80fcd4b70f866902","646ce47f8b33867ff5d17c39bd5f298a5f903bde264dfd658f2a7885d90e7acc","abd357e2483159049ec72564fef01ecb06fc1fdcc8dcad501a5506c65fa37e98"};
  var surfaces=keys.Select(key=>{var v=FormatterServices.GetUninitializedObject(surfaceType);using(var r=new BinaryReader(File.OpenRead(Path.Combine(Arg("-assetRoot"),"Materials/Draw01/RepeatUnits/"+key+"/Repeat.Surface"))))surfaceType.GetMethod("ReadHeader",Flags).Invoke(v,new object[]{r});((System.Collections.IDictionary)catalogue.GetField("Surfaces",Flags).GetValue(null))["Draw01/RepeatUnits/"+key+"/Repeat.Surface"]=v;return v;}).ToArray();var surface=surfaces[0];
  int Category(int i){int k=i%7;return k==0||k==5?0:k==1||k==4?1:k==2?2:3;}
  var px=new[]{512.0,492,512,640};var py=new[]{732.0,1828,768,768};
  var dx=new[]{769.5625/3.36602783203125/512,-1/2.1521285393334537,1/1.9158870967741934,1/2.39649297907489};
  var dz=new[]{.25,-1/8.0,-1/2.8729578864307217,-1/2.8729578864307217};
  var patchType=T("MovableExistingRoadSurface+Patch");var appearance=T("MovableRoadAppearance+Type");var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var bound=T("MovableRoadGeometry+BoundTriangle");var inputType=T("MovableSurfaceAtlas+Input");
  var apply=T("Draw01PartRepeats").GetMethod("Apply",Flags);var compose=T("MovableSurfaceAtlas").GetMethod("Compose",Flags);
  var regionMethod=T("Draw01RoadPartitions").GetMethod("MaterialRegions",Flags);var bind=T("MovableRoadGeometry").GetMethod("TryBind",Flags);int partitionChecks=0;
  for(int half=0;half<2;half++)for(int part=0;part<7;part++){
   bool sidewalk=part==1||part==2||part==4;float x=part==4||part==5||part==6?5:-5;float z=(part==2||part==3||part==6?85:40)*(half==0?1:-1);int expected=part+half*7;
   object V(float xx,float zz)=>New(vertex,new Vector3(xx,9.8f,zz),Vector3.up,new Vector4(1,0,0,1),new Vector4(.1f,.2f,.3f,.4f),Color.white);
   var tri=New(triangle,V(x,z),V(x+.1f,z),V(x,z+.1f),0,0,123);
   var region=regionMethod.Invoke(null,new object[]{sidewalk,Pose.identity});var args=new object[]{Arr(triangle,new[]{tri}),region,14,1,null,null,false,null,null};
   if(!(bool)bind.Invoke(null,args))throw new Exception("Partition failed "+args[5]);var result=(Array)args[4];if(result.Length!=1||Convert.ToUInt32(F(result.GetValue(0),"Bone"))!=expected)throw new Exception("Wrong physical part "+expected);partitionChecks++;
  }
  // Controlled input layouts exercise actual partition + Prepared projection. They
  // are not claimed to initialize native ECS or generate a new game owner.
  int layoutChecks=0;
  for(int lod=0;lod<3;lod++)foreach(var widths in new[]{new[]{0f,0f},new[]{2f,0f},new[]{0f,3f},new[]{1f,4f},new[]{4f,1f}}){
   var original=Arr(patchType,Enumerable.Range(0,14).Select(i=>New(patchType,Vector2.zero,Vector2.one)));
   var patches=(Array)apply.Invoke(null,new object[]{original,Pose.identity});int receiverCount=0;
   for(int side=0;side<2;side++)for(int leaf=0;leaf<2;leaf++){
    if(widths[side]==0)continue;
    float sign=side==0?-1:1,zsign=leaf==0?1:-1;
    float x0=sign*10.6f,x1=sign*(10.6f+widths[side]);
    object V(float x,float z)=>New(vertex,new Vector3(x,10,z),Vector3.up,new Vector4(1,0,0,1),Vector4.zero,Color.white);
    var tri=New(triangle,V(x0,zsign*37.9599609375f),V(x1,zsign*37.9599609375f),V(x0,zsign*41.9599609375f),0,0,0);
    var regions=regionMethod.Invoke(null,new object[]{true,Pose.identity});var ba=new object[]{Arr(triangle,new[]{tri}),regions,14,1,null,null,false,null,null};
    if(!(bool)bind.Invoke(null,ba))throw new Exception("Width layout partition failed");var assigned=(Array)ba[4];
    if(assigned.Length!=1)throw new Exception("Unexpected periodic geometry cut");
    int domain=Convert.ToInt32(F(assigned.GetValue(0),"Bone"));int expected=(side==0?1:4)+leaf*7;
    if(domain!=expected)throw new Exception("Wrong independent sidewalk domain");
    var project=T("MovableRoadProjection").GetMethod("Prepared",Flags);var patch=patches.GetValue(domain);
    if(!ReferenceEquals(F(patch,"Surface"),surfaces[1]))throw new Exception("Grating route lost");
    var aa=project.Invoke(null,new[]{F(tri,"A"),patch});var bb=project.Invoke(null,new[]{F(tri,"B"),patch});var cc=project.Invoke(null,new[]{F(tri,"C"),patch});
    var ua=(Vector4)F(aa,"UV");var ub=(Vector4)F(bb,"UV");var uc=(Vector4)F(cc,"UV");
    if(Math.Abs((ub.x-ua.x)/(x1-x0)-dx[1])>1e-5||Math.Abs((uc.y-ua.y)+.5f)>1e-6)throw new Exception("Width stretched wire or 4m period lost");
    receiverCount++;
   }
   if(receiverCount!=widths.Count(w=>w>0)*2)throw new Exception("Absent side acquired grating receiver");layoutChecks++;
  }
  int checks=0,poseChecks=0,scenarios=0,totalTriangles=0;double maxError=0;
  using var reader=new BinaryReader(File.OpenRead(Arg("-fixture")));int levels=reader.ReadInt32();
  for(int lod=0;lod<levels;lod++){
   int count=reader.ReadInt32();var records=new (int slot,Vector3[] p,Vector4[] uv)[count];
   for(int i=0;i<count;i++){int slot=reader.ReadInt32();var p=new Vector3[3];var uv=new Vector4[3];for(int j=0;j<3;j++){p[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());uv[j]=new Vector4(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());}records[i]=(slot,p,uv);}totalTriangles+=count;
   for(int domain=0;domain<14;domain++) foreach(float width in new[]{1f,1.7f})foreach(var pose in new[]{Pose.identity}){
    var original=Arr(patchType,Enumerable.Range(0,14).Select(i=>New(patchType,Vector2.zero,Vector2.one)));
    var types=T("Draw01RoadAppearance").GetField("SupportedTypes",Flags).GetValue(null);
    var patches=(Array)apply.Invoke(null,new object[]{original,pose});
    for(int i=0;i<14;i++){var p=patches.GetValue(i);if(!(bool)F(p,"PreparedUnit")||!(bool)F(p,"RepeatU")||!(bool)F(p,"RepeatV")||!ReferenceEquals(F(p,"Surface"),surfaces[Category(i)]))throw new Exception("Physical part bypass "+i);}
    var tiltedPose=new Pose(new Vector3(2,3,-4),Quaternion.Euler(0,37,0));
    var tiltedPatches=(Array)apply.Invoke(null,new object[]{original,tiltedPose});
    var projected=T("MovableRoadProjection").GetMethod("Prepared",Flags);
    var faces=new List<object>();var inverse=Quaternion.Inverse(pose.rotation);
    for(int i=0;i<count;i++){var record=records[i];var v=new object[3];for(int j=0;j<3;j++){var q=record.p[j];q.x*=width;var p=inverse*(q-pose.position);v[j]=New(vertex,p,inverse*Vector3.up,new Vector4(1,0,0,1),record.uv[j],Color.white);}faces.Add(New(bound,New(triangle,v[0],v[1],v[2],0,record.slot,i),(uint)domain));}
    var input=New(inputType,false);S(input,"Faces",Arr(bound,faces));S(input,"Materials",Arr(surfaceType,new[]{surface}));S(input,"Native",surface);S(input,"Patches",patches);S(input,"SupportedTypes",types);
    var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
    var output=(Array)((Array)compose.Invoke(null,new object[]{Arr(inputType,new[]{input}),owner,paint,null})).GetValue(0);
    if(output.Length!=count)throw new Exception("Compose changed topology");
    var positions=new List<Vector3>();var uvs=new List<Vector4>();
    for(int i=0;i<count;i++)foreach(var corner in new[]{"A","B","C"}){
     var before=F(F(faces[i],"Geometry"),corner);var after=F(output.GetValue(i),corner);
     foreach(var field in new[]{"Position","Normal","Color"})if(!F(before,field).Equals(F(after,field)))throw new Exception(field+" changed");
     var p=(Vector3)F(after,"Position");var objectP=pose.position+pose.rotation*p;var uv=(Vector4)F(after,"UV");var prior=(Vector4)F(before,"UV");if(uv.z!=prior.z||uv.w!=prior.w)throw new Exception("UV zw changed");
     int category=Category(domain);double u=objectP.x*dx[category]+(category==0?(820.5-128)/512:0);double v=objectP.z*dz[category]*(category==1&&domain>=7?-1:1)+(category==0?-73.9609375/4:category==1?1.9599609375/8:0);
     double error=Math.Max(Math.Abs(uv.x-u)*px[category],Math.Abs(uv.y-v)*py[category]);maxError=Math.Max(maxError,error);if(error>.025)throw new Exception("Physical density/phase differs domain "+domain+": "+error);
     var tiltedPosition=Quaternion.Inverse(tiltedPose.rotation)*(objectP-tiltedPose.position);
     var tiltedVertex=New(vertex,tiltedPosition,Quaternion.Inverse(tiltedPose.rotation)*Vector3.up,new Vector4(1,0,0,1),prior,Color.white);
     var tilted=projected.Invoke(null,new[]{tiltedVertex,tiltedPatches.GetValue(domain)});
     var tiltedUv=(Vector4)F(tilted,"UV");double pe=Math.Max(Math.Abs(tiltedUv.x-u)*px[category],Math.Abs(tiltedUv.y-v)*py[category]);
     maxError=Math.Max(maxError,pe);if(pe>.025)throw new Exception("Posed projection density/phase differs: "+pe);poseChecks++;
     var tangent=(Vector4)F(after,"Tangent");var pt=(Vector4)F(tilted,"Tangent");var direction=category==1?Vector3.left:Vector3.right;var td=Quaternion.Inverse(tiltedPose.rotation)*direction;float hand=category==0||(category==1&&domain<7)?-1:1;
     if((new Vector3(tangent.x,tangent.y,tangent.z)-direction).sqrMagnitude>1e-10f||tangent.w!=hand||(new Vector3(pt.x,pt.y,pt.z)-td).sqrMagnitude>1e-10f||pt.w!=hand)throw new Exception("Normal tangent frame differs domain "+domain);
     positions.Add(p);uvs.Add(uv);checks++;
    }
    var mesh=new Mesh();try{mesh.SetVertices(positions);mesh.SetUVs(0,uvs);mesh.SetTriangles(Enumerable.Range(0,positions.Count).ToArray(),0);mesh.UploadMeshData(false);var read=new List<Vector4>();mesh.GetUVs(0,read);if(!read.SequenceEqual(uvs)||!mesh.vertices.SequenceEqual(positions))throw new Exception("Native upload differs");}finally{UnityEngine.Object.DestroyImmediate(mesh);}scenarios++;
   }
  }
  File.WriteAllText(Arg("-report"),"{\"status\":\"PASS\",\"candidateSha256\":\""+hash+"\",\"lods\":"+levels+",\"fixtureOwnerTriangles\":"+totalTriangles+",\"scenarios\":"+scenarios+",\"vertexChecks\":"+checks+",\"posedPreparedChecks\":"+poseChecks+",\"maxSourcePixelFloat32Error\":"+maxError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"actualApplyComposeAndMeshUpload\":true,\"physicalPartitionCases\":"+partitionChecks+",\"syntheticFramesAndDomainAssignment\":true,\"fullGameComposition\":false,\"nativeVisualAcceptance\":false}");Debug.Log("PASS Draw01 four physical parts: "+checks+" vertices; independent sidewalk layout cases: "+layoutChecks);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
