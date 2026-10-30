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

// Native integration fixture: execute the unchanged compiled production Build
// and Retile, with explicit test-only classification/domain rows. Restore every
// injected row. This tests rendering data, not prototype classification evidence.
public static class IndependentAxesCheck
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct PNT { public Vector3 Position; public uint Normal; public float Tangent; }
    const BindingFlags Instance=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    const BindingFlags Static=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static object Get(object value,string name)=>value.GetType().GetField(name,Instance)?.GetValue(value)
        ?? value.GetType().GetProperty(name,Instance).GetValue(value);
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static byte[] Bytes(object attribute)=>((IEnumerable)Get(attribute,"data")).Cast<byte>().ToArray();
    static object Attribute(object model,VertexAttribute kind)=>((IEnumerable)model).Cast<object>()
        .Single(a=>(VertexAttribute)Get(a,"attribute")==kind);
    static object New(Type type,params object[] args)=>Activator.CreateInstance(type,Instance,null,args,null);
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();
            string Arg(string key)=>args[Array.IndexOf(args,key)+1];
            var managed=Arg("-gameManaged");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>
            {
                string file=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
                return File.Exists(file)?Assembly.LoadFrom(file):null;
            };
            var mod=Assembly.LoadFrom(Arg("-modAssembly"));
            using(var sha=System.Security.Cryptography.SHA256.Create())
                Debug.Log("Native independent-axis runtime SHA256: "+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(mod.Location))).Replace("-",""));
            Type T(string name)=>mod.GetType("BridgeBuilder.Bridges."+name,true);
            var domainType=T("MovableUvDomains");
            var domains=(IDictionary)domainType.GetField("Loaded",Static).GetValue(null);
            var sampling=(int[][])T("Lift04UvSampling+Data").GetField("Items",Static).GetValue(null);
            var charts=(Array)T("Lift04UvCharts+Data").GetField("Items",Static).GetValue(null);
            var tileType=T("MovableStructureTiles");
            var surfaceType=tileType.GetMethod("NativeSurface",Instance).ReturnType;
            var surface=FormatterServices.GetUninitializedObject(surfaceType);
            using(var reader=new BinaryReader(File.OpenRead(Arg("-surfaceFile"))))
                surfaceType.GetMethod("ReadHeader",Instance).Invoke(surface,new object[]{reader});
            var mathematics=Assembly.LoadFrom(Path.Combine(managed,"Unity.Mathematics.dll"));
            var mathUtils=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils");
            Debug.Log("OCT METHODS "+string.Join(";",mathUtils.GetMethods(Static).Where(m=>m.Name.Contains("Octahedral")).Select(m=>m.ToString())));
            object Float3(float x,float y,float z)=>Activator.CreateInstance(mathematics.GetType("Unity.Mathematics.float3"),new object[]{x,y,z});
            object Float4(float x,float y,float z,float w)=>Activator.CreateInstance(mathematics.GetType("Unity.Mathematics.float4"),new object[]{x,y,z,w});
            uint packedNormal=Convert.ToUInt32(mathUtils.GetMethod("NormalToOctahedral",Static).Invoke(null,new[]{Float3(0,1,0)}));
            float packedTangent=BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToUInt32(mathUtils.GetMethod("TangentToOctahedral",Static).Invoke(null,new[]{Float4(1,0,0,-1)}))),0);
            int cases=0,vertices=0;
            var names=new[]{"LiftBridge04 Mesh","LiftBridge04_LOD1 Mesh","LiftBridge04_LOD2 Mesh"};
            var rows=new[]{0,2,4};
            for(int lod=0;lod<3;lod++)
            {
                int row=rows[lod];string render=names[lod];
                string key="UvDomain_"+render.Replace(' ','_')+"_0";
                object previous=domains.Contains(key)?domains[key]:null;
                int[] savedSampling=sampling[row];object savedCharts=charts.GetValue(row);
                var items=Array.CreateInstance(T("MovableUvDomains+Domain"),2);
                for(int d=0;d<2;d++)items.SetValue(New(items.GetType().GetElementType(),
                    d*3,d*3+1,d*3+2,new[]{d},0,1,new Vector4(0,0,1,1),false),d);
                var recipe=FormatterServices.GetUninitializedObject(domainType);
                domainType.GetField("Items",Instance).SetValue(recipe,items);domains[key]=recipe;
                var emptyCharts=Array.CreateInstance(savedCharts.GetType().GetElementType(),2);
                for(int d=0;d<2;d++)emptyCharts.SetValue(Array.CreateInstance(T("MovableUvChart"),0),d);
                charts.SetValue(emptyCharts,row);
                try
                {
                    foreach(bool half in new[]{false,true})
                    foreach(bool fragment in new[]{false,true})
                    foreach(float scale in new[]{1f,2f,4f})
                    for(int mode=0;mode<4;mode++)
                    {
                        // All source, all density, and mixed domains sharing the
                        // identical native Surface. No missing-source fallback.
                        sampling[row]=new[]{mode,0};
                        var source=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(0,0,1),
                            new Vector3(0,0,4),new Vector3(1,0,4),new Vector3(0,0,5)};
                        var target=source.Select(p=>new Vector3(p.x*scale,p.y,p.z)).ToArray();
                        var uv=new[]{new Vector2(.125f,.125f),new Vector2(.25f,.125f),new Vector2(.125f,.25f),
                            new Vector2(.125f,.125f),new Vector2(.25f,.125f),new Vector2(.125f,.25f)};
                        var zw=Enumerable.Range(0,6).Select(i=>new Vector2(.375f+i*.03125f,1)).ToList();
                        var mesh=new Mesh();object built=null;IList owned=null;
                        try
                        {
                            mesh.SetVertexBufferParams(6,
                                new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3,0),
                                new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.SNorm16,2,0),
                                new VertexAttributeDescriptor(VertexAttribute.Tangent,VertexAttributeFormat.Float32,1,0),
                                new VertexAttributeDescriptor(VertexAttribute.TexCoord0,half?VertexAttributeFormat.Float16:VertexAttributeFormat.Float32,4,1),
                                new VertexAttributeDescriptor(VertexAttribute.BlendIndices,VertexAttributeFormat.UInt32,1,2),
                                new VertexAttributeDescriptor(VertexAttribute.Color,VertexAttributeFormat.UNorm8,4,3));
                            mesh.SetVertexBufferData(source.Select(p=>new PNT{Position=p,Normal=packedNormal,Tangent=packedTangent}).ToArray(),0,0,6,0);
                            if(half)
                            {
                                var xy=new ushort[]{0x3000,0x3000,0x3400,0x3000,0x3000,0x3400,0x3000,0x3000,0x3400,0x3000,0x3000,0x3400};
                                var z=new ushort[]{0x3600,0x3680,0x3700,0x3780,0x3800,0x3840};
                                mesh.SetVertexBufferData(Enumerable.Range(0,6).SelectMany(i=>new[]{xy[i*2],xy[i*2+1],z[i],(ushort)0x3c00}).ToArray(),0,0,24,1);
                            }
                            else mesh.SetVertexBufferData(uv.Select((q,i)=>new Vector4(q.x,q.y,zw[i].x,zw[i].y)).ToArray(),0,0,6,1);
                            mesh.SetVertexBufferData(Enumerable.Repeat(2u,6).ToArray(),0,0,6,2);
                            mesh.SetVertexBufferData(Enumerable.Repeat(0x12345678u,6).ToArray(),0,0,6,3);


                            mesh.SetIndices(new[]{0,1,2,3,4,5},MeshTopology.Triangles,0);
                            var edges=Array.CreateInstance(T("MovableStructureModel+EdgeVertex"),0);
                            var interiors=Array.CreateInstance(T("MovableStructureModel+TriangleVertex"),fragment?1:0);
                            var fragments=Array.CreateInstance(T("MovableStructureModel+Fragment"),fragment?1:0);
                            if(fragment)
                            {
                                interiors.SetValue(New(interiors.GetType().GetElementType(),0,1,2,.25f,.25f),0);
                                fragments.SetValue(New(fragments.GetType().GetElementType(),0,0,new[]{0,1,6,1,2,6,2,0,6}),0);
                            }
                            uv[0]=new Vector2(.125f,.15625f);
                            var plan=New(T("MovableStructureModel+Plan"),source,target,new[]{new[]{0,1,2,3,4,5}},
                                new[]{new bool[2]},edges,fragments,interiors,null,false,
                                new[]{new KeyValuePair<int,Vector2>(0,uv[0])});
                            object[] buildArgs={"Source UV native fixture",mesh,plan,5,null,null,false};
                            Require((bool)T("MovableStructureModel").GetMethod("TryBuild",Static).Invoke(null,buildArgs),"Build failed: "+buildArgs[5]);
                            built=buildArgs[4];
                            var positions=(Vector3[])Get(plan,"Positions");
                            var sourceUv=uv.ToList();
                            var sourcePositions=source.ToList();
                            if(fragment)
                            {
                                sourceUv.Add(uv[0]+(uv[1]-uv[0])*.25f+(uv[2]-uv[0])*.25f);
                                sourcePositions.Add(source[0]+(source[1]-source[0])*.25f+(source[2]-source[0])*.25f);
                                zw.Add(zw[0]+(zw[1]-zw[0])*.25f+(zw[2]-zw[0])*.25f);
                            }
                            owned=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(built.GetType()));
                            var models=(IList)Activator.CreateInstance(owned.GetType());
                            var surfaces=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
                            var tiles=New(tileType,"Lift04");
                            ((IDictionary)Get(tiles,"_nativeSurfaces")).Add(surface,surface);
                            T("MovableStructureRetile").GetMethod("Append",Static).Invoke(null,new[]{
                                "Source UV native fixture",render,(object)0,plan,built,surface,tiles,owned,models,surfaces});
                            Require(models.Count==1&&surfaces.Count==1&&ReferenceEquals(surfaces[0],surface),"Native Surface identity/batch changed");
                            var output=models[0];
                            Require((int)Get(output,"indexCount")== (fragment?12:6),"Physical triangle count changed");
                            var pbytes=Bytes(Attribute(output,VertexAttribute.Position));
                            var uattr=Attribute(output,VertexAttribute.TexCoord0);var ubytes=Bytes(uattr);
                            bool outputHalf=(VertexAttributeFormat)Get(uattr,"format")==VertexAttributeFormat.Float16;
                            var bones=Bytes(Attribute(output,VertexAttribute.BlendIndices));
                            var colors=Bytes(Attribute(output,VertexAttribute.Color));
                            var outPositions=new List<Vector3>();
                            var expectedUvs=new List<Vector4>(); var densityUvs=new List<Vector2>();
                            for(int v=0;v<(int)Get(output,"vertexCount");v++)
                            {
                                var p=new Vector3(BitConverter.ToSingle(pbytes,v*12),BitConverter.ToSingle(pbytes,v*12+4),BitConverter.ToSingle(pbytes,v*12+8));
                                int original=Array.IndexOf(positions,p);Require(original>=0,"Position changed");outPositions.Add(p);
                                int domain=original>=3&&original<6?1:0;
                                var expected=sourceUv[original];
                                if(sampling[row][domain]!=3)
                                {
                                    float dx=p.x-sourcePositions[original].x;
                                    var deltaUv=new Vector2(.125f,domain==0?-.03125f:0)*dx;
                                    if((sampling[row][domain]&1)==0)expected.x+=deltaUv.x;
                                    if((sampling[row][domain]&2)==0)expected.y+=deltaUv.y;
                                }
                                densityUvs.Add(expected);
                                // Half outputs only occur on untouched source paths;
                                // fixture values are exactly representable in Half.
                                float Read(int axis)
                                {
                                    if(!outputHalf)return BitConverter.ToSingle(ubytes,v*16+axis*4);
                                    ushort h=BitConverter.ToUInt16(ubytes,v*8+axis*2);
                                    int sign=(h&0x8000)==0?1:-1,exponent=(h>>10)&31,mantissa=h&1023;
                                    return exponent==0?sign*mantissa*(float)Math.Pow(2,-24):sign*(1+mantissa/1024f)*(float)Math.Pow(2,exponent-15);
                                }
                                Require(((sampling[row][domain]&1)==0||Math.Abs(Read(0)-expected.x)<1e-7f)&&((sampling[row][domain]&2)==0||Math.Abs(Read(1)-expected.y)<1e-7f),$"Native output UV mismatch lod={lod} half={half} fragment={fragment} scale={scale} mode={mode} v={v} original={original} actual={Read(0)},{Read(1)} expected={expected}");
                                expectedUvs.Add(new Vector4(Read(0),Read(1),Read(2),Read(3)));
                                Require(Read(2)==zw[original].x&&Read(3)==zw[original].y,"Native UV0.ZW source/interpolation changed");
                                Require(BitConverter.ToUInt32(bones,v*4)==2,"Bone changed");
                                Require(BitConverter.ToUInt32(colors,v*4)==0x12345678u,"Raw color channel changed");vertices++;
                            }
                            var actualIndices=((IEnumerable)Get(output,"indices")).Cast<int>().ToArray();
                            for(int at=0;at<actualIndices.Length;at+=3) for(int corner=1;corner<3;corner++)
                            { int a=actualIndices[at],b=actualIndices[at+corner]; Require(((Vector2)(expectedUvs[b]-expectedUvs[a])-(densityUvs[b]-densityUvs[a])).sqrMagnitude<1e-12f,"Native face gradient mismatch"); }
                            var normalBytes=Bytes(Attribute(output,VertexAttribute.Normal));
                            var tangentBytes=Bytes(Attribute(output,VertexAttribute.Tangent));
                            for(int at=0;at<actualIndices.Length;at+=3)
                            {
                                int ia=actualIndices[at],ib=actualIndices[at+1],ic=actualIndices[at+2];
                                int oa=Array.IndexOf(positions,outPositions[ia]),ob=Array.IndexOf(positions,outPositions[ib]),oc=Array.IndexOf(positions,outPositions[ic]);
                                int domain=oa>=3&&oa<6?1:0;
                                Vector3 Du(Vector3 a,Vector3 b,Vector3 c,Vector2 ua,Vector2 ub,Vector2 uc)
                                {var u=ub-ua;var v=uc-ua;return ((b-a)*v.y-(c-a)*u.y)/(u.x*v.y-u.y*v.x);}
                                var oldDu=Du(sourcePositions[oa],sourcePositions[ob],sourcePositions[oc],sourceUv[oa],sourceUv[ob],sourceUv[oc]);
                                var newDu=Du(outPositions[ia],outPositions[ib],outPositions[ic],densityUvs[ia],densityUvs[ib],densityUvs[ic]);
                                float angle=(sampling[row][domain]==1||sampling[row][domain]==2)?Mathf.Atan2(newDu.z,newDu.x)-Mathf.Atan2(oldDu.z,oldDu.x):0;
                                uint expectedTangent=Convert.ToUInt32(mathUtils.GetMethod("TangentToOctahedral",Static).Invoke(null,new[]{Float4(Mathf.Cos(angle),0,Mathf.Sin(angle),-1)}));
                                foreach(int index in new[]{ia,ib,ic})
                                {
                                    Require(BitConverter.ToUInt32(normalBytes,index*4)==packedNormal,"Mixed axis changed native normal direction");
                                    Require(BitConverter.ToUInt32(tangentBytes,index*4)==expectedTangent,$"Mixed tangent Jacobian mismatch mode={mode} scale={scale} fragment={fragment} domain={domain} expected={expectedTangent:X8} actual={BitConverter.ToUInt32(tangentBytes,index*4):X8}");
                                }
                            }
                            var expectedIndices=fragment?new[]{0,1,6,1,2,6,2,0,6,3,4,5}:new[]{0,1,2,3,4,5};
                            Require(actualIndices.Select(i=>outPositions[i]).SequenceEqual(expectedIndices.Select(i=>positions[i])),"Native output topology changed");
                            var uploaded=(Mesh)output.GetType().GetMethod("ToUnityMesh",Instance).Invoke(output,new object[]{true});
                            var uploadedUvs=new List<Vector4>();uploaded.GetUVs(0,uploadedUvs);
                            var uploadedColors=new List<Color32>();uploaded.GetColors(uploadedColors);
                            Require(uploadedColors.Count==expectedUvs.Count && uploadedColors.All(c=>c.r==0x78&&c.g==0x56&&c.b==0x34&&c.a==0x12),"Native upload changed raw COLOR");
                            Require(uploaded.vertices.SequenceEqual(outPositions)&&uploaded.triangles.SequenceEqual(actualIndices),"Native upload changed geometry/topology");
                            Require(uploadedUvs.Count==expectedUvs.Count&&uploadedUvs.Zip(expectedUvs,(a,b)=>(a-b).sqrMagnitude).All(e=>e<1e-14f),"Native upload changed retained/density UVs");

                            cases++;
                        }
                        finally
                        {
                            if(owned!=null)foreach(IDisposable model in owned)model.Dispose();
                            (built as IDisposable)?.Dispose();UnityEngine.Object.DestroyImmediate(mesh);
                        }
                    }
                }
                finally
                {
                    sampling[row]=savedSampling;charts.SetValue(savedCharts,row);
                    if(previous==null)domains.Remove(key);else domains[key]=previous;
                }
            }
            // A finite endpoint must survive a noisy parent inverse, while a
            // genuinely out-of-range authored sample still fails the same guard.
            var unit=New(T("MovableStructurePacking+Unit"));
            unit.GetType().GetField("Bounds",Instance).SetValue(unit,new Vector4(0,0,1,1));
            unit.GetType().GetField("RetainedAxes",Instance).SetValue(unit,2);
            var points=(List<Vector2>)Get(unit,"Points");points.AddRange(new[]{new Vector2(0,0),new Vector2(4,0),new Vector2(0,1.000001f)});
            var exact=new Dictionary<Vector2,Vector2>();foreach(var q in points)exact.Add(q,new Vector2(q.x,q.y>1?1:q.y));
            unit.GetType().GetField("ExactSourceUvs",Instance).SetValue(unit,exact);
            var region=New(T("MovableRepeatUnits+Region"),"finite-axis-control",0d,0d,1d,1d,0d,0d,1d,1d,true,false);
            var create=T("MovableRepeatUnits+RegionMapping").GetMethod("Create",Static);
            Require(create.Invoke(null,new[]{region,unit})!=null,"Exact finite endpoint lost through parent-coordinate inversion");
            exact[points[2]]=new Vector2(0,1.001f);
            Require(create.Invoke(null,new[]{region,unit})==null,"True authored finite-axis overrun was incorrectly accepted");
            exact[points[2]]=new Vector2(0,1);
            unit.GetType().GetField("RetainedAxes",Instance).SetValue(unit,0);
            Require(create.Invoke(null,new[]{region,unit})==null,"Density-only sampler acquired finite-axis permission");
            Debug.Log($"PASS native independent UV axes: {cases} Build/Retile/ToUnityMesh cases; {vertices} emitted vertices; 3 LOD routes; all four axis masks, original and interpolated corrected UVs; density controls; Float16/Float32; exact positions/topology/bones/raw color; independently checked packed tangent Jacobian/handedness and normal; finite-boundary positive/negative controls; one unchanged native Surface; no texture source IO or pixel creation.");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
