# Covered wooden pedestrian bridge: above-deck central column audit and repair

## Latest status: accepted by the user

On 2026-09-19, the user accepted the revised covered wooden pedestrian bridge and requested merging it with the accepted TrussArch01 fix into pre-release, switching branches and installing it. Statements below describing pending acceptance are retained as historical deployment records.

The user accepted TrussArch01 but rejected the first covered-bridge revision with 32.164623 m wide central columns. The user then explicitly corrected “1.6 m” to “the column-top width before the repair.” This revision changes only the above-deck central columns and replaces the first revision that increased the whole column width with the road width. Below-deck piers and TrussArch01 are unchanged.

Align the real pre-repair and first-revision meshes using the identities of the prototype's 272 column vertices, then inspect its 136 column-top vertices:

| Sample | Top y (m) | Top minX (m) | Top maxX (m) | Complete top width (m) | Complete column width (m) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Actual prototype | 4.4311523 | -0.08231163 | 0.08231163 | 0.16462326 | 0.16462326 |
| Pre-repair generated sample | 4.4311523 | -0.80877763 | 0.80877763 | 1.6175553 | 24.509445 |
| Rejected first revision | 4.4311523 | -16.082312 | 16.082312 | 32.164623 | 32.164623 |

The old sample is `PedestrianBridgeCoveredWood01 Section 32 Piece.Geometry` from the pre-cleanup backup below. Its SHA256 is `4D3C0C763AC82EA99C48B0D6EA842F2487A82376CAA1891DAD7318728AC3A0D9`. This does not treat an approximate 1.6 m screenshot measurement as exact or misreport the prototype's thin column as 1.6 m.

The metaprogram command now accepts the prototype archive and pre-repair full-detail Geometry. It verifies index topology, the top boundaries of all 17 columns and prototype y/z coordinates, then emits target left/right boundaries and the original vertex mapping. Runtime applies only immutable rectangular-face coordinates: it does not read the old sample, infer widths from runtime height slices or add road widening to column width. The target complete width is the measured 1.6175553 m throughout the column; height, longitudinal thickness and position are unchanged. The 272 full-detail and 189 LOD1 vertices receive consistent treatment; the prototype's empty LOD2 column mapping is retained.

Both changes remain on the user-specified dev branch. The SHA256 of TrussArch01Geometry.cs remains `650E2B3211DE4888785519C3CF714B9D2D2B9FDBD348642897F1C1A3E5646B3F`. The revised build and installation completed; all five installed files passed hash comparison. The DLL SHA256 is `F0830B589129D022BF5FF850AA1CDF7E52E2E63A3A5961A5E2C4728EF4F12D9D`. Metaprogramming used the real prototype and generated sample. No visual unit tests were run and the game was not controlled to generate samples.

After stopping the game as required, this revision was backed up to `C:/Users/admin/Documents/Codex/2026-09-01/qi/outputs/coveredwood-top-width-20260919`. Cleanup removed 23 mod-generated ImportedData directories, 24 geometry-related files and 1 state file. A repeated cleanup found zero remaining targets; 9 road-export dependencies were retained. Remaining Prefabs contained no references to the deleted geometry CIDs. The mod cache, saves and pre-release were unchanged. At that deployment stage, regenerated near/far visual acceptance of the covered bridge was still required.

The following is the historical audit of the first implementation. Its widening rule was superseded by the explicit user requirement above.

## Baseline and scope

Under the latest explicit user instruction, both fixes were completed on `dev`, based on `8386e64`. That instruction superseded the earlier single-bridge branch workflow. The fixes were not yet merged into `pre-release`; merging was to follow in-game acceptance. Before editing, the root, runtime and metaprogram AGENTS.md files and the complete project contract were read.

This report concerns the central columns inside the covered structure above the deck, **not the piers below the bridge**. It was built together with the TrussArch01 pier-width input fix; neither fix borrows prototype parameters from the other.

## Actual prototype and old generated sample

Prototype: `PedestrianBridgeCoveredWood01 Section` in `PedestrianBridgeCoveredWood01`. Its only net piece is `PedestrianBridgeCoveredWood01Cover Mesh`, with declared length 64 m and width 9.5 m. The original compressed vertices were decoded read-only from this installed-game archive, rather than inferred from images:

`D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/BridgesAndPorts/Blob.cok`

| LOD | Archive Geometry | Total vertices | Central-column vertices |
| --- | --- | ---: | ---: |
| Full detail | PedestrianBridgeCoveredWood01Cover_321693eb11cd6dbcd19faf61b9cac862.Geometry | 10564 | 272 |
| LOD1 | PedestrianBridgeCoveredWood01Cover_LOD1_9a6210a3c2b36fe34abf99311c03c593.Geometry | 6563 | 189 |
| LOD2 | PedestrianBridgeCoveredWood01Cover_LOD2_d4af66f1043b4287b5d5484901b2057b.Geometry | 444 | 0 |

The full-detail prototype has 17 separate rectangular central columns. Each has left/right bounds `[-0.08231163, 0.08231163]` m and vertical bounds `[0.15230179, 4.4311523]` m. The columns run along the bridge; every complete column crosses x=0. Corresponding LOD1 vertices inherit the full-detail coordinate classification. Prototype LOD2 has no central columns, only roof-centerline vertices at x=0; additional LOD components must not be invented.

The actual old generated sample is the user-created `一块板六车道_CoveredWood`, on a 40 m road. Its original export report records a structural widening of 32 m. The actual files are `PedestrianBridgeCoveredWood01 Section 32 Piece.Geometry` and its two LODs. They were read before cleanup and retained at `C:/Users/admin/Documents/Codex/2026-09-01/qi/outputs/trussarch01-before-fix-20260919/BridgeBuilder`.

Vertex correspondence showed that the same rectangular column in full detail and LOD1 had bottom x coordinates around ±12.25 m and top coordinates around ±0.81 m. A generic height-sliced profile assigned different scale factors to identical prototype x coordinates at different heights, producing a wedge. This was not a below-deck pier-placement or lighting problem.

## Implemented changes

1. Added `GeometryMetaprogram --covered-wood-columns <Blob.cok>` to identify the 17 columns in the actual prototype, review centerline crossing and constant rectangular boundaries, and emit exact vertex ranges and prototype spans.
2. LOD1 vertex identities inherit the full-detail prototype classification; LOD2 explicitly records empty ranges, preserving prototype simplification.
3. `CoveredWoodColumnData` stores generated immutable parameters. Runtime uses no height threshold, topology inference or nearest-point search.
4. `CoveredWoodGeometry` stretches each recorded complete column against its own constant span, applying the same rule at every height to remove the wedge. Under the then-applied contract rule for parts crossing x=0, 32 m of widening added 32 m to the original complete column width; it did not force thin columns or resize below-deck piers to road width. This first-revision behavior was superseded as described above.
5. Other parts, y/z coordinates, indices, materials and original vertex channels are preserved. Missing or mismatched mappings return failure before writing the affected geometry. No explicit runtime throw was added.

## Validation limits

Metaprogramming generated all the mappings above from the real prototype. Both changes compiled through `tools/Build.ps1`. No visual unit tests were run. Code inspection confirmed that the piece and LOD paths receive the same widening parameter. This establishes provenance, vertex identity and compilability; **it does not establish that near/far in-game visuals are fixed**.

Cities2.exe was stopped and verified absent. Cleanup removed 73 mod-owned ImportedData directories, 196 geometry files and 2 state files, retaining 9 RBExportDep road dependencies. Mod caches and saves were untouched. Another 65 pricing-clone directories from old pre-release builds were outside the dev cleanup script's coverage. An earlier deletion command was rejected before process creation, which does not establish a PowerShell ExecutionPolicy restriction. The historical list is `Pending-Generated-Pricing-Cleanup-20260919.txt`; these directories had been backed up.

After the user deleted them manually, all 65 directories were individually verified absent. ImportedData Prefab files no longer contained the six missing CIDs associated with this null-reference failure. Re-running repository cleanup found no remaining mod-generated assets and retained 9 road-export dependencies. After confirming the game was closed, dev was installed with `tools/Install.ps1 -SkipBuild`. Previous installed files were backed up under `InstalledModBeforeDev` within the backup directory above. SHA256 values for DLL, PDB, MJS, CSS and SVG matched their build outputs/source resources. Installed DLL SHA256: `CB5FA8374C04CAD1A1BB79AF6258A0226D3818614450A4406918827B79CA45E1`.

Deployment was complete at this historical stage, but visual validation remained pending. The user needed to regenerate both bridge types and inspect near/far views before merging pre-release. This deployment changed neither mod caches nor saves.
