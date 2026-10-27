[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameData,
    [Parameter(Mandatory)][string]$NativeAudit,
    [Parameter(Mandatory)][string]$StructuralInventory,
    [string]$Python = 'python',
    [string]$BakerTools,
    [string[]]$ReadOnlyProductStores = @(),
    [switch]$Publish
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if (!$BakerTools) { $BakerTools = Join-Path $repo 'tools' }
$BakerTools = [IO.Path]::GetFullPath($BakerTools)
$artifacts = Join-Path $repo 'artifacts/prepared-materials/Lift01'
$inventory = Join-Path $artifacts 'repeat-recipes.json'
$output = Join-Path $artifacts 'baked'
& $Python (Join-Path $PSScriptRoot 'ExportRecipes.py') $repo $GameData $NativeAudit $StructuralInventory $inventory
if ($LASTEXITCODE -ne 0) { throw 'Lift01 immutable recipe extraction failed.' }
& $Python (Join-Path $PSScriptRoot 'VerifyDensity.py') $repo $GameData $inventory `
    (Join-Path $artifacts 'repeat-density.json') `
    --native-mesh (Join-Path $repo 'artifacts/uv-density/native/LiftBridge01 Mesh.npz')
if ($LASTEXITCODE -ne 0) { throw 'Lift01 original-density verification failed.' }
& (Join-Path $BakerTools 'Prepare-Repeats.ps1') -ProjectPath $ProjectPath -GameData $GameData `
    -Inventory $inventory -Output $output -Report (Join-Path $artifacts 'repeat-bake.json') `
    -LogPath (Join-Path $artifacts 'repeat-bake.log') -ProductStore (Join-Path $artifacts 'products') `
    -ReadOnlyProductStores $ReadOnlyProductStores
& (Join-Path $BakerTools 'PreparedMaterials/Draw03/Test-Repeats.ps1') -ProjectPath $ProjectPath `
    -GameManaged (Join-Path $GameData 'Managed') -Inventory $inventory -Output $output `
    -Report (Join-Path $artifacts 'repeat-bake.json') `
    -LogPath (Join-Path $artifacts 'repeat-native.log')
if (!$Publish) { return }
$source = Join-Path $output 'Lift01'
$target = Join-Path $repo 'assets/Materials/Lift01'
if (!(Test-Path -LiteralPath $source -PathType Container)) { throw 'Lift01 baked payloads are missing.' }
$files = @(Get-ChildItem -LiteralPath $source -File -Recurse)
if (!$files.Count) { throw 'No Lift01 payloads to publish.' }
# Verify the entire publication set before copying. Existing resources remain immutable.
foreach ($file in $files) {
    if ($file.Extension -notin @('.Surface', '.Texture', '.cid')) { throw "Unexpected authoring file: $($file.FullName)" }
    if ($file.Extension -ne '.cid' -and !(Test-Path -LiteralPath ($file.FullName + '.cid') -PathType Leaf)) {
        throw "Missing native identity: $($file.FullName)"
    }
    $relative = $file.FullName.Substring($source.Length).TrimStart('\','/')
    $destination = Join-Path $target $relative
    if ((Test-Path -LiteralPath $destination) -and
        (Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) {
        throw "Existing Lift01 payload conflicts: $destination"
    }
}
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length).TrimStart('\','/')
    $destination = Join-Path $target $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    if (!(Test-Path -LiteralPath $destination)) { Copy-Item -LiteralPath $file.FullName -Destination $destination }
    if ((Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) {
        throw "Lift01 payload copy differs: $destination"
    }
}
Write-Output "PASS Lift01 prepared publication: $($files.Count) native payload/identity files, exact byte copies."
