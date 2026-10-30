param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameManaged,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$ModAssembly,
    [string]$Unity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project=[IO.Path]::GetFullPath($ProjectPath)
if(!$ModAssembly){$ModAssembly=Join-Path $repo 'src/BridgeBuilder/bin/Release/BridgeBuilder.dll'}
if(!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)){throw 'Use an independent Unity test project.'}
if(Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($project.Replace('\','/'))}){throw "Unity is already using $project"}
Copy-Item -LiteralPath "$repo/tools/GpuBake/IndependentAxesCheck.Editor.cs" -Destination "$project/Assets/Editor/IndependentAxesCheck.cs"
New-Item -ItemType Directory -Force -Path "$project/Assets/Plugins/x86_64" | Out-Null
Copy-Item -LiteralPath "$GameManaged/../Plugins/x86_64/PipelinePlugin.dll" -Destination "$project/Assets/Plugins/x86_64/PipelinePlugin.dll"
$arguments=@('-batchmode','-nographics','-projectPath',('"'+$project+'"'),'-executeMethod','IndependentAxesCheck.Run','-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),'-modAssembly',('"'+[IO.Path]::GetFullPath($ModAssembly)+'"'),'-surfaceFile',('"'+$repo+'/assets/Materials/Draw01/Road/NativeAsphalt.Surface"'),'-quit','-logFile',('"'+[IO.Path]::GetFullPath($LogPath)+'"'))
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if($process.ExitCode-ne 0-or !(Select-String -LiteralPath $LogPath -Pattern 'PASS native independent UV axes:' -Quiet)){throw "Independent UV axes check failed; see $LogPath"}
Select-String -LiteralPath $LogPath -Pattern 'PASS native independent UV axes:' | ForEach-Object Line
