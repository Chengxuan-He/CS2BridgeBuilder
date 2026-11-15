[CmdletBinding()]
param([Parameter(Mandatory)][string[]]$Files)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$profileRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
$roots = @($projectRoot, $profileRoot) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
$needles = @($roots | ForEach-Object { $_; $_.Replace('\', '/') })
foreach ($file in $Files) {
    $bytes = [IO.File]::ReadAllBytes([IO.Path]::GetFullPath($file))
    foreach ($encoding in @([Text.Encoding]::UTF8, [Text.Encoding]::Unicode)) {
        $text = $encoding.GetString($bytes)
        foreach ($needle in $needles) {
            if ($text.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Developer path embedded in release artifact: $file"
            }
        }
    }
}
Write-Output "Verified portable paths in $($Files.Count) release artifacts."
