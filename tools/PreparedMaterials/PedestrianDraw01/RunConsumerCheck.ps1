param([Parameter(Mandatory)][string]$Candidate,
      [Parameter(Mandatory)][string]$EnvironmentRoot,
      [Parameter(Mandatory)][string]$GameData,
      [string]$ReportName='native-cells-consumer',
      [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$private=[IO.Path]::GetFullPath($EnvironmentRoot)
$stage=Join-Path $repo 'artifacts/material-remap/PedestrianDraw01'
$native=(Get-Content -Raw (Join-Path $repo 'artifacts/layer-decoupling/PedestrianDraw01/outputs/native-repeat-mechanism/audit.json') | ConvertFrom-Json).farMaterial
$archive=[IO.Compression.ZipFile]::OpenRead($native.archive)
try {
 $stream=$archive.GetEntry($native.entry).Open()
 try { $output=[IO.File]::Create("$stage/native-far.Surface");try {$stream.CopyTo($output)}finally{$output.Dispose()} } finally {$stream.Dispose()}
} finally {$archive.Dispose()}
if((Get-FileHash -LiteralPath "$stage/native-far.Surface").Hash -ne $native.sha256){throw 'Native far Surface hash changed'}
Copy-Item -LiteralPath "$PSScriptRoot/PreparedRoadConsumerCheck.Editor.cs" -Destination "$private/unity/Assets/Editor/PreparedRoadConsumerCheck.cs"
$env:TEMP="$private/cache/temp"
$env:TMP="$private/cache/temp"
$env:UPM_CACHE_ROOT="$private/cache/upm"
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$private+'/unity"'),'-executeMethod','PedestrianPreparedRoadConsumerCheck.Run',
 '-gameManaged',('"'+[IO.Path]::GetFullPath($GameData)+'/Managed"'),'-modAssembly',('"'+[IO.Path]::GetFullPath($Candidate)+'"'),
 '-assetRoot',('"'+$repo+'/assets"'),'-nearSurface',('"'+$repo+'/artifacts/prepared-materials/PedestrianDraw01/repeat-units-v2/native-sources/15c63410efe103a46b109b0dd6aa246d.Surface"'),
 '-cells',('"'+$stage+'/native-road-cells.json"'),'-farSurface',('"'+$stage+'/native-far.Surface"'),'-report',('"'+$stage+'/'+$ReportName+'.json"'),
 '-logFile',('"'+$private+'/logs/'+$ReportName+'.log"'),'-quit')
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
Copy-Item -LiteralPath "$private/logs/$ReportName.log" -Destination "$stage/$ReportName.log"
$report=Get-Content -Raw "$stage/$ReportName.json" | ConvertFrom-Json
$report | Add-Member -NotePropertyName candidateSHA256 -NotePropertyValue (Get-FileHash -LiteralPath $Candidate).Hash
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath "$stage/$ReportName.json" -Encoding utf8
if($process.ExitCode -ne 0){throw "Consumer replay failed: $stage/$ReportName.log"}
Select-String -LiteralPath "$stage/$ReportName.log" -Pattern 'PASS PedestrianDraw01'
