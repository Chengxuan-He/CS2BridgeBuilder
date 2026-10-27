// DrawBridge01 far main-mesh central front caps; native game 1.6.2f1.
// Geometry SHA-256: C0CCAEFD49E4CBE4983DC8196F08FE2BA5649DBA8687474B711CFBD2B23EDEDE
using System;

namespace BridgeBuilder.Bridges;
internal static class Draw01FixedFrontFragmentData
{
    internal static MovableStructureModel.EdgeVertex[] Vertices(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.EdgeVertex>(),
        2 => new[]
        {
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[0], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[1], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[0]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[2], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[3], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[1]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[4], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[5], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[2]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[6], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[7], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[3]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[8], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[9], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[4]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[10], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[11], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[5]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[12], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[13], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[6]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[14], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[15], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[7]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[16], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[17], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[8]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[18], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[19], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[9]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[20], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[21], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[10]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[22], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[23], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[11]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[24], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[25], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[12]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[26], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[27], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[13]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[28], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[29], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[14]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[30], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[31], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.F32[15]),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    internal static MovableStructureModel.Fragment[] Faces(int lod, int firstAddedVertex) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[32], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[33], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[34], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[35], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[36], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[37], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[38], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[39]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[40], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[41], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[42], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[43], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[44]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[45], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[46], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[47], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[48], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[49]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[50], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[51], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[52], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[53], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[54], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[55], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[56], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[57]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[58], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[59], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[60], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[61], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[62]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[63], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[64], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[65], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[66], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[67], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[68], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[69], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[70]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[71], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[72], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[73], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[74], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[75], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[76], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[77], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[78]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[79], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[80], Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[81], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[82], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs.Values.I32[83]),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))};
}

// Embedded numerical arrays: Types_Draw01_Draw01FixedFrontFragmentData_g_cs
internal static class Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs
{
    static Numeric_Types_Draw01_Draw01FixedFrontFragmentData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01FixedFrontFragmentData_g_cs");
}
