using CS2Mods.Shared.Infrastructure;
var root=Path.Combine(Path.GetTempPath(),"BBExportState-"+Guid.NewGuid().ToString("N"));
ExportPaths.Root=root;
void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);}
Check(ExportStateStore.Load().ExportNames().Count==0&&!File.Exists(ExportPaths.StateFile),"missing state is empty without writing payload");
var current="exportName\tfingerprint\tlastExportUtc\nBridge\tfp\t2026-11-15T00:00:00Z\n";
File.WriteAllText(ExportPaths.StateFile,current);
var first=ExportStateStore.Load();Check(first.IsUnchanged("Bridge","fp")&&File.ReadAllText(ExportPaths.StateFile)==current,"current state read without rewriting");
first.Record("Second","fp2");first.Save();var saved=File.ReadAllText(ExportPaths.StateFile);
Check(ExportStateStore.Load().IsUnchanged("Second","fp2")&&File.ReadAllText(ExportPaths.StateFile)==saved,"new state cold reread unchanged");
var unsupported="sourceId\tfingerprint\texportName\tlastExportUtc\nsource\tfp\tBridge\t2026-11-15\n";
File.WriteAllText(ExportPaths.StateFile,unsupported);var rejected=false;
try{ExportStateStore.Load();}catch(InvalidDataException){rejected=true;}
Check(rejected&&File.ReadAllText(ExportPaths.StateFile)==unsupported,"unsupported historical state rejected without implicit rewriting");
Console.WriteLine("Retained isolated fixture: "+root);
namespace CS2Mods.Shared.Infrastructure {internal static class ExportPaths {internal static string Root="";internal static string StateFile=>Path.Combine(Root,"export-state.tsv");internal static void EnsureDataDirectory()=>Directory.CreateDirectory(Root);}}
