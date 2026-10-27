param([string]$Repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder')
$ErrorActionPreference='Stop'
$private=Join-Path $Repo 'artifacts/agent-material-env/Lift04'
$output=Join-Path $Repo 'artifacts/prepared-materials/Lift04/shared-sampling'
[IO.Directory]::CreateDirectory($output)|Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/SharedSamplingAudit.Editor.cs" -Destination "$private/unity/Assets/Editor/Lift04SharedSamplingAudit.cs"
Copy-Item -LiteralPath "$PSScriptRoot/SharedSamplingProbe.compute" -Destination "$private/unity/Assets/Lift04SharedSamplingProbe.compute"
$names=@('TEMP','TMP','UPM_CACHE_ROOT');$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    $env:TEMP="$private/cache/temp";$env:TMP=$env:TEMP;$env:UPM_CACHE_ROOT="$private/cache/upm"
    $arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$private+'/unity"'),'-executeMethod','Lift04SharedSamplingAudit.Run',
        '-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"',
        '-samplingInventory',('"'+$Repo+'/artifacts/uv-density/structure-tessellated-sources.json"'),
        '-samplingReport',('"'+$output+'/native.json"'),'-quit','-logFile',('"'+$output+'/native.log"'))
    $process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output "Lift04 source sampler audit PID=$($process.Id)"
    $process.WaitForExit()
    if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$output/native.log" -Pattern '^PASS Lift04 shared-source diagnostic:' -Quiet)) {
        throw 'Lift04 source sampler audit failed; inspect its native.log.'
    }
    Select-String -LiteralPath "$output/native.log" -Pattern '^PASS Lift04' | ForEach-Object Line
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}
