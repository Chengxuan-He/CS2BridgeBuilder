[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameDirectory,
      [Parameter(Mandatory=$true)][string]$HarmonyDll,
      [Parameter(Mandatory=$true)][string]$ReportDirectory)
$ErrorActionPreference='Stop'
$out=[IO.Path]::GetFullPath($ReportDirectory)
[IO.Directory]::CreateDirectory($out)|Out-Null
dotnet build $PSScriptRoot -c Release *> (Join-Path $out 'native-event-build.log')
if($LASTEXITCODE -ne 0){throw 'Probe build failed'}
$env:BB_QUIT_UNITY=Join-Path $GameDirectory 'Cities2_Data/Managed/UnityEngine.CoreModule.dll'
$env:BB_QUIT_HARMONY=[IO.Path]::GetFullPath($HarmonyDll)
$env:BB_QUIT_REPORT=Join-Path $out 'native-event-result.log'
dotnet run --project (Join-Path $PSScriptRoot 'Host') -- $GameDirectory $env:BB_QUIT_HARMONY (Join-Path $PSScriptRoot 'bin/Release/netstandard2.1/ShutdownEventCheck.dll') *> (Join-Path $out 'native-event-mono.log')
if($LASTEXITCODE -ne 0){throw 'Game Mono event check failed'}
Get-Content -LiteralPath $env:BB_QUIT_REPORT
