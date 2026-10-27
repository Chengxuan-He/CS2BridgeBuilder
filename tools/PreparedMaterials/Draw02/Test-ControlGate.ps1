[CmdletBinding()]
param([string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'artifacts/agent-material-env/Draw02/unity'
$out=Join-Path $repo 'artifacts/prepared-materials/Draw02'
$shader=Join-Path $repo 'tools/GpuBake/BridgeBake.compute'
$text=[IO.File]::ReadAllText($shader)
$declaration='int AtlasTile, SkipAtlasControl;'
$write='if(SkipAtlasControl==0) AtlasControl[pixel]=_ControlMask.SampleLevel(sampler_ControlMask,uv,mip);'
if(($text.Split(@($declaration),[StringSplitOptions]::None).Length -ne 2) -or ($text.Split(@($write),[StringSplitOptions]::None).Length -ne 2)){throw 'Expected exactly the two reviewed shader changes.'}
$old=$text.Replace($declaration,'int AtlasTile;').Replace($write,'AtlasControl[pixel]=_ControlMask.SampleLevel(sampler_ControlMask,uv,mip);')
Copy-Item -LiteralPath $shader -Destination "$project/Assets/ControlGateCurrent.compute" -Force
[IO.File]::WriteAllText("$project/Assets/ControlGateOld.compute",$old,[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath "$PSScriptRoot/ControlGateProbe.Editor.cs" -Destination "$project/Assets/Editor/ControlGateProbe.cs" -Force
$report="$out/control-gate-gpu.json"
$log="$out/control-gate-gpu.log"
$args=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','ControlGateProbe.Run','-gateReport',('"'+$report+'"'),'-logFile',('"'+$log+'"'))
$p=Start-Process -FilePath $Unity -ArgumentList $args -PassThru -WindowStyle Hidden
$p.WaitForExit()
$evidence=[ordered]@{source=$shader;sourceSha256=(Get-FileHash $shader).Hash;oldSha256=(Get-FileHash "$project/Assets/ControlGateOld.compute").Hash;probeSha256=(Get-FileHash "$PSScriptRoot/ControlGateProbe.Editor.cs").Hash;exitCode=$p.ExitCode;report=$report;log=$log}
$evidence|ConvertTo-Json|Set-Content "$out/control-gate-evidence.json" -Encoding UTF8
if($p.ExitCode -ne 0){throw "GPU probe failed: exit $($p.ExitCode); see $log"}
if(!(Get-Content $report -Raw|ConvertFrom-Json).passed){throw 'GPU report failed.'}
$evidence|ConvertTo-Json
