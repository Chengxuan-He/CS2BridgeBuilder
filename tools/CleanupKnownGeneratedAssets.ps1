[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if (Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue) {
    throw 'Cities: Skylines II is running. Exit the game before deleting generated bridge assets.'
}

$localLow = [Environment]::GetFolderPath('LocalApplicationData')
$localLow = Join-Path (Split-Path -Parent $localLow) 'LocalLow'
$gameRoot = Join-Path $localLow 'Colossal Order\Cities Skylines II'
$importedRoot = Join-Path $gameRoot 'ImportedData'
$modIds = @('BridgeBuilder', 'BridgePrefabGenerator')
$geometryRoots = @($modIds | ForEach-Object { Join-Path $gameRoot $_ })
$modDataRoots = @($modIds | ForEach-Object { Join-Path $gameRoot (Join-Path 'ModsData' $_) })
$stateFiles = @($modDataRoots | ForEach-Object { Join-Path $_ 'export-state.tsv' })
# Windows PowerShell 5.1 reads a BOM-less script using the current ANSI code page. Keep the script
# itself ASCII and decode the one non-ASCII road name explicitly so literal target names stay exact.
$roadName = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String('5Lik5Z2X5p2/5YWt6L2m6YGT'))
$currentRoadName = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String('5Lik5Z2X5p2/NCsy'))
$greenMainRoadName = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String('5LiJ6L2m6YGT5b+r6YCf6Lev5Li76Lev'))
$greenSideRoadName = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String('5LiJ6L2m6YGT5b+r6YCf6Lev6L6F6Lev'))
$wideBlueRoadName = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String('5Y+M5ZCRMSszKzMrMemrmOaetui3r+i+hei3rw=='))
$wideGreenRoadName = [Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String('5Y+M5ZCRMisyKzIrMumrmOaetui3r+i+hei3rw=='))
$vDoubleDeckName = "${greenMainRoadName}_BXP Quad Train Track_Extradosed01"

# This list was captured from export-state.tsv and then resolved against ImportedData. It is kept
# literal so cleanup cannot expand from a partial name and accidentally remove another mod's asset.
$importedDirectoryNames = @(
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 1",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 1 LOD1",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 1 LOD2",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 2",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 2 LOD1",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 2 LOD2",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh LOD1",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh LOD2",
    'RBBridgeDep_NetPiecePrefab_RB_Empty_Piece_2_c08cd07355',
    'RBBridgeDep_NetPiecePrefab_RB_Empty_Piece_Flat_2_46022b4728',
    'RBBridgeDep_NetPiecePrefab_RB_Empty_Piece_Middle_2_5889a2a3e0',
    'RBBridgeDep_NetPiecePrefab_RB_Empty_Piece_Middle_Flat_2_3e847d3068',
    'RBBridgeDep_NetPiecePrefab_RB_Median_Piece_5_68c5650e77',
    'RBBridgeDep_NetPiecePrefab_RB_Median_Piece_5_Grass_e486b7419b',
    'RBBridgeDep_NetPiecePrefab_RB_Median_Piece_5_Platform_7a7398f14a',
    'RBBridgeDep_NetSectionPrefab_RB_Empty_Section_2_bc8317e278',
    'RBBridgeDep_NetSectionPrefab_RB_Median_5_54a2b1d85d',
    "TrussArch01-40-${roadName}_TrussArch01",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh 1",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh 1 LOD1",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh 1 LOD2",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh LOD1",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh LOD2",
    "TrussArch03-40-${roadName}_TrussArch03",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh 1",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh 1 LOD1",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh 1 LOD2",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh LOD1",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh LOD2",
    'TrussArchBridge01 Section 30',
    'TrussArchBridge01 Section 30 Piece',
    'TrussArchBridge01 Section 30 Piece LOD1',
    'TrussArchBridge01 Section 30 Piece LOD2',
    'TrussArchBridge03 Section 16',
    'TrussArchBridge03 Section 16 Piece',
    'TrussArchBridge03 Section 16 Piece LOD1',
    'TrussArchBridge03 Section 16 Piece LOD2',
    "${roadName}_BXP Quad Train Track_Extradosed01",
    "${roadName}_BXP Quad Train Track_Extradosed01_Lower",
    "${roadName}_TrussArch01",
    "${roadName}_TrussArch03"
)
$importedDirectoryNames += @(
    "TrussArch01-40-${currentRoadName}_TrussArch01",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh 1",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh 1 LOD1",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh 1 LOD2",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh LOD1",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh LOD2",
    'TrussArchBridge01 Section 20',
    'TrussArchBridge01 Section 20 Piece',
    'TrussArchBridge01 Section 20 Piece LOD1',
    'TrussArchBridge01 Section 20 Piece LOD2',
    "${currentRoadName}_TrussArch01"
)
$importedDirectoryNames += @(
    "TrussArch03-16-${greenMainRoadName}_TrussArch03",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh 1",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh 1 LOD1",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh 1 LOD2",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh LOD1",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh LOD2",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh 1",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh 1 LOD1",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh 1 LOD2",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh LOD1",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh LOD2",
    'TrussArchBridge03 Section -8',
    'TrussArchBridge03 Section -8 Piece',
    'TrussArchBridge03 Section -8 Piece LOD1',
    'TrussArchBridge03 Section -8 Piece LOD2',
    'TrussArchBridge03 Section 4',
    'TrussArchBridge03 Section 4 Piece',
    'TrussArchBridge03 Section 4 Piece LOD1',
    'TrussArchBridge03 Section 4 Piece LOD2',
    "${greenMainRoadName}_TrussArch03",
    "${greenSideRoadName}_TrussArch03"
)
$importedDirectoryNames += @(
    "Extradosed01-16-${vDoubleDeckName}",
    "Extradosed01-16-${vDoubleDeckName} Mesh",
    "Extradosed01-16-${vDoubleDeckName} Mesh 1",
    "Extradosed01-16-${vDoubleDeckName} Mesh 1 LOD1",
    "Extradosed01-16-${vDoubleDeckName} Mesh 1 LOD2",
    "Extradosed01-16-${vDoubleDeckName} Mesh 2",
    "Extradosed01-16-${vDoubleDeckName} Mesh 2 LOD1",
    "Extradosed01-16-${vDoubleDeckName} Mesh 2 LOD2",
    "Extradosed01-16-${vDoubleDeckName} Mesh LOD1",
    "Extradosed01-16-${vDoubleDeckName} Mesh LOD2",
    'ExtradosedBridge01 Section -24',
    'ExtradosedBridge01 Section -24 Piece',
    'ExtradosedBridge01 Section -24 Piece LOD1',
    'ExtradosedBridge01 Section -24 Piece LOD2',
    'ExtradosedBridge01 Section -8',
    'ExtradosedBridge01 Section -8 Piece',
    'ExtradosedBridge01 Section -8 Piece LOD1',
    'ExtradosedBridge01 Section -8 Piece LOD2',
    $vDoubleDeckName,
    "${vDoubleDeckName}_Lower"
)
$importedDirectoryNames += @(
    "${greenMainRoadName}_TrussArch01",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh 1",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh 1 LOD1",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh 1 LOD2",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh LOD1",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh LOD2",
    "${wideBlueRoadName}_TrussArch01",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh 1",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh 1 LOD1",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh 1 LOD2",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh LOD1",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh LOD2",
    "${wideGreenRoadName}_TrussArch03",
    "${wideGreenRoadName}_TrussArch03 (1)",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh 1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh 1 LOD1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh 1 LOD2",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh LOD1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh LOD2",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1)",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh 1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh 1 LOD1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh 1 LOD2",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh LOD1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh LOD2"
)

# Exact mesh stems exported for the three bridges above. Only these two exact file extensions are
# derived; no wildcard or directory-wide removal is used.
$geometryStems = @(
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 1",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 1 LOD1",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 1 LOD2",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 2",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 2 LOD1",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh 2 LOD2",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh LOD1",
    "Extradosed01-40-${roadName}_BXP Quad Train Track_Extradosed01 Mesh LOD2",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh 1",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh 1 LOD1",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh 1 LOD2",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh LOD1",
    "TrussArch01-40-${roadName}_TrussArch01 Mesh LOD2",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh 1",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh 1 LOD1",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh 1 LOD2",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh LOD1",
    "TrussArch03-40-${roadName}_TrussArch03 Mesh LOD2",
    'TrussArchBridge01 Section 30 Piece',
    'TrussArchBridge01 Section 30 Piece LOD1',
    'TrussArchBridge01 Section 30 Piece LOD2',
    'TrussArchBridge03 Section 16 Piece',
    'TrussArchBridge03 Section 16 Piece LOD1',
    'TrussArchBridge03 Section 16 Piece LOD2'
)
$geometryStems += @(
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh 1",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh 1 LOD1",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh 1 LOD2",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh LOD1",
    "TrussArch01-40-${currentRoadName}_TrussArch01 Mesh LOD2",
    'TrussArchBridge01 Section 20 Piece',
    'TrussArchBridge01 Section 20 Piece LOD1',
    'TrussArchBridge01 Section 20 Piece LOD2'
)
$geometryStems += @(
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh 1",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh 1 LOD1",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh 1 LOD2",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh LOD1",
    "TrussArch03-16-${greenMainRoadName}_TrussArch03 Mesh LOD2",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh 1",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh 1 LOD1",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh 1 LOD2",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh LOD1",
    "TrussArch03-28-${greenSideRoadName}_TrussArch03 Mesh LOD2",
    'TrussArchBridge03 Section -8 Piece',
    'TrussArchBridge03 Section -8 Piece LOD1',
    'TrussArchBridge03 Section -8 Piece LOD2',
    'TrussArchBridge03 Section 4 Piece',
    'TrussArchBridge03 Section 4 Piece LOD1',
    'TrussArchBridge03 Section 4 Piece LOD2'
)
$geometryStems += @(
    "Extradosed01-16-${vDoubleDeckName} Mesh",
    "Extradosed01-16-${vDoubleDeckName} Mesh 1",
    "Extradosed01-16-${vDoubleDeckName} Mesh 1 LOD1",
    "Extradosed01-16-${vDoubleDeckName} Mesh 1 LOD2",
    "Extradosed01-16-${vDoubleDeckName} Mesh 2",
    "Extradosed01-16-${vDoubleDeckName} Mesh 2 LOD1",
    "Extradosed01-16-${vDoubleDeckName} Mesh 2 LOD2",
    "Extradosed01-16-${vDoubleDeckName} Mesh LOD1",
    "Extradosed01-16-${vDoubleDeckName} Mesh LOD2",
    'ExtradosedBridge01 Section -24 Piece',
    'ExtradosedBridge01 Section -24 Piece LOD1',
    'ExtradosedBridge01 Section -24 Piece LOD2',
    'ExtradosedBridge01 Section -8 Piece',
    'ExtradosedBridge01 Section -8 Piece LOD1',
    'ExtradosedBridge01 Section -8 Piece LOD2'
)
$geometryStems += @(
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh 1",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh 1 LOD1",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh 1 LOD2",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh LOD1",
    "TrussArch01-16-${greenMainRoadName}_TrussArch01 Mesh LOD2",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh 1",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh 1 LOD1",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh 1 LOD2",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh LOD1",
    "TrussArch01-64-${wideBlueRoadName}_TrussArch01 Mesh LOD2",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh 1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh 1 LOD1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh 1 LOD2",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh LOD1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 Mesh LOD2",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh 1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh 1 LOD1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh 1 LOD2",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh LOD1",
    "TrussArch03-64-${wideGreenRoadName}_TrussArch03 (1) Mesh LOD2",
    'TrussArchBridge01 Section 8 Piece',
    'TrussArchBridge01 Section 8 Piece LOD1',
    'TrussArchBridge01 Section 8 Piece LOD2',
    'TrussArchBridge01 Section 32 Piece',
    'TrussArchBridge01 Section 32 Piece LOD1',
    'TrussArchBridge01 Section 32 Piece LOD2',
    'TrussArchBridge01 Section 56 Piece',
    'TrussArchBridge01 Section 56 Piece LOD1',
    'TrussArchBridge01 Section 56 Piece LOD2',
    'TrussArchBridge03 Section 8 Piece',
    'TrussArchBridge03 Section 8 Piece LOD1',
    'TrussArchBridge03 Section 8 Piece LOD2',
    'TrussArchBridge03 Section 32 Piece',
    'TrussArchBridge03 Section 32 Piece LOD1',
    'TrussArchBridge03 Section 32 Piece LOD2',
    'TrussArchBridge03 Section 56 Piece',
    'TrussArchBridge03 Section 56 Piece LOD1',
    'TrussArchBridge03 Section 56 Piece LOD2',
    'TrussArchBridge03 Section 56 (2) Piece',
    'TrussArchBridge03 Section 56 (2) Piece LOD1',
    'TrussArchBridge03 Section 56 (2) Piece LOD2'
)

# The static list above removes assets left by older development versions. The current exporter also
# records every top-level bridge name it created. Resolve those complete names against ImportedData
# before deleting the state file, so newly added bridge styles are cleaned without maintaining
# another partial list. Matching includes the complete export name (not a road-name fragment): the
# bridge itself, an optional lower network, its independently named tower/section and their meshes.
$stateExportNames = @()
foreach ($stateFile in $stateFiles) {
    if (-not (Test-Path -LiteralPath $stateFile -PathType Leaf)) { continue }
    $stateExportNames += @(Import-Csv -LiteralPath $stateFile -Delimiter "`t" |
        ForEach-Object { $_.exportName } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}
$stateExportNames = @($stateExportNames | Select-Object -Unique)
if (Test-Path -LiteralPath $importedRoot -PathType Container) {
    $currentImportedNames = @(Get-ChildItem -LiteralPath $importedRoot -Directory |
        Select-Object -ExpandProperty Name)
    foreach ($exportName in $stateExportNames) {
        $ownedMarker = '-' + $exportName
        $importedDirectoryNames += @($currentImportedNames | Where-Object {
            $_ -eq $exportName `
                -or $_ -eq ($exportName + '_Lower') `
                -or $_.StartsWith($exportName + ' (', [StringComparison]::Ordinal) `
                -or $_.Contains($ownedMarker)
        })
    }

    # Towers created by current versions have an ownership name of
    # [style]-[width]-[complete bridge name]. Older in-game removal deleted only the top-level bridge,
    # so its name disappeared from export-state.tsv while these RenderPrefabs remained. The geometry
    # directory was then removed and the orphan loaded with a null mesh on the next launch. Match the
    # complete generated tower prefix (including a numeric width), never a road-name fragment.
    $towerStyles = @(
        'PedestrianDraw', 'CoveredWood', 'SuspensionGolden', 'Suspension',
        'Extradosed01', 'Extradosed02', 'Extradosed03', 'ExtradosedLarge', 'CableStayed',
        'TrussArch01', 'TrussArch02', 'TrussArch03', 'TrussArch', 'TiedArch', 'Grand', 'Draw', 'Lift'
    )
    # Current asset names replace decimal points with underscores (7.2 -> 7_2); retain the dot form
    # so cleanup also owns assets written by older versions.
    $generatedNumberPattern = '[-+]?[0-9]+(?:[._][0-9]+)?'
    $towerPattern = '^(' + (($towerStyles | ForEach-Object { [regex]::Escape($_) }) -join '|') `
        + ')-' + $generatedNumberPattern + '-.+'
    $importedDirectoryNames += @($currentImportedNames | Where-Object {
        $_ -match $towerPattern
    })

    # A top-level generated bridge ends in its style id. It can survive without an export-state
    # entry when an older cleanup removed the state first. Match the complete suffix only; this
    # deliberately does not treat an arbitrary road-name fragment as ownership evidence.
    $bridgeStyleSuffixPattern = '_(' `
        + (($towerStyles | ForEach-Object { [regex]::Escape($_) }) -join '|') `
        + ')(?:_(?:Lower|Upper))?(?: \([0-9]+\))?$'
    $importedDirectoryNames += @($currentImportedNames | Where-Object {
        $_ -match $bridgeStyleSuffixPattern
    })

    # Dependency clones created by BridgeBuilder (and its former BridgePrefabGenerator identity)
    # always use this ownership prefix.
    # RoadPrefabExporter's RBExportDep_* directories intentionally remain outside this match.
    $importedDirectoryNames += @($currentImportedNames | Where-Object {
        $_.StartsWith('RBBridgeDep_', [StringComparison]::Ordinal)
    })

    # Width-adjusted sections use an archetype name followed by the signed width delta. Name
    # collisions add " (n)" before the Piece suffix, so a static list cannot clean every run.
    # The source archetype list is exact and is intentionally kept narrower than "* Section *".
    $generatedSectionPrefixes = @(
        '2-Lane Suspension Bridge',
        '3-Lane Suspension Bridge',
        '4-Lane Suspension Bridge',
        '5-Lane Suspension Bridge',
        '8-Lane Cable Stayed Bridge 00',
        'Cable Stayed Pedestrian Bridge',
        'ExtradosedBridge01 Section',
        'ExtradosedBridge02 Section',
        'ExtradosedBridge03 Section',
        '6-Lane Extradosed Bridge',
        'SuspensionBridge03 Section',
        'PedestrianBridgeCoveredWood01 Section',
        'Grand Bridge',
        'LiftBridge01 Section',
        'LiftBridge03 Section',
        '2-Lane Truss Arch Bridge',
        'TrussArchBridge01 Section',
        'TrussArchBridge02 Section',
        'TrussArchBridge03 Section'
    )
    $generatedSectionPattern = '^(' `
        + (($generatedSectionPrefixes | ForEach-Object { [regex]::Escape($_) }) -join '|') `
        + ') ' + $generatedNumberPattern `
        + '(?: \([0-9]+\))?(?: Piece(?: [0-9]+)?(?: LOD[0-9]+)?)?$'
    $importedDirectoryNames += @($currentImportedNames | Where-Object {
        $_ -match $generatedSectionPattern
    })

    # Resolve every RenderPrefab which references Geometry currently owned by this mod. This covers
    # generated sections whose archetype-derived names intentionally do not carry the bridge name.
    $ownedGeometryIds = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase)
    foreach ($geometryRoot in $geometryRoots) {
        if (-not (Test-Path -LiteralPath $geometryRoot -PathType Container)) { continue }
        Get-ChildItem -LiteralPath $geometryRoot -Filter '*.Geometry.cid' -File | ForEach-Object {
            $id = [IO.File]::ReadAllText($_.FullName).Trim()
            if ($id -match '^[0-9a-fA-F]{32}$') { $ownedGeometryIds.Add($id) | Out-Null }
        }
    }

    if ($ownedGeometryIds.Count -gt 0) {
        foreach ($directory in Get-ChildItem -LiteralPath $importedRoot -Directory) {
            $prefabFile = Join-Path $directory.FullName ($directory.Name + '.Prefab')
            if (-not (Test-Path -LiteralPath $prefabFile -PathType Leaf)) { continue }
            $prefabText = [IO.File]::ReadAllText($prefabFile)
            foreach ($id in $ownedGeometryIds) {
                if ($prefabText.Contains('CID:' + $id)) {
                    $importedDirectoryNames += $directory.Name
                    break
                }
            }
        }
    }
}
$importedDirectoryNames = @($importedDirectoryNames | Select-Object -Unique)

function Assert-ExactChild([string]$Root, [string]$Path) {
    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    $prefix = $resolvedRoot + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing target outside '$resolvedRoot': $resolvedPath"
    }
    return $resolvedPath
}

$removedImported = 0
foreach ($name in $importedDirectoryNames) {
    if ($name.StartsWith('RBExportDep_', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove preserved dependency: $name"
    }
    $target = Assert-ExactChild $importedRoot (Join-Path $importedRoot $name)
    if (Test-Path -LiteralPath $target -PathType Container) {
        Remove-Item -LiteralPath $target -Recurse -Force
        $removedImported++
    }
}

$removedGeometry = 0
foreach ($geometryRoot in $geometryRoots) {
    foreach ($stem in $geometryStems) {
        foreach ($extension in @('.Geometry', '.Geometry.cid')) {
            $target = Assert-ExactChild $geometryRoot (Join-Path $geometryRoot ($stem + $extension))
            if (Test-Path -LiteralPath $target -PathType Leaf) {
                Remove-Item -LiteralPath $target -Force
                $removedGeometry++
            }
        }
    }
}

$stateFiles = @($stateFiles | ForEach-Object {
    $modDataRoot = Split-Path -Parent $_
    Assert-ExactChild $modDataRoot $_
})
$iconFiles = @($modDataRoots | ForEach-Object {
    $modDataRoot = $_
    Assert-ExactChild $modDataRoot (Join-Path $modDataRoot 'Icons\c84a2ef1a0a5779f79b5c65d20da1421.svg')
    Assert-ExactChild $modDataRoot (Join-Path $modDataRoot 'Icons\e931e1d62e11a4bf1bb2ac71fd570cd2.svg')
})
$removedState = 0
$removedIcon = 0
foreach ($stateFile in $stateFiles) {
    if (Test-Path -LiteralPath $stateFile -PathType Leaf) {
        Remove-Item -LiteralPath $stateFile -Force
        $removedState++
    }
}
foreach ($iconFile in $iconFiles) {
    if (Test-Path -LiteralPath $iconFile -PathType Leaf) {
        Remove-Item -LiteralPath $iconFile -Force
        $removedIcon++
    }
}

# Every file below either exact current/legacy directory is geometry emitted by this mod. Removing
# the complete directories is both more accurate and safer than guessing future mesh stems.
$resolvedGameRoot = [IO.Path]::GetFullPath($gameRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
foreach ($geometryRoot in $geometryRoots) {
    if (Test-Path -LiteralPath $geometryRoot -PathType Container) {
        $resolvedGeometryRoot = [IO.Path]::GetFullPath($geometryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
        if ((Split-Path -Parent $resolvedGeometryRoot) -ne $resolvedGameRoot `
            -or $modIds -notcontains (Split-Path -Leaf $resolvedGeometryRoot)) {
            throw "Refusing unexpected mod geometry target: $resolvedGeometryRoot"
        }
        $remainingOwnedGeometry = @(Get-ChildItem -LiteralPath $resolvedGeometryRoot -Recurse -File)
        $removedGeometry += $remainingOwnedGeometry.Count
        Remove-Item -LiteralPath $resolvedGeometryRoot -Recurse -Force
    }
}

$remainingImported = @($importedDirectoryNames | Where-Object {
    Test-Path -LiteralPath (Join-Path $importedRoot $_)
})
$remainingGeometry = @($geometryRoots | ForEach-Object {
    $geometryRoot = $_
    $geometryStems | ForEach-Object {
        $stem = $_
        @('.Geometry', '.Geometry.cid') | ForEach-Object {
            Join-Path $geometryRoot ($stem + $_)
        }
    }
} | Where-Object { Test-Path -LiteralPath $_ })

if (($remainingImported.Count -ne 0) `
    -or ($remainingGeometry.Count -ne 0) `
    -or @($geometryRoots | Where-Object { Test-Path -LiteralPath $_ }).Count -ne 0 `
    -or @($stateFiles | Where-Object { Test-Path -LiteralPath $_ }).Count -ne 0 `
    -or @($iconFiles | Where-Object { Test-Path -LiteralPath $_ }).Count -ne 0) {
    throw "Cleanup verification failed: imported=$($remainingImported.Count), geometry=$($remainingGeometry.Count)."
}

[pscustomobject]@{
    RemovedImportedDirectories = $removedImported
    RemovedGeometryFiles = $removedGeometry
    RemovedStateFiles = $removedState
    RemovedIcons = $removedIcon
    PreservedExportDependencies = @(Get-ChildItem -LiteralPath $importedRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object Name -Like 'RBExportDep_*').Count
}
