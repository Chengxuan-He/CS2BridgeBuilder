param(
    [string]$Unity = 'C:\Program Files\Unity 2022.3.62f2\Editor\Unity.exe',
    [Parameter(Mandatory=$true)][string]$ProjectPath
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath($ProjectPath)
New-Item -ItemType Directory -Force -Path "$project/Assets/Editor", "$project/Packages", "$project/ProjectSettings" | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/BridgeBake.compute" -Destination "$project/Assets/BridgeBake.compute" -Force
Copy-Item -LiteralPath "$PSScriptRoot/BakeBuild.Editor.cs" -Destination "$project/Assets/Editor/BakeBuild.cs" -Force
Set-Content -LiteralPath "$project/Packages/manifest.json" -Value '{"dependencies":{"com.unity.modules.assetbundle":"1.0.0"}}'
Set-Content -LiteralPath "$project/ProjectSettings/ProjectVersion.txt" -Value 'm_EditorVersion: 2022.3.62f2'
$log = "$project/build.log"
$process = Start-Process -FilePath $Unity -ArgumentList @('-batchmode','-nographics','-projectPath', ('"'+$project+'"'), '-executeMethod','BakeBuild.Build','-quit','-logFile', ('"'+$log+'"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "GPU bundle build failed; see $log" }
if (Select-String -LiteralPath $log -Pattern 'Shader error' -Quiet) { throw "GPU shader compilation failed; see $log" }
Copy-Item -LiteralPath "$project/Build/bridgebake" -Destination "$PSScriptRoot/../../assets/bridgebake" -Force
