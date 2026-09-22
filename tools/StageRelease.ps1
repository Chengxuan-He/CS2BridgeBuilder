[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Destination,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)

# Local staging only: this script never calls ModPublisher or installs into the game.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$destinationRoot = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $destinationRoot) { throw "Destination already exists: $destinationRoot" }
& (Join-Path $PSScriptRoot 'Build.ps1') -Configuration $Configuration
$output = Join-Path $projectRoot "src/BridgeBuilder/bin/$Configuration"
[xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot 'src/BridgeBuilder/BridgeBuilder.csproj') -Raw
$version = [string]$project.Project.PropertyGroup.Version
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $output 'BridgeBuilder.dll')).Version.ToString()
$ui = Get-Content -LiteralPath (Join-Path $output 'BridgeBuilder.mjs') -Raw
$uiVersion = [regex]::Match($ui, '(?m)^\s*\*\s*Version\s*:\s*(\S+)').Groups[1].Value
$docs = Join-Path $projectRoot 'docs/publishing'
[xml]$draft = Get-Content -LiteralPath (Join-Path $docs 'PublishConfiguration.draft.xml') -Raw
if ($assemblyVersion -ne "$version.0" -or $uiVersion -ne $version -or $draft.Publish.ModVersion.Value -ne $version) {
    throw "Release versions differ: project=$version, assembly=$assemblyVersion, UI=$uiVersion, draft=$($draft.Publish.ModVersion.Value)"
}
$files = @('BridgeBuilder.dll', 'BridgeBuilder.mjs', 'BridgeBuilder.css', 'BridgeBuilder.svg',
    'BridgeBuilderToolbar.svg', 'BridgeBuilderPack.svg', 'BridgeBuilderSearch.svg',
    'BridgeBuilderFilter.svg', 'BridgeBuilderArrowDown.svg')
foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath (Join-Path $output $file) -PathType Leaf)) { throw "Missing build output: $file" }
}
$content = Join-Path $destinationRoot 'content'
[IO.Directory]::CreateDirectory($content) | Out-Null
foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $output $file) -Destination $content }
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination (Join-Path $content 'LICENSE.txt')
foreach ($file in @('Mod-Description.md', 'PublishConfiguration.draft.xml', 'Thumbnail.png')) {
    Copy-Item -LiteralPath (Join-Path $docs $file) -Destination $destinationRoot
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets/licenses/Rajdhani-OFL.txt') -Destination $destinationRoot
$sourceRoots = @((Join-Path $projectRoot 'src/BridgeBuilder'),
    (Join-Path (Split-Path -Parent $projectRoot) 'CS2ModShared/src'))
$sourceHashes = @($sourceRoots | ForEach-Object { Get-ChildItem -LiteralPath $_ -Recurse -File } |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
    Sort-Object FullName | ForEach-Object {
        @{ Path = $_.FullName; SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
$manifest = [ordered]@{
    Status = 'NOT_READY_TO_PUBLISH'
    Version = $version
    AssemblyVersion = $assemblyVersion
    SourceCommit = (& git -C $projectRoot rev-parse HEAD).Trim()
    SourceBranch = (& git -C $projectRoot branch --show-current).Trim()
    WorkingTreeStatus = @(& git -C $projectRoot status --porcelain)
    SourceFiles = $sourceHashes
    ContentFiles = @(Get-ChildItem -LiteralPath $content -File | Sort-Object Name | ForEach-Object {
        @{ Name = $_.Name; SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
    OfficialPostProcessing = 'PENDING; must run on an isolated copy and retain its output/log before upload'
    InGameValidation = 'NOT_PERFORMED_ON_THIS_CANDIDATE'
    UploadPerformed = $false
}
[IO.File]::WriteAllText((Join-Path $destinationRoot 'Readiness.json'),
    ($manifest | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
Write-Host "Staged candidate (not publish-ready): $destinationRoot"
