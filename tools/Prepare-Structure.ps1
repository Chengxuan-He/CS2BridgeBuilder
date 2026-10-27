[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameData,
    [Parameter(Mandatory)][string]$Inventory,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$Report,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$StorePath,
    [string[]]$ReadOnlyProductStores = @(),
    [string]$ReusePipelineReport,
    [switch]$ReuseOnly,
    [string]$Style,
    [string]$Unity = 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = [IO.Path]::GetFullPath($ProjectPath)
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Use the dedicated Unity GPU preparation project.' }
$settings = Join-Path $project 'ProjectSettings/ProjectSettings.asset'
$settingsText = [IO.File]::ReadAllText($settings)
if ($settingsText -notmatch 'm_ActiveColorSpace: [01]') { throw 'Unity color-space setting is missing.' }
[IO.File]::WriteAllText($settings, ($settingsText -replace 'm_ActiveColorSpace: [01]', 'm_ActiveColorSpace: 1'))
foreach ($name in @('PreparePaint', 'PrepareStructure')) {
    Copy-Item -LiteralPath "$repo/tools/GpuBake/$name.Editor.cs" -Destination "$project/Assets/Editor/$name.cs"
}
Copy-Item -LiteralPath "$repo/tools/GpuBake/PreparedBakeStore.cs" -Destination "$project/Assets/Editor/PreparedBakeStore.cs"
foreach ($name in @('MovableUvChart','MovableStructurePacking', 'MovableStructureKey', 'MovableTextureFootprint', 'MovableAtlasChannel', 'MovableVtTextureSource', 'MovableTextureSource')) {
    $code = [IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/$name.cs")
    $code = $code.Replace('namespace BridgeBuilder.Bridges;', 'namespace BridgeBuilder.Bridges {') + "`n}"
    $code = $code.Replace('UnityEngine.Object.Destroy(', 'UnityEngine.Object.DestroyImmediate(')
    # Authoring must fingerprint decoded channel bytes before choosing an existing
    # product. Keep the identical upload bytes readable in this offline copy only.
    if ($name -eq 'MovableVtTextureSource') {
        if (!$code.Contains('texture.Apply(false,true)')) { throw 'VT source upload changed; review authoring fingerprint access.' }
        $code = $code.Replace('texture.Apply(false,true)', 'texture.Apply(false,false)')
    }
    if ($name -eq 'MovableTextureSource') {
        if (!$code.Contains('makeNoLongerReadable: true')) { throw 'Texture source upload changed; review authoring fingerprint access.' }
        $code = $code.Replace('makeNoLongerReadable: true', 'makeNoLongerReadable: false')
    }
    [IO.File]::WriteAllText("$project/Assets/Editor/$name.cs", $code)
}
Copy-Item -LiteralPath "$repo/tools/GpuBake/BridgeBake.compute" -Destination "$project/Assets/StructureBake.compute"
# Every output-affecting conversion implementation participates in the offline key.
# Paths and timestamps do not; byte-identical inputs remain reusable across runs/styles.
$pipelineFiles = @('PreparePaint.cs','PrepareStructure.cs','PreparedBakeStore.cs','MovableStructurePacking.cs',
    'MovableStructureKey.cs','MovableTextureFootprint.cs','MovableAtlasChannel.cs','MovableVtTextureSource.cs','MovableTextureSource.cs')
# MovableUvChart is a compile dependency of runtime selection only; the offline producer never calls it.
. "$PSScriptRoot/Get-PreparedPipelineHashes.ps1"
$pipelinePaths = @($pipelineFiles | ForEach-Object { "$project/Assets/Editor/$_" })
$pipelinePaths += "$project/Assets/StructureBake.compute"
$pipelinePaths += Join-Path $GameData 'Plugins/x86_64/PipelinePlugin.dll'
$pipelineHashes = @(Get-PreparedPipelineHashes -Paths $pipelinePaths -ReusePipelineReport $ReusePipelineReport)
if (!$StorePath) { $StorePath = Join-Path $repo 'artifacts/prepared-structure-products' }
$log = [IO.Path]::GetFullPath($LogPath)
foreach ($path in @($LogPath,$Report)) { [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($path))) | Out-Null }
$arguments = @('-batchmode', '-force-d3d11', '-projectPath', ('"'+$project+'"'), '-executeMethod', 'PrepareStructure.Run',
    '-gameData', ('"'+[IO.Path]::GetFullPath($GameData)+'"'), '-structureInventory', ('"'+[IO.Path]::GetFullPath($Inventory)+'"'),
    '-preparedOutput', ('"'+[IO.Path]::GetFullPath($Output)+'"'), '-preparationReport', ('"'+[IO.Path]::GetFullPath($Report)+'"'),
    '-preparedStore', ('"'+[IO.Path]::GetFullPath($StorePath)+'"'), '-pipelineKey', ($pipelineHashes -join ':'),
    '-quit', '-logFile', ('"'+$log+'"'))
if ($Style) { $arguments += @('-style', $Style) }
if ($ReadOnlyProductStores.Count) { $arguments += @('-readOnlyStores', ('"'+(($ReadOnlyProductStores | ForEach-Object { [IO.Path]::GetFullPath($_) }) -join '|')+'"')) }
if ($ReuseOnly) { $arguments += '-reuseOnly' }
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'PASS prepared structures:' -Quiet)) { throw "Structure preparation failed; see $log" }
Select-String -LiteralPath $log -Pattern 'PASS prepared structures:' | ForEach-Object Line
