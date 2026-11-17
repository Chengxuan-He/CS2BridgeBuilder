using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;
using UnityEngine;

static void Check(bool condition, string error) { if (!condition) throw new Exception(error); }
var id = "b11111111-2222-3333-4444-555555555555";
BridgePresentationIcons.Register(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../assets")));
var persisted=new Game.Prefabs.NetGeometryPrefab { asset=new PrefabAsset { path="ImportedData/"+id+"/Prefabs/Bridge.Prefab",id=(Colossal.Hash128.Parse("fixture"),0) } };
var inputs=new BridgeCreationDependencies();
foreach(var uri in new[]{"coui://bridgebuilderui/BridgeNanpu.svg","coui://bridgebuilderui/BridgeLupu.svg","coui://bridgebuilder/old","coui://bridgebuilderui/BridgeBuilder.svg","coui://bridgebuilderui/BridgeNanpu.svg.missing","coui://bridgebuilderui/Missing.svg"}) {
 inputs.Source=new(){Bytes=System.Text.Encoding.UTF8.GetBytes("{\"m_Icon\":\""+uri+"\"}")};
 var expected=uri.EndsWith("/BridgeNanpu.svg")||uri.EndsWith("/BridgeLupu.svg");
 Check(BridgeNativePresentation.Validate(id,[persisted],out var validationError,inputs)==expected,"Serialized icon policy: "+uri+": "+validationError);
}
inputs.Source=new(){Bytes=System.Text.Encoding.UTF8.GetBytes("{\"m_Icon\":\"coui://bridgebuilderui/BridgeNanpu.svg\",\"$type\":\"Legacy, BridgeBuilder\"}")};
Check(!BridgeNativePresentation.Validate(id,[persisted],out _,inputs),"Packaged icon must not whitelist legacy runtime components");
BridgePresentationIcons.Clear();
inputs.Source=new(){Bytes=System.Text.Encoding.UTF8.GetBytes("{\"m_Icon\":\"coui://bridgebuilderui/BridgeNanpu.svg\"}")};
Check(!BridgeNativePresentation.Validate(id,[persisted],out _,inputs),"Unregistered packaged URI accepted");
BridgePresentationIcons.Register(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../assets")));
Console.WriteLine("PASS serialized presentation: exact packaged icons, manager/unknown/missing URIs and legacy components.");
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
        var key = input.ReadString();
        var value = input.ReadString();
        Check(key.Contains(id), "Entry lost its UUID");
        Check(value == (key.StartsWith("Assets.NAME[") ? "Renamed bridge"
            : BridgeBuilder.Settings.RuntimeUiText.Format(locale, "BridgeAssetDescription")), "Native text not persisted");
    }
}
Check(languages.Count == 12, "Native language collisions");
var writes = assets.Sum(a => a.Saves);
Check(BridgeNativePresentation.EnsureDescriptions(new(id, "Do not rename"), out var changed, out error) && !changed, error);
Check(assets.Sum(a => a.Saves) == writes, "Complete descriptions were rewritten");
var first = assets[0];
first.Data.entries.Remove("Assets.DESCRIPTION[" + id + "]");
first.Data.entries["Assets.DESCRIPTION[" + id + "_Lower]"] = " ";
first.Data.entries["Unrelated.Entry"] = "preserved";
first.Data.indexCounts["Unrelated.Count"] = 7;
Check(BridgeNativePresentation.EnsureDescriptions(new(id, "Do not rename"), out changed, out error) && changed, error);
Check(assets.Sum(a => a.Saves) == writes + 1, "Repair must save only the incomplete locale");
Check(first.Data.entries["Assets.NAME[" + id + "]"] == "Renamed bridge"
    && first.Data.entries["Unrelated.Entry"] == "preserved" && first.Data.indexCounts["Unrelated.Count"] == 7,
    "Description repair changed existing names or other entries");
Check(!string.IsNullOrWhiteSpace(first.Data.entries["Assets.DESCRIPTION[" + id + "_Lower]"]), "Blank description not repaired");
Check(BridgeNativePresentation.EnsureDescriptions(new(id, "Do not rename"), out changed, out error) && !changed, "Repair is not idempotent");

first.Data.entries["Assets.NAME[" + id + "]"] = " ";
Check(BridgeNativePresentation.EnsureDescriptions(new(id, "Recovered name"), out changed, out error) && changed, error);
Check(first.Data.entries["Assets.NAME[" + id + "]"] == "Recovered name", "Blank name not filled");
Check(BridgeNativePresentation.EnsureDescriptions(new(id, "Do not rename"), out changed, out error) && !changed, "Name completion not idempotent");

// Startup uses the owner path list and exact CID lookup; no all-Prefab or locale filter query.
var temp=Path.Combine(Path.GetTempPath(), "BBLocale85-"+Guid.NewGuid()); Directory.CreateDirectory(temp);
var paths=new HashSet<string>();
foreach(var asset in assets) {
 asset.id=(Colossal.Hash128.Parse(Guid.NewGuid().ToString("N")),0);
 asset.path=Path.Combine(temp,id,"Locales",asset.Data.localeId+".loc");
 Directory.CreateDirectory(Path.GetDirectoryName(asset.path)!);File.WriteAllText(asset.path+".cid",asset.id.guid.ToString()); paths.Add(asset.path);
}
bridge.uiTag=id; AssetDatabase.user.ForbidEnumeration=true;
Check(BridgeNativePresentation.EnsureDescriptions(new(id,"Unchanged"),out changed,out error,new[]{bridge},paths)&&!changed,error);
first.Data.entries.Remove("Assets.DESCRIPTION["+id+"]");
Check(BridgeNativePresentation.EnsureDescriptions(new(id,"Unchanged"),out changed,out error,new[]{bridge},paths)&&changed,error);
AssetDatabase.user.ForbidEnumeration=false;
Console.WriteLine("PASS owner-local locale path/CID lookup with all-assets enumeration tripwire.");

// Native in-memory locale mappings are trusted; they are not a validation gate.
var bytes = assets.Select(a => Convert.ToHexString(a.Bytes)).ToArray();
GameManager.instance!.localizationManager.Missing = "zh-HANT";
Check(BridgeNativePresentation.EnsureDescriptions(new(id, "Unchanged"), out changed, out error)
    && !changed, "Complete locale data must remain unchanged with a trusted mapping");
Check(bytes.SequenceEqual(assets.Select(a => Convert.ToHexString(a.Bytes))), "No-op changed locale bytes");
foreach(var style in new[]{"Nanpu","Lupu","Grand","GoldenGate","GoldenGateDouble"}) {
 var owner="b"+Guid.NewGuid();
 var named=new Game.Prefabs.NetGeometryPrefab{name=owner};
 var factory=new BridgeBuilder.Bridges.TowerFactory();
 var random=factory.Source.AddComponent<Game.Prefabs.RandomLocalization>();random.m_LocalizationID="Random.Bridge";
 BridgeBuilder.Bridges.BridgeProperName.CreateAggregate(named,factory);
 Check(!ReferenceEquals(named.m_AggregateType,factory.Source)&&factory.Source.components.Contains(random),"Shared native Bridge aggregate changed");
 Check(named.m_AggregateType!.components.OfType<Game.Prefabs.RandomLocalization>().Count()==0&&factory.Created.Single()==named.m_AggregateType,"Named aggregate lifetime or random pool retained");
 var key=BridgeBuilder.Bridges.BridgeProperName.Key(owner);
 Check(named.m_AggregateType.components.OfType<Game.Prefabs.Localization>().Single().m_LocalizationID==key,"Wrong native naming key");
 var info=new BridgeAssetInfo(owner,"Selected road display name",style);
 Check(BridgeNativePresentation.Save(info,out error),error);
 var own=AssetDatabase.user.Items.OfType<LocaleAsset>().Where(a=>a.name.StartsWith(owner)).ToArray();
 Check(own.Length==12,"Named bridge locale count");
 foreach(var asset in own) Check(asset.Data.entries[key]==BridgeBuilder.Bridges.BridgeProperName.Label(style,asset.Data.localeId)
  &&asset.Data.entries["Assets.NAME["+owner+"]"]==info.DisplayName,"Asset candidate and placed proper name must remain separate");
 foreach(var asset in own) Check(asset.database.onAssetDatabaseChanged.Dictionaries[asset.Data.localeId][key]==BridgeBuilder.Bridges.BridgeProperName.Label(style,asset.Data.localeId),"Saved locale did not reach active native-event consumer");
 var saved=own.Sum(a=>a.Saves);
 Check(BridgeNativePresentation.EnsureDescriptions(info,out changed,out error)&&!changed&&own.Sum(a=>a.Saves)==saved,"Named bridge first cold inspection rewrites locales");
 info.DisplayName="Player chosen name";
 Check(BridgeNativePresentation.Save(info,out error),error);
 Check(own.All(a=>a.Data.entries["Assets.NAME["+owner+"]"]=="Player chosen name"&&a.Data.entries[key]==BridgeBuilder.Bridges.BridgeProperName.Label(style,a.Data.localeId)),"Player asset rename or native proper name lost");
}
Check(BridgeNativePresentation.EnsureDescriptions(new(id,"Unchanged","Grand"),out changed,out error)&&!changed,error);
Check(assets.All(a=>!a.Data.entries.ContainsKey(BridgeBuilder.Bridges.BridgeProperName.Key(id))),"Existing native-named bridge gained an unused private map key");
Console.WriteLine("PASS private native aggregate ownership, localized proper-name entries, no random pool, cold no-op and player rename preservation.");
GameManager.instance = null;
Check(!BridgeNativePresentation.Save(new(id, "No manager"), out error), "Missing manager accepted");
Console.WriteLine("PASS 12-language native headers, rename reuse, missing-only idempotence and unavailable-manager handling.");
