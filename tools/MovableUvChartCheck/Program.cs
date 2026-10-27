using BridgeBuilder.Bridges;
using UnityEngine;

int checks=0;
void Check(bool result,string description)
{checks++;if(!result)throw new Exception(description);}
MovableUvChart Chart(params double[][] loops)
{
    var ints=new List<int>{loops.Length};var values=new List<double>();
    foreach(var loop in loops){ints.Add(values.Count);ints.Add(loop.Length/2);values.AddRange(loop);}
    return MovableUvChart.Read(ints.ToArray(),values.ToArray(),0);
}
var square=Chart(new double[]{0,0,4,0,4,4,0,4});
Check(square.ContainsTriangle(new(0,0),new(4,0),new(0,4)),"Authored boundaries must be included.");
Check(!square.ContainsTriangle(new(0,0),new(MathF.BitIncrement(4),0),new(0,4)),"No epsilon expansion beyond chart.");
Check(!square.ContainsTriangle(new(float.NaN,0),new(1,0),new(0,1)),"Nonfinite receiver rejected.");
var notch=Chart(new double[]{0,0,4,0,4,4,3,4,3,1,1,1,1,4,0,4});
Check(!notch.ContainsTriangle(new(.5f,3),new(3.5f,3),new(2,.5f)),"Corners inside do not cover concave notch.");
Check(notch.ContainsTriangle(new(0,0),new(4,0),new(2,1)),"Triangle below notch remains covered.");
var hole=Chart(new double[]{0,0,8,0,8,8,0,8},new double[]{2,2,3,2,3,3,2,3});
Check(!hole.ContainsTriangle(new(0,0),new(8,0),new(0,8)),"Enclosed hole missed by edge-only tests.");
Check(!hole.ContainsTriangle(new(0,2.5f),new(4,2.5f),new(0,2.5f)),"Degenerate receiver crossing hole rejected.");
Check(hole.ContainsTriangle(new(0,0),new(8,0),new(0,1)),"Unrelated hole does not reject valid receiver.");
var island=Chart(new double[]{0,0,8,0,8,8,0,8},new double[]{1,1,7,1,7,7,1,7},new double[]{2,2,6,2,6,6,2,6});
Check(island.ContainsTriangle(new(2,2),new(6,2),new(2,6)),"Nested even/odd island.");
Check(!island.ContainsTriangle(new(0,0),new(8,0),new(0,8)),"Nested island does not fill surrounding hole.");
Check(!Chart(new double[]{0,0,1,1}).ContainsTriangle(new(0,0),new(1,1),new(.5f,.5f)),"Two-point charts conservatively rejected.");
bool rejected=false;
try{Chart(new double[]{0,0,double.PositiveInfinity,0,1,1});}catch(ArgumentException){rejected=true;}
Check(rejected,"Nonfinite chart payload rejected.");
rejected=false;
try{MovableUvChart.Read(new[]{1,int.MaxValue,3},Array.Empty<double>(),0);}catch(ArgumentException){rejected=true;}
Check(rejected,"Malformed coordinate range rejected without overflow.");

// A receiver can exceed its old single-face sampler while remaining wholly in
// the same authored chart. The production Unit must now bind the original atlas.
var unit=new MovableStructurePacking.Unit{Triangle=true,A=new(.5f,.5f),B=new(1,.5f),C=new(.5f,1)};
unit.Points.AddRange(new[]{new Vector2(0,0),new Vector2(2,0),new Vector2(0,2)});
Check(!unit.CanUseSource,"Old single-triangle rejection control.");
unit.Charts=new[]{square};
Check(unit.CanUseSource,"True-chart source reuse is connected to Unit.");
unit.Charts=new[]{Chart(new double[]{.5,.5,1,.5,1,1,.5,1})};
Check(!unit.CanUseSource,"Out-of-chart receiver cannot borrow adjacent pixels.");

// Fragment phase conditioning is only for a repeat page. Select must use Exact
// and final atlas Float32 values for native reuse, not the shifted Points array.
unit=new MovableStructurePacking.Unit{Triangle=true,A=new(0,0),B=new(1,0),C=new(0,1),Charts=new[]{square}};
var original=new[]{new[]{2.0,2.0},new[]{3.0,2.0},new[]{2.0,3.0}};
var phase=new MovableParentUv.Result(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1)},original,2,2);
var selected=phase.Select(unit);unit.Points.AddRange(selected);
Check(unit.CanUseSource && selected[0].Equals(new Vector2(2,2)),"Fragment recovers original phase before chart lookup.");
Check(unit.SourceUv(selected[0]).Equals(new Vector2(2,2)),"Output native UV retains exact parent phase.");
unit=new MovableStructurePacking.Unit{Triangle=true,A=new(0,0),B=new(1,0),C=new(0,1),Charts=new[]{Chart(new double[]{0,0,1,0,1,1,0,1})}};
selected=phase.Select(unit);unit.Points.AddRange(selected);
Check(!unit.CanUseSource && unit.RepeatOnly && selected[0].Equals(Vector2.zero),"Rejected native phase preserves existing repeat output.");
Console.WriteLine($"PASS {checks} production chart containment, native binding and fragment-phase checks.");
