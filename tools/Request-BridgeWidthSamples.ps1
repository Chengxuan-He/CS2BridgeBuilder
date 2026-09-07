[CmdletBinding()]
param(
    [string]$RoadId = '',
    [string]$RequestId = ("width-invariant-" + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
)

$ErrorActionPreference = 'Stop'
$local = [Environment]::GetFolderPath('LocalApplicationData')
$localLow = Join-Path (Split-Path -Parent $local) 'LocalLow'
$data = Join-Path $localLow 'Colossal Order\Cities Skylines II\ModsData\BridgeBuilder'
$request = Join-Path $data 'export.request'
$temporary = Join-Path $data 'export.request.new'
$styles = @(
    'Extradosed01',
    'Extradosed02',
    'Extradosed03',
    'CableStayed',
    'Suspension',
    'SuspensionGolden',
    'TrussArch01',
    'TrussArch03',
    'TrussArch',
    'TiedArch',
    'CoveredWood',
    'Grand'
)

[IO.Directory]::CreateDirectory($data) | Out-Null
$lines = @(
    'version=1',
    'operation=create-width-samples',
    ('requestId=' + $RequestId),
    ('road=' + $RoadId),
    ('styles=' + ($styles -join ','))
)
[IO.File]::WriteAllLines($temporary, $lines, [Text.UTF8Encoding]::new($false))
Move-Item -LiteralPath $temporary -Destination $request -Force

Write-Host "BridgeBuilder API request submitted: $request"
Write-Host "Request id: $RequestId"
if ([string]::IsNullOrWhiteSpace($RoadId)) {
    Write-Host 'Road: the mod will choose one measurable registered road.'
} else {
    Write-Host "Road: $RoadId"
}
Write-Host ('Styles: ' + ($styles -join ', '))
Write-Host 'This command does not start or control the game.'
