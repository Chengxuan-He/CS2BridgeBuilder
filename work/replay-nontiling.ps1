param([string]$Revision = 'final')
$ErrorActionPreference='Stop'
$repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder'
Set-Location -LiteralPath $repo
$python='C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$managed='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed'
 $out="artifacts/prepared-materials/shared-nontiling/actual-$Revision"
 $observer="artifacts/agent-material-env/shared-resume/nontiling-$Revision-observer"
New-Item -ItemType Directory -Force -Path $out | Out-Null
./tools/Build.ps1 *> "$out/build.log"
if($LASTEXITCODE -ne 0){throw 'Build failed'}
Copy-Item src/BridgeBuilder/bin/Release/BridgeBuilder.dll "$out/BridgeBuilder.dll"
./tools/Build-RetileObserver.ps1 -Python $python -Output $observer *> "$out/observer-build.log"
if($LASTEXITCODE -ne 0){throw 'Observer build failed'}
dotnet build "$observer/runner" "-p:GameManagedDir=$managed" *> "$out/runner-build.log"
if($LASTEXITCODE -ne 0){throw 'Runner build failed'}
dotnet "$observer/runner/bin/Debug/net8.0/RetileObserverRunner.dll" "$observer/BridgeBuilder.Observer.dll" $managed artifacts/uv-density/actual-plans.bin "$out/observer.json" *> "$out/observer-run.log"
if($LASTEXITCODE -ne 0){throw 'Actual plans failed'}
& $python tools/SummarizeRetileObserver.py "$out/observer.json.units.ndjson" "$out/styles" --source-uv "$out/observer.json.source-uv.ndjson" *> "$out/summarize.log"
if($LASTEXITCODE -ne 0){throw 'Summarize failed'}
$maps=@{
Draw01='Draw01/actual-retile/source-map.json';Draw02='Draw02/structure-units/retile-source-map.json';Draw03='Draw03/current-source-map.json';
Lift01='Lift01/structure-semantic/source-map-exact.json';Lift02='Lift02/current-readiness/source-map.json';Lift03='Lift03/actual-retile-current/source-map.json';
Lift04='Lift04/actual-retile/source-map.json';Lift05='Lift05/actual-unit-check/source-map.json';
PedestrianDraw01='PedestrianDraw01/actual-retile/source-map.json';PedestrianDraw02='PedestrianDraw02/current-source-map.json'}
dotnet build tools/RetileRegionCheck "-p:GameManagedDir=$managed" *> "$out/region-build.log"
if($LASTEXITCODE -ne 0){throw 'Region check build failed'}
foreach($style in $maps.Keys|Sort-Object){
 dotnet tools/RetileRegionCheck/bin/Debug/net8.0/RetileRegionCheck.dll "$out/BridgeBuilder.dll" $managed "$out/styles/$style.units.ndjson.gz" "artifacts/prepared-materials/$($maps[$style])" "$out/styles/$style.regions.json" *> "$out/styles/$style.regions.log"
 if($LASTEXITCODE -ne 0){throw "Region check failed: $style"}
}
Get-FileHash "$out/BridgeBuilder.dll", "$observer/BridgeBuilder.Observer.dll", artifacts/uv-density/actual-plans.bin | Format-List | Out-File "$out/input-hashes.txt"
