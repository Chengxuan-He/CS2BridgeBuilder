using BridgeBuilder.Runtime;
using System.Text;

var root = Path.Combine(Path.GetTempPath(), "BridgeBuilder-Metadata-" + Guid.NewGuid());
UnityEngine.Application.persistentDataPath = root;
Directory.CreateDirectory(Path.Combine(root, "ImportedData"));
var count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
string Fixture(string id) => "{\"name\":\"" + id + "\",\"components\":{\"$rcontent\":[{\"$type\":\"1|BridgeBuilder.Bridges.BridgeConstructionCost, BridgeBuilder\",\"m_BaseConstructionCost\":368}]},\"dependency\":$fstrref:\"CID:12345678901234567890123456789012\",\"m_Sections\":null}";
string Install(string id, string text)
{
    var dir = Path.Combine(root, "ImportedData", id,"Prefabs"); Directory.CreateDirectory(dir);
    var path = Path.Combine(dir, "Bridge.Prefab"); File.WriteAllText(path, text); File.WriteAllText(path + ".cid", "unchanged"); return path;
}
var a = BridgeAssetInfo.NewPrefabName();
var native = new BridgeAssetInfo(a, "原名 🌉", "Road", "Track", "Style", "", false);
var encoded = BridgeAssetMetadata.Encode(native);
var nativeText = "{\"name\":\"" + a + "\",\"components\":{\"$rcontent\":[{\"$type\":\"1|Game.Prefabs.UIObject, Game\",\"name\":\"" + encoded + "\",\"m_Icon\":\"\"}]},\"dependency\":$fstrref:\"CID:12345678901234567890123456789012\"}";
Check(BridgeAssetMetadata.TryRead(nativeText, out var nativeRead) && nativeRead.DisplayName == native.DisplayName,
    "native UI component carries current metadata without a mod type");
native.DisplayName = "新名";
Check(BridgeAssetMetadata.Rewrite(nativeText, native, out var rewrittenNative)
    && BridgeAssetMetadata.TryRead(rewrittenNative, out nativeRead) && nativeRead.DisplayName == "新名"
    && nativeRead.PrefabName == a && rewrittenNative.Contains("CID:12345678901234567890123456789012"),
    "native rename preserves UUID and referenced CID");
var b = BridgeAssetInfo.NewPrefabName();
var path = Install(a, Fixture(a));
Check(!BridgeAssetMetadata.TryRead(Fixture(a),out _) && BridgeAssetCatalog.Find(a)==null,
    "normal metadata reader and catalogue reject legacy component");
Check(!BridgeAssetMetadata.Rewrite(Fixture(a),native,out _),"normal writer never updates legacy component");
var oldDir=Path.Combine(root,"ImportedData",a,"Historical");Directory.CreateDirectory(oldDir);
File.WriteAllText(Path.Combine(oldDir,a+".Prefab"),nativeText);
File.Delete(path);
Check(BridgeAssetCatalog.Find(a)==null,"legacy filename is not a current catalogue fallback");
path=Install(a,nativeText.Replace("\"dependency\":","\"m_Sections\":null,\"dependency\":"));
var label = "桥梁\t\"名称\"\\轨道\n🌉";
Check(BridgeAssetCatalog.Rename(a, label), "unicode, quotes, slash and control characters persist");
Check(BridgeAssetCatalog.Find(a)?.DisplayName == label, "name survives disk reread");
Check(File.ReadAllText(path + ".cid") == "unchanged", "CID unchanged by rename");
Check(File.ReadAllText(path).Contains("\"m_Sections\":null") && File.ReadAllText(path).Contains("$fstrref:\"CID:12345678901234567890123456789012\""), "rename preserves damaged and external references verbatim");
var pending = new BridgeAssetInfo(b, "new bridge", "upper", "lower", "style", "2026-10-05T00:00:00Z", true);
Check(BridgeAssetCatalog.Begin(pending), "creation reserves only the in-memory identity");
var pendingPrefab=new Game.Prefabs.PrefabBase{name=b};BridgeAssetCatalog.Attach(pendingPrefab,pending);
var text="{\"name\":\""+b+"\",\"components\":{\"$rcontent\":[{\"$type\":\"Game.Prefabs.UIObject, Game\",\"name\":\""+pendingPrefab.GetComponent<Game.Prefabs.UIObject>()!.name+"\"}]}}";
Check(BridgeAssetMetadata.TryRead(text,out var pendingRead)&&pendingRead.Pending,"current metadata embeds before publication");
Install(b, text);
Check(BridgeAssetCatalog.Find(b) == null, "pending asset not shown as finished bridge");
Check(BridgeAssetCatalog.Commit(b), "completion saved into prefab");
BridgeAssetCatalog.ResetSession();
Check(BridgeAssetCatalog.Find(b)?.StyleId == "style", "recipe survives session reset");
Check(BridgeAssetMetadata.Write(Path.Combine(root,"ImportedData",b,"Prefabs","Bridge.Prefab"), pending), "simulate incomplete creation");
BridgeAssetCatalog.ResetSession();
Check(BridgeAssetCatalog.ReadAll().Any(e => e.PrefabName == b && e.Pending), "interrupted creation detected from asset alone");
var c = BridgeAssetInfo.NewPrefabName(); Install(c, "broken Odin");
Check(BridgeAssetCatalog.Find(a) != null && BridgeAssetCatalog.Find(c) == null, "normal catalogue rejects unreadable metadata without inventing an entry");
var original = File.ReadAllBytes(path);
using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
    Check(!BridgeAssetCatalog.Rename(a, "lost"), "locked asset fails safely");
Check(File.ReadAllBytes(path).SequenceEqual(original), "failed rename preserves original bytes");
foreach (var file in args)
{
    var source = File.ReadAllText(file);
    Check(BridgeAssetMetadata.TryRead(source, out var entry), "actual fixture read " + Path.GetFileName(file));
    entry.DisplayName = label;
    Check(BridgeAssetMetadata.Rewrite(source, entry, out var changed) && BridgeAssetMetadata.TryRead(changed, out var result) && result.DisplayName == label, "actual Odin fixture name round trip");
}


for (var fields = 1; fields <= 7; fields++)
{
    var values = new[] { a, "Existing name", "Road", "Track", "Style", "", "0" }.Take(fields);
    var partial = BridgeAssetMetadata.Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(
        string.Join("\n", values.Select(v => Convert.ToBase64String(Encoding.UTF8.GetBytes(v))))));
    var accepted=BridgeAssetMetadata.Decode(partial,out var recovered);
    Check(accepted==(fields==7), "normal reader requires exactly seven fields; fields="+fields);
    if(accepted) Check(recovered.PrefabName==a&&recovered.DisplayName=="Existing name", "complete metadata identity and value preserved");
}
Console.WriteLine($"PASS {count} asset metadata checks. Scratch: {root}");
Directory.Delete(root,true);
