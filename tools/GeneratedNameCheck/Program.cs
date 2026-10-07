using BridgeBuilder.Bridges;
using CS2Mods.Shared.Infrastructure;

const string owner = "b8a73af23-ea53-4b61-8e0b-12bae69355e2";
var inputs = new[] { new string('x', 150) + "-" + owner, new string('x', 70) + "-" + owner,
    owner + "-" + new string('x', 150), "Suspension-28-" + owner };
foreach (var input in inputs)
{
    var expected = new[] { " Piece", " Mesh", " Mesh 1", " Mesh 2", " LOD1", " LOD2" }
        .Select(part => input + part).ToArray();
    var names = expected.Select(TowerPrefabNaming.Safe).ToArray();
    if (!names.SequenceEqual(expected)) throw new Exception("Long name was modified or truncated");
    if (names.Distinct().Count() != names.Length || names.Any(name => !name.Contains(owner)
        || TowerPrefabNaming.Safe(name) != name))
        throw new Exception("UUID, uniqueness or idempotence lost: " + input);
}
var tower = TowerPrefabNaming.ForBridge(new string('x', 120), 28, owner, new string('y', 150), false);
if (tower != new string('x', 120) + "-28-" + owner + " " + new string('y', 150)) throw new Exception("Long tower name lost UUID");
Console.WriteLine("PASS generated section/piece/mesh/LOD names retain complete UUID and distinct identities.");

var longName = new string('x', 300) + "End";
if (NameSanitizer.MakeFileSystemSafe(longName) != longName)
    throw new Exception("Sanitizer truncated name");
var dependency = NameSanitizer.CreateDependencyName("RBBridge", "NetSectionPrefab", longName);
if (!dependency.Contains(longName)) throw new Exception("Dependency name truncated");
if (NameSanitizer.CreateDependencyName("RBBridge", "NetSectionPrefab", "A-B")
    == NameSanitizer.CreateDependencyName("RBBridge", "NetSectionPrefab", "A B"))
    throw new Exception("Sanitized dependency identities collide");
Console.WriteLine("PASS no 96/112 character truncation; dependency hash still distinguishes sanitized collisions.");
