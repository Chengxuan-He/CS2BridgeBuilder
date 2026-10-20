using BridgeBuilder.Bridges;
using UnityEngine;

var up = new Vector3(0, 1, 0);
var down = new Vector3(0, -1, 0);
var right = new Vector3(1, 0, 0);
var u = new Vector4(.4f, 0, 0, 0);
var v = new Vector4(0, 0, .4f, 0);
void Check(bool condition, string label)
{
    if (!condition) throw new Exception(label);
}
bool Repeat(Vector4 x, Vector4 y, Vector3 plane, Vector3 a, Vector3 b, Vector3 c)
    => MovableRoadGeometry.CanRepeatProjection(x, y, plane, a, b, c);
Check(Repeat(u, v, up, up, up, up), "Ordinary repeating road rejected");
Check(Repeat(-u, v, up, up, up, up), "Mirrored projection rejected");
Check(Repeat(u, v, down, down, down, down), "Consistent opposite face rejected");
Check(!Repeat(u, v, up, up, down, up), "Mixed handedness admitted");
Check(!Repeat(u, v, up, down, up, up), "First reversed corner admitted");
Check(!Repeat(u, v, up, up, up, down), "Last reversed corner admitted");
Check(!Repeat(u, v, up, up, right, up), "Singular corner admitted");
Check(!Repeat(u, u, up, up, up, up), "Parallel gradients admitted");
Check(!Repeat(u, v, right, up, up, up), "Singular plane admitted");
Check(!Repeat(u, v, up, up, new Vector3(0, float.NaN, 0), up), "Nonfinite corner admitted");

// The atlas's existing planar-chart representation preserves positions and normals,
// while supplying one consistent tangent orientation for non-repeatable receivers.
MovableRoadGeometry.Vertex Vertex(Vector3 p, Vector3 n, float sign) =>
    new(p, n, new Vector4(1, 0, 0, sign), new Vector4(p.x, p.z, 0, 0), new Color(1, 1, 1, 1));
var a = Vertex(new Vector3(0, 0, 0), up, 1);
var b = Vertex(new Vector3(0, 0, 1), down, -1);
var c = Vertex(new Vector3(1, 0, 0), up, 1);
var mixed = new MovableRoadGeometry.Triangle(a, b, c, 0, 7, 13);
var partition = new MovableRoadGeometry.Partition(0);
Check(!MovableRoadGeometry.TryBind(new[] { mixed }, partition, 1, 1, out _, out var error)
    && error!.Contains("mixed tangent handedness"), "Binder must still reject invalid frames");
var remapped = new MovableRoadGeometry.Triangle(a, Vertex(b.Position, b.Normal, 1), c, 0, 7, 13);
Check(MovableRoadGeometry.TryBind(new[] { remapped }, partition, 1, 1, out var result, out error),
    "Planar-chart receiver binding failed: " + error);
Check(result.Length == 1 && result[0].Geometry.B.Position == b.Position &&
    result[0].Geometry.B.Normal == b.Normal && result[0].Geometry.SourceTriangle == 13,
    "Receiver geometry or provenance changed");
Console.WriteLine("PASS projection orientation, mirrored/singular/nonfinite cases, strict binding and chart receiver preservation.");
