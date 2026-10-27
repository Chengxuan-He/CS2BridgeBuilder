using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;

var managed=Path.GetFullPath(args[1]);
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>
{
    var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
    return File.Exists(path)?Assembly.LoadFrom(path):null;
};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
const BindingFlags members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,members)!.GetValue(value)!;
using var input=JsonDocument.Parse(File.ReadAllText(args[2]));
var rows=new List<object>();
int domains=0,faces=0;
foreach(var mesh in input.RootElement.GetProperty("meshes").EnumerateArray())
{
    string name=mesh.GetProperty("render").GetString()!;
    var source=mesh.GetProperty("positions").EnumerateArray().Select(p=>new Vector3(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle())).ToArray();
    var uv=mesh.GetProperty("uv").EnumerateArray().Select(p=>new Vector2(p[0].GetSingle(),p[1].GetSingle())).ToArray();
    var indices=mesh.GetProperty("indices").EnumerateArray().Select(p=>p.GetInt32()).ToArray();
    int lod=mesh.GetProperty("lod").GetInt32();
    bool glass=mesh.GetProperty("glass").GetBoolean();
    var method=mod.GetType("BridgeBuilder.Bridges."+(glass?"PedestrianDraw01Glass":"PedestrianDraw01SupportGeometry"))!.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic)!;
    foreach(float delta in new[]{-1.25f,-.25f,0f,2f,8f,24f})
    {
        object plan=method.Invoke(null,glass?new object[]{lod,source,indices,delta}:new object[]{false,lod,source,indices,delta})!;
        var target=Field<Vector3[]>(plan,"Positions");
        int checkedDomains=0,checkedFaces=0;
        foreach(var d in mesh.GetProperty("domains").EnumerateArray())
        {
            int a=d.GetProperty("a").GetInt32(),b=d.GetProperty("b").GetInt32(),c=d.GetProperty("c").GetInt32();
            var frame=new MovableUvFrame(source[a],source[b],source[c],uv[a],uv[b],uv[c],target[a],target[b],target[c]);
            if(frame.HasStretch)throw new Exception("Rigid original unexpectedly stretched: "+name);
            foreach(var f in d.GetProperty("faces").EnumerateArray())
            {
                int face=f.GetInt32();
                for(int corner=0;corner<3;corner++)
                {
                    int vertex=indices[face*3+corner];
                    if((frame.Map(source[vertex],target[vertex],uv[vertex])-uv[vertex]).sqrMagnitude>1e-12f)
                        throw new Exception("Production original-UV branch rejected: "+name);
                }
                checkedFaces++;
            }
            checkedDomains++;
        }
        domains+=checkedDomains;faces+=checkedFaces;
        rows.Add(new{render=name,delta,domains=checkedDomains,faces=checkedFaces,productionAddFrameRequiresRetile=false});
    }
}
// Invoke the actual compiled production selector. PedestrianDraw01 must retain
// the exact source object, not a prepared copy or conversion for these rigid parts.
var tilesType=mod.GetType("BridgeBuilder.Bridges.MovableStructureTiles")!;
var tiles=Activator.CreateInstance(tilesType,members,null,new object[]{"PedestrianDraw01"},null)!;
var native=tilesType.GetMethod("NativeSurface",members)!;
var surface=RuntimeHelpers.GetUninitializedObject(native.GetParameters()[0].ParameterType);
bool identity=ReferenceEquals(surface,native.Invoke(tiles,new[]{surface}));
if(!identity)throw new Exception("Native surface identity changed");
var result=new{status="PASS",modSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))).ToLowerInvariant(),
    scenarioDomains=domains,scenarioFaces=faces,sourceSurfaceIdentityPreserved=identity,rows,
    scope="Actual compiled glass/support geometry plus production UV frame and NativeSurface selector; finite six-width scenarios. Does not run the game renderer."};
File.WriteAllText(args[3],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PASS native original reuse: {domains} scenario-domains, {faces} scenario-faces; source Surface identity retained.");
