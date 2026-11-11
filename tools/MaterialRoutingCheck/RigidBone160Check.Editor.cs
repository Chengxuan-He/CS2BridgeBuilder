using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;
public static class RigidBone160Check {
[System.Runtime.InteropServices.DllImport("kernel32",CharSet=System.Runtime.InteropServices.CharSet.Unicode)] static extern IntPtr LoadLibraryW(string path);
const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
public static void Run(){try{
string Arg(string n)=>Environment.GetCommandLineArgs().SkipWhile(x=>x!=n).Skip(1).First();var managed=Arg("-gameManaged");if(LoadLibraryW(Path.GetFullPath(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll")))==IntPtr.Zero)throw new Exception("native pipeline plugin unavailable");AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};var a=Assembly.LoadFrom(Arg("-candidate"));var t=a.GetType("BridgeBuilder.Bridges.MovableStructureModel",true);var pt=t.GetNestedType("Plan",F);var mesh=new Mesh();mesh.vertices=new[]{new Vector3(0,0,0),new Vector3(2,0,0),new Vector3(0,1,0)};mesh.normals=Enumerable.Repeat(Vector3.forward,3).ToArray();mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,1),3).ToArray();mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up};mesh.triangles=new[]{0,1,2};var ctor=pt.GetConstructors(F).Single();var args=ctor.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:null).ToArray();args[0]=mesh.vertices;args[1]=mesh.vertices;args[2]=new[]{mesh.triangles};args[3]=new[]{new bool[1]};var plan=ctor.Invoke(args);var build=t.GetMethod("TryBuild",F);
foreach(int bone in new[]{0,2}){object[] call={"rigid",mesh,plan,3,null,null,false,(int?)bone,null};if(!(bool)build.Invoke(null,call))throw new Exception((string)call[5]);var model=call[4];var toMesh=model.GetType().GetMethod("ToUnityMesh",F);var output=(Mesh)toMesh.Invoke(model,toMesh.GetParameters().Select(x=>x.DefaultValue).ToArray());using(var data=Mesh.AcquireReadOnlyMeshData(output)){var stream=output.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.BlendIndices);var offset=output.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.BlendIndices);var stride=output.GetVertexBufferStride(stream);var bytes=data[0].GetVertexData<byte>(stream).ToArray();for(int i=0;i<3;i++)if(BitConverter.ToUInt32(bytes,i*stride+offset)!=bone)throw new Exception("wrong native bone");}if(!output.vertices.SequenceEqual(mesh.vertices)||!output.uv.SequenceEqual(mesh.uv)||!output.triangles.SequenceEqual(mesh.triangles))throw new Exception("source channels changed");((IDisposable)model).Dispose();UnityEngine.Object.DestroyImmediate(output);}
if(Environment.GetCommandLineArgs().Contains("-connected")) Connected(a, mesh, Arg("-fixtureRoot"));
MultipleSlots(a, mesh);
PreparedPlacements(a);
BoneDecalModel(a);
UtilityCarrier(a);
Debug.Log("PASS native model encode/readback: original positions/UV/indices preserved; UInt32 rigid bones0/2; implicit weight one. No production attachment entry acceptance claimed.");EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
static void BoneDecalModel(Assembly mod)
{
    var geometry=mod.GetType("BridgeBuilder.Bridges.MovableRoadGeometry",true);
    var partition=geometry.GetNestedType("Partition",F);
    object Leaf(int bone)=>Activator.CreateInstance(partition,F,null,new object[]{bone},null);
    var split=Activator.CreateInstance(partition,F,null,new[]{(object)Matrix4x4.identity,Leaf(1),Leaf(2)},null);
    var regionType=mod.GetType("BridgeBuilder.Bridges.MovableNativeDecals+Region",true);
    var region=Activator.CreateInstance(regionType,F,null,new object[]{Vector3.zero,Quaternion.Euler(0,20,0),new Vector3(2,.1f,4),false,null,new Vector4(.1f,.2f,.7f,.9f)},null);
    var volume=mod.GetType("BridgeBuilder.Bridges.MovableRigidDecalVolume",true);
    var parts=(System.Collections.IList)volume.GetMethod("Build",F).Invoke(null,new[]{region,split});
    if(parts.Count!=2)throw new Exception("decal two-leaf partition missing");
    var modelType=mod.GetType("BridgeBuilder.Bridges.MovableBoneDecalModel",true);
    object[] arguments={"bone-decal-model",parts,3,null,null};
    if(!(bool)modelType.GetMethod("TryBuild",F).Invoke(null,arguments))throw new Exception((string)arguments[4]);
    var model=arguments[3];var conversion=model.GetType().GetMethod("ToUnityMesh",F);
    var mesh=(Mesh)conversion.Invoke(model,conversion.GetParameters().Select(x=>x.DefaultValue).ToArray());
    try
    {
        var uv=new System.Collections.Generic.List<Vector4>[4];for(int i=0;i<4;i++){uv[i]=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(i,uv[i]);if(uv[i].Count!=mesh.vertexCount)throw new Exception("missing projection UV channel");}
        var positions=mesh.vertices;int vertex=0;
        using(var data=Mesh.AcquireReadOnlyMeshData(mesh))
        {
            int stream=mesh.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.BlendIndices),offset=mesh.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.BlendIndices),stride=mesh.GetVertexBufferStride(stream);
            var bytes=data[0].GetVertexData<byte>(stream).ToArray();
            foreach(var part in parts)
            {
                int bone=(int)Get(part,"Bone");var center=(Vector3)Get(part,"Center");var rotation=(Quaternion)Get(part,"Rotation");var size=(Vector3)Get(part,"Size");var area=(Vector4)Get(part,"Area");
                foreach(var triangle in (Array)Get(part,"Triangles"))foreach(string corner in new[]{"A","B","C"})
                {
                    var local=(Vector3)Get(Get(Get(triangle,"Geometry"),corner),"Position");
                    if((positions[vertex]-(center+rotation*local)).sqrMagnitude>1e-10f || BitConverter.ToUInt32(bytes,vertex*stride+offset)!=bone || uv[3][vertex]!=area)
                        throw new Exception("decal frame/atlas/bone encode changed");
                    var expected=new[]{rotation*(Vector3.right*size.x),rotation*(Vector3.up*size.y),rotation*(Vector3.forward*size.z)};
                    for(int axis=0;axis<3;axis++)if(((Vector3)uv[axis][vertex]-expected[axis]).sqrMagnitude>1e-10f || uv[axis][vertex].w!=center[axis])throw new Exception("decal projection axis changed");
                    vertex++;
                }
            }
        }
        if(vertex!=mesh.vertexCount || !mesh.triangles.SequenceEqual(Enumerable.Range(0,vertex)))throw new Exception("decal emitted topology changed");
    }
    finally{UnityEngine.Object.DestroyImmediate(mesh);((IDisposable)model).Dispose();}
    object[] invalid={"bad-bone",parts,1,null,null};
    if((bool)modelType.GetMethod("TryBuild",F).Invoke(null,invalid)||invalid[3]!=null||string.IsNullOrEmpty((string)invalid[4]))throw new Exception("invalid bone retained partial native model");
    Debug.Log("PASS production current-bone decal model: two clipped leaves, native UInt32 bones1/2, all four projection UV channels, exact transformed positions/atlas frames and topology; missing bone returns no partial model.");
}
static void MultipleSlots(Assembly mod,Mesh source)
{
    source.subMeshCount=2; source.SetIndices(new[]{0,1,2},UnityEngine.MeshTopology.Triangles,0);
    source.SetIndices(new[]{2,1,0},UnityEngine.MeshTopology.Triangles,1);
    var type=mod.GetType("BridgeBuilder.Bridges.MovableStructureModel",true);
    var planType=type.GetNestedType("Plan",F);var ctor=planType.GetConstructors(F).Single();
    for(int slot=0;slot<2;slot++)
    {
        var expected=source.GetIndices(slot);
        var args=ctor.GetParameters().Select(p=>p.HasDefaultValue?p.DefaultValue:null).ToArray();
        args[0]=source.vertices;args[1]=source.vertices;args[2]=new[]{expected};args[3]=new[]{new bool[1]};
        object[] call={"material-slot",source,ctor.Invoke(args),3,null,null,false,(int?)2,(int?)slot};
        if(!(bool)type.GetMethod("TryBuild",F).Invoke(null,call))throw new Exception((string)call[5]);
        var model=call[4];var toMesh=model.GetType().GetMethod("ToUnityMesh",F);
        var output=(Mesh)toMesh.Invoke(model,toMesh.GetParameters().Select(p=>p.DefaultValue).ToArray());
        try {if(output.subMeshCount!=1 || !output.triangles.SequenceEqual(expected) ||
            !output.vertices.SequenceEqual(source.vertices) || !output.uv.SequenceEqual(source.uv))
            throw new Exception("multi-material slot changed channels or winding");}
        finally{UnityEngine.Object.DestroyImmediate(output);((IDisposable)model).Dispose();}
    }
    Debug.Log("PASS native multi-submesh split: two source slots independently retain exact opposite winding, positions and UV, one output slot each.");
}
static object Get(object value,string name) => value.GetType().GetField(name,F)?.GetValue(value) ?? value.GetType().GetProperty(name,F).GetValue(value);
static void Set(object value,string name,object data) {var field=value.GetType().GetField(name,F);if(field!=null)field.SetValue(value,data);else value.GetType().GetProperty(name,F).SetValue(value,data);}
static void UtilityCarrier(Assembly mod)
{
    var game=Assembly.Load("Game");Type G(string name)=>game.GetType("Game.Prefabs."+name,true);
    var source=ScriptableObject.CreateInstance(G("StaticObjectPrefab"));source.name="utility-fixture";
    var render=ScriptableObject.CreateInstance(G("RenderPrefab"));render.name="utility-render";
    object Add(object prefab,string kind)=>prefab.GetType().GetMethods(F).Single(m=>m.Name=="AddComponent"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==0).MakeGenericMethod(G(kind)).Invoke(prefab,null);
    var utility=Add(source,"UtilityObject");var utilityPosition=utility.GetType().GetField("m_UtilityPosition").FieldType;
    Set(utility,"m_UtilityPosition",Activator.CreateInstance(utilityPosition,new object[]{1f,2f,3f}));
    var boundsType=render.GetType().GetProperty("bounds").PropertyType;
    var bounds=Activator.CreateInstance(boundsType,new[]{Activator.CreateInstance(utilityPosition,new object[]{-2f,-1f,-3f}),Activator.CreateInstance(utilityPosition,new object[]{2f,8f,3f})});
    Set(render,"bounds",bounds);Set(render,"surfaceArea",43f);Set(render,"meshCount",1);Set(render,"indexCount",12);Set(render,"vertexCount",4);
    Add(render,"LodProperties");
    var info=Activator.CreateInstance(G("ObjectMeshInfo"));Set(info,"m_Mesh",render);
    var meshes=Array.CreateInstance(info.GetType(),1);meshes.SetValue(info,0);Set(source,"m_Meshes",meshes);
    var created=(System.Collections.IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(G("PrefabBase")));
    var carrier=mod.GetType("BridgeBuilder.Bridges.MovableRoadObjectCarrier",true).GetMethod("Create",F).Invoke(null,new object[]{"tmp11111111-2222-4333-8444-555555555555",source,created,false});
    var copies=(Array)Get(carrier,"m_Meshes");var copy=Get(copies.GetValue(0),"m_Mesh");
    Debug.Log("Carrier metadata: bounds="+Get(Get(copy,"bounds"),"min")+" / "+Get(Get(copy,"bounds"),"max")+" expected="+Get(bounds,"min")+" / "+Get(bounds,"max")+" sameRef="+ReferenceEquals(copy,render)+" area="+Get(copy,"surfaceArea")+" meshes="+Get(copy,"meshCount")+" indices="+Get(copy,"indexCount")+" vertices="+Get(copy,"vertexCount")+" materials="+((System.Collections.IEnumerable)Get(copy,"surfaceAssets")).Cast<object>().Count());
    if(copies.Length!=1||ReferenceEquals(copy,render)||!Get(copy,"bounds").Equals(bounds)||(float)Get(copy,"surfaceArea")!=43f||
       (int)Get(copy,"meshCount")!=0||(int)Get(copy,"indexCount")!=0||(int)Get(copy,"vertexCount")!=0||((System.Collections.IEnumerable)Get(copy,"surfaceAssets")).Cast<object>().Any())throw new Exception("utility carrier lost metadata or retained visual batches");
    var get=carrier.GetType().GetMethods(F).Single(m=>m.Name=="GetComponent"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==0);
    var copiedUtility=get.MakeGenericMethod(G("UtilityObject")).Invoke(carrier,null);
    Debug.Log("Carrier function: utility="+(copiedUtility==null?"null":Get(copiedUtility,"m_UtilityPosition").ToString())+" expected="+Get(utility,"m_UtilityPosition")+" lod="+get.MakeGenericMethod(G("LodProperties")).Invoke(copy,null)+" sourceMeshes="+Get(render,"meshCount")+" created="+created.Count);
    if(copiedUtility==null||!Get(copiedUtility,"m_UtilityPosition").Equals(Get(utility,"m_UtilityPosition"))||
       get.MakeGenericMethod(G("LodProperties")).Invoke(copy,null)!=null||(int)Get(render,"meshCount")!=1||created.Count!=2)throw new Exception("utility functional data/source isolation/LOD ownership changed");
    foreach(UnityEngine.Object item in created)UnityEngine.Object.DestroyImmediate(item);
    UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(render);
    Debug.Log("PASS actual native Prefab clone: utility point retained, authored bounds/surface area retained, zero visual slots, source untouched, visible LOD removed. ECS derived utility behavior remains separate.");
}
static void PreparedPlacements(Assembly mod)
{
    var game=Assembly.Load("Game"); Type G(string name)=>game.GetType("Game.Prefabs."+name,true);
    var factoryType=mod.GetType("BridgeBuilder.Bridges.TowerFactory",true);
    var factory=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(factoryType);
    var cache=factoryType.GetField("_preparedRoadPlacements",F);cache.SetValue(factory,Activator.CreateInstance(cache.FieldType));
    var source=(ScriptableObject)ScriptableObject.CreateInstance(G("StaticObjectPrefab"));
    var skeletonType=G("ProceduralAnimationProperties");
    object skeleton=typeof(ScriptableObject).IsAssignableFrom(skeletonType)?ScriptableObject.CreateInstance(skeletonType):Activator.CreateInstance(skeletonType);
    var bonesField=skeletonType.GetField("m_Bones",F);var boneType=bonesField.FieldType.GetElementType();var bones=Array.CreateInstance(boneType,2);
    for(int i=0;i<2;i++)
    {
        var bone=Activator.CreateInstance(boneType);Set(bone,"parentId",i-1);
        var kind=boneType.GetField("m_Type",F);kind.SetValue(bone,Enum.ToObject(kind.FieldType,i==0?1:0));bones.SetValue(bone,i);
    }
    bonesField.SetValue(skeleton,bones);
    var placementType=G("ObjectSubObjectInfo");var placements=Array.CreateInstance(placementType,1);
    var placement=Activator.CreateInstance(placementType);Set(placement,"m_Object",source);placements.SetValue(placement,0);
    var partitionType=mod.GetType("BridgeBuilder.Bridges.MovableRoadGeometry+Partition",true);
    var partitions=Array.CreateInstance(partitionType,1);partitions.SetValue(Activator.CreateInstance(partitionType,F,null,new object[]{1},null),0);
    var method=factoryType.GetMethod("PrepareRoadPlacements",F);var frame=new Pose(Vector3.zero,Quaternion.identity);
    var first=method.Invoke(factory,new object[]{placements,frame,partitions,skeleton});
    var second=method.Invoke(factory,new object[]{placements.Clone(),frame,partitions.Clone(),skeleton});
    if(!ReferenceEquals(first,second)||!((bool[])Get(first,"Moving"))[1])throw new Exception("placement preparation did not reuse complete inputs / moving ancestor");
    var changed=method.Invoke(factory,new object[]{placements,new Pose(Vector3.right,Quaternion.identity),partitions,skeleton});
    if(ReferenceEquals(first,changed))throw new Exception("placement preparation reused a different frame");
    UnityEngine.Object.DestroyImmediate(source);if(skeleton is UnityEngine.Object native)UnityEngine.Object.DestroyImmediate(native);
    Debug.Log("PASS actual Game types: placement preparation reused equivalent lists, separated changed frame, inherited moving ancestor; non-utility input needs no native world.");
}
static void Connected(Assembly mod,Mesh mesh,string directory)
{
    Type T(string name)=>mod.GetType("BridgeBuilder.Bridges."+name,true);
    var native=Assembly.Load("Colossal.IO.AssetDatabase");
    Type N(string name)=>native.GetType("Colossal.IO.AssetDatabase."+name,true);
    var game=Assembly.Load("Game"); Type G(string name)=>game.GetType("Game.Prefabs."+name,true);
    Directory.CreateDirectory(directory);
    var descriptorType=mod.GetType("BridgeBuilder.Runtime.BridgePreparedMaterials+Descriptor",true);
    var descriptor=Activator.CreateInstance(descriptorType,F,null,new object[]{directory},null);
    var databaseType=N("AssetDatabase`1").MakeGenericType(descriptorType);
    var database=databaseType.GetMethod("GetInstance",F).Invoke(null,new[]{descriptor});
    var pathType=N("AssetDataPath");
    var path=pathType.GetMethods(F).Single(m=>m.Name=="Create"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)&&m.GetParameters()[1].ParameterType==typeof(string));
    var assetPath=path.Invoke(null,new object[]{"Fixture","Lamp",Enum.Parse(path.GetParameters()[2].ParameterType,"None")});
    var add=databaseType.GetMethods(F).Single(m=>m.Name=="AddAsset"&&!m.IsGenericMethod&&m.GetParameters().Length==3&&m.GetParameters()[1].ParameterType==typeof(Type));
    var hash=add.GetParameters()[2].ParameterType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{"11111111222233334444555555555555"});
    var asset=add.Invoke(database,new[]{assetPath,(object)N("GeometryAsset"),hash});
    var assetFile=(string)Get(asset.GetType().GetMethod("GetMeta",F).Invoke(asset,null),"path");
    Directory.CreateDirectory(Path.GetDirectoryName(assetFile));
    File.Copy("E:/CS2BridgeBuilder/work/decal160/source-lamp.Geometry",assetFile,true);
    object global;try{global=N("AssetDatabase").GetProperty("global",F).GetValue(null);}catch(TargetInvocationException){global=N("AssetDatabase").GetField("s_Instance",F).GetValue(null);}
    ((System.Threading.Tasks.Task)global.GetType().GetMethod("RegisterDatabase",F).Invoke(global,new[]{database})).GetAwaiter().GetResult();
    var sourceRender=ScriptableObject.CreateInstance(G("RenderPrefab"));sourceRender.name="SourceLamp";
    Set(sourceRender,"geometryAsset",asset);
    var reader=mod.GetType("BridgeBuilder.Runtime.PrivateGeometryReader",true);
    var originalMeshes=(Mesh[])reader.GetMethod("Read",F).Invoke(null,new object[]{sourceRender});
    if(originalMeshes.Length!=1)throw new Exception("fixture requires one native source mesh, got "+originalMeshes.Length);
    var originalVertices=originalMeshes[0].vertices;
    reader.GetMethod("Release",F).Invoke(null,new object[]{originalMeshes});
    var source=ScriptableObject.CreateInstance(G("StaticObjectPrefab"));source.name="SourceObject";
    var info=Activator.CreateInstance(G("ObjectMeshInfo"));Set(info,"m_Mesh",sourceRender);
    Set(info,"m_Rotation",Activator.CreateInstance(G("ObjectMeshInfo").GetField("m_Rotation",F).FieldType,new object[]{0f,0f,0f,1f}));
    var infos=Array.CreateInstance(info.GetType(),1);infos.SetValue(info,0);Set(source,"m_Meshes",infos);
    var root=ScriptableObject.CreateInstance(G("RenderPrefab"));
    var skeleton=ScriptableObject.CreateInstance(G("ProceduralAnimationProperties"));
    var boneField=skeleton.GetType().GetField("m_Bones",F);var boneType=boneField.FieldType.GetElementType();var bones=Array.CreateInstance(boneType,2);
    for(int i=0;i<2;i++){var bone=Activator.CreateInstance(boneType);Set(bone,"parentId",i-1);Set(bone,"m_Type",Enum.ToObject(boneType.GetField("m_Type",F).FieldType,i==0?0:33));Set(bone,"m_ConnectionID",i==0?0:100);bones.SetValue(bone,i);}boneField.SetValue(skeleton,bones);
    var partition=Activator.CreateInstance(T("MovableRoadGeometry+Partition"),F,null,new object[]{1},null);
    var levelType=T("MovableRoadAssets+Level");var ctor=levelType.GetConstructors(F).Single(c=>c.GetParameters().Length==17);
    var args=ctor.GetParameters().Select(x=>x.HasDefaultValue?x.DefaultValue:x.ParameterType.IsValueType?Activator.CreateInstance(x.ParameterType):null).ToArray();
    args[0]=root;args[1]=Array.CreateInstance(T("MovableRoadGeometry+Triangle"),0);args[2]=Array.CreateInstance(N("SurfaceAsset"),0);args[3]=partition;args[5]=Pose.identity;args[7]=skeleton;
    var levels=Array.CreateInstance(levelType,3);for(int i=0;i<3;i++)levels.SetValue(ctor.Invoke(args),i);
    var placementType=G("ObjectSubObjectInfo");var placement=Activator.CreateInstance(placementType);Set(placement,"m_Object",source);
    var rotationType=placementType.GetField("m_Rotation",F).FieldType;Set(placement,"m_Rotation",Activator.CreateInstance(rotationType,new object[]{0f,0f,0f,1f}));
    var placements=Array.CreateInstance(placementType,1);placements.SetValue(placement,0);var partitions=Array.CreateInstance(partition.GetType(),1);partitions.SetValue(partition,0);
    var factory=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(T("TowerFactory"));
    var cache=T("TowerFactory").GetField("_directRoadMeshes",F);cache.SetValue(factory,Activator.CreateInstance(cache.FieldType));
    var result=(Array)T("TowerFactory").GetMethod("PrepareDirectRoadMeshes",F).Invoke(factory,new object[]{levels,placements,Pose.identity,partitions});
    for(int i=0;i<3;i++){var supplements=(Array)Get(result.GetValue(i),"AdditionalStructures");if(supplements.Length!=1)throw new Exception("LOD attachment missing "+i);var supplement=supplements.GetValue(0);if((int)Get(supplement,"RigidBone")!=1)throw new Exception("wrong graph bone");var plan=Get(supplement,"Plan");if(!((Vector3[])Get(plan,"Positions")).SequenceEqual(originalVertices))throw new Exception("attachment frame mismatch");}
    Set(placement,"m_ParentMesh",100);
    if(!(bool)T("TowerFactory").GetMethod("IsDirectRoadMesh",F).Invoke(factory,new[]{placement}))throw new Exception("binding would create duplicate Relative visual");
    Debug.Log("PASS connected factory preparation: real native Geometry IO, all three LOD supplements, same mechanism bone1, duplicate child exclusion identity. Effects/decals not accepted by this test.");
}
}
