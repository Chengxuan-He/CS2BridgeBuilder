[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Python,
    [Parameter(Mandatory)][string]$GameData,
    [Parameter(Mandatory)][string]$NativeAudit,
    [Parameter(Mandatory)][string]$StructuralInventory,
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$NativeMesh,
    [string[]]$ReadOnlyProductStores = @()
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$work = Join-Path $repo 'artifacts/prepared-repeat-draw01'
$inventory = Join-Path $work 'inventory.json'
$output = Join-Path $work 'output'

& $Python (Join-Path $PSScriptRoot 'ExportRecipes.py') $repo $GameData $NativeAudit $StructuralInventory $inventory
if ($LASTEXITCODE -ne 0) { throw 'Draw01 repeat recipe extraction failed.' }
& $Python (Join-Path $PSScriptRoot 'VerifyRecipes.py') $repo $NativeMesh $inventory (Join-Path $work 'source-density.json')
if ($LASTEXITCODE -ne 0) { throw 'Draw01 native source-density verification failed.' }

# The caller supplies an independent Draw01 Unity project. No shared project,
# game, installation or other bridge's product store is modified.
& (Join-Path $repo 'tools/Prepare-Repeats.ps1') -ProjectPath $ProjectPath -GameData $GameData `
    -Inventory $inventory -Output $output -Report (Join-Path $work 'bake.json') `
    -LogPath (Join-Path $work 'bake.log') -ProductStore (Join-Path $work 'products') `
    -ReadOnlyProductStores $ReadOnlyProductStores
& (Join-Path $repo 'tools/Test-PreparedNative.ps1') -ProjectPath $ProjectPath `
    -GameManaged (Join-Path $GameData 'Managed') -Structures (Join-Path $output 'Draw01') `
    -LogPath (Join-Path $work 'native.log')

Write-Host "Draw01 repeat outputs passed native reader checks: $output/Draw01"
Write-Host 'The coordinator must verify package integration and deploy before human acceptance.'
