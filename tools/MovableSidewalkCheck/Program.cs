using BridgeBuilder.Bridges;
using UnityEngine;

// Native geometry fixtures are produced from the same reviewed source as
// Draw02FixedSidewalks.py, not from the generated C# membership table.
var cases = new (Vector2 Left, Vector2 Right)[]
{
    (new(-9.5f, -6f), new(6f, 9.5f)),
    (default, default),
    (new(-9.5f, -6f), default),
    (default, new(6f, 9.5f)),
    (new(-13f, -12f), new(7f, 12f)),
    (new(-6.2f, -6f), new(6f, 6.3f))
};
var checks = 0;
for (var lod = 0; lod < 3; lod++)
{
    using var reader = new BinaryReader(File.OpenRead(Path.Combine(args[0], $"draw02-sidewalk-check-{lod}.bin")));
    var count = reader.ReadInt32();
    var indexCount = reader.ReadInt32();
    var source = Enumerable.Range(0, count).Select(_ =>
        new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())).ToArray();
    var indices = Enumerable.Range(0, indexCount).Select(_ => reader.ReadInt32()).ToArray();
    int[] ReadFaces() => Enumerable.Range(0, reader.ReadInt32()).Select(_ => reader.ReadInt32()).ToArray();
    var faces = new[] { ReadFaces(), ReadFaces() };
    var owned = faces.Select(group => group.SelectMany(face => indices.Skip(face * 3).Take(3)).ToHashSet()).ToArray();
    for (var test = 0; test < cases.Length; test++)
    foreach (var reverse in new[] { false, true })
    {
        var spans = new[] { cases[test].Left, cases[test].Right };
        // Nonzero translation and a 180-degree frame exercise side classification
        // after transforming source geometry into mechanism coordinates.
        var pose = new Pose(new Vector3(17f, 3f, -9f), new Quaternion(reverse));
        var sample = new MovableClosedRoad.Result();
        void Add(string name, Vector2 span)
        {
            var occurrence = sample.Occurrences.Length;
            sample.Occurrences = sample.Occurrences.Append(new MovableClosedRoad.Occurrence(name)).ToArray();
            Vector3 Local(float x, float z) => pose.rotation * new Vector3(x - 17f, -3f, z + 9f);
            sample.Triangles.Add(new(occurrence, Local(span.x, 0), Local(span.y, 0), Local(span.x, 1)));
        }
        if (spans[0].y > spans[0].x) Add("Sidewalk", spans[0]);
        if (spans[1].y > spans[1].x) Add("Sidewalk Elevated", spans[1]);
        Add("Curb", new(-30, 30));
        Add("Sidewalkish", new(-40, 40));
        var layout = new MovableSelectedRoadLayout(sample, pose);
        void EqualSpan(Vector2 actual, Vector2 expected)
        {
            if (Math.Abs(actual.x - expected.x) > 0.00001f || Math.Abs(actual.y - expected.y) > 0.00001f)
                throw new Exception("Selected-sidewalk presence/side/frame classification failed.");
        }
        EqualSpan(layout.LeftSidewalk, spans[0]);
        EqualSpan(layout.RightSidewalk, spans[1]);
        if (layout.SidewalkOccurrences[^1] || layout.SidewalkOccurrences[^2])
            throw new Exception("Non-Sidewalk family was classified as a sidewalk.");
        var positions = source.Select(p => new Vector3(p.x + Math.Sign(p.x) * 2f, p.y, p.z)).ToArray();
        var removed = new bool[indexCount / 3];
        // A prior road-conversion removal must survive the sidewalk stage.
        var prior = Enumerable.Range(0, removed.Length).First(i => !faces.Any(g => g.Contains(i)));
        removed[prior] = true;
        var plan = new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { removed });
        var result = Draw02FixedSidewalks.Apply(lod, plan, layout);
        var expectedRemoved = new HashSet<int> { prior };
        for (var side = 0; side < 2; side++)
            if (spans[side].y <= spans[side].x) expectedRemoved.UnionWith(faces[side]);
        for (var face = 0; face < removed.Length; face++)
            if (result.RemoveTriangles[0][face] != expectedRemoved.Contains(face))
                throw new Exception("Missing sidewalk removed unrelated structure or retained a sidewalk face.");
        for (var i = 0; i < count; i++)
        {
            var p = result.Positions[i];
            if (p.y != source[i].y || p.z != source[i].z || !float.IsFinite(p.x))
                throw new Exception("Sidewalk conversion changed Y/Z or produced a nonfinite position.");
            if (!owned.Any(g => g.Contains(i)) && p.x != positions[i].x)
                throw new Exception("Sidewalk conversion moved unrelated structure.");
            if (plan.Positions[i].x != source[i].x + Math.Sign(source[i].x) * 2f)
                throw new Exception("Sidewalk stage mutated its input plan.");
        }
        for (var i = 0; i < removed.Length; i++)
            if (removed[i] != (i == prior))
                throw new Exception("Sidewalk stage mutated its input removal mask.");
        for (var side = 0; side < 2; side++)
        {
            if (spans[side].y <= spans[side].x) continue;
            foreach (var i in owned[side])
                if (result.Positions[i].x < spans[side].x - .00001f || result.Positions[i].x > spans[side].y + .00001f)
                    throw new Exception("Sidewalk slab lies outside the selected side's allocation.");
        }
        if (!result.SourceIndices[0].SequenceEqual(indices))
            throw new Exception("Sidewalk conversion changed original indices.");
        checks++;
    }
}
Console.WriteLine($"PASS {checks} native Draw02 sidewalk/frame/LOD combinations: absent, one-sided, unequal and narrow; unrelated structure preserved.");
Draw03Check.Run(args[0]);

namespace UnityEngine
{
    internal struct Vector2(float x, float y)
    {
        public float x = x, y = y;
        public static Vector2 zero => default;
    }
    internal struct Vector3(float x, float y, float z)
    {
        public float x = x, y = y, z = z;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    internal readonly struct Quaternion(bool reverse)
    {
        public static Vector3 operator *(Quaternion q, Vector3 p) => q.reverse ? new(-p.x, p.y, -p.z) : p;
        private readonly bool reverse = reverse;
    }
    internal readonly struct Pose(Vector3 position, Quaternion rotation)
    {
        public readonly Vector3 position = position;
        public readonly Quaternion rotation = rotation;
    }
    internal static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
    }
}

namespace BridgeBuilder.Bridges
{
    internal static class MovableClosedRoad
    {
        internal sealed class Section(string name) { internal string name = name; }
        internal sealed class Occurrence(string name) { internal Section RootSection = new(name); }
        internal readonly record struct Vertex(Vector3 Position);
        internal sealed class Triangle(int occurrence, Vector3 a, Vector3 b, Vector3 c)
        {
            internal int Occurrence = occurrence;
            internal Vertex A = new(a), B = new(b), C = new(c);
        }
        internal sealed class Result
        {
            internal Occurrence[] Occurrences = [];
            internal List<Triangle> Triangles = [];
        }
    }
    internal static class MovableStructureModel
    {
        internal sealed class Plan(Vector3[] source, Vector3[] positions, int[][] indices, bool[][] removed,
            object? addedVertices = null, object? fragments = null, object? interiorVertices = null, float[]? constructedX = null)
        {
            internal readonly Vector3[] SourcePositions = source, Positions = positions;
            internal readonly int[][] SourceIndices = indices;
            internal readonly bool[][] RemoveTriangles = removed;
            internal readonly object? AddedVertices = addedVertices, Fragments = fragments, InteriorVertices = interiorVertices;
            internal int ConstructedCount => constructedX?.Length ?? 0;
        }
    }
}
