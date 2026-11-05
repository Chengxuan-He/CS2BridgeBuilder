using Unity.Collections;using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Runtime.Serialization;using UnityEngine;using UnityEngine.Rendering;using UnityEditor;
public static class PreparedTangentCheck {
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] struct PositionColorBone { public Vector3 Position; public uint Color,Bone; }
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static object New(Type t,params object[] a)=>Activator.CreateInstance(t,F & ~BindingFlags.Static,null,a,null);
 static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
 static Array Arr(Type t,params object[] a){var v=Array.CreateInstance(t,a.Length);for(int i=0;i<a.Length;i++)v.SetValue(a[i],i);return v;}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];string managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
 var math=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils",true);
 uint Pack(string name,params float[] x){var m=math.GetMethod(name,F);return Convert.ToUInt32(m.Invoke(null,new[]{New(m.GetParameters()[0].ParameterType,x.Cast<object>().ToArray())}));}
 var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var st=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var surface=FormatterServices.GetUninitializedObject(st);
 var domainType=T("MovableUvDomains+Domain");var recipe=FormatterServices.GetUninitializedObject(T("MovableUvDomains"));
 recipe.GetType().GetField("Items",F).SetValue(recipe,Arr(domainType,New(domainType,0,1,2,new[]{0},0,1,new Vector4(0,0,1,1),false)));
 ((IDictionary)T("MovableUvDomains").GetField("Loaded",F).GetValue(null))["UvDomain_PreparedTangentFixture_0"]=recipe;
 Vector3 DecodeNormal(uint packed) {
 var nt=Assembly.LoadFrom(Path.Combine(managed,"Game.dll")).GetType("Game.Prefabs.MeshNormal",true);
 var at=typeof(NativeArray<>).MakeGenericType(nt);var result=Activator.CreateInstance(at,new object[]{1,Allocator.Temp,NativeArrayOptions.ClearMemory});
 using var bytes=new NativeArray<byte>(BitConverter.GetBytes(packed),Allocator.Temp);
 try{nt.GetMethods(F).Single(m=>m.Name=="Unpack"&&m.GetParameters().Length==5&&m.GetParameters()[1].ParameterType==at).Invoke(null,new object[]{new NativeSlice<byte>(bytes),result,1,VertexAttributeFormat.SNorm16,2});var n=Get(at.GetProperty("Item").GetValue(result,new object[]{0}),"m_Normal");return new Vector3((float)Get(n,"x"),(float)Get(n,"y"),(float)Get(n,"z"));}finally{((IDisposable)result).Dispose();}
 }
 int cases=0,vertices=0;
 foreach(int mode in new[]{0,1,2})foreach(bool shear in new[]{false,true})foreach(int projectionMode in new[]{0,1,2}){
 var positions=new[]{Vector3.zero,Vector3.right,Vector3.forward};var mapped=(Vector3[])positions.Clone();var j=Matrix4x4.identity;if(shear)j.m10=.2f;for(int i=0;i<3;i++)mapped[i]=j.MultiplyPoint3x4(positions[i]);
 var mesh=new Mesh();mesh.SetVertexBufferParams(3,new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3,0),new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.SNorm16,2,1),new VertexAttributeDescriptor(VertexAttribute.Tangent,VertexAttributeFormat.Float32,1,2),new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,4,3),new VertexAttributeDescriptor(VertexAttribute.Color,VertexAttributeFormat.UNorm8,4,0),new VertexAttributeDescriptor(VertexAttribute.BlendIndices,VertexAttributeFormat.UInt32,1,0));
 mesh.SetVertexBufferData(positions.Select(p=>new PositionColorBone{Position=p,Color=0x12345678,Bone=1}).ToArray(),0,0,3,0);mesh.SetVertexBufferData(Enumerable.Repeat(Pack("NormalToOctahedral",0,1,0),3).ToArray(),0,0,3,1);mesh.SetVertexBufferData(Enumerable.Repeat(Pack("TangentToOctahedral",1,0,0,1),3).ToArray(),0,0,3,2);mesh.SetVertexBufferData(new[]{new Vector4(0,0,7,9),new Vector4(1,0,7,9),new Vector4(0,1,7,9)},0,0,3,3);mesh.SetIndices(new[]{0,1,2},MeshTopology.Triangles,0);
 var pc=T("MovableStructureModel+Plan").GetConstructors(F).Single();var a=pc.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:null).ToArray();a[0]=positions;a[1]=mapped;a[2]=new[]{new[]{0,1,2}};a[3]=new[]{new[]{mode!=0}};
 void Set(string name,object value){a[Array.FindIndex(pc.GetParameters(),p=>p.Name==name)]=value;}
 int count=mode==0?3:4;
 if(mode==1){Set("addedVertices",Arr(T("MovableStructureModel+EdgeVertex"),New(T("MovableStructureModel+EdgeVertex"),0,1,.5f)));Set("fragments",Arr(T("MovableStructureModel+Fragment"),New(T("MovableStructureModel+Fragment"),0,0,new[]{0,3,2,3,1,2})));}
 if(mode==2){Set("sourceCopies",new[]{1});Set("constructedX",new[]{mapped[1].x});Set("fragments",Arr(T("MovableStructureModel+Fragment"),New(T("MovableStructureModel+Fragment"),0,0,new[]{0,3,2})));}
 if(shear)Set("frameTransforms",Enumerable.Range(0,count).Select(i=>new KeyValuePair<int,Matrix4x4>(i,j)).ToArray());
 var frameType=T("MovableStructureModel+ProjectedUvFrame");var pairType=typeof(KeyValuePair<,>).MakeGenericType(typeof(int),frameType);var frames=Array.CreateInstance(pairType,projectionMode==0?0:count);
 for(int i=0;i<frames.Length;i++)frames.SetValue(New(pairType,i,New(frameType,projectionMode==2?Vector4.zero:new Vector4(0,0,-1,0),new Vector4(1,0,0,0))),i);
 var uvs=Enumerable.Range(0,count).Select(i=>new KeyValuePair<int,Vector2>(i,new Vector2(i,.4f))).ToArray();
 var prepared=New(T("MovableStructureModel+PreparedDomain"),0,surface,uvs,projectionMode==0?null:frames);Set("preparedDomains",Arr(T("MovableStructureModel+PreparedDomain"),prepared));
 var plan=pc.Invoke(a);object model=null;var owned=(IList)null;Mesh original=null;
 try{
 object[] b={"Prepared tangent fixture",mesh,plan,2,null,null,false};Need((bool)T("MovableStructureModel").GetMethod("TryBuild",F).Invoke(null,b),"TryBuild "+b[5]);model=b[4];
 Mesh Upload(object m){var fn=m.GetType().GetMethods(F).Where(q=>q.Name=="ToUnityMesh").OrderBy(q=>q.GetParameters().Length).First();return (Mesh)fn.Invoke(m,fn.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());}
 original=Upload(model);byte[] Read(Mesh m,VertexAttribute attr,int index){using var data=Mesh.AcquireReadOnlyMeshData(m);int stream=m.GetVertexAttributeStream(attr),size=attr==VertexAttribute.Position?12:attr==VertexAttribute.TexCoord0?16:4;var all=data[0].GetVertexData<byte>(stream);int at=index*m.GetVertexBufferStride(stream)+m.GetVertexAttributeOffset(attr);return Enumerable.Range(0,size).Select(i=>all[at+i]).ToArray();}
 var modelList=typeof(List<>).MakeGenericType(model.GetType());owned=(IList)New(modelList);var models=(IList)New(modelList);var surfaces=(IList)New(typeof(List<>).MakeGenericType(st));var tiles=New(T("MovableStructureTiles"),"PreparedTangentFixture");((IDictionary)Get(tiles,"_nativeSurfaces"))[surface]=surface;
 T("MovableStructureRetile").GetMethod("Append",F).Invoke(null,new[]{"Projection","PreparedTangentFixture",(object)0,plan,model,surface,tiles,owned,models,surfaces});Need(models.Count==1,"one resolved material");
 foreach(var m in models){var result=Upload(m);try{for(int i=0;i<result.vertexCount;i++){
 var uvBytes=Read(result,VertexAttribute.TexCoord0,i);int source=(int)BitConverter.ToSingle(uvBytes,0);Need(BitConverter.ToSingle(uvBytes,4)==.4f&&BitConverter.ToSingle(uvBytes,8)==7&&BitConverter.ToSingle(uvBytes,12)==9,"UV4 lost");
 foreach(var attr in new[]{VertexAttribute.Position,VertexAttribute.Normal,VertexAttribute.Color,VertexAttribute.BlendIndices})Need(Read(result,attr,i).SequenceEqual(Read(original,attr,source)),"non-tangent raw channel changed");
 uint actual=BitConverter.ToUInt32(Read(result,VertexAttribute.Tangent,i),0);if(projectionMode!=1)Need(actual==BitConverter.ToUInt32(Read(original,VertexAttribute.Tangent,source),0),"default/singular tangent changed");else {var n=DecodeNormal(BitConverter.ToUInt32(Read(original,VertexAttribute.Normal,source),0));var direction=Vector3.Cross(Vector3.right,n);float det=Vector3.Dot(Vector3.back,direction);direction=(direction/det).normalized;Need(actual==Pack("TangentToOctahedral",direction.x,direction.y,direction.z,det>0?1:-1),"projected native tangent bits");}vertices++;
 }}finally{UnityEngine.Object.DestroyImmediate(result);}}
 cases++;
 }finally{if(owned!=null)foreach(var m in owned)((IDisposable)m).Dispose();if(model!=null)((IDisposable)model).Dispose();if(original!=null)UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(mesh);}
 }
 Debug.Log("PASS prepared native tangent projection cases="+cases+" vertices="+vertices+" source/edge/copy; existing geometry frames, null/singular bit retention; UVzw/position/normal unchanged. Controlled recipe/native mesh, no renderer assertion.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
