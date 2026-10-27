param([string]$Repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder')
$ErrorActionPreference='Stop'
$envRoot=Join-Path $Repo 'artifacts/agent-material-env/Draw01'
$reportRoot=Join-Path $Repo 'artifacts/prepared-materials/Draw01/rebuild-native-v1'
Copy-Item -LiteralPath "$PSScriptRoot/NativeRebuildCheck.Editor.cs" -Destination "$envRoot/unity/Assets/Editor/Draw01NativeRebuildCheck.cs"
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$envRoot+'/unity"'),'-executeMethod','Draw01NativeRebuildCheck.Run',
 '-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"',
 '-candidateRoot',('"'+$envRoot+'/rebuild-native-v1/output/Draw01"'),
 '-originals',('"'+$envRoot+'/fresh-native-materials/inputs"'),
 '-report',('"'+$reportRoot+'/native.json"'),'-quit','-logFile',('"'+$reportRoot+'/native.log"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
Select-String -LiteralPath "$reportRoot/native.log" -Pattern '^PASS Draw01|error CS|InvalidDataException' | ForEach-Object Line
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$reportRoot/native.log" -Pattern '^PASS Draw01 native rebuild:' -Quiet)){throw 'Draw01 native rebuild check failed'}
