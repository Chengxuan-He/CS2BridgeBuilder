using System;
using System.Collections.Generic;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Cuts already evaluated road triangles at recorded seam frames. This is construction of a new
/// road surface, not classification or widening of the mechanism's authored structure.
/// </summary>
internal static class MovableRoadGeometry
{
    internal readonly struct Vertex
    {
        internal readonly Vector3 Position, Normal;
        internal readonly Vector4 Tangent, UV;
        internal readonly Color Color;
        internal Vertex(Vector3 position, Vector3 normal, Vector4 tangent, Vector4 uv, Color color)
        { Position = position; Normal = normal; Tangent = tangent; UV = uv; Color = color; }

        internal static Vertex Interpolate(Vertex a, Vertex b, float t)
        {
            var normal = Vector3.LerpUnclamped(a.Normal, b.Normal, t).normalized;
            var tangent = Vector4.LerpUnclamped(a.Tangent, b.Tangent, t);
            var direction = ((Vector3)tangent - normal * Vector3.Dot(normal, tangent)).normalized;
            return new Vertex(Vector3.LerpUnclamped(a.Position, b.Position, t), normal,
                new Vector4(direction.x, direction.y, direction.z, a.Tangent.w),
                Vector4.LerpUnclamped(a.UV, b.UV, t), Color.LerpUnclamped(a.Color, b.Color, t));
        }
    }

    /// <summary>
    /// A complete binary partition avoids overlapping independently specified regions.
    /// Internal nodes split at seam-local z=0. Leaves carry native bone ARRAY indices
    /// or explicitly exclude a measured expansion/leaf gap from generated geometry.
    /// Matrix and tree membership come from immutable offline archetype measurements.
    /// </summary>
    internal sealed class Partition
    {
        internal static readonly Partition Gap = new();
        internal readonly Matrix4x4 ObjectToSeam;
        internal readonly Partition? Negative, Positive;
        internal readonly int Bone;
        internal readonly bool IsGap;
        private Partition() { IsGap = true; Bone = -1; ObjectToSeam = Matrix4x4.identity; }
        internal Partition(int bone) { Bone = bone; ObjectToSeam = Matrix4x4.identity; }
        internal Partition(Matrix4x4 objectToSeam, Partition negative, Partition positive)
        { Bone = -1; ObjectToSeam = objectToSeam; Negative = negative; Positive = positive; }
    }

    internal readonly struct Triangle
    {
        internal readonly Vertex A, B, C;
        internal readonly int Material, Occurrence, SourceTriangle;
        internal Triangle(Vertex a, Vertex b, Vertex c, int material, int occurrence, int sourceTriangle)
        { A = a; B = b; C = c; Material = material; Occurrence = occurrence; SourceTriangle = sourceTriangle; }
    }

    internal readonly struct BoundTriangle
    {
        internal readonly Triangle Geometry;
        internal readonly uint Bone;
        internal BoundTriangle(Triangle geometry, uint bone) { Geometry = geometry; Bone = bone; }
    }

    // A repeating UV projection must stay invertible over the interpolated normal
    // field, not merely at its endpoints. Opposite signs cross a singular frame.
    internal static bool CanRepeatProjection(Vector4 u, Vector4 v, Vector3 plane,
        Vector3 a, Vector3 b, Vector3 c)
    {
        var gradientNormal = Vector3.Cross((Vector3)u, (Vector3)v);
        var determinant = Vector3.Dot(gradientNormal, plane);
        if (!Finite(determinant) || Mathf.Abs(determinant) <= 1e-10f) return false;
        var sign = determinant > 0f ? 1f : -1f;
        bool Compatible(Vector3 normal)
        {
            var value = Vector3.Dot(gradientNormal, normal) * sign;
            return Finite(value) && value > 1e-10f;
        }
        return Compatible(a) && Compatible(b) && Compatible(c);
    }

    // The selected road and the native mechanism have different local origins. A measured
    // rigid pose aligns newly evaluated road/lane vertices; authored structure stays untouched.
    // Texture coordinates and source provenance are preserved after appearance evaluation.
    internal static Triangle[] InFrame(IReadOnlyList<Triangle> source, Pose pose)
    {
        var offset = pose.position;
        var q = pose.rotation;
        var lengthSquared = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
        if (!float.IsFinite(offset.x) || !float.IsFinite(offset.y) || !float.IsFinite(offset.z) ||
            !float.IsFinite(lengthSquared) || lengthSquared <= 0f)
            throw new InvalidOperationException("The measured road-to-object pose is invalid.");
        q = q.normalized;
        Vertex Move(Vertex v)
        {
            var tangent = q * new Vector3(v.Tangent.x, v.Tangent.y, v.Tangent.z);
            return new Vertex(q * v.Position + offset, q * v.Normal,
                new Vector4(tangent.x, tangent.y, tangent.z, v.Tangent.w), v.UV, v.Color);
        }
        var result = new Triangle[source.Count];
        for (var i = 0; i < result.Length; i++)
        {
            var t = source[i];
            result[i] = new Triangle(Move(t.A), Move(t.B), Move(t.C),
                t.Material, t.Occurrence, t.SourceTriangle);
        }
        return result;
    }

    internal static bool TryBind(IReadOnlyList<Triangle> source, Partition partition, int boneCount,
        int materialCount, out BoundTriangle[] result, out string? error, bool allowEmpty = false,
        IReadOnlyList<Partition>? occurrencePartitions = null,
        IReadOnlyDictionary<(int Occurrence, int SourceTriangle), Partition>? trianglePartitions = null)
    {
        result = Array.Empty<BoundTriangle>(); error = null;
        if (source == null || source.Count == 0 || boneCount <= 0 || materialCount <= 0 ||
            !Validate(partition, boneCount, new HashSet<Partition>()))
        { error = "Road binding requires triangles, materials and a complete measured bone partition."; return false; }
        if (occurrencePartitions != null)
            foreach (var selected in occurrencePartitions)
                if (!Validate(selected, boneCount, new HashSet<Partition>()))
                    throw new InvalidOperationException("An occurrence has an invalid measured bone partition.");
        if (trianglePartitions != null)
        {
            var validated = new HashSet<Partition>();
            foreach (var entry in trianglePartitions)
                if (entry.Key.Occurrence < 0 || entry.Key.SourceTriangle < 0 ||
                    (validated.Add(entry.Value) && !Validate(entry.Value, boneCount, new HashSet<Partition>())))
                    throw new InvalidOperationException("A source triangle has an invalid measured bone partition.");
        }
        var output = new List<BoundTriangle>();
        for (var i = 0; i < source.Count; i++)
        {
            var triangle = source[i];
            if (triangle.Material < 0 || triangle.Material >= materialCount)
            { error = $"Road triangle {i} has invalid material index {triangle.Material}/{materialCount}."; return false; }
            if (!Valid(triangle.A) || !Valid(triangle.B) || !Valid(triangle.C))
            {
                error = $"Road triangle {i} has invalid attributes: valid A/B/C={Valid(triangle.A)}/{Valid(triangle.B)}/{Valid(triangle.C)}; source={triangle.Occurrence}/{triangle.SourceTriangle}; material={triangle.Material}.";
                return false;
            }
            if (triangle.A.Tangent.w != triangle.B.Tangent.w || triangle.A.Tangent.w != triangle.C.Tangent.w)
            {
                error = $"Road triangle {i} has mixed tangent handedness {triangle.A.Tangent.w}/{triangle.B.Tangent.w}/{triangle.C.Tangent.w}; source={triangle.Occurrence}/{triangle.SourceTriangle}; material={triangle.Material}.";
                return false;
            }
            var selected = partition;
            if (trianglePartitions != null)
            {
                if (!trianglePartitions.TryGetValue((triangle.Occurrence, triangle.SourceTriangle), out selected))
                    throw new InvalidOperationException($"Road triangle {i} has no measured partition for source ({triangle.Occurrence}, {triangle.SourceTriangle}).");
            }
            else if (occurrencePartitions != null)
            {
                if ((uint)triangle.Occurrence >= (uint)occurrencePartitions.Count)
                    throw new InvalidOperationException($"Road triangle {i} has no measured partition for occurrence {triangle.Occurrence}.");
                selected = occurrencePartitions[triangle.Occurrence];
            }
            if (!Split(new List<Vertex> { triangle.A, triangle.B, triangle.C }, selected, triangle, output))
            { error = $"Road triangle {i} could not be transformed into its recorded seam frame."; return false; }
        }
        if (output.Count == 0 && !allowEmpty)
        { error = "The road surface contains no nondegenerate triangles after seam clipping."; return false; }
        foreach (var triangle in output)
            if (!Valid(triangle.Geometry.A) || !Valid(triangle.Geometry.B) || !Valid(triangle.Geometry.C))
            { error = "Seam clipping produced invalid road attributes."; return false; }
        result = output.ToArray(); return true;
    }

    private static bool Split(List<Vertex> polygon, Partition partition, Triangle source,
        List<BoundTriangle> output)
    {
        if (polygon.Count < 3 || partition.IsGap) return true;
        if (partition.Bone >= 0)
        {
            for (var i = 1; i + 1 < polygon.Count; i++)
            {
                var a = polygon[0]; var b = polygon[i]; var c = polygon[i + 1];
                if (Vector3.Cross(b.Position - a.Position, c.Position - a.Position).sqrMagnitude == 0f) continue;
                output.Add(new BoundTriangle(new Triangle(a, b, c, source.Material,
                    source.Occurrence, source.SourceTriangle), (uint)partition.Bone));
            }
            return true;
        }

        var negative = new List<Vertex>(); var positive = new List<Vertex>();
        var hasNegative = false; var hasPositive = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
            var da = partition.ObjectToSeam.MultiplyPoint3x4(a.Position).z;
            var db = partition.ObjectToSeam.MultiplyPoint3x4(b.Position).z;
            if (!Finite(da) || !Finite(db)) return false;
            if (da <= 0f) negative.Add(a);
            if (da >= 0f) positive.Add(a);
            hasNegative |= da < 0f; hasPositive |= da > 0f;
            if ((da < 0f && db > 0f) || (da > 0f && db < 0f))
            {
                var vertex = Vertex.Interpolate(a, b, (float)((double)da / ((double)da - db)));
                negative.Add(vertex); positive.Add(vertex);
            }
        }
        // A coplanar triangle is assigned once. Border vertices may be duplicated on two leaves;
        // this is deliberate: their bone indices must remain independent when the bridge opens.
        if (!hasNegative && !hasPositive) return Split(polygon, partition.Positive!, source, output);
        return (!hasNegative || Split(negative, partition.Negative!, source, output)) &&
            (!hasPositive || Split(positive, partition.Positive!, source, output));
    }

    private static bool Validate(Partition? node, int boneCount, HashSet<Partition> path)
    {
        if (node == null || !path.Add(node)) return false;
        bool valid;
        if (node.IsGap) valid = node.Bone == -1 && node.Negative == null && node.Positive == null;
        else if (node.Bone >= 0) valid = node.Bone < boneCount && node.Negative == null && node.Positive == null;
        else
        {
            valid = node.ObjectToSeam.determinant != 0f;
            for (var i = 0; i < 16; i++) valid &= Finite(node.ObjectToSeam[i]);
            valid &= node.ObjectToSeam[3, 0] == 0f && node.ObjectToSeam[3, 1] == 0f &&
                node.ObjectToSeam[3, 2] == 0f && node.ObjectToSeam[3, 3] == 1f;
            valid = valid && Validate(node.Negative, boneCount, path) && Validate(node.Positive, boneCount, path);
        }
        path.Remove(node); return valid;
    }

    internal static bool Valid(Vertex v)
    {
        for (var i = 0; i < 3; i++) if (!Finite(v.Position[i]) || !Finite(v.Normal[i])) return false;
        for (var i = 0; i < 4; i++) if (!Finite(v.Tangent[i]) || !Finite(v.UV[i]) || !Finite(v.Color[i])) return false;
        return v.Normal.sqrMagnitude > 0f && ((Vector3)v.Tangent).sqrMagnitude > 0f &&
            (v.Tangent.w == 1f || v.Tangent.w == -1f);
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
