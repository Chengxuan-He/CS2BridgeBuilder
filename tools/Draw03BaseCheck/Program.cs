using System.IO.Compression;
using System.Text.Json;
using BridgeBuilder.Bridges;
using UnityEngine;

for (int lod = 0; lod < 2; lod++)
{
    using var partsJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(args[0], $"draw03-parts-{lod}.json")));
    var parts = partsJson.RootElement.EnumerateArray().Where(p => p.GetProperty("side").GetInt32() == 0).ToArray();
    HashSet<int> Members(params int[] ids) => ids.SelectMany(p => parts[p].GetProperty("vertices").EnumerateArray().Select(v => v.GetInt32())).ToHashSet();
    var moving = Members(0, 1, 88);
    var fixedBase = Members(3, 90);
    using var joints = JsonDocument.Parse(File.ReadAllText(Path.Combine(args[0], "draw03-sidewalk-joint-map.json")));
    var jointLevel = joints.RootElement.GetProperty("levels").EnumerateArray().Single(l => l.GetProperty("lod").GetInt32() == lod);
    var jointVertices = jointLevel.GetProperty("groups").EnumerateArray().SelectMany(g => g.GetProperty("vertices").EnumerateArray().Select(v => v.GetInt32()));
    if (moving.Overlaps(jointVertices) || moving.Overlaps(Members(2, 89, 74, 162, 6, 94, 7, 8, 84, 85, 95, 96, 172, 173)))
        throw new Exception("A downstream road/joint/platform/end-strip recipe overrides the moving base.");
    using var zip = ZipFile.OpenRead(Path.Combine(args[0], $"draw03-main-{lod}.npz"));
    using var reader = new BinaryReader(zip.GetEntry("positions.npy")!.Open());
    var magic = reader.ReadBytes(8);
    if (magic[0] != 147 || magic[6] != 1) throw new Exception("Expected NPY v1 positions.");
    var header = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadUInt16()));
    if (!header.Contains("'<f4'") || !header.Contains("False")) throw new Exception("Expected little-endian float32 row-major positions.");
    var source = new Vector3[lod == 0 ? 186078 : 146988];
    for (int i = 0; i < source.Length; i++) source[i] = new() { x = reader.ReadSingle(), y = reader.ReadSingle(), z = reader.ReadSingle() };
    foreach (float delta in new[] { -8f, -4f, -.1f, 0f, 2f, 8f })
    {
        var result = (Vector3[])source.Clone();
        Draw03Bases.Apply(lod, source, result, delta);
        for (int i = 0; i < source.Length; i++)
        {
            float x = source[i].x;
            float expected = moving.Contains(i) ? x + (x / 12.5f) * delta
                : fixedBase.Contains(i) && Math.Abs(x) > .000049591064453125f ? x + Math.Sign(x) * delta : x;
            if (result[i].x != expected || result[i].y != source[i].y || result[i].z != source[i].z)
                throw new Exception($"LOD{lod}, delta={delta}, vertex={i}: unexpected transform.");
        }
    }
    Console.WriteLine($"PASS LOD{lod}: {moving.Count} stretched vertices, {fixedBase.Count} translated/fixed vertices; all other vertices and Y/Z unchanged at six widths.");
    // Regression: all five longitudinal frames must share one scale; the centre
    // frame must not absorb the complete width increment on its own.
    var allParts = partsJson.RootElement.EnumerateArray().ToArray();
    var frameIds = lod == 0 ? new[] { 723,824,925,1026,1127 } : new[] { 763,864,965,1066,1167 };
    foreach (float delta in new[] { -8f,0f,2f,8f })
    {
        var result = (Vector3[])source.Clone();
        Draw03Truss.Apply(lod,source,result,delta);
        var fixedRanges = lod == 0
            ? new[] { (707,722),(1481,1510),(2178,2193),(2950,2979) }
            : new[] { (747,762),(1521,1550),(2266,2281),(3038,3067) };
        var fixedIds = fixedRanges.SelectMany(r => Enumerable.Range(r.Item1,r.Item2-r.Item1+1)).ToArray();
        if(fixedIds.Length != 92 || fixedIds.Any(i => allParts[i].GetProperty("bone").GetInt32()!=0))
            throw new Exception("Fixed end-truss ownership changed.");
        foreach (int part in frameIds.Concat(fixedIds))
        foreach (var id in allParts[part].GetProperty("vertices").EnumerateArray())
        {
            int i=id.GetInt32(); var expected=source[i].x+(source[i].x/12.5f)*delta;
            if(result[i].x!=expected || result[i].y!=source[i].y || result[i].z!=source[i].z)
                throw new Exception($"Truss frame {part} has an independent translation/stretch.");
        }
        foreach(int i in moving.Concat(fixedBase))
            if(result[i].x!=source[i].x)throw new Exception("Truss selection overlaps deck plates.");
    }
    Console.WriteLine($"PASS LOD{lod}: five moving frames and 92 bone-zero end-truss/bearing parts use one shared scale; plates excluded.");
}

namespace UnityEngine { public struct Vector3 { public float x, y, z; } }
