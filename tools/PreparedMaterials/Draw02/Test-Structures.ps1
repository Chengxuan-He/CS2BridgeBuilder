$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'artifacts/agent-material-env/Draw02/unity'
$log=Join-Path $repo 'artifacts/prepared-materials/Draw02/structure-integration-native.log'
$source=[IO.File]::ReadAllText("$repo/tools/GpuBake/PreparedNativeCheck.Editor.cs")
$old='Directory.GetFiles(structuralRoot, "*.Texture", SearchOption.AllDirectories)'
if(!$source.Contains($old)){throw 'Review native checker dependency lookup.'}
# The integrated layout keeps dependencies beside Structure in style-local Shared.
$source=$source.Replace($old,'Directory.GetFiles(Path.Combine(Path.GetDirectoryName(structuralRoot), "Shared"), "*.Texture", SearchOption.AllDirectories)')
$source=$source.Replace('foreach (var file in textureFiles)','foreach (var file in textureFiles.Distinct())')
[IO.File]::WriteAllText("$project/Assets/Editor/PreparedNativeCheck.cs",$source)
$args=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','PreparedNativeCheck.Run',
'-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"',
'-preparedPaint',('"'+$repo+'/assets/Materials/Draw02/Paint"'),
'-preparedStructures',('"'+$repo+'/assets/Materials/Draw02/Structure"'),'-quit','-logFile',('"'+$log+'"'))
$p=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $args -WindowStyle Hidden -PassThru
$p.WaitForExit()
[ordered]@{exitCode=$p.ExitCode;nativeCheckerSha256=(Get-FileHash "$project/Assets/Editor/PreparedNativeCheck.cs").Hash;log=$log}|ConvertTo-Json|Set-Content "$repo/artifacts/prepared-materials/Draw02/structure-integration-native.json"
if($p.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern '^PASS native prepared readers:' -Quiet)){throw "Native validation failed: $log"}
Select-String -LiteralPath $log -Pattern '^PASS native prepared readers:'|ForEach-Object Line
