[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameData,
    [Parameter(Mandatory)][string]$Inventory,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$Report,
    [Parameter(Mandatory)][string]$LogPath,
    [Parameter(Mandatory)][string]$ProductStore,
    [string[]]$ReadOnlyProductStores = @(),
    [string]$ReusePipelineReport,
    [switch]$ReuseOnly,
    [switch]$ReuseTexturesOnly,
    [string]$Unity = 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = [IO.Path]::GetFullPath($ProjectPath)
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Use an independent Unity preparation project.' }
$settings = Join-Path $project 'ProjectSettings/ProjectSettings.asset'
$settingsText = [IO.File]::ReadAllText($settings)
if ($settingsText -notmatch 'm_ActiveColorSpace: [01]') { throw 'Unity color-space setting is missing.' }
[IO.File]::WriteAllText($settings, ($settingsText -replace 'm_ActiveColorSpace: [01]', 'm_ActiveColorSpace: 1'))
foreach ($name in @('PreparePaint','PrepareStructure','PrepareRepeat')) {
    Copy-Item -LiteralPath "$repo/tools/GpuBake/$name.Editor.cs" -Destination "$project/Assets/Editor/$name.cs"
}
Copy-Item -LiteralPath "$repo/tools/GpuBake/PreparedBakeStore.cs" -Destination "$project/Assets/Editor/PreparedBakeStore.cs"
foreach ($name in @('MovableUvChart','MovableStructurePacking','MovableStructureKey','MovableTextureFootprint','MovableAtlasChannel','MovableVtTextureSource','MovableTextureSource')) {
    $code = [IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/$name.cs")
    $code = $code.Replace('namespace BridgeBuilder.Bridges;', 'namespace BridgeBuilder.Bridges {') + "`n}"
    $code = $code.Replace('UnityEngine.Object.Destroy(', 'UnityEngine.Object.DestroyImmediate(')
    if ($name -eq 'MovableVtTextureSource') {
        if (!$code.Contains('texture.Apply(false,true)')) { throw 'Review changed VT source upload.' }
        $code = $code.Replace('texture.Apply(false,true)', 'texture.Apply(false,false)')
    }
    if ($name -eq 'MovableTextureSource') {
        if (!$code.Contains('makeNoLongerReadable: true')) { throw 'Review changed texture source upload.' }
        $code = $code.Replace('makeNoLongerReadable: true', 'makeNoLongerReadable: false')
    }
    [IO.File]::WriteAllText("$project/Assets/Editor/$name.cs",$code)
}
Copy-Item -LiteralPath "$repo/tools/GpuBake/BridgeBake.compute" -Destination "$project/Assets/RepeatBake.compute"
$files = @('PreparePaint','PrepareStructure','PrepareRepeat','PreparedBakeStore','MovableStructurePacking','MovableStructureKey','MovableTextureFootprint','MovableAtlasChannel','MovableVtTextureSource','MovableTextureSource')
# MovableUvChart is a compile dependency of runtime selection only; the offline producer never calls it.
. "$PSScriptRoot/Get-PreparedPipelineHashes.ps1"
$pipelinePaths = @($files | ForEach-Object { "$project/Assets/Editor/$_.cs" })
$pipelinePaths += "$project/Assets/RepeatBake.compute"
$pipelinePaths += Join-Path $GameData 'Plugins/x86_64/PipelinePlugin.dll'
$hashes = @(Get-PreparedPipelineHashes -Paths $pipelinePaths -ReusePipelineReport $ReusePipelineReport)
foreach ($path in @($LogPath,$Report)) { [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($path))) | Out-Null }
$log = [IO.Path]::GetFullPath($LogPath)
$arguments = @('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','PrepareRepeat.Run',
    '-gameData',('"'+[IO.Path]::GetFullPath($GameData)+'"'),'-repeatInventory',('"'+[IO.Path]::GetFullPath($Inventory)+'"'),
    '-preparedOutput',('"'+[IO.Path]::GetFullPath($Output)+'"'),'-preparationReport',('"'+[IO.Path]::GetFullPath($Report)+'"'),
    '-preparedMaterials',('"'+$repo+'/assets/Materials"'),
    '-preparedStore',('"'+[IO.Path]::GetFullPath($ProductStore)+'"'),'-pipelineKey',($hashes -join ':'),'-quit','-logFile',('"'+$log+'"'))
if ($ReadOnlyProductStores.Count) { $arguments += @('-readOnlyStores',('"'+(($ReadOnlyProductStores | ForEach-Object { [IO.Path]::GetFullPath($_) }) -join '|')+'"')) }
if ($ReuseOnly) { $arguments += '-reuseOnly' }
if ($ReuseTexturesOnly) { $arguments += '-reuseTexturesOnly' }
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'PASS prepared repeats:' -Quiet)) { throw "Repeat preparation failed; see $log" }
Select-String -LiteralPath $log -Pattern 'PASS prepared repeats:' | ForEach-Object Line
