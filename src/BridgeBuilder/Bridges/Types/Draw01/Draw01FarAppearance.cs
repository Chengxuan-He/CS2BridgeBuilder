using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;
// Finite native LOD2 atlas charts. Coefficients are feasible within each original
// binary16 coordinate's own rounding interval, including after binary32 storage.
// Source mesh SHA-256: 0cee92b3b3503ab5a403e755940bcf39ab2dbe41e4d843bd34bf3b7a8a3025c0.
// No periodic crop, inferred repeat unit, or prototype-triangle intersection.
internal static class Draw01FarAppearance
{
    internal sealed class Result
    {
        internal readonly MovableExistingRoadSurface.Patch[] Patches;
        internal readonly Type[] SupportedTypes;
        internal readonly Partition[] Occurrences;
        internal Result(MovableExistingRoadSurface.Patch[] patches, Type[] types, Partition[] occurrences)
        {
            Patches = patches;
            SupportedTypes = types;
            Occurrences = occurrences;
        }
    }

    private static readonly float[] Data = HardcodedNumbers.Load("Types_Draw01_FarAppearanceMeasurements").F32;
    private static int Stride => Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[0];
    private static int Near => Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[1] * Stride;

    private readonly struct Extent
    {
        internal readonly float MinX, MaxX, EndZ;
        internal Extent(float minX, float maxX, float endZ)
        {
            MinX = minX;
            MaxX = maxX;
            EndZ = endZ;
        }
    }

    internal static Result Create(MovableClosedRoad.Result road, Pose roadToObject, SurfaceAsset native)
    {
        var objectGeometry = MovableRoadGeometry.InFrame(road.Triangles, roadToObject);
        // Multiple evaluated pieces can belong to one selected section. Their shared
        // finite domain must not restart the pattern at each piece occurrence.
        var bounds = new Dictionary<(object, bool, bool), (Vector3 Min, Vector3 Max)>();
        var cross = Matrix4x4.identity;
        cross.SetRow(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[2], new Vector4(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[3], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[4], -Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[5], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[6]));
        cross.SetRow(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[7], new Vector4(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[8], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[9], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[10], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[11]));
        var quadrants = new Partition(Matrix4x4.identity, new Partition(cross, new Partition(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[12]), new Partition(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[13])), new Partition(cross, new Partition(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[14]), new Partition(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[15])));
        if (!MovableRoadGeometry.TryBind(objectGeometry, quadrants, Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[16], road.Surfaces.Length, out var finiteHalves, out var error))
            throw new InvalidOperationException(error);
        foreach (var face in finiteHalves)
            foreach (var p in new[]
            {
                face.Geometry.A.Position,
                face.Geometry.B.Position,
                face.Geometry.C.Position
            }

            )
            {
                var rootSection = road.Occurrences[face.Geometry.Occurrence].RootSection ?? throw new InvalidOperationException("Draw01 finite mapping has no section provenance.");
                var key = ((object)rootSection, (face.Bone & (uint)(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[17])) != (uint)(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[18]), face.Bone < (uint)(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[19]));
                if (bounds.TryGetValue(key, out var b))
                    bounds[key] = (Vector3.Min(b.Min, p), Vector3.Max(b.Max, p));
                else
                    bounds[key] = (p, p);
            }

        var patches = new List<MovableExistingRoadSurface.Patch>();
        var types = new List<Type>();
        var partitions = new Partition[road.Occurrences.Length];
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Cut(Vector4 plane, Partition negative, Partition positive)
        {
            var transform = Matrix4x4.identity;
            transform.SetRow(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[20], new Vector4(-plane.z, Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[21], plane.x, Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[22]));
            transform.SetRow(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[23], plane);
            return new Partition(transform * frame, negative, positive);
        }

        for (int occurrence = Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[24]; occurrence < partitions.Length; occurrence++)
        {
            var section = road.Occurrences[occurrence].RootSection ?? throw new InvalidOperationException("Draw01 finite mapping has no section provenance.");
            bool sidewalk = section.name == "Sidewalk" || section.name.StartsWith("Sidewalk ", StringComparison.Ordinal);
            Partition Half(bool right, bool positive)
            {
                if (!bounds.TryGetValue(((object)section, right, positive), out var b))
                    return Partition.Gap;
                var extent = new Extent(b.Min.x, b.Max.x, positive ? b.Max.z : -b.Min.z);
                if (!(extent.MaxX > extent.MinX) || !(extent.EndZ > Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[25]))
                    return Partition.Gap;
                int quarter = (positive ? Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[26] : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[27]) + (right ? Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[28] : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[29]);
                float sign = positive ? Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[30] : -Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[31];
                float inner = Math.Max(Math.Abs(Data[(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[32] + quarter) * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[33]]), Math.Abs(Data[(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[34] + quarter) * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[35]]));
                float sourceX0 = right ? (sidewalk ? inner : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[36]) : (sidewalk ? Data[quarter * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[37]] : -inner);
                float sourceX1 = right ? (sidewalk ? Data[quarter * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[38]] : inner) : (sidewalk ? -inner : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[39]);
                var xScale = (sourceX1 - sourceX0) / (extent.MaxX - extent.MinX);
                var xOffset = sourceX0 - xScale * extent.MinX;
                int Add(int chart, float targetStart, float targetEnd, float sourceStart, float sourceEnd, float scaleX, float offsetX)
                {
                    if (!(targetEnd > targetStart))
                        throw new InvalidOperationException("Draw01 finite appearance domain collapsed.");
                    int at = chart * Stride;
                    float zs = (sourceEnd - sourceStart) / (targetEnd - targetStart);
                    float zo = sign * (sourceStart - zs * targetStart);
                    var u = new Vector4(Data[at] * scaleX, Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[40], Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[41]] * zs, Data[at] * offsetX + Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[42]] * zo + Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[43]]);
                    var v = new Vector4(Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[44]] * scaleX, Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[45], Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[46]] * zs, Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[47]] * offsetX + Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[48]] * zo + Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[49]]);
                    int index = patches.Count;
                    patches.Add(new MovableExistingRoadSurface.Patch(new Vector2(Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[50]], Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[51]]), new Vector2(Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[52]], Data[at + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[53]]), frame.transpose * u, frame.transpose * v, native, sourceUv: true));
                    types.Add(sidewalk ? Type.Sidewalk : Type.Asphalt);
                    return index;
                }

                float sourceMainStart = Math.Min(Math.Abs(Data[quarter * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[54]]), Math.Abs(Data[quarter * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[55]]));
                float sourceMainEnd = Math.Max(Math.Abs(Data[quarter * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[56]]), Math.Abs(Data[quarter * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[57]]));
                float targetStart = Data[Near + (sidewalk ? Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[58] : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[59])];
                float targetEnd = Data[Near + (sidewalk ? Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[60] : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[61])];
                float fixedStart = Data[Near + (sidewalk ? Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[62] : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[63])];
                int fixedChart = sidewalk ? (positive ? (right ? Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[64] : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[65]) : (right ? Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[66] : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[67])) : Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[68] + quarter;
                int fc = fixedChart * Stride;
                float sourceFixedStart = Math.Min(Math.Abs(Data[fc + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[69]]), Math.Abs(Data[fc + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[70]]));
                float sourceFixedEnd = Math.Max(Math.Abs(Data[fc + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[71]]), Math.Abs(Data[fc + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[72]]));
                float fixedX0 = Data[fc + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[73]], fixedX1 = Data[fc + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[74]];
                float fixedScaleX = (fixedX1 - fixedX0) / (extent.MaxX - extent.MinX);
                float fixedOffsetX = fixedX0 - fixedScaleX * extent.MinX;
                // Occurrences may contain only the moving half. Unvisited fixed leaves
                // need no fabricated positive span or fallback projection.
                Partition fixedPart = Partition.Gap;
                if (extent.EndZ > fixedStart)
                {
                    fixedPart = new Partition(Add(fixedChart, fixedStart, extent.EndZ, sourceFixedStart, sourceFixedEnd, fixedScaleX, fixedOffsetX));
                    if (sidewalk && !right && !positive)
                    {
                        var other = new Partition(Add(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[75], fixedStart, extent.EndZ, sourceFixedStart, sourceFixedEnd, fixedScaleX, fixedOffsetX));
                        // The actual finite UV seam separates the native triangular corner.
                        // Source points: chart B's far inner corner and near outer corner.
                        float x0 = Data[Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[76] * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[77]], z0 = Data[Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[78] * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[79]];
                        float x1 = Data[Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[80] * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[81]], z1 = Data[Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[82] * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[83]];
                        float nx = z1 - z0, nz = x0 - x1;
                        float zs = (sourceFixedEnd - sourceFixedStart) / (extent.EndZ - fixedStart);
                        float zo = -(sourceFixedStart - zs * fixedStart);
                        var plane = new Vector4(nx * fixedScaleX, Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[84], nz * zs, nx * (fixedOffsetX - x0) + nz * (zo - z0));
                        fixedPart = Cut(plane, fixedPart, other);
                    }
                }

                Partition moving;
                if (sidewalk)
                    moving = new Partition(Add(quarter, targetStart, targetEnd, sourceMainStart, sourceMainEnd, xScale, xOffset));
                else
                {
                    int terminal = Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[85] + quarter;
                    float sourceEnd = Math.Max(Math.Abs(Data[terminal * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[86]]), Math.Abs(Data[terminal * Stride + Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[87]]));
                    float cut = targetStart + (sourceMainEnd - sourceMainStart) / (sourceEnd - sourceMainStart) * (targetEnd - targetStart);
                    var main = new Partition(Add(quarter, targetStart, cut, sourceMainStart, sourceMainEnd, xScale, xOffset));
                    var end = new Partition(Add(terminal, cut, targetEnd, sourceMainEnd, sourceEnd, xScale, xOffset));
                    moving = Cut(new Vector4(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[88], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[89], sign, -cut), main, end);
                }

                moving = Cut(new Vector4(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[90], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[91], sign, -targetStart), Partition.Gap, moving);
                var leafAndGap = Cut(new Vector4(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[92], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[93], sign, -targetEnd), moving, Partition.Gap);
                return Cut(new Vector4(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[94], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[95], sign, -fixedStart), leafAndGap, fixedPart);
            }

            Partition Side(bool positive) => Cut(new Vector4(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[96], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[97], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[98], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[99]), Half(false, positive), Half(true, positive));
            partitions[occurrence] = Cut(new Vector4(Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[100], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[101], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[102], Numeric_Types_Draw01_Draw01FarAppearance_cs.Values.I32[103]), Side(false), Side(true));
        }

        return new Result(patches.ToArray(), types.ToArray(), partitions);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01FarAppearance_cs
internal static class Numeric_Types_Draw01_Draw01FarAppearance_cs
{
    static Numeric_Types_Draw01_Draw01FarAppearance_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01FarAppearance_cs");
}
