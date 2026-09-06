[CmdletBinding()]
param(
    [string]$GameDir,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

function Find-GameDirectory {
    param([string]$RequestedPath)

    if ($RequestedPath) {
        $resolved = [IO.Path]::GetFullPath($RequestedPath)
        if (Test-Path -LiteralPath (Join-Path $resolved 'Cities2_Data\Managed\Game.dll')) { return $resolved }
        throw "Game.dll was not found below: $resolved"
    }

    $candidates = [Collections.Generic.List[string]]::new()
    $steam = Get-ItemPropertyValue -Path 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam' -Name InstallPath -ErrorAction SilentlyContinue
    if ($steam) {
        $candidates.Add((Join-Path $steam 'steamapps\common\Cities Skylines II'))
        $libraries = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $libraries) {
            foreach ($match in [regex]::Matches([IO.File]::ReadAllText($libraries), '"path"\s+"([^"]+)"')) {
                $libraryPath = $match.Groups[1].Value.Replace('\\', '\')
                $candidates.Add((Join-Path $libraryPath 'steamapps\common\Cities Skylines II'))
            }
        }
    }

    $candidates.Add('D:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II')
    $candidates.Add('C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II')
    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'Cities2_Data\Managed\Game.dll')) { return $candidate }
    }
    throw 'Cities: Skylines II was not found. Pass -GameDir explicitly.'
}

$GameDir = Find-GameDirectory $GameDir
$managed = Join-Path $GameDir 'Cities2_Data\Managed'
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$dotnetRoot = Split-Path -Parent $dotnet
$sdkVersion = (& $dotnet --version).Trim()
$compiler = Join-Path $dotnetRoot "sdk\$sdkVersion\Roslyn\bincore\csc.dll"
$refPack = Get-ChildItem -LiteralPath (Join-Path $dotnetRoot 'packs\NETStandard.Library.Ref') -Directory |
    Sort-Object { [version]$_.Name } -Descending |
    Select-Object -First 1
if (-not $refPack) { throw 'NETStandard.Library.Ref is not installed with the .NET SDK.' }
$frameworkRefs = Join-Path $refPack.FullName 'ref\netstandard2.1'

$output = Join-Path $projectRoot "src\BridgeBuilder\bin\$Configuration"
[IO.Directory]::CreateDirectory($output) | Out-Null

$references = [Collections.Generic.List[string]]::new()
Get-ChildItem -LiteralPath $frameworkRefs -Filter '*.dll' | ForEach-Object {
    $references.Add('/reference:' + $_.FullName)
}
foreach ($assembly in @(
    'Game.dll',
    'Colossal.Core.dll',
    'Colossal.IO.AssetDatabase.dll',
    'Colossal.Localization.dll',
    'Colossal.Logging.dll',
    'Colossal.Mathematics.dll',
    'Unity.Collections.dll',
    'Colossal.AssetPipeline.dll',
    'Colossal.UI.dll',
    'Colossal.UI.Binding.dll',
    'UnityEngine.CoreModule.dll',
    'Unity.Entities.dll',
    'Unity.Mathematics.dll'
)) {
    $path = Join-Path $managed $assembly
    if (-not (Test-Path -LiteralPath $path)) { throw "Required game assembly is missing: $path" }
    $references.Add('/reference:' + $path)
}

$sharedRoot = Join-Path (Split-Path -Parent $projectRoot) 'CS2ModShared\src'
if (-not (Test-Path -LiteralPath $sharedRoot)) {
    throw "The shared sources are missing: $sharedRoot. Clone CS2ModShared next to this repository."
}
$sources = @(
    (Join-Path $projectRoot 'src\BridgeBuilder'),
    $sharedRoot
) | ForEach-Object { Get-ChildItem -LiteralPath $_ -Recurse -Filter '*.cs' } |
    Select-Object -ExpandProperty FullName
$arguments = @(
    $compiler,
    '/nologo',
    '/target:library',
    '/deterministic+',
    '/codepage:65001',
    '/optimize+',
    '/debug:portable',
    '/nullable:enable',
    '/langversion:latest',
    ('/out:' + (Join-Path $output 'BridgeBuilder.dll'))
) + $references + $sources

& $dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE" }
Write-Host "Built: $(Join-Path $output 'BridgeBuilder.dll')"
