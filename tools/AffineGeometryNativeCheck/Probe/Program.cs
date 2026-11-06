using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEngine;
using Unity.Mathematics;

public static class GameMonoProbe
{
    private const BindingFlags Methods=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    private const BindingFlags Fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    private static long _floatValues;
    public static void Run()
    {
        string report=Environment.GetEnvironmentVariable("BB_AFFINE_REPORT");
        try
        {
            string baseline=Environment.GetEnvironmentVariable("BB_AFFINE_BASELINE"),candidate=Environment.GetEnvironmentVariable("BB_AFFINE_CANDIDATE");
            var before=Assembly.Load(File.ReadAllBytes(baseline));var after=Assembly.Load(File.ReadAllBytes(candidate));
            if(before.ManifestModule.ModuleVersionId==after.ManifestModule.ModuleVersionId)throw new Exception("Comparison assemblies have the same identity.");
            foreach(var assembly in new[]{before,after})
            {
                var worker=assembly.GetType("BridgeBuilder.Bridges.AffineGeometry",true).GetField("_worker",Methods);
                if(worker==null)throw new Exception("Cannot establish matching outer-LOD worker mode.");
                worker.SetValue(null,true);
            }
            File.WriteAllText(report,"corelib="+typeof(object).Assembly.Location+"\nbaseline="+baseline+" sha256="+Hash(baseline)+" mvid="+before.ManifestModule.ModuleVersionId+"\ncandidate="+candidate+" sha256="+Hash(candidate)+" mvid="+after.ManifestModule.ModuleVersionId+"\n");
            File.AppendAllText(report,"validationAdapter="+Environment.GetEnvironmentVariable("BB_AFFINE_VALIDATION_ADAPTER")+"\n");
            File.AppendAllText(report,"threading=both baseline and candidate in outer-LOD worker mode; nested vertex scheduling suppressed\n");
            int cases=0;
            string manifest=Environment.GetEnvironmentVariable("BB_AFFINE_MANIFEST");
            foreach(string line in File.ReadAllLines(manifest))
            {
                if(string.IsNullOrWhiteSpace(line)||line.StartsWith("#"))continue;
                string[] cells=line.Split('\t');if(cells.Length!=4)throw new Exception("Invalid fixture row: "+line);
                string name=cells[0],method=cells[1].Split('+')[0].Split('@')[0];bool layoutChain=cells[1].Contains("+layout");int lod=int.Parse(cells[2]);
                string nativeName=cells[1].Contains("@")?cells[1].Split('@')[1]:null;
                string mesh=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifest),cells[3]));
                Vector3[] source;int[] indices;
                using(var input=new BinaryReader(File.OpenRead(mesh)))
                {
                    int count=input.ReadInt32(),length=input.ReadInt32();source=new Vector3[count];indices=new int[length];
                    for(int i=0;i<count;i++)source[i]=new Vector3(input.ReadSingle(),input.ReadSingle(),input.ReadSingle());
                    for(int i=0;i<length;i++)indices[i]=input.ReadInt32();
                    if(input.BaseStream.Position!=input.BaseStream.Length)throw new Exception("Fixture length mismatch.");
                }
                var left=before.GetType("BridgeBuilder.Bridges."+name,true).GetMethod(method,Methods);
                var right=after.GetType("BridgeBuilder.Bridges."+name,true).GetMethod(method,Methods);
                if(left==null || right==null)throw new Exception(name+"."+method+" lod="+lod+" missing method: baseline="+(left==null)+" candidate="+(right==null));
                long coldBefore=0,coldAfter=0,warmBefore=0,warmAfter=0;
                var pairedBefore=new System.Collections.Generic.List<long>();var pairedAfter=new System.Collections.Generic.List<long>();
                foreach(float delta in layoutChain?new[]{-2f,0f,8.5f}:new[]{-8f,BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)),0f,8.5f,float.MaxValue})
                foreach(int scenario in layoutChain?new[]{0,1,2}:new[]{0})
                {
                    object Invoke(MethodInfo target)
                    {
                        using var parameterScope = LifecycleCases.Begin(target.DeclaringType.Assembly);
                        if(nativeName!=null)
                        {
                            var original=Array.ConvertAll(source,p=>new float3(p.x,p.y,p.z));var moved=(float3[])original.Clone();
                            var parameters=target.GetParameters();
                            var values=parameters.Select(p=>p.IsOut?null:p.ParameterType==typeof(string)?(object)nativeName:
                                p.ParameterType==typeof(float3[])?(p.Name=="source"?original:moved):p.ParameterType==typeof(float)?
                                (p.Name=="rightDelta"?delta*.5f:delta):p.ParameterType==typeof(bool)?false:throw new Exception("Unsupported fixed parameter "+p)).ToArray();
                            if(!(bool)target.Invoke(null,values))throw new Exception("Native fixed fixture was not recognized: "+nativeName);
                            var outputs=new System.Collections.Generic.List<object>();
                            for(int i=0;i<parameters.Length;i++)if(parameters[i].IsOut || parameters[i].Name=="moved")outputs.Add(values[i]);
                            return outputs.ToArray();
                        }
                        var positions=(Vector3[])source.Clone();int coordinate=0;
                        var arguments=target.GetParameters().Select(p=>p.ParameterType==typeof(Vector3[])?(object)(coordinate++==0?source:positions):
                            p.ParameterType==typeof(int[])?indices:p.ParameterType==typeof(int[][])?new[]{indices}:
                            p.ParameterType==typeof(float)?delta:p.ParameterType==typeof(int)?(p.Name=="lod"?lod:0):
                            p.ParameterType==typeof(bool)?false:throw new Exception("Unsupported parameter "+p)).ToArray();
                        object result=target.Invoke(null,arguments);
                        if(layoutChain)
                        {
                            var assembly=target.DeclaringType.Assembly;object layout=Layout(assembly,delta,scenario);
                            string[] stages=name.StartsWith("Draw02")?new[]{"Draw02Underside.Apply","Draw02EndClosure.Apply","Draw02WalkEndClosure.Apply","Draw02FixedCurbs.Remove","Draw02FixedSidewalks.Apply"}:new[]{"Draw03JointLayout.Apply"};
                            foreach(string stage in stages)
                            {
                                var parts=stage.Split('.');var step=assembly.GetType("BridgeBuilder.Bridges."+parts[0],true).GetMethod(parts[1],Methods);
                                var values=step.GetParameters().Select(p=>p.ParameterType==typeof(int)?(object)lod:p.ParameterType==typeof(float)?delta:p.ParameterType.Name=="MovableSelectedRoadLayout"?layout:result).ToArray();
                                result=step.Invoke(null,values);
                            }
                        }
                        return target.ReturnType==typeof(void)?positions:result;
                    }
                    long start=Stopwatch.GetTimestamp();object baselineValue=Invoke(left);long elapsedBefore=Stopwatch.GetTimestamp()-start;
                    LifecycleCases.Capture=coldBefore==0;
                    start=Stopwatch.GetTimestamp();object candidateValue=LifecycleCases.InvokeCaptured(after,()=>Invoke(right));long elapsedAfter=Stopwatch.GetTimestamp()-start;
                    if(coldBefore==0){coldBefore=elapsedBefore;coldAfter=elapsedAfter;}
                    Compare(baselineValue,candidateValue,name+"/"+lod+"/"+delta+"/layout"+scenario);cases++;
                    if(LifecycleCases.Capture){LifecycleCases.AssertReleased(candidateValue,report,name+"/"+lod);LifecycleCases.Capture=false;}
                    if(delta==8.5f && scenario==0)
                        for(int repeat=0;repeat<9;repeat++)
                        {
                            long a,b;
                            if((repeat&1)==0){start=Stopwatch.GetTimestamp();Invoke(left);a=Stopwatch.GetTimestamp()-start;start=Stopwatch.GetTimestamp();Invoke(right);b=Stopwatch.GetTimestamp()-start;}
                            else{start=Stopwatch.GetTimestamp();Invoke(right);b=Stopwatch.GetTimestamp()-start;start=Stopwatch.GetTimestamp();Invoke(left);a=Stopwatch.GetTimestamp()-start;}
                            warmBefore+=a;warmAfter+=b;pairedBefore.Add(a);pairedAfter.Add(b);
                        }
                }
                pairedBefore.Sort();pairedAfter.Sort();
                File.AppendAllText(report,"PASS "+name+"."+cells[1]+" lod="+lod+" originalVertices="+source.Length+" indices="+indices.Length+" fixtureSha256="+Hash(mesh)+" firstTicks="+coldBefore+"/"+coldAfter+" warm9Ticks="+warmBefore+"/"+warmAfter+" pairedMedianTicks="+pairedBefore[4]+"/"+pairedAfter[4]+" frequency="+Stopwatch.Frequency+"\n");
            }
            File.AppendAllText(report,"PASS cases="+cases+" exactFloatComparisons="+_floatValues+"\n");
            LifecycleCases.Run(after,report);
            CoreCases.Run(before,after,report);
            var packed=after.GetType("BridgeBuilder.Bridges.StructureCoordinateProgram",true).GetMethod("Arithmetic",Methods);
            File.WriteAllText(report+".pointer",packed.MethodHandle.GetFunctionPointer().ToInt64().ToString());
        }
        catch(Exception error){File.AppendAllText(report,error.ToString());throw;}
    }
    private static string Hash(string path){using var sha=SHA256.Create();using var stream=File.OpenRead(path);return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
    private static object Layout(Assembly assembly,float delta,int scenario)
    {
        var type=assembly.GetType("BridgeBuilder.Bridges.MovableSelectedRoadLayout",true);
        object result=FormatterServices.GetUninitializedObject(type);
        void Set(string name,object value)=>type.GetField(name,Fields).SetValue(result,value);
        float left=scenario==1?0f:scenario==2?4.5f:3.5f,right=scenario==2?0f:scenario==1?1.5f:3.5f;
        Set("LeftSidewalk",left==0f?Vector2.zero:new Vector2(-6f-delta-left,-6f-delta));
        Set("RightSidewalk",right==0f?Vector2.zero:new Vector2(6f+delta,6f+delta+right));
        Set("SurfaceSpan",new Vector2(-6f-delta-left,6f+delta+right));
        var laneType=assembly.GetType("BridgeBuilder.Bridges.MovableRoadLaneLayout+Lane",true);
        var kind=assembly.GetType("BridgeBuilder.Bridges.MovableRoadLaneLayout+Kind",true);
        var constructor=laneType.GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single(c=>c.GetParameters().Length==14);
        var lanes=Array.CreateInstance(laneType,2);
        for(int side=0;side<2;side++)
        {
            float x=side==0?-3f-delta*.5f:3f+delta*.5f;
            float bend=scenario==0?0f:side==0?-.6f:.35f;
            lanes.SetValue(constructor.Invoke(new object[]{x,-1000f,x+bend,-333f,x-bend,333f,x,1000f,3.5f,3.5f,scenario==0?3.5f:4.25f,1f,Enum.ToObject(kind,1),(uint)0}),side);
        }
        Set("Lanes",lanes);Set("LaneStations",new[]{-1000f,1000f});Set("CarLaneCenters",new[]{-3f-delta*.5f,3f+delta*.5f});
        Set("Prototype","SIMD regression reconstructed layout");Set("SidewalkOccurrences",Array.Empty<bool>());
        return result;
    }
    private static void Compare(object a,object b,string label)
    {
        if(a==null || b==null){if(a!=null||b!=null)throw new Exception(label+" null mismatch");return;}
        if(a is float3[] triples && b is float3[] otherTriples)
        {
            if(triples.Length!=otherTriples.Length)throw new Exception(label+" coordinate count mismatch");
            _floatValues+=(long)triples.Length*3;
            for(int i=0;i<triples.Length;i++)
                if(BitConverter.SingleToInt32Bits(triples[i].x)!=BitConverter.SingleToInt32Bits(otherTriples[i].x)||
                    BitConverter.SingleToInt32Bits(triples[i].y)!=BitConverter.SingleToInt32Bits(otherTriples[i].y)||
                    BitConverter.SingleToInt32Bits(triples[i].z)!=BitConverter.SingleToInt32Bits(otherTriples[i].z))throw new Exception(label+" float3 "+i+" differs");
            return;
        }
        if(a is Vector3[] points && b is Vector3[] otherPoints)
        {
            if(points.Length!=otherPoints.Length)throw new Exception(label+" position count mismatch");
            _floatValues+=(long)points.Length*3;
            for(int i=0;i<points.Length;i++)
                if(BitConverter.SingleToInt32Bits(points[i].x)!=BitConverter.SingleToInt32Bits(otherPoints[i].x)
                    || BitConverter.SingleToInt32Bits(points[i].y)!=BitConverter.SingleToInt32Bits(otherPoints[i].y)
                    || BitConverter.SingleToInt32Bits(points[i].z)!=BitConverter.SingleToInt32Bits(otherPoints[i].z))
                    throw new Exception(label+" vertex "+i+" differs: "+points[i]+" / "+otherPoints[i]);
            return;
        }
        if(a is int[] integers && b is int[] otherIntegers)
        {if(integers.Length!=otherIntegers.Length)throw new Exception(label+" index count mismatch");for(int i=0;i<integers.Length;i++)if(integers[i]!=otherIntegers[i])throw new Exception(label+" index "+i+" differs");return;}
        if(a is bool[] flags && b is bool[] otherFlags)
        {if(flags.Length!=otherFlags.Length)throw new Exception(label+" flag count mismatch");for(int i=0;i<flags.Length;i++)if(flags[i]!=otherFlags[i])throw new Exception(label+" flag "+i+" differs");return;}
        if(a is float f && b is float g){_floatValues++;if(BitConverter.SingleToInt32Bits(f)!=BitConverter.SingleToInt32Bits(g))throw new Exception(label+" bits "+BitConverter.SingleToInt32Bits(f).ToString("X8")+" != "+BitConverter.SingleToInt32Bits(g).ToString("X8"));return;}
        if(a is Array x && b is Array y){if(x.Length!=y.Length)throw new Exception(label+" array length mismatch");for(int i=0;i<x.Length;i++)Compare(x.GetValue(i),y.GetValue(i),label+"["+i+"]");return;}
        if(a.GetType().IsPrimitive || a is string || a.GetType().IsEnum){if(!a.Equals(b))throw new Exception(label+" value mismatch");return;}
        if(a.GetType().FullName!=b.GetType().FullName)throw new Exception(label+" type mismatch");
        foreach(var field in a.GetType().GetFields(Fields))
        {
            var other=b.GetType().GetField(field.Name,Fields);if(other==null)throw new Exception(label+" field missing "+field.Name);
            Compare(field.GetValue(a),other.GetValue(b),label+"."+field.Name);
        }
    }
}
