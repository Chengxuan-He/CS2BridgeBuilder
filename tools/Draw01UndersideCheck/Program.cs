using System.Globalization;
using BridgeBuilder.Bridges;
using UnityEngine;

for (var lod = 0; lod < 2; lod++)
{
    var vertices = new List<Vector3>();
    var faces = new List<int[]>();
    var bones = new List<string>();
    var bone = "";
    foreach (var line in File.ReadLines(Path.Combine(args[0], "DrawBridge01" + (lod == 0 ? "" : "_LOD1") + " Mesh.mesh-0.obj")))
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) continue;
        if (parts[0] == "v") vertices.Add(new Vector3 { x = float.Parse(parts[1], CultureInfo.InvariantCulture), y = float.Parse(parts[2], CultureInfo.InvariantCulture), z = float.Parse(parts[3], CultureInfo.InvariantCulture) });
        if (parts[0] == "g") bone = parts[1];
        if (parts[0] == "f") { faces.Add(parts.Skip(1).Select(p => int.Parse(p.Split('/')[0]) - 1).ToArray()); bones.Add(bone); }
    }
    foreach (var spans in new[] {
        new[] { new Vector2(-8, 8), new Vector2(-10, -8), new Vector2(8, 11) },
        new[] { new Vector2(-8, 11), new Vector2(-10, -8), new Vector2(0, 0) },
        new[] { new Vector2(-10, 11), new Vector2(0, 0), new Vector2(0, 0) } })
    {
        var positions = vertices.ToArray();
        var removed = Enumerable.Repeat(true, faces.Count).ToArray();
        Draw01UndersideData.Apply(lod, new Draw01UndersideLayout(spans), positions, removed);
        var selected = Enumerable.Range(0, faces.Count).Where(i => !removed[i]).ToArray();
        var leaves = selected.GroupBy(i => bones[i]).ToArray();
        if (leaves.Length != 2) throw new Exception("Underside must cover both native leaf bones.");
        foreach (var leaf in leaves)
        {
            var ids = leaf.SelectMany(i => faces[i]).Distinct().ToArray();
            if (ids.Any(i => positions[i].y != vertices[i].y || positions[i].z != vertices[i].z)) throw new Exception("Original Y/Z changed.");
            if (ids.Min(i => positions[i].x) != spans.Min(s => s.x) || ids.Max(i => positions[i].x) != spans.Max(s => s.y)) throw new Exception("Leaf does not cover selected road width.");
            int missing = 0;
            for(int xi=0;xi<20;xi++)for(int zi=0;zi<40;zi++)
            {
                float x=spans[0].x+(spans[0].y-spans[0].x)*(xi+.5f)/20;
                float z=(1+(zi+.5f)*82/40)*(vertices[ids[0]].z<0?-1:1);
                bool Hit(int face)
                {
                    var a=positions[faces[face][0]];var b=positions[faces[face][1]];var c=positions[faces[face][2]];
                    if(Math.Abs(a.y-9.5f)>.001f || Math.Abs(b.y-9.5f)>.001f || Math.Abs(c.y-9.5f)>.001f)return false;
                    float Cross(float ax,float az,float bx,float bz)=>ax*bz-az*bx;
                    if(Cross(b.x-a.x,b.z-a.z,c.x-a.x,c.z-a.z)<=0)return false;
                    var ab=Cross(b.x-a.x,b.z-a.z,x-a.x,z-a.z);
                    var bc=Cross(c.x-b.x,c.z-b.z,x-b.x,z-b.z);
                    var ca=Cross(a.x-c.x,a.z-c.z,x-c.x,z-c.z);
                    return (ab>=0&&bc>=0&&ca>=0)||(ab<=0&&bc<=0&&ca<=0);
                }
                if(!leaf.Any(Hit))missing++;
            }
            Console.WriteLine($"LOD{lod} {leaf.Key}: uncovered bottom probes {missing}/800");
            if (missing != 0) throw new Exception("Underside has uncovered downward-facing samples.");
        }
        Console.WriteLine($"PASS LOD{lod} subset: {selected.Length} faces, two bones, full width, original Y/Z. LOD1 also supplies far supplement.");
    }
}

if (args.Length > 1)
    File.WriteAllLines(args[1], Enumerable.Range(0, 2).SelectMany(lod => Draw01UndersideUv.ForLod(lod)
        .Select(p => FormattableString.Invariant($"{lod},{p.Key},{p.Value.x:R},{p.Value.y:R}"))));

namespace UnityEngine {
    public struct Vector3 { public float x, y, z; }
    public struct Vector2 { public float x,y; public Vector2(float a,float b) { x=a;y=b; } }
    public static class Mathf { public static float LerpUnclamped(float a,float b,float t)=>a+(b-a)*t; }
}
namespace BridgeBuilder.Bridges {
    internal sealed class Draw01UndersideLayout { internal readonly Vector2[] Spans; internal Draw01UndersideLayout(Vector2[] spans)=>Spans=spans; }
}
