param([string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data',
      [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'artifacts/agent-material-env/Draw01/unity'
$report=Join-Path $repo 'artifacts/prepared-materials/Draw01/source-sharing'
if(Test-Path -LiteralPath "$report/native-source.json"){throw 'Native evidence already exists; do not repeat this audit.'}
[IO.Directory]::CreateDirectory($report) | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/SourceTextureAudit.Editor.cs" -Destination "$project/Assets/Editor/Draw01SourceTextureAudit.cs"
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','Draw01SourceTextureAudit.Run',
    '-gameData',('"'+$GameData+'"'),'-inventory',('"'+$repo+'/artifacts/uv-density/structure-tessellated-sources.json"'),
    '-report',('"'+$report+'/native-source.json"'),'-quit','-logFile',('"'+$report+'/native-source.log"'))
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$report/native-source.log" -Pattern 'PASS Draw01 native shared source:' -Quiet)){throw 'Draw01 native source inspection failed.'}
Select-String -LiteralPath "$report/native-source.log" -Pattern 'PASS Draw01 native shared source:' | ForEach-Object Line
