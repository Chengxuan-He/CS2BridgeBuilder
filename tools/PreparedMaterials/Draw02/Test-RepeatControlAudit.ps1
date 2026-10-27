param([Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$root=Join-Path $repo 'artifacts/agent-material-env/Draw02'
$outputPath=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $outputPath){throw 'Choose a fresh audit directory.'}
[IO.Directory]::CreateDirectory($outputPath)|Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/RepeatControlAudit.Editor.cs" -Destination "$root/unity/Assets/Editor/Draw02RepeatControlAudit.cs"
Copy-Item -LiteralPath "$PSScriptRoot/RepeatControlAudit.compute" -Destination "$root/unity/Assets/Draw02RepeatControlAudit.compute"
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$root+'/unity"'),'-executeMethod','Draw02RepeatControlAudit.Run',
'-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"',
'-repeatInventory',('"'+$repo+'/artifacts/prepared-materials/Draw02/repeat-inventory.json"'),
'-styleRoot',('"'+$repo+'/assets/Materials/Draw02"'),'-candidateOutput',('"'+$outputPath+'"'),
'-quit','-logFile',('"'+$outputPath+'/native-audit.log"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
[ordered]@{exitCode=$process.ExitCode;probeSha256=(Get-FileHash "$PSScriptRoot/RepeatControlAudit.Editor.cs").Hash;shaderSha256=(Get-FileHash "$PSScriptRoot/RepeatControlAudit.compute").Hash}|ConvertTo-Json|Set-Content "$outputPath/evidence.json"
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$outputPath/native-audit.log" -Pattern '^PASS Draw02 repeat control audit:' -Quiet)){throw 'Native source audit failed.'}
Get-Content "$outputPath/audit.json"
