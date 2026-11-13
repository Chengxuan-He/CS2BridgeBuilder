using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class StructuralFrameCheck
{
    [System.Runtime.InteropServices.DllImport("kernel32", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] private static extern IntPtr LoadLibraryW(string path);
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct PositionColorBone { public Vector3 Position; public uint Color, Bone; }
    const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static object Get(object o, string n) => o.GetType().GetField(n,I)?.GetValue(o) ?? o.GetType().GetProperty(n,I).GetValue(o);
    static void Need(bool b, string m) { if (!b) throw new Exception(m); }
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs(); string Arg(string n)=>args[Array.IndexOf(args,n)+1];
            var managed=Arg("-gameManaged");
            Need(LoadLibraryW(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll"))) != IntPtr.Zero, "Native geometry codec unavailable");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var mod=Assembly.LoadFrom(Arg("-modAssembly"));
            var type=mod.GetType("BridgeBuilder.Bridges.MovableStructureModel",true);
            var planType=type.GetNestedType("Plan",I);
            var ctor=planType.GetConstructors(I).Single();
            var build=type.GetMethod("TryBuild",S);
            var math=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils",true);
            uint Pack(string method,params float[] values)
            {
                var fn=math.GetMethod(method,S);var v=Activator.CreateInstance(fn.GetParameters()[0].ParameterType,values.Cast<object>().ToArray());
                return Convert.ToUInt32(fn.Invoke(null,new[]{v}));
            }
            int checks=0;
            foreach(int mode in new[]{0,1,2}) foreach(bool compact in new[]{false,true}) foreach(float shear in new[]{-.05f,0f,.05f}) foreach(float hand in new[]{-1f,1f}) foreach(float scale in new[]{-1f,0f,1f})
            {
                bool constructed=mode!=0;
                var p=new[]{new Vector3(8,8,8),new Vector3(0,0,0),new Vector3(0,1,0),new Vector3(0,0,1)};
                var uv=Enumerable.Range(0,4).Select(i=>new Vector4(i,.25f,7,9)).ToArray();
                uint n=Pack("NormalToOctahedral",1,0,0),t=Pack("TangentToOctahedral",0,1,0,hand);
                var mesh=new Mesh();mesh.SetVertexBufferParams(4,
                    new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3,0),
                    new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.SNorm16,2,1),
                    new VertexAttributeDescriptor(VertexAttribute.Tangent,VertexAttributeFormat.Float32,1,2),
                    new VertexAttributeDescriptor(VertexAttribute.Color,VertexAttributeFormat.UNorm8,4,0),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0,mode==2?VertexAttributeFormat.Float16:VertexAttributeFormat.Float32,4,3),
                    new VertexAttributeDescriptor(VertexAttribute.BlendIndices,VertexAttributeFormat.UInt32,1,0));
                mesh.SetVertexBufferData(p.Select(v=>new PositionColorBone{Position=v,Color=0x12345678,Bone=1}).ToArray(),0,0,4,0);mesh.SetVertexBufferData(Enumerable.Repeat(n,4).ToArray(),0,0,4,1);
                mesh.SetVertexBufferData(Enumerable.Repeat(t,4).ToArray(),0,0,4,2);if(mode==2)mesh.SetVertexBufferData(new ushort[]{0,0x3400,0x4700,0x4880,0x3c00,0x3400,0x4700,0x4880,0x4000,0x3400,0x4700,0x4880,0x4200,0x3400,0x4700,0x4880},0,0,16,3);
                else mesh.SetVertexBufferData(uv,0,0,4,3);
                mesh.SetIndices(new[]{1,2,3},MeshTopology.Triangles,0);
                var jac=Matrix4x4.identity;jac.m01=shear;jac.m00=scale;
                var positions=(Vector3[])p.Clone();positions[1]=jac.MultiplyPoint3x4(p[1]);positions[2]=jac.MultiplyPoint3x4(p[2]);
                object Plan(KeyValuePair<int,Matrix4x4>[] frames)
                {
                    var a=ctor.GetParameters().Select(q=>q.HasDefaultValue?q.DefaultValue:null).ToArray();
                    a[0]=p;a[1]=positions;a[2]=new[]{new[]{1,2,3}};a[3]=new[]{new[]{constructed}};
                    if(mode==1)
                    {
                        var edgeType=type.GetNestedType("EdgeVertex",I);var edges=Array.CreateInstance(edgeType,2);
                        for(int i=0;i<2;i++)edges.SetValue(Activator.CreateInstance(edgeType,I,null,new object[]{1,2,.5f},null),i);
                        a[4]=edges;
                        var fragmentType=type.GetNestedType("Fragment",I);var fragments=Array.CreateInstance(fragmentType,2);
                        fragments.SetValue(Activator.CreateInstance(fragmentType,I,null,new object[]{0,0,new[]{1,4,3}},null),0);
                        fragments.SetValue(Activator.CreateInstance(fragmentType,I,null,new object[]{0,0,new[]{5,2,3}},null),1);a[5]=fragments;
                    }
                    if(mode==2)
                    {
                        a[Array.FindIndex(ctor.GetParameters(),q=>q.Name=="sourceCopies")]=new[]{1,1};
                        a[7]=new[]{.00624f,.01248f};
                        var ft=type.GetNestedType("Fragment",I);var fs=Array.CreateInstance(ft,1);
                        fs.SetValue(Activator.CreateInstance(ft,I,null,new object[]{0,0,new[]{4,2,3}},null),0);a[5]=fs;
                    }
                    a[Array.FindIndex(ctor.GetParameters(),q=>q.Name=="frameTransforms")]=frames;
                    return ctor.Invoke(a);
                }
                object Build(object plan)
                {
                    object[] a={"frame fixture",mesh,plan,2,null,null,compact,null,null};
                    Need((bool)build.Invoke(null,a),"Native Build failed: "+a[5]);return a[4];
                }
                var corrections=new List<KeyValuePair<int,Matrix4x4>>{new KeyValuePair<int,Matrix4x4>(1,jac),new KeyValuePair<int,Matrix4x4>(2,jac)};
                if(constructed){corrections.Add(new KeyValuePair<int,Matrix4x4>(4,mode==2?Matrix4x4.identity:jac));corrections.Add(new KeyValuePair<int,Matrix4x4>(5,Matrix4x4.identity));}
                var baseline=Build(Plan(null));var changed=Build(Plan(corrections.ToArray()));
                byte[] Raw(object model,VertexAttribute kind)=>((IEnumerable)Get(((IEnumerable)model).Cast<object>().Single(a=>(VertexAttribute)Get(a,"attribute")==kind),"data")).Cast<byte>().ToArray();
                if (mode == 0 && !compact)
                {
                    var fixedBuild=mod.GetType("BridgeBuilder.Bridges.TowerFactory",true).GetMethod("BuildModel",S);
                    var pointType=fixedBuild.GetParameters()[2].ParameterType.GetElementType();
                    var points=Array.CreateInstance(pointType,positions.Length);
                    for(int k=0;k<positions.Length;k++) points.SetValue(Activator.CreateInstance(pointType,new object[]{positions[k].x,positions[k].y,positions[k].z}),k);
                    var fixedArgs = new object[fixedBuild.GetParameters().Length];
                    fixedArgs[0]="fixed frame fixture";fixedArgs[1]=mesh;fixedArgs[2]=points;fixedArgs[5]=corrections.ToArray();
                    var fixedModel=fixedBuild.Invoke(null,fixedArgs); Need(fixedModel!=null,"Fixed model build failed: "+fixedArgs[3]);
                    foreach(var attr in new[]{VertexAttribute.Position,VertexAttribute.Normal,VertexAttribute.Tangent,VertexAttribute.TexCoord0,VertexAttribute.Color,VertexAttribute.BlendIndices})
                        Need(Raw(fixedModel,attr).SequenceEqual(Raw(changed,attr)),"Fixed/native frame mismatch: "+attr);
                    ((IDisposable)fixedModel).Dispose();
                    var remappedPoints=Array.CreateInstance(pointType,5);
                    for(int k=0;k<5;k++)remappedPoints.SetValue(points.GetValue(k==4?1:k),k);
                    var remappedFrames=corrections.Concat(new[]{new KeyValuePair<int,Matrix4x4>(4,jac)}).ToArray();
                    fixedArgs[2]=remappedPoints;fixedArgs[5]=remappedFrames;
                    fixedArgs[7]=new[]{0,1,2,3,1};fixedArgs[8]=new[]{4,2,3};
                    var remapped=fixedBuild.Invoke(null,fixedArgs);Need(remapped!=null,"Recorded provenance build failed");
                    foreach(var attr in new[]{VertexAttribute.Position,VertexAttribute.Normal,VertexAttribute.Tangent,VertexAttribute.TexCoord0,VertexAttribute.Color,VertexAttribute.BlendIndices})
                    {
                        var bytes=Raw(remapped,attr);var stride=bytes.Length/5;
                        Need(bytes.Skip(stride).Take(stride).SequenceEqual(bytes.Skip(4*stride).Take(stride)),"Recorded source twin channel mismatch: "+attr);
                    }
                    var remappedMesh=(Mesh)remapped.GetType().GetMethod("ToUnityMesh",I).Invoke(remapped,new object[]{true});
                    Need(remappedMesh.triangles.SequenceEqual(new[]{4,2,3}),"Recorded corner topology changed");
                    UnityEngine.Object.DestroyImmediate(remappedMesh);((IDisposable)remapped).Dispose();
                }
                foreach(var attr in new[]{VertexAttribute.Position,VertexAttribute.TexCoord0,VertexAttribute.Color,VertexAttribute.BlendIndices})Need(Raw(baseline,attr).SequenceEqual(Raw(changed,attr)),"Unrelated channel changed");
                var nn=scale==0?Vector3.right:new Vector3(1/scale,-shear/scale,0).normalized;var tt=scale==0?Vector3.up:new Vector3(shear,1,0).normalized;
                uint expectedN=Pack("NormalToOctahedral",nn.x,nn.y,nn.z),expectedT=Pack("TangentToOctahedral",tt.x,tt.y,tt.z,scale<0?-hand:hand);
                var nb=Raw(changed,VertexAttribute.Normal);var tb=Raw(changed,VertexAttribute.Tangent);
                var used=mode==2?new[]{2,3,4}:Enumerable.Range(1,constructed?5:3).ToArray();
                for(int j=0;j<(compact?used.Length:(constructed?6:4));j++)
                {
                    int original=compact?used[j]:j;bool affected=original==1||original==2||(original==4&&mode==1);
                    Need(BitConverter.ToUInt32(nb,j*4)==(affected?expectedN:n),"Inverse-transpose native N mismatch");
                    Need(BitConverter.ToUInt32(tb,j*4)==(affected?expectedT:t),"Forward/orthogonal native T or handedness mismatch");
                }
                if(mode==2)
                {
                    var ua=((IEnumerable)changed).Cast<object>().Single(a=>(VertexAttribute)Get(a,"attribute")==VertexAttribute.TexCoord0);
                    Need((VertexAttributeFormat)Get(ua,"format")==VertexAttributeFormat.Float16,"Source-only copies promoted native UV format");
                    var bytes=Raw(changed,VertexAttribute.TexCoord0);int copy=compact?2:4;
                    Need(BitConverter.ToUInt16(bytes,copy*8)==0x3c00 && BitConverter.ToUInt16(bytes,copy*8+4)==0x4700 && BitConverter.ToUInt16(bytes,copy*8+6)==0x4880,"Source copy UV4 raw bytes changed");
                }
                var uploaded=(Mesh)changed.GetType().GetMethod("ToUnityMesh",I).Invoke(changed,new object[]{true});
                Need(uploaded.triangles.Length==(mode==1?6:3),"Topology changed on upload");
                if(mode==1)
                {
                    var vertices=uploaded.vertices;int offset=compact?1:0;
                    Need(vertices[4-offset]==vertices[5-offset],"Independent frame twins moved apart");
                }
                foreach(var bad in new[]{Matrix4x4.zero})
                {bool rejected=false;try{Plan(new[]{new KeyValuePair<int,Matrix4x4>(1,bad)});}catch(TargetInvocationException){rejected=true;}Need(rejected,"Invalid frame admitted");}
                var nan=Matrix4x4.identity;nan.m01=float.NaN;
                foreach(var invalid in new[]{new[]{new KeyValuePair<int,Matrix4x4>(1,nan)},new[]{new KeyValuePair<int,Matrix4x4>(constructed?6:4,jac)},new[]{new KeyValuePair<int,Matrix4x4>(1,jac),new KeyValuePair<int,Matrix4x4>(1,jac)}})
                {bool rejected=false;try{Plan(invalid);}catch(TargetInvocationException){rejected=true;}Need(rejected,"Invalid frame membership admitted");}
                ((IDisposable)baseline).Dispose();((IDisposable)changed).Dispose();UnityEngine.Object.DestroyImmediate(uploaded);UnityEngine.Object.DestroyImmediate(mesh);checks++;
            }
            Debug.Log("PASS structural frame native Build/upload: "+checks+" source/constructed/copy/compact/shear/handedness cases; coincident constructed frames independent; unlisted N/T and unrelated channels unchanged; signed and zero-scale native frames preserved; malformed/nonfinite/index negatives rejected.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
