using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Execute the production Compose method without a compute shader or asset writer.
// This checks native asset/UV routing; it does not claim shader or motion acceptance.
public static class FiniteRoadMappingCheck
{
    const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    static object Get(object value, string field) => value.GetType().GetField(field, Fields).GetValue(value);
    static void Set(object value, string field, object data) => value.GetType().GetField(field, Fields).SetValue(value, data);
    static object New(Type type, params object[] values) => Activator.CreateInstance(type, Fields, null, values, null);
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static Array ArrayOf(Type type, params object[] values)
    { var result=Array.CreateInstance(type,values.Length); for(int i=0;i<values.Length;i++)result.SetValue(values[i],i);return result; }
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();
            string Arg(string key)=>args[Array.IndexOf(args,key)+1];
            var managed=Arg("-gameManaged");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=> {
                var file=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
                return File.Exists(file)?Assembly.LoadFrom(file):null;
            };
            var mod=Assembly.LoadFrom(Arg("-modAssembly"));
            Type T(string name)=>mod.GetType("BridgeBuilder.Bridges."+name,true);
            var surfaceType=T("MovableStructureTiles").GetMethod("NativeSurface",Fields).ReturnType;
            object Surface()
            {
                var result=FormatterServices.GetUninitializedObject(surfaceType);
                using(var reader=new BinaryReader(File.OpenRead(Arg("-surfaceFile"))))
                    surfaceType.GetMethod("ReadHeader",Fields).Invoke(result,new object[]{reader});
                return result;
            }
            var original=Surface(); var prototype=Surface();
            var vertex=T("MovableRoadGeometry+Vertex"); var triangle=T("MovableRoadGeometry+Triangle");
            var bound=T("MovableRoadGeometry+BoundTriangle"); var patch=T("MovableExistingRoadSurface+Patch");
            var input=T("MovableSurfaceAtlas+Input");
            object V(float x,float z)=>New(vertex,new Vector3(x,0,z),new Vector3(0,1,1).normalized,new Vector4(1,0,0,-1),new Vector4(3+x,7+z,7,9),new Color(.2f,.3f,.4f,.7f));
            var face=New(triangle,V(0,0),V(1,0),V(0,1),0,5,23);
            var finite=New(patch,new Vector2(.2f,.3f),new Vector2(.4f,.5f),new Vector4(.01f,0,0,.2f),
                new Vector4(0,0,.02f,.3f),prototype,false,null,false,true,false,false,false,null,false,true);
            var repeatU=New(patch,Vector2.zero,Vector2.one,new Vector4(4,0,0,0),
                new Vector4(0,0,.1f,.25f),prototype,false,null,false,false,true,true,false,null,false,false);
            var repeatV=New(patch,Vector2.zero,Vector2.one,new Vector4(.1f,0,0,.25f),
                new Vector4(0,0,4,0),prototype,false,null,false,false,true,false,true,new Vector4(-1,0,0,-1),false,false);
            var finiteUnit=New(patch,Vector2.zero,Vector2.one,new Vector4(.1f,0,0,.25f),
                new Vector4(0,0,.1f,.25f),prototype,false,null,false,false,true,false,false,null,false,true);
            object Input(uint bone, bool selected=false)
            {
                var value=New(input,selected);Set(value,"Faces",ArrayOf(bound,New(bound,face,bone)));
                Set(value,"Materials",ArrayOf(surfaceType,original));Set(value,"Native",prototype);
                Set(value,"Patches",ArrayOf(patch,finite));return value;
            }
            var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));
            var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
            var uInput=Input(0);Set(uInput,"Patches",ArrayOf(patch,repeatU));
            var vInput=Input(0);Set(vInput,"Patches",ArrayOf(patch,repeatV));
            var finiteInput=Input(0);Set(finiteInput,"Patches",ArrayOf(patch,finiteUnit));
            var atlasWrap=New(patch,new Vector2(.25f,.125f),new Vector2(.5f,.625f),new Vector4(.625f,0,0,.25f),
                new Vector4(0,0,.5f,.125f),prototype,false,null,false,true,false,true,false,new Vector4(-1,0,0,-1),true,false);
            var wrapInput=Input(0);Set(wrapInput,"Patches",ArrayOf(patch,atlasWrap));
            var unsupportedWrap=Input(0,true);Set(unsupportedWrap,"Patches",ArrayOf(patch,atlasWrap));
            object[] call={ArrayOf(input,Input(0),Input(0x80000000,true),uInput,vInput,Input(0,true),finiteInput,wrapInput,unsupportedWrap),owner,paint,null};
            var outputs=(Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,call);
            var materials=(Array)call[3];
            var mapped=((Array)outputs.GetValue(0)).GetValue(0);
            var retained=((Array)outputs.GetValue(1)).GetValue(0);
            Require(ReferenceEquals(((Array)materials.GetValue(0)).GetValue(0),prototype),"Finite prototype Surface identity lost.");
            Require(ReferenceEquals(((Array)materials.GetValue(1)).GetValue(0),original),"Unsupported original Surface identity lost.");
            Require((Vector4)Get(Get(mapped,"A"),"UV")==new Vector4(.2f,.3f,7,9),"Finite atlas coordinates treated as a repeating crop.");
            Require((Vector4)Get(Get(mapped,"B"),"UV")==new Vector4(.21f,.3f,7,9),"Finite atlas gradient lost.");
            Require(retained.Equals(face),"Original selected receiver attributes changed.");
            Require(((Array)outputs.GetValue(4)).GetValue(0).Equals(face),"Fixed selected receiver was incorrectly treated as prototype-owned.");
            Require((Color)Get(Get(mapped,"A"),"Color")==new Color(.2f,.3f,.4f,.7f),"Finite atlas mapping erased source vertex color.");
            Require((Vector3)Get(Get(mapped,"A"),"Normal")==new Vector3(0,1,1).normalized,"Finite projected tangent changed receiver normal.");
            Require((Vector4)Get(Get(mapped,"A"),"Tangent")==new Vector4(1,0,0,-1),"Finite projected tangent disagrees with wrap formula.");
            Require((int)Get(mapped,"Occurrence")==5 && (int)Get(mapped,"SourceTriangle")==23,"Provenance changed.");
            var preparedFinite=((Array)outputs.GetValue(5)).GetValue(0);
            Require((Vector4)Get(Get(preparedFinite,"A"),"Tangent")==new Vector4(1,0,0,-1),"Prepared explicit projection tangent failed.");
            Require((Color)Get(Get(preparedFinite,"A"),"Color")==new Color(.2f,.3f,.4f,.7f),"Prepared projection lost source color.");
            Require((Vector3)Get(Get(preparedFinite,"A"),"Normal")==new Vector3(0,1,1).normalized,"Prepared projection changed receiver normal.");
            var uFace=((Array)outputs.GetValue(2)).GetValue(0);
            var vFace=((Array)outputs.GetValue(3)).GetValue(0);
            Require((Vector4)Get(Get(uFace,"B"),"UV")==new Vector4(4,.25f,7,9),"Repeat U was clamped or finite V changed.");
            Require((Vector4)Get(Get(vFace,"C"),"UV")==new Vector4(.25f,4,7,9),"Repeat V was clamped or finite U changed.");
            Require((Vector4)Get(Get(vFace,"C"),"Tangent")==new Vector4(-1,0,0,-1),"Measured packed tangent was replaced by UV Jacobian.");
            Require((Vector4)Get(Get(((Array)outputs.GetValue(5)).GetValue(0),"C"),"UV")==new Vector4(.25f,.35f,7,9),"Complete finite unit projection changed.");
            var wrapped=(Array)outputs.GetValue(6);
            Require(wrapped.Length>1,"Evidenced atlas repeat was stretched instead of wrapped.");
            Require(ReferenceEquals(((Array)materials.GetValue(6)).GetValue(0),prototype),"Atlas wrap replaced the original Surface.");
            Require(((Array)outputs.GetValue(7)).GetValue(0).Equals(face),"Unknown selected object incorrectly entered prototype wrap mapping.");
            double area=0;
            foreach(var item in wrapped)
            {
                var a=(Vector3)Get(Get(item,"A"),"Position");var b=(Vector3)Get(Get(item,"B"),"Position");var c=(Vector3)Get(Get(item,"C"),"Position");
                area+=Vector3.Cross(b-a,c-a).magnitude*.5;
                foreach(string corner in new[]{"A","B","C"})
                {
                    var value=Get(item,corner);var uv=(Vector4)Get(value,"UV");
                    Require(uv.x>=.25f&&uv.x<=.5f,"Atlas UV escaped its evidenced region.");
                    Require((Vector4)Get(value,"Tangent")==new Vector4(-1,0,0,-1),"Atlas wrap ignored evidenced source tangent frame.");
                }
                Require((int)Get(item,"Occurrence")==5&&(int)Get(item,"SourceTriangle")==23,"Atlas wrap lost provenance.");
            }
            Require(Math.Abs(area-.5)<1e-6,"Atlas wrap lost original triangle coverage.");
            var escaped=New(patch,Vector2.zero,Vector2.one,new Vector4(4,0,0,0),
                new Vector4(0,0,1,.25f),prototype,false,null,false,false,true,true,false,null,false,false);
            Set(uInput,"Patches",ArrayOf(patch,escaped));
            try
            {
                T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,new object[]{ArrayOf(input,uInput),owner,paint,null});
                throw new Exception("Finite unit axis escaped into whole-texture repeat.");
            }
            catch(TargetInvocationException e) when(e.InnerException is InvalidOperationException &&
                e.InnerException.Message.Contains("finite axis escaped")) { }
            var partitionType=T("MovableRoadGeometry+Partition");
            foreach(var rotation in new[]{Quaternion.identity,Quaternion.Euler(0,31,0),Quaternion.Euler(13,31,7)})
            {
                var pose=Matrix4x4.TRS(new Vector3(17,-3,21),rotation,Vector3.one);
                var local=Matrix4x4.identity;local.SetRow(0,new Vector4(0,0,-1,0));local.SetRow(2,new Vector4(1,0,0,0));
                var partition=New(partitionType,local*pose.inverse,New(partitionType,0),New(partitionType,1));
                object P(float x,float z)
                {
                    var n=pose.MultiplyVector(Vector3.up);var tangent=pose.MultiplyVector(Vector3.right);
                    return New(vertex,pose.MultiplyPoint3x4(new Vector3(x,0,z)),n,new Vector4(tangent.x,tangent.y,tangent.z,-1),new Vector4(x,z,7,9),new Color(.2f,.3f,.4f,.7f));
                }
                var left=New(triangle,P(-2,-1),P(0,1),P(0,-1),0,5,23);
                var right=New(triangle,P(0,-1),P(0,1),P(2,-1),0,5,24);
                object[] bind={ArrayOf(triangle,left,right),partition,2,1,null,null,false,null,null};
                Require((bool)T("MovableRoadGeometry").GetMethod("TryBind",Static).Invoke(null,bind),"Physical partition failed after rigid pose: "+bind[5]);
                Require(((Array)bind[4]).Length==2,"Physical partition recut a transformed original boundary.");
                var crossing=New(triangle,P(-.001f,-1),P(.001f,1),P(.001f,-1),0,5,25);
                object[] crossingBind={ArrayOf(triangle,crossing),partition,2,1,null,null,false,null,null};
                Require((bool)T("MovableRoadGeometry").GetMethod("TryBind",Static).Invoke(null,crossingBind),"True physical crossing failed.");
                Require(((Array)crossingBind[4]).Length>=2,"Physical boundary interval hid a genuine crossing.");
            }
            Debug.Log("PASS physical partition rigid poses: original boundary retained and genuine crossing preserved.");
            Debug.Log("PASS finite road Compose: unchanged native Surface identities, absolute atlas UV, original-input bypass, no GPU or material creation.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
