[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameData,
    [Parameter(Mandatory)][string]$NativeAudit,
    [Parameter(Mandatory)][string]$Python,
    [string[]]$ReadOnlyProductStores = @()
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$work=Join-Path $repo 'artifacts/prepared-materials/Draw02'
$inventory=Join-Path $work 'repeat-inventory.json'
& $Python "$PSScriptRoot/ExportRecipes.py" $repo $GameData $NativeAudit "$repo/artifacts/uv-density/structure-seam-sources.json" $inventory
if($LASTEXITCODE -ne 0){throw 'Draw02 repeat extraction failed.'}
& $Python "$PSScriptRoot/VerifyRecipes.py" $repo "$repo/artifacts/uv-density/native/DrawBridge02 Mesh.npz" $inventory "$work/source-density.json"
if($LASTEXITCODE -ne 0){throw 'Draw02 source-density verification failed.'}
& "$repo/tools/Prepare-Repeats.ps1" -ProjectPath $ProjectPath -GameData $GameData -Inventory $inventory `
    -Output "$work/repeat-bake" -Report "$work/repeat-bake.json" -LogPath "$work/repeat-bake.log" `
    -ProductStore "$work/products" -ReadOnlyProductStores $ReadOnlyProductStores
if(!$?){throw 'Draw02 repeat preparation failed.'}
& "$repo/tools/PreparedMaterials/Draw03/Test-Repeats.ps1" -ProjectPath $ProjectPath -GameManaged "$GameData/Managed" `
    -Inventory $inventory -Output "$work/repeat-bake" -Report "$work/repeat-bake.json" -LogPath "$work/repeat-native.log"
if(!$?){throw 'Draw02 native-repeat verification failed.'}
