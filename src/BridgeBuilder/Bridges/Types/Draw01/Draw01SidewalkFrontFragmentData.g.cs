// DrawBridge01 far main mesh, measured sidewalk front caps and retained outer lips.
// Geometry SHA-256: C0CCAEFD49E4CBE4983DC8196F08FE2BA5649DBA8687474B711CFBD2B23EDEDE
using System;

namespace BridgeBuilder.Bridges;

internal static class Draw01SidewalkFrontFragmentData
{
    internal static MovableStructureModel.EdgeVertex[] Edges(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.EdgeVertex>(),
        2 => new[]
        {
            new MovableStructureModel.EdgeVertex(170, 171, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(170, 172, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(170, 171, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(173, 170, 0.0022395097184926271f),
            new MovableStructureModel.EdgeVertex(443, 442, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(443, 444, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(443, 442, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(445, 443, 0.0022395097184926271f),
            new MovableStructureModel.EdgeVertex(723, 722, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(723, 724, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(723, 722, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(725, 723, 0.0022395097184926271f),
            new MovableStructureModel.EdgeVertex(998, 999, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(998, 1000, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(998, 999, 0.96809279918670654f),
            new MovableStructureModel.EdgeVertex(1001, 998, 0.0022395097184926271f),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    internal static MovableStructureModel.TriangleVertex[] Interior(int lod) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.TriangleVertex>(),
        2 => new[]
        {
            new MovableStructureModel.TriangleVertex(171, 170, 173, 0.0022395097184926271f, 0.022865211591124535f),
            new MovableStructureModel.TriangleVertex(171, 170, 173, 0.0022395097184926271f, 0.002963813254609704f),
            new MovableStructureModel.TriangleVertex(443, 442, 445, 0.97518521547317505f, 0.023839686065912247f),
            new MovableStructureModel.TriangleVertex(443, 442, 445, 0.99509251117706299f, 0.0039581814780831337f),
            new MovableStructureModel.TriangleVertex(723, 722, 725, 0.97489529848098755f, 0.022865211591124535f),
            new MovableStructureModel.TriangleVertex(723, 722, 725, 0.99479669332504272f, 0.002963813254609704f),
            new MovableStructureModel.TriangleVertex(999, 998, 1001, 0.00097512529464438558f, 0.023839686065912247f),
            new MovableStructureModel.TriangleVertex(999, 998, 1001, 0.00094931427156552672f, 0.0039581814780831337f),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };

    internal static MovableStructureModel.Fragment[] Faces(int lod, int firstEdge, int firstInterior) => lod switch
    {
        0 or 1 => Array.Empty<MovableStructureModel.Fragment>(),
        2 => new[]
        {
            new MovableStructureModel.Fragment(0, 105, 170, firstEdge + 0, firstEdge + 1),
            new MovableStructureModel.Fragment(0, 106, firstEdge + 2, 170, firstEdge + 3, firstEdge + 2, firstEdge + 3, firstInterior + 0, 171, firstInterior + 1, firstEdge + 3, 171, firstEdge + 3, 173),
            new MovableStructureModel.Fragment(0, 268, firstEdge + 4, 443, firstEdge + 5),
            new MovableStructureModel.Fragment(0, 269, 443, firstEdge + 6, firstInterior + 2, 443, firstInterior + 2, firstEdge + 7, 442, 445, firstEdge + 7, 442, firstEdge + 7, firstInterior + 3),
            new MovableStructureModel.Fragment(0, 433, firstEdge + 8, 723, firstEdge + 9),
            new MovableStructureModel.Fragment(0, 434, 723, firstEdge + 10, firstInterior + 4, 723, firstInterior + 4, firstEdge + 11, 722, 725, firstEdge + 11, 722, firstEdge + 11, firstInterior + 5),
            new MovableStructureModel.Fragment(0, 592, 998, firstEdge + 12, firstEdge + 13),
            new MovableStructureModel.Fragment(0, 593, firstEdge + 14, 998, firstEdge + 15, firstEdge + 14, firstEdge + 15, firstInterior + 6, 999, firstInterior + 7, firstEdge + 15, 999, firstEdge + 15, 1001),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(lod))
    };
}
