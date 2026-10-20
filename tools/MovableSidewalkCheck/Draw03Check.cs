using BridgeBuilder.Bridges;
using UnityEngine;

internal static class Draw03Check
{
    internal static void Run(string directory)
    {
        var cases = new (Vector2 Left, Vector2 Right, float Delta)[]
        {
            (new(-12.5f,-9f),new(9f,12.5f),0),
            (default,default,0),
            (new(-12.5f,-9f),default,0),
            (default,new(9f,12.5f),0),
            (new(-12.5f,-11f),new(7.5f,12.5f),0),
            (new(-20.5f,-17f),new(17f,20.5f),8)
        };
        for (var lod = 0; lod < 2; lod++)
        {
            using var reader = new BinaryReader(File.OpenRead(Path.Combine(directory, $"draw03-sidewalk-check-{lod}.bin")));
            var count = reader.ReadInt32();
            var indexCount = reader.ReadInt32();
            var source = Enumerable.Range(0, count).Select(_ => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())).ToArray();
            var indices = Enumerable.Range(0, indexCount).Select(_ => reader.ReadInt32()).ToArray();
            int[] ReadArray() => Enumerable.Range(0, reader.ReadInt32()).Select(_ => reader.ReadInt32()).ToArray();
            var left = ReadArray(); var right = ReadArray(); var car = ReadArray();
            var owned = ReadArray().ToHashSet();
            foreach (var entry in cases)
            foreach (var reverse in new[] { false, true })
            {
                var sample = new MovableClosedRoad.Result();
                var rotation = new Quaternion(reverse);
                void Add(Vector2 span)
                {
                    if (span.y <= span.x) return;
                    var i = sample.Occurrences.Length;
                    sample.Occurrences = sample.Occurrences.Append(new MovableClosedRoad.Occurrence("Sidewalk")).ToArray();
                    sample.Triangles.Add(new(i, rotation * new Vector3(span.x, 0, 0),
                        rotation * new Vector3(span.y, 0, 0), rotation * new Vector3(span.x, 0, 1)));
                }
                Add(entry.Left); Add(entry.Right);
                var layout = new MovableSelectedRoadLayout(sample, new Pose(default, rotation));
                var positions = source.Select(p => new Vector3(p.x + Math.Sign(p.x) * entry.Delta, p.y, p.z)).ToArray();
                var plan = new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[indexCount / 3] });
                var result = Draw03JointLayout.Apply(lod, plan, layout, entry.Delta);
                var removed = new HashSet<int>();
                if (entry.Left.y <= entry.Left.x) removed.UnionWith(left);
                if (entry.Right.y <= entry.Right.x) removed.UnionWith(right);
                for (var i = 0; i < result.RemoveTriangles[0].Length; i++)
                    if (result.RemoveTriangles[0][i] != removed.Contains(i))
                        throw new Exception("Draw03 joint removal differs from measured sidewalk faces.");
                if (car.Any(i => result.RemoveTriangles[0][i]))
                    throw new Exception("Draw03 sidewalk removal deleted carriageway.");
                for (var i = 0; i < count; i++)
                {
                    var p = result.Positions[i];
                    if (!float.IsFinite(p.x) || p.y != source[i].y || p.z != source[i].z ||
                        !owned.Contains(i) && p.x != positions[i].x)
                        throw new Exception("Draw03 joint adaptation changed unrelated geometry.");
                }
                if (!result.SourceIndices[0].SequenceEqual(indices))
                    throw new Exception("Draw03 joint adaptation rewrote native indices.");
            }
        }
        Console.WriteLine("PASS 24 Draw03 detailed joint/sidewalk/frame combinations; exact removal sets and carriageway preservation.");
    }
}
