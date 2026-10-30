using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

// Run compiled production Create/TryBind in an isolated Unity editor. This is
// not a game-world construction request and does not publish any game assets.
public static class PedestrianAtlasPlanCheck
{
    [Serializable] public sealed class Failure { public float halfWidthDelta; public int cell; public float crossY; public Vector3 a,b,c; }
    [Serializable] public sealed class Failures { public Failure[] winding; }
    [Serializable] public sealed class FieldTriangle {public float delta;public float[] raw;public uint[] packed;}
    [Serializable] public sealed class FieldTriangles {public FieldTriangle[] triangles;}
    const BindingFlags Instance=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    const BindingFlags Static=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static object Get(object value,string name) {var field=value.GetType().GetField(name,Instance);return field!=null?field.GetValue(value):value.GetType().GetProperty(name,Instance).GetValue(value);}
    static object New(Type type,params object[] args)=>Activator.CreateInstance(type,Instance,null,args,null);
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        string managed=Arg("-gameManaged");
        AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=> {
            string path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
            return File.Exists(path)?Assembly.LoadFrom(path):null;
        };
        var mod=Assembly.LoadFrom(Arg("-modAssembly"));
        Type T(string name)=>mod.GetType("BridgeBuilder.Bridges."+name,true);
        var create=T("PedestrianDraw02AtlasAppearance").GetMethod("Prepared",Static);
        var native=FormatterServices.GetUninitializedObject(create.GetParameters()[0].ParameterType);
        var right=FormatterServices.GetUninitializedObject(create.GetParameters()[0].ParameterType);
        var left=FormatterServices.GetUninitializedObject(create.GetParameters()[0].ParameterType);
        bool composeEnabled=args.Contains("-nativeSurface");
        void ReadSurface(object asset,string path,string cid) {
            var type=asset.GetType();
            using(var reader=new BinaryReader(File.OpenRead(path)))type.GetMethod("ReadHeader",Instance).Invoke(asset,new object[]{reader});
            var property=type.GetProperty("id");var hashType=property.PropertyType.GetField("guid").FieldType;
            var hash=hashType.GetMethod("Parse",new[]{typeof(string)}).Invoke(null,new object[]{cid});
            property.SetValue(asset,New(property.PropertyType,hash));
        }
        if(composeEnabled) {
            ReadSurface(native,Arg("-nativeSurface"),"a1b3ca16b946cc34fc031e376ba03d71");
            ReadSurface(right,Arg("-rightSurface"),"192abd6474e10998755ea80510e7324c");
            ReadSurface(left,Arg("-leftSurface"),"5bf3e56979b3a3cdae31d7ea0106d0ff");
        }
        var geometry=T("MovableRoadGeometry");
        var vertexType=T("MovableRoadGeometry+Vertex");
        var triangleType=T("MovableRoadGeometry+Triangle");
        var bind=geometry.GetMethod("TryBind",Static);
        var failures=new System.Collections.Generic.List<Failure>();
        var fieldTriangles=new System.Collections.Generic.List<FieldTriangle>();
        bool complexityPassed=true;
        int projectionFailures=0;
        var project=T("MovableRoadProjection").GetMethod("Prepared",Static);
        object Vertex(float x,float z)=>New(vertexType,new Vector3(x,10,z),Vector3.up,
            new Vector4(1,0,0,-1),new Vector4(.2f,.3f,.4f,.5f),Color.white);
        foreach(float delta in new[]{-.5f,0f,2f,8f})
        {
            var level=create.Invoke(null,new object[]{native,right,left,new Pose(Vector3.zero,Quaternion.identity),delta});
            var patches=(Array)Get(level,"Patches");
            Require(patches.Length==102,"Changed measured appearance cell count.");
            foreach(object patch in patches) {
                bool prepared=(bool)Get(patch,"PreparedUnit");
                Require(prepared ? ReferenceEquals(Get(patch,"Surface"),right)||ReferenceEquals(Get(patch,"Surface"),left)
                    : ReferenceEquals(Get(patch,"Surface"),native),"Source Surface identity changed.");
                Require(prepared ? !(bool)Get(patch,"RepeatU")&&(bool)Get(patch,"RepeatV")
                    : (bool)Get(patch,"SourceUv"),"Unexpected mapping route.");
            }
            float width=4+delta;
            var triangles=Array.CreateInstance(triangleType,2);
            triangles.SetValue(New(triangleType,Vertex(-width,-76),Vertex(-width,76),Vertex(width,76),0,0,0),0);
            triangles.SetValue(New(triangleType,Vertex(-width,-76),Vertex(width,76),Vertex(width,-76),0,0,1),1);
            var oldPhysical=T("PedestrianDraw02RoadPartitions").GetMethod("Create",Static).Invoke(null,new object[]{delta});
            var physical=T("PedestrianDraw02LocalAppearance").GetMethod("Physical",Static).Invoke(null,new object[]{delta});
            Array Bind(object partition,int bones,Array input=null,int materialCount=1) {
                var parameters=new object[]{input??triangles,partition,bones,materialCount,null,null,false,null,null};
                Require((bool)bind.Invoke(null,parameters),"Production partition rejected: "+parameters[5]);
                return (Array)parameters[4];
            }
            var original=Bind(physical,3);
            var previous=Bind(oldPhysical,3);
            int Classify(object node,Vector3 p) {
                while(!(bool)Get(node,"IsGap")&&Convert.ToInt32(Get(node,"Bone"))<0) {
                    var frame=(Matrix4x4)Get(node,"ObjectToSeam");
                    node=Get(node,frame.MultiplyPoint3x4(p).z<0?"Negative":"Positive");
                }
                return (bool)Get(node,"IsGap")?-1:Convert.ToInt32(Get(node,"Bone"));
            }
            int boneMismatches=0;
            foreach(object bound in original) {
                var face=Get(bound,"Geometry");
                var a=(Vector3)Get(Get(face,"A"),"Position");
                var b=(Vector3)Get(Get(face,"B"),"Position");
                var c=(Vector3)Get(Get(face,"C"),"Position");
                if(Classify(oldPhysical,(a+b+c)/3)!=Convert.ToInt32(Get(bound,"Bone")))boneMismatches++;
            }
            Debug.Log($"PED02 PHYSICAL oldTriangles={previous.Length} newTriangles={original.Length} centroidBoneMismatches={boneMismatches}");
            Require(boneMismatches==0,"Physical bone ownership changed.");
            var physicalGeometry=Array.CreateInstance(triangleType,original.Length);
            for(int i=0;i<original.Length;i++)physicalGeometry.SetValue(Get(original.GetValue(i),"Geometry"),i);
            var direct=Bind(Get(level,"Carriageway"),patches.Length);
            var sequential=Bind(Get(level,"Carriageway"),patches.Length,physicalGeometry);
            var joint=geometry.GetMethod("CombinePartitions",Static).Invoke(null,new object[]{
                physical,Get(level,"Carriageway"),Matrix4x4.identity,new[]{false,true,true},patches.Length});
            var mapped=Bind(joint,patches.Length*2);
            foreach(object bound in mapped) {
                int label=Convert.ToInt32(Get(bound,"Bone")),cell=label%patches.Length;
                var jointFace=Get(bound,"Geometry");
                var centre=((Vector3)Get(Get(jointFace,"A"),"Position")+(Vector3)Get(Get(jointFace,"B"),"Position")+(Vector3)Get(Get(jointFace,"C"),"Position"))/3;
                Require((label>=patches.Length)==(Classify(oldPhysical,centre)>0),"Combined physical moving label changed.");
                Require(Classify(Get(level,"Carriageway"),centre)==cell,"Combined appearance label changed.");
                var patch=patches.GetValue(cell);
                if(!(bool)Get(patch,"PreparedUnit"))continue;
                var face=Get(bound,"Geometry");
                foreach(string corner in new[]{"A","B","C"}) {
                    var vertex=Get(face,corner);
                    try {
                        var output=project.Invoke(null,new[]{vertex,patch});
                        var oldUv=(Vector4)Get(vertex,"UV");var newUv=(Vector4)Get(output,"UV");
                        Require(oldUv.z==newUv.z&&oldUv.w==newUv.w&&Get(vertex,"Color").Equals(Get(output,"Color")),"Prepared attributes changed.");
                    } catch(Exception ex) {
                        projectionFailures++;
                        if(projectionFailures<=8)Debug.Log($"PED02 PROJECTION_FAILURE delta={delta:R} cell={cell} position={((Vector3)Get(vertex,"Position")).ToString("R")} U={((Vector4)Get(patch,"U")).ToString("R")} V={((Vector4)Get(patch,"V")).ToString("R")} min={Get(patch,"Min")} max={Get(patch,"Max")} error={ex.GetBaseException().Message}");
                    }
                }
            }
            double Area(Array faces,string label) {
                double total=0,negativeArea=0;int reversed=0;
                foreach(object bound in faces) {
                    object face=Get(bound,"Geometry");
                    var a=(Vector3)Get(Get(face,"A"),"Position");
                    var b=(Vector3)Get(Get(face,"B"),"Position");
                    var c=(Vector3)Get(Get(face,"C"),"Position");
                    var cross=Vector3.Cross(b-a,c-a);
                    if(cross.y<0){reversed++;negativeArea-=cross.y*.5;
                        int cell=Convert.ToInt32(Get(bound,"Bone"));
                        if(label=="appearance")failures.Add(new Failure{halfWidthDelta=delta,cell=cell,crossY=cross.y,a=a,b=b,c=c});
                        if(reversed<=3)Debug.Log($"PED02 REVERSED {label} cell={cell} cross={cross.y:R} A={a:R} B={b:R} C={c:R}");}
                    total+=cross.magnitude*.5;
                }
                Debug.Log($"PED02 WINDING {label} reversed={reversed} negativeArea={negativeArea:R}");
                return total;
            }
            double previousArea=Area(previous,"oldPhysical"),before=Area(original,"physical"),after=Area(mapped,"appearance");
            Require(Math.Abs(before-previousArea)<.001,"Physical footprint changed.");
            Debug.Log($"PED02 PLAN delta={delta} physicalTriangles={original.Length} directAppearanceTriangles={direct.Length} sequentialTriangles={sequential.Length} combinedTriangles={mapped.Length} physicalArea={before:R} appearanceArea={after:R} areaDifference={after-before:R}");
            if(composeEnabled) {
                var boundType=T("MovableRoadGeometry+BoundTriangle");var remapped=Array.CreateInstance(boundType,mapped.Length);
                for(int n=0;n<mapped.Length;n++) {
                    var face=mapped.GetValue(n);uint label=(uint)Get(face,"Bone");
                    remapped.SetValue(New(boundType,Get(face,"Geometry"),label%(uint)patches.Length),n);
                }
                var inputType=T("MovableSurfaceAtlas+Input");var input=New(inputType,false);
                void Set(object o,string name,object value)=>o.GetType().GetField(name,Instance).SetValue(o,value);
                Set(input,"Faces",remapped);Set(input,"Patches",patches);Set(input,"Native",native);
                var sources=Array.CreateInstance(native.GetType(),1);sources.SetValue(native,0);Set(input,"Materials",sources);
                var inputs=Array.CreateInstance(inputType,1);inputs.SetValue(input,0);
                var owner=FormatterServices.GetUninitializedObject(T("MovableRoadMaterial"));Set(owner,"<StyleId>k__BackingField","PedestrianDraw02");
                var paint=Array.CreateInstance(T("MovableNativeDecals+Region"),0);
                var call=new object[]{inputs,owner,paint,null};
                var generated=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,call)).GetValue(0);
                Require(generated.Length==mapped.Length,"Compose added receiver cuts.");
                int fieldVertices=0;
                for(int n=0;n<generated.Length;n++) {
                    var patch=patches.GetValue(Convert.ToInt32(Get(remapped.GetValue(n),"Bone")));
                    var fieldValue=Get(patch,"SourceTangent");
                    if(fieldValue==null)continue;
                    var field=(Matrix4x4)fieldValue;var face=generated.GetValue(n);
                    foreach(string name in new[]{"A","B","C"}) {
                        var vertex=Get(face,name);var position=(Vector3)Get(vertex,"Position");
                        var expected=field*new Vector4(position.x,position.y,position.z,1);
                        var tangent=(Vector4)Get(vertex,"Tangent");
                        Require(tangent.Equals(expected),"Actual Compose did not preserve raw source tangent field.");fieldVertices++;
                    }
                }
                var unsupported=New(inputType,true);Set(unsupported,"Faces",remapped);Set(unsupported,"Patches",patches);Set(unsupported,"Native",native);Set(unsupported,"Materials",sources);
                var unsupportedInputs=Array.CreateInstance(inputType,1);unsupportedInputs.SetValue(unsupported,0);
                var unsupportedCall=new object[]{unsupportedInputs,owner,paint,null};
                var retained=(Array)((Array)T("MovableSurfaceAtlas").GetMethod("Compose",Static).Invoke(null,unsupportedCall)).GetValue(0);
                Require(retained.Length==remapped.Length,"Unsupported selected object face count changed.");
                for(int n=0;n<retained.Length;n++)foreach(string name in new[]{"A","B","C"}) {
                    var beforeVertex=Get(Get(remapped.GetValue(n),"Geometry"),name);var afterVertex=Get(retained.GetValue(n),name);
                    foreach(string attribute in new[]{"Position","Normal","Tangent","UV","Color"})Require(Get(beforeVertex,attribute).Equals(Get(afterVertex,attribute)),"Unsupported input attribute changed.");
                }
                Debug.Log($"PED02 FIELD_CONSUMER delta={delta:R} rawFieldVertices={fieldVertices} selectedObjectRetained={retained.Length}");
                var materials=(Array)((Array)call[3]).GetValue(0);
                var rebound=Bind(physical,3,generated,materials.Length);
                var build=new object[]{"PedestrianDraw02 native consumer verification",rebound,materials.Length,3,null,null};
                Require((bool)T("MovableRoadModel").GetMethod("TryBuild",Static).Invoke(null,build),"Native model build: "+build[5]);
                var model=build[4];Mesh upload=null;
                try {
                    upload=(Mesh)model.GetType().GetMethod("ToUnityMesh",Instance).Invoke(model,new object[]{true});
                    var uv4=new System.Collections.Generic.List<Vector4>();upload.GetUVs(0,uv4);
                    var positions=upload.vertices;Require(positions.Length==rebound.Length*3,"Uploaded vertex count changed.");
                    using(var read=Mesh.AcquireReadOnlyMeshData(upload)) {
                        foreach(int attribute in new[]{1,2,3,13}) {
                            var entry=((IEnumerable)model).Cast<object>().Single(e=>(int)(VertexAttribute)Get(e,"attribute")==attribute);
                            var raw=((IEnumerable)Get(entry,"data")).Cast<byte>().ToArray();
                            int stream=upload.GetVertexAttributeStream((VertexAttribute)attribute),start=upload.GetVertexAttributeOffset((VertexAttribute)attribute),stride=upload.GetVertexBufferStride(stream);
                            var bytes=read[0].GetVertexData<byte>(stream);
                            for(int v=0;v<positions.Length;v++)for(int b=0;b<4;b++)Require(bytes[v*stride+start+b]==raw[v*4+b],"Native packed attribute changed on upload.");
                        }
                        int bs=upload.GetVertexAttributeStream(VertexAttribute.BlendIndices),bo=upload.GetVertexAttributeOffset(VertexAttribute.BlendIndices),strideB=upload.GetVertexBufferStride(bs);
                        var boneBytes=read[0].GetVertexData<byte>(bs);
                        int ts=upload.GetVertexAttributeStream(VertexAttribute.Tangent),to=upload.GetVertexAttributeOffset(VertexAttribute.Tangent),tstride=upload.GetVertexBufferStride(ts);
                        var tangentBytes=read[0].GetVertexData<byte>(ts);
                        for(int n=0;n<rebound.Length;n++) {
                            var boundFace=rebound.GetValue(n);var face=Get(boundFace,"Geometry");var bone=BitConverter.GetBytes((uint)Get(boundFace,"Bone"));
                            var centre=((Vector3)Get(Get(face,"A"),"Position")+(Vector3)Get(Get(face,"B"),"Position")+(Vector3)Get(Get(face,"C"),"Position"))/3;
                            var field=Get(patches.GetValue(Classify(Get(level,"Carriageway"),centre)),"SourceTangent");
                            var record=field==null?null:new FieldTriangle{delta=delta,raw=new float[9],packed=new uint[3]};
                            int corner=0;
                            foreach(string name in new[]{"A","B","C"}) {
                                int v=n*3+corner++;var originalVertex=Get(face,name);
                                Require(positions[v].Equals(Get(originalVertex,"Position"))&&uv4[v].Equals(Get(originalVertex,"UV")),"Native position/UV4 changed.");
                                for(int b=0;b<4;b++)Require(boneBytes[v*strideB+bo+b]==bone[b],"Native bone changed.");
                                if(record!=null) {
                                    var raw=(Vector4)Get(originalVertex,"Tangent");int index=corner-1;
                                    for(int component=0;component<3;component++)record.raw[index*3+component]=raw[component];
                                    int start=v*tstride+to;
                                    record.packed[index]=(uint)(tangentBytes[start]|tangentBytes[start+1]<<8|tangentBytes[start+2]<<16|tangentBytes[start+3]<<24);
                                }
                            }
                            if(record!=null)fieldTriangles.Add(record);
                        }
                    }
                    for(int material=0;material<materials.Length;material++) {
                        var expected=Enumerable.Range(0,rebound.Length).Where(n=>(int)Get(Get(rebound.GetValue(n),"Geometry"),"Material")==material).SelectMany(n=>new[]{n*3,n*3+1,n*3+2}).ToArray();
                        Require(upload.GetIndices(material).SequenceEqual(expected),"Native triangle/material indices changed.");
                    }
                    Debug.Log($"PED02 UPLOAD delta={delta:R} vertices={positions.Length} triangles={rebound.Length}; packed normal/tangent/color/bones, UV4 and indices PASS");
                } finally {(model as IDisposable)?.Dispose();if(upload!=null)UnityEngine.Object.DestroyImmediate(upload);}
                Debug.Log($"PED02 COMPOSE delta={delta:R} input={mapped.Length} output={generated.Length} rebound={rebound.Length}; native descriptor direct-consumer route; selected-input classification excluded");
            }
            Require(Math.Abs(after-before)<.001,"Appearance coverage differs from measured physical partition.");
            complexityPassed&=mapped.Length<=276;
        }
        File.WriteAllText(Arg("-failureReport"),JsonUtility.ToJson(new Failures{winding=failures.ToArray()},true));
        File.WriteAllText(Arg("-failureReport")+".tangent.json",JsonUtility.ToJson(new FieldTriangles{triangles=fieldTriangles.ToArray()},true));
        Debug.Log($"PED02 GEOMETRY windingPass={failures.Count==0} prototypeTriangleCeilingPass={complexityPassed}; sourceIdentity=PASS; areaComparison=PASS");
        Debug.Log($"PED02 PROJECTION failures={projectionFailures}");
        Require(failures.Count==0&&complexityPassed&&projectionFailures==0,"PedestrianDraw02 acceptance failed: inspect separate winding/complexity/projection measurements.");
        Debug.Log("PASS compiled PedestrianDraw02 source identity and conservative geometry checks; native rendering and normals remain pending.");
    }
}
