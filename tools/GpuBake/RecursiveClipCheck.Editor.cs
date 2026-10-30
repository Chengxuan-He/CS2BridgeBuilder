using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using G=BridgeBuilder.Bridges.MovableRoadGeometry;
public static class RecursiveClipCheck
{
 public static void Run(){try{
 string root=@"C:\Users\admin\Documents\Codex\CS2BridgeBuilder\artifacts\material-remap\Draw02\fixed-car";
 float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
 Vector3 P(string s){var a=s.Split(',').Select(F).ToArray();return new Vector3(a[0],a[1],a[2]);}
 var rows=File.ReadAllLines(Path.Combine(root,"candidate-area-partition.csv")).Select(s=>s.Split(',')).ToArray();
 G.Partition Build(int index){var r=rows[index];int bone=int.Parse(r[3]);if(bone>=0)return new G.Partition(bone);var m=new Matrix4x4();for(int i=0;i<16;i++)m[i/4,i%4]=F(r[i+4]);return new G.Partition(m,Build(int.Parse(r[1])),Build(int.Parse(r[2])));}
 G.Vertex V(Vector3 p)=>new(p,Vector3.up,new Vector4(1,0,0,-1),new Vector4(p.x,p.z,7,9),Color.gray);
 var points=File.ReadAllLines(Path.Combine(root,"candidate-area-counterexample.txt"))[0].Split(';').Select(P).ToArray();
 var input=new[]{new G.Triangle(V(points[0]),V(points[1]),V(points[2]),0,19,37)};
 if(!G.TryBind(input,Build(0),rows.Max(r=>int.Parse(r[3]))+1,1,out var output,out var error))throw new Exception(error);
 string Bits(Vector3 p)=>string.Join(",",new[]{p.x,p.y,p.z}.Select(f=>BitConverter.SingleToInt32Bits(f).ToString("X8")));
 string Signature(IEnumerable<Vector3> ps)=>string.Join(";",ps.Select(Bits).OrderBy(x=>x,StringComparer.Ordinal));
 var expected=File.ReadAllLines(Path.Combine(root,"candidate-area-double-carry.txt")).Skip(1).Select(s=>Signature(s.Split(';').Select(P))).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
 var actual=output.Select(t=>Signature(new[]{t.Geometry.A.Position,t.Geometry.B.Position,t.Geometry.C.Position})).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
 if(!actual.SequenceEqual(expected))throw new Exception("Actual production clip does not match single-rounding witness bits.");
 double Area(Vector3 a,Vector3 b,Vector3 c)=>((double)b.x-a.x)*((double)c.z-a.z)-((double)b.z-a.z)*((double)c.x-a.x);
 double original=Area(points[0],points[1],points[2]),area=0;
 foreach(var t in output){var g=t.Geometry;var a=Area(g.A.Position,g.B.Position,g.C.Position);if(a*original<=0)throw new Exception("Reversed or degenerate output");area+=a;
 if(g.Material!=0||g.Occurrence!=19||g.SourceTriangle!=37)throw new Exception("Provenance lost");
 foreach(var v in new[]{g.A,g.B,g.C})if(v.Normal!=Vector3.up||v.Tangent!=new Vector4(1,0,0,-1)||v.Color!=Color.gray||v.UV.z!=7||v.UV.w!=9||Math.Abs(v.UV.x-v.Position.x)>0.00002||Math.Abs(v.UV.y-v.Position.z)>0.00002)throw new Exception("Original attribute interpolation changed");}
 if(Math.Abs(area-original)/Math.Abs(original)>1e-4)throw new Exception("Original area acceptance bound failed");
 Debug.Log($"PASS actual Draw02 recursive clip: {output.Length} triangles; exact double-carry position bits; relative area error {Math.Abs(area-original)/Math.Abs(original):R}; original attributes and provenance retained.");
 CombinedPartitionCheck.Run();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
