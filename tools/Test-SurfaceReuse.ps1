param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$GameManaged,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$ModAssembly,
    [string]$Unity = 'C:/Program Files/Unity 2022.3.62f2/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = [IO.Path]::GetFullPath($ProjectPath)
if (!$ModAssembly) { $ModAssembly = Join-Path $repo 'src/BridgeBuilder/bin/Release/BridgeBuilder.dll' }
if (!(Test-Path -LiteralPath "$project/Assets/Editor" -PathType Container)) { throw 'Use the dedicated Unity preparation project.' }
Copy-Item -LiteralPath "$repo/tools/GpuBake/SurfaceReuseCheck.Editor.cs" -Destination "$project/Assets/Editor/SurfaceReuseCheck.cs"
$log = [IO.Path]::GetFullPath($LogPath)
$arguments = @('-batchmode','-nographics','-projectPath',('"'+$project+'"'),
    '-executeMethod','SurfaceReuseCheck.Run','-gameManaged',('"'+[IO.Path]::GetFullPath($GameManaged)+'"'),
    '-modAssembly',('"'+[IO.Path]::GetFullPath($ModAssembly)+'"'),
    '-surfaceFile',('"'+$repo+'/assets/Materials/Draw01/Road/NativeAsphalt.Surface"'),
    '-quit','-logFile',('"'+$log+'"'))
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'PASS original surface reuse:' -Quiet) -or
    !(Select-String -LiteralPath $log -Pattern 'PASS appearance recipe keys:' -Quiet) -or
    !(Select-String -LiteralPath $log -Pattern 'PASS constant control routing:' -Quiet) -or
    !(Select-String -LiteralPath $log -Pattern 'PASS authored texture detection:' -Quiet) -or
    !(Select-String -LiteralPath $log -Pattern 'PASS structural native charts:' -Quiet) -or
    !(Select-String -LiteralPath $log -Pattern 'PASS structural material selection:' -Quiet)) { throw "Surface reuse checks failed; see $log" }
Select-String -LiteralPath $log -Pattern 'PASS original surface reuse:|PASS appearance recipe keys:|PASS constant control routing:|PASS authored texture detection:|PASS structural material selection:|PASS structural native charts:' | ForEach-Object Line
