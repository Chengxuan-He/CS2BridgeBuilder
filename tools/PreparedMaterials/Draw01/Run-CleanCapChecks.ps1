param([Parameter(Mandatory=$true)][string]$Candidate,
      [Parameter(Mandatory=$true)][string]$CandidateHash,
      [string]$OutputDirectory='artifacts/material-remap/Draw01/yellow-line32')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$out=Join-Path $repo $OutputDirectory
$project=Join-Path $repo 'artifacts/material-remap/environments/Draw01/unity'
if((Get-FileHash -LiteralPath $Candidate).Hash -ne $CandidateHash){throw 'Candidate changed'}
Copy-Item -LiteralPath "$PSScriptRoot/ProductionContinuousCheck.Editor.cs","$PSScriptRoot/CandidateFarCheck.Editor.cs" -Destination "$project/Assets/Editor"
foreach($mode in @('near','middle','far')) {
    $method=if($mode -eq 'far'){'Draw01CandidateFarCheck.Run'}else{'Draw01ProductionContinuousCheck.Run'}
    $arguments=@('-batchmode','-quit','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod',$method,
        '-candidate',('"'+$Candidate+'"'),'-candidateHash',$CandidateHash,
        '-gameManaged','"D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed"',
        '-assetRoot',('"'+$repo+'/assets"'),
        '-surfaceFile',('"'+$repo+'/assets/Materials/Draw01/RepeatUnits/dfaf17e54e1938f1501398483bdc610c9046a2fbd097701940766b595fa4aeac/Repeat.Surface"'),
        '-report',('"'+$out+'/consumer-'+$mode+'.json"'),'-logFile',('"'+$out+'/consumer-'+$mode+'.log"'))
    if($mode -eq 'middle'){$arguments+='-middle'}
    $process=Start-Process -FilePath 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if($process.ExitCode -ne 0){throw "$mode candidate consumer failed"}
    $result=Get-Content -LiteralPath "$out/consumer-$mode.json" -Raw | ConvertFrom-Json
    if($result.status -ne 'PASS' -or $result.candidateSha256 -ne $CandidateHash){throw "$mode verification result differs"}
    Write-Output "$mode PASS $($result.vertexChecks) vertex checks"
}
