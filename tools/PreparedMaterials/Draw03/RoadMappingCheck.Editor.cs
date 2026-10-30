using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Exercise compiled style mappings with real native Surface headers in an
// isolated catalogue; no scene, prefab registration or bridge construction.
public static class Draw03RoadMappingCheck
{
    const BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,I)?.GetValue(o)??o.GetType().GetProperty(n,I)?.GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
    static void Need(bool ok,string reason){if(!ok)throw new InvalidDataException(reason);}
    static float Sample(object patch,string axis,Vector3 point){var p=(Vector4)Get(patch,axis);return Vector4.Dot(new Vector4(point.x,point.y,point.z,1),p);}
    static int Domain(object partition,Vector3 point)
    {
        if((bool)Get(partition,"IsGap"))return -1;
        var negative=Get(partition,"Negative");var positive=Get(partition,"Positive");
        if(negative==null||positive==null)return (int)Get(partition,"Bone");
        var frame=(Matrix4x4)Get(partition,"ObjectToSeam");
        return Domain(frame.MultiplyPoint3x4(point).z<0?negative:positive,point);
    }
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
            string managed=Arg("-gameManaged"),materials=Arg("-materials");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
            var database=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var st=database.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            var prepared=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials",true);
            prepared.GetMethod("Initialize",S).Invoke(null,new[]{Directory.GetParent(Directory.GetParent(materials).FullName).FullName});
            var catalogue=(IDictionary)prepared.GetField("Surfaces",S).GetValue(null);
            foreach(var name in new[]{"FullPeriodLaneWear","FullPeriodLaneGap","FullPeriodCleanAsphalt","FullPeriodGrating","FarRoadAtlas"})
            {
                var surface=FormatterServices.GetUninitializedObject(st);
                using(var reader=new BinaryReader(File.OpenRead(Path.Combine(materials,"RepeatUnits",name,"Repeat.Surface"))))st.GetMethod("ReadHeader",I).Invoke(surface,new object[]{reader});
                catalogue.Add("Draw03/RepeatUnits/"+name+"/Repeat.Surface",surface);
            }
            var native=FormatterServices.GetUninitializedObject(st);int checks=0,variants=0;
            var vertexType=T("MovableRoadGeometry+Vertex");
            var prepare=T("MovableRoadProjection").GetMethod("Prepared",S);
            int preparedChecks=0;
            int composeChecks=0;
            object New(Type t,params object[] v)=>Activator.CreateInstance(t,I,null,v,null);
            Array Items(Type t,params object[] v){var a=Array.CreateInstance(t,v.Length);for(int n=0;n<v.Length;n++)a.SetValue(v[n],n);return a;}
            var triangleType=T("MovableRoadGeometry+Triangle");var boundType=T("MovableRoadGeometry+BoundTriangle");var inputType=T("MovableSurfaceAtlas+Input");
            var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));
            var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
            foreach(var span in new[]{new Vector2(-12.5f,12.5f),new Vector2(-10,14),new Vector2(-7,8)})
            foreach(var centers in new[]{Array.Empty<float>(),new[]{-4.5f,-1.5f,1.5f,4.5f}})
            foreach(var pose in new[]{new Pose(Vector3.zero,Quaternion.identity),new Pose(new Vector3(2,0,5),Quaternion.Euler(0,90,0))})
            {
                var layout=FormatterServices.GetUninitializedObject(T("MovableSelectedRoadLayout"));
                Vector2 left=new Vector2(span.x,span.x+3),right=new Vector2(span.y-2,span.y);
                Set(layout,"Prototype","Draw03");Set(layout,"CarLaneCenters",centers);Set(layout,"SurfaceSpan",span);Set(layout,"LeftSidewalk",left);Set(layout,"RightSidewalk",right);
                foreach(string mode in new[]{"Near","Far"})
                {
                    Debug.Log($"Draw03 mapping variant {mode}: span={span}, lanes={centers.Length}");
                    if(mode=="Near")
                    {
                        var raw=(Array)T("Draw03RoadAppearance").GetMethod("Patches",S).Invoke(null,new[]{layout,(object)pose,span});
                        foreach(var patch in raw)
                        {
                            var bands=(Array)Get(patch,"Bands");if(bands==null)continue;
                            Debug.Log("Draw03 band coverage: "+bands.Length+"; "+string.Join(",",bands.Cast<object>().Select(b=>Get(b,"Start")+".."+Get(b,"End"))));break;
                        }
                    }
                    var level=T("Draw03PreparedRoad").GetMethod(mode,S).Invoke(null,new[]{native,layout,(object)pose,span});
                    Need(ReferenceEquals(Get(level,"Native"),native),"Original LOD Native identity");
                    var patches=(Array)Get(level,"Patches");var types=(Array)Get(level,"SupportedTypes");Need(patches.Length==types.Length,"One explicit type set per patch");
                    foreach(bool walk in new[]{false,true})
                    foreach(float z in new[]{-95f,-90,-85,-75,-60,-40,-20,20,40,60,75,85,90,95})
                    foreach(bool side in new[]{false,true})
                    for(int step=0;step<=8;step++)
                    {
                        var interval=walk?(side?right:left):(side?new Vector2(0,right.x):new Vector2(left.y,0));
                        float x=Mathf.Lerp(interval.x,interval.y,step/8f);if(x==0)x=side?.0001f:-.0001f;
                        var inverse=Quaternion.Inverse(pose.rotation);
                        var point=inverse*(new Vector3(x,0,z)-pose.position);int domain=Domain(Get(level,walk?"Sidewalk":"Carriageway"),point);if(domain<0)continue;
                        var patch=patches.GetValue(domain);int supported=Convert.ToInt32(types.GetValue(domain));
                        Need((supported&(walk?2:1))!=0,"Per-domain appearance type");Need(Get(patch,"Bands")==null,"No unreduced old bands reach rendering");
                        Need(Get(patch,"Surface")!=null,"Prepared native road Surface");
                        float u=Sample(patch,"U",point),v=Sample(patch,"V",point);Need(float.IsFinite(u)&&float.IsFinite(v),"Finite affine mapping");
                        Need(v>=-0.00001f&&v<=1.00001f,"Finite V escaped source atlas");
                        if(mode=="Near")
                        {
                            float period=1;
                            Need((bool)Get(patch,"PreparedUnit") && !(bool)Get(patch,"SourceAtlasRepeat") && !(bool)Get(patch,"SourceUv"),"Offline unit uses continuous UV without atlas seam cuts");
                            Need((bool)Get(patch,"ProjectTangent"),"Every prepared domain uses its projection frame");
                            var originalUv=new Vector4(.12f,.34f,.56f,.78f);
                            var originalColor=new Color(.2f,.4f,.6f,.8f);
                            var inputVertex=Activator.CreateInstance(vertexType,I,null,new object[]{point,Vector3.up,new Vector4(1,0,0,1),originalUv,originalColor},null);
                            var mapped=prepare.Invoke(null,new[]{inputVertex,patch});
                            var actualUv=(Vector4)Get(mapped,"UV");var actualT=(Vector4)Get(mapped,"Tangent");
                            Need((Vector3)Get(mapped,"Position")==point&&(Vector3)Get(mapped,"Normal")==Vector3.up,"Prepared preserves position/normal");
                            Need((Color)Get(mapped,"Color")==originalColor&&actualUv.z==originalUv.z&&actualUv.w==originalUv.w,"Prepared preserves source Color and UV zw");
                            Need(actualUv.x==u&&actualUv.y==v,"Prepared writes continuous affine coordinates");
                            var ug=((Vector3)(Vector4)Get(patch,"U")).normalized;
                            var vg=((Vector3)(Vector4)Get(patch,"V")).normalized;
                            Need(Vector3.Dot((Vector3)actualT,ug)>.99999f,"Prepared tangent follows increasing source U");
                            Need(Vector3.Dot(Vector3.Cross(Vector3.up,(Vector3)actualT)*actualT.w,vg)>.99999f,"Prepared handedness follows increasing source V");
                            preparedChecks++;
                            if(step==4&&z==20)
                            foreach(float length in new[]{8f,160f,800f})
                            {
                                object Vertex(Vector3 p)=>New(vertexType,p,Vector3.up,new Vector4(1,0,0,1),originalUv,originalColor);
                                var face=New(triangleType,Vertex(point),Vertex(point+inverse*Vector3.right*.01f),Vertex(point+inverse*Vector3.forward*length),0,5,23);
                                var input=New(inputType,false);var surface=Get(patch,"Surface");
                                Set(input,"Faces",Items(boundType,New(boundType,face,0u)));Set(input,"Materials",Items(st,surface));Set(input,"Native",surface);Set(input,"Patches",Items(patch.GetType(),patch));
                                object[] call={Items(inputType,input),owner,paint,null};
                                var result=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",S).Invoke(null,call)).GetValue(0);
                                Need(result.Length==1,"Continuous period count must not add triangles");
                                var output=result.GetValue(0);var slots=(Array)((Array)call[3]).GetValue(0);
                                Need(ReferenceEquals(slots.GetValue(0),surface),"Compose preserves finished Surface binding");
                                var positions=new Vector3[3];var normals=new Vector3[3];var tangents=new Vector4[3];var uvs=new List<Vector4>();var colors=new Color[3];
                                int cornerIndex=0;
                                foreach(string corner in new[]{"A","B","C"})
                                {
                                    var before=Get(face,corner);var after=Get(output,corner);var expected=prepare.Invoke(null,new[]{before,patch});
                                    foreach(string field in new[]{"Position","Normal","Tangent","UV","Color"})Need(Get(after,field).Equals(Get(expected,field)),"Compose differs from actual Prepared: "+field);
                                    positions[cornerIndex]=(Vector3)Get(after,"Position");normals[cornerIndex]=(Vector3)Get(after,"Normal");tangents[cornerIndex]=(Vector4)Get(after,"Tangent");colors[cornerIndex]=(Color)Get(after,"Color");uvs.Add((Vector4)Get(after,"UV"));cornerIndex++;
                                }
                                var mesh=new Mesh();mesh.vertices=positions;mesh.normals=normals;mesh.tangents=tangents;mesh.colors=colors;mesh.SetUVs(0,uvs);mesh.triangles=new[]{0,1,2};mesh.UploadMeshData(false);
                                var readback=new List<Vector4>();mesh.GetUVs(0,readback);
                                Need(mesh.vertexCount==3&&mesh.triangles.Length==3&&readback.SequenceEqual(uvs),"Unity mesh retains continuous UV and original triangle count");
                                UnityEngine.Object.DestroyImmediate(mesh);composeChecks++;
                            }
                            Need((bool)Get(patch,"RepeatU")&&!(bool)Get(patch,"RepeatV"),"Original independent near axes");
                            if(Mathf.Abs(z)<75)Need(Mathf.Abs(Mathf.Abs(Sample(patch,"U",point+inverse*Vector3.forward*8)-u)-period)<.00001f,"Original 8m period");
                        }
                        else Need((bool)Get(patch,"SourceUv")&&!(bool)Get(patch,"Repeat"),"Far finite atlas mapping");
                        checks++;
                    }
                }
                variants++;
            }
            Debug.Log($"PASS Draw03 compiled road mappings: {variants} asymmetric-width/lane variants, {checks} domain/axis/period samples; every used patch has a prepared native Surface; finite far uses source UV; no generation scene. Visual acceptance remains separate.");
            Debug.Log($"PASS actual Prepared consumer: {preparedChecks} vertices retain position/normal/Color/UVzw and projection-aligned tangent frames. This does not certify Compose topology or filtered PBR equivalence.");
            Debug.Log($"PASS actual Compose and Unity mesh upload: {composeChecks} synthetic domain triangles across 8/160/800m; no period cuts, exact Prepared attributes and prepared Surface binding. This does not cover game render or cold load.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
