using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Triangle = BridgeBuilder.Bridges.MovableRoadGeometry.Triangle;
using Vertex = BridgeBuilder.Bridges.MovableRoadGeometry.Vertex;

namespace BridgeBuilder.Bridges;

// Repeats measured UV rectangles inside an existing object material. Only geometry
// and UVs are emitted: no material descriptors, pixels, readback or texture files.
internal static class MovableExistingRoadSurface
{
    internal readonly struct Patch
    {
        internal readonly Vector2 Min, Max;
        internal readonly Vector4 U, V;
        internal readonly bool Project;
        internal readonly SurfaceAsset? Surface;
        internal Patch(Vector2 min, Vector2 max) { Min = min; Max = max; U = V = default; Project = false; Surface = null; }
        internal Patch(Vector2 min, Vector2 max, Vector4 u, Vector4 v, SurfaceAsset? surface = null)
        { Min = min; Max = max; U = u; V = v; Project = true; Surface = surface; }
    }

    internal static Triangle[] Apply(IReadOnlyList<Triangle> source, SurfaceAsset[] materials,
        MovableRoadGeometry.Partition regions, SurfaceAsset nativeSurface, Patch[] patches,
        out SurfaceAsset[] outputMaterials,
        IReadOnlyList<MovableRoadGeometry.Partition>? occurrenceRegions = null)
    {
        var converted = new List<Triangle>();
        var surfaces = new List<SurfaceAsset> { nativeSurface };
        if (!MovableRoadGeometry.TryBind(source, regions, patches.Length, materials.Length,
                out var assigned, out var error, occurrencePartitions: occurrenceRegions)) throw new InvalidOperationException(error);
        // Project before coalescing: network UV seams must not constrain object-atlas
        // tessellation. Keep physical shape, occurrence and appearance boundaries.
        for (var i = 0; i < assigned.Length; i++)
        {
            var bound = assigned[i]; var triangle = bound.Geometry;
            var original = materials[triangle.Material]; original.LoadHeader();
            if (original.materialTemplateHash is 0 or 1 or 13 or 14 or 35 or 36) continue;
            var patch = patches[bound.Bone];
            if (!patch.Project) continue;
            Vertex Project(Vertex vertex)
            {
                var p = vertex.Position;
                var uv = new Vector4(Vector3.Dot(p, patch.U) + patch.U.w,
                    Vector3.Dot(p, patch.V) + patch.V.w, 0f, 0f);
                return new Vertex(p, vertex.Normal, vertex.Tangent, uv, Color.white);
            }
            assigned[i] = new MovableRoadGeometry.BoundTriangle(ProjectedBasis(new Triangle(
                Project(triangle.A), Project(triangle.B), Project(triangle.C), triangle.Material,
                triangle.Occurrence, triangle.SourceTriangle), patch), bound.Bone);
        }
        // Clip merged convex panels, not each prototype/source triangle. Artificial
        // diagonals inside one affine appearance region do not need to survive UV tiling.
        var panels = MovableRoadLod.Panels(assigned);
        foreach (var panel in panels)
        {
            var bound = panel.Source;
            var t = bound.Geometry;
            var vertices = panel.Vertices;
            var original = materials[t.Material]; original.LoadHeader();
            if (original.materialTemplateHash is 0 or 1 or 13 or 14 or 35 or 36)
            {
                var slot = surfaces.IndexOf(original);
                if (slot < 0) { slot = surfaces.Count; surfaces.Add(original); }
                for (var j = 1; j + 1 < vertices.Length; j++)
                    converted.Add(new Triangle(vertices[0], vertices[j], vertices[j + 1], slot,
                        t.Occurrence, t.SourceTriangle));
                continue;
            }
            var patch = patches[bound.Bone];
            var material = 0;
            if (patch.Surface != null)
            {
                material = surfaces.IndexOf(patch.Surface);
                if (material < 0) { material = surfaces.Count; surfaces.Add(patch.Surface); }
            }
            // Measured object-space projection supplies repetition for converted network surfaces.
            // Clip at repetition boundaries before mapping into an atlas rectangle;
            // applying frac per vertex alone would interpolate across unrelated islands.
            var minU = Mathf.FloorToInt(vertices.Min(p => p.UV.x));
            var maxU = Mathf.CeilToInt(vertices.Max(p => p.UV.x));
            var minV = Mathf.FloorToInt(vertices.Min(p => p.UV.y));
            var maxV = Mathf.CeilToInt(vertices.Max(p => p.UV.y));
            maxU = Math.Max(maxU, minU + 1); maxV = Math.Max(maxV, minV + 1);
            for (var u = minU; u < maxU; u++)
                for (var v = minV; v < maxV; v++)
                {
                    var polygon = new List<Vertex>(vertices);
                    polygon = Clip(polygon, 0, u, true);
                    polygon = Clip(polygon, 0, u + 1, false);
                    polygon = Clip(polygon, 1, v, true);
                    polygon = Clip(polygon, 1, v + 1, false);
                    for (var i = 1; i + 1 < polygon.Count; i++)
                    {
                        var a = Remap(polygon[0]); var b = Remap(polygon[i]); var c = Remap(polygon[i + 1]);
                        if (Vector3.Cross(b.Position - a.Position, c.Position - a.Position).sqrMagnitude == 0f) continue;
                        converted.Add(new Triangle(a, b, c, material, t.Occurrence, t.SourceTriangle));
                    }
                    Vertex Remap(Vertex vertex) => new(vertex.Position, vertex.Normal, vertex.Tangent,
                        new Vector4(Mathf.LerpUnclamped(patch.Min.x, patch.Max.x, vertex.UV.x - u),
                            Mathf.LerpUnclamped(patch.Min.y, patch.Max.y, vertex.UV.y - v), 0f, 0f), Color.white);
                }
        }
        outputMaterials = surfaces.ToArray();
        Mod.Log.Info($"Existing moving-road materials={surfaces.Count}; inputTriangles={source.Count}; uvPanels={panels.Length}; triangles={converted.Count}; baked textures=0; generated road materials=0.");
        return converted.ToArray();
    }

    // Different triangulations of the same projected plane have identical derivatives.
    // Solve from the measured affine projection directly, avoiding rounded per-triangle
    // UV differences that create false tangent seams and prevent panel coalescing.
    private static Triangle ProjectedBasis(Triangle triangle, Patch patch)
    {
        var normal = Vector3.Cross(triangle.B.Position - triangle.A.Position,
            triangle.C.Position - triangle.A.Position).normalized;
        var tangent = Vector3.Cross((Vector3)patch.V, normal);
        var determinant = Vector3.Dot((Vector3)patch.U, tangent);
        // A singular projection belongs to the whole face, not individual smoothed vertices.
        if (determinant == 0f) return triangle;
        tangent = (tangent / determinant).normalized;
        var bitangent = Vector3.Cross(normal, (Vector3)patch.U) / determinant;
        var sign = Vector3.Dot(Vector3.Cross(normal, tangent), bitangent) < 0f ? -1f : 1f;
        Vertex Frame(Vertex vertex)
        {
            var direction = normal.Equals(vertex.Normal) ? tangent :
                Quaternion.FromToRotation(normal, vertex.Normal) * tangent;
            return new Vertex(vertex.Position, vertex.Normal,
                new Vector4(direction.x, direction.y, direction.z, sign), vertex.UV, vertex.Color);
        }
        return new Triangle(Frame(triangle.A), Frame(triangle.B), Frame(triangle.C),
            triangle.Material, triangle.Occurrence, triangle.SourceTriangle);
    }

    private static List<Vertex> Clip(List<Vertex> input, int axis, float seam, bool positive)
    {
        var output = new List<Vertex>();
        if (input.Count == 0) return output;
        var previous = input[input.Count - 1];
        var previousDistance = (previous.UV[axis] - seam) * (positive ? 1f : -1f);
        foreach (var current in input)
        {
            var distance = (current.UV[axis] - seam) * (positive ? 1f : -1f);
            if ((distance >= 0f) != (previousDistance >= 0f))
                output.Add(Vertex.Interpolate(previous, current, previousDistance / (previousDistance - distance)));
            if (distance >= 0f) output.Add(current);
            previous = current; previousDistance = distance;
        }
        return output;
    }
}
