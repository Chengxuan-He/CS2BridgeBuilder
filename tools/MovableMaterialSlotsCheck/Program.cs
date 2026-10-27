using BridgeBuilder.Bridges;

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

record Surface(string Name);
record Face(int Id, int Slot, int Bone, int Occurrence);
