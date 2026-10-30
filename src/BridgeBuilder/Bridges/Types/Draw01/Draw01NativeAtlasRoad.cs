using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;

namespace BridgeBuilder.Bridges;
// Original finite terminals and continuous offline source-unit domains.
// Internal prototype reset stations are not emitted as geometry partitions.
internal static class Draw01NativeAtlasRoad
{
    private static readonly float[] Data = HardcodedNumbers.Load("Types_Draw01_NativeAtlasRoadMeasurements").F32;
    private static int Stride => Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[0];

    internal static Draw01FarAppearance.Result Create(MovableClosedRoad.Result road, Pose roadToObject, SurfaceAsset native, SurfaceAsset? paintedRoad = null, Draw01FarAppearance.Result? fallback = null, bool middleLod = false)
    {
        var geometry = MovableRoadGeometry.InFrame(road.Triangles, roadToObject);
        var cross = Matrix4x4.identity;
        cross.SetRow(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[1], new Vector4(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[2], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[3], -Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[4], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[5]));
        cross.SetRow(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[6], new Vector4(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[7], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[8], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[9], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[10]));
        var quadrants = new Partition(Matrix4x4.identity, new Partition(cross, new Partition(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[11]), new Partition(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[12])), new Partition(cross, new Partition(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[13]), new Partition(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[14])));
        if (!MovableRoadGeometry.TryBind(geometry, quadrants, Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[15], road.Surfaces.Length, out var halves, out var error))
            throw new InvalidOperationException(error);
        var bounds = new Dictionary<(object, bool, bool), (float Min, float Max)>();
        foreach (var face in halves)
        {
            object section = road.Occurrences[face.Geometry.Occurrence].RootSection ?? throw new InvalidOperationException("Draw01 atlas road has no section provenance.");
            var key = (section, (face.Bone & (uint)(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[16])) != (uint)(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[17]), face.Bone >= (uint)(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[18]));
            foreach (var v in new[]
            {
                face.Geometry.A,
                face.Geometry.B,
                face.Geometry.C
            }

            )
            {
                float x = v.Position.x;
                bounds[key] = bounds.TryGetValue(key, out var b) ? (Math.Min(x, b.Min), Math.Max(x, b.Max)) : (x, x);
            }
        }

        var patches = fallback == null ? new List<MovableExistingRoadSurface.Patch>() : new List<MovableExistingRoadSurface.Patch>(fallback.Patches);
        var types = fallback == null ? new List<Type>() : new List<Type>(fallback.SupportedTypes);
        int untouched = patches.Count;
        patches.Add(new MovableExistingRoadSurface.Patch(Vector2.zero, Vector2.one));
        types.Add(Type.None);
        var original = new Partition(untouched);
        var partitions = new Partition[road.Occurrences.Length];
        var frame = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        Partition Cut(Vector4 plane, Partition negative, Partition positive)
        {
            var transform = Matrix4x4.identity;
            transform.SetRow(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[19], new Vector4(-plane.z, Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[20], plane.x, Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[21]));
            transform.SetRow(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[22], plane);
            return new Partition(transform * frame, negative, positive);
        }

        for (int occurrence = Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[23]; occurrence < partitions.Length; occurrence++)
        {
            var section = road.Occurrences[occurrence].RootSection ?? throw new InvalidOperationException("Draw01 atlas road has no section provenance.");
            bool sidewalk = section.name == "Sidewalk" || section.name.StartsWith("Sidewalk ", StringComparison.Ordinal);
            Partition Half(bool right, bool positive)
            {
                if (!bounds.TryGetValue(((object)section, right, positive), out var extent) || !(extent.Max > extent.Min))
                    return Partition.Gap;
                float sign = positive ? Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[24] : -Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[25];
                Partition Domain(int domain)
                {
                    // Middle fixed-carriageway XYZ/UV triangles and native N/T/bones
                    // exactly match near; AuditMiddleCoverage.py retains the proof.
                    if (fallback != null && middleLod && domain == 2)
                        return Draw01MiddleFixedWalk.Create(right,positive,extent.Min,extent.Max,frame,native,fallback.Occurrences[occurrence],patches,types,Cut);
                    if (fallback != null && middleLod && domain == 0)
                        return Draw01MiddleAsphaltEnds.Create(right,positive,extent.Min,extent.Max,frame,fallback.Occurrences[occurrence],patches,types,Cut);
                    var cells = new List<int>();
                    for (int at = Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[26]; at < Data.Length; at += Stride)
                        if ((int)Data[at] == domain && (Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[27]] != Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[28]) == right && (Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[29]] != Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[30]) == positive)
                            cells.Add(at);
                    cells.Sort((a, b) => Data[a + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[31]].CompareTo(Data[b + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[32]]));
                    // First/last authored partial domains are finite. Collapse only
                    // the evidenced full-period interior into one continuous domain.
                    var domains = new List<(int At, float End, bool Prepared)>();
                    if (cells.Count != 0)
                    {
                        domains.Add((cells[0], Data[cells[0] + 4], false));
                        bool terminal = domain < 2;
                        int last = cells.Count - (terminal ? 2 : 1);
                        int reference = Math.Min(2, last);
                        domains.Add((cells[reference], Data[cells[last] + 4], true));
                        if (terminal) domains.Add((cells[cells.Count-1], Data[cells[cells.Count-1]+4], false));
                    }
                    Partition next = original;
                    for (int cell = domains.Count - 1; cell >= 0; cell--)
                    {
                        int at = domains[cell].At;
                        float scale = (Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[35]] - Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[36]]) / (extent.Max - extent.Min);
                        float offset = Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[37]] - scale * extent.Min;
                        var u = new Vector4(Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[38]] * scale, Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[39], Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[40]], Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[41]] * offset + Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[42]]);
                        var v = new Vector4(Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[43]] * scale, Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[44], Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[45]], Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[46]] * offset + Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[47]]);
                        // Native codec proves this projection frame matches every near cell.
                        int index = patches.Count;
                        if (domains[cell].Prepared)
                            patches.Add(Draw01PreparedRoadUnit.Create(domain,u,v,frame,Data[at+7],Data[at+9]));
                        else if (domain == 0)
                            patches.Add(Draw01FiniteAsphalt.Create(u,v,frame,new Vector2(Data[at+7],Data[at+8]),new Vector2(Data[at+9],Data[at+10])));
                        else
                        patches.Add(new MovableExistingRoadSurface.Patch(new Vector2(Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[48]], Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[49]]), new Vector2(Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[50]], Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[51]]), frame.transpose * u, frame.transpose * v, domain == Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[52] ? paintedRoad ?? native : native, sourceUv: true, projectTangent: true));
                        types.Add(sidewalk ? Type.Sidewalk : Type.Asphalt);
                        next = Cut(new Vector4(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[53], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[54], sign, -domains[cell].End), new Partition(index), next);
                    }

                    return cells.Count == Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[56] ? Partition.Gap : Cut(new Vector4(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[57], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[58], sign, -Data[cells[Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[59]] + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[60]]), Partition.Gap, next);
                }

                int movingDomain = sidewalk ? Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[61] : Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[62], fixedDomain = sidewalk ? Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[63] : Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[64];
                float fixedStart = float.PositiveInfinity;
                for (int at = Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[65]; at < Data.Length; at += Stride)
                    if ((int)Data[at] == fixedDomain && (Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[66]] != Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[67]) == right && (Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[68]] != Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[69]) == positive)
                        fixedStart = Math.Min(fixedStart, Data[at + Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[70]]);
                return Cut(new Vector4(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[71], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[72], sign, -fixedStart), Domain(movingDomain), Domain(fixedDomain));
            }

            Partition Side(bool positive) => Cut(new Vector4(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[73], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[74], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[75], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[76]), Half(false, positive), Half(true, positive));
            partitions[occurrence] = Cut(new Vector4(Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[77], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[78], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[79], Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs.Values.I32[80]), Side(false), Side(true));
        }

        return new Draw01FarAppearance.Result(patches.ToArray(), types.ToArray(), partitions);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01NativeAtlasRoad_cs
internal static class Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs
{
    static Numeric_Types_Draw01_Draw01NativeAtlasRoad_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01NativeAtlasRoad_cs");
}
