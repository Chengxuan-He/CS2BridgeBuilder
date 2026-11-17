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
    style.BindSources(Array.Empty<PrefabBase>());
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
var direct = new BridgePrototypeSource { Key = "mod:92245" };
var mod = new BridgePrototypeSource { Key = "mod:92245" };
var dlc = new BridgePrototypeSource { Key = "content:SanFranciscoSet" };
BridgePrototypeSource.Requirements[mod.Key]=mod; BridgePrototypeSource.Requirements[dlc.Key]=dlc;
var ordered = new BridgeStyle("GoldenGateDouble", "test", () => "test");
var registered = new BridgeStyleVariant("test", true, "icon", direct, 25f,39f);
ordered.Add(registered); ordered.BindSources(Array.Empty<PrefabBase>());
Check(ordered.Sources.SequenceEqual(new[]{mod.Key,dlc.Key}), "explicit source order retained");
foreach(var donorOn in new[]{false,true}) foreach(var modOn in new[]{false,true}) foreach(var dlcOn in new[]{false,true}) {
 direct.IsAvailable=donorOn;mod.IsAvailable=modOn;dlc.IsAvailable=dlcOn;
 Check(ordered.IsInstalled==(donorOn&&modOn&&dlcOn), "all source entries and donor gate style");
 Check(registered.IsAvailable==(donorOn&&modOn&&dlcOn), "variant repeats complete availability after state change");
}
var wrongOwner = new BridgeStyle("GoldenGateDouble", "test", () => "test");
wrongOwner.Add(new BridgeStyleVariant("wrong",true,"icon",new BridgePrototypeSource {Key="mod:124160"},25f,39f));
wrongOwner.BindSources(Array.Empty<PrefabBase>());Check(!wrongOwner.IsInstalled,"B&P mod cannot replace declared base BEP owner");
var empty = new BridgeStyle("Grand","test",()=>"test");empty.BindSources(Array.Empty<PrefabBase>());
Check(empty.Sources.Length==0&&!empty.IsInstalled,"base empty requirements do not invent unregistered donor");
Console.WriteLine($"PASS {checks} recorded-variant checks; native availability is a test boundary, not game acceptance.");
