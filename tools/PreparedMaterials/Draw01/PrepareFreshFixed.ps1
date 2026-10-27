[CmdletBinding()]
param([string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data',
      [ValidateSet('Cable','Road','Lanes')][string[]]$Kinds=@('Cable','Road','Lanes'))
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$fresh=Join-Path $repo 'artifacts/agent-material-env/Draw01/fresh-native-materials'
$output=Join-Path $fresh 'output'
$pattern='new\[\] \{ "Draw01", "Draw02", "Draw03", "Lift01", "Lift02", "Lift03", "Lift04", "Lift05", "PedestrianDraw01", "PedestrianDraw02" \}'
foreach($kind in $Kinds) {
    $source=Join-Path $repo "tools/Prepare$kind/Program.cs"
    $code=[IO.File]::ReadAllText($source)
    if ([regex]::Matches($code,$pattern).Count -ne 1) {throw "Reviewed style-selection source changed: $source"}
    $project=Join-Path $fresh "fixed-producers/$kind"
    [IO.Directory]::CreateDirectory($project) | Out-Null
    [IO.File]::WriteAllText((Join-Path $project 'Program.cs'),[regex]::Replace($code,$pattern,'new[] { "Draw01" }'))
    $includes=if($kind -eq 'Lanes'){'<ItemGroup><Compile Include="'+(Join-Path $repo 'src/BridgeBuilder/Runtime/BridgeSurfaceReferences.cs')+'" /></ItemGroup>'}else{''}
    [IO.File]::WriteAllText((Join-Path $project 'Prepare.csproj'),('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets></PropertyGroup>'+$includes+'</Project>'))
    $parameters=@($GameData)
    if($kind -eq 'Cable'){$parameters+=(Join-Path $repo 'tools/PrepareCable/NativeTemplate.json')}
    if($kind -eq 'Lanes'){$parameters+=(Join-Path $repo 'tools/PrepareLanes/NativeTemplates.json')}
    $parameters+=$output
    & dotnet run --project (Join-Path $project 'Prepare.csproj') -- @parameters
    if($LASTEXITCODE -ne 0){throw "Fresh Draw01 $kind preparation failed."}
}
Write-Output "Fresh Draw01 fixed descriptors were generated from native sources at $output. No existing prepared asset was copied."
