# Lift03 source-level material preparation

`PrepareSourceRebuild.py` reopens the real native game archives, verifies original
Surface and texture CID hashes, and prepares a manifest for the common
`tools/Rebuild-NativeMaterials.ps1` producer. It reads measured Main/Auxiliary
sampling expressions and BBN arrays without changing geometry. It never reads
old generated material products or another bridge's ImportedData assets.

The source set contains 12 structural Surface roots, one glass Surface root,
and nine built-in texture payload identities (21 decoded channel views).
Compatible originals retain their original CIDs and native VT/mip graphs.
The two measured road units have three reachable native template variants.
`ExportRecipes.py` and `CheckRecipes.py` provide measured UV bounds, source density
and original source verification. `ExportRepeatUnitInputs.py` locates their exact
complete authoring Texture inputs across all installed built-in Blob archives.
The shared RepeatUnits/2 producer prepares each measured unit once from authored
mips. Template differences share all resulting texture bytes.

Run `PrepareSourceRebuild.py --game-data <Cities2_Data> --output <private-input>`.
Pass its `sources.json` to the common source-level producer, using independent
output and report paths and the coordinator's freshly rebuilt common materials.
Use `tools/Test-NativeMaterialDescriptors.ps1` in the private Lift03 Unity project;
the old checker requiring all materials to be non-VT does not apply.
`NativeGraphCheck` then verifies exact CIDs, files, dependencies and native hashes.

The retained reconstruction and native-reader evidence is under
`artifacts/prepared-materials/Lift03/rebuild-native-v1`. It contains 60 files
including sidecars, 15,078 bytes, with 29 Surface descriptors and one transparent
control Texture. The native reader consumed all descriptors, including 27 VT
materials. Original structural images and paint mip content were not re-encoded.
This is a descriptor-stage result, not full native rendering or style acceptance.

`Lift03RuntimeUvCharts.py` and `CheckRuntimeCharts.py` provide the runtime
`Lift03UvCharts.For` table. Source charts require matching XYZ and UV edge
adjacency; unrelated physical parts are not joined through neighboring atlas
coordinates. The `ReceiverCoverage` source-linked tool evaluates actual geometry
plans through the shared chart implementation. Its numerical containment checks
do not prove atlas-local repetition, normal interpretation or all-mip rendering.

`IntegrateRepeatUnits.py` stages the verified repeat outputs, creates Main's native
template 0 descriptor by changing only the template field, and derives its native
CID and RepeatUnits/2 key. Main template 1 and Auxiliary template 1 retain their
original candidate descriptors. Before `--publish`, run the independent native
descriptor reader and `NativeGraphCheck` on the complete staged library. Publishing
requires matching graph hashes, only adds byte-identical or new files to this
style, and writes `Lift03RepeatUnits.cs` with the three exact sampler aliases.

`BindRepeatRegions.py` applies the final native road-template distinction: cutout
road aliases use template 14 with the original road producer's alpha-test,
double-sided, SSR and packed-normal keywords; the default road alias uses
template 0. Structural region descriptors retain source template 1 and the source
keyword/parameter maps. These descriptors all reference the same prepared pixels.
`Lift03Regions` matches the exact source Surface SHA and reads bounds from the
existing lazy RoadAppearance BBN arrays. No independently guessed crop is added.

The formal Lift03 library contains 84 files and 9,445,939 bytes: 42 unique local
payloads with 84 built-in dependencies. `RegionCoverage` reproduces production
domain aggregation and parent projection/phase handling for 21 measured meshes
and 144 real parameter scenarios. The two registered regions add zero structural
receiver coverage: no complete source domain fits inside them. The remaining
176,184 receiver triangle occurrences are explicitly unresolved. These counts span
all scenarios and are not a count of unique mesh faces.

The shared pipeline still owns cross-chart structural sampling and final
preview/save/export/cold-load/persistence validation.
Existing-atlas edge pattern differences follow the accepted contract exception
and remain a human observation item; measured values are retained. No arbitrary
structural recipe is mapped to a road unit. Do not restore old per-sampler texture
files as fallback or claim this style's bytes are the complete SDK package.
No build, installation or release is performed by these tools.

Localization review: no user-facing runtime/UI text is introduced.

`GroupStructureUnits.py` consumes the complete per-domain outcomes emitted by
`RegionCoverage`, matches every original chart domain, and prepares the public
StructureUnits/1 manifest for `tools/PrepareRepeatUnits/AuditStructureUnits.py`.
Its current result records 606,619 domains and 18,610 unresolved domains; no new
structural periodic unit is certified. See `artifacts/prepared-materials/Lift03/
structure-semantic-v1` for exact constraints and the distinction between generic
audit class membership and actual failed receiver counts. Do not invent a bake
batch from the incomplete periodicity evidence.
