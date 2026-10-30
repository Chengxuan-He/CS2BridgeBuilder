[CmdletBinding()]
param([string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$private=Join-Path $repo 'artifacts/material-remap/environments/Lift01'
$output=Join-Path $repo 'artifacts/material-remap/Lift01/far-finite'
$env:TEMP="$private/cache/temp"
$env:TMP=$env:TEMP
$env:UPM_CACHE_ROOT="$private/cache/upm"
$project="$private/unity"
$pipeline=(@(Get-Item "$project/Assets/Editor/PrepareRepeatUnits.cs","$project/Assets/RepeatUnit.compute","$project/Assets/Lift01FiniteTrapezoid.hlsl") | ForEach-Object {(Get-FileHash -LiteralPath $_.FullName).Hash}) -join ':'
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','PrepareRepeatUnits.Run','-style','Lift01','-unitPipeline',$pipeline)
foreach($pair in @(@('-unitManifest',"$output/private-manifest.json"),@('-unitOutput',"$output/candidate-v2"),@('-unitStore',"$private/cache/far-products"),@('-unitReport',"$output/report-v2.json"),@('-preparedMaterials',"$repo/assets/Materials"),@('-gameData','D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'),@('-logFile',"$private/logs/far-finite-bake-v2.log"))) {
 $arguments+=@($pair[0],('"'+$pair[1]+'"'))
}
$arguments+='-quit'
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$private/logs/far-finite-bake-v2.log" -Pattern 'PASS genuine repeat units:' -Quiet)){throw 'Lift01 finite candidate preparation failed.'}
Write-Output 'PASS candidate serialization only; this is not production qualification.'
