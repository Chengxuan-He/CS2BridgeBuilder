using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Vertex = BridgeBuilder.Bridges.MovableRoadGeometry.Vertex;
using Triangle = BridgeBuilder.Bridges.MovableRoadGeometry.Triangle;
using Bound = BridgeBuilder.Bridges.MovableRoadGeometry.BoundTriangle;

namespace BridgeBuilder.Bridges;

// Paint replaces receiver triangles in the same skinned render. No projection volumes,
// depth offsets, material instances, textures or object prefabs are emitted.
internal static class MovableRoadPaint
{
    internal static Bound[] Apply(Bound[] input, MovableLaneVisuals.Batch[] batches,
        Pose pose, float density, ref SurfaceAsset[] surfaces)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var regions = batches.SelectMany(b => MovableNativeDecals.Regions(b, pose, density)).ToArray();
        if (regions.Length == 0) return input;
        var materials = surfaces.ToList();
        int Material(string cid)
        {
            if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse(cid), out SurfaceAsset surface))
                throw new InvalidOperationException("Missing native bridge paint surface: " + cid);
            var slot = materials.IndexOf(surface);
            if (slot < 0) { slot = materials.Count; materials.Add(surface); }
            return slot;
        }
        // Original B&P object surfaces (template 1), with paint measured from their
        // base-color atlases. All material channels remain the original asset references.
        var white = Material("792a855e9d0c5c34d2c41d48b062139d");
        var yellow = Material("97291ab7f7938fc35cc670b15f2bca36");
        return ApplyRegions(input, regions, white, yellow, materials.ToArray(), ref surfaces, timer);
    }

    // Pure managed geometry, also used by offline replay of real saved receivers.
    internal static Bound[] ApplyRegions(Bound[] input, MovableNativeDecals.Region[] regions,
        int white, int yellow, SurfaceAsset[] materials, ref SurfaceAsset[] surfaces,
        System.Diagnostics.Stopwatch timer)
    {
        var panels = MovableRoadLod.Panels(input);
        var results = new List<Bound>[panels.Length];
        // Pure clipping of managed snapshots: no asset, Unity object or graphics API on workers.
        System.Threading.Tasks.Parallel.For(0, panels.Length, index =>
        {
            var source = panels[index].Source;
            var vertices = panels[index].Vertices;
            var polygons = new List<(List<Vertex> Vertices, int Material)>
                { (new List<Vertex>(vertices), source.Geometry.Material) };
            // Resolve candidates once per original panel. Unrelated strokes must not
            // repeatedly traverse every fragment produced by earlier paint cuts.
            var candidates = regions.Where(r => Intersects(vertices, r)).ToArray();
            foreach (var region in candidates)
            {
                var next = new List<(List<Vertex>, int)>();
                foreach (var polygon in polygons)
                {
                    var inside = polygon.Vertices;
                    if (!Intersects(inside, region)) { next.Add(polygon); continue; }
                    for (int axis = 0; axis < 3 && inside.Count >= 3; axis++)
                    {
                        var normal = axis == 0 ? region.Right : axis == 1 ? region.Up : region.Forward;
                        for (int sign = -1; sign <= 1 && inside.Count >= 3; sign += 2)
                        {
                            Split(inside, normal * sign, Vector3.Dot(region.Center, normal * sign) + region.Half[axis],
                                out var kept, out var outside);
                            if (outside.Count >= 3) next.Add((outside, polygon.Material));
                            inside = kept;
                        }
                    }
                    if (inside.Count >= 3)
                    {
                        // Interior texels, away from atlas seams. Inlay follows receiver positions
                        // exactly; native paint occupies the same surface instead of floating over it.
                        var uv = region.Yellow ? new Vector4(800f/4096f,3000f/4096f,0,0)
                            : new Vector4(3500f/4096f,820f/4096f,0,0);
                        next.Add((inside.Select(v => new Vertex(v.Position,v.Normal,v.Tangent,uv,Color.white)).ToList(),
                            region.Yellow ? yellow : white));
                    }
                }
                polygons = next;
            }
            var result = new List<Bound>();
            foreach (var polygon in polygons)
                for (int j = 1; j + 1 < polygon.Vertices.Count; j++)
                {
                    var a=polygon.Vertices[0]; var b=polygon.Vertices[j]; var c=polygon.Vertices[j+1];
                    if (Vector3.Cross(b.Position-a.Position,c.Position-a.Position).sqrMagnitude == 0f) continue;
                    result.Add(new Bound(new Triangle(a,b,c,polygon.Material,source.Geometry.Occurrence,
                        source.Geometry.SourceTriangle),source.Bone));
                }
            results[index] = result;
        });
        surfaces = materials.ToArray();
        var output = results.SelectMany(p => p).ToArray();
        Mod.Log.Info($"Moving road paint inlay: strokes={regions.Length}; triangles={input.Length}->{output.Length}; receiverPanels={panels.Length}; independent marking objects=0; elapsedMs={timer.ElapsedMilliseconds}.");
        return output;
    }

    private static bool Intersects(IReadOnlyList<Vertex> vertices, MovableNativeDecals.Region region)
    {
        for (var axis = 0; axis < 3; axis++)
        {
            var normal = axis == 0 ? region.Right : axis == 1 ? region.Up : region.Forward;
            var min = float.PositiveInfinity; var max = float.NegativeInfinity;
            foreach (var v in vertices)
            {
                var d = Vector3.Dot(v.Position - region.Center, normal);
                min = Math.Min(min, d); max = Math.Max(max, d);
            }
            if (min >= region.Half[axis] || max <= -region.Half[axis]) return false;
        }
        return true;
    }

    private static void Split(List<Vertex> input, Vector3 normal, float plane,
        out List<Vertex> inside, out List<Vertex> outside)
    {
        inside = new List<Vertex>(); outside = new List<Vertex>();
        var previous=input[input.Count-1]; var pd=Vector3.Dot(previous.Position,normal)-plane;
        foreach (var current in input)
        {
            var d=Vector3.Dot(current.Position,normal)-plane;
            if ((pd <= 0f) != (d <= 0f))
            { var cut=Vertex.Interpolate(previous,current,pd/(pd-d)); inside.Add(cut); outside.Add(cut); }
            if (d <= 0f) inside.Add(current); else outside.Add(current);
            previous=current; pd=d;
        }
    }
}
