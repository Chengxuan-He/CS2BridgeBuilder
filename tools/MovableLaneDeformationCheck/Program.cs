using BridgeBuilder.Bridges;
using UnityEngine;
using BridgeBuilder.Runtime;
using Game.Net;
using Game.Prefabs;
using Unity.Mathematics;

// Native train and tram auxiliary wires exclude ordinary straight nodes.
var wire = new AuxiliaryNetLane {
    m_CompositionAny = new CompositionFlags { m_General = CompositionFlags.General.Edge |
        CompositionFlags.General.Intersection | CompositionFlags.General.DeadEnd |
        CompositionFlags.General.LevelCrossing },
    m_Position = new float3(0, 6, 0), m_Spacing = new float3(0, 0, 50)
};
var straight = new CompositionFlags { m_General = CompositionFlags.General.Node };
if (NetCompositionHelpers.TestLaneFlags(wire, straight)) throw new Exception("Invalid source fixture.");
var convertedWire = MovableCatenarySampling.ForNode(wire, UtilityTypes.Catenary);
if (!NetCompositionHelpers.TestLaneFlags(MovableCatenarySampling.ForNode(wire, UtilityTypes.LowVoltageLine), straight))
    throw new Exception("Train contact wire was omitted.");
var tram = wire;
tram.m_CompositionAny = default;
tram.m_Position = new float3(0, 5, 2);
if (!NetCompositionHelpers.TestLaneFlags(MovableCatenarySampling.ForNode(tram, UtilityTypes.Catenary), straight) ||
    !MovableCatenarySampling.ForNode(tram, UtilityTypes.Catenary).m_Position.Equals(tram.m_Position))
    throw new Exception("Native tram contact wire changed.");
if (!NetCompositionHelpers.TestLaneFlags(convertedWire, straight) ||
    !convertedWire.m_Position.Equals(wire.m_Position) || !convertedWire.m_Spacing.Equals(wire.m_Spacing) ||
    NetCompositionHelpers.TestLaneFlags(MovableCatenarySampling.ForNode(wire, UtilityTypes.WaterPipe), straight))
    throw new Exception("Catenary topology conversion failed.");
wire.m_CompositionNone.m_General = CompositionFlags.General.Tunnel;
var tunnel = straight; tunnel.m_General |= CompositionFlags.General.Tunnel;
if (NetCompositionHelpers.TestLaneFlags(MovableCatenarySampling.ForNode(wire, UtilityTypes.Catenary), tunnel))
    throw new Exception("Catenary exclusion lost.");
wire.m_CompositionAny.m_Left = CompositionFlags.Side.PrimaryTrack;
if (!MovableCatenarySampling.ForNode(wire, UtilityTypes.Catenary).m_CompositionAny.Equals(wire.m_CompositionAny))
    throw new Exception("Mixed upgrade requirements must remain unchanged.");
Console.WriteLine("PASS catenary straight-node sampling and non-catenary/upgrade isolation.");

// Native node tracks carry an elevated rail datum. Projecting the lane centre
// onto the road buries negative-local-Y sleepers and the lower rail profile.
var size = new float4(4, 1, 16, 0);
var nodeLane = new BridgePreviewLanes.Lane(null!, default, null,
    node: new NodeLane { m_SharedStartCount = 1, m_SharedEndCount = 3,
        m_WidthOffset = new float2(0, 0) }, laneData: new NetLaneData { m_Width = 4 });
var nodeParams = nodeLane.CurveParameters(size);
if (nodeParams.y != 0 || nodeParams.z != .5f || nodeParams.w != .25f)
    throw new Exception("Native node lane parameters were lost.");
if (!math.all(nodeLane.CurveScale() == new float4(1, 0, 1, 1)))
    throw new Exception("Native node lane scale changed.");
foreach (var localY in new[] { -.3f, -.15f, 0f })
foreach (var tiled in new[] { false, true })
{
    var curve = new Matrix4x4();
    curve.SetColumn(0, new Vector4(0, .4f, -8, 0));
    curve.SetColumn(1, new Vector4(0, .4f, -8f/3, 1f/3));
    curve.SetColumn(2, new Vector4(0, .4f, 8f/3, 2f/3));
    curve.SetColumn(3, new Vector4(0, .4f, 8, 1));
    var rail = new MovableLaneVisuals.Batch { CurveMatrix = curve,
        Transform = Matrix4x4.identity, Parameters = nodeParams,
        GeometryTiling = tiled, Dilation = new Vector4(1f/16, .01f, 0, 0) };
    var vertex = new MovableRoadGeometry.Vertex(new Vector3(.75f, localY, 0),
        Vector3.up, new Vector4(1,0,0,1), Vector4.zero, Color.white);
    var actual = MovableLaneDeformation.Evaluate(rail, vertex,
        _ => throw new Exception("Node rail was incorrectly projected onto the road."));
    if (Math.Abs(actual.Position.y - (.4f+localY)) > .00001f)
        throw new Exception("Rail/sleeper height lost.");
}
var edgeLane = new BridgePreviewLanes.Lane(null!, default, new EdgeLane());
if (edgeLane.CurveParameters(size).y != 0)
    throw new Exception("Edge rail was projected.");
var projected = new BridgePreviewLanes.Lane(null!, default, null);
if (projected.CurveParameters(size).y != 1)
    throw new Exception("Untyped projected-lane behavior changed.");
var elevated = new BridgePreviewLanes.Lane(null!, default, null,
    elevation: new Game.Net.Elevation { m_Elevation = new float2(1, 1) });
if (elevated.CurveParameters(size).y != 0)
    throw new Exception("Explicit native elevation was lost.");
Console.WriteLine("PASS native node/edge/elevation parameter precedence and rail/sleeper height preservation.");

// LaneSystem's static sag is already present in the native control points.
// For equal-height anchors, sag at the midpoint is length * hanging ratio.
int checkedVertices = 0;
foreach (float length in new[] { 16f, 64f, 144f })
foreach (float hanging in new[] { 0f, .025f, .1f })
foreach (bool reverse in new[] { false, true })
foreach (bool flagged in new[] { false, true })
{
    float sign = reverse ? -1 : 1;
    float controlSag = length * hanging * (4f / 3f);
    var curve = new Matrix4x4();
    curve.SetColumn(0, new Vector4(0, 7, -sign * length / 2, 0));
    curve.SetColumn(1, new Vector4(0, 7-controlSag, -sign * length / 6, 1f/3));
    curve.SetColumn(2, new Vector4(0, 7-controlSag, sign * length / 6, 2f/3));
    curve.SetColumn(3, new Vector4(0, 7, sign * length / 2, 1));
    var batch = new MovableLaneVisuals.Batch
    {
        HangingSwaying = flagged, CurveMatrix = curve,
        Transform = Matrix4x4.identity,
        Parameters = new Vector4(length, 0, 0, 0),
        Dilated = true, Dilation = new Vector4(1, 1, 0, 0)
    };
    for (int i=0; i<=16; i++)
    {
        float t = i / 16f;
        var input = new MovableRoadGeometry.Vertex(new Vector3(0,0,(t-.5f)*length),
            Vector3.up, new Vector4(1,0,0,-1), new Vector4(0,-.25f,0,0), Color.white);
        var actual = MovableLaneDeformation.Evaluate(batch, input, null);
        // Analytic parabola, independent of evaluator's cubic Bezier implementation.
        var expected = new Vector3(0, 7-4*length*hanging*t*(1-t), sign*(t-.5f)*length);
        if ((actual.Position-expected).magnitude > .0001f)
            throw new Exception($"Cable shape mismatch: length={length}, hanging={hanging}, t={t}");
        if (Math.Abs(actual.Normal.magnitude-1) > .0001f ||
            Math.Abs(((Vector3)actual.Tangent).magnitude-1) > .0001f ||
            actual.Tangent.w != -1 || actual.UV.z != 1)
            throw new Exception("Cable attributes were not preserved.");
        if (Math.Abs(Vector3.Dot(actual.Normal, (Vector3)actual.Tangent)) > .0001f)
            throw new Exception("Cable tangent frame is not orthogonal.");
        checkedVertices++;
    }
}
var decal = new MovableLaneVisuals.Batch { Decal = true };
try
{
    MovableLaneDeformation.Evaluate(decal, default, null);
    throw new Exception("Decal geometry was incorrectly accepted.");
}
catch (InvalidOperationException) { }
Console.WriteLine($"PASS {checkedVertices} cable vertices: native static sag once, both directions, endpoints, frame attributes, hanging/ordinary parity; decal rejection retained.");

namespace BridgeBuilder.Bridges
{
    internal static class MovableLaneVisuals
    {
        internal sealed class Batch
        {
            internal bool Decal, HangingSwaying, Dilated;
            internal bool GeometryTiling = false;
            internal Matrix4x4 Transform, CurveMatrix;
            internal Vector4 Parameters, Dilation;
            internal SourceName Source = new();
        }
        internal sealed class SourceName { internal string name = "Measured cable"; }
    }
    internal static class MovableRoadGeometry
    {
        internal readonly record struct Vertex(Vector3 Position, Vector3 Normal,
            Vector4 Tangent, Vector4 UV, Color Color);
    }
}
