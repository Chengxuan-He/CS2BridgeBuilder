using BridgeBuilder.Bridges;
using UnityEngine;

for (var lod = 0; lod < 3; lod++)
{
    using var reader = new BinaryReader(File.OpenRead(Path.Combine(args[0], $"lift02-widthcheck-{lod}.bin")));
    var count = reader.ReadInt32();
    var indexCount = reader.ReadInt32();
    var source = new Vector3[count];
    for (var i = 0; i < count; i++)
        source[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    var indices = new int[indexCount];
    for (var i = 0; i < indexCount; i++) indices[i] = reader.ReadInt32();
    var before = (Vector3[])source.Clone();
    foreach (var delta in new[] { -4.99f, -4f, -3f, -.0266017913818359375f, -.01f, 0f, 2f, 8f })
    {
        var converted = Lift02StructurePlan.Create(lod, source, indices, delta);
        var expectedCount = lod switch { 0 => 41540, 1 => 25498, _ => 6 };
        if (converted.RemoveTriangles[0].Count(x => x) != expectedCount ||
            !converted.SourceIndices[0].SequenceEqual(indices) ||
            converted.Positions.Any(p => !float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z)))
            throw new Exception($"LOD {lod} delta {delta}: combined road conversion invariant failed.");
        var plan = Lift02StructureWidthPlan.Create(lod, source, indices, delta);
        if (!plan.SourceIndices[0].SequenceEqual(indices) || plan.RemoveTriangles[0].Any(x => x))
            throw new Exception("Width composition changed native topology.");
        for (var i = 0; i < count; i++)
        {
            var p = plan.Positions[i];
            if (!source[i].Equals(before[i]) || p.y != source[i].y || p.z != source[i].z ||
                !float.IsFinite(p.x) || delta == 0f && p.x != source[i].x)
                throw new Exception($"LOD {lod} vertex {i}: width/source invariant failed.");
        }
    }
    Console.WriteLine($"PASS LOD{lod}: {count} vertices, {indexCount / 3} triangles; eight composed widths, native topology, Y/Z and source immutability.");
    var replacement = Lift02StructurePlan.Create(lod, source, indices, 2f);
    var removed = replacement.RemoveTriangles[0];
    var expectedRemoved = lod switch { 0 => 41540, 1 => 25498, _ => 6 };
    if (removed.Count(x => x) != expectedRemoved || !replacement.SourceIndices[0].SequenceEqual(indices))
        throw new Exception($"LOD {lod}: native road replacement membership changed.");
    if (lod == 2)
    {
        if (!replacement.ReconstructsAuthoredPositions) throw new Exception("Far alignment lost reconstruction metadata.");
        var changed = 0;
        var widthOnly = Lift02StructureWidthPlan.Create(lod, source, indices, 2f);
        for (var i = 0; i < count; i++)
        {
            var p = replacement.Positions[i];
            if (p.x != widthOnly.Positions[i].x) throw new Exception("Boundary alignment changed width.");
            if (p.y == source[i].y && p.z == source[i].z) continue;
            changed++;
            if (!(i >= 610 && i <= 661 || i >= 1814 && i <= 1865 || i >= 2508 && i <= 2531))
                throw new Exception("Boundary alignment changed another part.");
            if (p.y != source[i].y && p.y != 9.2666015625f ||
                p.z != source[i].z && Math.Abs(p.z) != 82f && Math.Abs(p.z) != 106f)
                throw new Exception("Boundary alignment does not match detailed datums.");
        }
        if (changed != 72) throw new Exception("Boundary membership changed.");
        var expected = new HashSet<int> { 410, 411, 1178, 1179, 1618, 1619 };
        for (var i = 0; i < removed.Length; i++)
            if (removed[i] != expected.Contains(i)) throw new Exception("Far underside/sidewall was removed.");
    }
    var damaged = (int[])indices.Clone();
    var firstRemoved = Array.FindIndex(removed, x => x);
    damaged[firstRemoved * 3] = (damaged[firstRemoved * 3] + 1) % count;
    var rejected = false;
    try { Lift02StructurePlan.Create(lod, source, damaged, 2f); }
    catch (InvalidOperationException) { rejected = true; }
    if (!rejected) throw new Exception("Changed native topology was accepted.");
    Console.WriteLine($"PASS LOD{lod}: {expectedRemoved} replacement faces; changed topology rejected.");
}

namespace UnityEngine
{
    internal struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
}

namespace BridgeBuilder.Bridges
{
    internal static class MovableStructureModel
    {
        internal sealed class Plan
        {
            internal readonly Vector3[] SourcePositions, Positions;
            internal readonly bool ReconstructsAuthoredPositions;
            internal readonly int[][] SourceIndices;
            internal readonly bool[][] RemoveTriangles;
            internal Plan(Vector3[] source, Vector3[] positions, int[][] indices, bool[][] removed, bool reconstructsAuthoredPositions = false)
            { SourcePositions = source; Positions = positions; SourceIndices = indices; RemoveTriangles = removed; ReconstructsAuthoredPositions = reconstructsAuthoredPositions; }
        }
    }
}

