using BridgeBuilder.Runtime;
var root = Path.Combine(Path.GetTempPath(), "BridgeMove-" + Guid.NewGuid());
var imported = Path.Combine(root, "ImportedData");
var geometry = Path.Combine(root, "BridgeBuilder");
Directory.CreateDirectory(imported); Directory.CreateDirectory(geometry);
var owner = "b" + Guid.NewGuid(); var healthy = "b" + Guid.NewGuid();
var source = Path.Combine(imported, "legacy-prefix" + owner + "suffix");
Directory.CreateDirectory(Path.Combine(source,"nested","empty"));
File.WriteAllText(Path.Combine(source,"broken.Prefab"), "unparseable; no metadata or CID");
var other = Path.Combine(imported,healthy); Directory.CreateDirectory(other);
var deps = Path.Combine(imported,owner+"_Dependencies"); Directory.CreateDirectory(deps);
var mesh = Path.Combine(geometry,"anything-"+owner+".unknown"); File.WriteAllText(mesh,"geometry");
File.WriteAllText(Path.Combine(geometry,"shared.Geometry"),"shared");
var verdict = new Dictionary<string,string>{{owner,"invalid"}};
void Check(bool ok,string label) { if(!ok) throw new Exception(label); Console.WriteLine("PASS "+label); }
var audit = BridgeDiskAudit.ForMemoryFailures(root,verdict);
Check(audit.Complete && audit.FileOwners.Count==3,"substring alone selects directory, empty dependencies and arbitrary-extension file");
var backup = root+"-backup"; Directory.CreateDirectory(backup);
File.WriteAllText(Path.Combine(backup,Path.GetFileName(source)),"existing");
File.WriteAllText(Path.Combine(source,"new.bin"),"added after inventory");
File.AppendAllText(Path.Combine(source,"broken.Prefab"),"changed");
Check(audit.RetireFiles(new HashSet<string>{owner},backup,out var error),error);
var moved = Directory.GetDirectories(backup,Path.GetFileName(source)+"__*").Single();
Check(File.ReadAllText(Path.Combine(moved,"new.bin"))=="added after inventory"
 && Directory.Exists(Path.Combine(moved,"nested","empty")),"move includes changed contents and empty nested directories");
Check(!Directory.Exists(source) && !Directory.Exists(deps) && !File.Exists(mesh),"matched source paths moved");
Check(Directory.Exists(other) && File.Exists(Path.Combine(geometry,"shared.Geometry")),"unmatched assets preserved");
Check(File.ReadAllText(Path.Combine(backup,Path.GetFileName(source)))=="existing","backup collision does not overwrite");
Directory.CreateDirectory(source); File.WriteAllText(Path.Combine(source,"remaining"),"keep");
audit=BridgeDiskAudit.ForMemoryFailures(root,verdict);
Check(!audit.RetireFiles(new HashSet<string>{owner},Path.Combine(root,"Recovery"),out error)
 && Directory.Exists(source),"unsafe destination refused without deleting source");
var blocked=root+"-blocked"; File.WriteAllText(blocked,"file");
Check(!audit.RetireFiles(new HashSet<string>{owner},blocked,out error) && Directory.Exists(source),"failed move setup reported without deletion");
Check(!audit.RetireFiles(new HashSet<string>{healthy},backup,out error),"unselected owner refused");
Check(BridgeAssetInfo.TryFileOwner("prefix"+owner+"suffix/file.cid",out var found) && found==owner,"regex has no token boundaries");
Check(BridgeAssetInfo.MatchesOwner("prefix"+owner+"suffix",owner) && !BridgeAssetInfo.MatchesOwner(healthy,owner),"single owner uses literal substring");
Check(!BridgeAssetInfo.TryFileOwner("BridgeBuilder normal road",out _),"labels and mod names alone do not imply ownership");
