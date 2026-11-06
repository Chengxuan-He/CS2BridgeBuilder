# Structural SIMD verification

`Host` embeds the installed game's Mono/core library without starting Cities2 or
changing JIT settings. `Probe` loads baseline and candidate DLL bytes separately,
logs SHA256/MVID, invokes production methods and compares every Plan field, float
bit, index, mask, fragment and frame. It also decodes all embedded numerical
programs and checks tails, signed zero, subnormals, extreme values, aliases,
duplicate destinations and malformed instructions. It does not modify saves.

Export audited movable NPZ inputs with `ExportInputs.py RESEARCH CASES.tsv OUTPUT`.
`full-plan-cases.tsv` selects complete structural chains; `+layout` reconstructs
three explicit road layouts, including missing/asymmetric sidewalks and curved,
varying-width car lanes. These are test layouts, not captured gameplay samples.

Build Probe with `GameManagedDir` set to the installed managed directory, then run:

```text
dotnet run --project tools/AffineGeometryNativeCheck/Host -- GAME PROBE_DLL BASELINE_DLL CANDIDATE_DLL MANIFEST.tsv REPORT.txt
```

For fixed-family native inputs, run `GeometryMetaprogram --simd-fixed-fixtures
Blob.cok OUTPUT` with the game's `Plugins/x86_64` directory on PATH, followed by
`FixedCases.py OUTPUT`. Original ZIP entry and payload/fixture hashes are retained.
Historical TrussArch01 maps use explicitly labelled synthetic coordinates because
their matching native geometry is absent from the installed archives.

The host supplies a `System.Numerics.Matrix4x4` determinant adapter solely for the
Plan finite-determinant validation gate. It does not replace coordinate arithmetic
or compare native rendering. Real Unity `TryBuild`, packed bone/color preservation,
frame handling, uploaded positions/indices and physical retiling are checked by
`AffineGeometry88NativeMeshCheck.Editor.cs` using retained original Pedestrian02
raw fixtures. Its candidate/input/output paths are environment-controlled; reuse
an exclusive initialized Unity project. Do not add this editor file to the mod.

Timing includes actual production calls and their allocations. Reports separate
each entry's first invocation from nine alternating baseline/candidate warm calls
and their medians. Earlier entries may have initialized shared caches; these are
not whole-game cold-load measurements. Packed instructions are established on
the actual game Mono runtime; editor Mono uses different scalar intermediates.
Report regressions as well as improvements. Deployment does not establish a new
in-game near/far, motion or visual acceptance result.

Both compared assemblies are placed in the same outer-LOD worker context, so
nested vertex parallelism cannot confound timing. For a newer baseline without
the historical scalar helper, set `BB_AFFINE_SCALAR_ORACLE` to the retained scalar
assembly; it supplies only core arithmetic and historical-map reference results.
The primary full-plan timing still compares the two explicitly named assemblies.
After execution, Host obtains the actual arithmetic method's JIT extent from
game Mono and saves its machine-code body beside the report for instruction
inspection. This does not modify JIT options or generate custom machine code.

`AffineGeometryCheck` also runs the actual offline AoS compiler against synthetic
ordered programs: cross-axis dependencies, preloaded values across writes, aliasing,
block boundaries, first offsets and untouched NaN payload/signed-zero axes. All
embedded APF2 resources must decode; APF1 is rejected rather than silently skipped.
