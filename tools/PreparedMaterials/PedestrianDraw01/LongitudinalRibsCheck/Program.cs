using System.Reflection;
using System.Text.Json;
using UnityEngine;

// Actual production plan, including all overlapping width maps and road-removal tables.
var managed=Path.GetFullPath(args[1]);
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>
{
    var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
    return File.Exists(path)?Assembly.LoadFrom(path):null;
};
var mod=Assembly.LoadFrom(Path.GetFullPath(args[0]));
var create=mod.GetType("BridgeBuilder.Bridges.PedestrianDraw01StructurePlan")!.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic)!;
const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
T Field<T>(object plan,string name)=>(T)plan.GetType().GetField(name,fields)!.GetValue(plan)!;
using var input=JsonDocument.Parse(File.ReadAllText(args[2]));
int cases=0,faces=0;
foreach(var level in input.RootElement.EnumerateArray())
{
    int lod=level.GetProperty("lod").GetInt32();
    var source=level.GetProperty("positions").EnumerateArray().Select(p=>new Vector3(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle())).ToArray();
    var indices=level.GetProperty("indices").EnumerateArray().Select(p=>p.GetInt32()).ToArray();
    foreach(float delta in new[]{-2f,0f,2f,8f,24f})
    {
        var plan=create.Invoke(null,new object[]{lod,source,indices,delta})!;
        var target=Field<Vector3[]>(plan,"Positions");
        var remove=Field<bool[][]>(plan,"RemoveTriangles");
        var fragments=Field<Array>(plan,"Fragments");
        var frames=Field<Array>(plan,"FrameTransforms");
        var selected=new HashSet<int>();
        var sourceFaces=new HashSet<int>();
        foreach(var member in level.GetProperty("members").EnumerateArray())
        {
            float centre=member.GetProperty("centre").GetSingle();
            var vertices=member.GetProperty("vertices").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
            float shift=centre/4f*delta;
            foreach(int i in vertices)
            {
                selected.Add(i);
                if(target[i].x != source[i].x+shift || target[i].y!=source[i].y || target[i].z!=source[i].z)
                    throw new Exception($"Incorrect rib map LOD{lod} delta{delta} vertex{i}");
            }
            float actual=vertices.Max(i=>target[i].x)-vertices.Min(i=>target[i].x);
            float original=vertices.Max(i=>source[i].x)-vertices.Min(i=>source[i].x);
            if(Math.Abs(actual-original)>2e-6f)throw new Exception("Rib section widened");
            foreach(var f in member.GetProperty("faces").EnumerateArray())
            {
                int face=f.GetInt32();sourceFaces.Add(face);
                if(remove[0][face])throw new Exception("Rib face removed");
                faces++;
            }
        }
        foreach(var f in fragments)
            if(sourceFaces.Contains(Field<int>(f!,"Triangle")))throw new Exception("Rib face replaced by fragment");
        foreach(var f in frames)
        {
            int i=(int)f!.GetType().GetProperty("Key")!.GetValue(f)!;
            if(selected.Contains(i))throw new Exception("Rigid rib frame changed");
        }
        cases++;
    }
}
Console.WriteLine($"PASS {cases} actual production plans; {faces} retained rib face checks; exact affine positions, Y/Z and central width; no rib fragments/frame transforms. Far native shell has no independent ribs.");
