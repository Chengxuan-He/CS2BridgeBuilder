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
            new MovableStructureModel.EdgeVertex(536, 534, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(534, 537, 0.99905580282211304f),
            new MovableStructureModel.EdgeVertex(1100, 1099, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1099, 1102, 0.99905580282211304f),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    internal static MovableStructureModel.TriangleVertex[] Interior(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.TriangleVertex>(),
        2 => new[]
        {
            new MovableStructureModel.TriangleVertex(534, 536, 537, 0.20495568215847015f, 0.72345364093780518f),
            new MovableStructureModel.TriangleVertex(534, 536, 537, 0.011584078893065453f, 0.98745667934417725f),
            new MovableStructureModel.TriangleVertex(1100, 1099, 1102, 0.071590684354305267f, 0.72345364093780518f),
            new MovableStructureModel.TriangleVertex(1100, 1099, 1102, 0.00095921452157199383f, 0.98745667934417725f),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    // Absolute starts: all edge vertices precede all interior vertices in Plan.
    internal static MovableStructureModel.Fragment[] Faces(int lod, int firstEdge, int firstInterior) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(0, 317, firstInterior + 0, firstEdge + 0, 536, firstInterior + 0, 536, firstInterior + 1, firstInterior + 1, 536, 537, firstInterior + 1, 537, firstEdge + 1),
            new MovableStructureModel.Fragment(0, 646, firstInterior + 2, 1100, firstEdge + 2, firstInterior + 2, firstInterior + 3, 1100, firstInterior + 3, 1102, 1100, firstInterior + 3, firstEdge + 3, 1102),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };
}
