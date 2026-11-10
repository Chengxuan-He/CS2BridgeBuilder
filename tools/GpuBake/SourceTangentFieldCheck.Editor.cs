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
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
            var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var readHash=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
            var textureType=db.GetType("Colossal.IO.AssetDatabase.TextureAsset",true);
            var identifierType=db.GetType("Colossal.IO.AssetDatabase.Identifier",true);
            object Surface()
            {
                var result=FormatterServices.GetUninitializedObject(surfaceType);
                using(var reader=new BinaryReader(File.OpenRead(Arg("-surfaceFile"))))
                {
                    surfaceType.GetMethod("ReadHeader",Fields).Invoke(result,new object[]{reader});
                    foreach(var pair in new[]{("m_Floats","ReadStringFloatDictionary"),("m_Ints","ReadStringIntDictionary"),("m_Vectors","ReadStringVectorDictionary"),("m_Colors","ReadStringColorDictionary")})
                        Set(result,pair.Item1,binary.GetMethod(pair.Item2).Invoke(null,new object[]{reader}));
                    Require(reader.ReadByte()==255,"Missing native texture dictionary");
                    var textures=(System.Collections.IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string),textureType));
                    for(int n=reader.ReadInt32();n>0;n--)
                    {
                        string key=reader.ReadString();byte present=reader.ReadByte();object texture=null;
                        if(present==255){texture=FormatterServices.GetUninitializedObject(textureType);textureType.GetProperty("id").SetValue(texture,New(identifierType,readHash.Invoke(null,new object[]{reader})));}
                        else Require(present==0,"Invalid native texture marker");
                        textures.Add(key,texture);
                    }
                    Set(result,"m_Textures",textures);Set(result,"m_Keywords",binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader}));
                    Require(reader.BaseStream.Position==reader.BaseStream.Length,"Native descriptor trailing bytes");
                }
                // Keep the real decoded descriptor resident; texture handles are identities only.
                // This is not a game database/rendering test.
                Set(result,"m_InstanceRefCount",1);
                return result;
            }
            Type D(string name)=>db.GetType("Colossal.IO.AssetDatabase."+name,true);
            var global=FormatterServices.GetUninitializedObject(D("AssetDatabase"));
            D("AssetDatabase").GetField("s_Instance",Static).SetValue(null,global);
            var resources=ScriptableObject.CreateInstance(D("AssetDatabaseResources"));var library=New(D("MaterialLibrary"));
            var descriptions=D("MaterialLibrary").GetField("m_Materials",Fields);
            var list=(System.Collections.IList)Activator.CreateInstance(descriptions.FieldType);
            var description=D("MaterialLibrary").GetNestedType("MaterialDescription");var entry=New(description);
            description.GetField("m_Hash",Fields).SetValue(entry,0);
            var stacks=description.GetField("m_Stacks",Fields);stacks.SetValue(entry,Array.CreateInstance(stacks.FieldType.GetElementType(),0));
            description.GetField("m_Material",Fields).SetValue(entry,new Material(Shader.Find("Standard")));list.Add(entry);descriptions.SetValue(library,list);
            resources.GetType().GetField("m_MaterialLibrary",Fields).SetValue(resources,library);
            D("AssetDatabase").GetField("m_Resources",Fields).SetValue(global,resources);
            var original=Surface(); var prototype=Surface();
            var vertex=T("MovableRoadGeometry+Vertex"); var triangle=T("MovableRoadGeometry+Triangle");
            var bound=T("MovableRoadGeometry+BoundTriangle"); var patch=T("MovableExistingRoadSurface+Patch");
            var input=T("MovableSurfaceAtlas+Input");
            object V(float x,float z)=>New(vertex,new Vector3(x,0,z),new Vector3(0,1,1).normalized,new Vector4(1,0,0,-1),new Vector4(3+x,7+z,7,9),new Color(.2f,.3f,.4f,.7f));
            var face=New(triangle,V(0,0),V(1,0),V(0,1),0,5,23);
            // Reproduce Unity's magnitude cutoff without changing the projection
            // determinant threshold or dropping a small, nonzero-area triangle.
            var planeMethod=T("MovableSurfaceAtlas").GetMethod("TryPlaneNormal",Static) ?? T("MovableSurfaceAtlas").GetMethod("PlaneNormal",Static);
            bool Plane(object[] args){
                if(planeMethod.GetParameters().Length==3)return (bool)planeMethod.Invoke(null,args);
                try{args[1]=planeMethod.Invoke(null,new[]{args[0]});return true;}catch(TargetInvocationException e){args[2]=e.InnerException.Message;return false;}
            }
            var eligibility=T("MovableRoadGeometry").GetMethod("CanRepeatProjection",Static);
            foreach(float height in new[]{1f,1e-6f,1e-8f})
            {
                var thin=New(triangle,V(0,0),V(0,height),V(1.5f,0),0,5,23);
                var old=Vector3.Cross(new Vector3(0,0,height),new Vector3(1.5f,0,0)).normalized;
                object[] planeCall={thin,null,null};Require(Plane(planeCall),"Thin plane failed result: "+planeCall[2]);var actual=(Vector3)planeCall[1];
                Require(actual==Vector3.up,"Nonzero triangle lost its actual plane");
                bool Can(Vector3 n,Vector3 vertexNormal)=>(bool)eligibility.Invoke(null,new object[]{new Vector4(1,0,0,0),new Vector4(0,0,1,0),n,vertexNormal,vertexNormal,vertexNormal});
                Require(Can(actual,Vector3.up),"Valid thin receiver was rejected");
                Require(!Can(actual,Vector3.down),"Opposite authored normal was admitted");
                Require(!Can(actual,Vector3.right),"Singular authored normal was admitted");
                if(height<1e-5f)Require(old==Vector3.zero&&!Can(old,Vector3.up),"Fixture did not reproduce old gate failure");
            }
            object[] degenerateCall={New(triangle,V(0,0),V(1,0),V(2,0),0,5,23),null,null};
            bool degenerateRejected=!Plane(degenerateCall) && degenerateCall[2]!=null;
            Require(degenerateRejected,"Truly degenerate chart was admitted");
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
                object[] call={ArrayOf(input,Input(keep,src),Input(plain,src),Input(finite,src),Input(keep,src,true)),owner,paint,null,null};
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
            object[] pairCall={ArrayOf(input,pair),owner,paint,null,null};
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
                var rejected=keepMethod.Invoke(item,field);
                Require(!string.IsNullOrEmpty((string)Get(rejected,"Error")),"Invalid retention combination accepted");
                var failedOwner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));
                object[] failureCall={ArrayOf(input,Input(rejected,face)),failedOwner,paint,null,null};
                Require(T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,failureCall)==null,"Invalid patch succeeded in Compose");
                Require(((Array)failureCall[3]).Length==0,"Invalid patch returned material output");
            }
            foreach(var bad in new[]{new object[]{new Vector4(float.NaN,0,0,1),Vector4.zero,Vector4.zero,1f},new object[]{Vector4.zero,Vector4.zero,Vector4.zero,0f}})
            {
                var rejected=keepMethod.Invoke(plain,bad);
                Require(!string.IsNullOrEmpty((string)Get(rejected,"Error")),"Nonfinite coefficient or invalid handedness accepted");
            }
            var zero=keepMethod.Invoke(plain,new object[]{Vector4.zero,Vector4.zero,Vector4.zero,1f});
            object[] zeroCall={ArrayOf(input,Input(zero,face)),owner,paint,null,null};
            var zeroResult=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,zeroCall)).GetValue(0);
            foreach(string corner in new[]{"A","B","C"})
                Require(Get(Get(zeroResult.GetValue(0),corner),"Tangent").Equals(Get(Get(face,corner),"Tangent")),"Collapsed tangent field changed source frame");
            var collapsed=New(triangle,V(0,0),V(1,0),V(2,0),0,5,23);
            object[] collapsedCall={ArrayOf(input,Input(plain,collapsed)),owner,paint,null,null};
            var collapsedResult=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,collapsedCall)).GetValue(0);
            Require(collapsedResult.Length==1,"Collapsed receiver was rejected");
            var collapsedOutput=collapsedResult.GetValue(0);
            foreach(string fieldName in new[]{"Occurrence","SourceTriangle"})Require(Get(collapsedOutput,fieldName).Equals(Get(collapsed,fieldName)),"Collapsed receiver changed "+fieldName);
            foreach(string corner in new[]{"A","B","C"}){
                var before=Get(collapsed,corner);var after=Get(collapsedOutput,corner);
                foreach(string attribute in new[]{"Position","Normal","Tangent","Color"})Require(Get(before,attribute).Equals(Get(after,attribute)),"Collapsed receiver changed "+attribute);
                var position=(Vector3)Get(before,"Position");var uv=(Vector4)Get(after,"UV");var originalUv=(Vector4)Get(before,"UV");
                Require(uv==new Vector4(.01f*position.x+.2f,.02f*position.z+.3f,originalUv.z,originalUv.w),"Collapsed SourceUv patch projection differs");
            }
            Require(ReferenceEquals(((Array)((Array)collapsedCall[3]).GetValue(0)).GetValue((int)Get(collapsedOutput,"Material")),prototype),"Collapsed SourceUv patch lost its assigned material");
            Debug.Log($"PASS source tangent opt-in: {checks} real Compose/native codec vertices; default/projected controls; five invalid modes; Surface/UV/attributes/topology retained. No varying-field equivalence claim.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
