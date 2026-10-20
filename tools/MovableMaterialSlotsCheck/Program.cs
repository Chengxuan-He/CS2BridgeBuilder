using BridgeBuilder.Bridges;

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
