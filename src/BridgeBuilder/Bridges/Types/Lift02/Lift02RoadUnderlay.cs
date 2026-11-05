using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// The removed native sleepers supplied the underside. Reconstruct their measured
// board stations below the evaluated road. Source channels define provenance;
// the runtime supplement receives the actual selected-road appearance afterward.
internal static class Lift02RoadUnderlay
{
    private sealed class Face
    {
        internal int Triangle;
        internal int[] Vertices = null !;
        internal Vector2[] Parameters = null !;
        internal Matrix4x4 Inverse;
    }

    private sealed class Board
    {
        internal bool First, Last;
        internal int Bone;
        internal float Start, End;
        internal Face[][] Faces = null !;
    }

    private sealed class Recipe
    {
        internal int VertexCount, FaceCount;
        internal Board[] Boards = null !;
    }

    private readonly struct Corner
    {
        internal readonly Vector2 Parameter;
        internal readonly Vector3 Weights;
        internal Corner(Vector2 parameter, Vector3 weights)
        {
            Parameter = parameter;
            Weights = weights;
        }
    }

    private sealed class Constructed
    {
        internal Face Face = null !;
        internal Vector3 Weights, Position;
        internal Matrix4x4 Frame;
    }

    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_Lift02_RoadUnderlay");
    private static readonly Recipe[] Recipes = Read();
    internal static void Append(MovableRoadAssets.Level[] levels, RenderPrefab[] sources, float delta, IReadOnlyList<MovableRoadAppearanceBinding>[] appearances)
    {
        for (int lod = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[0]; lod < levels.Length; lod++)
        {
            var input = levels[lod];
            int sourceLod = Math.Min(lod, Recipes.Length - Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[1]);
            var source = sources[sourceLod];
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(source);
                var mesh = meshes[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[2]];
                var plan = Create(lod, mesh.vertices, mesh.GetIndices(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[3]), mesh.uv, MovableRoadGeometry.InFrame(input.Triangles, input.RoadToObject), delta, source.name, source.surfaceAssets.First());
                var extras = input.AdditionalStructures.ToList();
                var remapped = Lift02UnderlayAppearance.Apply(plan, FaceKinds(sourceLod), source.name, appearances[lod], input.RoadToObject);
                foreach (var mapping in remapped)
                    extras.Add(new MovableRoadAssets.Supplement(source, Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[4], mapping.Value, mapping.Key));
                var bounds = input.RenderBounds;
                foreach (var fragment in plan.Fragments)
                    foreach (int index in fragment.Indices)
                    {
                        bounds.min = math.min(bounds.min, (float3)plan.Positions[index]);
                        bounds.max = math.max(bounds.max, (float3)plan.Positions[index]);
                    }

                levels[lod] = new MovableRoadAssets.Level(input.Source, input.Triangles, input.Surfaces, input.Partition!, bounds, input.RoadToObject, input.Structure, input.Skeleton, input.Lanes, input.ClosedWorld, input.AppearancePageSize, input.TexelsPerMeter, input.RoadPartitions, input.LanePartitions, input.RoadTrianglePartitions, input.ExtraStructure, extras);
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }
    }

    internal static IReadOnlyDictionary<int, int> FaceKinds(int lod)
    {
        var result = new Dictionary<int, int>();
        foreach (var board in Recipes[Math.Min(lod, Recipes.Length - Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[5])].Boards)
            for (int kind = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[6]; kind < board.Faces.Length; kind++)
                foreach (var face in board.Faces[kind])
                    result.Add(face.Triangle, kind);
        return result;
    }

    internal static MovableStructureModel.Plan Create(int lod, Vector3[] positions, int[] indices, Vector2[] uv, IReadOnlyList<MovableRoadGeometry.Triangle> road, float delta, string renderName, Colossal.IO.AssetDatabase.SurfaceAsset surface)
    {
        var recipe = Recipes[Math.Min(lod, Recipes.Length - Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[7])];
        if (positions.Length != recipe.VertexCount || indices.Length != recipe.FaceCount * Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[8] || uv.Length != positions.Length)
            throw new InvalidOperationException("Lift02 underlay source layout differs from its recorded native source.");
        var constructed = new List<Constructed>();
        var fragments = new List<MovableStructureModel.Fragment>();
        float bottom = Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[9]], oldTop = Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[10]];
        void Emit(Face[] faces, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u0 = 0, float u1 = 1, float v0 = 0, float v1 = 1)
        {
            foreach (var face in faces)
            {
                var polygon = new List<Corner>();
                for (int i = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[11]; i < face.Vertices.Length; i++)
                {
                    if (indices[face.Triangle * Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[12] + i] != face.Vertices[i])
                        throw new InvalidOperationException("Lift02 underlay source triangle identity changed.");
                    var weight = Vector3.zero;
                    weight[i] = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[13];
                    polygon.Add(new Corner(face.Parameters[i], weight));
                }

                void Clip(int axis, float value, bool greater)
                {
                    var clipped = new List<Corner>();
                    for (int i = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[14]; i < polygon.Count; i++)
                    {
                        var x = polygon[i];
                        var y = polygon[(i + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[15]) % polygon.Count];
                        bool insideX = greater ? x.Parameter[axis] >= value : x.Parameter[axis] <= value;
                        bool insideY = greater ? y.Parameter[axis] >= value : y.Parameter[axis] <= value;
                        if (insideX)
                            clipped.Add(x);
                        if (insideX != insideY)
                        {
                            float t = (value - x.Parameter[axis]) / (y.Parameter[axis] - x.Parameter[axis]);
                            var parameter = Vector2.LerpUnclamped(x.Parameter, y.Parameter, t);
                            parameter[axis] = value;
                            clipped.Add(new Corner(parameter, Vector3.LerpUnclamped(x.Weights, y.Weights, t)));
                        }
                    }

                    polygon = clipped;
                }

                Clip(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[16], u0, true);
                Clip(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[17], u1, false);
                Clip(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[18], v0, true);
                Clip(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[19], v1, false);
                for (int k = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[20]; k + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[21] < polygon.Count; k++)
                {
                    var corners = new[]
                    {
                        polygon[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[22]],
                        polygon[k],
                        polygon[k + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[23]]
                    };
                    Vector3 Mix(Vector3 x, Vector3 y, float t) => t == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[24] ? x : t == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[25] ? y : Vector3.LerpUnclamped(x, y, t);
                    var target = corners.Select(q => Mix(Mix(a, b, q.Parameter.x), Mix(c, d, q.Parameter.x), q.Parameter.y)).ToArray();
                    var native = corners.Select(q => positions[face.Vertices[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[26]]] * q.Weights.x + positions[face.Vertices[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[27]]] * q.Weights.y + positions[face.Vertices[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[28]]] * q.Weights.z).ToArray();
                    Matrix4x4 Basis(Vector3[] q)
                    {
                        var u = q[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[29]] - q[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[30]];
                        var v = q[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[31]] - q[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[32]];
                        var n = Vector3.Cross(u, v);
                        float length = n.magnitude;
                        if (length != Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[33])
                            n /= length;
                        var result = Matrix4x4.identity;
                        result.SetColumn(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[34], new Vector4(u.x, u.y, u.z, Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[35]));
                        result.SetColumn(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[36], new Vector4(v.x, v.y, v.z, Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[37]));
                        result.SetColumn(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[38], new Vector4(n.x, n.y, n.z, Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[39]));
                        return result;
                    }

                    var sourceBasis = Basis(native);
                    var frame = sourceBasis.determinant == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[40] ? Matrix4x4.identity : Basis(target) * sourceBasis.inverse;
                    var ids = new int[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[41]];
                    for (int i = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[42]; i < ids.Length; i++)
                    {
                        ids[i] = constructed.Count;
                        constructed.Add(new Constructed { Face = face, Weights = corners[i].Weights, Position = target[i], Frame = frame });
                    }

                    fragments.Add(new MovableStructureModel.Fragment(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[43], face.Triangle, ids));
                }
            }
        }

        var profiles = new Dictionary<float, Profile>();
        Profile At(float z)
        {
            if (!profiles.TryGetValue(z, out var profile))
                profiles.Add(z, profile = new Profile(road, z));
            return profile;
        }

        foreach (var board in recipe.Boards)
        {
            // Preserve the authored board subdivision and finite texture phase.
            // This style receives the straight sampled road's cross-section.
            var zs = new[]
            {
                board.Start,
                board.End
            };
            int region = board.End <= Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[44]] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[45] : board.Start >= Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[46]] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[47] : Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[48];
            float innerLeft = Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[49] + region * Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[50]] - delta;
            float innerRight = Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[51] + region * Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[52]] + delta;
            for (int s = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[53]; s + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[54] < zs.Length; s++)
            {
                float z0 = zs[s], z1 = zs[s + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[55]];
                var p0 = At(z0);
                var p1 = At(z1);
                if (p0.Empty || p1.Empty)
                    continue;
                void Floor(float u0, float u1, float v0 = 0, float v1 = 1) => Emit(board.Faces[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[56]], new Vector3(p0.Left, bottom, z0), new Vector3(p0.Right, bottom, z0), new Vector3(p1.Left, bottom, z1), new Vector3(p1.Right, bottom, z1), u0, u1, v0, v1);
                if (lod < Recipes.Length)
                    Floor(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[57], Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[58]);
                else
                {
                    // Existing far bed retains its deep underside. Only the uncovered
                    // outboard strips receive a new bottom at the detailed sleeper datum.
                    var splits = new[]
                    {
                        z0,
                        z1,
                        Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[59]],
                        Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[60]],
                        Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[61]],
                        Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[62]]
                    }.Where(z => z >= z0 && z <= z1).Distinct().OrderBy(z => z).ToArray();
                    for (int part = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[63]; part + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[64] < splits.Length; part++)
                    {
                        float middle = (splits[part] + splits[part + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[65]]) / Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[66];
                        float v0 = (splits[part] - z0) / (z1 - z0), v1 = (splits[part + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[67]] - z0) / (z1 - z0);
                        bool open = (middle >= Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[68]] && middle <= Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[69]]) || (middle >= Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[70]] && middle <= Data.F32[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[71]]);
                        if (open)
                            Floor(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[72], Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[73], v0, v1);
                        else
                        {
                            float left = (innerLeft - p0.Left) / (p0.Right - p0.Left);
                            float right = (innerRight - p0.Left) / (p0.Right - p0.Left);
                            if (left > Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[74])
                                Floor(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[75], Math.Min(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[76], left), v0, v1);
                            if (right < Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[77])
                                Floor(Math.Max(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[78], right), Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[79], v0, v1);
                        }
                    }

                    if (p0.Left < innerLeft || p1.Left < innerLeft)
                    {
                        float r0 = Math.Min(p0.Right, innerLeft), r1 = Math.Min(p1.Right, innerLeft);
                        Emit(board.Faces[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[80]], new Vector3(r0, oldTop, z0), new Vector3(r1, oldTop, z1), new Vector3(r0, bottom, z0), new Vector3(r1, bottom, z1));
                    }

                    if (p0.Right > innerRight || p1.Right > innerRight)
                    {
                        float l0 = Math.Max(p0.Left, innerRight), l1 = Math.Max(p1.Left, innerRight);
                        Emit(board.Faces[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[81]], new Vector3(l0, oldTop, z0), new Vector3(l1, oldTop, z1), new Vector3(l0, bottom, z0), new Vector3(l1, bottom, z1));
                    }
                }

                float leftBottom0 = lod >= Recipes.Length && p0.Left >= innerLeft ? oldTop : bottom;
                float leftBottom1 = lod >= Recipes.Length && p1.Left >= innerLeft ? oldTop : bottom;
                float rightBottom0 = lod >= Recipes.Length && p0.Right <= innerRight ? oldTop : bottom;
                float rightBottom1 = lod >= Recipes.Length && p1.Right <= innerRight ? oldTop : bottom;
                Emit(board.Faces[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[82]], new Vector3(p0.Left, leftBottom0, z0), new Vector3(p1.Left, leftBottom1, z1), new Vector3(p0.Left, p0.Height(p0.Left), z0), new Vector3(p1.Left, p1.Height(p1.Left), z1));
                Emit(board.Faces[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[83]], new Vector3(p0.Right, rightBottom0, z0), new Vector3(p1.Right, rightBottom1, z1), new Vector3(p0.Right, p0.Height(p0.Right), z0), new Vector3(p1.Right, p1.Height(p1.Right), z1));
            }

            void Cap(float z, int kind)
            {
                var p = At(z);
                foreach (var segment in p.Segments)
                {
                    var xs = new[]
                    {
                        segment.A.x,
                        segment.B.x,
                        innerLeft,
                        innerRight
                    }.Where(x => x >= segment.A.x && x <= segment.B.x).Distinct().OrderBy(x => x).ToArray();
                    for (int j = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[84]; j + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[85] < xs.Length; j++)
                    {
                        float x0 = xs[j], x1 = xs[j + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[86]];
                        float y = lod >= Recipes.Length && (x0 + x1) / Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[87] >= innerLeft && (x0 + x1) / Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[88] <= innerRight ? oldTop : bottom;
                        Emit(board.Faces[kind], new Vector3(p.Left, y, z), new Vector3(p.Right, y, z), new Vector3(p.Left, segment.Height(p.Left), z), new Vector3(p.Right, segment.Height(p.Right), z), (x0 - p.Left) / (p.Right - p.Left), (x1 - p.Left) / (p.Right - p.Left));
                    }
                }
            }

            if (board.First)
                Cap(board.Start, Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[89]);
            if (board.Last)
                Cap(board.End, Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[90]);
        }

        var edges = new List<MovableStructureModel.EdgeVertex>();
        var interiors = new List<MovableStructureModel.TriangleVertex>();
        var copies = new List<int>();
        var kinds = new int[constructed.Count];
        var offsets = new int[constructed.Count];
        var values = new Vector2[constructed.Count];
        for (int i = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[91]; i < constructed.Count; i++)
        {
            var item = constructed[i];
            var ids = item.Face.Vertices;
            var w = item.Weights;
            float sum = w.x + w.y + w.z;
            float wb = w.y / sum, wc = w.z / sum;
            if (w.x == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[92])
                wc = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[93] - wb;
            float wa = w.x == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[94] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[95] : Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[96] - (wb + wc);
            int corner = wb == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[97] && wc == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[98] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[99] : wa == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[100] && wc == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[101] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[102] : wa == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[103] && wb == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[104] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[105] : -Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[106];
            if (corner >= Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[107])
            {
                kinds[i] = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[108];
                offsets[i] = copies.Count;
                copies.Add(ids[corner]);
                values[i] = uv[ids[corner]];
            }
            else if (wa == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[109] || wb == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[110] || wc == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[111])
            {
                int a = wa == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[112] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[113] : Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[114], b = wc == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[115] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[116] : Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[117];
                float fraction = wa == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[118] ? wc : wb == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[119] ? wc : wb;
                offsets[i] = edges.Count;
                edges.Add(new MovableStructureModel.EdgeVertex(ids[a], ids[b], fraction));
                values[i] = Vector2.LerpUnclamped(uv[ids[a]], uv[ids[b]], fraction);
            }
            else
            {
                kinds[i] = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[120];
                offsets[i] = interiors.Count;
                interiors.Add(new MovableStructureModel.TriangleVertex(ids[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[121]], ids[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[122]], ids[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[123]], wb, wc));
                values[i] = uv[ids[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[124]]] + (uv[ids[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[125]]] - uv[ids[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[126]]]) * wb + (uv[ids[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[127]]] - uv[ids[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[128]]]) * wc;
            }
        }

        int Final(int i) => positions.Length + offsets[i] + (kinds[i] == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[129] ? Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[130] : edges.Count) + (kinds[i] == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[131] ? interiors.Count : Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[132]);
        var frames = constructed.Select((item, i) => new KeyValuePair<int, Matrix4x4>(Final(i), item.Frame)).ToArray();
        var faceVertices = new Dictionary<int, List<int>>();
        var uvValues = new Dictionary<int, Vector2>();
        var finalFragments = new List<MovableStructureModel.Fragment>();
        foreach (var fragment in fragments)
        {
            if (!faceVertices.TryGetValue(fragment.Triangle, out var used))
                faceVertices.Add(fragment.Triangle, used = new List<int>());
            var ids = fragment.Indices.Select(Final).ToArray();
            used.AddRange(ids);
            foreach (int i in fragment.Indices)
                uvValues[Final(i)] = values[i];
            finalFragments.Add(new MovableStructureModel.Fragment(Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[133], fragment.Triangle, ids));
        }

        var domains = MovableUvDomains.For(renderName, Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[134]);
        var prepared = new List<MovableStructureModel.PreparedDomain>();
        if (domains != null)
            for (int domain = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[135]; domain < domains.Items.Length; domain++)
            {
                var domainValues = new Dictionary<int, Vector2>();
                foreach (int face in domains.Items[domain].Faces)
                    if (faceVertices.TryGetValue(face, out var used))
                        foreach (int vertex in used)
                            domainValues[vertex] = uvValues[vertex];
                if (domainValues.Count != Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[136])
                    prepared.Add(new MovableStructureModel.PreparedDomain(domain, surface, domainValues.ToArray()));
            }

        var result = new MovableStructureModel.Plan(positions, positions, new[] { indices }, new[] { Enumerable.Repeat(true, recipe.FaceCount).ToArray() }, addedVertices: edges.ToArray(), fragments: finalFragments.ToArray(), interiorVertices: interiors.ToArray(), reconstructsAuthoredPositions: true, preparedDomains: prepared.ToArray(), frameTransforms: frames, sourceCopies: copies.ToArray());
        for (int i = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[137]; i < constructed.Count; i++)
            result.Positions[Final(i)] = constructed[i].Position;
        return result;
    }

    private readonly struct Segment
    {
        internal readonly Vector2 A, B;
        internal Segment(Vector2 a, Vector2 b)
        {
            A = a;
            B = b;
        }

        internal float Height(float x) => x == A.x ? A.y : x == B.x ? B.y : A.y + (B.y - A.y) * ((x - A.x) / (B.x - A.x));
    }

    private sealed class Profile
    {
        internal readonly Segment[] Segments;
        internal bool Empty => Segments.Length == Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[138];
        internal float Left => Segments[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[139]].A.x;
        internal float Right => Segments[Segments.Length - Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[140]].B.x;

        internal float Height(float x) => Segments.Where(s => x >= s.A.x && x <= s.B.x).Max(s => s.Height(x));
        internal Profile(IReadOnlyList<MovableRoadGeometry.Triangle> road, float z)
        {
            var raw = new List<Segment>();
            foreach (var triangle in road)
            {
                var v = new[]
                {
                    triangle.A.Position,
                    triangle.B.Position,
                    triangle.C.Position
                };
                if (Vector3.Cross(v[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[141]] - v[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[142]], v[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[143]] - v[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[144]]).y <= Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[145])
                    continue;
                var points = new List<Vector2>();
                for (int i = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[146]; i < v.Length; i++)
                {
                    var a = v[i];
                    var b = v[(i + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[147]) % v.Length];
                    if (a.z == z)
                        points.Add(new Vector2(a.x, a.y));
                    if ((a.z < z && b.z > z) || (a.z > z && b.z < z))
                    {
                        var p = Vector3.LerpUnclamped(a, b, (z - a.z) / (b.z - a.z));
                        points.Add(new Vector2(p.x, p.y));
                    }
                }

                if (points.Count < Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[148])
                    continue;
                var sorted = points.OrderBy(p => p.x).ToArray();
                if (sorted[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[149]].x != sorted[sorted.Length - Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[150]].x)
                    raw.Add(new Segment(sorted[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[151]], sorted[sorted.Length - Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[152]]));
            }

            var xs = raw.SelectMany(s => new[] { s.A.x, s.B.x }).Distinct().OrderBy(x => x).ToArray();
            var segments = new List<Segment>();
            for (int i = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[153]; i + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[154] < xs.Length; i++)
            {
                float middle = (xs[i] + xs[i + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[155]]) / Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[156];
                var covering = raw.Where(s => middle >= s.A.x && middle <= s.B.x).OrderByDescending(s => s.Height(middle)).ToArray();
                if (covering.Length != Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[157])
                    segments.Add(new Segment(new Vector2(xs[i], covering[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[158]].Height(xs[i])), new Vector2(xs[i + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[159]], covering[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[160]].Height(xs[i + Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[161]]))));
            }

            Segments = segments.ToArray();
        }
    }

    private static Recipe[] Read()
    {
        int ip = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[162], fp = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[163];
        var recipes = new Recipe[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[164]];
        for (int lod = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[165]; lod < recipes.Length; lod++)
        {
            var recipe = new Recipe
            {
                VertexCount = Data.I32[ip++],
                FaceCount = Data.I32[ip++]
            };
            recipe.Boards = new Board[Data.I32[ip++]];
            for (int row = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[166]; row < recipe.Boards.Length; row++)
            {
                var board = new Board
                {
                    First = Data.I32[ip++] != Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[167],
                    Last = Data.I32[ip++] != Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[168],
                    Bone = Data.I32[ip++],
                    Start = Data.F32[fp++],
                    End = Data.F32[fp++],
                    Faces = new Face[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[169]][]
                };
                for (int kind = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[170]; kind < board.Faces.Length; kind++)
                {
                    board.Faces[kind] = new Face[Data.I32[ip++]];
                    for (int t = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[171]; t < board.Faces[kind].Length; t++)
                    {
                        var face = new Face
                        {
                            Triangle = Data.I32[ip++],
                            Vertices = new int[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[172]],
                            Parameters = new Vector2[Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[173]],
                            Inverse = Matrix4x4.identity
                        };
                        for (int v = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[174]; v < Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[175]; v++)
                            face.Vertices[v] = Data.I32[ip++];
                        for (int v = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[176]; v < Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[177]; v++)
                            face.Parameters[v] = new Vector2(Data.F32[fp++], Data.F32[fp++]);
                        for (int r = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[178]; r < Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[179]; r++)
                            for (int c = Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[180]; c < Numeric_Types_Lift02_Lift02RoadUnderlay_cs.Values.I32[181]; c++)
                                face.Inverse[r, c] = Data.F32[fp++];
                        board.Faces[kind][t] = face;
                    }
                }

                recipe.Boards[row] = board;
            }

            recipes[lod] = recipe;
        }

        if (ip != Data.I32.Length || fp != Data.F32.Length)
            throw new InvalidOperationException("Lift02 underlay recipe has an invalid record layout.");
        return recipes;
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02RoadUnderlay_cs
internal static class Numeric_Types_Lift02_Lift02RoadUnderlay_cs
{
    static Numeric_Types_Lift02_Lift02RoadUnderlay_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift02_Lift02RoadUnderlay_cs");
}
