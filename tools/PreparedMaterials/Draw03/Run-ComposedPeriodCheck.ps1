param(
    [Parameter(Mandatory)][string]$EnvironmentPath,
    [Parameter(Mandatory)][string]$Materials,
    [string]$GameData='D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($EnvironmentPath)
$project=Join-Path $root 'unity'
if(!(Test-Path -LiteralPath "$project/Assets/Editor")){throw 'Independent Unity project required.'}
if(Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))}){throw 'Unity already uses this environment.'}
Copy-Item -LiteralPath "$PSScriptRoot/ComposedPeriodCheck.Editor.cs" -Destination "$project/Assets/Editor/ComposedPeriodCheck.cs"
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$code=[IO.File]::ReadAllText("$repo/src/BridgeBuilder/Bridges/Common/Movable/MovableTextureSource.cs")
$code=$code.Replace('namespace BridgeBuilder.Bridges;','namespace BridgeBuilder.Bridges {').Replace('makeNoLongerReadable: true','makeNoLongerReadable: false')+"`n}"
[IO.File]::WriteAllText("$project/Assets/Editor/MovableTextureSource.cs",$code)
$prior=@{}
foreach($key in @('TEMP','TMP','UPM_CACHE_ROOT')){$prior[$key]=[Environment]::GetEnvironmentVariable($key,'Process')}
try {
    $env:TEMP=Join-Path $root 'cache/temp';$env:TMP=$env:TEMP;$env:UPM_CACHE_ROOT=Join-Path $root 'cache/upm'
    $arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','Draw03ComposedPeriodCheck.Run',
        '-gameData',('"'+[IO.Path]::GetFullPath($GameData)+'"'),'-materials',('"'+[IO.Path]::GetFullPath($Materials)+'"'),
        '-quit','-logFile',('"'+(Join-Path $root 'logs/composed-periods-native.log')+'"'))
    Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru | Select-Object Id
} finally {foreach($key in $prior.Keys){[Environment]::SetEnvironmentVariable($key,$prior[$key],'Process')}}
