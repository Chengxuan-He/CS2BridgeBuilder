param([string]$ResultRoot)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'artifacts/agent-material-env/Draw02/unity'
$result=if($ResultRoot){[IO.Path]::GetFullPath($ResultRoot)}else{Join-Path $repo 'artifacts/prepared-materials/Draw02/repeat-units-v2/trial-result-corrected'}
Copy-Item -LiteralPath "$PSScriptRoot/RepeatUnitCheck.Editor.cs" -Destination "$project/Assets/Editor/Draw02RepeatUnitCheck.cs"
Copy-Item -LiteralPath "$PSScriptRoot/RepeatUnitCheck.compute" -Destination "$project/Assets/Draw02RepeatUnitCheck.compute"
foreach($name in @('PrepareRepeatUnits','PreparePaint')) {
 Copy-Item -LiteralPath "$repo/tools/GpuBake/$name.Editor.cs" -Destination "$project/Assets/Editor/$name.cs"
}
$source=[IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/MovableTextureSource.cs")
$source=$source.Replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {')+"`n}"
$source=$source.Replace('UnityEngine.Object.Destroy(','UnityEngine.Object.DestroyImmediate(')
[IO.File]::WriteAllText("$project/Assets/Editor/MovableTextureSource.cs",$source)
$log=Join-Path $result 'native-check.log'
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','Draw02RepeatUnitCheck.Run',
 '-unitRoot',('"'+$result+'"'),'-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"','-quit','-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Draw02 repeat readback PID=$($process.Id)"
$process.WaitForExit()
[ordered]@{exitCode=$process.ExitCode;readerSha256=(Get-FileHash "$PSScriptRoot/RepeatUnitCheck.Editor.cs").Hash;shaderSha256=(Get-FileHash "$PSScriptRoot/RepeatUnitCheck.compute").Hash;log=$log}|ConvertTo-Json|Set-Content "$result/native-check-exit.json"
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern '^PASS Draw02 native repeat candidates:' -Quiet)){throw "Draw02 native check failed: $log"}
Select-String -LiteralPath $log -Pattern '^PASS Draw02 native'|ForEach-Object Line
