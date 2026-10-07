using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using UnityEngine;

static void Check(bool condition, string error) { if (!condition) throw new Exception(error); }
var id = "b11111111-2222-3333-4444-555555555555";
var bridge = new Game.Prefabs.NetGeometryPrefab();
var icon = new Game.Prefabs.UIObject { m_Icon = "Media/Game/Icons/FourLaneSuspension.svg" };
bridge.components.Add(icon);
BridgeNativePresentation.Prepare(bridge);
Check(icon.m_Icon == "Media/Game/Icons/FourLaneSuspension.svg", "Native prototype icon was erased");
Check(BridgeNativePresentation.Save(new(id, "Old bridge"), out var error), error);
var assets = AssetDatabase.user.Items.OfType<LocaleAsset>().ToArray();
Check(assets.Length == 12, "Missing languages");
var originalAssets = assets.ToArray();
Check(BridgeNativePresentation.Save(new(id, "Renamed bridge"), out error), error);
Check(AssetDatabase.user.Items.Count == 12 && originalAssets.SequenceEqual(assets), "Rename allocated duplicate assets");

// Reproduce native cold-start registration: locales sorted by ID, first source owns the
// SystemLanguage entry; subsequent sources with the same locale ID are allowed.
var locales = new HashSet<string>();
var languages = new Dictionary<SystemLanguage, string>();
foreach (var asset in assets.OrderBy(a => a.Data.localeId))
{
    using var input = new BinaryReader(new MemoryStream(asset.Bytes));
    Check(input.ReadUInt16() == 1, "Locale format changed");
    var language = Enum.Parse<SystemLanguage>(input.ReadString());
    var locale = input.ReadString();
    Check(language != SystemLanguage.Unknown, "Unknown language persisted");
    Check(input.ReadString() == GameManager.instance!.localizationManager.GetLocalizedName(locale), "Native display name lost");
    if (locales.Add(locale)) languages.Add(language, locale);
    var count = input.ReadInt32();
    for (var i = 0; i < count; i++)
    {
        Check(input.ReadString().StartsWith("Assets.NAME[" + id), "Name entry lost its UUID");
        Check(input.ReadString() == "Renamed bridge", "Rename was not persisted");
    }
}
Check(languages.Count == 12, "Native language collisions");

// Missing native metadata must fail before creating or saving even the first file.
var bytes = assets.Select(a => Convert.ToHexString(a.Bytes)).ToArray();
GameManager.instance!.localizationManager.Missing = "zh-HANT";
Check(!BridgeNativePresentation.Save(new(id, "Bad replacement"), out error), "Accepted Unknown mapping");
Check(bytes.SequenceEqual(assets.Select(a => Convert.ToHexString(a.Bytes))), "Failure partially rewrote locales");
Check(!BridgeNativePresentation.Save(new("b22222222-2222-3333-4444-555555555555", "New"), out error)
    && AssetDatabase.user.Items.Count == 12, "Failure created partial language files");
GameManager.instance = null;
Check(!BridgeNativePresentation.Save(new(id, "No manager"), out error), "Missing manager accepted");
Console.WriteLine("PASS 12-language cold-start registration, native headers, rename reuse and failure-before-write.");
