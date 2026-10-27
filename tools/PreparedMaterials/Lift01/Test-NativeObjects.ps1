param([string]$Repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder', [string]$CandidateRoot)
$ErrorActionPreference='Stop'
$root=Join-Path $Repo 'artifacts/agent-material-env/Lift01'
$candidate=if ($CandidateRoot) { [IO.Path]::GetFullPath($CandidateRoot) } else { Join-Path $Repo 'artifacts/prepared-materials/Lift01/native-object-candidates' }
Copy-Item -LiteralPath "$PSScriptRoot/NativeObjectCheck.Editor.cs" -Destination "$root/unity/Assets/Editor/Lift01NativeObjectCheck.cs"
$env:TEMP="$root/cache/temp"; $env:TMP=$env:TEMP; $env:UPM_CACHE_ROOT="$root/cache/upm"
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$root+'/unity"'),'-executeMethod','Lift01NativeObjectCheck.Run',
'-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"',
'-candidateRoot',('"'+$candidate+'"'),'-quit','-logFile',('"'+$candidate+'/native-check.log"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Lift01 native descriptor verification PID=$($process.Id)"
$process.WaitForExit()
Select-String -LiteralPath "$candidate/native-check.log" -Pattern '^PASS Lift01|error CS|^Exception|InvalidDataException' | ForEach-Object Line
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$candidate/native-check.log" -Pattern '^PASS Lift01 native object descriptors:' -Quiet)){throw 'Lift01 native descriptor verification failed'}
