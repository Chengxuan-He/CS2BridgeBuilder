// Measured DrawBridge01Pillar main mesh, native game 1.6.2f1.
// Simplified LOD stations inherit reviewed high-detail wall membership and span.
using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw01ColumnWideningData
{
    internal static MovableStructureModel.Plan CreatePlan(int lod, Vector3[] sourcePositions,
        float halfWidthDelta, int[][] sourceIndices)
    {
        var counts = lod switch
        {
            0 => (Vertices: 288, Triangles: 144),
            1 => (Vertices: 259, Triangles: 129),
            2 => (Vertices: 131, Triangles: 88),
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
        if (sourcePositions.Length != counts.Vertices || sourceIndices.Length != 1 ||
            sourceIndices[0].Length != counts.Triangles * 3)
            throw new ArgumentException("Draw01 column requires the recorded native mesh layout.");
        var positions = (Vector3[])sourcePositions.Clone();
        var groups = lod switch { 0 => Near, 1 => Middle, 2 => Far, _ => throw new ArgumentOutOfRangeException(nameof(lod)) };
        foreach (var group in groups)
        {
            if (group.Weight == 0f) continue;
            var displacement = group.Weight * halfWidthDelta;
            foreach (var index in group.Vertices) positions[index].x += displacement;
        }
        return new MovableStructureModel.Plan(sourcePositions, positions, sourceIndices,
            new[] { new bool[counts.Triangles] });
    }

    // Native Geometry SHA-256: 58E287710EBCCC17052472C54F6C88EDFD876DE5C8DFB68BB40BE5236DF4E5B8
    private static readonly (float Weight, int[] Vertices)[] Near =
    {
        (-1f, new[]
        {
            12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27,
            28, 29, 30, 31, 36, 37, 40, 41, 42, 43, 56, 57, 58, 59, 66, 67,
            78, 79, 84, 85, 86, 87, 100, 101, 102, 103, 104, 105, 112, 113, 114, 115,
            116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131,
            156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171,
            172, 173, 174, 175, 180, 182, 184, 185, 186, 187, 200, 201, 202, 203, 209, 211,
            221, 223, 228, 229, 230, 231, 244, 245, 246, 247, 248, 250, 256, 257, 258, 259,
            260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275,
        }),
        (0f, new[]
        {
            2, 3, 32, 33, 38, 39, 64, 65, 76, 77, 106, 107, 108, 109, 142, 143,
            145, 147, 176, 178, 181, 183, 208, 210, 220, 222, 249, 251, 252, 254, 285, 287,
        }),
        (1f, new[]
        {
            0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 34, 35, 44, 45, 46, 47,
            48, 49, 50, 51, 52, 53, 54, 55, 60, 61, 62, 63, 68, 69, 70, 71,
            72, 73, 74, 75, 80, 81, 82, 83, 88, 89, 90, 91, 92, 93, 94, 95,
            96, 97, 98, 99, 110, 111, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141,
            144, 146, 148, 149, 150, 151, 152, 153, 154, 155, 177, 179, 188, 189, 190, 191,
            192, 193, 194, 195, 196, 197, 198, 199, 204, 205, 206, 207, 212, 213, 214, 215,
            216, 217, 218, 219, 224, 225, 226, 227, 232, 233, 234, 235, 236, 237, 238, 239,
            240, 241, 242, 243, 253, 255, 276, 277, 278, 279, 280, 281, 282, 283, 284, 286,
        }),
    };

    // Native Geometry SHA-256: 75FD4EC55594F8B785CF8ABA7D826645BB92FF04BD7AA7FF7B0E3C030DD3BB45
    private static readonly (float Weight, int[] Vertices)[] Middle =
    {
        (-1f, new[]
        {
            26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 37, 38, 39, 40, 41, 42,
            43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 58, 59,
            61, 62, 63, 64, 65, 66, 67, 70, 71, 72, 102, 105, 106, 109, 110, 111,
            115, 116, 117, 121, 122, 123, 124, 125, 154, 155, 156, 157, 158, 159, 160, 161,
            162, 163, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178,
            179, 180, 181, 182, 183, 184, 186, 188, 189, 190, 191, 192, 193, 194, 195, 198,
            199, 200, 230, 233, 234, 237, 238, 239, 243, 244, 245, 249, 250, 251, 252, 253,
        }),
        (0f, new[]
        {
            2, 3, 15, 16, 17, 24, 57, 60, 68, 69, 73, 76, 101, 112, 118, 119,
            132, 134, 143, 144, 145, 152, 185, 187, 196, 197, 202, 204, 229, 240, 246, 247,
        }),
        (1f, new[]
        {
            0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 18, 19, 20,
            21, 22, 23, 25, 36, 74, 75, 77, 78, 79, 80, 81, 82, 83, 84, 85,
            86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 103,
            104, 107, 108, 113, 114, 120, 126, 127, 128, 129, 130, 131, 133, 135, 136, 137,
            138, 139, 140, 141, 142, 146, 147, 148, 149, 150, 151, 153, 164, 201, 203, 205,
            206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221,
            222, 223, 224, 225, 226, 227, 228, 231, 232, 235, 236, 241, 242, 248, 254, 255,
            256, 257, 258,
        }),
    };

    // Native Geometry SHA-256: DC1A8A335221D46CAAF2D531D9B5B8D69DD6B6AB830EC4F229BB07491B5AEFB9
    private static readonly (float Weight, int[] Vertices)[] Far =
    {
        (-1f, new[]
        {
            0, 2, 4, 5, 6, 8, 9, 10, 14, 20, 25, 31, 34, 41, 45, 46,
            63, 72, 73, 78, 80, 81, 82, 84, 86, 89, 90, 91, 99, 100, 102, 110,
            111, 112, 113,
        }),
        (-0.201095581f, new[]
        {
            98, 108,
        }),
        (-0.154919028f, new[]
        {
            52, 62,
        }),
        (-0.074007228f, new[]
        {
            13, 17, 19, 22,
        }),
        (-0.0492286682f, new[]
        {
            83, 85,
        }),
        (0f, new[]
        {
            1, 3, 7, 11, 12, 15, 26, 29, 30, 32, 33, 35, 61, 74, 75, 76,
            77, 79, 97, 101, 106, 107, 109, 114, 122, 128,
        }),
        (0.154926181f, new[]
        {
            121, 126,
        }),
        (0.201095581f, new[]
        {
            40, 129,
        }),
        (1f, new[]
        {
            16, 18, 21, 23, 24, 27, 28, 36, 37, 38, 39, 42, 43, 44, 47, 48,
            49, 50, 51, 53, 54, 55, 56, 57, 58, 59, 60, 64, 65, 66, 67, 68,
            69, 70, 71, 87, 88, 92, 93, 94, 95, 96, 103, 104, 105, 115, 116, 117,
            118, 119, 120, 123, 124, 125, 127, 130,
        }),
    };

}
