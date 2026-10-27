param([string]$Repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder')
$ErrorActionPreference='Stop'
$root=Join-Path $Repo 'artifacts/agent-material-env/PedestrianDraw01'
$output=Join-Path $Repo 'artifacts/prepared-materials/PedestrianDraw01/shared-sampling'
[IO.Directory]::CreateDirectory($output)|Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/SharedSamplingProbe.Editor.cs" -Destination "$root/unity/Assets/Editor/PedestrianSharedSamplingProbe.cs"
Copy-Item -LiteralPath "$PSScriptRoot/SharedSamplingProbe.compute" -Destination "$root/unity/Assets/PedestrianSharedSamplingProbe.compute"
$env:TEMP="$root/cache/temp"; $env:TMP=$env:TEMP; $env:UPM_CACHE_ROOT="$root/cache/upm"
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$root+'/unity"'),'-executeMethod','PedestrianSharedSamplingProbe.Run','-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"','-sourceInventory',('"'+$Repo+'/artifacts/uv-density/structure-tessellated-sources.json"'),'-probeReport',('"'+$output+'/native-probe.json"'),'-quit','-logFile',('"'+$output+'/native-probe.log"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Native source probe PID=$($process.Id)"
$process.WaitForExit()
Select-String -LiteralPath "$output/native-probe.log" -Pattern '^PASS shared|error CS|^Exception|InvalidDataException' | ForEach-Object Line
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$output/native-probe.log" -Pattern '^PASS shared-source capability probe:' -Quiet)){throw 'Native source probe failed'}
