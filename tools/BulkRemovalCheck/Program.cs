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
 GameManager.instance.isGameLoading=true;
 GameManager.instance.userInterface!.appBindings=null;
 BridgeBulkRemoval.RequestConfirmation();Check(ui.Calls==0,"request retained before bindings exist");
 GameManager.instance.userInterface.appBindings=ui; BridgeBulkRemoval.Update(()=>0);
 Check(ui.Calls==1,"confirmation available during loading"); ui.Reply!(1);
 GameManager.instance.isGameLoading=false; int mapCalls=0; BridgeBulkRemoval.Update(()=>++mapCalls);
 Check(mapCalls==0 && Directory.Exists(Path.Combine(imported,owner)) && Directory.Exists(Path.Combine(imported,second)),"cancel changes neither map nor files");
 GameManager.instance.gameMode=Game.GameMode.Editor;BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);
 BridgeBulkRemoval.Update(()=>throw new IOException("fixture map failure"));
 Check(Mod.Messages.Last()=="RemoveAllBridgesFailed" && BridgeRuntimeRequests.Requests.Count==0 && Directory.Exists(Path.Combine(imported,owner)) && Directory.Exists(Path.Combine(imported,second)),"batch map failure keeps every directory");
 GameManager.instance.isGameLoading=true; BridgeBulkRemoval.RequestConfirmation(); ui.Reply!(0);
 BridgeBulkRemoval.WorldChanging();GameManager.instance.isGameLoading=false;
 BridgeBuilder.Systems.BridgeStartupAssetSystem.IsStartupInspection=true;BridgeBulkRemoval.Update(()=>0);
 Check(BridgeRuntimeRequests.Requests.Count==0 && !BridgeDeferredRemoval.Pending(owner),"confirmed request waits for inspection");
 BridgeBuilder.Systems.BridgeStartupAssetSystem.IsStartupInspection=false;GameManager.instance.Saving(true);
 BridgeBulkRemoval.Update(()=>0);Check(!BridgeDeferredRemoval.Pending(owner),"storage waits for save");
 GameManager.instance.Saving(false);GameManager.instance.gameMode=Game.GameMode.Editor;
 Check(!BridgeBulkRemoval.Update(()=>-1) && BridgeRuntimeRequests.Requests.Count==0 && Directory.Exists(Path.Combine(imported,owner)) && Directory.Exists(Path.Combine(imported,second)),"editor aggregate map drain precedes first and second directory");
 BridgeBulkRemoval.Update(()=>2);Check(BridgeRuntimeRequests.Requests.Count==1,"confirmed request survives loading in editor");
 BridgeRuntimeRequests.Requests.Clear();BridgeBulkRemoval.WorldChanging();BridgeBulkRemoval.Update(()=>0);
 Check(BridgeRuntimeRequests.Requests.Count==1,"interrupted owner resumes in new world");BridgeRuntimeRequests.Requests.Clear();BridgeBulkRemoval.Stop();
 GameManager.instance.isGameLoading=false; GameManager.instance.gameMode=Game.GameMode.Game;
 BridgeBulkRemoval.RequestConfirmation();var stale=ui.Reply!;stale(1);Check(BridgeRuntimeRequests.Requests.Count==0,"cancel has no effect");
 BridgeBulkRemoval.RequestConfirmation();stale(0);Check(BridgeRuntimeRequests.Requests.Count==0,"stale callback blocked");
 ui.Reply!(0);BridgeBulkRemoval.Update(()=>0);Check(BridgeRuntimeRequests.Requests.Count==1 && Directory.Exists(Path.Combine(imported,owner)),"confirmation queues first individual deletion, no direct moves");
 var first=BridgeRuntimeRequests.Requests.Dequeue();BridgeBulkRemoval.Completed(first.PrefabName,false);BridgeBulkRemoval.Update(()=>0);
 Check(BridgeRuntimeRequests.Requests.Count==1,"failure does not skip remaining bridge");
 var last=BridgeRuntimeRequests.Requests.Dequeue();BridgeBulkRemoval.Completed(last.PrefabName,true);BridgeBulkRemoval.Update(()=>0);
 Check(Mod.Messages.Last()=="RemoveAllBridgesFailed" ,"partial failure not success");
 BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);BridgeBulkRemoval.Update(()=>0);
 void FinishOwner(string id) {
   string p=Path.Combine(imported,id);
   Check(BridgeDeferredRemoval.Pending(id) && Directory.Exists(p),"persist intent before cleanup");
   Check(!File.Exists(Path.Combine(p,"BridgeRemoval.pending")) && !Directory.Exists(Path.Combine(sandbox,"ModsData","BridgeBuilder","PendingRemoval")),"no ticket or proof in normal path");
   BridgeRemovalFiles.Clear(sandbox,id);
   Check(!Directory.Exists(p),"physical removal before notification");
   BridgeLegacyRemoval.Finish(id);BridgeDeferredRemoval.Complete(id,true);
   int before=Mod.Messages.Count;
   BridgeBulkRemoval.Completed(id,true);
   bool last=!Directory.Exists(Path.Combine(imported,owner))&&!Directory.Exists(Path.Combine(imported,second));
   Check(Mod.Messages.Count == before+(last?1:0),"one immediate final native message, no intermediate one");
   BridgeBulkRemoval.Completed(id,true);
   BridgeBulkRemoval.Update(()=>0);
   Check(Mod.Messages.Count==before+(last?1:0),"duplicate callback and next update do not replay");
 }
 int calls=ui.Calls;
 BridgeBulkRemoval.RequestConfirmation();Check(ui.Calls==calls,"second click cannot queue another batch");
 while(BridgeRuntimeRequests.Requests.Count>0){var r=BridgeRuntimeRequests.Requests.Dequeue();BridgeDeferredRemoval.Schedule(r.PrefabName);BridgeDeferredRemoval.Schedule(r.PrefabName);FinishOwner(r.PrefabName);}
 Check(!Directory.Exists(Path.Combine(imported,owner)) && !Directory.Exists(Path.Combine(imported,second)) && File.ReadAllText(unrelated)=="keep","same-session deletion and unrelated file retained");
 Check(Directory.Exists(Path.Combine(imported,"prefix-"+owner)) && Directory.Exists(Path.Combine(imported,owner+"_Dependencies"))
  && Directory.Exists(Path.Combine(imported,owner+"-suffix")) && Directory.Exists(Path.Combine(imported,"nested",owner)),"partial-name and nested directories preserved");
 Check(Mod.Messages.Last()=="RemoveAllBridgesDone","completion after final verification");
 string third="b33333333-2222-3333-4444-555555555555"; Directory.CreateDirectory(Path.Combine(imported,third));GameManager.instance.gameMode=Game.GameMode.MainMenu;
 BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);BridgeBulkRemoval.Update(()=>0);
 Check(BridgeRuntimeRequests.Requests.Count==0 && BridgeDeferredRemoval.Pending(third) ,"no early main-menu success");
 FinishOwner(third);
 Check(!Directory.Exists(Path.Combine(imported,third)),"main-menu files physically gone before success");
 int shown=Mod.Messages.Count;BridgeBulkRemoval.WorldChanging();BridgeBulkRemoval.Update(()=>0);
 Check(shown==Mod.Messages.Count,"next world does not receive prior success");
 Directory.CreateDirectory(Path.Combine(imported,third));
 BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);BridgeBulkRemoval.Update(()=>0);
 BridgeDeferredRemoval.Complete(third,true);BridgeBulkRemoval.Completed(third,true);
 Check(Mod.Messages.Last()=="RemoveAllBridgesFailed","existing target cannot produce success");
 BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);BridgeBulkRemoval.Update(()=>0);
 GameManager.instance.userInterface!.appBindings=null;
 Check(!BridgeBulkRemoval.Update(()=>throw new Exception("map ran without result UI")) && Directory.Exists(Path.Combine(imported,third)),"pause before commit when there is no UI");
 GameManager.instance.userInterface.appBindings=ui;
 BridgeRemovalFiles.Clear(sandbox,third);BridgeDeferredRemoval.Complete(third,true);
 GameManager.instance.userInterface!.appBindings=null;int missingUi=Mod.Messages.Count;
 BridgeBulkRemoval.Completed(third,true);
 GameManager.instance.userInterface.appBindings=ui;BridgeBulkRemoval.WorldChanging();BridgeBulkRemoval.Update(()=>0);
 Check(Mod.Messages.Count==missingUi,"unavailable UI result never leaks into next context");
 owner="b44444444-2222-3333-4444-555555555555";second="b55555555-2222-3333-4444-555555555555";
 // Both retirements may start without waiting for the first owner's reader.
 Directory.CreateDirectory(Path.Combine(imported,owner)); Directory.CreateDirectory(Path.Combine(imported,second));
 BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);
 Directory.CreateDirectory(Path.Combine(imported,third));
 Check(!BridgeBulkRemoval.IncludesMapOwner(third),"later bridge excluded from map and disk phases");
 BridgeBulkRemoval.Update(()=>0);BridgeBulkRemoval.Update(()=>0);
 Check(BridgeDeferredRemoval.Pending(owner)&&BridgeDeferredRemoval.Pending(second),"second schedules while first waits");
 BridgeRemovalFiles.Clear(sandbox,second);BridgeDeferredRemoval.Complete(second,true);BridgeBulkRemoval.Completed(second,true);
 GameManager.instance.Saving(true);
 typeof(BridgeBulkRemoval).GetField("_progress",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.SetValue(null,DateTime.UtcNow.AddMinutes(-4));
 BridgeBulkRemoval.Update(()=>0);GameManager.instance.Saving(false);
 Check(Mod.Messages.Last()=="RemoveAllBridgesFailed" && Mod.Log.Lines.Last().Contains("remainingCount=1")
   && Mod.Log.Lines.Last().Contains(owner) && !Mod.Log.Lines.Last().Contains(second) && !Mod.Log.Lines.Last().Contains(third)
   && !BridgeStartupRecovery.Retired.Contains(owner) && Directory.Exists(Path.Combine(imported,third)),"timeout while saving logs precise remaining owner and restores retry");
 BridgeRemovalFiles.Clear(sandbox,third);
 BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);BridgeBulkRemoval.Update(()=>0);
 BridgeRemovalFiles.Clear(sandbox,owner);BridgeDeferredRemoval.Complete(owner,true);
 BridgeBuilder.UI.BridgeBuilderUISystem.Residual=new[]{owner};
 BridgeBulkRemoval.Completed(owner,true);
 Check(Mod.Messages.Last()=="RemoveAllBridgesFailed" && Mod.Log.Lines.Last().Contains("final directory/index/management verification")
   && Mod.Log.Lines.Last().Contains("remainingCount=1") && Mod.Log.Lines.Last().Contains(owner),"management binding residue must emit ERROR");
 BridgeBuilder.UI.BridgeBuilderUISystem.Residual=Array.Empty<string>();
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
