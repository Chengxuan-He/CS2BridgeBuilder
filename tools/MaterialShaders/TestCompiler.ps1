[CmdletBinding()]
param([string]$AssemblyPath,[string]$SeedPath,[string]$BundleOutput,[int]$SourceTemplate=17)
$ErrorActionPreference='Stop'
if (!$AssemblyPath) { $AssemblyPath=Join-Path $PSScriptRoot '../../src/BridgeBuilder/bin/Release/BridgeBuilder.dll' }
$assembly=[Reflection.Assembly]::Load([IO.File]::ReadAllBytes([IO.Path]::GetFullPath($AssemblyPath)))
$types=$assembly.GetTypes() | Where-Object { $_.Namespace -like 'BridgeBuilder*' }
$flags=[Reflection.BindingFlags]'Static,NonPublic'
# The production assembly must expose loading only; compilation is an offline tool.
foreach($type in $types) { if($type.GetMethods($flags) | Where-Object { $_.Name -in @('TryCompilePackage','D3DCompile') }) { throw 'Production assembly still contains the HLSL compiler.' } }
'PASS production material loader contains no compilation entry point'
if($SeedPath) {
 $bytes=[IO.File]::ReadAllBytes([IO.Path]::GetFullPath($SeedPath))
 if($bytes.Length -lt 8 -or [Text.Encoding]::ASCII.GetString($bytes,0,8) -cne "UnityFS`0") { throw 'External file is not a native UnityFS bundle.' }
 if($BundleOutput){[IO.File]::WriteAllBytes([IO.Path]::GetFullPath($BundleOutput),$bytes)}
 'PASS external native bundle accepted without compilation or transformation'
}
