using BridgeBuilder;
using BridgeBuilder.Runtime;
using Game.SceneFlow;
void Check(bool ok,string why){if(!ok)throw new Exception(why);}
var sandbox=Path.Combine(Path.GetTempPath(),"BBBulk-"+Guid.NewGuid());
UnityEngine.Application.persistentDataPath=sandbox;
var owner="b11111111-2222-3333-4444-555555555555";
var second="b22222222-2222-3333-4444-555555555555";
var imported=Path.Combine(sandbox,"ImportedData");
var ui=GameManager.instance.userInterface!.appBindings!;
try {
 foreach(var id in new[]{owner,second}) {var dir=Path.Combine(imported,id);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"bad.Prefab"),"no metadata");}
 foreach (var name in new[]{"prefix-"+owner,owner+"_Dependencies",owner+"-suffix","nested"}) {
  var dir=Path.Combine(imported,name);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"keep"),"keep");
 }
 Directory.CreateDirectory(Path.Combine(imported,"nested",owner));
 Check(BridgeOwnedDirectoryRemoval.Owners(sandbox).Order().SequenceEqual(new[]{owner,second}.Order()),"only exact immediate owner directories selected");
 Check(!BridgeOwnedDirectoryRemoval.Delete(sandbox,"../outside",out _),"traversal rejected");
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
 while(BridgeRuntimeRequests.Requests.Count>0){var r=BridgeRuntimeRequests.Requests.Dequeue();Check(BridgeOwnedDirectoryRemoval.Delete(sandbox,r.PrefabName,out var error),error);BridgeBulkRemoval.Completed(r.PrefabName,true);}
 Check(!Directory.Exists(Path.Combine(imported,owner)) && !Directory.Exists(Path.Combine(imported,second)) && File.ReadAllText(unrelated)=="keep","permanent UUID-only deletion");
 Check(Directory.Exists(Path.Combine(imported,"prefix-"+owner)) && Directory.Exists(Path.Combine(imported,owner+"_Dependencies"))
  && Directory.Exists(Path.Combine(imported,owner+"-suffix")) && Directory.Exists(Path.Combine(imported,"nested",owner)),"partial-name and nested directories preserved");
 Check(BridgeOwnedDirectoryRemoval.Delete(sandbox,owner,out _),"already missing exact directory is idempotent");
 Check(Mod.Messages.Last()=="RemoveAllBridgesDone","completion after final callback");
 Console.WriteLine("PASS city availability, confirmation/cancellation, sequential individual deletion, partial failure and permanent file removal.");
} finally {BridgeBulkRemoval.Stop(); if(Path.GetFullPath(sandbox).StartsWith(Path.GetFullPath(Path.GetTempPath())) && Path.GetFileName(sandbox).StartsWith("BBBulk-")) Directory.Delete(sandbox,true);}
