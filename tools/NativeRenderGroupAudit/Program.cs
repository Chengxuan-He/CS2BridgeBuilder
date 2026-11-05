using System.Reflection;
using System.Text.Json;
using BridgeBuilder.Bridges;

if (args.Length < 2) throw new ArgumentException("Usage: NativeRenderGroupAudit input.json report.json [frozen.dll]");
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
var input = JsonSerializer.Deserialize<Input>(File.ReadAllText(args[0]), options) ?? throw new Exception("No producer input");
if (input.Levels.Length == 0) throw new Exception("No LODs");
var totals = input.Levels.Select(x => x.Slots.Length).ToArray();
var starts = input.Levels.Select(x => x.RoadStart).ToArray();
var lanes = input.Levels.Select(x => x.LaneStart).ToArray();
if (input.Levels.Any(x => x.RoadStart < 0 || x.LaneStart < x.RoadStart || x.LaneStart > x.Slots.Length)) throw new Exception("Invalid producer boundaries");
Type planner = args.Length > 2 ? Assembly.LoadFrom(Path.GetFullPath(args[2])).GetType("BridgeBuilder.Bridges.MovableMaterialSlots", true)! : typeof(MovableMaterialSlots);
object? Call(string name, params object[] values) => planner.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, values);
var groups = (int[][][]?)Call("RoadRenderGroups", totals, starts);
string route = "complete-structure-road";
var sharedSurfaces = new Dictionary<string, int>(StringComparer.Ordinal);
var actualSurfaceKeys = input.Levels.Select(level => level.Slots.Select(slot => {
    if (string.IsNullOrEmpty(slot.Surface)) throw new Exception("Actual Surface identity missing");
    if (!sharedSurfaces.TryGetValue(slot.Surface, out var key)) sharedSurfaces.Add(slot.Surface, key=sharedSurfaces.Count);
    return key;
}).ToArray()).ToArray();
// Frozen 53 lacks this new entry; preserve its original selection for comparison.
if(groups == null && planner.GetMethod("StructureRoadRenderGroups", BindingFlags.NonPublic | BindingFlags.Static) != null)
{
    groups=(int[][][]?)Call("StructureRoadRenderGroups",actualSurfaceKeys,starts);
    if(groups!=null)route="protected-structure-road-identities";
}
if (groups == null && input.StableSupplementKeys)
{
    var roleIds = new Dictionary<string, int>(StringComparer.Ordinal) { ["structure"] = 0, ["road"] = int.MaxValue-1, ["lanes"] = int.MaxValue };
    var surfaceIds = new Dictionary<string, int>(StringComparer.Ordinal);
    int[][] roles = input.Levels.Select(level => level.Slots.Select(slot => {
        if (string.IsNullOrEmpty(slot.Role)) throw new Exception("Explicit stable role missing");
        if (!roleIds.TryGetValue(slot.Role, out var id)) roleIds.Add(slot.Role, id = roleIds.Count);
        return id;
    }).ToArray()).ToArray();
    int[][] surfaces = input.Levels.Select(level => level.Slots.Select(slot => {
        if (string.IsNullOrEmpty(slot.Surface)) throw new Exception("Actual Surface identity missing");
        if (!surfaceIds.TryGetValue(slot.Surface, out var id)) surfaceIds.Add(slot.Surface, id=surfaceIds.Count);
        return id;
    }).ToArray()).ToArray();
    groups = (int[][][]?)Call("StableRoleGroups", roles, surfaces); route = "stable-source-roles";
}
if (groups == null)
{
    int minimum = (int)Call("MinimumLodSlots", totals)!;
    if (totals.Any(n => n < minimum)) {
        File.WriteAllText(args[1], JsonSerializer.Serialize(new { input.Style, input.Scope, totals, minimum,
            status="requires-post-balancing-native-slot-evidence", reason="Do not guess physical coverage after native triangle-stream slicing." }, options));
        Console.WriteLine("UNRESOLVED native balancing requires post-slice producer evidence"); return 2;
    }
    groups=(int[][][])Call("RenderGroups",totals,lanes)!; route="legacy-count-groups";
}
for(int lod=0;lod<totals.Length;lod++)
    if(!groups.SelectMany(g=>g[lod]).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,totals[lod]))) throw new Exception("Slot loss/duplication");
if(groups.Any(g=>g.Sum(x=>x.Length)>16 || g.Any(x=>x.Length==0)))throw new Exception("Native capacity/empty LOD violation");
var parts=input.Levels.SelectMany(l=>l.Slots).SelectMany(s=>s.PartIds).Distinct().OrderBy(x=>x).ToArray();
var checks=new List<object>(); int failures=0;
foreach(string part in parts)
{
    var present = Enumerable.Range(0,groups.Length).Select(group=>Enumerable.Range(0,totals.Length)
        .Select(lod=>groups[group][lod].Any(slot=>input.Levels[lod].Slots[slot].PartIds.Contains(part))).ToArray()).ToArray();
    var expected=input.ExpectedPresenceByLod.TryGetValue(part,out var declared) ? declared : Enumerable.Repeat(true,totals.Length).ToArray();
    if(expected.Length!=totals.Length)throw new Exception("Expected physical coverage needs one entry per LOD");
    bool allLods=Enumerable.Range(0,totals.Length).All(lod=>present.Any(row=>row[lod])==expected[lod]);
    // Authored low-detail omissions are not missing-face bugs. In required LODs,
    // preserve each group's membership; omitted levels must actually be absent.
    bool stable=present.All(row=>row.Where((_,lod)=>expected[lod]).Distinct().Count()<=1);
    if(!allLods || !stable)failures++;
    checks.Add(new {part, matchesExpectedLodPresence=allLods, expectedPresenceByLod=expected, stableRendererMembership=stable, groupLodPresence=present});
}
var report = new {input.Style,input.Scope,input.Provenance, route, totals, groups,
    budgets=groups.Select(g=>g.Sum(x=>x.Length)).ToArray(), exactSlotCoverage=true,
    checkedAtomicParts=parts.Length,unstableOrMissingParts=failures, parts=checks,
    limit="Atomic physical part IDs must come from source-face coverage, not material names. Stable labels alone do not prove geometry, alpha, motion or live game rendering."};
File.WriteAllText(args[1],JsonSerializer.Serialize(report,options));
Console.WriteLine($"{(failures!=0 ? "FAIL" : parts.Length==0 ? "UNRESOLVED" : "PASS")} {input.Style}: route={route}, slots={string.Join('/',totals)}, budgets={string.Join('/',report.budgets)}, measured parts={parts.Length}, unstable/missing={failures}");
return failures==0 ? (parts.Length==0 ? 2 : 0) : 1;

sealed class Input { public string Style {get;set;}=""; public string Scope {get;set;}=""; public string Provenance {get;set;}=""; public bool StableSupplementKeys {get;set;} public Dictionary<string,bool[]> ExpectedPresenceByLod {get;set;}=new(); public Level[] Levels {get;set;}=Array.Empty<Level>(); }
sealed class Level { public int RoadStart {get;set;} public int LaneStart {get;set;} public Slot[] Slots {get;set;}=Array.Empty<Slot>(); }
sealed class Slot { public string Surface {get;set;}=""; public string Role {get;set;}=""; public int Triangles {get;set;} public string[] PartIds {get;set;}=Array.Empty<string>(); }
