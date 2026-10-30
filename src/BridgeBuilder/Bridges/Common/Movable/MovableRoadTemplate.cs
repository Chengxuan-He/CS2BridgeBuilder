using System.Linq;
using UnityEngine;
using Triangle = BridgeBuilder.Bridges.MovableRoadGeometry.Triangle;
using Vertex = BridgeBuilder.Bridges.MovableRoadGeometry.Vertex;
using Bound = BridgeBuilder.Bridges.MovableRoadGeometry.BoundTriangle;

namespace BridgeBuilder.Bridges;

// Prepare selected-road receivers after hardcoded motion-domain partitioning.
// Face records are retained for offline evidence only; runtime never consumes them.
internal static class MovableRoadTemplate
{
    internal readonly struct Face
    {
        internal readonly Vector2 A, B, C;
        internal readonly int Role;
        internal readonly uint Bone;
        internal readonly int OpenEdges;
        internal Face(int role, uint bone, Vector2 a, Vector2 b, Vector2 c, int openEdges)
        { Role = role; Bone = bone; A = a; B = b; C = c; OpenEdges = openEdges; }
    }

    // Motion ownership and gaps have already been constructed from the style's
    // hardcoded seam frames. Preserve the selected road; never intersect it with
    // measured prototype triangles (including copies stored in source tables).
    internal static Triangle[] Prepare(Bound[] input, bool[] converted)
    {
        // Appearance selection happens after physical partitioning. Even network
        // receivers must retain evaluated source UVs and vertex colors here:
        // unsupported types reuse that mapping, and supported composites still
        // need the original mask coordinates. A prototype projection must never
        // erase those inputs before the per-layer decision has been made.
        var prepared = input.Select(b => b.Geometry).ToArray();
        Mod.Log.Info($"Hardcoded road domains: selectedFaces={input.Length}; preparedFaces={prepared.Length}; prototype intersections=0.");
        return prepared;
    }
}
