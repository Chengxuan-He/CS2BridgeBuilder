using BridgeBuilder;
using BridgeBuilder.Runtime;
using Game.SceneFlow;

int checks=0;
void Check(bool value,string message) { if(!value) throw new Exception(message); checks++; }
var root=Path.GetFullPath(Path.Combine("work","deletion149-test-"+Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(Path.Combine(root,"ImportedData"));
UnityEngine.Application.persistentDataPath=root;
var one="b11111111-2222-3333-4444-555555555555";
var two="b22222222-2222-3333-4444-555555555555";
var mapOnly="b33333333-2222-3333-4444-555555555555";
var later="b44444444-2222-3333-4444-555555555555";
string Dir(string id)=>Path.Combine(root,"ImportedData",id);
foreach(var id in new[]{one,two}) { Directory.CreateDirectory(Dir(id)); File.WriteAllText(Path.Combine(Dir(id),"payload"),"unaltered"); }
BridgeDeferredRemoval.Initialize("");
var manager=new Unity.Entities.EntityManager();
var prefabs=new Game.Prefabs.PrefabSystem();
BridgeBulkRemoval.NativeReady=()=>BridgePrefabRetirement.Ready(manager,prefabs);
var ui=GameManager.instance.userInterface!.appBindings!;
BridgeBulkRemoval.RequestConfirmation(); ui.Reply!(1);
Check(!BridgeDeletionIntent.Contains(root,one)&&!BridgeDeletionIntent.Contains(root,two),"cancel writes nothing");
BridgeBulkRemoval.PlacedOwners=()=>new[]{mapOnly};
BridgeBulkRemoval.RequestConfirmation(); ui.Reply!(0);
Check(new[]{one,two,mapOnly}.All(o=>BridgeDeletionIntent.Contains(root,o)),"all durable before map");
Check(new FileInfo(Path.Combine(Dir(one),BridgeDeletionIntent.Marker)).Length==0,"zero byte marker");
Check(!Directory.Exists(Dir(mapOnly))&&File.Exists(BridgeDeletionIntent.Record(root,mapOnly)),"map-only outside intent");
Directory.CreateDirectory(Dir(later));
Check(!BridgeBulkRemoval.IncludesMapOwner(later),"subsequent owner not captured");
GameManager.instance.gameMode=Game.GameMode.Editor;
manager.Created=true;int mapCalls=0;
BridgeBulkRemoval.Update(()=>{mapCalls++;return 0;});
Check(mapCalls==0 && BridgeDeferredRemoval.ReadyOwners.Count==0,"confirmed bulk intent cannot run map phase while native Created prefabs remain");
manager.Created=false;
Check(!BridgeBulkRemoval.Update(()=>-1)&&BridgeDeferredRemoval.ReadyOwners.Count==0,"whole batch map barrier");
Check(new[]{one,two}.All(o=>File.ReadAllText(Path.Combine(Dir(o),"payload"))=="unaltered"),"no payload mutation");
GameManager.instance.gameMode=Game.GameMode.MainMenu;
for(int i=0;i<4;i++)BridgeBulkRemoval.Update(()=>0);
foreach(var owner in new[]{one,two,mapOnly}) { BridgeDeferredRemoval.Complete(owner,true); BridgeBulkRemoval.Completed(owner,true); }
Check(Mod.Messages.Count==1&&Mod.Messages[0]=="RemoveAllBridgesDone","verified batch shows only concise success");
BridgeBulkRemoval.Completed(one,true);BridgeBulkRemoval.Update(()=>0);
Check(Mod.Messages.Count==1,"late duplicate completion cannot repeat success");
Check(Directory.Exists(Dir(one))&&Directory.Exists(Dir(two)),"logical completion retains directories");
var originalRoot=UnityEngine.Application.persistentDataPath;
UnityEngine.Application.persistentDataPath=Path.Combine(root,"empty-batch");
Directory.CreateDirectory(Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData"));
BridgeBulkRemoval.PlacedOwners=()=>Array.Empty<string>();
int confirmations=ui.Calls;
BridgeBulkRemoval.RequestConfirmation();Check(ui.Calls==confirmations+1,"pre-deletion confirmation remains visible");ui.Reply!(0);
BridgeBulkRemoval.Update(()=>0);
Check(Mod.Messages.Count==2&&Mod.Messages[1]=="RemoveAllBridgesDone","confirmed empty batch shows same concise success");
ui.Fail=true;BridgeBulkRemoval.RequestConfirmation();ui.Fail=false;
Check(Mod.Messages.Count==3&&Mod.Messages[2]=="RemoveAllBridgesFailed","genuine confirmation error never becomes success");
UnityEngine.Application.persistentDataPath=Path.Combine(root,"residual-batch");
Directory.CreateDirectory(Path.Combine(UnityEngine.Application.persistentDataPath,"ImportedData",one));
BridgeBulkRemoval.RequestConfirmation();ui.Reply!(0);BridgeBulkRemoval.Update(()=>0);
BridgeDeferredRemoval.Complete(one,true);
BridgeBuilder.UI.BridgeBuilderUISystem.Residual=new[]{one};
BridgeBulkRemoval.Completed(one,true);
Check(Mod.Messages.Count==4&&Mod.Messages.Last()=="RemoveAllBridgesFailed","remaining management entry blocks success popup");
BridgeBuilder.UI.BridgeBuilderUISystem.Residual=Array.Empty<string>();
UnityEngine.Application.persistentDataPath=originalRoot;
int messages=Mod.Messages.Count;
BridgeDeferredRemoval.Stop();BridgeDeferredRemoval.Initialize("");
Check(BridgeDeferredRemoval.ReadyOwners.SetEquals(new[]{one,two,mapOnly}),"restart resumes exact intents");
Check(new[]{one,two,mapOnly}.All(o=>BridgeDeferredRemoval.IsSilent(o)),"restart is silent");
Check(Mod.Messages.Count==messages,"no startup popup");
BridgeDeferredRemoval.Complete(one,false);
Check(BridgeDeferredRemoval.Excluded(one),"failure never resurrects owner");
BridgeDeletionIntent.CommitLedger(root,two);
File.Delete(Path.Combine(Dir(two),BridgeDeletionIntent.Marker));
Check(BridgeDeletionIntent.Contains(root,two),"ledger survives partial removal of marker");
BridgeDeletionIntent.Verify(root,two);
Check(File.ReadAllText(Path.Combine(Dir(two),"payload"))=="unaltered","ledger commit does not mutate assets");
BridgeDeletionIntent.Accept(root,one);
Check(new FileInfo(Path.Combine(Dir(one),BridgeDeletionIntent.Marker)).Length==0,"idempotent existing marker");
File.WriteAllText(Path.Combine(Dir(later),BridgeDeletionIntent.Marker),"invalid");
try { BridgeDeletionIntent.Accept(root,later); throw new Exception("bad marker accepted"); } catch(IOException) { checks++; }
Check(File.ReadAllText(Path.Combine(Dir(later),BridgeDeletionIntent.Marker))=="invalid","bad marker never truncated");
try { BridgeDeletionIntent.Accept(root,"../outside"); throw new Exception("traversal accepted"); } catch(InvalidDataException) { checks++; }
Check(!Directory.GetFiles(root,"*",SearchOption.AllDirectories).Any(p=>p.EndsWith(".ps1")||p.EndsWith(".exe")),"no external executors");
using(var held=new FileStream(Path.Combine(Dir(two),"payload"),FileMode.Open,FileAccess.Read,FileShare.None))
{
    try { BridgeDeletionIntent.Cleanup(root,two); throw new Exception("locked payload removed"); } catch(IOException) { checks++; }
    Check(File.Exists(BridgeDeletionIntent.Record(root,two)),"failed recursive cleanup retains outside ledger");
}
BridgeDeletionIntent.Cleanup(root,two);
Check(!Directory.Exists(Dir(two))&&!File.Exists(BridgeDeletionIntent.Record(root,two)),"physical absence precedes clearing ledger");
BridgeDeletionIntent.Cleanup(root,one);
Check(!Directory.Exists(Dir(one)),"whole-directory primitive removes confirmed owner");
BridgeDeletionIntent.Cleanup(root,mapOnly);
Check(!File.Exists(BridgeDeletionIntent.Record(root,mapOnly)),"map-only absent directory completes after logical caller gate");
Check(Directory.Exists(Dir(later)),"unconfirmed/corrupt marker owner preserved");
GameManager.instance.gameMode=Game.GameMode.Other;
Check(!BridgePrefabRetirement.Ready(manager,prefabs),"WorldReady before first native boot passes is not a loaded context");
GameManager.instance.gameMode=Game.GameMode.MainMenu;manager.Created=true;
Check(!BridgePrefabRetirement.Ready(manager,prefabs),"Created prefab blocks retirement in a menu too");
manager.Created=false;manager.Updated=true;
Check(!BridgePrefabRetirement.Ready(manager,prefabs),"Updated prefab blocks retirement");
manager.Updated=false;prefabs.Queue();
Check(!BridgePrefabRetirement.Ready(manager,prefabs),"queued replacement blocks retirement before it creates an entity");
prefabs.Drain();
foreach(var mode in new[]{Game.GameMode.MainMenu,Game.GameMode.Game,Game.GameMode.Editor}) {
 GameManager.instance.gameMode=mode;
 Check(BridgePrefabRetirement.Ready(manager,prefabs),"initialized native context admits retirement: "+mode);
}
// Minimal reproduction of native RemovePrefab's sentinel and Created initializer lookup.
var table=new List<string>{"bridge","Ground Cable Section1"};int index=0;manager.Created=true;
void Remove(){index=-1000000000;table.RemoveAt(table.Count-1);}
void Initialize(){_ = table[index];manager.Created=false;}
try{Remove();Initialize();throw new Exception("Old ordering did not reproduce invalid prefab index");}catch(ArgumentOutOfRangeException){checks++;}
table=new List<string>{"bridge","Ground Cable Section1"};index=0;
if(BridgePrefabRetirement.Ready(manager,prefabs))Remove();
Initialize();Check(table[index]=="bridge"&&table.Contains("Ground Cable Section1"),"new ordering lets all native entries initialize before removal");
Check(BridgePrefabRetirement.Ready(manager,prefabs),"native completion releases pending work without a timed delay");
// Exercise the production event observer against native lifecycle doubles.
UnityEngine.Application.dataPath=Path.Combine(root,"PlayerData");
Directory.CreateDirectory(UnityEngine.Application.dataPath);
File.WriteAllText(Path.Combine(UnityEngine.Application.dataPath,"boot.config"),"single-instance=\n");
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var hook=typeof(BridgeShutdownCleanup);
HashSet<string> Done()=>(HashSet<string>)hook.GetField("Completed",flags)!.GetValue(null)!;
string ShutdownLog()=>File.ReadAllText(Path.Combine(root,"Logs","BridgeBuilder.Deletion.Shutdown.log"));
void BeginShutdown(){
 GameManager.instance=new(); Colossal.IO.AssetDatabase.AssetDatabase.global=new();Colossal.IO.AssetDatabase.AssetDatabase.exists=true;
 Colossal.TaskManager.instance=new();Unity.Entities.World.All.Clear();
 BridgeShutdownCleanup.Start();BridgeShutdownCleanup.LogicallyCompleted(later);
 GameManager.instance.state=GameManager.State.Quitting;BridgeShutdownCleanup.DisposingMod();
}
BridgeShutdownCleanup.Start();BridgeShutdownCleanup.Start();
Check(UnityEngine.Application.Subscribers==1,"initialization subscribes once without Harmony");
BridgeShutdownCleanup.LogicallyCompleted(later);BridgeShutdownCleanup.WorldChanging();
Check(Done().Count==0,"world transition invalidates prior completion");
BridgeShutdownCleanup.DisposingMod();
Check(UnityEngine.Application.Subscribers==0,"non-shutdown mod unload removes event and never deletes");
BeginShutdown();BridgeShutdownCleanup.WorldChanging();
Check(Done().Contains(later),"committed shutdown retains logical completion");
Check(UnityEngine.Application.Subscribers==1,"event retained after mod disposal");
GameManager.instance.state=GameManager.State.Terminated;
UnityEngine.Application.QuitEvent();
Check(ShutdownLog().Contains("databaseDisposed=False"),"terminal state alone cannot authorize cleanup with live database");
Check(Directory.Exists(Dir(later))&&Done().Count==0,"failed shutdown keeps files and consumes observer once");
var once=ShutdownLog().Length;UnityEngine.Application.QuitEvent();
Check(ShutdownLog().Length==once,"repeated event cannot retry cleanup");
BeginShutdown();Colossal.IO.AssetDatabase.AssetDatabase.global.Dispose();
GameManager.instance.state=GameManager.State.Terminated;
UnityEngine.Application.QuitEvent();
Check(ShutdownLog().Contains("terminal=True; databaseDisposed=True; nativeDrain=True; worldsDisposed=True"),"complete native release admitted up to process exclusion");
Check(Directory.Exists(Dir(later)),"non-game test host cannot authorize production cleanup");
foreach(var pending in new[]{new TaskCompletionSource<bool>().Task,Task.FromException(new IOException("native task failure"))}){
 BeginShutdown();Colossal.TaskManager.instance.taskQueue.Completion=pending;
 Colossal.IO.AssetDatabase.AssetDatabase.global.Dispose();GameManager.instance.state=GameManager.State.Terminated;
 UnityEngine.Application.QuitEvent();Check(ShutdownLog().Split('\n').Last(l=>l.Contains("Final Unity quit event;")).Contains("nativeDrain=False"),"incomplete/faulted queue remains pending without blocking event");
 Check(Directory.Exists(Dir(later)),"failed native drain cannot delete files");
}
BeginShutdown();Unity.Entities.World.All.Add(new(){IsCreated=true});
Colossal.IO.AssetDatabase.AssetDatabase.global.Dispose();GameManager.instance.state=GameManager.State.Terminated;
UnityEngine.Application.QuitEvent();Check(ShutdownLog().Contains("worldsDisposed=False"),"live ECS world blocks cleanup");
BeginShutdown();Colossal.TaskManager.instance.taskRegistry["late"]=Task.CompletedTask;
Colossal.IO.AssetDatabase.AssetDatabase.global.Dispose();GameManager.instance.state=GameManager.State.Terminated;
UnityEngine.Application.QuitEvent();Check(Directory.Exists(Dir(later)),"uncleared native registry keeps files");
BeginShutdown();
var unfinished=new TaskCompletionSource<bool>();Colossal.TaskManager.instance.taskRegistry["captured"]=unfinished.Task;
BridgeShutdownCleanup.DisposingMod();Colossal.TaskManager.instance.taskRegistry.Clear();
Colossal.IO.AssetDatabase.AssetDatabase.global.Dispose();GameManager.instance.state=GameManager.State.Terminated;
UnityEngine.Application.QuitEvent();
Check(ShutdownLog().Split('\n').Last(l=>l.Contains("Final Unity quit event;")).Contains("nativeDrain=False"),"clearing registry cannot hide unfinished captured task");
BeginShutdown();Colossal.IO.AssetDatabase.AssetDatabase.global.Dispose();
UnityEngine.Application.QuitEvent();
Check(ShutdownLog().Split('\n').Last(l=>l.Contains("Final Unity quit event;")).Contains("terminal=False"),"event before captured manager termination cannot authorize deletion");
Check(BridgeShutdownCleanup.Eligible(true,true,true,true,false),"all verified lifecycle conditions admit cleanup");
Check(!BridgeShutdownCleanup.Eligible(false,true,true,true,false),"missing logical completion blocks cleanup");
Check(!BridgeShutdownCleanup.Eligible(true,false,true,true,false),"failed native release blocks cleanup");
Check(!BridgeShutdownCleanup.Eligible(true,true,false,true,false),"live worlds block cleanup");
Check(!BridgeShutdownCleanup.Eligible(true,true,true,false,false),"missing native single-instance restriction blocks cleanup");
Check(!BridgeShutdownCleanup.Eligible(true,true,true,true,true),"observable competing player blocks cleanup");
Console.WriteLine($"PASS {checks} checks; production intent/deferred/bulk/shutdown sources with native lifecycle doubles. Fixture retained: {root}");
