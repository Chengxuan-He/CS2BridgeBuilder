using System; using System.Linq; using System.Collections.Generic; using UnityEngine.Rendering; using System.IO; using System.Reflection; using UnityEngine; using UnityEditor;
public static class AffineGeometry88NativeMeshCheck {
 [System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]static extern IntPtr LoadLibraryW(string path);
 public static void Run(){try{
 string root=Environment.GetEnvironmentVariable("BB_AFFINE_ROOT")??"E:/CS2BridgeBuilder",candidate=Environment.GetEnvironmentVariable("BB_AFFINE_CANDIDATE");
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 string managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var n=new AssemblyName(e.Name).Name+".dll";var p=Path.Combine(candidate,n);if(!File.Exists(p))p=Path.Combine(managed,n);return File.Exists(p)?Assembly.LoadFrom(p):null;};
 if(LoadLibraryW("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll")==IntPtr.Zero)throw new Exception("Native source pipeline plugin not loaded");
 var asm=Assembly.LoadFrom(candidate+"/BridgeBuilder.dll");Debug.Log("Loaded candidate: "+asm.Location);var shell=asm.GetType("BridgeBuilder.Bridges.PedestrianDraw02RenderAssembly",true);var dir=(Environment.GetEnvironmentVariable("BB_AFFINE_NATIVE_INPUT")??root+"/artifacts/material-remap/shared/pedestrian82-native/").TrimEnd('/','\\')+"/";
 var outputDir=Environment.GetEnvironmentVariable("BB_AFFINE_NATIVE_OUTPUT")??dir;Directory.CreateDirectory(outputDir);
 object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 int nativeCases=0,retileCases=0,meshIndex=0;float currentDelta=0;var inventory=new List<string>{"lod\tdelta\tmesh\tslot\tcid\ttriangles"};
 void VerifyNative(int lod,object plan,int[] sourceIndices,bool verifyRetile=false) {
  using var raw=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-raw.bin"));int count=raw.ReadInt32(),indexCount=raw.ReadInt32(),indexStride=raw.ReadInt32(),attributeCount=raw.ReadInt32();raw.ReadBytes(indexCount*indexStride);
  var attributes=new List<(int a,int format,int dimension,byte[] bytes)>();
  for(int i=0;i<attributeCount;i++){int a=raw.ReadInt32(),format=raw.ReadInt32(),dimension=raw.ReadInt32(),size=raw.ReadInt32();attributes.Add((a,format,dimension,raw.ReadBytes(size)));}
  var source=new Mesh();Mesh output=null;object model=null;
  try {
   source.SetVertexBufferParams(count,attributes.Select(a=>new VertexAttributeDescriptor((VertexAttribute)a.a,(VertexAttributeFormat)a.format,a.dimension,a.a==0?0:1)).ToArray());
   source.SetVertexBufferData(attributes[0].bytes,0,0,attributes[0].bytes.Length,0);
   int stride=source.GetVertexBufferStride(1);var buffer=new byte[count*stride];
   foreach(var a in attributes.Where(a=>a.a!=0)){int offset=source.GetVertexAttributeOffset((VertexAttribute)a.a),size=a.bytes.Length/count;for(int i=0;i<count;i++)Array.Copy(a.bytes,i*size,buffer,i*stride+offset,size);}
   source.SetVertexBufferData(buffer,0,0,buffer.Length,1);source.indexFormat=IndexFormat.UInt32;source.SetIndices(sourceIndices,MeshTopology.Triangles,0);
   if(currentDelta==0) {
    var writer=asm.GetType("BridgeBuilder.Bridges.TowerFactory",true).GetMethod("BuildModel",F);
    var pointType=writer.GetParameters()[2].ParameterType.GetElementType();var owned=Array.CreateInstance(pointType,count);
    var original=source.vertices;for(int i=0;i<count;i++)owned.SetValue(Activator.CreateInstance(pointType,new object[]{original[i].x,original[i].y,original[i].z}),i);
    var coordinates=Array.CreateInstance(pointType.MakeArrayType(),2);coordinates.SetValue(owned,0);coordinates.SetValue(owned,1);
    using var fixedParameterScope=(IDisposable)asm.GetType("BridgeBuilder.Bridges.GenerationParameters",true).GetMethod("Begin",F).Invoke(null,null);
    var programs=asm.GetType("BridgeBuilder.Bridges.StructurePrograms",true);var program=programs.GetProperty("Data",F).GetValue(null);
    asm.GetType("BridgeBuilder.Bridges.AffineGeometry",true).GetMethod("Execute",F).MakeGenericMethod(pointType).Invoke(null,new object[]{program,17,coordinates,Array.Empty<float[]>(),new[]{1.25f},0,count,null});
    object fixedModel=null;Mesh fixedMesh=null;
    try {
     object[] fixedArgs={"PackedFixedWriter",source,owned,null,null};fixedModel=writer.Invoke(null,fixedArgs);if(fixedModel==null)throw new Exception("Fixed writer: "+fixedArgs[3]);
     var toMesh=fixedModel.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();fixedMesh=(Mesh)toMesh.Invoke(fixedModel,toMesh.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());
     var actual=fixedMesh.vertices;for(int i=0;i<count;i++)if(actual[i].x!=original[i].x*1.25f || BitConverter.SingleToInt32Bits(actual[i].y)!=BitConverter.SingleToInt32Bits(original[i].y) || BitConverter.SingleToInt32Bits(actual[i].z)!=BitConverter.SingleToInt32Bits(original[i].z))throw new Exception("Fixed direct target mismatch");
     if(!fixedMesh.triangles.SequenceEqual(sourceIndices))throw new Exception("Fixed indices changed");
     using var fixedRead=Mesh.AcquireReadOnlyMeshData(fixedMesh);
     foreach(var attribute in attributes.Where(a=>a.a!=0)) {
      var attr=(VertexAttribute)attribute.a;int stream=fixedMesh.GetVertexAttributeStream(attr),size=attribute.bytes.Length/count,offset=fixedMesh.GetVertexAttributeOffset(attr),step=fixedMesh.GetVertexBufferStride(stream);var bytes=fixedRead[0].GetVertexData<byte>(stream).ToArray();
      for(int i=0;i<count;i++)for(int k=0;k<size;k++)if(bytes[i*step+offset+k]!=attribute.bytes[i*size+k])throw new Exception("Fixed packed attribute changed");
     }
     Debug.Log("PASS fixed direct float3 target writer LOD="+lod+" vertices="+count+" exact XYZ/indices/all original attributes");
    } finally {if(fixedMesh!=null)UnityEngine.Object.DestroyImmediate(fixedMesh);(fixedModel as IDisposable)?.Dispose();}
   }
   var args=new object[]{"Lift04FullRetileCheck",source,plan,5,null,null,false};
   if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureModel",true).GetMethod("TryBuild",F).Invoke(null,args))throw new Exception("Native structure build failed: "+args[5]);model=args[4];
   var upload=model.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();output=(Mesh)upload.Invoke(model,upload.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());
   using var snapshot=Mesh.AcquireReadOnlyMeshData(output);
   foreach(var a in attributes.Where(a=>a.a==3 || a.a==13)){
    var attr=(VertexAttribute)a.a;bool promoted=a.a==4 && a.format==1 && output.GetVertexAttributeFormat(attr)==VertexAttributeFormat.Float32; if((!promoted && (int)output.GetVertexAttributeFormat(attr)!=a.format)||output.GetVertexAttributeDimension(attr)!=a.dimension)throw new Exception("Raw structural attribute layout changed: "+a.a);
    int stream=output.GetVertexAttributeStream(attr),size=a.bytes.Length/count,offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream);var actual=snapshot[0].GetVertexData<byte>(stream).ToArray();
    if(promoted){for(int i=0;i<count;i++)for(int axis=0;axis<a.dimension;axis++){float expectedUv=Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,i*size+axis*2));if(BitConverter.ToSingle(actual,i*step+offset+axis*4)!=expectedUv)throw new Exception("Original Float16 UV value changed during required Float32 promotion.");}}else for(int i=0;i<count;i++)for(int b=0;b<size;b++)if(a.bytes[i*size+b]!=actual[i*step+offset+b])throw new Exception("Original packed structural channel changed: "+a.a+" vertex "+i);
   }
   var planned=(Vector3[])Get(plan,"Positions");var actualPositions=output.vertices;if(planned.Length!=actualPositions.Length)throw new Exception("Native count mismatch");for(int vi=0;vi<planned.Length;vi++)if(!planned[vi].Equals(actualPositions[vi]))throw new Exception("Native position mismatch "+vi);
   var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];var expected=new List<int>();var replacements=new Dictionary<int,int[]>();foreach(var fragment in (Array)Get(plan,"Fragments"))replacements.Add((int)Get(fragment,"Triangle"),(int[])Get(fragment,"Indices"));for(int i=0;i<mask.Length;i++){if(!mask[i])expected.AddRange(new[]{sourceIndices[i*3],sourceIndices[i*3+1],sourceIndices[i*3+2]});if(replacements.TryGetValue(i,out var addition))expected.AddRange(addition);}
   if(!output.GetIndices(0).SequenceEqual(expected))throw new Exception("Native structural triangle order or restored membership changed.");
   if(verifyRetile){
    using var retileParameterScope=(IDisposable)asm.GetType("BridgeBuilder.Bridges.GenerationParameters",true).GetMethod("Begin",F).Invoke(null,null);
    var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var surfaceType=native.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var identifier=native.GetType("Colossal.IO.AssetDatabase.Identifier",true);var guidType=identifier.GetField("guid").FieldType;
    var cid=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"a1b3ca16b946cc34fc031e376ba03d71"});var surface=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(surface,Activator.CreateInstance(identifier,new[]{cid}));
    var tiles=Activator.CreateInstance(asm.GetType("BridgeBuilder.Bridges.MovableStructureTiles",true),F,null,new object[]{"PedestrianDraw02"},null);
    string sourceCid=cid.ToString();using(var reader=new BinaryReader(File.OpenRead(root+"/artifacts/agent-material-env/PedestrianDraw02/source-input/native/"+sourceCid+".Surface")))surfaceType.GetMethod("ReadHeader",F).Invoke(surface,new object[]{reader});
    var preparedType=asm.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);preparedType.GetField("_root",F).SetValue(null,root+"/assets/Materials");var cache=(System.Collections.IDictionary)preparedType.GetField("Surfaces",F).GetValue(null);
    foreach(var file in Directory.GetFiles(root+"/assets/Materials/PedestrianDraw02","*.Surface",SearchOption.AllDirectories)){
     string id=File.ReadAllText(file+".cid").Trim();var item=Activator.CreateInstance(surfaceType,true);using(var reader=new BinaryReader(File.OpenRead(file)))surfaceType.GetMethod("ReadHeader",F).Invoke(item,new object[]{reader});var hash=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{id});surfaceType.BaseType.GetField("m_Id",F).SetValue(item,Activator.CreateInstance(identifier,new[]{hash}));string key=file.Substring((root+"/assets/Materials/").Length).Replace('\\','/');if(!cache.Contains(key))cache.Add(key,item);
    }
var listType=typeof(List<>).MakeGenericType(model.GetType());var owned=(System.Collections.IList)Activator.CreateInstance(listType);var models=(System.Collections.IList)Activator.CreateInstance(listType);var surfaces=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
    var retileArgs=new object[]{"Restored outer/walk structure","PedestrianDrawBridge02"+(lod==0?"":"_LOD"+lod)+" Mesh",meshIndex,plan,model,surface,tiles,owned,models,surfaces,null};if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureRetile",true).GetMethod("TryAppend",F).Invoke(null,retileArgs))throw new Exception((string)retileArgs[retileArgs.Length-1]);
    string Point(Vector3 v)=>BitConverter.SingleToInt32Bits(v.x)+","+BitConverter.SingleToInt32Bits(v.y)+","+BitConverter.SingleToInt32Bits(v.z);
    string Tri(Vector3 a,Vector3 b,Vector3 c)=>string.Join(";",new[]{Point(a),Point(b),Point(c)}.OrderBy(x=>x));
    var counts=new Dictionary<string,int>();for(int t=0;t<expected.Count;t+=3){var key=Tri(planned[expected[t]],planned[expected[t+1]],planned[expected[t+2]]);counts.TryGetValue(key,out int n);counts[key]=n+1;}
    long triangles=0;for(int slot=0;slot<models.Count;slot++){var part=models[slot];var method=part.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();var mesh=(Mesh)method.Invoke(part,method.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());long countTriangles=(long)mesh.GetIndexCount(0)/3;triangles+=countTriangles;var pv=mesh.vertices;var pi=mesh.GetIndices(0);for(int t=0;t<pi.Length;t+=3){var key=Tri(pv[pi[t]],pv[pi[t+1]],pv[pi[t+2]]);if(!counts.TryGetValue(key,out int n)||n==0)throw new Exception("Retile changed physical triangle");counts[key]=n-1;}string sid=surfaceType.GetProperty("id").GetValue(surfaces[slot]).ToString();inventory.Add(lod+"\t"+currentDelta+"\t"+meshIndex+"\t"+slot+"\t"+sid+"\t"+countTriangles);UnityEngine.Object.DestroyImmediate(mesh);}if(counts.Values.Any(n=>n!=0))throw new Exception("Retile lost physical triangles");if(triangles!=expected.Count/3)throw new Exception("Full Retile triangle count changed");foreach(IDisposable part in owned)part.Dispose();
    retileCases++;
   }
   nativeCases++;
  }finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(model!=null)((IDisposable)model).Dispose();UnityEngine.Object.DestroyImmediate(source);}
 }

 for(int lod=0;lod<2;lod++){
 using var r=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-fixture.bin"));var v=new Vector3[r.ReadInt32()];var ix=new int[r.ReadInt32()];for(int i=0;i<v.Length;i++)v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());for(int i=0;i<ix.Length;i++)ix[i]=r.ReadInt32();
 foreach(float delta in new[]{-6f,-4.4952392578125f,-1f,0f,2f,7.25f,24f}){
 object plan;using(var parameterScope=(IDisposable)asm.GetType("BridgeBuilder.Bridges.GenerationParameters")?.GetMethod("Begin",BindingFlags.Static|BindingFlags.NonPublic)?.Invoke(null,null)){plan=shell.GetMethod("Structure",F).Invoke(null,new object[]{lod,0,v,ix,delta});}currentDelta=delta;VerifyNative(lod,plan,ix,true);
 }}File.WriteAllLines(Path.Combine(outputDir,"full-retile-slots.tsv"),inventory);var report="PASS Pedestrian02 production plan/native TryBuild/upload/exact positions/bone/color/output index cases="+nativeCases+"; actual Retile exact physical triangle multisets="+retileCases;File.WriteAllText(Path.Combine(outputDir,"full-retile-result.txt"),report);Debug.Log(report);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
