using BridgeBuilder.Runtime;
using System.Text;

if(args.Length==4 && args[0]=="--audit-existing")
{
    ExistingOwnerAudit.Run(args[1],args[2],args[3]);
    return;
}

string root=Path.Combine(Path.GetTempPath(),"BBFootprint-"+Guid.NewGuid().ToString("N"));
const string owner="b11111111-1111-1111-1111-111111111111";
string a=new('a',32),b=new('b',32),c=new('c',32),d=new('d',32);
string surface=Path.Combine(root,"ImportedData",owner,"Appearance","test.Surface");
Directory.CreateDirectory(Path.GetDirectoryName(surface)!);
File.WriteAllBytes(surface,new byte[17]);File.WriteAllText(surface+".cid",b,new UTF8Encoding(false));
var sources=new Dictionary<string,BridgeDependencyCopies.Source>{
    [a]=new(){Owned=true,Extension=".Prefab",Bytes=Encoding.UTF8.GetBytes("CID:"+b)},
    [b]=new(){Owned=true,Extension=".Surface",PhysicalPath=surface,SerializedReferences=new[]{c,c,d}},
    [c]=new(){Extension=".Texture",Bytes=new byte[64]},
    [d]=new(){Builtin=true,Extension=".Texture"}
};
var measure=new BridgeMaterialFootprint();
if(!BridgeDependencyCopies.Save(root,owner,new[]{a},cid=>sources.GetValueOrDefault(cid),out _,out var error,out _,measure.Observe))throw new Exception(error);
string report=measure.Write(root,owner,"fixture-build");string text=File.ReadAllText(report);
if(!text.Contains("MaterialBytes\t145")||!text.Contains("MaterialFiles\t4")||!text.Contains("BudgetStatus\tPASS")||!text.Contains("BuiltinReferences\t1"))throw new Exception("Persisted graph totals/dedup failed: "+text);
string auditPath=Path.Combine(Path.GetTempPath(),"BBAudit-"+Guid.NewGuid().ToString("N")+".json");
string checkerAssembly=typeof(ExistingOwnerAudit).Assembly.Location;
measure.Write(root,owner,typeof(ExistingOwnerAudit).Assembly.ManifestModule.ModuleVersionId.ToString());
ExistingOwnerAudit.Run(root,checkerAssembly,auditPath);
using(var audit=System.Text.Json.JsonDocument.Parse(File.ReadAllText(auditPath)))
    if(audit.RootElement.GetProperty("verifiedCurrentOwners").GetInt32()!=1)throw new Exception("Read-only report verifier rejected complete fixture");
File.Delete(surface+".cid");
ExistingOwnerAudit.Run(root,checkerAssembly,auditPath);
using(var audit=System.Text.Json.JsonDocument.Parse(File.ReadAllText(auditPath)))
    if(audit.RootElement.GetProperty("verifiedCurrentOwners").GetInt32()!=0)throw new Exception("Read-only report verifier accepted missing sidecar");
File.Delete(auditPath);
text=File.ReadAllText(measure.Write(root,owner,"fixture-build"));
if(!text.Contains("BudgetStatus\tUNVERIFIED")||!text.Contains("CompleteMaterialGraph\tFalse"))throw new Exception("Missing physical dependency falsely passed");
var vt=new BridgeMaterialFootprint();sources[b].SerializedReferences=new[]{c};sources[c].Extension=".VTTexture";
File.WriteAllText(surface+".cid",b,new UTF8Encoding(false));
if(!BridgeDependencyCopies.Save(root,owner,new[]{a},cid=>sources.GetValueOrDefault(cid),out _,out error,out _,vt.Observe))throw new Exception(error);
text=File.ReadAllText(vt.Write(root,owner,"fixture-build"));
if(!text.Contains("External VT graph not expanded")||!text.Contains("BudgetStatus\tUNVERIFIED"))throw new Exception("Unparsed external VT falsely passed");
Console.WriteLine("PASS actual saved-file footprint, CID dedup, builtin boundary, missing sidecar and unparsed-VT rejection. Fixture: "+root);
