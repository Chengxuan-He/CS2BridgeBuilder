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
            new MovableStructureModel.EdgeVertex(192, 191, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(193, 191, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(192, 191, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(192, 194, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(459, 458, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(459, 460, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(459, 458, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(461, 458, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(742, 741, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(742, 743, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(742, 741, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(744, 741, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(1015, 1014, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(1016, 1014, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(1015, 1014, 0.95997393131256104f),
            new MovableStructureModel.EdgeVertex(1015, 1017, 0.95997393131256104f),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    internal static MovableStructureModel.Fragment[] Faces(int lod, int firstAddedVertex) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(0, 116, firstAddedVertex + 0, 192, 193, firstAddedVertex + 0, 193, firstAddedVertex + 1),
            new MovableStructureModel.Fragment(0, 117, 192, firstAddedVertex + 2, firstAddedVertex + 3),
            new MovableStructureModel.Fragment(0, 276, firstAddedVertex + 4, 459, firstAddedVertex + 5),
            new MovableStructureModel.Fragment(0, 277, 459, firstAddedVertex + 6, firstAddedVertex + 7, 459, firstAddedVertex + 7, 461),
            new MovableStructureModel.Fragment(0, 442, firstAddedVertex + 8, 742, firstAddedVertex + 9),
            new MovableStructureModel.Fragment(0, 443, 742, firstAddedVertex + 10, firstAddedVertex + 11, 742, firstAddedVertex + 11, 744),
            new MovableStructureModel.Fragment(0, 600, firstAddedVertex + 12, 1015, 1016, firstAddedVertex + 12, 1016, firstAddedVertex + 13),
            new MovableStructureModel.Fragment(0, 601, 1015, firstAddedVertex + 14, firstAddedVertex + 15),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };
}
