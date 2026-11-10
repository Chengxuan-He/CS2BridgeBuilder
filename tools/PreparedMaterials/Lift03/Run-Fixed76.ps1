param([Parameter(Mandatory)][string]$Dll)
$taskRoot='E:/CS2BridgeBuilder'
$taskProject="$taskRoot/artifacts/material-remap/environments/Lift03/unity"
$taskOutput="$taskRoot/artifacts/material-remap/Lift03/appearance76/fixed-source"
$env:LIFT03_CANDIDATE_PATH=Split-Path -Parent $Dll
$env:LIFT03_PROBE_OUTPUT=$taskOutput
$env:TEMP="$taskRoot/artifacts/material-remap/environments/Lift03/cache/temp"
$env:TMP=$env:TEMP
$env:UPM_CACHE_ROOT="$taskRoot/artifacts/material-remap/environments/Lift03/cache/upm"
$taskUnity='C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
$taskFirst=@('-batchmode','-projectPath',$taskProject,'-executeMethod','Lift03Fixed76Check.Run','-logFile',"$taskOutput/native-fixed.log")
$taskProcess=Start-Process $taskUnity -ArgumentList $taskFirst -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0){throw "Lift03 fixed native geometry verification failed: $($taskProcess.ExitCode)"}
$taskSecond=@('-batchmode','-projectPath',$taskProject,'-executeMethod','Lift03FixedNetwork76Check.Run','-style','Lift03','-inputs',"$taskOutput/native-network",'-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"','-modAssembly',$Dll,'-logFile',"$taskOutput/native-network.log")
$taskProcess=Start-Process $taskUnity -ArgumentList $taskSecond -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0){throw "Lift03 fixed native network verification failed: $($taskProcess.ExitCode)"}
Write-Output 'PASS Lift03 fixed native geometry and network material checks'
