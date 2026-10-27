using UnityEngine;
namespace BridgeBuilder.Bridges;
// Data-only fixture for the unchanged public RegionMapping implementation.
// This does not simulate runtime Unit construction or prove future width coverage.
internal static class MovableStructurePacking
{
    internal sealed class Unit
    {
        internal bool Triangle;
        internal bool RepeatOnly=false;
        internal Vector2 A,B,C;
        internal Vector4 Bounds;
        internal Dictionary<Vector2,double[]>? ExactCoordinates=null;
        internal readonly List<Vector2> Points=new();
    }
}
