using BridgeBuilder.Bridges;
using UnityEngine;
// Validate the extracted Plan against current production source before testing.
var project = ProjectDirectory();
var modelSource = File.ReadAllText(Path.Combine(project, "../../src/BridgeBuilder/Bridges/Common/Movable/MovableStructureModel.cs"));
var expectedModel = "#nullable enable\nusing System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing UnityEngine;\nnamespace BridgeBuilder.Bridges;\n" +
    modelSource[modelSource.IndexOf("internal static class MovableStructureModel", StringComparison.Ordinal)..modelSource.IndexOf("    internal static bool TryBuild", StringComparison.Ordinal)] + "}\n";
if (File.ReadAllText(Path.Combine(project, "Model.generated.cs")).Replace("\r\n", "\n") != expectedModel.Replace("\r\n", "\n"))
    throw new InvalidOperationException("Structural Plan changed; regenerate inputs with ExportInputs.py before running this check.");
static string ProjectDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
if (args.Length < 1 || args.Length > 2 || (args.Length == 2 && args[1] != "--export-detailed"))
    throw new ArgumentException("Usage: Lift03StructureCheck RESEARCH_DIRECTORY [--export-detailed]");
for(int lod=0;lod<2;lod++)
{
 using var reader=new BinaryReader(File.OpenRead(Path.Combine(args[0],$"lift03-widthcheck-{lod}.bin")));
 int n=reader.ReadInt32(),ic=reader.ReadInt32();var source=new Vector3[n];
 for(int i=0;i<n;i++)source[i]=new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
 var indices=new int[ic];for(int i=0;i<ic;i++)indices[i]=reader.ReadInt32();
 var before=(Vector3[])source.Clone();
 foreach(var pair in new (float Upper,float Lower)[]{(-.25f,-.25f),(0f,0f),(2f,2f),(8f,2f),(2f,8f),(-.25f,8f),(8f,-.25f),(0f,2f),(2f,0f),(-9.99f,-4.99f),(-9.99f,0f),(0f,-4.99f),(-3.3563232421875f,-3.3563232421875f),(-9.429199f,-4.99f)})
 {
  float upperDelta=pair.Upper, lowerDelta=pair.Lower, delta=Math.Max(upperDelta,lowerDelta);
  var combined=Lift03DetailedStructure.Create(lod,source,indices,delta,upperDelta,lowerDelta);
  var catenaryFaces = File.ReadLines(Path.Combine(args[0], $"lift03-catenary-faces-{lod}.txt")).Select(int.Parse).ToHashSet();
  if(catenaryFaces.Count != (lod == 0 ? 13832 : 4921)) throw new Exception("Catenary fixture incomplete");
  foreach(var face in catenaryFaces)
   if(!combined.RemoveTriangles[0][face]) throw new Exception($"Native catenary retained: LOD{lod}, face={face}");
  if(combined.Fragments.Any(fragment => catenaryFaces.Contains(fragment.Triangle)))
   throw new Exception("Native catenary reintroduced by structural fragments");
  if(args.Length == 2 && upperDelta == 0f && lowerDelta == 0f)
   ExportDetailed(Path.Combine(args[0], $"lift03-result-{lod}.bin"), combined);
  var positions=(Vector3[])source.Clone();
  Lift03RigidParts.Apply(lod,source,positions,delta);
  Lift03DeckBases.Apply(lod,source,positions,upperDelta,lowerDelta);
  Lift03DeckRailings.Apply(lod,source,positions,upperDelta,lowerDelta);
  Lift03CrossingMembers.Apply(lod,source,positions,upperDelta,lowerDelta);
  var parts=new[]{Lift03Frames.Complete(lod,source,positions,indices,delta),Lift03EndBeams.Complete(lod,source,positions,indices,delta),Lift03JoinedShells.Complete(lod,source,positions,indices,delta),Lift03TallSupports.Complete(lod,source,positions,indices,delta),Lift03SlopedSupports.Complete(lod,source,positions,indices,delta)};
  foreach(var vertex in combined.Positions) if(!float.IsFinite(vertex.x)||!float.IsFinite(vertex.y)||!float.IsFinite(vertex.z)) throw new Exception("Nonfinite output position");
  var byFace=combined.Fragments.ToDictionary(f=>f.Triangle);
  int edgeOffset=0,insideOffset=0;
  foreach(var part in parts)
  {
   foreach(var f in part.Fragments)
   {
    var target=byFace[f.Triangle];if(target.Indices.Length!=f.Indices.Length)throw new Exception("Topology changed");
    for(int i=0;i<f.Indices.Length;i++)
    {
     int old=f.Indices[i],actual=target.Indices[i];
     var expected=old<n?positions[old]:part.Positions[old];
     if(!combined.Positions[actual].Equals(expected))throw new Exception("Position remap failed");
     if(old>=n)
     {
      var a=part.Construction(old-n);var b=combined.Construction(actual-n);
      if(a.A!=b.A||a.B!=b.B||a.C!=b.C||a.WeightB!=b.WeightB||a.WeightC!=b.WeightC)throw new Exception("Attribute recipe remap failed");
     }
    }
   }
   edgeOffset+=part.AddedVertices.Length;insideOffset+=part.InteriorVertices.Length;
  }
  if(lod==1)
  {
   var supplement=Lift03FarSupports.CreateSupplement(source,indices,delta,upperDelta,lowerDelta);
   var selection=new bool[ic/3];Lift03SupportFaces.SelectMiddle(indices,selection);
   if(selection.Count(x=>!x)!=2406)throw new Exception("Support selection mismatch");
   Lift03FarDeckBases.Include(indices,selection);
   var expectedBases=(Vector3[])source.Clone(); Lift03DeckBases.Apply(1,source,expectedBases,upperDelta,lowerDelta);
   for(int i=0;i<n;i++) if(!expectedBases[i].Equals(source[i]) && !supplement.Positions[i].Equals(expectedBases[i])) throw new Exception("Far base width mismatch");
   foreach(var line in File.ReadLines(Path.Combine(args[0],"lift03-deck-ownership.txt")))
   {
    var fields=line.Split(' '); int vertex=int.Parse(fields[0]),sign=int.Parse(fields[2]);
    float width=fields[1]=="1"?upperDelta:lowerDelta;
    var actual=supplement.Positions[vertex];var original=source[vertex];
    if(actual.x!=original.x+sign*width||actual.y!=original.y||actual.z!=original.z)
     throw new Exception($"Measured deck ownership mismatch at {vertex}, upper={upperDelta}, lower={lowerDelta}");
   }
   foreach(var fragment in supplement.Fragments)
    if(selection[fragment.Triangle])throw new Exception("Supplement includes unrelated structure");
   for(int i=0;i<selection.Length;i++)
    if(selection[i]&&!supplement.RemoveTriangles[0][i])throw new Exception("Unrelated face retained");
   var triangles=supplement.RemoveTriangles[0].Count(x=>!x)+supplement.Fragments.Sum(f=>f.Indices.Length/3);
   Console.WriteLine($"PASS support supplement upper={upperDelta} lower={lowerDelta} structure={delta}: {triangles} result triangles, {supplement.ConstructedCount} constructed vertices; supports and two independently sized road bases selected");
  }
  var top=new bool[ic/3];Lift03RoadTops.RemoveDetailed(lod,indices,top); Lift03TrackRemoval.RemoveDetailed(lod,indices,top);
  if(combined.RemoveTriangles[0].Count(x=>x)!=top.Count(x=>x)+byFace.Count)throw new Exception("Removal mismatch");
  for(int i=0;i<n;i++)if(!source[i].Equals(before[i])||!combined.Positions[i].Equals(positions[i]))throw new Exception("Original vertex mismatch");
  if(!combined.SourceIndices[0].SequenceEqual(indices))throw new Exception("Original indices modified");
  Console.WriteLine($"PASS LOD{lod} upper={upperDelta} lower={lowerDelta} structure={delta}: {edgeOffset} edges/{insideOffset} interiors; every fragment position and attribute recipe preserved; separate upper/lower widths; road removals disjoint");
 }
}
// Diagnostic mesh only: original face identity accompanies each resulting triangle.
// Export source topology minus removed faces, then the production replacement fragments.
static void ExportDetailed(string path, MovableStructureModel.Plan plan)
{
 var faces = new List<(int A, int B, int C, int Source)>();
 for(int face = 0; face < plan.SourceIndices[0].Length / 3; face++)
  if(!plan.RemoveTriangles[0][face])
   faces.Add((plan.SourceIndices[0][face*3], plan.SourceIndices[0][face*3+1],
       plan.SourceIndices[0][face*3+2], face));
 foreach(var fragment in plan.Fragments)
  for(int i = 0; i < fragment.Indices.Length; i += 3)
   faces.Add((fragment.Indices[i], fragment.Indices[i+1], fragment.Indices[i+2], fragment.Triangle));
 using var output = new BinaryWriter(File.Create(path));
 output.Write(plan.Positions.Length); output.Write(faces.Count);
 foreach(var point in plan.Positions)
 { output.Write(point.x); output.Write(point.y); output.Write(point.z); }
 foreach(var face in faces)
 { output.Write(face.A); output.Write(face.B); output.Write(face.C); output.Write(face.Source); }
 Console.WriteLine($"EXPORTED LOD diagnostic: {path}, {plan.Positions.Length} vertices, {faces.Count} triangles");
}
namespace UnityEngine
{
 internal struct Vector3
 {
  public float x,y,z;
  public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
  public static Vector3 LerpUnclamped(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
 }
}




