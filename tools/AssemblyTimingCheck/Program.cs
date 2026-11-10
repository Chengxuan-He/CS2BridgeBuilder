using System.Globalization;
using System.Text.RegularExpressions;
using BridgeBuilder.Bridges;
static double Value(string line,string key)=>double.Parse(Regex.Match(line,@"(?:^|; )"+key+@"=(-?[0-9]+(?:\.[0-9]+)?)").Groups[1].Value,CultureInfo.InvariantCulture);
void Need(bool yes,string message){if(!yes)throw new Exception(message);}
foreach(bool success in new[]{true,false})
{
 Mod.Log.Rows.Clear();var t=new MovableAssemblyTiming();
 try
 {
  t.Stage("laneMaterials",0);t.Count("laneMaterialMisses");
  t.Count("laneInputTriangles",42);t.Stage("laneBind");Thread.SpinWait(10000);
  t.Count("laneOutputTriangles",45);t.Stage("laneMeshes");t.Encoding(100);
  t.Stage("laneMaterials",1);t.Count("laneMaterialHits");
  if(!success)throw new InvalidOperationException("simulated pipeline failure");
 }
 catch(InvalidOperationException){}
 finally{t.Stage("dispose",-1);t.Finish("test","Draw01",success);t.Finish("test","Draw01",success);}
 var rows=Mod.Log.Rows.Where(x=>x.StartsWith("Assembly detail:")).ToArray();var total=Mod.Log.Rows.Single(x=>x.StartsWith("Assembly detail total:"));
 Need(rows.Length==3,"Expected two LODs and graph bucket");
 Need(Math.Abs(Value(total,"totalMs")-Value(total,"stageSumMs"))<.001,"Exclusive total mismatch");
 Need(Value(total,"unaccountedMs")==0,"Unaccounted ticks");
 Need(Math.Abs(rows.Sum(x=>Value(x,"rowTotalMs"))-Value(total,"stageSumMs"))<.004,"Row sum mismatch");
 Need(rows.Any(x=>x.Contains("laneInputTriangles=42")&&x.Contains("laneOutputTriangles=45")),"Lost counts");
 Need(rows.Any(x=>x.Contains("laneMaterialHits=1")),"Lost cache hit");
 Need(total.Contains("success="+success),"Wrong success");
 Need(Value(total,"threadAllocatedBytes")>=0,"Allocation counter unavailable on test runtime");
 Console.WriteLine("PASS exclusive timings, LOD counts, cache counts, encoding subset, duplicate finish, allocation and "+(success?"success":"failure/finally"));
}
Mod.Log.Throw=true;new MovableAssemblyTiming().Finish("test","Draw01",false);Console.WriteLine("PASS diagnostic sink cannot change construction outcome");
internal static class Mod {internal static readonly Sink Log=new();internal sealed class Sink {internal readonly List<string> Rows=new();internal bool Throw;internal void Info(string s){if(Throw)throw new Exception("sink");Rows.Add(s);}}}
