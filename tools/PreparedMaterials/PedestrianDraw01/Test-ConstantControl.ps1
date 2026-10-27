param([string]$Repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder')
$ErrorActionPreference='Stop'
$root=Join-Path $Repo 'artifacts/agent-material-env/PedestrianDraw01'
$out=Join-Path $Repo 'artifacts/prepared-materials/PedestrianDraw01/constant-control/native-1x1'
[IO.Directory]::CreateDirectory($out)|Out-Null
# New diagnostic constant only: no encoder and no rewrite of any existing asset.
$bytes=[byte[]]@(5,0,1,0,1,0,1,0,1,8,2,1,0,8,255,255,255,255,0,0,0,255)
$constant=Join-Path $out 'OpaqueBlackControl.Texture'
if(Test-Path -LiteralPath $constant){
    if([Convert]::ToBase64String([IO.File]::ReadAllBytes($constant)) -ne [Convert]::ToBase64String($bytes)){throw 'Diagnostic constant differs.'}
} else {[IO.File]::WriteAllBytes($constant,$bytes)}
Copy-Item -LiteralPath "$PSScriptRoot/ConstantControlProbe.Editor.cs" -Destination "$root/unity/Assets/Editor/PedestrianConstantControlProbe.cs"
Copy-Item -LiteralPath "$PSScriptRoot/ConstantControlProbe.compute" -Destination "$root/unity/Assets/ConstantControlProbe.compute"
$env:TEMP="$root/cache/temp"; $env:TMP=$env:TEMP; $env:UPM_CACHE_ROOT="$root/cache/upm"
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$root+'/unity"'),'-executeMethod','PedestrianConstantControlProbe.Run','-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"','-referenceTexture',('"'+$Repo+'/artifacts/prepared-repeat/PedestrianDraw01/products/839cae063c8a2be86808535445195d41b5770c016ba47a1efa0666ff04b09b5a.Texture"'),'-candidateTexture',('"'+$out+'/OpaqueBlackControl.Texture"'),'-transparentTexture',('"'+$Repo+'/assets/Materials/PedestrianDraw01/Cable/ZeroControl.Texture"'),'-probeReport',('"'+$out+'/probe-report.json"'),'-quit','-logFile',('"'+$out+'/probe.log"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Probe PID=$($process.Id)"
$process.WaitForExit()
Select-String -LiteralPath "$out/probe.log" -Pattern '^PASS constant|^SAMPLES|error CS|^Exception|Shader error' | ForEach-Object Line
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$out/probe.log" -Pattern 'PASS constant control native probe:' -Quiet)){throw 'Probe failed'}
