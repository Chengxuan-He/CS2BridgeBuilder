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

    // Compose measured trees before clipping. Intermediate physical polygons are
    // not triangulated and therefore cannot seed artificial appearance diagonals.
    // Labels [0,count) are fixed; [count,2*count) retain the moving-domain marker.
    internal static Partition CombinePartitions(Partition physical,Partition appearance,
        Matrix4x4 sourceToPhysical,IReadOnlyList<bool> movingBones,int appearanceCount)
    {
        if(appearanceCount<=0||appearanceCount>int.MaxValue/2||movingBones.Count==0||
            !Validate(physical,movingBones.Count,new HashSet<Partition>())||
            !Validate(appearance,appearanceCount,new HashSet<Partition>()))
            throw new ArgumentException("Combined binding requires complete measured physical and appearance trees.");
        var clones=new Dictionary<(Partition,bool),Partition>();
        Partition Appearance(Partition node,bool moving)
        {
            if(node.IsGap)return Partition.Gap;
            if(clones.TryGetValue((node,moving),out var known))return known;
            var result=node.Bone>=0?new Partition(node.Bone+(moving?appearanceCount:0)):
                new Partition(node.ObjectToSeam,Appearance(node.Negative!,moving),Appearance(node.Positive!,moving));
            clones.Add((node,moving),result);return result;
        }
        Partition Physical(Partition node)
        {
            if(node.IsGap)return Partition.Gap;
            if(node.Bone>=0)return Appearance(appearance,movingBones[node.Bone]);
            return new Partition(node.ObjectToSeam*sourceToPhysical,Physical(node.Negative!),Physical(node.Positive!));
        }
        var combined=Physical(physical);
        if(!Validate(combined,checked(appearanceCount*2),new HashSet<Partition>()))
            throw new ArgumentException("Combined source-to-physical partition frame is invalid.");
        return combined;
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
            if (!Split(new List<ClipVertex> { new(triangle.A), new(triangle.B), new(triangle.C) }, selected, triangle, output))
            { error = $"Road triangle {i} could not be transformed into its recorded seam frame."; return false; }
        }
        if (output.Count == 0 && !allowEmpty)
        { error = "The road surface contains no nondegenerate triangles after seam clipping."; return false; }
        foreach (var triangle in output)
            if (!Valid(triangle.Geometry.A) || !Valid(triangle.Geometry.B) || !Valid(triangle.Geometry.C))
            { error = "Seam clipping produced invalid road attributes."; return false; }
        result = output.ToArray(); return true;
    }

    // Keep construction positions in double precision through every partition node.
    // Native Float32 rounding belongs at the final leaf, not on an intermediate edge
    // which a later plane will intersect again. Attribute interpolation is unchanged.
    private readonly struct ClipVertex
    {
        internal readonly Vertex Attributes;
        internal readonly double X, Y, Z;
        internal ClipVertex(Vertex value) : this(value, value.Position.x, value.Position.y, value.Position.z) { }
        internal ClipVertex(Vertex value, double x, double y, double z)
        { Attributes = value; X = x; Y = y; Z = z; }
        internal double Coordinate(int axis) => axis == 0 ? X : axis == 1 ? Y : Z;
        internal Vertex Finish() => new(new Vector3((float)X, (float)Y, (float)Z),
            Attributes.Normal, Attributes.Tangent, Attributes.UV, Attributes.Color);
    }

    private static bool Split(List<ClipVertex> polygon, Partition partition, Triangle source,
        List<BoundTriangle> output)
    {
        if (polygon.Count < 3 || partition.IsGap) return true;
        if (partition.Bone >= 0)
        {
            for (var i = 1; i + 1 < polygon.Count; i++)
            {
                var a = polygon[0].Finish(); var b = polygon[i].Finish(); var c = polygon[i + 1].Finish();
                if (Vector3.Cross(b.Position - a.Position, c.Position - a.Position).sqrMagnitude == 0f) continue;
                output.Add(new BoundTriangle(new Triangle(a, b, c, source.Material,
                    source.Occurrence, source.SourceTriangle), (uint)partition.Bone));
            }
            return true;
        }

        var negative = new List<ClipVertex>(); var positive = new List<ClipVertex>();
        var hasNegative = false; var hasPositive = false;
        var distances=SeamDistances(partition.ObjectToSeam,polygon);
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
            var da = distances[i];
            var db = distances[(i+1)%polygon.Count];
            if (!double.IsFinite(da) || !double.IsFinite(db)) return false;
            if (da <= 0f) negative.Add(a);
            if (da >= 0f) positive.Add(a);
            hasNegative |= da < 0f; hasPositive |= da > 0f;
            if ((da < 0f && db > 0f) || (da > 0f && db < 0f))
            {
                // Opposite triangle edge directions must create the same Float32 point.
                var start=a;var end=b;double ds=da,de=db;
                for(int axis=0;axis<3;axis++)
                {
                    int order=a.Coordinate(axis).CompareTo(b.Coordinate(axis));
                    if(order==0)continue;
                    if(order>0){start=b;end=a;ds=db;de=da;}
                    break;
                }
                double t=ds/(ds-de);
                var value=Vertex.Interpolate(start.Attributes,end.Attributes,(float)t);
                var vertex=new ClipVertex(value, start.X+(end.X-start.X)*t,
                    start.Y+(end.Y-start.Y)*t, start.Z+(end.Z-start.Z)*t);
                negative.Add(vertex); positive.Add(vertex);
            }
        }
        // A coplanar triangle is assigned once. Border vertices may be duplicated on two leaves;
        // this is deliberate: their bone indices must remain independent when the bridge opens.
        if (!hasNegative && !hasPositive) return Split(polygon, partition.Positive!, source, output);
        return (!hasNegative || Split(negative, partition.Negative!, source, output)) &&
            (!hasPositive || Split(positive, partition.Positive!, source, output));
    }

    private static double[] SeamDistances(Matrix4x4 frame,IReadOnlyList<ClipVertex> polygon)
    {
        var values=new double[polygon.Count];var errors=new double[polygon.Count];
        bool reliable=false;
        for(int i=0;i<values.Length;i++)
        {
            var p=polygon[i];
            values[i]=PlaneValue(frame,p.X,p.Y,p.Z,out errors[i]);
            reliable|=Math.Abs(values[i])>errors[i];
        }
        if(reliable)
            for(int i=0;i<values.Length;i++)
                if(Math.Abs(values[i])<=errors[i])values[i]=0;
        return values;
    }

    private static double PlaneValue(Matrix4x4 frame,Vector3 position,out double error)
        => PlaneValue(frame,position.x,position.y,position.z,out error);

    private static double PlaneValue(Matrix4x4 frame,double px,double py,double pz,out double error)
    {
        double x=(double)frame[2,0]*px,y=(double)frame[2,1]*py,
            z=(double)frame[2,2]*pz,w=frame[2,3];
        const double unitRoundoff=1.0/16777216.0;
        const double gamma6=6*unitRoundoff/(1-6*unitRoundoff);
        error=gamma6*(Math.Abs(x)+Math.Abs(y)+Math.Abs(z)+Math.Abs(w));
        return x+y+z+w;
    }

    internal static double SeamDistance(Matrix4x4 frame,Vector3 position)
    {
        double value=PlaneValue(frame,position,out var error);
        return Math.Abs(value)<=error?0:value;
    }

    internal static double[] SeamDistances(Matrix4x4 frame,IReadOnlyList<Vertex> polygon)
    {
        var values=new double[polygon.Count];var errors=new double[polygon.Count];
        bool reliable=false;
        for(int i=0;i<values.Length;i++)
        {
            values[i]=PlaneValue(frame,polygon[i].Position,out errors[i]);
            reliable|=Math.Abs(values[i])>errors[i];
        }
        // Rounding uncertainty alone does not prove that a complete thin polygon
        // is coplanar. Preserve its exact double-side classification, especially
        // at a gap boundary; otherwise a genuine narrow wrap cell can disappear.
        if(reliable)
            for(int i=0;i<values.Length;i++)
                if(Math.Abs(values[i])<=errors[i])values[i]=0;
        return values;
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
