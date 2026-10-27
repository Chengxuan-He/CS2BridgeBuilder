param([string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data',
      [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe',
      [string]$Python='C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'artifacts/agent-material-env/Draw01/unity'
$evidence=Join-Path $repo 'artifacts/prepared-materials/Draw01'
& $Python "$PSScriptRoot/ExportAliasProbe.py"
if($LASTEXITCODE -ne 0){throw 'Alias input extraction failed.'}
Copy-Item -LiteralPath "$PSScriptRoot/StructureAliasProbe.Editor.cs" -Destination "$project/Assets/Editor/StructureAliasProbe.cs"
Copy-Item -LiteralPath "$PSScriptRoot/StructureAliasProbe.compute" -Destination "$project/Assets/StructureAliasProbe.compute"
$probeArgs=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','Draw01StructureAliasProbe.Run',
    '-gameData',('"'+$GameData+'"'),'-aliasInput',('"'+$evidence+'/alias-filter-input.json"'),
    '-aliasOutput',('"'+$evidence+'/alias-filter-results.json"'),'-quit','-logFile',('"'+$evidence+'/alias-filter.log"'))
$process=Start-Process -FilePath $Unity -ArgumentList $probeArgs -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$evidence/alias-filter.log" -Pattern 'PASS Draw01 alias native sampling:' -Quiet)){throw 'Native alias probe failed.'}
& $Python "$PSScriptRoot/SummarizeAliasProbe.py"
if($LASTEXITCODE -ne 0){throw 'Alias summary failed.'}
