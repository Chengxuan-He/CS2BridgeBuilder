using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Original raw prototype inputs; actual compiled complete style Plan and native writer.
public static class StructuralContinuousCheck
{
    [System.Runtime.InteropServices.DllImport("kernel32", CharSet=System.Runtime.InteropServices.CharSet.Unicode, SetLastError=true)]
    static extern IntPtr LoadLibrary(string path);
    const BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object x,string n)=>x.GetType().GetField(n,I)?.GetValue(x)??x.GetType().GetProperty(n,I).GetValue(x);
    static object New(Type t,params object[] a)=>Activator.CreateInstance(t,I,null,a,null);
    static void Require(bool test,string message){if(!test)throw new Exception(message);}
    static byte[] Bytes(object x)=>((IEnumerable)Get(x,"data")).Cast<byte>().ToArray();
    static object Attr(object x,VertexAttribute a)=>((IEnumerable)x).Cast<object>().Single(v=>(VertexAttribute)Get(v,"attribute")==a);
    static double Area(Vector3 a,Vector3 b,Vector3 c)
    { double ux=(double)b.x-a.x,uy=(double)b.y-a.y,uz=(double)b.z-a.z,vx=(double)c.x-a.x,vy=(double)c.y-a.y,vz=(double)c.z-a.z;
      double x=uy*vz-uz*vy,y=uz*vx-ux*vz,z=ux*vy-uy*vx;return Math.Sqrt(x*x+y*y+z*z)/2; }
    static double Ulp(float value)
    {value=Math.Abs(value);int bits=BitConverter.ToInt32(BitConverter.GetBytes(value),0);return (double)BitConverter.ToSingle(BitConverter.GetBytes(bits+1),0)-value;}
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
            var managed=Arg("-gameManaged");var input=Arg("-inputs");
            Require(LoadLibrary(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")))!=IntPtr.Zero,"Native PipelinePlugin load failed");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
            using(var sha=System.Security.Cryptography.SHA256.Create())Debug.Log("Lift05 native continuous candidate SHA256="+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(mod.Location))).Replace("-",""));
            var tilesType=T("MovableStructureTiles");var surfaceType=tilesType.GetMethod("NativeSurface",I).ReturnType;
            var surface=FormatterServices.GetUninitializedObject(surfaceType);
            using(var reader=new BinaryReader(File.OpenRead(Path.Combine(input,"native.Surface"))))surfaceType.GetMethod("ReadHeader",I).Invoke(surface,new object[]{reader});
            var finished=FormatterServices.GetUninitializedObject(surfaceType);
            using(var reader=new BinaryReader(File.OpenRead(Arg("-finishedSurface"))))surfaceType.GetMethod("ReadHeader",I).Invoke(finished,new object[]{reader});
            int cases=0;
            for(int lod=0;lod<2;lod++)
            {
                int vc,ic;int[] indices;var attrs=new Dictionary<int,byte[]>();var declarations=new List<VertexAttributeDescriptor>();
                using(var reader=new BinaryReader(File.OpenRead(Path.Combine(input,"lod"+lod+".bin"))))
                {
                    vc=reader.ReadInt32();ic=reader.ReadInt32();int ac=reader.ReadInt32();indices=new int[ic];
                    for(int i=0;i<ic;i++)indices[i]=reader.ReadInt32();
                    for(int i=0;i<ac;i++){int a=reader.ReadInt32(),format=reader.ReadInt32(),dim=reader.ReadInt32(),length=reader.ReadInt32();attrs.Add(a,reader.ReadBytes(length));declarations.Add(new VertexAttributeDescriptor((VertexAttribute)a,(VertexAttributeFormat)format,dim,0));}
                }
                var source=new Vector3[vc];for(int i=0;i<vc;i++)source[i]=new Vector3(BitConverter.ToSingle(attrs[0],i*12),BitConverter.ToSingle(attrs[0],i*12+4),BitConverter.ToSingle(attrs[0],i*12+8));
                var data=T("Lift05StructuralWrapData").GetMethod("For",S).Invoke(null,new object[]{lod});
                var faces=((IEnumerable)Get(data,"Faces")).Cast<object>().ToArray();var allowed=new HashSet<int>(faces.Select(f=>(int)Get(f,"Triangle")));
                foreach(float delta in new[]{0f,1f,3f,6f,12f})
                {
                    var mesh=new Mesh();object built=null;Mesh uploaded=null;
                    try
                    {
                        mesh.SetVertexBufferParams(vc,declarations.ToArray());int stride=mesh.GetVertexBufferStride(0);var raw=new byte[vc*stride];
                        foreach(var pair in attrs){int size=pair.Value.Length/vc,offset=mesh.GetVertexAttributeOffset((VertexAttribute)pair.Key);for(int v=0;v<vc;v++)Buffer.BlockCopy(pair.Value,v*size,raw,v*stride+offset,size);}
                        mesh.SetVertexBufferData(raw,0,0,raw.Length);mesh.SetIndices(indices,MeshTopology.Triangles,0);
                        var before=T("Lift05StructureWidthPlan").GetMethod("Create",S).Invoke(null,new object[]{lod,source,indices,delta});
                        var plan=T("Lift05StructurePlan").GetMethod("Create",S).Invoke(null,new object[]{lod,source,indices,delta,finished,new Vector2(0,.25f),new Vector2(1,.75f)});
                        Require(((int[])Get(plan,"Lift05WrapDomains")).Length==0,"Obsolete wrap domain enabled");
                        Require(((Vector3[])Get(plan,"Positions")).SequenceEqual((Vector3[])Get(before,"Positions")),"Continuous binding changed positions");
                        foreach(string field in new[]{"AddedVertices","InteriorVertices"})Require(((Array)Get(plan,field)).Cast<object>().SequenceEqual(((Array)Get(before,field)).Cast<object>()),"Continuous binding added construction vertices");
                        var existing=((IEnumerable)Get(plan,"Fragments")).Cast<object>().ToDictionary(f=>(int)Get(f,"Triangle"));
                        var originalFragments=((IEnumerable)Get(before,"Fragments")).Cast<object>().ToDictionary(f=>(int)Get(f,"Triangle"));
                        Require(existing.Count==originalFragments.Count && existing.All(pair=>((int[])Get(pair.Value,"Indices")).SequenceEqual((int[])Get(originalFragments[pair.Key],"Indices"))),"Continuous binding changed fragments");
                        // Verify all legitimate constructed vertices/fragments before profile-only isolation.
                        var beforeMask=((bool[][])Get(before,"RemoveTriangles"))[0];
                        T("Lift05RoadTops").GetMethod("RemoveDetailedFixed",S).Invoke(null,new object[]{lod,indices,beforeMask});
                        T("Lift05RoadTops").GetMethod("RemoveMoving",S).Invoke(null,new object[]{lod,indices,beforeMask});
                        object[] baselineBuild={"Lift05 pre-repeat baseline",mesh,before,3,null,null,false};
                        object[] completeBuild={"Lift05 complete continuous",mesh,plan,3,null,null,false};
                        Require((bool)T("MovableStructureModel").GetMethod("TryBuild",S).Invoke(null,baselineBuild),"Baseline Build failed: "+baselineBuild[5]);
                        Require((bool)T("MovableStructureModel").GetMethod("TryBuild",S).Invoke(null,completeBuild),"Complete Build failed: "+completeBuild[5]);
                        int fullTriangles=0;
                        try
                        {
                            foreach(var attribute in ((IEnumerable)baselineBuild[4]).Cast<object>())Require(Bytes(attribute).SequenceEqual(Bytes(Attr(completeBuild[4],(VertexAttribute)Get(attribute,"attribute")))),"Complete constructed attribute changed");
                            var bm=(Mesh)baselineBuild[4].GetType().GetMethod("ToUnityMesh",I).Invoke(baselineBuild[4],new object[]{true});
                            var cm=(Mesh)completeBuild[4].GetType().GetMethod("ToUnityMesh",I).Invoke(completeBuild[4],new object[]{true});
                            try
                            {
                                Require(bm.triangles.SequenceEqual(cm.triangles)&&bm.vertices.SequenceEqual(cm.vertices),"Complete native geometry changed");fullTriangles=cm.triangles.Length/3;
                                using(var br=Mesh.AcquireReadOnlyMeshData(bm))using(var cr=Mesh.AcquireReadOnlyMeshData(cm))
                                for(int stream=0;stream<bm.vertexBufferCount;stream++)Require(br[0].GetVertexData<byte>(stream).ToArray().SequenceEqual(cr[0].GetVertexData<byte>(stream).ToArray()),"Complete native upload attribute changed");
                            }
                            finally{UnityEngine.Object.DestroyImmediate(bm);UnityEngine.Object.DestroyImmediate(cm);}
                        }
                        finally{(baselineBuild[4] as IDisposable)?.Dispose();(completeBuild[4] as IDisposable)?.Dispose();}
                        var fragmentType=T("MovableStructureModel").GetNestedType("Fragment",I);
                        var fragments=allowed.OrderBy(f=>f).Select(f=>existing.TryGetValue(f,out var fragment)?fragment:New(fragmentType,0,f,indices.Skip(f*3).Take(3).ToArray())).ToArray();
                        var prepared=((IEnumerable)Get(plan,"PreparedDomains")).Cast<object>().ToDictionary(d=>(int)Get(d,"Domain"));
                        var expectedDomains=new Dictionary<int,Dictionary<int,Vector2>>();
                        using(var reader=new BinaryReader(File.OpenRead(Path.Combine(input,"continuous-lod"+lod+"-delta"+delta+".bin"))))
                        {int n=reader.ReadInt32();for(int d=0;d<n;d++){int id=reader.ReadInt32(),nv=reader.ReadInt32();var values=new Dictionary<int,Vector2>();for(int v=0;v<nv;v++)values.Add(reader.ReadInt32(),new Vector2(reader.ReadSingle(),reader.ReadSingle()));expectedDomains.Add(id,values);}}
                        Require(prepared.Count==expectedDomains.Count,"Prepared domain membership count differs");
                        double maxFixtureError=0;
                        foreach(var pair in prepared)
                        {Require(expectedDomains.ContainsKey(pair.Key),"Unexpected prepared domain");Require(ReferenceEquals(Get(pair.Value,"Surface"),finished),"Wrong finished Surface");
                         foreach(var uv in (IReadOnlyDictionary<int,Vector2>)Get(pair.Value,"Uvs"))if(uv.Key<vc){var target=expectedDomains[pair.Key][uv.Key];double error=Math.Max(Math.Abs(uv.Value.x-target.x),Math.Abs(uv.Value.y-target.y));maxFixtureError=Math.Max(maxFixtureError,error);Require(error<=2*Math.Max(Ulp(target.x),Ulp(target.y)),"Independent phase fixture mismatch");}}
                        var beforeFragments=((IEnumerable)Get(before,"Fragments")).Cast<object>().ToDictionary(f=>(int)Get(f,"Triangle"));
                        var p=(Vector3[])Get(plan,"Positions");var bp=(Vector3[])Get(before,"Positions");
                        int zero=0,triangles=0,preexisting=0;double maxAreaError=0,maxBaryError=0,maxPositionError=0;
                        foreach(var fragment in fragments)
                        {
                            int face=(int)Get(fragment,"Triangle");var ids=(int[])Get(fragment,"Indices");
                            var old=beforeFragments.TryGetValue(face,out var oldFragment)?(int[])Get(oldFragment,"Indices"):indices.Skip(face*3).Take(3).ToArray();
                            if(oldFragment!=null)preexisting++;
                            double[] Weights(object owner,int vertex)
                            {
                                var values=new double[3];
                                if(vertex<vc){int k=Array.IndexOf(indices,vertex,face*3,3)-face*3;Require(k>=0&&k<3,"Corner left source face");values[k]=1;return values;}
                                var c=owner.GetType().GetMethod("Construction",I).Invoke(owner,new object[]{vertex-vc});
                                var si=new[]{(int)Get(c,"A"),(int)Get(c,"B"),(int)Get(c,"C")};double b=(float)Get(c,"WeightB"),v=(float)Get(c,"WeightC");var sw=new[]{1-b-v,b,v};
                                for(int k=0;k<3;k++){int local=Array.IndexOf(indices,si[k],face*3,3)-face*3;Require(local>=0&&local<3,"Constructed point left source face");values[local]+=sw[k];}
                                Require(values.All(w=>w>=0&&w<=1),"Constructed point left source triangle");return values;
                            }
                            double Coverage(object owner,int[] corners)
                            {double sum=0;for(int at=0;at<corners.Length;at+=3){var a=Weights(owner,corners[at]);var b=Weights(owner,corners[at+1]);var c=Weights(owner,corners[at+2]);sum+=(b[1]-a[1])*(c[2]-a[2])-(b[2]-a[2])*(c[1]-a[1]);}return sum;}
                            double baryError=Math.Abs(Coverage(before,old)-Coverage(plan,ids));maxBaryError=Math.Max(maxBaryError,baryError);
                            Require(baryError<=16*Math.Pow(2,-24)*(ids.Length/3),"Barycentric source-face coverage changed");
                            if(oldFragment==null)foreach(int vertex in ids)
                            {
                                var w=Weights(plan,vertex);
                                for(int axis=0;axis<3;axis++)
                                {double expected=0,ulp=0;for(int k=0;k<3;k++){expected+=w[k]*bp[indices[face*3+k]][axis];ulp=Math.Max(ulp,Ulp(bp[indices[face*3+k]][axis]));}
                                    double error=Math.Abs(p[vertex][axis]-expected);maxPositionError=Math.Max(maxPositionError,error);
                                    Require(error<=4*ulp,"Native position exceeds float interpolation rounding bound");}
                            }
                            double oldArea=0,newArea=0;for(int at=0;at<old.Length;at+=3)oldArea+=Area(bp[old[at]],bp[old[at+1]],bp[old[at+2]]);
                            for(int at=0;at<ids.Length;at+=3){double area=Area(p[ids[at]],p[ids[at+1]],p[ids[at+2]]);if(area==0)zero++;newArea+=area;triangles++;
                                string detail=string.Join(";",ids.Skip(at).Take(3).Select(v=>p[v].ToString("R")));
                                Require(Vector3.Dot(Vector3.Cross(p[ids[at+1]]-p[ids[at]],p[ids[at+2]]-p[ids[at]]),Vector3.Cross(source[indices[face*3+1]]-source[indices[face*3]],source[indices[face*3+2]]-source[indices[face*3]]))>0,$"Winding changed or zero face lod={lod} delta={delta} face={face} area={area:R} positions={detail}");}
                            maxAreaError=Math.Max(maxAreaError,Math.Abs(oldArea-newArea));
                            // Native float positions have source-coordinate ULP error. Coverage
                            // is checked in barycentric space above; retain the actual area error.
                        }
                        var oldUsed=allowed.SelectMany(face=>beforeFragments.TryGetValue(face,out var f)?(int[])Get(f,"Indices"):indices.Skip(face*3).Take(3)).Distinct().ToArray();
                        var newUsed=fragments.SelectMany(f=>(int[])Get(f,"Indices")).Distinct().ToArray();
                        for(int axis=0;axis<3;axis++)Require(oldUsed.Min(v=>bp[v][axis])==newUsed.Min(v=>p[v][axis])&&oldUsed.Max(v=>bp[v][axis])==newUsed.Max(v=>p[v][axis]),"Authorized structural bounds changed");
                        // Isolate authorized faces only for native Retile; keep the real complete
                        // style's positions, constructed attributes, UV corrections and membership.
                        var masks=((bool[][])Get(plan,"RemoveTriangles"));for(int i=0;i<masks[0].Length;i++)masks[0][i]=true;
                        var allFragments=(Array)Get(plan,"Fragments");var isolated=Array.CreateInstance(allFragments.GetType().GetElementType(),fragments.Length);for(int i=0;i<fragments.Length;i++)isolated.SetValue(fragments[i],i);
                        plan.GetType().GetField("Fragments",I).SetValue(plan,isolated);
                        object[] build={"Lift05 native continuous",mesh,plan,3,null,null,false};
                        Require((bool)T("MovableStructureModel").GetMethod("TryBuild",S).Invoke(null,build),"Native Build failed: "+build[5]);built=build[4];
                        var outputAttrs=new[]{1,2,13}.ToDictionary(a=>a,a=>Bytes(Attr(built,(VertexAttribute)a)));
                        var uvBytes=Bytes(Attr(built,VertexAttribute.TexCoord0));
                        foreach(var fragment in fragments)
                        {
                            int face=(int)Get(fragment,"Triangle");foreach(int vertex in (int[])Get(fragment,"Indices"))
                            {
                                foreach(int attribute in new[]{1,2,13})for(int b=0;b<4;b++)Require(outputAttrs[attribute][vertex*4+b]==attrs[attribute][indices[face*3]*4+b],"Packed original normal/tangent/bone changed");
                                Require(float.IsFinite(BitConverter.ToSingle(uvBytes,vertex*8))&&float.IsFinite(BitConverter.ToSingle(uvBytes,vertex*8+4)),"Nonfinite native UV");
                            }
                        }
                        var owned=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(built.GetType()));var models=(IList)Activator.CreateInstance(owned.GetType());var surfaces=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
                        T("MovableStructureRetile").GetMethod("Append",S).Invoke(null,new[]{"Lift05 native continuous",lod==0?"LiftBridge05 Mesh":"LiftBridge05_LOD1 Mesh",(object)0,plan,built,surface,New(tilesType,"Lift05"),owned,models,surfaces});
                        Require(models.Count==1,"Expected one isolated profile material");
                        Require(surfaces.Count==1&&ReferenceEquals(surfaces[0],delta==0?surface:finished),"Wrong retained/prepared Surface");
                        var result=models[0];uploaded=(Mesh)result.GetType().GetMethod("ToUnityMesh",I).Invoke(result,new object[]{true});
                        var uploadedUv=new List<Vector2>();uploaded.GetUVs(0,uploadedUv);
                        var sequence=fragments.SelectMany(f=>(int[])Get(f,"Indices")).ToArray();
                        Require(uploaded.triangles.Length==sequence.Length,"Native retile introduced period triangles");
                        var recipe=T("MovableUvDomains").GetMethod("For",S).Invoke(null,new object[]{lod==0?"LiftBridge05 Mesh":"LiftBridge05_LOD1 Mesh",0});
                        var domains=((IEnumerable)Get(recipe,"Items")).Cast<object>().ToArray();var vertexUv=new Dictionary<int,Vector2>();
                        foreach(var pd in prepared.Values)foreach(var pair in (IReadOnlyDictionary<int,Vector2>)Get(pd,"Uvs"))vertexUv[pair.Key]=pair.Value;
                        using(var read=Mesh.AcquireReadOnlyMeshData(uploaded))
                        for(int at=0;at<sequence.Length;at++)
                        {
                            int sourceVertex=sequence[at],v=uploaded.triangles[at];Require(uploaded.vertices[v].Equals(p[sourceVertex]),"Retile/upload changed position");
                            var expectedUv=delta==0?new Vector2(BitConverter.ToSingle(uvBytes,sourceVertex*8),BitConverter.ToSingle(uvBytes,sourceVertex*8+4)):vertexUv[sourceVertex];
                            Require(uploadedUv[v].Equals(expectedUv),"Retile/upload changed final continuous UV");
                            foreach(var attribute in ((IEnumerable)built).Cast<object>())
                            {
                                var a=(VertexAttribute)Get(attribute,"attribute");if(a==VertexAttribute.TexCoord0)continue;
                                var originalBytes=Bytes(attribute);int size=(int)Get(attribute,"elementSize");
                                int stream=uploaded.GetVertexAttributeStream(a),offset=uploaded.GetVertexAttributeOffset(a),step=uploaded.GetVertexBufferStride(stream);var bytes=read[0].GetVertexData<byte>(stream);
                                for(int by=0;by<size;by++)Require(bytes[v*step+offset+by]==originalBytes[sourceVertex*size+by],"Native upload changed raw/interpolated "+a);
                            }
                        }
                        foreach(var item in owned.Cast<object>())(item as IDisposable)?.Dispose();
                        Debug.Log($"PASS Lift05 native continuous case lod={lod} delta={delta} sourceFaces={faces.Length} preexistingFragments={preexisting} outputTriangles={triangles} zeroArea={zero} maxAreaError={maxAreaError:R} maxBaryError={maxBaryError:R} maxPositionError={maxPositionError:R} preparedDomains={prepared.Count} constructedVertices={Get(plan,"ConstructedCount")} independentPhaseError={maxFixtureError:R} completeTriangles={fullTriangles} completeFragments={existing.Count}");cases++;
                    }
                    finally{(built as IDisposable)?.Dispose();if(uploaded!=null)UnityEngine.Object.DestroyImmediate(uploaded);UnityEngine.Object.DestroyImmediate(mesh);}
                }
            }
            Require(cases==10,"Missing native cases");Debug.Log("PASS Lift05 native structural continuous: 10 actual Plan/Build/Retile/ToUnityMesh cases; exact original normal/tangent/bone bytes; original-width Surface and widened prepared Surface; no period cuts; profile raw channels plus complete-plan existing constructed attributes/upload verified independently; profile domains have no existing fragments. Header-only native Surface fixture; no texture reads/bakes. Native filtered appearance and cold load remain untested.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
