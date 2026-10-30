using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
public static class Draw01MiddleUnitConsumerCheck
{
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static object New(Type t,params object[] a)=>Activator.CreateInstance(t,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,a,null);
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static Array ArrayOf(Type t,params object[] v){var a=Array.CreateInstance(t,v.Length);for(int i=0;i<v.Length;i++)a.SetValue(v[i],i);return a;}
    public static void Run(){try{
        string Arg(string k)=>Draw01AppearancePartitionCheck.Argument(k);
        AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(Arg("-gameManaged"),new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        var mod=Assembly.LoadFrom(Arg("-candidate"));Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
        var inputType=T("MovableSurfaceAtlas+Input");var surfaceType=inputType.GetField("Materials",Flags).FieldType.GetElementType();
        var surface=FormatterServices.GetUninitializedObject(surfaceType);using(var reader=new BinaryReader(File.OpenRead(Arg("-surface"))))surfaceType.GetMethod("ReadHeader",Flags).Invoke(surface,new object[]{reader});
        var patchType=T("MovableExistingRoadSurface+Patch");var vertexType=T("MovableRoadGeometry+Vertex");var triangleType=T("MovableRoadGeometry+Triangle");
        var u=new Vector4(.5f/3.36602783203125f,0,0,.75f);var v=new Vector4(0,0,.125f,-19.9599609375f*.125f);
        var patch=New(patchType,new Vector2(.25f,0),new Vector2(.75f,1),u,v,surface,false,null,false,false,true,false,true,new Vector4(1,0,0,-1),false,false);
        patch=patchType.GetMethod("WithFiniteSampling",Flags).Invoke(patch,new object[]{Vector2.zero,Vector2.one});
        object V(float x,float z)=>New(vertexType,new Vector3(x,9.8f,z),Vector3.up,new Vector4(1,0,0,-1),new Vector4(.7f,.2f,.3f,.9f),new Color(.2f,.4f,.6f,.8f));
        var face=New(triangleType,V(-3.36602783203125f,19.9599609375f),V(-3.36602783203125f,35.9599609375f),V(0,35.9599609375f),0,0,23);
        var input=New(inputType,false);Set(input,"Faces",ArrayOf(T("MovableRoadGeometry+BoundTriangle"),New(T("MovableRoadGeometry+BoundTriangle"),face,0u)));Set(input,"Materials",ArrayOf(surfaceType,surface));Set(input,"Native",surface);Set(input,"Patches",ArrayOf(patchType,patch));
        var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
        object[] call={ArrayOf(inputType,input),owner,paint,null};var output=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",Flags).Invoke(null,call)).GetValue(0);
        if(output.Length!=1)throw new Exception("Middle continuous unit added period topology.");
        var result=output.GetValue(0);var positions=new Vector3[3];var uv=new Vector4[3];int j=0;
        foreach(string corner in new[]{"A","B","C"}){var a=Get(result,corner);var b=Get(face,corner);foreach(string name in new[]{"Position","Normal","Tangent","Color"})if(!Get(a,name).Equals(Get(b,name)))throw new Exception(name+" changed");positions[j]=(Vector3)Get(a,"Position");uv[j]=(Vector4)Get(a,"UV");if(uv[j].z!=.3f||uv[j].w!=.9f)throw new Exception("UVzw changed");j++;}
        if(Math.Abs(uv[1].y-uv[0].y-2)>1e-6)throw new Exception("Original eight-metre period or phase changed.");
        var mesh=new Mesh();try{mesh.vertices=positions;mesh.SetUVs(0,uv.ToList());mesh.triangles=new[]{0,1,2};mesh.UploadMeshData(false);var copy=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(0,copy);if(!copy.SequenceEqual(uv))throw new Exception("Native upload changed continuous UV.");}finally{UnityEngine.Object.DestroyImmediate(mesh);}
        File.WriteAllText(Arg("-report"),"{\"status\":\"MAPPER_FIXTURE_PASS\",\"trianglesIn\":1,\"trianglesOut\":1,\"physicalLength\":16,\"sourceUnitLength\":8,\"nativeMeshUpload\":true,\"productionConnected\":false,\"qualityAccepted\":false,\"scope\":\"Positive-left middle unit only; direct compiled Compose on a synthetic receiver; no transition/other-quarter or native registration acceptance\"}");Debug.Log("PASS actual middle8 unit Compose and native upload");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
