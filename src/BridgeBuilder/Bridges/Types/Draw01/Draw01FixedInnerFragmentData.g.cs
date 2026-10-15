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
            new MovableStructureModel.EdgeVertex(195, 198, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(468, 470, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(752, 754, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(1023, 1026, 0.96809279918670654f),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    internal static MovableStructureModel.Fragment[] Faces(int lod, int firstAddedVertex) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(0, 119, 196, 195, firstAddedVertex + 0),
            new MovableStructureModel.Fragment(0, 282, 468, 467, firstAddedVertex + 1),
            new MovableStructureModel.Fragment(0, 447, 752, 751, firstAddedVertex + 2),
            new MovableStructureModel.Fragment(0, 606, 1024, 1023, firstAddedVertex + 3),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };
}
