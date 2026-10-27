$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'artifacts/agent-material-env/Draw02/unity'
$out=Join-Path $repo 'artifacts/prepared-materials/Draw02/shared-source-migration'
Copy-Item -LiteralPath "$PSScriptRoot/SharedSourceProbe.Editor.cs" -Destination "$project/Assets/Editor/Draw02SharedSourceProbe.cs"
Copy-Item -LiteralPath "$PSScriptRoot/SharedSourceProbe.compute" -Destination "$project/Assets/Draw02SharedSourceProbe.compute"
$args=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','Draw02SharedSourceProbe.Run',
'-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"',
'-inventory',('"'+$repo+'/artifacts/uv-density/structure-tessellated-sources.json"'),
'-representative',('"'+$out+'/representative.json"'),'-report',('"'+$out+'/representative-native.json"'),'-quit','-logFile',('"'+$out+'/representative-native.log"'))
$p=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $args -WindowStyle Hidden -PassThru
$p.WaitForExit()
[ordered]@{exitCode=$p.ExitCode;probeSha256=(Get-FileHash "$PSScriptRoot/SharedSourceProbe.Editor.cs").Hash;shaderSha256=(Get-FileHash "$PSScriptRoot/SharedSourceProbe.compute").Hash}|ConvertTo-Json|Set-Content "$out/representative-evidence.json"
if($p.ExitCode -ne 0 -or !(Select-String -LiteralPath "$out/representative-native.log" -Pattern '^PASS Draw02 shared source representative:' -Quiet)){throw 'Representative native check failed.'}
Select-String -LiteralPath "$out/representative-native.log" -Pattern '^PASS Draw02 shared source representative:'|ForEach-Object Line
