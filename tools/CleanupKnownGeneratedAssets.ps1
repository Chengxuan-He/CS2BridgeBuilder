[CmdletBinding()]
param([switch]$ListOnly, [string]$BackupDirectory, [string]$Owner)
$ErrorActionPreference = 'Stop'
if (Get-Process Cities2 -ErrorAction SilentlyContinue) { throw 'Exit Cities: Skylines II before moving assets.' }
$pattern = 'b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}'
if ($Owner -and $Owner -cnotmatch ('^' + $pattern + '$')) { throw 'Owner must be b{uuid}.' }
$local = [Environment]::GetFolderPath('LocalApplicationData')
$gameRoot = Join-Path (Split-Path $local) 'LocalLow\Colossal Order\Cities Skylines II'
$roots = @('ImportedData','BridgeBuilder','BridgePrefabGenerator') | ForEach-Object { Join-Path $gameRoot $_ }
function Find-BridgePaths([string]$Directory) {
    if (!(Test-Path -LiteralPath $Directory)) { return }
    if ((Get-Item -LiteralPath $Directory).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Unsafe asset root.' }
    foreach ($item in Get-ChildItem -LiteralPath $Directory -Force) {
        $matched = if ($Owner) { $item.Name.IndexOf($Owner,[StringComparison]::OrdinalIgnoreCase) -ge 0 } else { $item.Name -cmatch $pattern }
        if ($matched) { $item }
        elseif ($item.PSIsContainer -and !($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { Find-BridgePaths $item.FullName }
    }
}
$targets = @($roots | ForEach-Object { Find-BridgePaths $_ })
if ($ListOnly) { $targets | Select-Object FullName; return }
if (!$BackupDirectory) { $BackupDirectory = Join-Path ([Environment]::GetFolderPath('MyDocuments')) ('BridgeBuilder-Recovery\' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$backup = [IO.Path]::GetFullPath($BackupDirectory)
if ($backup.Equals($gameRoot,[StringComparison]::OrdinalIgnoreCase) -or $backup.StartsWith($gameRoot+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Backup must be outside game data.' }
for ($parent = [IO.DirectoryInfo]::new($backup); $null -ne $parent; $parent=$parent.Parent) {
    if ($parent.Exists -and ($parent.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Unsafe backup path.' }
}
New-Item -ItemType Directory -Force -Path $backup | Out-Null
foreach ($item in $targets) {
    $source=[IO.Path]::GetFullPath($item.FullName)
    if (!(@($roots | Where-Object { $source.StartsWith($_+'\',[StringComparison]::OrdinalIgnoreCase) }).Count)) { throw 'Source outside intended asset roots.' }
    $destination=Join-Path $backup $item.Name
    while(Test-Path -LiteralPath $destination) { $destination=Join-Path $backup ($item.Name+'__'+[guid]::NewGuid().ToString('N')) }
    Move-Item -LiteralPath $source -Destination $destination
}
Write-Output "Moved $($targets.Count) matching paths to $backup"
