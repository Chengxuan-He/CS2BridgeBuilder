using System.Text.Json;
using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using G=BridgeBuilder.Bridges.MovableRoadGeometry;
internal static class AtlasWrapReproChecks
{
 internal static void Run(string path,SurfaceAsset surface)
 {
  using var doc=JsonDocument.Parse(File.ReadAllBytes(path));var d=doc.RootElement;
  Vector3 V3(JsonElement e)=>new(e.GetProperty("x").GetSingle(),e.GetProperty("y").GetSingle(),e.GetProperty("z").GetSingle());
  Vector4 V4(JsonElement e)=>new(e.GetProperty("x").GetSingle(),e.GetProperty("y").GetSingle(),e.GetProperty("z").GetSingle(),e.GetProperty("w").GetSingle());
  Vector2 V2(JsonElement e)=>new(e.GetProperty("x").GetSingle(),e.GetProperty("y").GetSingle());
  G.Vertex Vertex(JsonElement e){var c=e.GetProperty("color");return new(V3(e.GetProperty("position")),V3(e.GetProperty("normal")),V4(e.GetProperty("tangent")),V4(e.GetProperty("uv")),new(c.GetProperty("r").GetSingle(),c.GetProperty("g").GetSingle(),c.GetProperty("b").GetSingle(),c.GetProperty("a").GetSingle()));}
  var vs=d.GetProperty("triangle").EnumerateArray().Select(Vertex).ToArray();
  var patch=new MovableExistingRoadSurface.Patch(V2(d.GetProperty("min")),V2(d.GetProperty("max")),V4(d.GetProperty("u")),V4(d.GetProperty("v")),surface,sourceUv:true,repeatU:d.GetProperty("repeatU").GetBoolean(),repeatV:d.GetProperty("repeatV").GetBoolean(),tangent:V4(d.GetProperty("tangent")),sourceAtlasRepeat:true);
  var result=MovableRoadAtlasWrap.Apply(new(vs[0],vs[1],vs[2],0,13,102),patch);
  var seam=result.SelectMany(t=>new[]{t.A,t.B,t.C}).Where(v=>v.Position.x>11&&(v.UV.y==patch.Min.y||v.UV.y==patch.Max.y)).GroupBy(v=>v.Position).ToArray();
  if(seam.Length!=2||seam.Any(g=>!g.Any(v=>v.UV.y==patch.Min.y)||!g.Any(v=>v.UV.y==patch.Max.y)))throw new Exception("Real Lift04 neighbouring wrap cells disagree on their original-edge intersection.");
  var plane=Matrix4x4.identity;plane[2,3]=80;
  var smallest=result.OrderBy(t=>Vector3.Cross(t.B.Position-t.A.Position,t.C.Position-t.A.Position).sqrMagnitude).First();
  var sides=G.SeamDistances(plane,new[]{smallest.A,smallest.B,smallest.C});
  if(!sides.Any(v=>v<0)||sides.Any(v=>v>0))throw new Exception("Genuine final wrap cell loses its negative physical side.");
  if(result.Length!=7)throw new Exception("The genuine fourth repeat cell was removed instead of repaired.");
  Console.WriteLine("PASS actual Lift04 wide parent: seven required triangles retained, both original-edge seam intersections bit-identical, tiny final cell retains negative side.");
 }
}
