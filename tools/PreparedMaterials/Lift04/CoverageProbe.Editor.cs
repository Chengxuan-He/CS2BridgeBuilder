using System; using System.Linq; using System.Collections.Generic; using UnityEngine.Rendering; using System.IO; using System.Reflection; using UnityEngine; using UnityEditor;
public static class Lift04CoverageProbe {
 [System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]static extern IntPtr LoadLibraryW(string path);
 public static void Run(){try{
 string root="E:/CS2BridgeBuilder",candidate=Environment.GetEnvironmentVariable("LIFT04_CANDIDATE_PATH");
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 string managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var n=new AssemblyName(e.Name).Name+".dll";var p=Path.Combine(candidate,n);if(!File.Exists(p))p=Path.Combine(managed,n);return File.Exists(p)?Assembly.LoadFrom(p):null;};
 if(LoadLibraryW("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll")==IntPtr.Zero)throw new Exception("Native source pipeline plugin not loaded");
 var asm=Assembly.LoadFrom(candidate+"/BridgeBuilder.dll");var shell=asm.GetType("BridgeBuilder.Bridges.Lift04StructurePlan",true);var dir=root+"/artifacts/material-remap/Lift04/coverage54/";
 var outputDir=Environment.GetEnvironmentVariable("LIFT04_PROBE_OUTPUT")??dir;Directory.CreateDirectory(outputDir);
 object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 int nativeCases=0,retileCases=0,meshIndex=0;
 void VerifyNative(int lod,object plan,int[] sourceIndices,bool verifyRetile=false) {
  using var raw=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-mesh"+meshIndex+"-raw.bin"));int count=raw.ReadInt32(),indexCount=raw.ReadInt32(),indexStride=raw.ReadInt32(),attributeCount=raw.ReadInt32();raw.ReadBytes(indexCount*indexStride);
  var attributes=new List<(int a,int format,int dimension,byte[] bytes)>();
  for(int i=0;i<attributeCount;i++){int a=raw.ReadInt32(),format=raw.ReadInt32(),dimension=raw.ReadInt32(),size=raw.ReadInt32();attributes.Add((a,format,dimension,raw.ReadBytes(size)));}
  var source=new Mesh();Mesh output=null;object model=null;
  try {
   source.SetVertexBufferParams(count,attributes.Select(a=>new VertexAttributeDescriptor((VertexAttribute)a.a,(VertexAttributeFormat)a.format,a.dimension,a.a==0?0:1)).ToArray());
   source.SetVertexBufferData(attributes[0].bytes,0,0,attributes[0].bytes.Length,0);
   int stride=source.GetVertexBufferStride(1);var buffer=new byte[count*stride];
   foreach(var a in attributes.Where(a=>a.a!=0)){int offset=source.GetVertexAttributeOffset((VertexAttribute)a.a),size=a.bytes.Length/count;for(int i=0;i<count;i++)Array.Copy(a.bytes,i*size,buffer,i*stride+offset,size);}
   source.SetVertexBufferData(buffer,0,0,buffer.Length,1);source.indexFormat=IndexFormat.UInt32;source.SetIndices(sourceIndices,MeshTopology.Triangles,0);
   var args=new object[]{"Draw02UndersideCheck",source,plan,3,null,null,false};
   if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureModel",true).GetMethod("TryBuild",F).Invoke(null,args))throw new Exception("Native structure build failed: "+args[5]);model=args[4];
   var upload=model.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();output=(Mesh)upload.Invoke(model,upload.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());
   using var snapshot=Mesh.AcquireReadOnlyMeshData(output);
   foreach(var a in attributes.Where(a=>a.a!=0)){
    var attr=(VertexAttribute)a.a;bool promoted=a.a==4 && a.format==1 && output.GetVertexAttributeFormat(attr)==VertexAttributeFormat.Float32; if((!promoted && (int)output.GetVertexAttributeFormat(attr)!=a.format)||output.GetVertexAttributeDimension(attr)!=a.dimension)throw new Exception("Raw structural attribute layout changed: "+a.a);
    int stream=output.GetVertexAttributeStream(attr),size=a.bytes.Length/count,offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream);var actual=snapshot[0].GetVertexData<byte>(stream).ToArray();
    if(promoted){for(int i=0;i<count;i++)for(int axis=0;axis<a.dimension;axis++){float expectedUv=Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,i*size+axis*2));if(BitConverter.ToSingle(actual,i*step+offset+axis*4)!=expectedUv)throw new Exception("Original Float16 UV value changed during required Float32 promotion.");}}else for(int i=0;i<count;i++)for(int b=0;b<size;b++)if(a.bytes[i*size+b]!=actual[i*step+offset+b])throw new Exception("Original packed structural channel changed: "+a.a+" vertex "+i);
   }
   var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];var expected=new List<int>();var replacements=new Dictionary<int,int[]>();foreach(var fragment in (Array)Get(plan,"Fragments"))replacements.Add((int)Get(fragment,"Triangle"),(int[])Get(fragment,"Indices"));for(int i=0;i<mask.Length;i++){if(!mask[i])expected.AddRange(new[]{sourceIndices[i*3],sourceIndices[i*3+1],sourceIndices[i*3+2]});if(replacements.TryGetValue(i,out var addition))expected.AddRange(addition);}
   if(!output.GetIndices(0).SequenceEqual(expected))throw new Exception("Native structural triangle order or restored membership changed.");
   if(verifyRetile){
    var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var surfaceType=native.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var identifier=native.GetType("Colossal.IO.AssetDatabase.Identifier",true);var guidType=identifier.GetField("guid").FieldType;
    var cid=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{meshIndex==1?"5a7696889d52714b56ac38b90d16c2de":lod==2?"3530c13c82fb42d7bd090005884e4956":"6b973ac2133fa9bd8d897bc9eff3363f"});var surface=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(surface,Activator.CreateInstance(identifier,new[]{cid}));
    var tiles=Activator.CreateInstance(asm.GetType("BridgeBuilder.Bridges.MovableStructureTiles",true),F,null,new object[]{"Lift04"},null);var listType=typeof(List<>).MakeGenericType(model.GetType());var owned=(System.Collections.IList)Activator.CreateInstance(listType);var models=(System.Collections.IList)Activator.CreateInstance(listType);var surfaces=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
    var retileArgs=new object[]{"Restored outer/walk structure","LiftBridge04"+(lod==0?"":"_LOD"+lod)+" Mesh",meshIndex,plan,model,surface,tiles,owned,models,surfaces,null};if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureRetile",true).GetMethod("TryAppend",F).Invoke(null,retileArgs))throw new Exception((string)retileArgs[retileArgs.Length-1]);
    if(owned.Count!=0||models.Count!=1||!ReferenceEquals(models[0],model)||surfaces.Count!=1||!ReferenceEquals(surfaces[0],surface))throw new Exception("Restored finite-source structure did not retain its actual native model/material through Retile.");
    retileCases++;
   }
   nativeCases++;
  }finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(model!=null)((IDisposable)model).Dispose();UnityEngine.Object.DestroyImmediate(source);}
 }

 for(int lod=0;lod<3;lod++)for(meshIndex=0;meshIndex<(lod<2?2:1);meshIndex++){
 using var r=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-mesh"+meshIndex+"-fixture.bin"));var v=new Vector3[r.ReadInt32()];var ix=new int[r.ReadInt32()];for(int i=0;i<v.Length;i++)v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());for(int i=0;i<ix.Length;i++)ix[i]=r.ReadInt32();
 foreach(float delta in new[]{0f,2f,4f,8f}){
 var current=meshIndex==0?shell:asm.GetType("BridgeBuilder.Bridges.Lift04Glass",true);var plan=current.GetMethod("Create",F).Invoke(null,new object[]{lod,v,ix,delta});VerifyNative(lod,plan,ix,meshIndex==1);
 if(meshIndex==0){
 var maskOriginal=((bool[][])Get(plan,"RemoveTriangles"))[0];var retained=Enumerable.Repeat(true,maskOriginal.Length).ToArray();string render="LiftBridge04"+(lod==0?"":"_LOD"+lod)+" Mesh";
 var domains=asm.GetType("BridgeBuilder.Bridges.MovableUvDomains",true).GetMethod("For",F).Invoke(null,new object[]{render,0});var ds=(Array)Get(domains,"Items");var tileType=asm.GetType("BridgeBuilder.Bridges.MovableStructureTiles",true);var tiles=Activator.CreateInstance(tileType,F,null,new object[]{"Lift04"},null);
 for(int di=0;di<ds.Length;di++)if((bool)tileType.GetMethod("PreserveSourceUv",F).Invoke(tiles,new object[]{render,0,di})){var d=ds.GetValue(di);var data=(int[])Get(d,"_data");int start=(int)Get(d,"_start"),count=(int)Get(d,"_count");for(int j=0;j<count;j++){int f=data[start+j];if(!maskOriginal[f])retained[f]=false;}}
 if(retained.All(x=>x))throw new Exception("No actual finite retained faces for Retile fixture");var ctor=plan.GetType().GetConstructors(F).Single();var ca=ctor.GetParameters().Select(q=>q.HasDefaultValue?q.DefaultValue:null).ToArray();ca[0]=v;ca[1]=((Vector3[])Get(plan,"Positions")).Take(v.Length).ToArray();ca[2]=new[]{ix};ca[3]=new[]{retained};var finite=ctor.Invoke(ca);VerifyNative(lod,finite,ix,true);
 }
 var p=(Vector3[])Get(plan,"Positions");var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];var fragments=(Array)Get(plan,"Fragments");
 using var w=new BinaryWriter(File.Create(Path.Combine(outputDir,"lod"+lod+"-mesh"+meshIndex+"-delta"+delta+".bin")));w.Write(p.Length);w.Write(mask.Length);w.Write(fragments.Length);foreach(var a in p){w.Write(a.x);w.Write(a.y);w.Write(a.z);}foreach(bool a in mask)w.Write(a);foreach(var frag in fragments){w.Write((int)Get(frag,"Triangle"));var ids=(int[])Get(frag,"Indices");w.Write(ids.Length);foreach(int i in ids)w.Write(i);}
 }}var report="PASS Lift04 production plan/native TryBuild/upload/raw N/T/bone and exact UV promotion/output index cases="+nativeCases+"; actual retained finite/glass Retile="+retileCases;File.WriteAllText(Path.Combine(outputDir,"native-result.txt"),report);Debug.Log(report);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
