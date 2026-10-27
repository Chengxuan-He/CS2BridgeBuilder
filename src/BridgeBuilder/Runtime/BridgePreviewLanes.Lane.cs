using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Unity.Mathematics;

namespace BridgeBuilder.Runtime;

internal sealed partial class BridgePreviewLanes
{
    internal readonly struct Lane
    {
        internal Lane(NetLaneGeometryPrefab prefab, Curve curve, EdgeLane? edge,
            SubMeshFlags rejectedSubMeshes = 0, NodeLane? node = null,
            NetLaneData laneData = default, Game.Net.Elevation? elevation = null)
        { Prefab = prefab; Curve = curve; Edge = edge; RejectedSubMeshes = rejectedSubMeshes;
            Node = node; LaneData = laneData; Elevation = elevation; }
        internal NetLaneGeometryPrefab Prefab { get; }
        internal Curve Curve { get; }
        internal EdgeLane? Edge { get; }
        internal SubMeshFlags RejectedSubMeshes { get; }
        internal NodeLane? Node { get; }
        internal NetLaneData LaneData { get; }
        internal Game.Net.Elevation? Elevation { get; }

        // Match BatchDataSystem's precedence. Node lanes have an authored height;
        // the untyped fallback projects them to the receiver and buries rail ties.
        internal float4 CurveScale() => Node.HasValue
            ? BatchDataHelpers.BuildCurveScale(Node.Value, LaneData)
            : Edge.HasValue ? BatchDataHelpers.BuildCurveScale(Edge.Value)
            : BatchDataHelpers.BuildCurveScale();
        internal float4 CurveParameters(float4 size) => Node.HasValue
            ? BatchDataHelpers.BuildCurveParams(size, Node.Value)
            : Edge.HasValue ? BatchDataHelpers.BuildCurveParams(size, Edge.Value)
            : Elevation.HasValue ? BatchDataHelpers.BuildCurveParams(size, Elevation.Value)
            : BatchDataHelpers.BuildCurveParams(size);
    }

}
