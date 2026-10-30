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

// Runs the compiled native path in an isolated Editor, never the game or a city.
// Original mesh positions/indices/UV0.xy and Surface headers are extracted inputs.
// Extra UV0.zw/COLOR/bone channels are explicit sentinels, not prototype evidence.
public static class Draw03NativeNetworkCheck
{
    const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    [Serializable] public class Row { public string render,cid,mesh; public int family,lod,template; }
    [Serializable] public class Input { public Row[] rows; }
    static object Get(object o,string name)=>o.GetType().GetField(name,I)?.GetValue(o)??o.GetType().GetProperty(name,I).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,I).SetValue(o,value);
    static object New(Type t,params object[] args)=>Activator.CreateInstance(t,I,null,args,null);
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static byte[] Bytes(object a)=>((IEnumerable)Get(a,"data")).Cast<byte>().ToArray();
    static object Attr(object m,VertexAttribute a)=>((IEnumerable)m).Cast<object>().Single(x=>(VertexAttribute)Get(x,"attribute")==a);
    static bool Bool(object o,string method,params object[] args)=>(bool)o.GetType().GetMethod(method,I).Invoke(o,args);
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];
            string managed=Arg("-gameManaged"),inputs=Arg("-inputs");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var mod=Assembly.LoadFrom(Arg("-modAssembly"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
            var tileType=T("MovableStructureTiles");var surfaceType=tileType.GetMethod("NativeSurface",I).ReturnType;
            var dbAssembly=surfaceType.Assembly;
            var gameDb=FormatterServices.GetUninitializedObject(dbAssembly.GetType("Colossal.IO.AssetDatabase.AssetDatabase`1",true)
                .MakeGenericType(dbAssembly.GetType("Colossal.IO.AssetDatabase.Game",true)));
            var idProperty=surfaceType.GetProperty("id",I);
            object Surface(Row row)
            {
                var surface=FormatterServices.GetUninitializedObject(surfaceType);
                using(var reader=new BinaryReader(File.OpenRead(Path.Combine(inputs,row.cid+".Surface"))))surfaceType.GetMethod("ReadHeader",I).Invoke(surface,new object[]{reader});
                var id=Activator.CreateInstance(idProperty.PropertyType);
                var field=id.GetType().GetFields(I).Single(f=>f.FieldType.FullName=="Colossal.Hash128");
                field.SetValue(id,field.FieldType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{row.cid}));
                idProperty.SetValue(surface,id);surfaceType.GetProperty("database",I).SetValue(surface,gameDb);
                Require((int)Get(surface,"materialTemplateHash")==row.template,"Header template differs");return surface;
            }
            var records=JsonUtility.FromJson<Input>(File.ReadAllText(Path.Combine(inputs,"inputs.json"))).rows;
            int cases=0,totalVertices=0,controls=0;
            foreach(var row in records)
            {
                var surface=Surface(row);var tiles=New(tileType,"Draw03",true);var objectTiles=New(tileType,"Draw03",false);
                Require(Bool(tiles,"PreserveNativeNetwork",row.render,0,surface),"Native identity rejected");
                Require(!Bool(objectTiles,"PreserveNativeNetwork",row.render,0,surface),"Object context admitted");
                Require(!Bool(tiles,"NativeNetworkIdentity",row.render,1,row.cid),"Wrong mesh admitted");
                Require(!Bool(tiles,"NativeNetworkIdentity",row.render,0,new string('0',32)),"Wrong CID admitted");
                Require(!Bool(tiles,"NativeNetworkIdentity","DrawBridge03 Mesh",0,row.cid),"Mechanism admitted");controls+=4;
                Set(surface,"m_MaterialTemplateHash",0);
                bool failed=false;try{Bool(tiles,"PreserveNativeNetwork",row.render,0,surface);}catch(TargetInvocationException e){failed=e.InnerException is InvalidOperationException;}
                Require(failed,"Object template accepted as native network");Set(surface,"m_MaterialTemplateHash",row.template);controls++;
                surfaceType.GetProperty("database",I).SetValue(surface,null);failed=false;
                try{Bool(tiles,"PreserveNativeNetwork",row.render,0,surface);}catch(TargetInvocationException e){failed=e.InnerException is InvalidOperationException;}
                Require(failed,"Non-built-in source admitted");surfaceType.GetProperty("database",I).SetValue(surface,gameDb);controls++;
                Vector3[] positions;ushort[] uv;int[] indices;
                using(var reader=new BinaryReader(File.OpenRead(Path.Combine(inputs,row.mesh))))
                {
                    int vc=reader.ReadInt32(),ic=reader.ReadInt32();positions=new Vector3[vc];uv=new ushort[vc*2];indices=new int[ic];
                    for(int v=0;v<vc;v++)positions[v]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                    for(int n=0;n<uv.Length;n++)uv[n]=reader.ReadUInt16();for(int n=0;n<ic;n++)indices[n]=reader.ReadInt32();
                }
                foreach(float delta in new[]{-1.25f,0f,4f})foreach(bool sentinels in new[]{false,true})
                {
                    var mesh=new Mesh();object built=null;Mesh uploaded=null;
                    try
                    {
                        int count=positions.Length;
                        var channels=new List<VertexAttributeDescriptor>{new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3,0),new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float16,sentinels?4:2,1)};
                        if(sentinels){channels.Add(new VertexAttributeDescriptor(VertexAttribute.Color,VertexAttributeFormat.UNorm8,4,2));channels.Add(new VertexAttributeDescriptor(VertexAttribute.BlendIndices,VertexAttributeFormat.UInt32,1,3));}
                        mesh.SetVertexBufferParams(count,channels.ToArray());mesh.SetVertexBufferData(positions,0,0,count,0);
                        ushort[] uvData=sentinels?Enumerable.Range(0,count).SelectMany(v=>new[]{uv[v*2],uv[v*2+1],(ushort)0x3600,(ushort)0x3c00}).ToArray():uv;
                        mesh.SetVertexBufferData(uvData,0,0,uvData.Length,1);
                        if(sentinels){mesh.SetVertexBufferData(Enumerable.Repeat(0x12345678u,count).ToArray(),0,0,count,2);mesh.SetVertexBufferData(Enumerable.Repeat(2u,count).ToArray(),0,0,count,3);}
                        mesh.SetIndices(indices,MeshTopology.Triangles,0);
                        var plan=T("Draw03ApproachGeometry").GetMethod("Create",S).Invoke(null,new object[]{row.family,row.lod,positions,indices,delta});
                        object[] ba={"Draw03 native network fixture",mesh,plan,sentinels?5:0,null,null,false};
                        Require((bool)T("MovableStructureModel").GetMethod("TryBuild",S).Invoke(null,ba),"Build failed: "+ba[5]);built=ba[4];
                        var uvBefore=Bytes(Attr(built,VertexAttribute.TexCoord0));
                        var owned=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(built.GetType()));
                        var models=(IList)Activator.CreateInstance(owned.GetType());var surfaces=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(surfaceType));
                        T("MovableStructureRetile").GetMethod("Append",S).Invoke(null,new[]{"Draw03 native network fixture",row.render,(object)0,plan,built,surface,tiles,owned,models,surfaces});
                        Require(models.Count==1&&ReferenceEquals(models[0],built)&&owned.Count==0,"Native bypass allocated/replaced model");
                        Require(surfaces.Count==1&&ReferenceEquals(surfaces[0],surface),"Native Surface identity replaced");
                        Require(Bytes(Attr(models[0],VertexAttribute.TexCoord0)).SequenceEqual(uvBefore),"Raw UV changed");
                        uploaded=(Mesh)built.GetType().GetMethod("ToUnityMesh",I).Invoke(built,new object[]{true});
                        Require(uploaded.vertices.SequenceEqual((Vector3[])Get(plan,"Positions"))&&uploaded.triangles.SequenceEqual(indices),"Native upload changed geometry/topology");
                        var uploadedUv=new List<Vector4>();uploaded.GetUVs(0,uploadedUv);
                        float Half(ushort h){int sign=(h&0x8000)==0?1:-1,exp=(h>>10)&31,m=h&1023;return exp==0?sign*m*(float)Math.Pow(2,-24):sign*(1+m/1024f)*(float)Math.Pow(2,exp-15);}
                        for(int v=0;v<count;v++)Require(uploadedUv[v].x==Half(uv[v*2])&&uploadedUv[v].y==Half(uv[v*2+1])&&(!sentinels||(uploadedUv[v].z==.375f&&uploadedUv[v].w==1)),"Uploaded signed source UV differs");
                        if(sentinels)
                        {
                            var colors=new List<Color32>();uploaded.GetColors(colors);Require(colors.Count==count&&colors.All(c=>c.r==0x78&&c.g==0x56&&c.b==0x34&&c.a==0x12),"Uploaded COLOR differs");
                            var bones=Bytes(Attr(built,VertexAttribute.BlendIndices));for(int v=0;v<count;v++)Require(BitConverter.ToUInt32(bones,v*4)==2,"Raw bones differ");
                            Require(uploaded.GetVertexAttributeFormat(VertexAttribute.BlendIndices)==VertexAttributeFormat.UInt32,"Uploaded bone format differs");
                            using(var read=Mesh.AcquireReadOnlyMeshData(uploaded))
                            {
                                int stream=uploaded.GetVertexAttributeStream(VertexAttribute.BlendIndices),stride=uploaded.GetVertexBufferStride(stream),offset=uploaded.GetVertexAttributeOffset(VertexAttribute.BlendIndices);
                                var raw=read[0].GetVertexData<byte>(stream);
                                for(int v=0;v<count;v++){int at=v*stride+offset;Require(raw[at]==2&&raw[at+1]==0&&raw[at+2]==0&&raw[at+3]==0,"Native upload changed bone bytes");}
                            }
                        }
                        cases++;totalVertices+=count;
                    }
                    finally{(built as IDisposable)?.Dispose();if(uploaded!=null)UnityEngine.Object.DestroyImmediate(uploaded);UnityEngine.Object.DestroyImmediate(mesh);}
                }
            }
            var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));var levelType=T("MovableRoadAssets+Level");
            var root=ScriptableObject.CreateInstance(game.GetType("Game.Prefabs.NetPiecePrefab",true));
            var lod1=ScriptableObject.CreateInstance(game.GetType("Game.Prefabs.RenderPrefab",true));
            var lod2=ScriptableObject.CreateInstance(game.GetType("Game.Prefabs.RenderPrefab",true));
            var levels=Array.CreateInstance(levelType,3);
            foreach(var pair in new[]{(0,root),(1,lod1),(2,lod2)}){var level=FormatterServices.GetUninitializedObject(levelType);Set(level,"Source",pair.Item2);levels.SetValue(level,pair.Item1);}
            bool Graph()=>(bool)T("MovableRoadAssets").GetMethod("NativeNetworkGraph",S).Invoke(null,new object[]{levels});
            Require(Graph(),"Native root with plain RenderPrefab LOD children rejected");
            Set(levels.GetValue(0),"Source",lod1);Require(!Graph(),"Object graph admitted");Set(levels.GetValue(0),"Source",root);
            Set(levels.GetValue(2),"HasRoad",true);Require(!Graph(),"Road LOD graph admitted");Set(levels.GetValue(2),"HasRoad",false);
            var skeleton=ScriptableObject.CreateInstance(game.GetType("Game.Prefabs.ProceduralAnimationProperties",true));
            Set(levels.GetValue(1),"Skeleton",skeleton);Require(!Graph(),"Skinned LOD graph admitted");Set(levels.GetValue(1),"Skeleton",null);Require(Graph(),"Capability not restored");
            // Exercise the real native component-copy API used by TryCreate, without
            // registering/generating a bridge or claiming the whole save path passed.
            var tilingType=game.GetType("Game.Prefabs.NetPieceTiling",true);
            var add=root.GetType().GetMethods(I).Single(m=>m.Name=="AddComponent"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==0);
            var tiling=add.MakeGenericMethod(tilingType).Invoke(root,null);Set(tiling,"m_DisableTextureTiling",true);
            var lodType=game.GetType("Game.Prefabs.LodProperties",true);var lod=add.MakeGenericMethod(lodType).Invoke(root,null);
            var children=Array.CreateInstance(lod1.GetType(),2);children.SetValue(lod1,0);children.SetValue(lod2,1);Set(lod,"m_LodMeshes",children);
            var clone=ScriptableObject.CreateInstance(root.GetType());var copy=clone.GetType().GetMethods(I).Single(m=>m.Name=="AddComponentFrom"&&!m.IsGenericMethod&&m.GetParameters().Length==1);
            copy.Invoke(clone,new[]{tiling});copy.Invoke(clone,new[]{lod});
            var get=clone.GetType().GetMethods(I).Single(m=>m.Name=="GetComponent"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==0);
            var copiedTiling=get.MakeGenericMethod(tilingType).Invoke(clone,null);var copiedLod=get.MakeGenericMethod(lodType).Invoke(clone,null);
            Require(!ReferenceEquals(copiedTiling,tiling)&&(bool)Get(copiedTiling,"m_DisableTextureTiling"),"Native AddComponentFrom lost tiling flag");
            Require(!ReferenceEquals(copiedLod,lod)&&((Array)Get(copiedLod,"m_LodMeshes")).Cast<object>().SequenceEqual(children.Cast<object>()),"Native AddComponentFrom lost LOD links");
            foreach(var prefab in new[]{root,clone})foreach(UnityEngine.Object component in (IEnumerable)Get(prefab,"components"))UnityEngine.Object.DestroyImmediate(component);
            UnityEngine.Object.DestroyImmediate(clone);
            foreach(var o in new[]{root,lod1,lod2,skeleton})UnityEngine.Object.DestroyImmediate(o);
            Debug.Log($"PASS Draw03 native network: {records.Length} archived Surface headers; {cases} actual Create/Build/Append/ToUnityMesh cases; {totalVertices} vertices; {controls} rejection controls; native/plain-LOD, object, road, skinned graph gates; exact UV0.xy/ZW, COLOR, uploaded bone bytes, geometry, topology; native AddComponentFrom retains NetPieceTiling and LOD links; no replacement Surface/model/texture. Full TryCreate/serialization/cold load remain untested.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
