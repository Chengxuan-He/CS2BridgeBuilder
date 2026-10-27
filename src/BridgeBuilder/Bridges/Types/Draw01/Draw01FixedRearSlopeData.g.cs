// DrawBridge01 far main mesh: measured retained rear control-building-side slopes.
// Geometry SHA-256: C0CCAEFD49E4CBE4983DC8196F08FE2BA5649DBA8687474B711CFBD2B23EDEDE
using System;

namespace BridgeBuilder.Bridges;
internal static class Draw01FixedRearSlopeData
{
    internal static MovableStructureModel.EdgeVertex[] Edges(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.EdgeVertex>(),
        2 => new[]
        {
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[0], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[1], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[0]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[2], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[3], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[1]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[4], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[5], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[2]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[6], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[7], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[3]),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    internal static MovableStructureModel.TriangleVertex[] Interior(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.TriangleVertex>(),
        2 => new[]
        {
            new MovableStructureModel.TriangleVertex(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[8], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[9], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[10], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[4], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[5]),
            new MovableStructureModel.TriangleVertex(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[11], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[12], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[13], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[6], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[7]),
            new MovableStructureModel.TriangleVertex(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[14], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[15], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[16], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[8], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[9]),
            new MovableStructureModel.TriangleVertex(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[17], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[18], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[19], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[10], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.F32[11]),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    // Absolute starts: all edge vertices precede all interior vertices in Plan.
    internal static MovableStructureModel.Fragment[] Faces(int lod, int firstEdge, int firstInterior) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[20], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[21], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[22], firstEdge + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[23], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[24], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[25], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[26], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[27], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[28], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[29], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[30], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[31], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[32], firstEdge + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[33]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[34], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[35], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[36], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[37], firstEdge + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[38], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[39], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[40], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[41], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[42], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[43], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[44], firstInterior + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[45], firstEdge + Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[46], Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs.Values.I32[47]),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))};
}

// Embedded numerical arrays: Types_Draw01_Draw01FixedRearSlopeData_g_cs
internal static class Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs
{
    static Numeric_Types_Draw01_Draw01FixedRearSlopeData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01FixedRearSlopeData_g_cs");
}
