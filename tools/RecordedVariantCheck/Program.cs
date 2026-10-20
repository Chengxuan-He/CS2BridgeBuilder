using BridgeBuilder.Bridges;
using Game.Prefabs;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
foreach (var (id, name, road, width) in new[] {
    ("Draw02", "DrawBridge02", true, 19f),
    ("Draw03", "DrawBridge03", true, 25f),
    ("Lift01", "LiftBridge01", false, 6f),
    ("Lift02", "LiftBridge02", false, 10f),
    ("Lift04", "LiftBridge04", false, 10f),
    ("Lift05", "LiftBridge05", false, 8f),
    ("PedestrianDraw01", "PedestrianDrawBridge01", false, 8f),
    ("PedestrianDraw02", "PedestrianDrawBridge02", false, 8f) })
{
    var source = new BridgePrototypeSource();
    var icon = "Media/Game/Icons/" + name + ".svg";
    var variant = new BridgeStyleVariant(name, road, icon, source);
    var style = new BridgeStyle(id, id, () => "Localized name", 0);
    style.Add(variant);
    style.BindSingleSource();
    Check(variant.IsRecorded && variant.IsBuiltin && variant.IsRoad == road, "Recorded type metadata");
    Check(variant.Name == name && variant.RoadWidth == width, "Measured identity and width");
    Check(variant.Overhead == null && variant.SubObjects == null && variant.LowerDeck == null && !variant.IsDoubleDeck,
        "No fabricated native components");
    Check(!variant.CarriesTower("Absent prototype") && variant.DescribeTowers().Contains(name), "Diagnostics without donor");
    Check(ReferenceEquals(style.Select(width, road).Variant, variant), "Selection without original network");
    Check(style.Select(width, road, doubleDeck: true).Variant == null, "No invented second deck");
    Check(style.Select(width, road, allow: _ => false).Variant == null, "Caller restriction retained");
    // The production UI/migration fallback must short-circuit the forbidden donor access.
    Check((variant.RecordedIcon ?? variant.Donor.name) == icon, "Recorded icon without original network");
    source.IsAvailable = false;
    Check(!style.IsInstalled && style.Select(width, road).Variant == null, "Missing prerequisite excludes variant");
    source.IsAvailable = true;
    Check(style.IsInstalled && ReferenceEquals(style.Select(width + 8, !road).Variant, variant), "Availability and selection recover");
}
var dualSource = new BridgePrototypeSource();
var dual = new BridgeStyleVariant("LiftBridge03", true, "Media/Game/Icons/LiftBridge03.svg", dualSource, -10f);
var dualStyle = new BridgeStyle("Lift03", "Lift03", () => "Localized dual deck", 0);
dualStyle.Add(dual);
Check(dual.IsRecorded && dual.IsDoubleDeck && dual.RoadWidth == 20f, "Recorded dual-deck metadata");
Check(dual.LowerDeck?.m_Position.y == -10f, "Measured lower-deck separation without donor");
Check(dualStyle.Select(20f, true).Variant == null, "Dual-deck archetype excludes single deck");
Check(ReferenceEquals(dualStyle.Select(20f, true, doubleDeck: true).Variant, dual), "Dual-deck selection without donor");
var arrangementCopy = dual.LowerDeck!;
arrangementCopy.m_Position.y = 100f;
Check(dual.LowerDeck!.m_Position.y == -10f, "Caller cannot mutate recorded arrangement");
dualSource.IsAvailable = false;
Check(!dualStyle.IsInstalled && dualStyle.Select(20f, true, doubleDeck: true).Variant == null,
    "Dual-deck prerequisite availability retained");
var native = new RoadPrefab { name = "DrawBridge02" };
var bridge = new Bridge();
var original = new BridgeStyleVariant(native, bridge, 20.2f);
Check(!original.IsRecorded && ReferenceEquals(original.Donor, native) && ReferenceEquals(original.Bridge, bridge),
    "Existing native variants retain their references");
Check(original.RecordedIcon == null && original.Width == 20.2f && original.RoadWidth == 19,
    "Existing native width and icon behavior");
Console.WriteLine($"PASS {checks} recorded-variant checks; native availability is a test boundary, not game acceptance.");
