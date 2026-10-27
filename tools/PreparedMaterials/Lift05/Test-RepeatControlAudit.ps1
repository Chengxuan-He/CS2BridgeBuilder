param([string]$Repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder', [Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$root=Join-Path $Repo 'artifacts/agent-material-env/Lift05'
$outputPath=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $outputPath){throw 'Choose a new candidate output path; existing candidates are immutable.'}
$logPath=$outputPath+'.log'
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputPath))|Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/RepeatControlAudit.Editor.cs" -Destination "$root/unity/Assets/Editor/Lift05RepeatControlAudit.cs"
Copy-Item -LiteralPath "$PSScriptRoot/ControlMipRead.shader" -Destination "$root/unity/Assets/Lift05ControlMipRead.shader"
$names=@('TEMP','TMP','UPM_CACHE_ROOT');$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    $env:TEMP="$root/cache/temp"; $env:TMP=$env:TEMP; $env:UPM_CACHE_ROOT="$root/cache/upm"
    $arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$root+'/unity"'),'-executeMethod','Lift05RepeatControlAudit.Run',
    '-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"',
    '-repeatInventory',('"'+$Repo+'/artifacts/prepared-lift05/repeat-recipes.json"'),
    '-styleRoot',('"'+$Repo+'/assets/Materials/Lift05"'),'-candidateOutput',('"'+$outputPath+'"'),
    '-quit','-logFile',('"'+$logPath+'"'))
    $process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output "Audit PID=$($process.Id)"
    $process.WaitForExit()
    Select-String -LiteralPath $logPath -Pattern '^PASS Lift05|error CS|^Exception|InvalidDataException' | ForEach-Object Line
    if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $logPath -Pattern '^PASS Lift05 repeat control audit:' -Quiet)){throw 'Native source audit failed'}
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}