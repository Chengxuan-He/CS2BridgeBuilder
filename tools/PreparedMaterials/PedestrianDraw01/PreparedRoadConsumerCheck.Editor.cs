using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Actual compiled TryBind and Compose consumers. Asset registration is isolated
// by supplying real, native-read descriptors directly; no registration or game-world generation is performed.
public static class PedestrianPreparedRoadConsumerCheck
{
    const BindingFlags Instance=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    const BindingFlags Static=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static object Get(object o,string n)=>o.GetType().GetField(n,Instance).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Instance).SetValue(o,v);
    static object New(Type t,params object[] values)=>Activator.CreateInstance(t,Instance,null,values,null);
    static Array ArrayOf(Type t,params object[] values){var r=Array.CreateInstance(t,values.Length);for(int i=0;i<values.Length;i++)r.SetValue(values[i],i);return r;}
    static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
    [Serializable] public sealed class Case { public int lod,pose,boundFaces,outputFaces,domains,zeroFaces,reversedFaces;public float halfDelta,left,right,maxUvError,maxTangentError;public double area,expectedArea,relativeAreaError,minTriangleArea;public string defects=""; }
    [Serializable] public sealed class Report { public string scope,limitations;public Case[] cases; }
    [Serializable] public sealed class Cell { public int parent,side;public float z0,z1,halfWidth;public float[] u,v; }
    [Serializable] public sealed class CellEvidence { public Cell[] records; }
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();string Arg(string key)=>args[Array.IndexOf(args,key)+1];
            string managed=Arg("-gameManaged");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string name)=>mod.GetType("BridgeBuilder.Bridges."+name,true);
            var vertex=T("MovableRoadGeometry+Vertex");var triangle=T("MovableRoadGeometry+Triangle");var input=T("MovableSurfaceAtlas+Input");
            var surfaceType=input.GetField("Materials",Instance).FieldType.GetElementType();
            object Surface(string path)
            {
                var s=FormatterServices.GetUninitializedObject(surfaceType);
                using(var reader=new BinaryReader(File.OpenRead(path)))surfaceType.GetMethod("ReadHeader",Instance).Invoke(s,new object[]{reader});
                string cid=File.Exists(path+".cid")?File.ReadAllText(path+".cid").Trim():Path.GetFileName(path)=="native-far.Surface"?"0f4e5a3c788ce4bbd5f18bfff2224ea1":"15c63410efe103a46b109b0dd6aa246d";
                var idProperty=surfaceType.GetProperty("id");var hashType=idProperty.PropertyType.GetField("guid").FieldType;
                var hash=hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{cid});
                idProperty.SetValue(s,New(idProperty.PropertyType,hash));return s;
            }
            var mapping=T("PedestrianDraw01NativeRoadCells");
            var nativeNear=Surface(Arg("-nearSurface"));var nativeFar=Surface(Arg("-farSurface"));
            var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));Set(owner,"<StyleId>k__BackingField","PedestrianDraw01");
            var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
            var physical=T("PedestrianDraw01RoadPartitions").GetField("Carriageway",Static).GetValue(null);
            var golden=JsonUtility.FromJson<CellEvidence>(File.ReadAllText(Arg("-cells")));
            var cases=new List<Case>();
            foreach(float delta in new[]{0f,2f,8f})
            foreach(int poseIndex in new[]{0,1,2})
            foreach(bool asymmetric in new[]{false,true})
            {
                var pose=poseIndex==0?new Pose(Vector3.zero,Quaternion.identity):new Pose(new Vector3(3,2,-7),poseIndex==1?Quaternion.Euler(0,31,0):Quaternion.Euler(13,31,-9));
                var matrix=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);var inverse=matrix.inverse;
                float left=asymmetric?-1.75f-delta:-3.5f-delta,right=3.5f+delta;
                object V(float x,float z){var p=inverse.MultiplyPoint3x4(new Vector3(x,10,z));return New(vertex,p,inverse.MultiplyVector(Vector3.up),new Vector4(-1,0,0,-1),new Vector4((x-left)/(right-left)*.5f,(z+76)/152,.23f,.71f),new Color(.2f,.4f,.6f,.8f));}
                var faces=ArrayOf(triangle,New(triangle,V(left,-76),V(left,76),V(right,76),0,2,10),New(triangle,V(left,-76),V(right,76),V(right,-76),0,2,11));
                for(int lod=0;lod<3;lod++)
                {
                    Array patches;object regions;
                    if(lod<2)
                    {
                        patches=(Array)mapping.GetMethod("Patches",Static).Invoke(null,new object[]{lod,nativeNear,pose,delta});
                        var longitudinal=T("PedestrianDraw01RoadAppearance").GetMethod("Regions",Static).Invoke(null,new[]{physical,(object)pose});
                        regions=mapping.GetMethod("Regions",Static).Invoke(null,new object[]{lod,longitudinal,pose});
                    }
                    else
                    {
                        patches=(Array)T("PedestrianDraw01FarRoadMapping").GetMethod("Patches",Static).Invoke(null,new[]{nativeFar,(object)pose,delta});
                        regions=T("PedestrianDraw01FarRoadMapping").GetMethod("Regions",Static).Invoke(null,new[]{physical,(object)pose});
                    }
                    object[] bind={faces,regions,patches.Length,1,null,null,false,null,null};
                    Need((bool)T("MovableRoadGeometry").GetMethod("TryBind",Static).Invoke(null,bind),"Actual measured partition failed: "+bind[5]);
                    var bound=(Array)bind[4];Need(bound.Length>0,"Empty bound road.");
                    var domains=bound.Cast<object>().Select(b=>(uint)Get(b,"Bone")).Distinct().OrderBy(b=>b).ToArray();Need(domains.SequenceEqual(Enumerable.Range(0,patches.Length).Select(x=>(uint)x)),"Missing original appearance domain.");
                    var source=New(input,false);Set(source,"Faces",bound);Set(source,"Materials",ArrayOf(surfaceType,nativeNear));Set(source,"Native",lod<2?nativeNear:nativeFar);Set(source,"Patches",patches);
                    object[] compose={ArrayOf(input,source),owner,paint,null};
                    var output=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,compose)).GetValue(0);
                    var materials=(Array)((Array)compose[3]).GetValue(0);
                    Need(output.Length==bound.Length,"Compose introduced additional tessellation.");
                    var row=new Case{lod=lod,pose=poseIndex,halfDelta=delta,left=left,right=right,boundFaces=bound.Length,outputFaces=output.Length,domains=domains.Length,minTriangleArea=double.MaxValue,expectedArea=(right-left)*(152-2*(60.3857421875-60.28515625)-2*.029998779296875)};
                    for(int i=0;i<bound.Length;i++)
                    {
                        var b=bound.GetValue(i);uint domain=(uint)Get(b,"Bone");var patch=patches.GetValue(domain);var expectedSurface=Get(patch,"Surface");var actual=output.GetValue(i);var before=Get(b,"Geometry");
                        var pa=(Vector3)Get(Get(before,"A"),"Position");var pb=(Vector3)Get(Get(before,"B"),"Position");var pc=(Vector3)Get(Get(before,"C"),"Position");
                        var cross=Vector3.Cross(pb-pa,pc-pa);double faceArea=cross.magnitude*.5;
                        row.area+=faceArea;row.minTriangleArea=Math.Min(row.minTriangleArea,faceArea);
                        if(faceArea<=1e-10)row.zeroFaces++;
                        if(Vector3.Dot(cross,inverse.MultiplyVector(Vector3.up))<=0)
                        {
                            row.reversedFaces++;
                            row.defects+="domain="+domain+" source="+Get(before,"SourceTriangle")+" area="+faceArea+" A="+pa.ToString("R")+" B="+pb.ToString("R")+" C="+pc.ToString("R")+"; ";
                        }
                        var midpoint=matrix.MultiplyPoint3x4(((Vector3)Get(Get(before,"A"),"Position")+(Vector3)Get(Get(before,"B"),"Position")+(Vector3)Get(Get(before,"C"),"Position"))/3);
                        int originalRegion=midpoint.z>60.3857421875f?0:midpoint.z< -60.3857421875f?1:midpoint.z>0?2:3;
                        if(lod<2)
                        {
                            var cell=golden.records[lod*56+(int)domain];
                            Need(cell.parent==originalRegion && cell.side==(midpoint.x>0?1:0),"Physical point reached wrong native half-width cell.");
                            Need(midpoint.z>=cell.z0-.00002f&&midpoint.z<=cell.z1+.00002f,"Physical point crossed an original reset station.");
                        }
                        Need(ReferenceEquals(materials.GetValue((int)Get(actual,"Material")),expectedSurface),"Compose selected the wrong native unit/LOD Surface.");
                        Need((int)Get(actual,"Occurrence")==2&&(int)Get(actual,"SourceTriangle")== (int)Get(before,"SourceTriangle"),"Receiver provenance changed.");
                        foreach(string corner in new[]{"A","B","C"})
                        {
                            var av=Get(actual,corner);var bv=Get(before,corner);var position=(Vector3)Get(av,"Position");Need(position==(Vector3)Get(bv,"Position"),"Compose moved a receiver vertex.");
                            var p4=new Vector4(position.x,position.y,position.z,1);var expected=new Vector4(Vector4.Dot(p4,(Vector4)Get(patch,"U")),Vector4.Dot(p4,(Vector4)Get(patch,"V")),((Vector4)Get(bv,"UV")).z,((Vector4)Get(bv,"UV")).w);
                            Need(((Color)Get(av,"Color")).Equals((Color)Get(bv,"Color")),"Native atlas reuse lost receiver Color.");
                            Need(((Vector3)Get(av,"Normal")).Equals((Vector3)Get(bv,"Normal")),"Native atlas reuse changed receiver Normal.");
                            row.maxUvError=Mathf.Max(row.maxUvError,((Vector4)Get(av,"UV")-expected).magnitude);
                            if(lod<2)
                            {
                                var objectPosition=matrix.MultiplyPoint3x4(position);
                                var cell=golden.records[lod*56+(int)domain];
                                float goldenU=cell.u[0]*cell.halfWidth/(cell.halfWidth+delta)*objectPosition.x+cell.u[3];
                                float goldenV=cell.v[2]*objectPosition.z+cell.v[3];
                                Need(Mathf.Abs(expected.x-goldenU)<.00002f&&Mathf.Abs(expected.y-goldenV)<.00002f,"Original measured cell phase/finite-width projection changed.");
                                var expectedT=Quaternion.Inverse(pose.rotation)*Vector3.left;var actualT=(Vector4)Get(av,"Tangent");
                                row.maxTangentError=Mathf.Max(row.maxTangentError,((Vector3)actualT-expectedT).magnitude);Need(actualT.w==-1,"Native tangent handedness changed.");
                                Need(expected.x>0&&expected.x<1,"Finite U guard escaped.");
                            }
                            else
                            {
                                var expectedT=Quaternion.Inverse(pose.rotation)*(domain==1||domain==6?Vector3.back:Vector3.right);
                                var actualT=(Vector4)Get(av,"Tangent");row.maxTangentError=Mathf.Max(row.maxTangentError,((Vector3)actualT-expectedT).magnitude);
                                Need(actualT.w==-1,"Far native tangent handedness changed.");
                            }
                        }
                    }
                    row.relativeAreaError=Math.Abs(row.area-row.expectedArea)/row.expectedArea;

                    Need(row.maxUvError<.00001f&&row.maxTangentError<.00001f,"Native projection/frame changed in Compose.");
                    var retained=New(input,true);Set(retained,"Faces",bound);Set(retained,"Materials",ArrayOf(surfaceType,nativeNear));Set(retained,"Native",nativeFar);Set(retained,"Patches",patches);
                    object[] retainCall={ArrayOf(input,retained),owner,paint,null};
                    var retainedOutput=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,retainCall)).GetValue(0);
                    Need(retainedOutput.Length==bound.Length,"Selected original object receiver changed triangle count.");
                    for(int i=0;i<bound.Length;i++)Need(retainedOutput.GetValue(i).Equals(Get(bound.GetValue(i),"Geometry")),"Selected original object receiver was remapped.");
                    cases.Add(row);
                }
            }
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(new Report{scope="Actual compiled PedestrianDraw01 partitions, TryBind and Compose; three LODs, three widths, asymmetric widths, no-sidewalk receiver and three poses including yaw/pitch/roll. Original provenance, phases, native Surface identity and frames checked.",limitations="Real native descriptor headers supplied directly; asset registration isolated. Synthetic road receiver fixtures, not game-world Prepare/selected-source classification, animation or saved-owner cold-load acceptance.",cases=cases.ToArray()},true));
            Need(cases.All(c=>c.zeroFaces==0&&c.reversedFaces==0&&c.relativeAreaError<.00001),"Geometry acceptance failed; complete case report retained.");
            Debug.Log("PASS PedestrianDraw01 production TryBind/Compose: "+cases.Count+" cases; original finite cell domains; no composition tessellation or material writer.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
