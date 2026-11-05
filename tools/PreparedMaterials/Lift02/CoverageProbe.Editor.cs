using System; using System.Linq; using System.Collections.Generic; using UnityEngine.Rendering; using System.IO; using System.Reflection; using UnityEngine; using UnityEditor;
public static class Lift02CoverageProbe {
 [System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]static extern IntPtr LoadLibraryW(string path);
 public static void Run(){try{
 string root="D:/CS2BridgeBuilder",candidate=Environment.GetEnvironmentVariable("LIFT02_CANDIDATE_PATH");
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 string managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var n=new AssemblyName(e.Name).Name+".dll";var p=Path.Combine(candidate,n);if(!File.Exists(p))p=Path.Combine(managed,n);return File.Exists(p)?Assembly.LoadFrom(p):null;};
 if(LoadLibraryW("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll")==IntPtr.Zero)throw new Exception("Native source pipeline plugin not loaded");
 var asm=Assembly.LoadFrom(candidate+"/BridgeBuilder.dll");var shell=asm.GetType("BridgeBuilder.Bridges.Lift02StructurePlan",true);var dir=root+"/artifacts/material-remap/Lift02/coverage54/";
 var outputDir=Environment.GetEnvironmentVariable("LIFT02_PROBE_OUTPUT")??dir;Directory.CreateDirectory(outputDir);
 object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 int nativeCases=0,retileCases=0;
 void VerifyNative(int lod,object plan,int[] sourceIndices,bool verifyRetile=false) {
  using var raw=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-raw.bin"));int count=raw.ReadInt32(),indexCount=raw.ReadInt32(),indexStride=raw.ReadInt32(),attributeCount=raw.ReadInt32();raw.ReadBytes(indexCount*indexStride);
  var attributes=new List<(int a,int format,int dimension,byte[] bytes)>();
  for(int i=0;i<attributeCount;i++){int a=raw.ReadInt32(),format=raw.ReadInt32(),dimension=raw.ReadInt32(),size=raw.ReadInt32();attributes.Add((a,format,dimension,raw.ReadBytes(size)));}
  var source=new Mesh { indexFormat=IndexFormat.UInt32 };Mesh output=null;object model=null;
  try {
   source.SetVertexBufferParams(count,attributes.Select(a=>new VertexAttributeDescriptor((VertexAttribute)a.a,(VertexAttributeFormat)a.format,a.dimension,a.a==0?0:1)).ToArray());
   source.SetVertexBufferData(attributes[0].bytes,0,0,attributes[0].bytes.Length,0);
   int stride=source.GetVertexBufferStride(1);var buffer=new byte[count*stride];
   foreach(var a in attributes.Where(a=>a.a!=0)){int offset=source.GetVertexAttributeOffset((VertexAttribute)a.a),size=a.bytes.Length/count;for(int i=0;i<count;i++)Array.Copy(a.bytes,i*size,buffer,i*stride+offset,size);}
   source.SetVertexBufferData(buffer,0,0,buffer.Length,1);source.SetIndices(sourceIndices,MeshTopology.Triangles,0);
   var args=new object[]{"Draw02UndersideCheck",source,plan,10,null,null,false};
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
    var binding=File.ReadAllLines(dir+"retile-inputs.txt")[lod].Split('|');
    var cid=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{binding[2]});var surface=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(surface,Activator.CreateInstance(identifier,new[]{cid}));
    var tiles=Activator.CreateInstance(asm.GetType("BridgeBuilder.Bridges.MovableStructureTiles",true),F,null,new object[]{"Lift02"},null);var listType=typeof(List<>).MakeGenericType(model.GetType());var owned=(System.Collections.IList)Activator.CreateInstance(listType);var models=(System.Collections.IList)Activator.CreateInstance(listType);var surfaces=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
    asm.GetType("BridgeBuilder.Bridges.MovableStructureRetile",true).GetMethod("Append",F).Invoke(null,new object[]{"Restored outer/walk structure",binding[0],int.Parse(binding[1]),plan,model,surface,tiles,owned,models,surfaces});
    if(owned.Count!=0||models.Count!=1||!ReferenceEquals(models[0],model)||surfaces.Count!=1||!ReferenceEquals(surfaces[0],surface))throw new Exception("Restored finite-source structure did not retain its actual native model/material through Retile.");
    File.AppendAllText(Path.Combine(outputDir,"retile-batches.txt"),binding[0]+"|"+binding[1]+"|"+binding[2]+"|triangles="+output.GetIndices(0).Length/3+"\n");
    retileCases++;
   }
   nativeCases++;
  }finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(model!=null)((IDisposable)model).Dispose();UnityEngine.Object.DestroyImmediate(source);}
 }
 for(int lod=0;lod<3;lod++){
 using var r=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-fixture.bin"));var v=new Vector3[r.ReadInt32()];var ix=new int[r.ReadInt32()];for(int i=0;i<v.Length;i++)v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());for(int i=0;i<ix.Length;i++)ix[i]=r.ReadInt32();
 foreach(float delta in new[]{0f,2f,4f,8f}) {
 var plan=shell.GetMethod("Create",F).Invoke(null,new object[]{lod,v,ix,delta});
 VerifyNative(lod,plan,ix,true);
 var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];
 using(var w=new BinaryWriter(File.Create(Path.Combine(outputDir,"lod"+lod+"-delta"+delta+"-removed.bin")))) foreach(bool removed in mask) w.Write(removed);
 Debug.Log("Lift02 actual final Plan lod="+lod+" delta="+delta+" retained="+mask.Count(x=>!x)+" removed="+mask.Count(x=>x)+" fragments="+((Array)Get(plan,"Fragments")).Length);
 }
 }File.WriteAllText(Path.Combine(outputDir,"native-result.txt"),"PASS Lift02 final Plan/native TryBuild/upload/raw attribute and exact triangle membership cases="+nativeCases+"; no live owner/animation/retile acceptance.");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
