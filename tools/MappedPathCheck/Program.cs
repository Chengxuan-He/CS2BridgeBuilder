using BridgeBuilder.Runtime;
using System.Diagnostics;
using System.IO.Compression;
if(!OperatingSystem.IsWindows()){Console.WriteLine("SKIP Windows junction fixtures");return;}
var root=Path.GetFullPath(Path.Combine("work","mapped-check-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(root);
Console.WriteLine("FIXTURE "+root);
string Dir(string p){Directory.CreateDirectory(p);return p;}
void Link(string p,string target){Dir(Path.GetDirectoryName(p)!);var start=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true};foreach(var a in new[]{"-NoProfile","-NonInteractive","-EncodedCommand",Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes("$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path '"+p.Replace("'","''")+"' -Target '"+target.Replace("'","''")+"' | Out-Null"))})start.ArgumentList.Add(a);using var child=Process.Start(start)!;child.WaitForExit();if(child.ExitCode!=0)throw new Exception("junction creation");}
void Check(bool yes,string message){if(!yes)throw new Exception(message);Console.WriteLine("PASS "+message);}
void Reject(Action action,string message){try{action();}catch(IOException){Console.WriteLine("PASS "+message);return;}throw new Exception(message);}
var owner="b"+Guid.NewGuid();var other="b"+Guid.NewGuid();var game=Dir(Path.Combine(root,"game"));var imported=Dir(Path.Combine(game,"ImportedData"));
var a=Dir(Path.Combine(imported,owner));var payload=Dir(Path.Combine(root,"payload"));File.WriteAllText(Path.Combine(payload,"data"),"same");Link(Path.Combine(a,"Appearance"),payload);Link(Path.Combine(a,"second"),payload);
Check(BridgeFileAccess.LogicalUnder(Path.Combine(payload,"data"),Path.Combine(a,"Appearance"))==Path.Combine(a,"Appearance","data"),"known logical scope resolves native physical path");
var backup=Path.Combine(root,"backup");BridgeFileAccess.MovePayload(a,backup);Check(!Directory.Exists(payload)&&!Directory.Exists(a)&&File.ReadAllText(Path.Combine(backup,"Appearance","data"))=="same"&&File.ReadAllText(Path.Combine(backup,"second","data"))=="same","duplicate nested mapping backup materializes both routes and removes actual target");
a=Dir(Path.Combine(imported,owner));var b=Dir(Path.Combine(imported,other));File.WriteAllText(Path.Combine(b,"keep"),"other");Link(Path.Combine(a,"Appearance"),b);Reject(()=>BridgeRemovalFiles.Clear(game,owner),"other owner alias rejected before removal");Check(File.Exists(Path.Combine(b,"keep")),"other owner bytes preserved");Directory.Delete(Path.Combine(a,"Appearance"));
var shared=Dir(Path.Combine(root,"shared-anonymous-payload"));File.WriteAllText(Path.Combine(shared,"keep"),"shared");
Link(Path.Combine(a,"Appearance"),shared);Link(Path.Combine(b,"Appearance"),shared);
Reject(()=>BridgeRemovalFiles.Clear(game,owner),"shared anonymous mapped boundary rejected before deletion");
Check(File.ReadAllText(Path.Combine(b,"Appearance","keep"))=="shared"&&Directory.Exists(a),"shared target and both owner namespaces preserved");
Check(BridgeFileAccess.WithinAssetStorage(Path.Combine(shared,"backup"),game),"backup alias inside unselected mapped owner rejected");
var unrelatedCache=Dir(Path.Combine(game,"unrelated-cache"));Link(Path.Combine(unrelatedCache,"cycle"),unrelatedCache);
Check(!BridgeFileAccess.WithinAssetStorage(Path.Combine(root,"valid-independent-backup"),game),"unrelated game cache cycle does not reject independent backup");
Directory.Delete(Path.Combine(a,"Appearance"));Directory.Delete(Path.Combine(b,"Appearance"));
var otherPayload=Dir(Path.Combine(root,"unselected-private-payload"));File.WriteAllText(Path.Combine(otherPayload,"keep"),"other");Link(Path.Combine(b,"Appearance"),otherPayload);
File.WriteAllText(Path.Combine(a,"selected"),"selected");var direct=BridgeDiskAudit.ForMemoryFailures(game,new Dictionary<string,string>{{owner,"fixture"}},new[]{a});
Check(!direct.RetireFiles(new HashSet<string>{owner},Path.Combine(otherPayload,"direct-backup"),out _)&&!direct.Changed&&File.Exists(Path.Combine(a,"selected"))&&!Directory.Exists(Path.Combine(otherPayload,"direct-backup")),"direct retirement rejects backup in unselected mapped owner before writes");
Directory.Delete(Path.Combine(b,"Appearance"));

Link(Path.Combine(a,"cycle"),a);Reject(()=>BridgeRemovalFiles.Clear(game,owner),"cycle rejected before deletion");Directory.Delete(Path.Combine(a,"cycle"));
var mapped=Dir(Path.Combine(root,"zip-payload"));File.WriteAllText(Path.Combine(mapped,"data"),"zip");Link(Path.Combine(a,"Appearance"),mapped);var zip=Path.Combine(root,"export.zip");BridgeZipExport.Write(a,zip,owner);using(var z=ZipFile.OpenRead(zip))Check(z.GetEntry(owner+"/Appearance/data")!=null,"ZIP follows nested mapping preserving logical path");Reject(()=>BridgeZipExport.Write(a,Path.Combine(mapped,"bad.zip"),owner),"ZIP destination alias into nested payload rejected");
var audit=BridgeDiskAudit.ForMemoryFailures(game,new Dictionary<string,string>{{owner,"fixture"}},new[]{a});Check(!audit.RetireFiles(new HashSet<string>{owner},Path.Combine(mapped,"backup"),out _)&&!audit.Changed&&File.Exists(Path.Combine(mapped,"data")),"backup alias rejected without mutation or restart");
BridgeDeletionIntent.Accept(game,owner);BridgeDeletionIntent.Cleanup(game,owner);Check(!Directory.Exists(mapped)&&!Directory.Exists(a)&&Directory.Exists(imported),"mapped deletion intent removes payload and retains parent");BridgeRemovalFiles.Clear(game,owner);Check(Directory.Exists(b),"repeated deletion preserves unrelated owner");
var lockSource=Dir(Path.Combine(root,"locked-source"));var lockPayload=Dir(Path.Combine(root,"locked-target"));var locked=Path.Combine(lockPayload,"locked");File.WriteAllText(locked,"keep");Link(Path.Combine(lockSource,"nested"),lockPayload);using(var stream=new FileStream(locked,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Reject(()=>BridgeFileAccess.MovePayload(lockSource,Path.Combine(root,"partial-backup")),"copy failure preserves source before deletion");Check(File.Exists(locked)&&Directory.Exists(lockSource),"failed copy retains all source payload");
var retry=Dir(Path.Combine(imported,owner));var gone=Dir(Path.Combine(root,"gone"));Link(Path.Combine(retry,"dangling"),gone);Directory.Delete(gone);BridgeRemovalFiles.Clear(game,owner);Check(!Directory.Exists(retry),"owned dangling junction retry completes");
Console.WriteLine("PASS fixture retained for audit; no live assets touched");
