$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$out = Join-Path $repo 'work/source-dependency-20261116/admission'
New-Item -ItemType Directory -Force -Path $out | Out-Null
# Compile the actual entrypoint prefixes, stopping immediately before generation.
# Native geometry is intentionally outside this managed admission test.
function Prefix($file, $start, $end) {
    $text = [IO.File]::ReadAllText((Join-Path $repo $file))
    $a = $text.IndexOf($start, [StringComparison]::Ordinal)
    $b = $text.IndexOf($end, $a, [StringComparison]::Ordinal)
    if ($a -lt 0 -or $b -lt 0) { throw "Admission boundary changed: $file" }
    return $text.Substring($a, $b - $a)
}
$create = Prefix 'src/BridgeBuilder/Systems/BridgeGenerationSystem.Management.cs' 'private void CreateRuntimeBridge(' '        var doubleDeck ='
$preview = Prefix 'src/BridgeBuilder/Systems/BridgeGenerationSystem.Preview.cs' 'private void BuildPreview(' '            var work = new GenerationWork('
$movable = Prefix 'src/BridgeBuilder/Bridges/BridgeStyleDefinitions.cs' 'internal static bool IsMovable(' '    internal static string Family('
$rootGate = Prefix 'src/BridgeBuilder/Runtime/BridgePreparedMaterials.cs' 'internal static bool TrySourceRoot(' '    private static ILocalAssetDatabase?'
$header = @'
using System;
using System.Collections.Generic;
static class Mod { public static Logger Log=new(); public class Logger { public void Error(Exception error,string text){} } }
class BridgeOptions {}
class BridgeRuntimeRequest { public string StyleId="", UpperDeckId="road", LowerDeckId="", PrefabName=""; }
class Style { public string Id=""; public bool IsInstalled; }
static class BridgeStyleCatalog { public static Style? Value; public static Style? Find(string id)=>Value; }
static class DeckCatalog { public static object Find(string id)=>new(); }
class ExportReport { public ExportReport(bool logIssues){} public void Failed(string label,Exception error){} }
static class ExportStateStore { public static object Load()=>new(); }
static class BridgeRuntimeRequests { public static string? Error; public static void Complete(string error)=>Error=error; }
'@
$test = @'
class SystemUnderTest {
 public int Reads,Reached; public string? Error; public Action? Resume;
 void Finish(ExportReport report,object state,string operation,bool showMessage){}
 void FailPreview(int revision,string key)=>Error=key;
 void FailMaterialCreation(Exception error)=>Error=error.Message;
 void PrepareMaterialFiles(string id,Action ready,Action<Exception> failed,int? revision=null) {
   if(!BridgePreparedMaterials.TrySourceRoot(id,out _,out var error)){failed(new Exception(error));return;}
   Reads++;Resume=ready;
 }
 public void Run(bool preview,BridgeRuntimeRequest request){if(preview)BuildPreview(request,1);else CreateRuntimeBridge(request);}
'@
$body = $header + "`nstatic class BridgeStyleDefinitions {`n" + $movable + "}`n" + @'
static class BridgePreparedMaterials {
 static string? _root="unused-test-root";
 public static object? RequestFiles;
 static bool SourceAvailable(string id)=>BridgeStyleCatalog.Find(id)?.IsInstalled==true;
'@ + $rootGate + "}`n" + $test + $create + "Reached++; }`n" + $preview + "Reached++; } catch(Exception error){Error=error.Message;} }`n}" + @'

class Program {
 static void Main(){int count=0;
 foreach(bool preview in new[]{false,true})
 foreach(string style in new[]{"Suspension","GOViaduct","Nanpu","Lupu","Draw01","GoldenGate","GoldenGateDouble","ArtDecoViaduct"})
 foreach(bool available in new[]{false,true}) {
   BridgeStyleCatalog.Value=new Style{Id=style,IsInstalled=available};
   BridgePreparedMaterials.RequestFiles=null; BridgeRuntimeRequests.Error=null;
   var system=new SystemUnderTest();system.Run(preview,new BridgeRuntimeRequest{StyleId=style});
   bool prepared=available&&style=="Draw01";
   if(system.Reads!=(prepared?1:0)||system.Reached!=(available&&!prepared?1:0))throw new Exception("Admission "+style);
   if(!available&&system.Error==null&&BridgeRuntimeRequests.Error==null)throw new Exception("Missing failure");
   if(prepared){BridgeStyleCatalog.Value.IsInstalled=false;BridgePreparedMaterials.RequestFiles=new();system.Resume!();
     if(system.Reached!=0||system.Reads!=1)throw new Exception("Stale resumed request");}
   count++;
 }
 BridgeStyleCatalog.Value=null;BridgePreparedMaterials.RequestFiles=null;
 var saved=new SystemUnderTest();saved.Run(true,new BridgeRuntimeRequest{PrefabName="saved-owner"});
 if(saved.Reads!=0||saved.Reached!=1)throw new Exception("Saved preview incorrectly gated");
 Console.WriteLine($"PASS {count+1} production entrypoint admission cases; native geometry not exercised.");
 }
}
'@
Set-Content -LiteralPath (Join-Path $out 'Program.cs') -Value $body
Set-Content -LiteralPath (Join-Path $out 'Admission.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable></PropertyGroup></Project>'
dotnet run --project (Join-Path $out 'Admission.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Request admission tests failed' }
