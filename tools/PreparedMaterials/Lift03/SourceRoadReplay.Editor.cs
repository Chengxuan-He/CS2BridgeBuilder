using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Exercises the built DLL's measured domain tree. This is not a renderer acceptance test.
public static class Lift03SourceRoadReplay
{
    [Serializable] public sealed class Sample { public float[] position, uv; public double[] lower,upper; }
    [Serializable] public sealed class Case { public int lod; public string deck; public Sample[] samples; }
    [Serializable] public sealed class Fixture { public Case[] cases; }
    [Serializable] public sealed class Result { public int lod, samples, patches, partitionTriangles, wrapTriangles, reversedTriangles, degenerateTriangles; public string deck; public float halfDelta, maxSourcePixelError,maxUPixelError,maxVPixelError; public double maxUIntervalExcessPixels,maxVIntervalExcessPixels; public float[] worstPosition,worstSourceUv,worstMappedUv; public bool worstPrepared; public double inputArea, outputArea; public bool transformed; }
    [Serializable] public sealed class Report { public string dllSha256, scope; public Result[] results; }
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static object Field(object o, string name) => o.GetType().GetField(name, Flags).GetValue(o);
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        string Arg(string key) => args[Array.IndexOf(args, key) + 1];
        string dll = Arg("-candidateDll"), managed = Path.Combine(Arg("-gameData"), "Managed");
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
            var path = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        var assembly = Assembly.LoadFrom(dll);
        var type = assembly.GetType("BridgeBuilder.Bridges.Lift03SourceRoadMapping", true);
        var create = type.GetMethod("Create", Flags);
        // The mapping only passes this identity through. No native Surface loading is claimed.
        var surface = FormatterServices.GetUninitializedObject(create.GetParameters()[2].ParameterType);
        var fixture = JsonUtility.FromJson<Fixture>(File.ReadAllText(Arg("-fixture")));
        string unitsRoot = Path.Combine(Path.GetDirectoryName(Arg("-fixture")), "continuous-shared-grid-v3");
        var unitReport = JsonUtility.FromJson<PrepareRepeatUnits.Report>(File.ReadAllText(Path.Combine(unitsRoot,"bake.json")));
        var unitManifest = JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Path.Combine(unitsRoot,"manifest.json")));
        var results = new List<Result>();
        foreach (var item in fixture.cases)
        foreach (float delta in new[] { 0f, 2f })
        foreach (bool transformed in new[] { false, true })
        {
            var pose = transformed ? new Pose(new Vector3(3, 1, -7), Quaternion.Euler(0, 31, 0)) : Pose.identity;
            var matrix = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
            var unit = unitReport.units.Single(x=>x.id.Contains("."+item.deck+"."));
            var recipe = unitManifest.units.Single(x=>x.id==unit.id);
            var prepared = FormatterServices.GetUninitializedObject(create.GetParameters()[2].ParameterType);
            using(var reader = new BinaryReader(File.OpenRead(Path.Combine(unitsRoot,"output/Lift03/RepeatUnits",unit.key,"Repeat.Surface"))))
                prepared.GetType().GetMethod("ReadHeader",Flags).Invoke(prepared,new object[]{reader});
            var level = create.Invoke(null, new[] { Enum.Parse(create.GetParameters()[0].ParameterType, item.deck), (object)item.lod, surface, pose, delta, prepared });
            var patches = (Array)Field(level, "Patches");
            var regions = Field(level, "Carriageway");
            var result = new Result { lod = item.lod, deck = item.deck, halfDelta = delta, transformed = transformed, patches = patches.Length };
            foreach (var sample in item.samples)
            {
                float width = item.deck == "Main" ? 8f : 5f;
                var road = new Vector3(sample.position[0] * (width + delta) / width, sample.position[1], sample.position[2]);
                var point = matrix.inverse.MultiplyPoint3x4(road);
                var node = regions;
                while (Field(node, "Negative") != null)
                {
                    var frame = (Matrix4x4)Field(node, "ObjectToSeam");
                    if (frame.determinant == 0f) throw new InvalidDataException("Singular mapping plane.");
                    node = Field(node, frame.MultiplyPoint3x4(point).z < 0 ? "Negative" : "Positive");
                }
                if ((bool)Field(node, "IsGap")) throw new InvalidDataException("Unexpected mapping gap.");
                var patch = patches.GetValue((int)Field(node, "Bone"));
                bool isPrepared=(bool)Field(patch,"PreparedUnit");
                if (!ReferenceEquals(Field(patch, "Surface"), isPrepared?prepared:surface) || (bool)Field(patch, "SourceUv")==isPrepared || (bool)Field(patch,"SourceAtlasRepeat") || (bool)Field(patch, "RepeatU") || !(bool)Field(patch, "ProjectTangent"))
                    throw new InvalidDataException("Original atlas source invariants failed.");
                var p4 = new Vector4(point.x, point.y, point.z, 1);
                float u = Vector4.Dot((Vector4)Field(patch, "U"), p4), v = Vector4.Dot((Vector4)Field(patch, "V"), p4);
                if(isPrepared){var product=unit.channels[0];u=(float)((u*product.width+product.sourceOriginPixels[0])/4096);v=(float)(recipe.min[1]+(v-Math.Floor(v))*(recipe.max[1]-recipe.min[1]));}
                if (float.IsNaN(u) || float.IsNaN(v) || float.IsInfinity(u) || float.IsInfinity(v)) throw new InvalidDataException("Nonfinite source UV.");
                float eu=4096*Mathf.Abs(u-sample.uv[0]),ev=4096*Mathf.Abs(v-sample.uv[1]);
                result.maxUIntervalExcessPixels=Math.Max(result.maxUIntervalExcessPixels,4096*Math.Max(sample.lower[0]-u,u-sample.upper[0]));
                result.maxVIntervalExcessPixels=Math.Max(result.maxVIntervalExcessPixels,4096*Math.Max(sample.lower[1]-v,v-sample.upper[1]));
                if(Mathf.Max(eu,ev)>result.maxSourcePixelError){result.worstPosition=sample.position;result.worstSourceUv=sample.uv;result.worstMappedUv=new[]{u,v};result.worstPrepared=isPrepared;}
                result.maxUPixelError=Mathf.Max(result.maxUPixelError,eu);result.maxVPixelError=Mathf.Max(result.maxVPixelError,ev);
                result.maxSourcePixelError = Mathf.Max(result.maxSourcePixelError,Mathf.Max(eu,ev));
                result.samples++;
            }
            // Synthetic receiver: two selected-shape triangles spanning the measured length.
            // No prototype triangles are supplied to the runtime tree or wrap operation.
            var geometry = assembly.GetType("BridgeBuilder.Bridges.MovableRoadGeometry", true);
            var vertexType = geometry.GetNestedType("Vertex", Flags);
            var triangleType = geometry.GetNestedType("Triangle", Flags);
            object MakeVertex(float x, float z)
            {
                var position = matrix.inverse.MultiplyPoint3x4(new Vector3(x, 0, z));
                var normal = matrix.inverse.MultiplyVector(Vector3.up).normalized;
                var t = matrix.inverse.MultiplyVector(Vector3.right).normalized;
                return Activator.CreateInstance(vertexType, Flags, null, new object[] { position, normal, new Vector4(t.x,t.y,t.z,1), new Vector4(0,0,.25f,.75f), Color.white }, null);
            }
            float w = (item.deck == "Main" ? 8 : 5) + delta;
            object a = MakeVertex(-w,-127), b = MakeVertex(w,-127), c = MakeVertex(w,127), d = MakeVertex(-w,127);
            object MakeTriangle(object x,object y,object z,int id) => Activator.CreateInstance(triangleType,Flags,null,new object[]{x,y,z,0,7,id},null);
            var input = Array.CreateInstance(triangleType,2); input.SetValue(MakeTriangle(a,b,c,0),0);input.SetValue(MakeTriangle(a,c,d,1),1);
            object[] bindArgs = { input, regions, patches.Length, 1, null, null, false, null, null };
            if (!(bool)geometry.GetMethod("TryBind",Flags).Invoke(null,bindArgs)) throw new InvalidDataException("Synthetic receiver partition: "+bindArgs[5]);
            var bound = (Array)bindArgs[4]; result.partitionTriangles = bound.Length;
            var projectPrepared = assembly.GetType("BridgeBuilder.Bridges.MovableRoadProjection",true).GetMethod("Prepared",Flags);
            double Area(object triangle)
            {
                var av=(Vector3)Field(Field(triangle,"A"),"Position");var bv=(Vector3)Field(Field(triangle,"B"),"Position");var cv=(Vector3)Field(Field(triangle,"C"),"Position");
                double x1=(double)bv.x-av.x,y1=(double)bv.y-av.y,z1=(double)bv.z-av.z,x2=(double)cv.x-av.x,y2=(double)cv.y-av.y,z2=(double)cv.z-av.z;
                double x=y1*z2-z1*y2,y=z1*x2-x1*z2,z=x1*y2-y1*x2;return Math.Sqrt(x*x+y*y+z*z)*.5;
            }
            foreach(var triangle in input) result.inputArea+=Area(triangle);
            foreach(var piece in bound)
            {
                var triangle=Field(piece,"Geometry");var patch=patches.GetValue((int)(uint)Field(piece,"Bone"));
                if((bool)Field(patch,"SourceAtlasRepeat"))throw new InvalidDataException("Period wrap clipping is prohibited.");
                object MapVertex(string name)
                {
                    var before=Field(triangle,name);
                    if(!(bool)Field(patch,"PreparedUnit"))return before;
                    var after=projectPrepared.Invoke(null,new[]{before,patch});
                    if(!((Vector3)Field(before,"Position")).Equals((Vector3)Field(after,"Position")))throw new InvalidDataException("Prepared projection changed geometry.");
                    if(!((Vector3)Field(before,"Normal")).Equals((Vector3)Field(after,"Normal")))throw new InvalidDataException("Prepared projection changed source normal.");
                    return after;
                }
                var output=new[]{MakeTriangle(MapVertex("A"),MapVertex("B"),MapVertex("C"),(int)Field(triangle,"SourceTriangle"))};
                foreach(var mapped in output)
                {
                    if((int)Field(mapped,"Material")!=0 || (int)Field(mapped,"Occurrence")!=7 || (int)Field(mapped,"SourceTriangle")!=(int)Field(triangle,"SourceTriangle"))throw new InvalidDataException("Receiver identity changed.");
                    foreach(string name in new[]{"A","B","C"})
                    {
                        var uv=(Vector4)Field(Field(mapped,name),"UV");
                        if(uv.z!=.25f || uv.w!=.75f)throw new InvalidDataException("Unmapped UV channels changed.");
                        if((Color)Field(Field(mapped,name),"Color")!=Color.white)throw new InvalidDataException("Receiver color changed.");
                    }
                    var pa=(Vector3)Field(Field(mapped,"A"),"Position");var pb=(Vector3)Field(Field(mapped,"B"),"Position");var pc=(Vector3)Field(Field(mapped,"C"),"Position");
                    var receiverNormal=matrix.inverse.MultiplyVector(Vector3.up);
                    double dx1=(double)pb.x-pa.x,dy1=(double)pb.y-pa.y,dz1=(double)pb.z-pa.z,dx2=(double)pc.x-pa.x,dy2=(double)pc.y-pa.y,dz2=(double)pc.z-pa.z;
                    double winding=(dy1*dz2-dz1*dy2)*receiverNormal.x+(dz1*dx2-dx1*dz2)*receiverNormal.y+(dx1*dy2-dy1*dx2)*receiverNormal.z;
                    // Input a,b,c and a,c,d have negative winding relative to +Y.
                    if(winding>0)result.reversedTriangles++;if(winding==0)result.degenerateTriangles++;
                    result.outputArea+=Area(mapped);result.wrapTriangles++;
                }
            }
            results.Add(result);
        }
        using var sha = System.Security.Cryptography.SHA256.Create();
        var report = new Report { dllSha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(dll))).Replace("-", ""), results = results.ToArray(), scope = "Actual candidate C# domain tree, original source interiors, finite width changes and rigid pose; records float32 UV error. Synthetic two-triangle receiver also exercises actual TryBind and atlas wrap; reports area and complexity without asserting geometric acceptance. Surface identity stub only: no texture/native load/render or real selected-road bone/lane acceptance." };
        File.WriteAllText(Arg("-report"), JsonUtility.ToJson(report, true));
        Debug.Log("Lift03 source-road C# replay completed.");
    }
}
