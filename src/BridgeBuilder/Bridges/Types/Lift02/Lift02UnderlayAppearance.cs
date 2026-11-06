using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Regions = BridgeBuilder.Bridges.Lift02UnderlayRegions;

namespace BridgeBuilder.Bridges;
// Task 71 changes appearance only. All receiver points are subdivisions of the
// verified task-70 Plan; their native source-face/bone provenance remains intact.
internal static class Lift02UnderlayAppearance
{
    private sealed class Vertex
    {
        internal Vector3 Position, Weights;
        internal Vector2 Uv;
        internal Matrix4x4 Frame;
        internal MovableStructureModel.ProjectedUvFrame Projection;
        internal int Face;
    }

    private sealed class Group
    {
        internal readonly SurfaceAsset Surface;
        internal readonly List<Vertex[]> Triangles = new();
        internal Group(SurfaceAsset surface)
        {
            Surface = surface;
        }
    }

    internal static KeyValuePair<string, MovableStructureModel.Plan>[] Apply(MovableStructureModel.Plan source, IReadOnlyDictionary<int, int> kinds, string renderName, IReadOnlyList<MovableRoadAppearanceBinding> bindings, Pose roadToObject)
    {
        var first = bindings.FirstOrDefault();
        bool uniform = first != null && first.ProjectionKind == MovableRoadAppearanceBinding.Projection.Corresponding && bindings.All(b => b.ProjectionKind == first.ProjectionKind && b.ReceiverType == first.ReceiverType && b.Surface.id.guid == first.Surface.id.guid && b.SourceFrame.Equals(first.SourceFrame) && b.CompatibleScale == first.CompatibleScale);
        var regions = new Regions(bindings, roadToObject, !uniform);
        var frames = source.FrameTransforms.ToDictionary(p => p.Key, p => p.Value);
        var groups = new Dictionary<string, Group>(StringComparer.Ordinal);
        int originalSampling = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[0], uncovered = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[1];
        foreach (var fragment in source.Fragments)
        {
            int face = fragment.Triangle;
            int kind = kinds[face];
            var original = source.SourceIndices[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[2]].Skip(face * Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[3]).Take(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[4]).ToArray();
            for (int at = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[5]; at < fragment.Indices.Length; at += Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[6])
            {
                var vertices = new Vertex[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[7]];
                for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[8]; i < Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[9]; i++)
                {
                    int id = fragment.Indices[at + i];
                    var c = source.Construction(id - source.SourcePositions.Length);
                    var w = Vector3.zero;
                    w[Array.IndexOf(original, c.A)] += Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[10] - c.WeightB - c.WeightC;
                    w[Array.IndexOf(original, c.B)] += c.WeightB;
                    w[Array.IndexOf(original, c.C)] += c.WeightC;
                    vertices[i] = new Vertex
                    {
                        Position = source.Positions[id],
                        Weights = w,
                        Frame = frames.TryGetValue(id, out var frame) ? frame : Matrix4x4.identity,
                        Face = face
                    };
                }

                // One identical native repeat field needs no receiver-diagonal
                // cuts. This applies equally to any corresponding road family.
                if (uniform && regions.Receivers.Length != Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[11])
                {
                    Emit(vertices, regions.Receivers[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[12]], kind, false, true);
                    continue;
                }

                if (!HasArea(vertices))
                {
                    var receiver = regions.Receivers.OrderBy(r => Distance(r, vertices[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[13]].Position)).ThenBy(r => r.Index).FirstOrDefault();
                    if (receiver == null)
                        throw new InvalidOperationException("Prepared underlay has no selected-road material receiver.");
                    Emit(vertices, receiver, kind, true, true);
                    continue;
                }

                var remaining = new List<Vertex[]>
                {
                    vertices
                };
                void Assign(Regions.Receiver receiver, Regions.Line[] clip)
                {
                    var next = new List<Vertex[]>();
                    foreach (var polygon in remaining)
                    {
                        // A disjoint finite receiver must not cut this polygon
                        // along the receiver's infinite supporting lines.
                        var intersection = polygon;
                        foreach (var line in clip)
                        {
                            intersection = Clip(intersection, line);
                            if (!HasArea(intersection))
                                break;
                        }

                        if (!HasArea(intersection))
                        {
                            next.Add(polygon);
                            continue;
                        }

                        var inside = polygon;
                        foreach (var line in clip)
                        {
                            var outside = Clip(inside, line.Reverse);
                            if (HasArea(outside))
                                next.Add(outside);
                            inside = Clip(inside, line);
                            if (!HasArea(inside))
                                break;
                        }

                        if (HasArea(inside))
                            Emit(inside, receiver, kind, false);
                    }

                    remaining = next;
                }

                if (kind == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[14])
                {
                    float minX = vertices.Min(v => v.Position.x), maxX = vertices.Max(v => v.Position.x);
                    float minZ = vertices.Min(v => v.Position.z), maxZ = vertices.Max(v => v.Position.z);
                    foreach (var region in regions.BottomRegions)
                    {
                        if (remaining.Count == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[15])
                            break;
                        if (maxX <= region.MinX || minX >= region.MaxX || maxZ <= region.MinZ || minZ >= region.MaxZ)
                            continue;
                        Assign(region.Owner, region.Edges);
                    }
                }
                else
                {
                    int axis = kind < Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[16] ? Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[17] : Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[18];
                    float value = vertices[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[19]].Position[axis];
                    int along = axis == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[20] ? Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[21] : Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[22];
                    float min = vertices.Min(v => v.Position[along]), max = vertices.Max(v => v.Position[along]);
                    foreach (var interval in regions.Section(axis, value))
                    {
                        if (remaining.Count == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[23])
                            break;
                        if (max <= interval.Start || min >= interval.End)
                            continue;
                        Assign(interval.Owner, axis == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[24] ? new[] { new Regions.Line(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[25], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[26], -interval.Start), new Regions.Line(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[27], -Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[28], interval.End) } : new[] { new Regions.Line(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[29], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[30], -interval.Start), new Regions.Line(-Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[31], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[32], interval.End) });
                    }
                }

                // An uncovered selected-input hole has no corresponding material route.
                // Keep geometry and the nearest actual input's material. An already
                // verified native field keeps its density; unknown sampling remains
                // Original. Never substitute wood or a bridge-wide asphalt.
                foreach (var polygon in remaining)
                {
                    var center = polygon.Aggregate(Vector3.zero, (sum, v) => sum + v.Position) / polygon.Length;
                    var receiver = regions.Receivers.OrderBy(r => Distance(r, center)).ThenBy(r => r.Index).FirstOrDefault();
                    if (receiver == null)
                        throw new InvalidOperationException("Prepared underlay has physical faces but no selected-road material receiver.");
                    uncovered++;
                    bool nativeField = receiver.Binding.ProjectionKind == MovableRoadAppearanceBinding.Projection.Corresponding || receiver.Binding.SourceDomain.HasValue;
                    Emit(polygon, receiver, kind, !nativeField);
                }
            }
        }

        void Emit(Vertex[] polygon, Regions.Receiver receiver, int kind, bool originalOnly, bool preserveDegenerate = false, bool splitHeight = true)
        {
            if (splitHeight && kind != Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[33] && !originalOnly && receiver.Binding.SourceDomain.HasValue)
            {
                double Depth(Vertex v) => (double)v.Position.y - receiver.Height.At(new Regions.Point(v.Position.x, v.Position.z));
                if (polygon.Any(v => Depth(v) < Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[34]) && polygon.Any(v => Depth(v) > Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[35]))
                {
                    // The retained road can have a native quantized height step.
                    // Split the actual zero-depth fold instead of moving geometry,
                    // extending the atlas band or flattening the wall's UVs.
                    foreach (int side in new[]
                    {
                        -Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[36],
                        Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[37]
                    }

                    )
                    {
                        var part = ClipValues(polygon, v => side * Depth(v));
                        if (HasArea(part))
                            Emit(part, receiver, kind, false, false, false);
                    }

                    return;
                }
            }

            string material = receiver.Binding.Surface.id.guid.ToString();
            if (!groups.TryGetValue(material, out var group))
                groups.Add(material, group = new Group(receiver.Binding.Surface));
            var polygonNormal = Vector3.zero;
            for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[38]; i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[39] < polygon.Length && polygonNormal == Vector3.zero; i++)
                polygonNormal = Normal(polygon[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[40]].Position, polygon[i].Position, polygon[i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[41]].Position);
            bool unfold = kind != Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[42] && !originalOnly;
            bool Permitted(Vector2[] values) => receiver.Binding.ProjectionKind is MovableRoadAppearanceBinding.Projection.Original or MovableRoadAppearanceBinding.Projection.Atlas ? receiver.Binding.SourceDomain is { } domain ? values.All(domain.Contains) : receiver.Binding.ReceiverType != MovableRoadAppearance.Type.None && regions.CoversUv(receiver, values) : Legal(receiver.Binding, values);
            var mappedPolygon = polygon.Select(v => Project(receiver, v.Position, polygonNormal, kind, unfold)).ToArray();
            if (unfold && !Permitted(mappedPolygon))
            {
                // A finite source chart may continue on only one side of its
                // boundary. Try the other isometric fold for the whole polygon,
                // keeping phase and density, before retaining Original sampling.
                var reversed = polygon.Select(v => Project(receiver, v.Position, polygonNormal, kind, true, -Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[43])).ToArray();
                if (Permitted(reversed))
                    mappedPolygon = reversed;
                else
                {
                    mappedPolygon = polygon.Select(v => Project(receiver, v.Position, polygonNormal, kind, false)).ToArray();
                    originalSampling++;
                }
            }

            for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[44]; i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[45] < polygon.Length; i++)
            {
                var triangle = new[]
                {
                    polygon[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[46]],
                    polygon[i],
                    polygon[i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[47]]
                };
                if (!preserveDegenerate && !HasArea(triangle))
                    continue;
                var normal = Normal(triangle[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[48]].Position, triangle[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[49]].Position, triangle[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[50]].Position);
                var mapped = new[]
                {
                    mappedPolygon[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[51]],
                    mappedPolygon[i],
                    mappedPolygon[i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[52]]
                };
                var final = new Vertex[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[53]];
                for (int corner = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[54]; corner < Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[55]; corner++)
                {
                    var v = triangle[corner];
                    final[corner] = new Vertex
                    {
                        Position = v.Position,
                        Weights = v.Weights,
                        Frame = v.Frame,
                        Face = v.Face,
                        Uv = mapped[corner]
                    };
                }

                MovableRoadGeometry.Vertex RoadVertex(Vertex v) => new(v.Position, normal, new Vector4(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[56], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[57], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[58], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[59]), new Vector4(v.Uv.x, v.Uv.y, Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[60], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[61]), Color.white);
                var projection = MovableSurfaceAtlas.OriginalProjection(new MovableRoadGeometry.Triangle(RoadVertex(final[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[62]]), RoadVertex(final[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[63]]), RoadVertex(final[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[64]]), Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[65], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[66], Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[67]));
                foreach (var vertex in final)
                    vertex.Projection = new MovableStructureModel.ProjectedUvFrame(projection.U, projection.V);
                group.Triangles.Add(final);
            }
        }

        var result = groups.Values.Select(g => new KeyValuePair<string, MovableStructureModel.Plan>("Lift02.RoadUnderlay." + g.Surface.id.guid, Build(source, g, renderName))).ToArray();
        Mod.Log.Info($"Lift02 underlay road appearance: inputFaces={source.Fragments.Sum(f => f.Indices.Length / Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[68])}; outputFaces={groups.Values.Sum(g => g.Triangles.Count)}; materials={groups.Count}; originalVerticalSampling={originalSampling}; uncoveredOriginal={uncovered}; textureBaking=0.");
        return result;
    }

    private static Vector2 Project(Regions.Receiver receiver, Vector3 point, Vector3 normal, int kind, bool unfold, int direction = 1)
    {
        double height = receiver.Height.At(new Regions.Point(point.x, point.z));
        double x = point.x, z = point.z;
        if (unfold)
        {
            int axis = kind < Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[69] ? Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[70] : Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[71];
            double depth = (double)point.y - height;
            if (receiver.Binding.SourceDomain.HasValue)
                depth = -Math.Abs(depth);
            double offset = direction * Math.Sign(normal[axis]) * depth;
            if (axis == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[72])
                x += offset;
            else
                z += offset;
        }

        if (receiver.Binding.ProjectionKind != MovableRoadAppearanceBinding.Projection.Corresponding)
            return receiver.Sample(x, z);
        var p = new Vector4((float)x, (float)height, (float)z, Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[73]);
        return new Vector2(Vector4.Dot(p, receiver.U), Vector4.Dot(p, receiver.V));
    }

    private static bool Legal(MovableRoadAppearanceBinding binding, Vector2[] uv)
    {
        if (binding.ProjectionKind == MovableRoadAppearanceBinding.Projection.Corresponding)
            return true;
        var patch = binding.Patch;
        if (binding.ProjectionKind is MovableRoadAppearanceBinding.Projection.Prepared or MovableRoadAppearanceBinding.Projection.SourceUv or MovableRoadAppearanceBinding.Projection.SourceAtlasRepeat)
        {
            var min = patch.PreparedUnit ? patch.FiniteSamplingMin : patch.Min;
            var max = patch.PreparedUnit ? patch.FiniteSamplingMax : patch.Max;
            bool repeatU = patch.PreparedUnit && patch.RepeatU;
            bool repeatV = patch.PreparedUnit && patch.RepeatV;
            // SourceAtlasRepeat was already wrapped by Compose. Extending beyond
            // that finite receiver would need another original-atlas wrap, not
            // merely the texture's full-page wrap mode.
            return uv.All(p => (repeatU || p.x >= min.x && p.x <= max.x) && (repeatV || p.y >= min.y && p.y <= max.y));
        }

        return false;
    }

    private static double Distance(Regions.Receiver receiver, Vector3 point)
    {
        double best = double.PositiveInfinity;
        for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[74]; i < receiver.Polygon.Length; i++)
        {
            var a = receiver.Polygon[i];
            var b = receiver.Polygon[(i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[75]) % receiver.Polygon.Length];
            double x = b.X - a.X, z = b.Z - a.Z, length = x * x + z * z;
            double t = length == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[76] ? Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[77] : Math.Max(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[78], Math.Min(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[79], ((point.x - a.X) * x + (point.z - a.Z) * z) / length));
            double dx = point.x - a.X - t * x, dz = point.z - a.Z - t * z;
            best = Math.Min(best, dx * dx + dz * dz);
        }

        return best;
    }

    private static Vertex[] Clip(Vertex[] polygon, Regions.Line line) => ClipValues(polygon, v => line.At(new Regions.Point(v.Position.x, v.Position.z)));
    private static Vertex[] ClipValues(Vertex[] polygon, Func<Vertex, double> value)
    {
        var result = new List<Vertex>();
        for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[80]; i < polygon.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[81]) % polygon.Length];
            double av = value(a);
            double bv = value(b);
            if (av >= Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[82])
                result.Add(a);
            if ((av < Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[83] && bv > Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[84]) || (av > Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[85] && bv < Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[86]))
            {
                float t = (float)(av / (av - bv));
                result.Add(new Vertex { Position = Vector3.LerpUnclamped(a.Position, b.Position, t), Weights = Vector3.LerpUnclamped(a.Weights, b.Weights, t), Frame = a.Frame, Face = a.Face });
            }
        }

        return result.ToArray();
    }

    private static bool HasArea(IReadOnlyList<Vertex> vertices)
    {
        for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[87]; i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[88] < vertices.Count; i++)
            if (Normal(vertices[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[89]].Position, vertices[i].Position, vertices[i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[90]].Position) != Vector3.zero)
                return true;
        return false;
    }

    private static Vector3 Normal(Vector3 a, Vector3 b, Vector3 c)
    {
        double ex = (double)b.x - a.x, ey = (double)b.y - a.y, ez = (double)b.z - a.z;
        double fx = (double)c.x - a.x, fy = (double)c.y - a.y, fz = (double)c.z - a.z;
        double x = ey * fz - ez * fy, y = ez * fx - ex * fz, z = ex * fy - ey * fx;
        double length = Math.Sqrt(x * x + y * y + z * z);
        return length == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[91] ? Vector3.zero : new Vector3((float)(x / length), (float)(y / length), (float)(z / length));
    }

    private static MovableStructureModel.Plan Build(MovableStructureModel.Plan source, Group group, string renderName)
    {
        var vertices = group.Triangles.SelectMany(t => t).ToArray();
        var edges = new List<MovableStructureModel.EdgeVertex>();
        var interiors = new List<MovableStructureModel.TriangleVertex>();
        var copies = new List<int>();
        var kinds = new int[vertices.Length];
        var offsets = new int[vertices.Length];
        for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[92]; i < vertices.Length; i++)
        {
            var v = vertices[i];
            var ids = source.SourceIndices[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[93]].Skip(v.Face * Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[94]).Take(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[95]).ToArray();
            // Store the largest weight implicitly. Encoding a tiny A as 1-B-C
            // can round B+C to one even though the source point is interior.
            // A changed source-corner order preserves provenance without epsilon.
            var weights = new[]
            {
                (double)v.Weights.x,
                v.Weights.y,
                v.Weights.z
            };
            double sum = weights.Sum();
            if (weights.Any(w => w < Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[96]) || !(sum > Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[97]))
                throw new InvalidOperationException($"Underlay material subdivision has invalid source weights: face={v.Face}; weights={v.Weights}.");
            int anchor = weights[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[98]] > weights[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[99]] ? Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[100] : Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[101];
            if (weights[Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[102]] > weights[anchor])
                anchor = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[103];
            int second = (anchor + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[104]) % Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[105], third = (anchor + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[106]) % Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[107];
            float wb = (float)(weights[second] / sum), wc = (float)(weights[third] / sum);
            if (wb == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[108] && wc == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[109])
            {
                kinds[i] = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[110];
                offsets[i] = copies.Count;
                copies.Add(ids[anchor]);
            }
            else if (wb == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[111] || wc == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[112])
            {
                offsets[i] = edges.Count;
                edges.Add(new MovableStructureModel.EdgeVertex(ids[anchor], ids[wb == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[113] ? third : second], wb == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[114] ? wc : wb));
            }
            else
            {
                kinds[i] = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[115];
                offsets[i] = interiors.Count;
                interiors.Add(new MovableStructureModel.TriangleVertex(ids[anchor], ids[second], ids[third], wb, wc));
            }
        }

        int Final(int i) => source.SourcePositions.Length + offsets[i] + (kinds[i] == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[116] ? Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[117] : edges.Count) + (kinds[i] == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[118] ? interiors.Count : Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[119]);
        var fragments = new List<MovableStructureModel.Fragment>();
        var byFace = new Dictionary<int, List<int>>();
        for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[120]; i < vertices.Length; i += Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[121])
        {
            int face = vertices[i].Face;
            fragments.Add(new MovableStructureModel.Fragment(Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[122], face, Final(i), Final(i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[123]), Final(i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[124])));
            if (!byFace.TryGetValue(face, out var used))
                byFace.Add(face, used = new List<int>());
            used.AddRange(new[] { i, i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[125], i + Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[126] });
        }

        var domains = MovableUvDomains.For(renderName, Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[127]) ?? throw new InvalidOperationException("Underlay source UV-domain recipe is missing.");
        var prepared = new List<MovableStructureModel.PreparedDomain>();
        var assigned = new HashSet<int>();
        for (int domain = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[128]; domain < domains.Items.Length; domain++)
        {
            var used = new List<int>();
            foreach (int face in domains.Items[domain].Faces)
                if (byFace.TryGetValue(face, out var list))
                {
                    used.AddRange(list);
                    assigned.Add(face);
                }

            if (used.Count == Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[129])
                continue;
            prepared.Add(new MovableStructureModel.PreparedDomain(domain, group.Surface, used.Select(i => new KeyValuePair<int, Vector2>(Final(i), vertices[i].Uv)).ToArray(), used.Select(i => new KeyValuePair<int, MovableStructureModel.ProjectedUvFrame>(Final(i), vertices[i].Projection)).ToArray()));
        }

        if (assigned.Count != byFace.Count)
            throw new InvalidOperationException("An authored underlay face is missing from its UV-domain recipe.");
        var result = new MovableStructureModel.Plan(source.SourcePositions, source.SourcePositions, source.SourceIndices, source.RemoveTriangles.Select(m => Enumerable.Repeat(true, m.Length).ToArray()).ToArray(), addedVertices: edges.ToArray(), fragments: fragments.ToArray(), interiorVertices: interiors.ToArray(), reconstructsAuthoredPositions: true, preparedDomains: prepared.ToArray(), frameTransforms: vertices.Select((v, i) => new KeyValuePair<int, Matrix4x4>(Final(i), v.Frame)).ToArray(), sourceCopies: copies.ToArray());
        for (int i = Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values.I32[130]; i < vertices.Length; i++)
            result.Positions[Final(i)] = vertices[i].Position;
        return result;
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02UnderlayAppearance_cs
internal static class Numeric_Types_Lift02_Lift02UnderlayAppearance_cs
{
    static Numeric_Types_Lift02_Lift02UnderlayAppearance_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift02_Lift02UnderlayAppearance_cs.Values", () => HardcodedNumbers.Load("Types_Lift02_Lift02UnderlayAppearance_cs"));
}
