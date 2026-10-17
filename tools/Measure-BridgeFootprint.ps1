param(
    [Parameter(Mandatory=$true)][ValidatePattern('^b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$')][string]$Owner,
    [string]$GameData = "$env:USERPROFILE/AppData/LocalLow/Colossal Order/Cities Skylines II"
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $GameData).Path
$assetRoots = @("$root/ImportedData", "$root/BridgeBuilder") | Where-Object { Test-Path -LiteralPath $_ }
$files = @(Get-ChildItem -LiteralPath $assetRoots -File -Recurse | Where-Object {
    $_.FullName.Substring($root.Length).IndexOf($Owner, [StringComparison]::OrdinalIgnoreCase) -ge 0
})
if ($files.Count -eq 0) { throw "No persisted files found for $Owner under $root" }
$groups = @($files | Group-Object Extension | ForEach-Object {
    [pscustomobject]@{ Extension=$_.Name; Files=$_.Count; Bytes=[long](($_.Group | Measure-Object Length -Sum).Sum) }
})
$dependencies = @($files | Where-Object { $_.FullName.Contains($Owner + '_Dependencies') })
[pscustomobject]@{
    Owner=$Owner
    PhysicalFiles=$files.Count
    PhysicalBytes=[long](($files | Measure-Object Length -Sum).Sum)
    SnapshotFiles=$dependencies.Count
    SnapshotBytes=[long](($dependencies | Measure-Object Length -Sum).Sum)
    Categories=$groups
    Scope='All UUID-owned physical files, including dependency snapshots and CID sidecars. Referenced built-in payloads require the separate logical graph comparison; this is not a complete native-size verdict.'
}

