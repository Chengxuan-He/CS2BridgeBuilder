function Get-PreparedPipelineHashes {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string[]]$Paths, [string]$ReusePipelineReport)
    # Only explicitly reviewed edits preserve the offline producer's conversion.
    # An unlisted source hash is never treated as output-equivalent.
    $equivalences = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'GpuBake/PipelineEquivalence.json') -Raw | ConvertFrom-Json
    $actual = @($Paths | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash })
    $requested = $null
    if ($ReusePipelineReport) {
        $report = Get-Content -LiteralPath $ReusePipelineReport -Raw | ConvertFrom-Json
        if (!$report.pipeline) { throw 'Previous preparation report has no pipeline fingerprint.' }
        $requested = @(($report.pipeline -split '/',2)[0] -split ':')
        if ($requested.Count -ne $actual.Count) { throw 'Previous pipeline component count differs.' }
    }
    for ($i = 0; $i -lt $Paths.Count; $i++) {
        $name = [IO.Path]::GetFileName($Paths[$i])
        $entry = @($equivalences | Where-Object { $_.file -ceq $name -and $_.current -ceq $actual[$i] })
        if ($entry.Count -gt 1) { throw "Ambiguous pipeline equivalence: $name" }
        if ($requested) {
            $wanted = $requested[$i]
            if ($wanted -cne $actual[$i] -and !($entry.Count -eq 1 -and $wanted -cin $entry[0].equivalent)) {
                throw "Previous pipeline is not verified output-equivalent: $name"
            }
            $wanted
        } elseif ($entry.Count -eq 1) { $entry[0].canonical }
        else { $actual[$i] }
    }
}
