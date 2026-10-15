// Native DrawBridge01 far-LOD mixed road/structure end-cap fragments, game 1.6.2f1.
// Geometry SHA-256: C0CCAEFD49E4CBE4983DC8196F08FE2BA5649DBA8687474B711CFBD2B23EDEDE
// Main mesh 0. Original corners remain unchanged; only the upper road fragment is removed.
using System;

namespace BridgeBuilder.Bridges;

internal static class Draw01RoadFragmentData
{
    internal static MovableStructureModel.EdgeVertex[] MovingVertices(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.EdgeVertex>(),
        2 => new[]
        {
            new MovableStructureModel.EdgeVertex(39, 41, 0.84051460027694702f), // face 24
            new MovableStructureModel.EdgeVertex(319, 320, 0.84051460027694702f), // face 193
            new MovableStructureModel.EdgeVertex(591, 592, 0.84051460027694702f), // face 351
            new MovableStructureModel.EdgeVertex(871, 873, 0.84051460027694702f), // face 516
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    // firstAddedVertex is the original vertex count plus any earlier constructed vertices.
    internal static MovableStructureModel.Fragment[] MovingFaces(int lod, int firstAddedVertex) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(0, 24, 39, 38, firstAddedVertex + 0),
            new MovableStructureModel.Fragment(0, 193, 318, 319, firstAddedVertex + 1),
            new MovableStructureModel.Fragment(0, 351, 590, 591, firstAddedVertex + 2),
            new MovableStructureModel.Fragment(0, 516, 871, 870, firstAddedVertex + 3),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };
}
