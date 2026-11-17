using System.Security.AccessControl;
using System.Security.Principal;
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
var scopedOwner="b"+Guid.NewGuid();var scopedRoot=Path.Combine(imported,scopedOwner);
Directory.CreateDirectory(Path.Combine(scopedRoot,"Prefabs"));var scopedFile=Path.Combine(scopedRoot,"Prefabs","Bridge.Prefab");File.WriteAllText(scopedFile,"test");
var scoped=BridgeDiskAudit.ForMemoryFailures(root,new Dictionary<string,string>{{scopedOwner,"test"}},new[]{scopedFile,scopedFile.Replace('\\','/'),scopedRoot});
Check(scoped.Complete && scoped.FileOwners.Count==1 && scoped.FileOwners.ContainsKey(scopedRoot),"Scoped inspection selects the whole owner tree once");
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
 && Directory.Exists(source),"unsafe backup preserves source");
var blocked=root+"-blocked"; File.WriteAllText(blocked,"file");
audit=BridgeDiskAudit.ForMemoryFailures(root,verdict);
Check(!audit.RetireFiles(new HashSet<string>{owner},blocked,out error) && Directory.Exists(source),"non-permission backup failure preserves source");
Check(!audit.Changed,"no mutation does not request restart");
var finalBackup=root+"-final";
Check(audit.RetireFiles(new HashSet<string>{owner},finalBackup,out error)&&audit.Changed,"first session moves failed owner");
var cold=BridgeDiskAudit.ForMemoryFailures(root,verdict);
Check(cold.FileOwners.Count==0&&!cold.Changed,"cold second session sees no failed owner or mutation");
Check(Directory.Exists(other) && File.Exists(Path.Combine(geometry,"shared.Geometry")),"fallback never removes unrelated assets");
Check(!audit.RetireFiles(new HashSet<string>{healthy},backup,out error),"unselected owner refused");
Check(BridgeAssetInfo.TryFileOwner("prefix"+owner+"suffix/file.cid",out var found) && found==owner,"regex has no token boundaries");
Check(BridgeAssetInfo.MatchesOwner("prefix"+owner+"suffix",owner) && !BridgeAssetInfo.MatchesOwner(healthy,owner),"single owner uses literal substring");
Check(!BridgeAssetInfo.TryFileOwner("BridgeBuilder normal road",out _),"labels and mod names alone do not imply ownership");

var permissionRoot=Path.Combine(root,"permissions");Directory.CreateDirectory(permissionRoot);
Check(BridgeInspectionPermissions.Check(new[]{permissionRoot})&&!Directory.EnumerateFileSystemEntries(permissionRoot).Any(),"startup read/write/move/delete probe cleans its files");
var sid=WindowsIdentity.GetCurrent().User!;
void Deny(string path,FileSystemRights rights,Action body) {
 var info=new DirectoryInfo(path);var original=info.GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access);var denied=info.GetAccessControl();
 var rule=new FileSystemAccessRule(sid,rights,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Deny);
 denied.AddAccessRule(rule);
 try{info.SetAccessControl(denied);body();}finally{var restored=new DirectorySecurity();restored.SetSecurityDescriptorSddlForm(original,AccessControlSections.Access);info.SetAccessControl(restored);if(info.GetAccessControl().GetAccessRules(true,false,typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().Any(r=>r.AccessControlType==AccessControlType.Deny && r.IdentityReference.Equals(sid)))throw new Exception("Deny rule remains after restoration");var probe=Path.Combine(path,"restoration-"+Guid.NewGuid());File.WriteAllText(probe,"restored");if(File.ReadAllText(probe)!="restored")throw new Exception("Restored read failed");File.Delete(probe);}
}
Deny(permissionRoot,FileSystemRights.WriteData,()=>{
 bool raised=false;try{BridgeInspectionPermissions.Check(new[]{permissionRoot});}catch(BridgeRequiredFilePermissionException){raised=true;}
 Check(raised,"required startup write permission denial propagates");
});
var deniedBackup=root+"-denied";Directory.CreateDirectory(deniedBackup);
Directory.CreateDirectory(source);File.WriteAllText(Path.Combine(source,"failed.Prefab"),"failed");
Deny(deniedBackup,FileSystemRights.WriteData,()=>{
 var deniedAudit=BridgeDiskAudit.ForMemoryFailures(root,verdict);
 Check(deniedAudit.RetireFiles(new HashSet<string>{owner},deniedBackup,out var failure)&&deniedAudit.Changed&&!Directory.Exists(source),"existing backup destination write denial permits owner deletion");
});
var permissionFile=Path.Combine(permissionRoot,"owned");File.WriteAllText(permissionFile,"owner");
Deny(permissionRoot,FileSystemRights.ReadData,()=>{
 bool raised=false;try{BridgeFileAccess.ReadText(permissionFile);}catch(BridgeRequiredFilePermissionException){raised=true;}
 Check(raised,"actual owner read permission denial propagates");
});
