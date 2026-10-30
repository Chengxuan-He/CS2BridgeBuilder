param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$ModAssembly,
    [Parameter(Mandatory)][string]$Inputs,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$GameManaged='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed',
    [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath($ProjectPath)
if(!(Test-Path -LiteralPath "$project/Assets/Editor")){throw 'Use an independent Unity environment.'}
if(Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))}){throw 'Unity already uses this environment.'}
Copy-Item -LiteralPath "$PSScriptRoot/NativeNetworkCheck.Editor.cs" -Destination "$project/Assets/Editor/NativeNetworkCheck.cs"
$envRoot=Split-Path -Parent $project
foreach($p in @('cache/temp','cache/upm')){New-Item -ItemType Directory -Path (Join-Path $envRoot $p) -Force | Out-Null}
$previous=@{}
foreach($key in @('TEMP','TMP','UPM_CACHE_ROOT')){$previous[$key]=[Environment]::GetEnvironmentVariable($key,'Process')}
try {
    $env:TEMP=Join-Path $envRoot 'cache/temp';$env:TMP=$env:TEMP;$env:UPM_CACHE_ROOT=Join-Path $envRoot 'cache/upm'
    $arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','Draw03NativeNetworkCheck.Run','-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),'-modAssembly',('"'+[IO.Path]::GetFullPath($ModAssembly)+'"'),'-inputs',('"'+[IO.Path]::GetFullPath($Inputs)+'"'),'-quit','-logFile',('"'+[IO.Path]::GetFullPath($LogPath)+'"'))
    Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru | Select-Object Id
} finally {foreach($key in $previous.Keys){[Environment]::SetEnvironmentVariable($key,$previous[$key],'Process')}}
