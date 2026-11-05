using BridgeBuilder.Bridges;

// Actual Draw03 owner b82088213: structural slots 2/2/1, selected road 5/5/5.
// Its native thresholds select main middle + appearance far for viewLOD 126..131.
var draw03Totals = new[] { 7, 7, 6 };
var roadStarts = new[] { 2, 2, 1 };
var stable = MovableMaterialSlots.RoadRenderGroups(draw03Totals, roadStarts)
    ?? throw new Exception("Actual Draw03 semantic plan was not selected.");
if (!stable.Select(g => g.Sum(slots => slots.Length)).SequenceEqual(new[] { 5, 15 }))
    throw new Exception("Actual Draw03 native group budgets changed.");
for (int lod=0;lod<3;lod++)
    if (!stable.SelectMany(g=>g[lod]).SequenceEqual(Enumerable.Range(0,draw03Totals[lod])))
        throw new Exception("Semantic grouping lost or duplicated an original slot.");
var sidewalkSlots = new[] { 3, 3, 2 };
var curbSlots = new[] { 2, 2, 1 };
for(int main=0;main<3;main++) for(int appearance=0;appearance<3;appearance++)
{
    foreach(var part in new[]{sidewalkSlots,curbSlots})
        if ((stable[0][main].Contains(part[main])?1:0)+(stable[1][appearance].Contains(part[appearance])?1:0)!=1)
            throw new Exception("Independent native LOD choices lost or duplicated a road part.");
}
var old = MovableMaterialSlots.RenderGroups(draw03Totals,draw03Totals);
if (old[0][1].Contains(sidewalkSlots[1]) || old[1][2].Contains(sidewalkSlots[2]))
    throw new Exception("The actual old middle/far absence counterexample no longer reproduces.");
// Only feasible complete semantic plans supersede legacy planning. No new failure
// or fabricated empty mesh is introduced for other bridge graphs.
foreach(var input in new[]{
    (new[]{5,5,5},new[]{2,2,1}), (new[]{11,11,11},new[]{4,4,4}),
    (new[]{7,7,6},new[]{0,0,0}), (new[]{7,7,6},new[]{7,7,6}),
    (new[]{7,7,6},new[]{2,2,0})})
    if(MovableMaterialSlots.RoadRenderGroups(input.Item1,input.Item2)!=null)
        throw new Exception("An incomplete or overflowing semantic split was selected.");
Console.WriteLine("PASS actual Draw03 7/7/6 -> stable 5/15 batches; all nine independent LOD pairs retain sidewalk and curb exactly once; old absence reproduced; legacy paths unchanged.");

if (MovableMaterialSlots.RenderGroups(new[] { 6, 6, 5 }, new[] { 4, 4, 3 }).Length != 2 ||
    MovableMaterialSlots.RenderGroups(new[] { 6, 5, 5 }, new[] { 4, 4, 3 }).Length != 1 ||
    MovableMaterialSlots.RenderGroups(new[] { 12, 10, 10 }, new[] { 6, 5, 5 }).Length != 2)
    throw new Exception("Native LOD batch budget planning failed.");
foreach (var input in new[] {
    (new[] { 12, 12, 10 }, new[] { 6, 6, 5 }),
    (new[] { 12, 12, 10 }, new[] { 4, 4, 3 }),
    (new[] { 35, 28, 16 }, new[] { 35, 28, 16 }),
    (new[] { 64, 64, 64 }, new[] { 0, 0, 0 }) })
{
    var groups = MovableMaterialSlots.RenderGroups(input.Item1, input.Item2);
    foreach (var group in groups)
        if (group.Length != input.Item1.Length || group.Any(slots => slots.Length == 0) ||
            group.Sum(slots => slots.Length) > 16)
            throw new Exception("Incomplete or overflowing native group.");
    for (int level = 0; level < input.Item1.Length; level++)
        if (!groups.SelectMany(group => group[level]).SequenceEqual(Enumerable.Range(0, input.Item1[level])))
            throw new Exception("A material slot was lost, duplicated, or reordered across groups.");
}
try { MovableMaterialSlots.RenderGroups(new[] { 8, 8, 1 }, new[] { 4, 4, 1 }); throw new Exception("An unverified empty-LOD group was fabricated."); }
catch (InvalidOperationException) { }
Console.WriteLine("PASS 16/32 boundaries, 34/79/192-batch complete grouping, exact per-LOD coverage and no fabricated empty LODs.");

foreach(var totals in new[]{new[]{48,24,2},new[]{256,8,1},new[]{1024,16,4},Enumerable.Repeat(1,16).Append(1).Take(16).Select((n,i)=>i==0?33:n).ToArray()})
{
    int minimum=MovableMaterialSlots.MinimumLodSlots(totals);
    var balanced=totals.Select(n=>Math.Max(n,minimum)).ToArray();
    var groups=MovableMaterialSlots.RenderGroups(balanced,new int[totals.Length]);
    if(groups.Any(g=>g.Sum(slots=>slots.Length)>16 || g.Any(slots=>slots.Length==0)))
        throw new Exception("Unequal near/far native batch planning failed.");
    for(int lod=0;lod<totals.Length;lod++)
        if(!groups.SelectMany(g=>g[lod]).SequenceEqual(Enumerable.Range(0,balanced[lod])))
            throw new Exception("Balanced native groups changed slot coverage.");
}
Console.WriteLine("PASS unequal 48/24/2, 256/8/1, 1024/16/4 and sixteen-level grouping.");

// Deliberately value-equal but separately owned descriptors must not coalesce.
var cable = new Surface("cable");
var rail = new Surface("rail");
var otherCable = new Surface("cable");
var surfaces = new[] { cable, rail, cable, cable, rail, otherCable, cable };
var map = MovableMaterialSlots.Canonical(surfaces);
if (!map.SequenceEqual(new[] { 0, 1, 0, 0, 1, 5, 0 }))
    throw new Exception("Canonical slots must use exact reference identity and stable first occurrence.");
// Different decks/bones share a material but keep their independent membership.
var faces = Enumerable.Range(0, 70).Select(i =>
    new Face(i, i % surfaces.Length, i % 2, i % 10)).ToArray();
var batches = faces.GroupBy(face => map[face.Slot]).OrderBy(group => group.Key).ToArray();
if (batches.Length != 3 || batches.SelectMany(group => group).OrderBy(face => face.Id)
        .Where((face, i) => face != faces[i]).Any())
    throw new Exception("Batching lost triangle, bone or occurrence identity.");
foreach (var batch in batches)
    if (batch.Any(face => !ReferenceEquals(surfaces[face.Slot], surfaces[batch.Key])))
        throw new Exception("A batch contains a different descriptor.");
// Native batch cap is a graph-wide limit. Repetition must not multiply it.
var repeated = Enumerable.Repeat(cable, 64).ToArray();
if (MovableMaterialSlots.Canonical(repeated).Distinct().Count() != 1)
    throw new Exception("Repeated cable tiles still require separate material batches.");
if (MovableMaterialSlots.Canonical(Array.Empty<Surface>()).Length != 0)
    throw new Exception("Empty input was not retained.");
try
{
    MovableMaterialSlots.Canonical(new Surface[] { cable, null! });
    throw new Exception("Null surface accepted.");
}
catch (ArgumentException) { }
Console.WriteLine("PASS stable reference-only surface grouping; 70 independent face/bone/occurrence records preserved; 64 repeated cable slots use one batch; empty/null cases.");




// Actual candidate52 owner: 16 measured rail supplements and the original main mesh slots.
var actualRoles52 = new int[][] { new[]{0,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,2147483646,2147483646,2147483646,2147483646,2147483646,2147483646,2147483646,2147483646},new[]{0,0,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,2147483646,2147483646,2147483646,2147483646,2147483646,2147483646,2147483646,2147483646},new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,2147483646,2147483646,2147483646,2147483646} };
var actualSurfaces52 = new int[][] { new[]{0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,0,3,4,5,6,7,8},new[]{0,9,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,0,3,4,5,6,7,8},new[]{10,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,10,7,8} };
var stable52 = MovableMaterialSlots.StableRoleGroups(actualRoles52, actualSurfaces52) ?? throw new Exception("Actual Draw02 roles fell back.");
for(int lod=0;lod<3;lod++)
 if(!stable52.SelectMany(g=>g[lod]).OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,actualRoles52[lod].Length))) throw new Exception("Actual slot lost/duplicated");
if(stable52.Any(g=>g.Sum(x=>x.Length)>16 || g.Any(x=>x.Length==0))) throw new Exception("Invalid native capacity");
foreach(int role in actualRoles52.SelectMany(x=>x).Distinct()) {
 foreach(int key in actualSurfaces52.SelectMany(x=>x).Distinct()) {
  var owners=new HashSet<int>();
  for(int lod=0;lod<3;lod++) for(int g=0;g<stable52.Length;g++)
   if(stable52[g][lod].Any(i=>actualRoles52[lod][i]==role && actualSurfaces52[lod][i]==key)) owners.Add(g);
  if(owners.Count>1) throw new Exception("Role/material migrated between native renderers");
 }
}
Console.WriteLine("PASS actual Draw02 26/27/21 slots: stable semantic groups="+stable52.Length+" budgets="+string.Join(",",stable52.Select(g=>g.Sum(x=>x.Length)))+"; all slots once, all roles/materials stable across independent LOD choices, original viewLOD132 counterexample removed.");

// Every independent native LOD selection, not only synchronous near/middle/far.
int combinations = (int)Math.Pow(3, stable52.Length);
var commonRoadKeys = actualSurfaces52[0].Where((key, slot) => actualRoles52[0][slot] == int.MaxValue-1)
    .Distinct().Where(key => Enumerable.Range(1,2).All(lod => actualSurfaces52[lod].Where((_,slot)=>actualRoles52[lod][slot]==int.MaxValue-1).Contains(key))).ToArray();
for(int combination=0;combination<combinations;combination++) {
 var selection=new int[stable52.Length]; int digits=combination;
 for(int group=0;group<selection.Length;group++){selection[group]=digits%3;digits/=3;}
 foreach(int key in commonRoadKeys) {
  int count=0;
  for(int group=0;group<selection.Length;group++) {
   int lod=selection[group]; count+=stable52[group][lod].Count(slot=>actualRoles52[lod][slot]==int.MaxValue-1 && actualSurfaces52[lod][slot]==key);
  }
  if(count!=1)throw new Exception("Independent LODs omitted or duplicated measured road batch");
 }
 for(int role=1;role<=16;role++) {
  int count=0;
  for(int group=0;group<selection.Length;group++)count+=stable52[group][selection[group]].Count(slot=>actualRoles52[selection[group]][slot]==role);
  if(count!=1)throw new Exception("Rail occurrence disappeared/duplicated across LODs");
 }
}
if(MovableMaterialSlots.StableRoleGroups(new[]{new[]{0,1},new[]{0}},new[]{new[]{1,2},new[]{1}})!=null)
 throw new Exception("Missing role invented a native LOD");
Console.WriteLine("PASS "+combinations+" independent LOD combinations: each measured common road batch and all 16 physical rail occurrences exactly once; no fabricated missing role.");

// Measured Lift01/Lift03 structure is 2/2/2: near/middle main+glass,
// far main+middle-source supplement. Road slots below are controlled capacity
// inputs, not a claim that a live saved owner has these particular road slots.
foreach (var structureCounts in new[] { new[] {2,2,2}, new[] {2,2,1} })
{
 var keys = structureCounts.Select((n,lod)=>Enumerable.Range(0,n).Select(i=>100+lod*10+i)
     .Concat(Enumerable.Range(0,8)).ToArray()).ToArray();
 var grouped = MovableMaterialSlots.StructureRoadRenderGroups(keys, structureCounts)
     ?? throw new Exception("Complete measured structure prefix lost protection when road exceeds 16 batches");
 for(int lod=0;lod<3;lod++)
  if(!grouped.SelectMany(g=>g[lod]).OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,keys[lod].Length)))
   throw new Exception("Structure/road plan lost or duplicated slots");
 int structureGroup=Array.FindIndex(grouped,g=>g[0].Contains(0));
 for(int lod=0;lod<3;lod++)
  if(!Enumerable.Range(0,structureCounts[lod]).All(grouped[structureGroup][lod].Contains))
   throw new Exception("Near main / far supplement migrated between renderers");
 if(grouped.Any(g=>g.Sum(x=>x.Length)>16 || g.Any(x=>x.Length==0)))throw new Exception("Invalid protected structure capacity");
 foreach(int key in Enumerable.Range(0,8))
  if(Enumerable.Range(0,3).Select(lod=>Array.FindIndex(grouped,g=>g[lod].Any(slot=>keys[lod][slot]==key))).Distinct().Count()!=1)
   throw new Exception("Controlled road identity migrated between renderers");
}
if(MovableMaterialSlots.StructureRoadRenderGroups(actualSurfaces52,new[]{18,19,17})!=null)
 throw new Exception("Oversized Draw02 structure bypassed its explicit occurrence roles");
// Capacity counterexample using measured structural ownership and controlled
// repeated road occurrences: near/middle tower is slot 0, far tower is slot 1.
// No claim is made that these road counts came from a saved Lift owner.
var pressureKeys = new[]{12,12,3}.Select((n,lod)=>new[]{100+lod*2,101+lod*2}
 .Concat(Enumerable.Range(0,n).Select(i=>i%3)).ToArray()).ToArray();
var pressureTotals=pressureKeys.Select(row=>row.Length).ToArray();
var legacyPressure=MovableMaterialSlots.RenderGroups(pressureTotals,pressureTotals);
int[] towerSlots={0,0,1};
if(Enumerable.Range(0,3).Select(lod=>Array.FindIndex(legacyPressure,g=>g[lod].Contains(towerSlots[lod]))).Distinct().Count()==1)
 throw new Exception("Controlled old structural migration counterexample did not reproduce");
var protectedPressure=MovableMaterialSlots.StructureRoadRenderGroups(pressureKeys,new[]{2,2,2})!;
int pressureCombinations=(int)Math.Pow(3,protectedPressure.Length);
for(int c=0;c<pressureCombinations;c++) {
 int digits=c,found=0;
 foreach(var group in protectedPressure){int lod=digits%3;digits/=3;if(group[lod].Contains(towerSlots[lod]))found++;}
 if(found!=1)throw new Exception("Independent native LODs lost/duplicated tower main-to-supplement coverage");
}
Console.WriteLine("PASS old 14/14/5 controlled-road structural migration reproduced; protected structure retains tower/end-lip ownership in all "+pressureCombinations+" independent LOD combinations.");
Console.WriteLine("PASS measured 2/2/2 and 2/2/1 structural prefixes with controlled 24-road-batch pressure: complete structural coverage stays together; road identities stable; Draw02 explicit-role route retained.");

if(args.Length!=0) {
 var assembly=System.Reflection.Assembly.LoadFrom(Path.GetFullPath(args[0]));
 var type=assembly.GetType("BridgeBuilder.Bridges.MovableMaterialSlots",true)!;
 var method=type.GetMethod("StableRoleGroups",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
 var actual=(int[][][])method.Invoke(null,new object[]{actualRoles52,actualSurfaces52})!;
 if(System.Text.Json.JsonSerializer.Serialize(actual)!=System.Text.Json.JsonSerializer.Serialize(stable52))
  throw new Exception("Frozen DLL semantic planner differs from tested real-owner plan");
 var structuralMethod=type.GetMethod("StructureRoadRenderGroups",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
 if(structuralMethod!=null) {
  var actualProtected=(int[][][])structuralMethod.Invoke(null,new object[]{pressureKeys,new[]{2,2,2}})!;
  if(System.Text.Json.JsonSerializer.Serialize(actualProtected)!=System.Text.Json.JsonSerializer.Serialize(protectedPressure))
   throw new Exception("Frozen DLL structural protection differs from validated capacity counterexample");
  Console.WriteLine("PASS frozen DLL actual protected-structure method matches all 27 independent-LOD checks.");
 }
 if(args.Length>1)File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new {roles=actualRoles52,surfaces=actualSurfaces52,groups=actual,independentCombinations=combinations}));
 Console.WriteLine("PASS frozen production DLL actual-owner planner matches all validated assignments.");
}



// Unused source slots do not participate in emitted object meshes or canonical IDs.
var used = new object();
var sparse = new object[] { null!, used, used, new object() };
if (!MovableMaterialSlots.Canonical(sparse,new[]{2,1,2}).SequenceEqual(new[]{-1,1,1,-1}))
    throw new Exception("Sparse material references were not preserved.");
foreach(var invalid in new[]{new[]{0},new[]{-1},new[]{4}}) {
    bool rejected=false;try { MovableMaterialSlots.Canonical(sparse,invalid); } catch(ArgumentException) { rejected=true; }
    if(!rejected)throw new Exception("An active invalid material slot was accepted.");
}
Console.WriteLine("PASS unused null/source slots excluded; repeated active references canonicalized; active null and invalid indices still fail.");

record Surface(string Name);
record Face(int Id, int Slot, int Bone, int Occurrence);
