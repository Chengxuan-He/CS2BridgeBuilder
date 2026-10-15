// Measured DrawBridge01 far main-mesh rear caps, game 1.6.2f1.
// Geometry SHA-256: C0CCAEFD49E4CBE4983DC8196F08FE2BA5649DBA8687474B711CFBD2B23EDEDE
// Central and negative-X rear caps only. Positive-X sloped caps 317/646 are separate.
using System;

namespace BridgeBuilder.Bridges;

internal static class Draw01FixedRearFragmentData
{
    internal static MovableStructureModel.EdgeVertex[] Vertices(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.EdgeVertex>(),
        2 => new[]
        {
            new MovableStructureModel.EdgeVertex(535, 534, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(536, 534, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(535, 534, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(535, 539, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(535, 539, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(535, 540, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(535, 540, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(541, 540, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(541, 540, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(541, 542, 0.79067385196685791f),
            new MovableStructureModel.EdgeVertex(1098, 1099, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1100, 1099, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1098, 1099, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1098, 1101, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1105, 1104, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1106, 1104, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1105, 1104, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1105, 1107, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1106, 1104, 0.84208160638809204f),
            new MovableStructureModel.EdgeVertex(1106, 1108, 0.79067385196685791f),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    // firstAddedVertex is the absolute index of this table's first appended vertex.
    internal static MovableStructureModel.Fragment[] Faces(int lod, int firstAddedVertex) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(0, 316, firstAddedVertex + 0, 535, 536, firstAddedVertex + 0, 536, firstAddedVertex + 1),
            new MovableStructureModel.Fragment(0, 319, 535, firstAddedVertex + 2, firstAddedVertex + 3),
            new MovableStructureModel.Fragment(0, 320, 535, firstAddedVertex + 4, firstAddedVertex + 5),
            new MovableStructureModel.Fragment(0, 321, 535, firstAddedVertex + 6, firstAddedVertex + 7, 535, firstAddedVertex + 7, 541),
            new MovableStructureModel.Fragment(0, 322, 541, firstAddedVertex + 8, firstAddedVertex + 9),
            new MovableStructureModel.Fragment(0, 644, 1098, firstAddedVertex + 10, firstAddedVertex + 11, 1098, firstAddedVertex + 11, 1100),
            new MovableStructureModel.Fragment(0, 645, firstAddedVertex + 12, 1098, firstAddedVertex + 13),
            new MovableStructureModel.Fragment(0, 648, firstAddedVertex + 14, 1105, 1106, firstAddedVertex + 14, 1106, firstAddedVertex + 15),
            new MovableStructureModel.Fragment(0, 649, 1105, firstAddedVertex + 16, firstAddedVertex + 17),
            new MovableStructureModel.Fragment(0, 650, firstAddedVertex + 18, 1106, firstAddedVertex + 19),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };
}
