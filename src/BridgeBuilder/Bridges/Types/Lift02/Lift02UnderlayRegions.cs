using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Only evaluated selected-road receivers enter this numerical overlay. Prototype
// membership remains the immutable sleeper-face recipe used by task 70.
internal sealed class Lift02UnderlayRegions
{
    internal readonly struct Point
    {
        internal readonly double X, Z;
        internal Point(double x, double z)
        {
            X = x;
            Z = z;
        }
    }

    internal readonly struct Line
    {
        internal readonly double X, Z, C;
        internal Line(double x, double z, double c)
        {
            X = x;
            Z = z;
            C = c;
        }

        internal double At(Point p) => X * p.X + Z * p.Z + C;
        internal Line Reverse => new(-X, -Z, -C);
    }

    internal sealed class Receiver
    {
        internal readonly int Index;
        internal readonly MovableRoadAppearanceBinding Binding;
        internal readonly Point[] Polygon;
        internal readonly Line Height;
        internal readonly Vector4 U, V;
        private readonly Point _origin, _b, _c;
        private readonly Vector2 _uvA, _uvB, _uvC;
        internal Receiver(int index, MovableRoadAppearanceBinding binding, MovableRoadGeometry.Triangle triangle, Matrix4x4 inverse)
        {
            Index = index;
            Binding = binding;
            var a = triangle.A.Position;
            var b = triangle.B.Position;
            var c = triangle.C.Position;
            Polygon = new[]
            {
                new Point(a.x, a.z),
                new Point(b.x, b.z),
                new Point(c.x, c.z)
            };
            double ex = (double)b.x - a.x, ez = (double)b.z - a.z;
            double fx = (double)c.x - a.x, fz = (double)c.z - a.z;
            double det = ex * fz - ez * fx;
            _origin = Polygon[Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[0]];
            _b = new Point(ex, ez);
            _c = new Point(fx, fz);
            _uvA = triangle.A.UV;
            _uvB = triangle.B.UV;
            _uvC = triangle.C.UV;
            double hx = (((double)b.y - a.y) * fz - ((double)c.y - a.y) * ez) / det;
            double hz = (ex * ((double)c.y - a.y) - fx * ((double)b.y - a.y)) / det;
            Height = new Line(hx, hz, a.y - hx * a.x - hz * a.z);
            if (Area(Polygon) < Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[1])
                Array.Reverse(Polygon);
            // Corresponding materials explicitly carry their native world-density
            // projection. Do not reconstruct it from quantized triangle UVs.
            U = inverse.transpose * (binding.ProjectionKind == MovableRoadAppearanceBinding.Projection.Corresponding ? binding.SourceFrame.GetRow(Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[2]) * binding.CompatibleScale : binding.U);
            V = inverse.transpose * (binding.ProjectionKind == MovableRoadAppearanceBinding.Projection.Corresponding ? binding.SourceFrame.GetRow(Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[3]) * binding.CompatibleScale : binding.V);
        }

        internal Vector2 Sample(double x, double z)
        {
            double px = x - _origin.X, pz = z - _origin.Z;
            double determinant = _b.X * _c.Z - _b.Z * _c.X;
            double wb = (px * _c.Z - pz * _c.X) / determinant;
            double wc = (_b.X * pz - _b.Z * px) / determinant;
            return new Vector2((float)(_uvA.x + ((double)_uvB.x - _uvA.x) * wb + ((double)_uvC.x - _uvA.x) * wc), (float)(_uvA.y + ((double)_uvB.y - _uvA.y) * wb + ((double)_uvC.y - _uvA.y) * wc));
        }
    }

    internal sealed class Region
    {
        internal readonly Receiver Owner;
        internal readonly Point[] Polygon;
        internal readonly Line[] Edges;
        internal readonly double MinX, MaxX, MinZ, MaxZ;
        internal Region(Receiver owner, Point[] polygon)
        {
            Owner = owner;
            Polygon = polygon;
            Edges = Lines(polygon);
            MinX = polygon.Min(p => p.X);
            MaxX = polygon.Max(p => p.X);
            MinZ = polygon.Min(p => p.Z);
            MaxZ = polygon.Max(p => p.Z);
        }
    }

    internal readonly struct Interval
    {
        internal readonly double Start, End;
        internal readonly Receiver Owner;
        internal Interval(double start, double end, Receiver owner)
        {
            Start = start;
            End = end;
            Owner = owner;
        }
    }

    internal readonly Receiver[] Receivers;
    internal readonly Region[] Regions;
    internal readonly Region[] BottomRegions;
    private readonly Dictionary<(int Axis, float Value), Interval[]> _sections = new();
    internal Lift02UnderlayRegions(IReadOnlyList<MovableRoadAppearanceBinding> bindings, Pose roadToObject, bool partition = true)
    {
        var receivers = new List<Receiver>();
        var inverse = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one).inverse;
        for (int i = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[4]; i < bindings.Count; i++)
        {
            var triangle = MovableRoadGeometry.InFrame(new[] { bindings[i].Receiver }, roadToObject)[Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[5]];
            var a = triangle.A.Position;
            var b = triangle.B.Position;
            var c = triangle.C.Position;
            double up = ((double)b.z - a.z) * ((double)c.x - a.x) - ((double)b.x - a.x) * ((double)c.z - a.z);
            if (up > Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[6])
                receivers.Add(new Receiver(i, bindings[i], triangle, inverse));
        }

        Receivers = receivers.ToArray();
        if (!partition)
        {
            Regions = BottomRegions = Array.Empty<Region>();
            return;
        }

        var regions = new List<Region>();
        foreach (var receiver in Receivers)
        {
            var pieces = new List<Point[]>
            {
                receiver.Polygon
            };
            foreach (var other in Receivers)
            {
                if (ReferenceEquals(receiver, other) || !Overlap(receiver.Polygon, other.Polygon))
                    continue;
                var height = new Line(other.Height.X - receiver.Height.X, other.Height.Z - receiver.Height.Z, other.Height.C - receiver.Height.C);
                if (height.X == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[7] && height.Z == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[8] && height.C == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[9] && other.Index > receiver.Index)
                    continue;
                var cut = Clip(other.Polygon, height);
                if (Area(cut) == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[10])
                    continue;
                pieces = pieces.SelectMany(piece => Subtract(piece, Lines(cut))).ToList();
                if (pieces.Count == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[11])
                    break;
            }

            regions.AddRange(pieces.Where(p => Area(p) != Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[12]).Select(p => new Region(receiver, p)));
        }

        Regions = regions.ToArray();
        BottomRegions = MergeBottom(Regions);
    }

    private static Region[] MergeBottom(Region[] input)
    {
        var regions = input.Select(r => (Region? )r).ToList();
        var edges = new Dictionary<(double, double, double, double), int>();
        bool Same(Receiver a, Receiver b) => a.Binding.ProjectionKind == MovableRoadAppearanceBinding.Projection.Corresponding && b.Binding.ProjectionKind == a.Binding.ProjectionKind && a.Binding.Surface.id.guid == b.Binding.Surface.id.guid && a.U.y == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[13] && a.V.y == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[14] && a.U.Equals(b.U) && a.V.Equals(b.V);
        for (int index = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[15]; index < regions.Count; index++)
        {
            var current = regions[index];
            if (current == null)
                continue;
            var p = current.Polygon;
            bool merged = false;
            for (int i = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[16]; i < p.Length; i++)
            {
                var a = p[i];
                var b = p[(i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[17]) % p.Length];
                if (!edges.TryGetValue((b.X, b.Z, a.X, a.Z), out int neighbor))
                    continue;
                var other = regions[neighbor];
                if (other == null || !Same(current.Owner, other.Owner))
                    continue;
                var q = other.Polygon;
                int j = Array.FindIndex(q, v => v.X == b.X && v.Z == b.Z);
                if (j < Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[18] || q[(j + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[19]) % q.Length].X != a.X || q[(j + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[20]) % q.Length].Z != a.Z)
                    continue;
                var joined = new List<Point>();
                for (int k = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[21]; k < p.Length; k++)
                    joined.Add(p[(i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[22] + k) % p.Length]);
                for (int k = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[23]; k < q.Length - Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[24]; k++)
                    joined.Add(q[(j + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[25] + k) % q.Length]);
                bool convex = true;
                for (int k = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[26]; k < joined.Count; k++)
                {
                    var x = joined[k];
                    var y = joined[(k + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[27]) % joined.Count];
                    var z = joined[(k + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[28]) % joined.Count];
                    if ((y.X - x.X) * (z.Z - y.Z) - (y.Z - x.Z) * (z.X - y.X) < Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[29])
                    {
                        convex = false;
                        break;
                    }
                }

                if (!convex)
                    continue;
                // Drop only exactly collinear boundary points. No rounding,
                // snapping, holes or concave material boundaries are hidden.
                for (int k = joined.Count - Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[30]; k >= Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[31] && joined.Count > Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[32]; k--)
                {
                    var x = joined[(k + joined.Count - Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[33]) % joined.Count];
                    var y = joined[k];
                    var z = joined[(k + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[34]) % joined.Count];
                    if ((y.X - x.X) * (z.Z - y.Z) == (y.Z - x.Z) * (z.X - y.X))
                        joined.RemoveAt(k);
                }

                regions[index] = regions[neighbor] = null;
                regions.Add(new Region(current.Owner, joined.ToArray()));
                merged = true;
                break;
            }

            if (merged)
                continue;
            for (int i = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[35]; i < p.Length; i++)
            {
                var a = p[i];
                var b = p[(i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[36]) % p.Length];
                edges[(a.X, a.Z, b.X, b.Z)] = index;
            }
        }

        return regions.Where(r => r != null).Select(r => r!).ToArray();
    }

    internal Interval[] Section(int axis, float value)
    {
        if (_sections.TryGetValue((axis, value), out var cached))
            return cached;
        var ranges = new List<Interval>();
        foreach (var region in Regions)
        {
            var points = new List<double>();
            for (int i = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[37]; i < region.Polygon.Length; i++)
            {
                var a = region.Polygon[i];
                var b = region.Polygon[(i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[38]) % region.Polygon.Length];
                double av = axis == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[39] ? a.X : a.Z, bv = axis == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[40] ? b.X : b.Z;
                double ao = axis == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[41] ? a.Z : a.X, bo = axis == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[42] ? b.Z : b.X;
                if (av == value)
                    points.Add(ao);
                if ((av < value && bv > value) || (av > value && bv < value))
                    points.Add(ao + (bo - ao) * ((value - av) / (bv - av)));
            }

            if (points.Count > Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[43] && points.Min() != points.Max())
                ranges.Add(new Interval(points.Min(), points.Max(), region.Owner));
        }

        var boundaries = ranges.SelectMany(r => new[] { r.Start, r.End }).Distinct().OrderBy(v => v).ToArray();
        var result = new List<Interval>();
        for (int i = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[44]; i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[45] < boundaries.Length; i++)
        {
            double middle = (boundaries[i] + boundaries[i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[46]]) / Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[47];
            var point = axis == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[48] ? new Point(value, middle) : new Point(middle, value);
            var candidates = ranges.Where(r => r.Start <= middle && r.End >= middle).OrderByDescending(r => r.Owner.Height.At(point)).ThenBy(r => r.Owner.Index).ToArray();
            if (candidates.Length != Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[49])
                result.Add(new Interval(boundaries[i], boundaries[i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[50]], candidates[Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[51]].Owner));
        }

        _sections.Add((axis, value), cached = result.ToArray());
        return cached;
    }

    internal bool CoversUv(Receiver receiver, Vector2[] uv)
    {
        var polygon = uv.Select(p => new Point(p.x, p.y)).ToArray();
        if (Area(polygon) == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[52])
            return true;
        if (Area(polygon) < Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[53])
            Array.Reverse(polygon);
        var remaining = new List<Point[]>
        {
            polygon
        };
        var binding = receiver.Binding;
        foreach (var candidate in Receivers)
        {
            var b = candidate.Binding;
            if (b.Surface.id.guid != binding.Surface.id.guid || b.SourceSurface.id.guid != binding.SourceSurface.id.guid || b.ReceiverType != binding.ReceiverType || b.ProjectionKind != binding.ProjectionKind || b.AppearanceDomain != binding.AppearanceDomain)
                continue;
            var triangle = new[]
            {
                b.Receiver.A.UV,
                b.Receiver.B.UV,
                b.Receiver.C.UV
            }.Select(p => new Point(p.x, p.y)).ToArray();
            double area = Area(triangle);
            if (area == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[54])
                continue;
            if (area < Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[55])
                Array.Reverse(triangle);
            var edges = Lines(triangle);
            var next = new List<Point[]>();
            foreach (var piece in remaining)
            {
                if (!Overlap(piece, triangle))
                {
                    next.Add(piece);
                    continue;
                }

                next.AddRange(Subtract(piece, edges));
            }

            remaining = next;
            if (remaining.Count == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[56])
                return true;
        }

        return false;
    }

    internal static double Area(IReadOnlyList<Point> polygon)
    {
        double area = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[57];
        for (int i = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[58]; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[59]) % polygon.Count];
            area += a.X * b.Z - a.Z * b.X;
        }

        return area / Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[60];
    }

    internal static Line[] Lines(IReadOnlyList<Point> polygon)
    {
        var result = new Line[polygon.Count];
        for (int i = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[61]; i < result.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[62]) % polygon.Count];
            result[i] = new Line(a.Z - b.Z, b.X - a.X, (b.Z - a.Z) * a.X - (b.X - a.X) * a.Z);
        }

        // Repeated exact polygon points do not define a half-plane. Keeping
        // their zero equation would put the whole polygon in both halves.
        return result.Where(line => line.X != Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[63] || line.Z != Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[64]).ToArray();
    }

    private static bool Overlap(IReadOnlyList<Point> a, IReadOnlyList<Point> b) => a.Min(p => p.X) < b.Max(p => p.X) && b.Min(p => p.X) < a.Max(p => p.X) && a.Min(p => p.Z) < b.Max(p => p.Z) && b.Min(p => p.Z) < a.Max(p => p.Z);
    private static Point[] Clip(IReadOnlyList<Point> polygon, Line line)
    {
        var result = new List<Point>();
        for (int i = Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[65]; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[66]) % polygon.Count];
            double av = line.At(a), bv = line.At(b);
            if (av >= Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[67])
                result.Add(a);
            if ((av < Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[68] && bv > Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[69]) || (av > Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[70] && bv < Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[71]))
            {
                double t = av / (av - bv);
                result.Add(new Point(a.X + (b.X - a.X) * t, a.Z + (b.Z - a.Z) * t));
            }
        }

        return result.ToArray();
    }

    private static IEnumerable<Point[]> Subtract(Point[] polygon, Line[] cut)
    {
        var remaining = polygon;
        foreach (var line in cut)
        {
            var outside = Clip(remaining, line.Reverse);
            if (Area(outside) != Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[72])
                yield return outside;
            remaining = Clip(remaining, line);
            if (Area(remaining) == Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values.I32[73])
                yield break;
        }
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02UnderlayRegions_cs
internal static class Numeric_Types_Lift02_Lift02UnderlayRegions_cs
{
    static Numeric_Types_Lift02_Lift02UnderlayRegions_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift02_Lift02UnderlayRegions_cs.Values", () => HardcodedNumbers.Load("Types_Lift02_Lift02UnderlayRegions_cs"));
}
