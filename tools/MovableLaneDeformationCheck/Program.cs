using BridgeBuilder.Bridges;
using UnityEngine;

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
