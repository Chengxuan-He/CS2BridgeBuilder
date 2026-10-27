[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^(Draw0[1-3]|Lift0[1-5]|PedestrianDraw0[12])$')][string]$Style,
    [Parameter(Mandatory)][string]$Manifest,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$Report,
    [Parameter(Mandatory)][string]$GameData,
    [string]$TemplateDefaults,
    [string]$CommonMaterials
)
$ErrorActionPreference = 'Stop'
$arguments = @('run','--project',(Join-Path $PSScriptRoot 'PrepareNativeMaterials/PrepareNativeMaterials.csproj'),'-c','Release','--',
    $Style,[IO.Path]::GetFullPath($Manifest),[IO.Path]::GetFullPath($Output),[IO.Path]::GetFullPath($Report),[IO.Path]::GetFullPath($GameData))
if ($TemplateDefaults) { $arguments += @('--defaults',[IO.Path]::GetFullPath($TemplateDefaults)) }
if ($CommonMaterials) { $arguments += @('--common',[IO.Path]::GetFullPath($CommonMaterials)) }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Native material preparation failed.' }
