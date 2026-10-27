$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/Get-PreparedPipelineHashes.ps1"
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('BridgeBuilder-Pipeline-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
try {
    $repeat = Join-Path $testRoot 'PrepareRepeat.cs'
    $store = Join-Path $testRoot 'PreparedBakeStore.cs'
    $structure = Join-Path $testRoot 'PrepareStructure.cs'
    $codec = Join-Path $testRoot 'PipelinePlugin.dll'
    Copy-Item -LiteralPath "$PSScriptRoot/GpuBake/PrepareRepeat.Editor.cs" -Destination $repeat
    Copy-Item -LiteralPath "$PSScriptRoot/GpuBake/PreparedBakeStore.cs" -Destination $store
    Copy-Item -LiteralPath "$PSScriptRoot/GpuBake/PrepareStructure.Editor.cs" -Destination $structure
    [IO.File]::WriteAllText($codec, 'codec fixture')
    $paths = @($repeat, $store, $codec)
    $codecHash = (Get-FileHash -LiteralPath $codec).Hash
    $canonical = @(Get-PreparedPipelineHashes -Paths $paths)
    if ($canonical.Count -ne 3 -or $canonical[0] -cne '84A44C0DBB24AB96FF6951D4F7DD0424F93031ACFD6AEED9D339F69A5AE3B3F8' -or
        $canonical[1] -cne 'FBF2826233C5BD63E5DF69AC09EA5031D94AAFE1A947D0BCBED8EB2B2D1300A6' -or $canonical[2] -cne $codecHash) {
        throw 'Reviewed storage-only changes did not preserve canonical fingerprints.'
    }
    $reportPath = Join-Path $testRoot 'report.json'
    foreach ($name in @('RepeatBake.compute','StructureBake.compute')) {
        $shader = Join-Path $testRoot $name
        Copy-Item -LiteralPath "$PSScriptRoot/GpuBake/BridgeBake.compute" -Destination $shader
        $expectedShader = '0502DA78915F28030A10B2C8B3604055E378F40486F0C5666C06B3CF38C57CDB'
        $shaderHashes = @(Get-PreparedPipelineHashes -Paths @($shader))
        if ($shaderHashes[0] -cne $expectedShader) { throw 'Reviewed default-mode shader equivalence was lost.' }
        @{pipeline=$expectedShader + '/Unity/Graphics'} | ConvertTo-Json | Set-Content -LiteralPath $reportPath
        Get-PreparedPipelineHashes -Paths @($shader) -ReusePipelineReport $reportPath | Out-Null
        [IO.File]::AppendAllText($shader, '// unreviewed shader change')
        $rejectedShader=$false
        try { Get-PreparedPipelineHashes -Paths @($shader) -ReusePipelineReport $reportPath | Out-Null }
        catch { $rejectedShader=$true }
        if (!$rejectedShader) { throw 'Unreviewed shader change reused previous products.' }
    }
    $previous = @('9BF1FEB126E6E94C60C289054AB8A5AA13A4181918CE77352FA4181E3BE921B4', $canonical[1], $codecHash)
    @{pipeline=($previous -join ':') + '/Unity/Graphics'} | ConvertTo-Json | Set-Content -LiteralPath $reportPath
    $actual = @(Get-PreparedPipelineHashes -Paths $paths -ReusePipelineReport $reportPath)
    if (($actual -join ':') -cne ($previous -join ':')) { throw 'Verified previous pipeline was not retained.' }
    $structureCanonical = @(Get-PreparedPipelineHashes -Paths @($structure,$store,$codec))
    if ($structureCanonical[0] -cne 'FF4761076F7809189660C8172CCF950C2A9001939E906C2F3970A414CAAC7E28') {
        throw 'Read-only structural lookup invalidated the unchanged producer.'
    }
    @{pipeline=($structureCanonical -join ':') + '/Unity/Graphics'} | ConvertTo-Json | Set-Content -LiteralPath $reportPath
    $structurePrevious = @(Get-PreparedPipelineHashes -Paths @($structure,$store,$codec) -ReusePipelineReport $reportPath)
    if (($structurePrevious -join ':') -cne ($structureCanonical -join ':')) { throw 'Structural pipeline adoption differs.' }
    [IO.File]::AppendAllText($structure, '// unreviewed producer change')
    $structureRejected=$false
    try { Get-PreparedPipelineHashes -Paths @($structure,$store,$codec) -ReusePipelineReport $reportPath | Out-Null }
    catch { $structureRejected=$true }
    if (!$structureRejected) { throw 'Unknown structural producer reused old products.' }
    @{pipeline=($previous -join ':') + '/Unity/Graphics'} | ConvertTo-Json | Set-Content -LiteralPath $reportPath
    function Reject-ChangedPipeline {
        $rejected = $false
        try { Get-PreparedPipelineHashes -Paths $paths -ReusePipelineReport $reportPath | Out-Null }
        catch { $rejected = $true }
        if (!$rejected) { throw 'An unreviewed pipeline change reused previous products.' }
    }
    [IO.File]::AppendAllText($codec, ' changed codec')
    Reject-ChangedPipeline
    [IO.File]::WriteAllText($codec, 'codec fixture')
    [IO.File]::AppendAllText($repeat, '// unreviewed change')
    Reject-ChangedPipeline
    $changed = @(Get-PreparedPipelineHashes -Paths $paths)
    if ($changed[0] -cne (Get-FileHash -LiteralPath $repeat).Hash) { throw 'Unknown source edit was normalized.' }
    @{pipeline='missing:components'} | ConvertTo-Json | Set-Content -LiteralPath $reportPath
    Reject-ChangedPipeline
    'PASS prepared pipeline fingerprints: reviewed aliases only; unknown source, changed codec and component-count mismatch rejected.'
} finally {
    # This invocation owns exactly this randomly named temporary directory.
    $resolved = [IO.Path]::GetFullPath($testRoot)
    $temporary = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (!$resolved.StartsWith($temporary, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notmatch '^BridgeBuilder-Pipeline-[a-f0-9]{32}$') { throw 'Invalid test cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
