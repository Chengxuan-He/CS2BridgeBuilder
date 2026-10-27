param([Parameter(Mandatory)][string]$Output,
      [string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data',
      [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$root=Join-Path $repo 'artifacts/agent-material-env/Draw01'
$candidate=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $candidate){throw 'Choose a new candidate output; do not overwrite existing evidence.'}
[IO.Directory]::CreateDirectory($candidate) | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/RepeatControlAudit.Editor.cs" -Destination "$root/unity/Assets/Editor/Draw01RepeatControlAudit.cs"
Copy-Item -LiteralPath "$PSScriptRoot/ControlZero.compute" -Destination "$root/unity/Assets/Draw01ControlZero.compute"
$probeArgs=@('-batchmode','-force-d3d11','-projectPath',('"'+$root+'/unity"'),'-executeMethod','Draw01RepeatControlAudit.Run',
    '-gameData',('"'+$GameData+'"'),'-repeatInventory',('"'+$repo+'/artifacts/prepared-repeat-draw01/inventory.json"'),
    '-styleRoot',('"'+$repo+'/assets/Materials/Draw01"'),'-candidateOutput',('"'+$candidate+'"'),
    '-quit','-logFile',('"'+$candidate+'/native-audit.log"'))
$process=Start-Process -FilePath $Unity -ArgumentList $probeArgs -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$candidate/native-audit.log" -Pattern 'PASS repeat control source audit:' -Quiet)){throw "Native Draw01 control audit failed: $candidate/native-audit.log"}
Select-String -LiteralPath "$candidate/native-audit.log" -Pattern 'PASS repeat control source audit:' | ForEach-Object Line
