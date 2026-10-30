param(
    [Parameter(Mandatory)][string]$EnvironmentPath,
    [Parameter(Mandatory)][string]$ModAssembly,
    [Parameter(Mandatory)][string]$Materials,
    [Parameter(Mandatory)][string]$LogName,
    [string]$GameManaged='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed'
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($EnvironmentPath)
$project=Join-Path $root 'unity'
if(!(Test-Path -LiteralPath "$project/Assets/Editor")){throw 'Independent Unity project required.'}
if(Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))}){throw 'Unity already uses this environment.'}
if([IO.Path]::GetFileName($LogName) -ne $LogName){throw 'Use a log filename within the private environment.'}
Copy-Item -LiteralPath "$PSScriptRoot/RoadMappingCheck.Editor.cs" -Destination "$project/Assets/Editor/RoadMappingCheck.cs"
$prior=@{}
foreach($key in @('TEMP','TMP','UPM_CACHE_ROOT')){$prior[$key]=[Environment]::GetEnvironmentVariable($key,'Process')}
try {
    $env:TEMP=Join-Path $root 'cache/temp';$env:TMP=$env:TEMP;$env:UPM_CACHE_ROOT=Join-Path $root 'cache/upm'
    $arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','Draw03RoadMappingCheck.Run',
        '-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),'-modAssembly',('"'+[IO.Path]::GetFullPath($ModAssembly)+'"'),
        '-materials',('"'+[IO.Path]::GetFullPath($Materials)+'"'),'-quit','-logFile',('"'+(Join-Path $root "logs/$LogName")+'"'))
    Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru | Select-Object Id
} finally {foreach($key in $prior.Keys){[Environment]::SetEnvironmentVariable($key,$prior[$key],'Process')}}
