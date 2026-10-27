param([string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data',
      [string]$Python='C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$privateEnv=Join-Path $repo 'artifacts/agent-material-env/Draw01'
$run=Join-Path $repo 'artifacts/prepared-materials/Draw01/producer-control-validation'
& "$repo/tools/Prepare-Repeats.ps1" -ProjectPath "$privateEnv/unity" -GameData $GameData -Inventory "$repo/artifacts/prepared-repeat-draw01/inventory.json" -Output "$run/output" -Report "$run/bake.json" -LogPath "$run/producer.log" -ProductStore "$run/products" -ReadOnlyProductStores @("$privateEnv/products") -ReusePipelineReport "$privateEnv/reports/bake.json" -ReuseTexturesOnly
& "$repo/tools/PreparedMaterials/Draw03/Test-Repeats.ps1" -ProjectPath "$privateEnv/unity" -GameManaged "$GameData/Managed" -Inventory "$repo/artifacts/prepared-repeat-draw01/inventory.json" -Output "$run/output" -Report "$run/bake.json" -LogPath "$run/native.log"
& $Python "$PSScriptRoot/VerifyProducerControl.py"
if($LASTEXITCODE -ne 0){throw 'Draw01 producer byte comparison failed.'}
