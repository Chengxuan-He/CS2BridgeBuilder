// DrawBridge01 far main mesh, inner approach-wall road fragments; game 1.6.2f1.
// Geometry SHA-256: C0CCAEFD49E4CBE4983DC8196F08FE2BA5649DBA8687474B711CFBD2B23EDEDE
using System;

namespace BridgeBuilder.Bridges;
internal static class Draw01FixedInnerFragmentData
{
    internal static MovableStructureModel.EdgeVertex[] Vertices(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.EdgeVertex>(),
        2 => new[]
        {
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[0], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[1], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.F32[0]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[2], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[3], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.F32[1]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[4], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[5], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.F32[2]),
            new MovableStructureModel.EdgeVertex(Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[6], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[7], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.F32[3]),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    internal static MovableStructureModel.Fragment[] Faces(int lod, int firstAddedVertex) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[8], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[9], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[10], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[11], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[12]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[13], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[14], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[15], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[16], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[17]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[18], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[19], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[20], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[21], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[22]),
            new MovableStructureModel.Fragment(Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[23], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[24], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[25], Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[26], firstAddedVertex + Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs.Values.I32[27]),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))};
}

// Embedded numerical arrays: Types_Draw01_Draw01FixedInnerFragmentData_g_cs
internal static class Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs
{
    static Numeric_Types_Draw01_Draw01FixedInnerFragmentData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01FixedInnerFragmentData_g_cs");
}
