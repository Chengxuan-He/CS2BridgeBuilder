param([string]$GameManaged='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed',
      [string]$Python='C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$evidence=Join-Path $repo 'artifacts/prepared-materials/Draw01'
$dll=Join-Path $repo 'src/BridgeBuilder/bin/Release/BridgeBuilder.dll'
$before=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
& $Python "$PSScriptRoot/ExportReceiverProbe.py"
if($LASTEXITCODE -ne 0){throw 'Receiver observer creation failed.'}
& dotnet run --project "$repo/artifacts/agent-material-env/Draw01/receiver-probe/Draw01ReceiverProbe.csproj" -c Release "-p:GameManagedDir=$GameManaged" -- $dll $GameManaged "$repo/artifacts/uv-density/layout-plans.bin" "$evidence/receiver-provenance-layout.json" --probe-parent-samplers *> "$evidence/receiver-provenance-layout.log"
if($LASTEXITCODE -ne 0){throw 'Production receiver observation failed.'}
if((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash -ne $before){throw 'Main DLL changed during observation; evidence must be rerun against a stable build.'}
$previousPythonPath=$env:PYTHONPATH
try {
    $env:PYTHONPATH="$repo/artifacts/agent-material-env/Draw01/python-deps"
    & $Python "$PSScriptRoot/GroupStructureUnits.py" --receivers "$evidence/receiver-provenance-layout.json.receivers.json" --output "$evidence/structure-provenance-units" --no-integrate
    if($LASTEXITCODE -ne 0){throw 'Receiver containment analysis failed.'}
    & $Python "$repo/tools/PrepareRepeatUnits/AuditStructureUnits.py" "$evidence/structure-provenance-units/manifest.json" "$evidence/structure-provenance-units/audit.json"
    if($LASTEXITCODE -ne 0){throw 'Receiver grouping audit failed.'}
} finally { $env:PYTHONPATH=$previousPythonPath }
@{dll=$dll;dllSha256=$before;fixtureSha256=(Get-FileHash "$repo/artifacts/uv-density/layout-plans.bin" -Algorithm SHA256).Hash;
    receiverSha256=(Get-FileHash "$evidence/receiver-provenance-layout.json.receivers.json" -Algorithm SHA256).Hash;
    scope='Finite production-plan receiver evidence; actual Retile emission and saved-owner graph remain separate checks.'} |
    ConvertTo-Json | Set-Content -LiteralPath "$evidence/receiver-provenance-run.json" -Encoding UTF8
