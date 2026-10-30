using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Execute the production Compose method without a compute shader or asset writer.
// This checks native asset/UV routing; it does not claim shader or motion acceptance.
public static class SourceTangentFieldCheck
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

            var keepMethod=patch.GetMethod("WithSourceTangentField",Fields);
            var plain=New(patch,new Vector2(.2f,.3f),new Vector2(.4f,.5f),new Vector4(.01f,0,0,.2f),
                new Vector4(0,0,.02f,.3f),prototype,false,null,false,true,false,false,false,null,false,false);
            object[] field={new Vector4(0,0,0,1),Vector4.zero,new Vector4(0,0,.003f,0),1f};
            var keep=keepMethod.Invoke(plain,field);
            object Input(object mapping, object triangleValue,bool selected=false)
            {
                var value=New(input,selected); Set(value,"Faces",ArrayOf(bound,New(bound,triangleValue,0u)));
                Set(value,"Materials",ArrayOf(surfaceType,original));Set(value,"Native",prototype);
                Set(value,"Patches",ArrayOf(patch,mapping));return value;
            }
            var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));
            var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
            var math=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils");
            var pack=math.GetMethod("TangentToOctahedral",Static);var f4=pack.GetParameters()[0].ParameterType;
            object Pack(Vector4 t)=>pack.Invoke(null,new[]{Activator.CreateInstance(f4,new object[]{t.x,t.y,t.z,t.w})});
            int checks=0;
            foreach(float phase in new[]{0f,.5f,1f})
            {
                object W(float x,float z)
                {
                    var dir=new Vector3(1,0,.003f*(phase+z)).normalized;
                    return New(vertex,new Vector3(x,0,z),Vector3.up,new Vector4(dir.x,dir.y,dir.z,1),new Vector4(3+x,7+z,7,9),new Color(.2f,.3f,.4f,.7f));
                }
                var src=New(triangle,W(0,0),W(1,0),W(0,1),0,5,23);
                object[] call={ArrayOf(input,Input(keep,src),Input(plain,src),Input(finite,src),Input(keep,src,true)),owner,paint,null};
                var results=(Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,call);
                var materials=(Array)call[3];
                Require(((Array)results.GetValue(0)).Length==1,"Field changed topology");
                Require(ReferenceEquals(((Array)materials.GetValue(0)).GetValue(0),prototype),"Retention replaced Surface");
                var dst=((Array)results.GetValue(0)).GetValue(0);
                foreach(string corner in new[]{"A","B","C"})
                {
                    var a=Get(src,corner);var b=Get(dst,corner);
                    foreach(string attr in new[]{"Position","Normal","Color"})Require(Get(a,attr).Equals(Get(b,attr)),"Field changed "+attr);
                    var uv=(Vector4)Get(b,"UV");var p=(Vector3)Get(a,"Position");
                    Require(uv==new Vector4(.01f*p.x+.2f,.02f*p.z+.3f,7,9),"Field changed UV mapping/ZW");
                    var expectedT=new Vector4(1,0,.003f*p.z,1);
                    Require((Vector4)Get(b,"Tangent")==expectedT,"Source field mismatch");
                    Require(Pack(expectedT).Equals(Pack((Vector4)Get(b,"Tangent"))),"Native tangent code changed");checks++;
                }
                Require(((Array)results.GetValue(3)).GetValue(0).Equals(src),"Unsupported input was modified before appearance gate");
                var legacy=Get(((Array)results.GetValue(1)).GetValue(0),"C");
                Require((Vector4)Get(legacy,"Tangent")==new Vector4(1,0,0,-1),"Default finite tangent behavior changed");
                var projected=Get(((Array)results.GetValue(2)).GetValue(0),"C");
                Require((Vector4)Get(projected,"Tangent")==new Vector4(1,0,0,-1),"Projected tangent changed");
            }
            var closeField=(object[])field.Clone();closeField[2]=new Vector4(0,0,.0030001f,0);
            var close=keepMethod.Invoke(plain,closeField);
            var pair=Input(keep,face);
            Set(pair,"Faces",ArrayOf(bound,New(bound,face,0u),New(bound,face,1u)));
            Set(pair,"Patches",ArrayOf(patch,keep,close));
            object[] pairCall={ArrayOf(input,pair),owner,paint,null};
            var pairResult=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,pairCall)).GetValue(0);
            Require(pairResult.Length==2,"Close-field test lost faces");
            float z0=((Vector4)Get(Get(pairResult.GetValue(0),"C"),"Tangent")).z;
            float z1=((Vector4)Get(Get(pairResult.GetValue(1),"C"),"Tangent")).z;
            Require(z0==.003f&&z1==.0030001f,"Distinct close tangent fields merged into one chart");
            // No priority silently wins: conflicting or unrelated modes are rejected.
            var invalid=new[]{finite,New(patch,Vector2.zero,Vector2.one),
                New(patch,Vector2.zero,Vector2.one,new Vector4(1,0,0,0),new Vector4(0,1,0,0),prototype,false,null,false,true,false,false,false,new Vector4(1,0,0,1),false,false),
                New(patch,Vector2.zero,Vector2.one,new Vector4(1,0,0,0),new Vector4(0,1,0,0),prototype,false,null,false,false,true,false,true,null,false,false),
                New(patch,Vector2.zero,Vector2.one,new Vector4(1,0,0,0),new Vector4(0,1,0,0),prototype,false,null,false,true,false,true,false,null,true,false)};
            foreach(var item in invalid)
            {
                bool rejected=false;try{keepMethod.Invoke(item,field);}catch(TargetInvocationException e)when(e.InnerException is ArgumentException){rejected=true;}
                Require(rejected,"Invalid retention combination accepted");
            }
            foreach(var bad in new[]{new object[]{new Vector4(float.NaN,0,0,1),Vector4.zero,Vector4.zero,1f},new object[]{Vector4.zero,Vector4.zero,Vector4.zero,0f}})
            {
                bool rejected=false;try{keepMethod.Invoke(plain,bad);}catch(TargetInvocationException e)when(e.InnerException is ArgumentException){rejected=true;}
                Require(rejected,"Nonfinite coefficient or invalid handedness accepted");
            }
            var zero=keepMethod.Invoke(plain,new object[]{Vector4.zero,Vector4.zero,Vector4.zero,1f});
            bool zeroRejected=false;
            try{T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,new object[]{ArrayOf(input,Input(zero,face)),owner,paint,null});}
            catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){zeroRejected=true;}
            Require(zeroRejected,"Zero field direction accepted");
            Debug.Log($"PASS source tangent opt-in: {checks} real Compose/native codec vertices; default/projected controls; five invalid modes; Surface/UV/attributes/topology retained. No varying-field equivalence claim.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
