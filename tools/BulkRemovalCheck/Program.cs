using BridgeBuilder;
using BridgeBuilder.Runtime;
using Game.SceneFlow;
void Check(bool ok,string why){if(!ok)throw new Exception(why);}
var sandbox=args.Length==2?Path.GetFullPath(args[0]):Path.Combine(Path.GetTempPath(),"BBBulk-"+Guid.NewGuid());
if(Directory.Exists(sandbox)) throw new Exception("Use a new test directory");
UnityEngine.Application.persistentDataPath=sandbox;
var owner="b11111111-2222-3333-4444-555555555555";
var second="b22222222-2222-3333-4444-555555555555";
var imported=Path.Combine(sandbox,"ImportedData");
var ui=GameManager.instance.userInterface!.appBindings!;
try {
 BridgeDeferredRemoval.Initialize(args.Length==2?args[1]:Path.GetFullPath("src/BridgeBuilder/bin/Release"));
 GameManager.instance.Saving(true); Check(BridgeDeferredRemoval.Saving,"Native save start observed");
 GameManager.instance.Saving(false); Check(!BridgeDeferredRemoval.Saving,"Native save end observed");
 foreach(var path in new string?[]{null,""," ",Path.Combine(imported,second,"Road.Surface"),Path.Combine(imported,owner+"-suffix","Road.Surface")})
  Check(BridgeOwnedDirectoryRemoval.AssetPath(sandbox,owner,path)==null,"pathless and foreign assets excluded before normalization");
 var ownedSurface=Path.Combine(imported,owner,"Appearance","Road.Surface");
 Check(BridgeOwnedDirectoryRemoval.AssetPath(sandbox,owner,ownedSurface)==Path.GetFullPath(ownedSurface),"owned nested surface selected even before file exists");
 Check(BridgeOwnedDirectoryRemoval.AssetPath(sandbox,owner,@"\\?\"+ownedSurface)==Path.GetFullPath(ownedSurface),"extended native path ownership preserved");
 foreach(var id in new[]{owner,second}) {var dir=Path.Combine(imported,id);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"bad.Prefab"),"no metadata");}
 foreach (var name in new[]{"prefix-"+owner,owner+"_Dependencies",owner+"-suffix","nested"}) {
  var dir=Path.Combine(imported,name);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"keep"),"keep");
 }
 Directory.CreateDirectory(Path.Combine(imported,"nested",owner));
 Check(BridgeOwnedDirectoryRemoval.Owners(sandbox).Order().SequenceEqual(new[]{owner,second}.Order()),"only exact immediate owner directories selected");
 try { BridgeDeferredRemoval.Prepare("../outside"); throw new Exception("Traversal accepted"); } catch(InvalidDataException) { }
 var unrelated=Path.Combine(imported,"other.txt");File.WriteAllText(unrelated,"keep");
 GameManager.instance.isGameLoading=true; BridgeBulkRemoval.RequestConfirmation();Check(ui.Calls==0,"loading blocked");
 GameManager.instance.isGameLoading=false; GameManager.instance.gameMode=Game.GameMode.Game;
 BridgeBulkRemoval.RequestConfirmation();var stale=ui.Reply!;stale(1);Check(BridgeRuntimeRequests.Requests.Count==0,"cancel has no effect");
 BridgeBulkRemoval.RequestConfirmation();stale(0);Check(BridgeRuntimeRequests.Requests.Count==0,"stale callback blocked");
 ui.Reply!(0);Check(BridgeRuntimeRequests.Requests.Count==1 && Directory.Exists(Path.Combine(imported,owner)),"confirmation queues first individual deletion, no direct moves");
 var first=BridgeRuntimeRequests.Requests.Dequeue();BridgeBulkRemoval.Completed(first.PrefabName,false);
 Check(BridgeRuntimeRequests.Requests.Count==1,"failure does not skip remaining bridge");
 var last=BridgeRuntimeRequests.Requests.Dequeue();BridgeBulkRemoval.Completed(last.PrefabName,true);
 Check(Mod.Messages.Last()=="RemoveAllBridgesFailed" && BridgeBulkRemoval.CanRequest,"partial failure not success");
 BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);
 void FinishOwner(string id) {
   string p=Path.Combine(imported,id);
   Check(BridgeDeferredRemoval.Pending(id) && Directory.Exists(p),"persist intent before cleanup");
   Check(!File.Exists(Path.Combine(p,"BridgeRemoval.pending")) && !Directory.Exists(Path.Combine(sandbox,"ModsData","BridgeBuilder","PendingRemoval")),"no ticket or proof in normal path");
   BridgeRemovalFiles.Clear(sandbox,id);
   Check(!Directory.Exists(p),"physical removal before notification");
   BridgeLegacyRemoval.Finish(id);BridgeDeferredRemoval.Complete(id,true);BridgeBulkRemoval.Completed(id,true);
 }
 while(BridgeRuntimeRequests.Requests.Count>0){var r=BridgeRuntimeRequests.Requests.Dequeue();BridgeDeferredRemoval.Schedule(r.PrefabName);BridgeDeferredRemoval.Schedule(r.PrefabName);FinishOwner(r.PrefabName);}
 Check(!Directory.Exists(Path.Combine(imported,owner)) && !Directory.Exists(Path.Combine(imported,second)) && File.ReadAllText(unrelated)=="keep","same-session deletion and unrelated file retained");
 Check(Directory.Exists(Path.Combine(imported,"prefix-"+owner)) && Directory.Exists(Path.Combine(imported,owner+"_Dependencies"))
  && Directory.Exists(Path.Combine(imported,owner+"-suffix")) && Directory.Exists(Path.Combine(imported,"nested",owner)),"partial-name and nested directories preserved");
 Check(Mod.Messages.Last()=="RemoveAllBridgesDone","completion after final verification");
 string third="b33333333-2222-3333-4444-555555555555"; Directory.CreateDirectory(Path.Combine(imported,third));GameManager.instance.gameMode=Game.GameMode.MainMenu;
 BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);
 Check(BridgeRuntimeRequests.Requests.Count==0 && BridgeDeferredRemoval.Pending(third) && !BridgeBulkRemoval.CanRequest,"no early main-menu success");
 FinishOwner(third);
 Check(BridgeBulkRemoval.CanRequest && !Directory.Exists(Path.Combine(imported,third)),"main-menu files physically gone before success");
 var legacyDir=Directory.CreateDirectory(Path.Combine(imported,owner));
 var legacyInbox=Directory.CreateDirectory(Path.Combine(sandbox,"ModsData","BridgeBuilder","PendingRemoval"));
 var token=Guid.NewGuid().ToString("N");File.WriteAllText(Path.Combine(legacyDir.FullName,"BridgeRemoval.pending"),token);
 File.WriteAllText(Path.Combine(legacyInbox.FullName,owner+".ticket"),token);
 BridgeDeferredRemoval.Stop();BridgeDeferredRemoval.Initialize("");
 Check(BridgeDeferredRemoval.Pending(owner),"matching old ticket independently resumed");
 BridgeRemovalFiles.Clear(sandbox,owner); BridgeLegacyRemoval.Finish(owner);
 Check(!Directory.Exists(legacyDir.FullName)&&!File.Exists(Path.Combine(legacyInbox.FullName,owner+".ticket")),"legacy owner and ticket both gone");
 Console.WriteLine("PASS same-process city/menu, confirmation, partial failure, index barrier, save notification, duplicate request, prior ticket, actual files and indices verified.");
} finally {BridgeBulkRemoval.Stop();BridgeDeferredRemoval.Stop();}
