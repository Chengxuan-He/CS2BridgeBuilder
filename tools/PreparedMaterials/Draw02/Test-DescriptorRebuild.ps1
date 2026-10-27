$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'artifacts/agent-material-env/Draw02/unity'
$work=Join-Path $repo 'artifacts/agent-material-env/Draw02/rebuild-native-v1'
$evidence=Join-Path $repo 'artifacts/prepared-materials/Draw02/rebuild-native-v1'
$source=[IO.File]::ReadAllText("$repo/tools/GpuBake/PreparedNativeCheck.Editor.cs")
$source=$source.Replace('new[] { "Paint", "Cable", "Road" }','new[] { "Cable", "Road" }')
$old='File.ReadAllText(Path.Combine(variantFolder, item.Item2 + ".cid")).Trim()'
if(!$source.Contains($old)){throw 'Review native checker zero-control identity verification.'}
$source=$source.Replace($old,'File.ReadAllText(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(folder), "Shared"), "*.Texture.cid").Single()).Trim()')
[IO.File]::WriteAllText("$project/Assets/Editor/PreparedNativeCheck.cs",$source)
$log=Join-Path $evidence 'native-reader.log'
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','PreparedNativeCheck.Run',
'-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"',
'-preparedPaint',('"'+$work+'/output/Draw02/Paint"'),'-quit','-logFile',('"'+$log+'"'))
$p=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Draw02 native reader PID=$($p.Id)"
$p.WaitForExit()
[ordered]@{exitCode=$p.ExitCode;nativeCheckerSha256=(Get-FileHash "$project/Assets/Editor/PreparedNativeCheck.cs").Hash;log=$log}|ConvertTo-Json|Set-Content "$evidence/native-reader.json"
if($p.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern '^PASS native prepared readers:' -Quiet)){throw "Native validation failed: $log"}
Select-String -LiteralPath $log -Pattern '^PASS native prepared readers:'|ForEach-Object Line
