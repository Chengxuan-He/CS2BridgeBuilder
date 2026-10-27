# Lift04 native binding integration

## Existing native input path

`Lift04SourceRenders` already gives all nine mechanism, pillar and footing LOD
inputs their original Surface CIDs. `MovableSourceRenders.Create` resolves those
CIDs only to built-in assets and assigns the original Surface instances. Its
Surface/VTTexture/Texture references therefore retain original pixel and mip data.
Replacing this working path with another Lift04-only material registry would add
no native sampling capability and is deliberately avoided.

`ExportSharedSampling.py` now implements a complete, checked source binding
catalog, rather than treating the 525 retile entries as the complete render graph.
Its `native-bindings.json` output contains all nine render bindings, seven original
Surfaces (including glass), and ten original native texture resources: six
VTTextures and four ordinary glass textures. Native descriptor parsing validates
the complete serialized body; original references and source hashes are retained.
Every structural sampling record refers to the shared source content identity.
The catalog produces zero new Surface files and zero new texture files.

## Shared-code handoff

`Lift04NativeUvCharts.py` supplies the common consumer with measured source UV
charts, original face membership, directed boundary edges and all 525 sampler
aliases. Its full offline BBN1 inventory is 877,316 bytes and contains no pixels.
`Lift04UvChartBindings.py` installs the 303,784-byte domain-reference resource and
`Lift04UvCharts.cs` adapter using the common `MovableUvChart.Read` interface.
The common consumer must select the chart by exact render, mesh and original face,
then map density-preserving coordinates to the original Surface. This integration
accepts contained triangles only and introduces no repeat inference or splitting.
The seven source Surfaces
and ten source textures retain their complete original native mip dependencies.
See [chart schema](NativeChartSchema.md) for the binary layout and ambiguity rules.

`OriginalBindingOnly.patch` is an unapplied diagnostic guard, not the integration
solution: rejecting previously supported cases does not implement consolidation.
The measured chart data is the actual integration input. The source-level rebuild
must resolve uncovered receivers explicitly; the removed per-sampler library is
not a delivery fallback.

The old per-triangle fold is a generated representation, not proof that the
prototype used that fold. Prior GPU comparison against it only excludes a simple
drop-in equivalent; the required oracle is the original prototype's pattern,
density, channel orientation and complete native mip behavior. No new fold probe
is run as part of this handoff.

## Verification boundary

The binding catalog verifies source identities and physical dependencies without
rebaking or copying built-in resources. The existing native original-identity test
for shared code can exercise the retained compatible branch. All-region density
extension, native full-mip appearance, independent persisted cold loading, and
the complete 50,000,000-byte SDK package remain unverified. No production material,
installed file, UI text or localization key is changed by this handoff.
