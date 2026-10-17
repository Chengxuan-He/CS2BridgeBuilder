using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vertex = BridgeBuilder.Bridges.MovableRoadGeometry.Vertex;
using Triangle = BridgeBuilder.Bridges.MovableRoadGeometry.Triangle;
using Bound = BridgeBuilder.Bridges.MovableRoadGeometry.BoundTriangle;

namespace BridgeBuilder.Bridges;

// Coalesce converted road tessellation into convex native-style panels. This does
// not classify or transform authored structure, move vertices, or erase paint/UV seams.
internal static class MovableRoadLod
{
    internal static Bound[] Compact(Bound[] input, bool preserveSourceFaces = false)
    {
        var output = new List<Bound>();
        foreach (var panel in Panels(input, preserveSourceFaces))
        {
            var t = panel.Source.Geometry; var p = panel.Vertices;
            for (var j = 1; j + 1 < p.Length; j++)
                output.Add(new Bound(new Triangle(p[0], p[j], p[j + 1],
                    t.Material, t.Occurrence, t.SourceTriangle), panel.Source.Bone));
        }
        Mod.Log.Info($"Moving road LOD panels: triangles={input.Length}->{output.Count}; materials, bones and UV seams retained.");
        return output.ToArray();
    }

    // Keep the merged convex boundary until appearance boundaries are applied.
    // Triangulating it first would introduce arbitrary fan diagonals into every LOD.
    internal static (Bound Source, Vertex[] Vertices)[] Panels(Bound[] input, bool preserveSourceFaces = false)
    {
        var vertices = new List<Vertex>();
        var ids = new Dictionary<(uint, int, int, int, Vertex), int>();
        var polygons = new List<int>?[input.Length];
        var edges = new Dictionary<(int, int), HashSet<int>>();
        var pending = new Queue<(int, int)>();
        int Id(Vertex vertex, Bound source)
        {
            // Occurrence controls sidewalk and motion-domain selection downstream.
            // Equal material/position does not authorize merging different road pieces.
            var key = (source.Bone, source.Geometry.Material, source.Geometry.Occurrence,
                preserveSourceFaces ? source.Geometry.SourceTriangle : 0, vertex);
            if (ids.TryGetValue(key, out var id)) return id;
            id = vertices.Count; vertices.Add(vertex); ids.Add(key, id); return id;
        }
        (int, int) Edge(int a, int b) => a < b ? (a, b) : (b, a);
        void Register(int owner, bool add)
        {
            var polygon = polygons[owner]!;
            for (var i = 0; i < polygon.Count; i++)
            {
                var edge = Edge(polygon[i], polygon[(i + 1) % polygon.Count]);
                if (!edges.TryGetValue(edge, out var owners))
                    edges.Add(edge, owners = new HashSet<int>());
                if (add)
                {
                    owners.Add(owner);
                    if (owners.Count == 2) pending.Enqueue(edge);
                }
                else owners.Remove(owner);
            }
        }
        for (var i = 0; i < input.Length; i++)
        {
            var t = input[i].Geometry;
            polygons[i] = new List<int> { Id(t.A, input[i]), Id(t.B, input[i]), Id(t.C, input[i]) };
            Register(i, true);
        }
        while (pending.Count != 0)
        {
            var edge = pending.Dequeue();
            var owners = edges[edge];
            if (owners.Count != 2) continue;
            var pair = owners.OrderBy(i => i).ToArray();
            var a = polygons[pair[0]]!; var b = polygons[pair[1]]!;
            var ai = -1; var bi = -1;
            for (var i = 0; i < a.Count; i++)
                if (Edge(a[i], a[(i + 1) % a.Count]) == edge) ai = i;
            for (var i = 0; i < b.Count; i++)
                if (Edge(b[i], b[(i + 1) % b.Count]) == edge) bi = i;
            if (ai < 0 || bi < 0 || a[ai] != b[(bi + 1) % b.Count] ||
                b[bi] != a[(ai + 1) % a.Count]) continue;
            var merged = new List<int>();
            for (var i = 1; i <= a.Count; i++) merged.Add(a[(ai + i) % a.Count]);
            for (var i = 2; i < b.Count; i++) merged.Add(b[(bi + i) % b.Count]);
            if (merged.Distinct().Count() != merged.Count) continue;
            var reference = input[pair[0]].Geometry;
            if (!merged.All(id => SamePlaneAndAttributes(reference, vertices[id]))) continue;
            var normal = Vector3.Cross(reference.B.Position - reference.A.Position,
                reference.C.Position - reference.A.Position).normalized;
            // Remove only redundant collinear boundary vertices. Their attributes have
            // already been verified against the same affine surface above.
            for (var i = merged.Count - 1; i >= 0 && merged.Count > 3; i--)
            {
                var p = vertices[merged[(i + merged.Count - 1) % merged.Count]].Position;
                var q = vertices[merged[i]].Position;
                var r = vertices[merged[(i + 1) % merged.Count]].Position;
                var span = r - p;
                if (span.sqrMagnitude > 0f && Vector3.Dot(q - p, r - q) >= 0f &&
                    Vector3.Cross(q - p, span).sqrMagnitude <= 1e-12f * span.sqrMagnitude)
                    merged.RemoveAt(i);
            }
            var convex = true;
            for (var i = 0; i < merged.Count; i++)
            {
                var p = vertices[merged[i]].Position;
                var q = vertices[merged[(i + 1) % merged.Count]].Position;
                var r = vertices[merged[(i + 2) % merged.Count]].Position;
                if (Vector3.Dot(Vector3.Cross(q - p, r - q), normal) < 0f) { convex = false; break; }
            }
            if (!convex) continue;
            Register(pair[0], false); Register(pair[1], false);
            polygons[pair[0]] = merged; polygons[pair[1]] = null;
            Register(pair[0], true);
        }
        var output = new List<(Bound Source, Vertex[] Vertices)>();
        for (var i = 0; i < polygons.Length; i++)
        {
            var p = polygons[i]; if (p == null) continue;
            output.Add((input[i], p.Select(id => vertices[id]).ToArray()));
        }
        return output.ToArray();
    }

    private static bool SamePlaneAndAttributes(Triangle t, Vertex p)
    {
        var u = t.B.Position - t.A.Position; var v = t.C.Position - t.A.Position;
        var n = Vector3.Cross(u, v); var area = n.sqrMagnitude;
        if (area == 0f) return false;
        var d = p.Position - t.A.Position;
        // Numerical tolerances check affine equivalence, not authored-part membership.
        if (Mathf.Abs(Vector3.Dot(d, n)) > 1e-6f * Mathf.Sqrt(area)) return false;
        var b = Vector3.Dot(Vector3.Cross(d, v), n) / area;
        var c = Vector3.Dot(Vector3.Cross(u, d), n) / area;
        var uv = t.A.UV + (t.B.UV - t.A.UV) * b + (t.C.UV - t.A.UV) * c;
        // Only constant lighting attributes can be carried across a new diagonal.
        return (uv - p.UV).sqrMagnitude <= 1e-12f &&
            p.Normal.Equals(t.A.Normal) && p.Normal.Equals(t.B.Normal) && p.Normal.Equals(t.C.Normal) &&
            p.Tangent.Equals(t.A.Tangent) && p.Tangent.Equals(t.B.Tangent) && p.Tangent.Equals(t.C.Tangent) &&
            p.Color.Equals(t.A.Color) && p.Color.Equals(t.B.Color) && p.Color.Equals(t.C.Color);
    }
}
