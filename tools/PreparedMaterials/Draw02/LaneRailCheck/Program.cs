using BridgeBuilder.Bridges;
using UnityEngine;
using System.Text.Json;
var root=Path.GetFullPath(args[0]);
var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"artifacts/material-remap/Draw02/lane-rails/rail-runtime-records.json")));
int cases=0,faces=0;
for(int lod=0;lod<3;lod++)
{
 int sourceLod=Draw02LaneRails.SourceLod(lod);
 using var r=new BinaryReader(File.OpenRead(Path.Combine(root,$"artifacts/material-remap/Draw02/lane-rails/lod{sourceLod}-fixture.bin")));
 var p=new Vector3[r.ReadInt32()];var ix=new int[r.ReadInt32()];
 for(int i=0;i<p.Length;i++)p[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
 for(int i=0;i<ix.Length;i++)ix[i]=r.ReadInt32();
 for(int local=0;local<6;local++)foreach(float x in new[]{-14.25f,-3f,0f,6.75f,17.125f})
 {
  int part=sourceLod*6+local;var evidence=json.RootElement[part];
  var selected=evidence.GetProperty("faces").EnumerateArray().Select(x=>x.GetInt32()).ToHashSet();
  var plan=Draw02LaneRails.Create(part,x,p,ix);float anchor=evidence.GetProperty("anchorX").GetSingle();
  var members=selected.SelectMany(t=>new[]{ix[t*3],ix[t*3+1],ix[t*3+2]}).ToHashSet();
  for(int t=0;t<ix.Length/3;t++)if(plan.RemoveTriangles[0][t]==selected.Contains(t))throw new Exception("Wrong face membership");
  for(int v=0;v<p.Length;v++)
  {
   var q=plan.Positions[v];float expected=members.Contains(v)?p[v].x+(x-anchor):p[v].x;
   if(BitConverter.SingleToInt32Bits(q.x)!=BitConverter.SingleToInt32Bits(expected)||q.y!=p[v].y||q.z!=p[v].z)throw new Exception("Non-rigid rail or unrelated position edit");
  }
  if(local>=2) {
   bool carOnLeft=local>=4;
   if(Draw02LaneRails.Part(lod,false,carOnLeft,(local&1)!=0)!=part)throw new Exception("Rail side chosen by global X instead of contact roles");
   float inner=carOnLeft?members.Min(v=>plan.Positions[v].x):members.Max(v=>plan.Positions[v].x);
   if(inner!=x || members.Any(v=>carOnLeft?plan.Positions[v].x<x:plan.Positions[v].x>x))throw new Exception("High rail intrudes into car lane");
  }
  if(!plan.SourceIndices[0].SequenceEqual(ix))throw new Exception("Index order changed");
  cases++;faces+=selected.Count;
 }
}
Console.WriteLine($"PASS {cases} source-linked native rail plans, {faces} selected source faces; exact member X translation; unchanged Y/Z/index order. Native raw-channel upload remains separate.");
namespace UnityEngine
{
 internal struct Vector3 {internal float x,y,z;internal Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 internal struct Vector2 {internal float x,y;internal Vector2(float a,float b){x=a;y=b;}}
}
namespace BridgeBuilder.Bridges
{

 internal static class MovableStructureModel
 {
  internal sealed class Plan
  {
   internal readonly Vector3[] Positions;internal readonly int[][] SourceIndices;internal readonly bool[][] RemoveTriangles;
   internal Plan(Vector3[] source,Vector3[] positions,int[][] indices,bool[][] removed){Positions=positions;SourceIndices=indices;RemoveTriangles=removed;}
  }
 }
}
