[CmdletBinding()]
param(
    [string]$GameDir
)

# Runs the tower tests without the game.
#
# Only the mod's engine-free files are compiled in: the widening rule and the tower table. Everything
# else in the mod needs the game's assemblies and a loaded world, and dragging those in would make the
# tests need the very thing they exist to avoid.

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

if (-not $GameDir) {
    $candidates = @(
        'C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II',
        'D:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II'
    )
    $GameDir = $candidates | Where-Object { Test-Path (Join-Path $_ 'Cities2_Data\Managed') } | Select-Object -First 1
}
if (-not $GameDir) { throw 'Could not find the game. Pass -GameDir.' }
$managed = Join-Path $GameDir 'Cities2_Data\Managed'

$dotnetRoot = Split-Path -Parent (Get-Command dotnet).Source
$sdkVersion = (& dotnet --list-sdks | Select-Object -Last 1).Split(' ')[0]
$compiler = Join-Path $dotnetRoot "sdk\$sdkVersion\Roslyn\bincore\csc.dll"

$frameworkRefs = Get-ChildItem -LiteralPath (Join-Path $dotnetRoot 'packs\Microsoft.NETCore.App.Ref') -Directory |
    Sort-Object Name | Select-Object -Last 1
$refDir = Join-Path $frameworkRefs.FullName 'ref\net8.0'

$references = New-Object System.Collections.Generic.List[string]
Get-ChildItem -LiteralPath $refDir -Filter '*.dll' | ForEach-Object {
    $references.Add('/reference:' + $_.FullName)
}
$references.Add('/reference:' + (Join-Path $managed 'Unity.Mathematics.dll'))

$output = Join-Path $projectRoot 'tests\bin'
[IO.Directory]::CreateDirectory($output) | Out-Null
$exe = Join-Path $output 'TowerTests.dll'

$sources = @(
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\TowerWidening.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\GoldenBridgeRailings.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\BridgeTowers.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\BridgeStyleDefinitions.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\DeckArrangement.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\SectionNames.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\BridgeMeasurements.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\BridgeCables.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\BridgeTowerMaterials.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\BridgeTowerSpec.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\BridgeSpec.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\TowerPrefabNaming.cs'),
    (Join-Path $projectRoot 'src\BridgeBuilder\Bridges\PrototypeBridgeSizing.cs'),
    (Join-Path $projectRoot 'tests\TowerTests.cs'),
    (Join-Path $projectRoot 'tests\TowerGenerationTests.cs')
)
foreach ($source in $sources) {
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing source: $source" }
}

$arguments = @(
    $compiler, '/nologo', '/target:exe', '/codepage:65001', '/nullable:enable',
    '/langversion:latest', ('/out:' + $exe)
) + $references + $sources

& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Test compilation failed with exit code $LASTEXITCODE" }

$runtimeConfig = Join-Path $output 'TowerTests.runtimeconfig.json'
$frameworkVersion = (Get-ChildItem -LiteralPath (Join-Path $dotnetRoot 'shared\Microsoft.NETCore.App') -Directory |
    Sort-Object Name | Select-Object -Last 1).Name
@"
{
  "runtimeOptions": {
    "tfm": "net8.0",
    "framework": { "name": "Microsoft.NETCore.App", "version": "$frameworkVersion" }
  }
}
"@ | Set-Content -LiteralPath $runtimeConfig -Encoding utf8

Copy-Item -LiteralPath (Join-Path $managed 'Unity.Mathematics.dll') -Destination $output -Force

& dotnet $exe
exit $LASTEXITCODE
