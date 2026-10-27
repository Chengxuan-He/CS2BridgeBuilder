param([string]$Repo='C:/Users/admin/Documents/Codex/CS2BridgeBuilder', [Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$root=Join-Path $Repo 'artifacts/agent-material-env/PedestrianDraw01'
$outputPath=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $outputPath){throw 'Choose a new candidate output path; existing candidates are immutable.'}
[IO.Directory]::CreateDirectory($outputPath)|Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/RepeatControlAudit.Editor.cs" -Destination "$root/unity/Assets/Editor/PedestrianRepeatControlAudit.cs"
$env:TEMP="$root/cache/temp"; $env:TMP=$env:TEMP; $env:UPM_CACHE_ROOT="$root/cache/upm"
$arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$root+'/unity"'),'-executeMethod','PedestrianRepeatControlAudit.Run',
'-gameData','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data"',
'-repeatInventory',('"'+$Repo+'/artifacts/prepared-repeat/PedestrianDraw01/recipes.json"'),
'-styleRoot',('"'+$Repo+'/assets/Materials/PedestrianDraw01"'),'-candidateOutput',('"'+$outputPath+'"'),
'-quit','-logFile',('"'+$outputPath+'/native-audit.log"'))
$process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Audit PID=$($process.Id)"
$process.WaitForExit()
Select-String -LiteralPath "$outputPath/native-audit.log" -Pattern '^PASS repeat|error CS|^Exception|InvalidDataException' | ForEach-Object Line
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath "$outputPath/native-audit.log" -Pattern '^PASS repeat control source audit:' -Quiet)){throw 'Native source audit failed'}
