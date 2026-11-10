using System; using System.Linq; using System.Collections.Generic; using UnityEngine.Rendering; using System.IO; using System.Reflection; using UnityEngine; using UnityEditor;
public static class Draw02UndersideProbe {
 [System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]static extern IntPtr LoadLibraryW(string path);
 public static void Run(){try{
 string root="E:/CS2BridgeBuilder",candidate=Environment.GetEnvironmentVariable("DRAW02_CANDIDATE_PATH");
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 string managed="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed";
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var n=new AssemblyName(e.Name).Name+".dll";var p=Path.Combine(candidate,n);if(!File.Exists(p))p=Path.Combine(managed,n);return File.Exists(p)?Assembly.LoadFrom(p):null;};
 if(LoadLibraryW("D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll")==IntPtr.Zero)throw new Exception("Native source pipeline plugin not loaded");
 var asm=Assembly.LoadFrom(candidate+"/BridgeBuilder.dll");var shell=asm.GetType("BridgeBuilder.Bridges.Draw02MovingShell",true);var dir=root+"/artifacts/material-remap/Draw02/underside52/";
 var outputDir=Environment.GetEnvironmentVariable("DRAW02_PROBE_OUTPUT")??dir;Directory.CreateDirectory(outputDir);
 object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 int nativeCases=0,retileCases=0,frameVertices=0;
 Dictionary<int,(uint n,uint t)> ExpectedFrames(object plan,List<(int a,int format,int dimension,byte[] bytes)> attrs,int count){
  var field=plan.GetType().GetField("FrameTransforms",F);var result=new Dictionary<int,(uint,uint)>();if(field==null)return result;
  var values=(Array)field.GetValue(plan);if(values.Length==0)return result;
  var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.AssetPipeline.Native.dll")).GetType("Colossal.AssetPipeline.Native.NativeMath",true);
  float[] Decode(byte[] bytes,string method,int dimension){var input=System.Runtime.InteropServices.Marshal.AllocHGlobal(bytes.Length);var output=System.Runtime.InteropServices.Marshal.AllocHGlobal(count*dimension*4);try{System.Runtime.InteropServices.Marshal.Copy(bytes,0,input,bytes.Length);native.GetMethod(method,F).Invoke(null,new object[]{input,count,output});var data=new float[count*dimension];System.Runtime.InteropServices.Marshal.Copy(output,data,0,data.Length);return data;}finally{System.Runtime.InteropServices.Marshal.FreeHGlobal(input);System.Runtime.InteropServices.Marshal.FreeHGlobal(output);}}
  var ns=Decode(attrs.Single(a=>a.a==1).bytes,"ArrayOctahedralToNormals",3);var ts=Decode(attrs.Single(a=>a.a==2).bytes,"ArrayOctahedralToTangents",4);
  var maths=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils",true);
  var unityMath=Assembly.LoadFrom(Path.Combine(managed,"Unity.Mathematics.dll"));var f3=unityMath.GetType("Unity.Mathematics.float3",true);var f4=unityMath.GetType("Unity.Mathematics.float4",true);
  Vector3 Unit(Vector3 p){double length=Math.Sqrt((double)p.x*p.x+(double)p.y*p.y+(double)p.z*p.z);return new Vector3((float)(p.x/length),(float)(p.y/length),(float)(p.z/length));}
  foreach(var pair in values){int index=(int)pair.GetType().GetProperty("Key").GetValue(pair);var matrix=(Matrix4x4)pair.GetType().GetProperty("Value").GetValue(pair);float shear=matrix.m01,scale=matrix.m00;int src=index;
   if(index>=count){var edges=(Array)Get(plan,"AddedVertices");object node;int[] endpoints;if(index-count<edges.Length){node=edges.GetValue(index-count);endpoints=new[]{(int)Get(node,"A"),(int)Get(node,"B")};}else{node=((Array)Get(plan,"InteriorVertices")).GetValue(index-count-edges.Length);endpoints=new[]{(int)Get(node,"A"),(int)Get(node,"B"),(int)Get(node,"C")};}src=endpoints[0];foreach(int endpoint in endpoints)foreach(int attribute in new[]{1,2}){var bytes=attrs.Single(a=>a.a==attribute).bytes;if(BitConverter.ToUInt32(bytes,src*4)!=BitConverter.ToUInt32(bytes,endpoint*4))throw new Exception("Constructed frame fixture requires independently equal original endpoint N/T.");}}
   var n=Unit(new Vector3(ns[src*3]/scale,ns[src*3+1]-(shear/scale)*ns[src*3],ns[src*3+2]));var ft=new Vector3(scale*ts[src*4]+shear*ts[src*4+1],ts[src*4+1],ts[src*4+2]);var t=Unit(ft-n*Vector3.Dot(n,ft));
   uint nb=Convert.ToUInt32(maths.GetMethod("NormalToOctahedral",F).Invoke(null,new[]{Activator.CreateInstance(f3,new object[]{n.x,n.y,n.z})}));uint tb=Convert.ToUInt32(maths.GetMethod("TangentToOctahedral",F).Invoke(null,new[]{Activator.CreateInstance(f4,new object[]{t.x,t.y,t.z,ts[src*4+3]})}));result.Add(index,(nb,tb));
  }frameVertices+=result.Count;return result;
 }
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
   source.SetVertexBufferData(buffer,0,0,buffer.Length,1);source.SetIndices(sourceIndices,MeshTopology.Triangles,0);
   var args=new object[]{"Draw02UndersideCheck",source,plan,3,null,null,false};
   if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureModel",true).GetMethod("TryBuild",F).Invoke(null,args))throw new Exception("Native structure build failed: "+args[5]);model=args[4];
   var upload=model.GetType().GetMethods(F).Where(m=>m.Name=="ToUnityMesh").OrderBy(m=>m.GetParameters().Length).First();output=(Mesh)upload.Invoke(model,upload.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:Type.Missing).ToArray());
   using var snapshot=Mesh.AcquireReadOnlyMeshData(output);
   var nativeBuffers=new Dictionary<int,byte[]>(); byte[] Raw(int stream){if(!nativeBuffers.TryGetValue(stream,out var bytes))nativeBuffers.Add(stream,bytes=snapshot[0].GetVertexData<byte>(stream).ToArray());return bytes;}
   var expectedFrames=ExpectedFrames(plan,attributes,count);
   foreach(var a in attributes.Where(a=>a.a!=0)){
    var attr=(VertexAttribute)a.a;bool promoted=a.a==4 && a.format==1 && output.GetVertexAttributeFormat(attr)==VertexAttributeFormat.Float32; if((!promoted && (int)output.GetVertexAttributeFormat(attr)!=a.format)||output.GetVertexAttributeDimension(attr)!=a.dimension)throw new Exception("Raw structural attribute layout changed: "+a.a);
    int stream=output.GetVertexAttributeStream(attr),size=a.bytes.Length/count,offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream);var actual=Raw(stream);
    if(promoted){for(int i=0;i<count;i++)for(int axis=0;axis<a.dimension;axis++){float expectedUv=Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,i*size+axis*2));if(BitConverter.ToSingle(actual,i*step+offset+axis*4)!=expectedUv)throw new Exception("Original Float16 UV value changed during required Float32 promotion.");}}else for(int i=0;i<count;i++){if((a.a==1||a.a==2)&&expectedFrames.TryGetValue(i,out var frame)){uint expectedBits=a.a==1?frame.n:frame.t;if(BitConverter.ToUInt32(actual,i*step+offset)!=expectedBits)throw new Exception("Independent shear/native codec frame mismatch: "+a.a+" vertex "+i);continue;}for(int b=0;b<size;b++)if(a.bytes[i*size+b]!=actual[i*step+offset+b])throw new Exception("Original packed structural channel changed: "+a.a+" vertex "+i);}
   }
   foreach(var frame in expectedFrames.Where(p=>p.Key>=count))foreach(var attr in new[]{VertexAttribute.Normal,VertexAttribute.Tangent}){int stream=output.GetVertexAttributeStream(attr),offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream);var actual=Raw(stream);uint expectedBits=attr==VertexAttribute.Normal?frame.Value.n:frame.Value.t;if(BitConverter.ToUInt32(actual,frame.Key*step+offset)!=expectedBits)throw new Exception("Constructed wall independent native frame mismatch at "+frame.Key);}
   if(expectedFrames.Keys.Any(i=>i>=count)){
    var added=(Array)Get(plan,"AddedVertices");
    for(int e=lod==0?880:lod==1?876:24;e<added.Length;e++){
     var edge=added.GetValue(e);int aIndex=(int)Get(edge,"A"),bIndex=(int)Get(edge,"B"),outputIndex=count+e;float t=(float)Get(edge,"Fraction");
     foreach(var a in attributes.Where(a=>a.a!=0)){
      if((a.a==1||a.a==2)&&expectedFrames.ContainsKey(outputIndex))continue;
      var attr=(VertexAttribute)a.a;int stream=output.GetVertexAttributeStream(attr),offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream),size=a.bytes.Length/count;var bytes=Raw(stream);
      if(a.a==4){for(int axis=0;axis<a.dimension;axis++){float av=a.format==1?Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,aIndex*size+axis*2)):BitConverter.ToSingle(a.bytes,aIndex*size+axis*4);float bv=a.format==1?Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,bIndex*size+axis*2)):BitConverter.ToSingle(a.bytes,bIndex*size+axis*4);float value=av+(bv-av)*t;if(BitConverter.ToSingle(bytes,outputIndex*step+offset+axis*4)!=value)throw new Exception("Constructed original UV interpolation changed");}}
      else for(int channel=0;channel<size;channel++){if(a.bytes[aIndex*size+channel]!=a.bytes[bIndex*size+channel])throw new Exception("Wall fixture has unequal source endpoint channel "+a.a);if(bytes[outputIndex*step+offset+channel]!=a.bytes[aIndex*size+channel])throw new Exception("Constructed original packed channel changed "+a.a);}
     }
    }
   }
   var innerNodes=(Array)Get(plan,"InteriorVertices");var edgeNodes=(Array)Get(plan,"AddedVertices");
   for(int k=0;k<innerNodes.Length;k++){var node=innerNodes.GetValue(k);int ai=(int)Get(node,"A"),bi=(int)Get(node,"B"),ci=(int)Get(node,"C"),oi=count+edgeNodes.Length+k;float wb=(float)Get(node,"WeightB"),wc=(float)Get(node,"WeightC");foreach(var a in attributes.Where(a=>a.a!=0)){if((a.a==1||a.a==2)&&expectedFrames.ContainsKey(oi))continue;var attr=(VertexAttribute)a.a;int stream=output.GetVertexAttributeStream(attr),offset=output.GetVertexAttributeOffset(attr),step=output.GetVertexBufferStride(stream),size=a.bytes.Length/count;var bytes=Raw(stream);if(a.a==4){for(int axis=0;axis<a.dimension;axis++){float U(int i)=>a.format==1?Mathf.HalfToFloat(BitConverter.ToUInt16(a.bytes,i*size+axis*2)):BitConverter.ToSingle(a.bytes,i*size+axis*4);float av=U(ai),bv=U(bi),cv=U(ci),value=av+(bv-av)*wb;value+=(cv-av)*wc;float got=BitConverter.ToSingle(bytes,oi*step+offset+axis*4);double exact=(double)av+((double)bv-av)*wb+((double)cv-av)*wc,budget=5*Math.Pow(2,-24)*(Math.Abs(av)+Math.Abs(((double)bv-av)*wb)+Math.Abs(((double)cv-av)*wc));if(Math.Abs(got-exact)>budget||got!=value)throw new Exception("Interior original UV interpolation changed at "+oi+" axis "+axis+": actual="+got.ToString("R")+" expected="+value.ToString("R")+" exact="+exact.ToString("R"));}}else for(int c=0;c<size;c++){if(a.bytes[ai*size+c]!=a.bytes[bi*size+c]||a.bytes[ai*size+c]!=a.bytes[ci*size+c])throw new Exception("Interior fixture source attribute differs "+a.a);if(bytes[oi*step+offset+c]!=a.bytes[ai*size+c])throw new Exception("Interior original packed attribute changed "+a.a);}}}
   var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];var expected=new List<int>();var replacements=new Dictionary<int,int[]>();foreach(var fragment in (Array)Get(plan,"Fragments"))replacements.Add((int)Get(fragment,"Triangle"),(int[])Get(fragment,"Indices"));for(int i=0;i<mask.Length;i++){if(!mask[i])expected.AddRange(new[]{sourceIndices[i*3],sourceIndices[i*3+1],sourceIndices[i*3+2]});if(replacements.TryGetValue(i,out var addition))expected.AddRange(addition);}
   if(!output.GetIndices(0).SequenceEqual(expected))throw new Exception("Native structural triangle order or restored membership changed.");
   if(verifyRetile){
    var native=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));var surfaceType=native.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);var identifier=native.GetType("Colossal.IO.AssetDatabase.Identifier",true);var guidType=identifier.GetField("guid").FieldType;
    var cid=guidType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{lod==2?"436d1828c36d1336cf115c85def2c62c":"8a28ab8d9526e258efbcb81c3bbc936b"});var surface=Activator.CreateInstance(surfaceType,true);surfaceType.BaseType.GetField("m_Id",F).SetValue(surface,Activator.CreateInstance(identifier,new[]{cid}));
    var tiles=Activator.CreateInstance(asm.GetType("BridgeBuilder.Bridges.MovableStructureTiles",true),F,null,new object[]{"Draw02"},null);var listType=typeof(List<>).MakeGenericType(model.GetType());var owned=(System.Collections.IList)Activator.CreateInstance(listType);var models=(System.Collections.IList)Activator.CreateInstance(listType);var surfaces=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
    var retileArgs=new object[]{"Restored outer/walk structure","DrawBridge02"+(lod==0?"":"_LOD"+lod)+" Mesh",0,plan,model,surface,tiles,owned,models,surfaces,null};if(!(bool)asm.GetType("BridgeBuilder.Bridges.MovableStructureRetile",true).GetMethod("TryAppend",F).Invoke(null,retileArgs))throw new Exception((string)retileArgs[retileArgs.Length-1]);
    if(owned.Count!=0||models.Count!=1||!ReferenceEquals(models[0],model)||surfaces.Count!=1||!ReferenceEquals(surfaces[0],surface))throw new Exception("Restored finite-source structure did not retain its actual native model/material through Retile.");
    retileCases++;
   }
   nativeCases++;
  }finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(model!=null)((IDisposable)model).Dispose();UnityEngine.Object.DestroyImmediate(source);}
 }
 for(int lod=0;lod<3;lod++){
 using var r=new BinaryReader(File.OpenRead(dir+"lod"+lod+"-fixture.bin"));var v=new Vector3[r.ReadInt32()];var ix=new int[r.ReadInt32()];for(int i=0;i<v.Length;i++)v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());for(int i=0;i<ix.Length;i++)ix[i]=r.ReadInt32();
 var widths=Environment.GetEnvironmentVariable("DRAW02_PROBE_DELTAS");
 var selectedScenarios=Environment.GetEnvironmentVariable("DRAW02_PROBE_SCENARIOS");
 foreach(float delta in widths==null?new[]{0f,2f,4f,5.75f,8f}:widths.Split(',').Select(s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray())foreach(int scenario in selectedScenarios==null?Enumerable.Range(0,6):selectedScenarios.Split(',').Select(int.Parse)){
 var plan=shell.GetMethod("Create",F).Invoke(null,new object[]{lod,v,v,ix,delta});string mode=Environment.GetEnvironmentVariable("DRAW02_PROBE_TAG")??"before";
 if(mode!="before") {
  var layoutType=asm.GetType("BridgeBuilder.Bridges.MovableSelectedRoadLayout",true);var layout=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(layoutType);
  var left=new[]{new Vector2(-9.5f-delta,-6f-delta),Vector2.zero,new Vector2(-12f,-7f),Vector2.zero,new Vector2(-13f,-8f),new Vector2(-13.5f,-10.5f)}[scenario];
  var right=new[]{new Vector2(6f+delta,9.5f+delta),Vector2.zero,Vector2.zero,new Vector2(8f,12f),new Vector2(9f,11f),new Vector2(10.5f,13.5f)}[scenario];
  if((mode.EndsWith("59",StringComparison.Ordinal)||(mode.EndsWith("60",StringComparison.Ordinal)||mode.EndsWith("61",StringComparison.Ordinal)))){
   float edge=9.5f+delta;
   left=new[]{new Vector2(-edge,-edge+3.5f),Vector2.zero,new Vector2(-edge,-edge+3f),Vector2.zero,new Vector2(-edge,-edge+2f),new Vector2(-edge,-edge+3.5f)}[scenario];
   right=new[]{new Vector2(edge-3.5f,edge),Vector2.zero,Vector2.zero,new Vector2(edge-3f,edge),new Vector2(edge-4f,edge),new Vector2(edge-3.5f,edge)}[scenario];
  }
  layoutType.GetField("LeftSidewalk",F).SetValue(layout,left);layoutType.GetField("RightSidewalk",F).SetValue(layout,right);
  if((mode.EndsWith("59",StringComparison.Ordinal)||(mode.EndsWith("60",StringComparison.Ordinal)||mode.EndsWith("61",StringComparison.Ordinal)))){
   var laneType=asm.GetType("BridgeBuilder.Bridges.MovableRoadLaneLayout+Lane",true);
   var kindType=asm.GetType("BridgeBuilder.Bridges.MovableRoadLaneLayout+Kind",true);
   var ctor=laneType.GetConstructors(F).Single(c=>c.GetParameters().Length==11);
   float outer=delta==5.75f?13.25f:6f+delta,width=delta==5.75f?4f:3f;
   var lanes=Array.CreateInstance(laneType,2);
   for(int side=0;side<2;side++){float bound=side==0?(left.y>left.x?-left.y:outer):(right.y>right.x?right.x:outer);float center=(side==0?-1:1)*(bound-width*.5f);lanes.SetValue(ctor.Invoke(new object[]{center,width,center-width*.5f,center+width*.5f,-90f,90f,Enum.ToObject(kindType,1),(uint)0,null,null,true}),side);}
   if(delta==4f && scenario==5){
    // Exact native58 owner log: all six car and two pedestrian curves/widths/flags.
    var nativeCtor=laneType.GetConstructors(F).Single(c=>c.GetParameters().Length==14);lanes=Array.CreateInstance(laneType,8);
    var centers=new[]{-2.5f,-5.5f,-8.5f,2.5f,5.5f,8.5f,12f,-12f};
    for(int i=0;i<centers.Length;i++){float x=centers[i],z=x<0?90f:-90f;lanes.SetValue(nativeCtor.Invoke(new object[]{x,z,x,z/3,x,-z/3,x,-z,3f,3f,3f,1f,Enum.ToObject(kindType,i<6?1:4),(uint)(i<6?67108872:67109008)}),i);}
   }
   if(delta==5.75f && scenario==1){
    var nativeCtor=laneType.GetConstructors(F).Single(c=>c.GetParameters().Length==14);lanes=Array.CreateInstance(laneType,8);
    var centers=new[]{-3.25f,-7.25f,-11.25f,3.25f,7.25f,11.25f,14.25f,-14.25f};
    for(int i=0;i<centers.Length;i++){float x=centers[i],z=x<0?90f:-90f,laneWidth=i<6?4f:2f;lanes.SetValue(nativeCtor.Invoke(new object[]{x,z,x,z/3,x,-z/3,x,-z,laneWidth,laneWidth,laneWidth,1f,Enum.ToObject(kindType,i<6?1:4),(uint)(i<6?67108872:67109008)}),i);}
   }
   if(delta==-4.5f && scenario==1){
    // Actual native61 Double Train Track opposite failure: elevated width 10 m.
    // Pedestrian track shoulders are not Sidewalk sections and are not car lanes.
    var nativeCtor=laneType.GetConstructors(F).Single(c=>c.GetParameters().Length==14);lanes=Array.CreateInstance(laneType,4);
    var centers=new[]{4.5f,-4.5f,2f,-2f};
    for(int i=0;i<4;i++){float x=centers[i],z=x<0?90f:-90f,trainWidth=i<2?1f:4f;lanes.SetValue(nativeCtor.Invoke(new object[]{x,z,x,z<0?-29.9999886f:29.9999886f,x,z<0?29.9999886f:-29.9999886f,x,-z,trainWidth,trainWidth,trainWidth,1f,Enum.ToObject(kindType,i<2?4:16),(uint)(i<2?144:4194368)}),i);}
   }
   layoutType.GetField("Lanes",F).SetValue(layout,lanes);
  }
  plan=asm.GetType("BridgeBuilder.Bridges.Draw02Underside",true).GetMethod("Apply",F).Invoke(null,new object[]{lod,plan,layout});
 if((mode.EndsWith("60",StringComparison.Ordinal)||mode.EndsWith("61",StringComparison.Ordinal))){var closure=asm.GetType("BridgeBuilder.Bridges.Draw02EndClosure",false);if(closure!=null)plan=closure.GetMethod("Apply",F).Invoke(null,new object[]{lod,plan,layout});}
 if(mode.EndsWith("61",StringComparison.Ordinal))plan=asm.GetType("BridgeBuilder.Bridges.Draw02WalkEndClosure",true).GetMethod("Apply",F).Invoke(null,new object[]{lod,plan,layout});
 plan=asm.GetType("BridgeBuilder.Bridges.Draw02FixedCurbs",true).GetMethod("Remove",F).Invoke(null,new object[]{lod,plan});
 plan=asm.GetType("BridgeBuilder.Bridges.Draw02FixedSidewalks",true).GetMethod("Apply",F).Invoke(null,new object[]{lod,plan,layout});
 VerifyNative(lod,plan,ix);
 if(File.Exists(Path.Combine(outputDir,"lod"+lod+"-retile-faces.bin"))){
  var selectedMask=Enumerable.Repeat(true,ix.Length/3).ToArray();var selectedFaces=new HashSet<int>();var originalMask=((bool[][])Get(plan,"RemoveTriangles"))[0];using(var ids=new BinaryReader(File.OpenRead(Path.Combine(outputDir,"lod"+lod+"-retile-faces.bin"))))while(ids.BaseStream.Position<ids.BaseStream.Length){int face=ids.ReadInt32();selectedMask[face]=originalMask[face];selectedFaces.Add(face);}
  var ctor=plan.GetType().GetConstructors(F).Single();var ca=ctor.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:null).ToArray();ca[0]=Get(plan,"SourcePositions");ca[1]=((Vector3[])Get(plan,"Positions")).Take(v.Length).ToArray();ca[2]=new[]{ix};ca[3]=new[]{selectedMask};ca[4]=Get(plan,"AddedVertices");var allFragments=(Array)Get(plan,"Fragments");var chosen=allFragments.Cast<object>().Where(f=>selectedFaces.Contains((int)Get(f,"Triangle"))).ToArray();var fragmentArray=Array.CreateInstance(allFragments.GetType().GetElementType(),chosen.Length);for(int j=0;j<chosen.Length;j++)fragmentArray.SetValue(chosen[j],j);ca[5]=fragmentArray;ca[6]=Get(plan,"InteriorVertices");ca[7]=((Vector3[])Get(plan,"Positions")).Skip(v.Length).Select(p=>p.x).ToArray();int frameParameter=Array.FindIndex(ctor.GetParameters(),p=>p.Name=="frameTransforms");if(frameParameter>=0)ca[frameParameter]=Get(plan,"FrameTransforms");var retainedPlan=ctor.Invoke(ca);VerifyNative(lod,retainedPlan,ix,true);
 }
 } var p=(Vector3[])Get(plan,"Positions");var mask=((bool[][])Get(plan,"RemoveTriangles"))[0];var fragments=(Array)Get(plan,"Fragments");
 string tag=Environment.GetEnvironmentVariable("DRAW02_PROBE_TAG")??"before";
 using var w=new BinaryWriter(File.Create(Path.Combine(outputDir,tag+"-lod"+lod+"-delta"+delta+"-case"+scenario+".bin")));w.Write(p.Length);w.Write(mask.Length);w.Write(fragments.Length);foreach(var a in p){w.Write(a.x);w.Write(a.y);w.Write(a.z);}foreach(bool a in mask)w.Write(a);foreach(var frag in fragments){w.Write((int)Get(frag,"Triangle"));var ids=(int[])Get(frag,"Indices");w.Write(ids.Length);foreach(int i in ids)w.Write(i);}
 }}var report="PASS Draw02 production structural plan export; native TryBuild/upload/unaffected raw N/T/bone, numerically exact UV promotion and triangle membership cases="+nativeCases+"; independently transformed native frame vertex visits="+frameVertices+"; isolated restored-structure actual Retile retained identical model/Surface cases="+retileCases;File.WriteAllText(Path.Combine(outputDir,"native-result.txt"),report);Debug.Log(report);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
