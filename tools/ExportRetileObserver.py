"""Emit an isolated observer copy of current production Retile; never edit runtime code."""
import argparse
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("output", type=Path)
parser.add_argument("--cpu", action="store_true", help="Use raw UV bytes and actual Plan; return observed objects without Unity model allocation.")
args = parser.parse_args()
source = root / "src/BridgeBuilder/Bridges/Common/Movable/MovableStructureRetile.cs"
raw = source.read_bytes()
code = raw.decode("utf-8-sig")

def replace_once(before, after):
    global code
    if code.count(before) != 1:
        raise RuntimeError("Production observer anchor changed: " + before)
    code = code.replace(before, after)

replace_once("var frames=new Dictionary<int,MovableUvFrame>();", """var observerParents=new Dictionary<int,int>();
        var observerRetained=new List<int>();
        var observerFaces=new Dictionary<int,List<(int Face,int FragmentTriangle)>>();
        var frames=new Dictionary<int,MovableUvFrame>();""")
replace_once("if(tiles.PreserveSourceUv(renderName,meshIndex,di))continue;",
             "if(tiles.PreserveSourceUv(renderName,meshIndex,di)){observerRetained.Add(di);continue;}")
replace_once("fragmentUnits.Add(mapping,unit);", """fragmentUnits.Add(mapping,unit);
                        observerParents.Add(mapping,di);""")
replace_once("unit.Min=q;unit.Max=q;units.Add(domain,unit);", """unit.Min=q;unit.Max=q;units.Add(domain,unit);
                    if(!observerParents.ContainsKey(domain))observerParents.Add(domain,domain);
                    observerFaces.Add(domain,new());""")
replace_once("for(int corner=0;corner<3;corner++)\n                {\n                    var q=UnitUv", """observerFaces[domain].Add((face,fragmentDomains.ContainsKey(face)?at/3:-1));
                for(int corner=0;corner<3;corner++)
                {
                    var q=UnitUv""")
replace_once("var slots=tiles.Build(surface,units.Values);", """// The harness receives the actual production objects before material selection.
        RetileObserver.Capture(name,renderName,meshIndex,surface,units,observerParents,observerFaces);
        throw new RetileObserver.Complete();
        var slots=tiles.Build(surface,units.Values);""")
args.output.mkdir(parents=True, exist_ok=True)
if args.cpu:
    # Preserve the production planning body, cutting only the native model input
    # and output boundaries. The host invokes plans from this same assembly.
    start = code.index("        var recipe=MovableUvDomains.For(renderName,meshIndex);")
    end = code.index("        // The harness receives the actual production objects")
    body = code[start:end]
    first = body.index("        if(recipe==null")
    uv = body.index("        Vector2 Uv2(int index)")
    body = body[:first] + """        if(recipe==null || recipe.Items.Length==0)return Array.Empty<object>();
        var tiles=new MovableStructureTiles(style,nativeNetwork);
        if(tiles.NativeNetworkIdentity(renderName,meshIndex,sourceCid))
            return new object[]{new Dictionary<int,MovableStructurePacking.Unit>(),new Dictionary<int,int>(),
                new Dictionary<int,List<(int Face,int FragmentTriangle)>>(),new List<int>(),true};
        float Uv(int index,int axis) => !uvHalf
            ? BitConverter.ToSingle(uvBytes,index*uvStride+axis*4)
            : (float)new half {value=BitConverter.ToUInt16(uvBytes,index*uvStride+axis*2)};
""" + body[uv:]
    body = body.replace("if(frames.Count==0){models.Add(original);surfaces.Add(tiles.NativeSurface(surface));return;}",
                        "if(frames.Count==0)return new object[]{new Dictionary<int,MovableStructurePacking.Unit>(),observerParents,observerFaces,observerRetained,false};")
    code = code[:code.index("    internal static void Append(")] + """    internal static object[] Observe(string renderName,int meshIndex,
        MovableStructureModel.Plan plan,byte[] uvBytes,int uvStride,bool uvHalf,string style,bool nativeNetwork,string sourceCid)
    {
""" + body + "        return new object[]{units,observerParents,observerFaces,observerRetained,false};\n    }\n}\n"
    code = code.replace("internal static class MovableStructureRetile", "internal static class CpuRetileObserver")
target = args.output / source.name
target.write_text(code, encoding="utf-8", newline="\n")
(args.output / "RetileObserver.cs").write_text("""using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
namespace BridgeBuilder.Bridges;
internal static class RetileObserver
{
    internal sealed class Complete : Exception {}
    internal static Action<string,string,int,SurfaceAsset,
        Dictionary<int,MovableStructurePacking.Unit>,Dictionary<int,int>,
        Dictionary<int,List<(int Face,int FragmentTriangle)>>> Sink;
    internal static void Capture(string name,string renderName,int meshIndex,SurfaceAsset surface,
        Dictionary<int,MovableStructurePacking.Unit> units,Dictionary<int,int> originalDomains,
        Dictionary<int,List<(int Face,int FragmentTriangle)>> sourceFaces)
    {
        if(Sink==null)throw new InvalidOperationException("Install the offline observer sink first.");
        Sink(name,renderName,meshIndex,surface,units,originalDomains,sourceFaces);
    }
}
""", encoding="utf-8")
(args.output / "retile-observer-source.json").write_text(json.dumps({
    "source": str(source), "sha256": hashlib.sha256(raw).hexdigest(),
    "observerSha256": hashlib.sha256(target.read_bytes()).hexdigest(),
    "scope": "Actual Retile emission before tiles.Build; material construction intentionally stopped.",
}, indent=2) + "\n", encoding="utf-8")
print(target)
if args.cpu:
    runner = args.output / "runner"
    runner.mkdir(exist_ok=True)
    checker = root / "tools/MovablePlanUvCheck"
    for item in checker.glob("*.cs"):
        if item.name != "Program.cs":
            (runner / item.name).write_bytes(item.read_bytes())
    program = (checker / "Program.cs").read_text()
    program = program[:program.index('        var positions=Field<Vector3[]>(plan,"Positions");')]
    program = program.replace('bool probeParentSamplers=', 'bool fingerprints=args.Contains("--plan-fingerprints");\nargs=args.Where(a=>a!="--plan-fingerprints").ToArray();\nbool probeParentSamplers=')
    program = program.replace('bool fingerprints=', 'var stylesArgument=args.SingleOrDefault(a=>a.StartsWith("--styles="));\nvar selectedStyles=stylesArgument?.Substring(9).Split(\',\').ToHashSet(StringComparer.Ordinal);\nargs=args.Where(a=>!a.StartsWith("--styles=")).ToArray();\nbool fingerprints=')
    program = program.replace('bool fingerprints=', '''var networkArgument=args.Single(a=>a.StartsWith("--network-evidence="));
var networkPath=networkArgument.Substring("--network-evidence=".Length);
var networkEvidence=JsonDocument.Parse(File.ReadAllText(networkPath)).RootElement.EnumerateArray()
    .ToDictionary(e=>(e.GetProperty("style").GetString()!,e.GetProperty("render").GetString()!,e.GetProperty("mesh").GetInt32()),
        e=>e.GetProperty("sourceCid").GetString()!);
args=args.Where(a=>!a.StartsWith("--network-evidence=")).ToArray();
bool fingerprints=''')
    program = program.replace('    evaluatedInputs++;', '    if(selectedStyles!=null)\n    {\n        var styleMatch=System.Text.RegularExpressions.Regex.Match(name,@"^(PedestrianDraw|Draw|Lift)Bridge(\\d{2})");\n        if(!styleMatch.Success || !selectedStyles.Contains(styleMatch.Groups[1].Value+styleMatch.Groups[2].Value))continue;\n    }\n    evaluatedInputs++;')
    program = program.replace('var nativeCodec=new NativeUvCodec(Path.Combine(managed,"../Plugins/x86_64/PipelinePlugin.dll"));', '')
    program = program.replace('var preparationUnits = new Dictionary<string, object>();',
        'using var output=new StreamWriter(args[3]+".units.ndjson");\nusing var sourceReport=new StreamWriter(args[3]+".source-uv.ndjson");\nusing var planReport=new StreamWriter(args[3]+".plans.ndjson");\nint observedPlans=0,observedUnits=0,retainedDomains=0;')
    program += r'''
        observedPlans++;
        planReport.WriteLine(JsonSerializer.Serialize(new{name,implementation,family,lod,mesh,delta,scenario=scenario.Label,
            upper=scenario.Upper,lower=scenario.Lower,left=scenario.Left,right=scenario.Right,sha256=PlanFingerprint.Hash(plan)}));
        if(fingerprints)continue;
        var rawUv=new byte[inputUv.Length*8];
        var corrected=(Vector2[])inputUv.Clone();
        foreach(var correction in Field<KeyValuePair<int,Vector2>[]>(plan,"UvCorrections"))corrected[correction.Key]=correction.Value;
        for(int i=0;i<corrected.Length;i++)
        {BitConverter.GetBytes(corrected[i].x).CopyTo(rawUv,i*8);BitConverter.GetBytes(corrected[i].y).CopyTo(rawUv,i*8+4);}
        var match=System.Text.RegularExpressions.Regex.Match(name,@"^(PedestrianDraw|Draw|Lift)Bridge(\d{2})");
        if(!match.Success)throw new Exception("Unknown style: "+name);
        string style=match.Groups[1].Value+match.Groups[2].Value;
        var observe=mod.GetType("BridgeBuilder.Bridges.CpuRetileObserver")!.GetMethod("Observe",BindingFlags.Static|BindingFlags.NonPublic)!;
        bool nativeNetwork=networkEvidence.TryGetValue((style,name,mesh),out var sourceCid);
        var result=(object[])observe.Invoke(null,new object[]{name,mesh,plan,rawUv,8,false,style,nativeNetwork,sourceCid??""})!;
        if(result.Length==0)continue;
        var emitted=(System.Collections.IDictionary)result[0];
        var parents=(System.Collections.IDictionary)result[1];
        var faces=(System.Collections.IDictionary)result[2];
        var retained=(List<int>)result[3];
        retainedDomains+=retained.Count;
        sourceReport.WriteLine(JsonSerializer.Serialize(new{name,implementation,family,lod,mesh,delta,scenario=scenario.Label,
            upper=scenario.Upper,lower=scenario.Lower,left=scenario.Left,right=scenario.Right,style,
            sourceUvDomains=retained,sourceFaces=retained.Sum(d=>domains.Items[d].Faces.Length),
            nativeNetwork=(bool)result[4],nativeNetworkDomains=(bool)result[4]?Enumerable.Range(0,domains.Items.Length).ToArray():Array.Empty<int>(),
            sourceCid=nativeNetwork?sourceCid:null,
            policy="Verified prototype non-tiling mapping; retain corrected/interpolated source UV. Expected density change is not a density pass."}));
        float[] V(Vector2 v)=>new[]{v.x,v.y};
        object? Exact(object unit,string field)
        {
            var value=unit.GetType().GetField(field,members)!.GetValue(unit);
            if(value==null)return null;
            var dictionary=(System.Collections.IDictionary)value;
            return dictionary.Keys.Cast<object>()
                .Select(key=>new{key=V((Vector2)key),value=dictionary[key] is Vector2 v?(object)V(v):dictionary[key]}).ToArray();
        }
        foreach(System.Collections.DictionaryEntry entry in emitted)
        {
            var u=entry.Value!;var b=Field<Vector4>(u,"Bounds");
            var charts=Field<Array>(u,"Charts").Cast<object>().Select(chart=>
                Field<Array>(chart,"_loops").Cast<Array>().Select(loop=>loop.Cast<object>().Select(p=>new[]{Field<double>(p,"X"),Field<double>(p,"Y")}).ToArray()).ToArray()).ToArray();
            output.WriteLine(JsonSerializer.Serialize(new{name,implementation,family,lod,mesh,delta,scenario=scenario.Label,
                upper=scenario.Upper,lower=scenario.Lower,left=scenario.Left,right=scenario.Right,style,
                id=entry.Key,originalDomain=parents[entry.Key],sourceFaces=((System.Collections.IEnumerable)faces[entry.Key]!).Cast<object>().Select(p=>new[]{Field<int>(p,"Item1"),Field<int>(p,"Item2")}),
                triangle=Field<bool>(u,"Triangle"),repeatOnly=Field<bool>(u,"RepeatOnly"),retainedAxes=Field<int>(u,"RetainedAxes"),
                a=V(Field<Vector2>(u,"A")),b=V(Field<Vector2>(u,"B")),c=V(Field<Vector2>(u,"C")),bounds=new[]{b.x,b.y,b.z,b.w},
                points=Field<List<Vector2>>(u,"Points").Select(V),exactCoordinates=Exact(u,"ExactCoordinates"),exactSourceUvs=Exact(u,"ExactSourceUvs"),
                exactPoints=u.GetType().GetField("ExactPoints",members)!.GetValue(u),charts,
                canUseSource=(bool)u.GetType().GetProperty("CanUseSource",members)!.GetValue(u)!}));
            observedUnits++;
        }
    }
}
File.WriteAllText(args[3],JsonSerializer.Serialize(new{observedPlans,observedUnits,retainedDomains,
    assemblySha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(args[0]))),
    fixtureSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(args[2]))),
    networkEvidenceSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(networkPath))),
    input="Fixture v2 Float32 UV with actual plan corrections; raw native Float16 fixture fidelity not claimed.",
    scope="Production Retile pre-material body; emitted Unit objects and exact original provenance."},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Observed {observedPlans} actual plans and {observedUnits} production units.");
'''
    (runner / "Program.cs").write_text(program, encoding="utf-8")
    (runner / "PlanFingerprint.cs").write_text(r'''using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
internal static class PlanFingerprint
{
    internal static string Hash(object value)
    {
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))Write(writer,value);
        stream.Position=0;return Convert.ToHexString(SHA256.HashData(stream));
    }
    private static void Write(BinaryWriter w,object? value)
    {
        w.Write(value!=null);if(value==null)return;
        var type=value.GetType();w.Write(type.FullName!);
        switch(value)
        {
            case bool b:w.Write(b);return;
            case int i:w.Write(i);return;
            case float f:w.Write(f);return;
            case double d:w.Write(d);return;
            case Vector2 v:w.Write(v.x);w.Write(v.y);return;
            case Vector3 v:w.Write(v.x);w.Write(v.y);w.Write(v.z);return;
            case Array a:w.Write(a.Length);foreach(var item in a)Write(w,item);return;
        }
        var fields=type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).OrderBy(f=>f.Name,StringComparer.Ordinal).ToArray();
        if(fields.Length==0)throw new Exception("Unsupported plan field: "+type);
        w.Write(fields.Length);foreach(var field in fields){w.Write(field.Name);Write(w,field.GetValue(value));}
    }
}
''', encoding="utf-8")
    project = (checker / "MovablePlanUvCheck.csproj").read_text().replace("../../src/", root.as_posix()+"/src/")
    project = project.replace("</Project>", '<Import Project="'+root.as_posix()+'/tools/Directory.Build.targets" /></Project>')
    (runner / "RetileObserverRunner.csproj").write_text(project, encoding="utf-8")
