# Blue deck truss-arch bridge: dev audit record

## User acceptance

On 2026-09-19, the user explicitly reported that TrussArch01 passed acceptance; this is recorded as user in-game acceptance. The subsequent covered-bridge central-column revision did not change this bridge implementation. `TrussArch01Geometry.cs` SHA256 remains `650E2B3211DE4888785519C3CF714B9D2D2B9FDBD348642897F1C1A3E5646B3F`. This result does not replace the unfinished normalization of prototype road-width measurement boundaries below. The user subsequently accepted the revised covered bridge and requested merging both fixes into pre-release and installing them.

## Branch and status

- The project was saved in `pre-release` commit `b0fbec59052cb0b24c410bd0efa84d9bdc4936de`.
- The initial audit used `dev` commit `8386e64`, without merging or cherry-picking pre-release changes.
- Pre-release retained the pier width-plan propagation change and compiled, but had no new generated model or in-game visual validation.
- The initial dev audit added only this document. The user then requested an immediate fix; implementation started on `bridge/truss-arch-01`, fast-forwarded from dev to baseline `8386e64`. See the final section for subsequent status.

## Audit sources

1. The `TrussArch01` row in `agent-contract/BRIDGE_WIDTH_INVARIANT_AUDIT.md`.
2. Its measured row in `agent-contract/bridge-width-invariant-measurements.tsv`.
3. Local `ModsData/BridgeBuilder/asset-anatomy.txt`, internally timestamped `2026-09-14 16:10:01Z`.
4. Local `ModsData/BridgeBuilder/tower-measurements.txt`.

The historical report measures the entire bridge envelope: prototype bounds `[-9.203892, 9.215492]`, generated bounds `[-23.21549, 23.21549]`, and full-width increment `-0.01159668` (`0xBC3E0000`). It is not a component measurement establishing equal main-pier and arch widths. Do not apply that value again as pier compensation or use historical acceptance or a small envelope discrepancy to explain this visibly incorrect width.

## Dev code audit

- `BridgeTowers.cs` marks `TrussArchBridge01NetPillar` as `support: true`, recording prototype road width as 20 m. Supports are not selected as tower candidates.
- `BridgeStyle.Selection.ExtraFor` still reads `RoadOf(style.Id)` when no tower is selected; the arch increment uses target width minus prototype road width.
- `BridgeComposer.FitTower` instead passes target `deckWidth` as the source road width in its primary-support fallback.
- `TowerFactory.ExtraFor` consequently computes zero for `deckWidth - authored`, then passes that result to `PierExtraForSection`. The pier loses the target-minus-prototype term used by the arch.
- `MeasureStructureExtra` already receives the arch width plan, but dev retained it only for green `TrussArch03`; the blue variant did not use it.

Conclusion: this is an identifiable width-plan propagation defect, not merely another adjustment to a historical full-width audit constant. The blue pier and arch should consume the same width plan, with the measured prototype body/arch difference aligning their widths. The separate base must not be conflated with the main body. Full detail and every LOD must be checked together. These are code findings, not measurements of a newly generated model.

## Initial inspection, supplemented by the new sample below

The existing audit tool was run on dev with output outside the repository, preserving the historical TSV:

```powershell
.\tools\AuditBridgeWidths.ps1 -Styles TrussArch01 -OutputPath <outputs>/trussarch01-dev-width-audit-20260919.tsv
```

The tool exited with code 1. Its original failure was:

```text
ERROR: Required real prefab block is absent: 两块板六车道_TrussArch01
```

The initial dump contained this bridge's prototype but no corresponding generated sample. The runtime bridge registry was empty and the latest export report described Golden Gate deletion. Therefore no current pier/arch full widths, error bit patterns or LOD consistency result could be reported. The measured audit could be marked neither passed nor skipped.

## New sample measurements: 2026-09-19 18:51:45

The user generated `一块板六车道_TrussArch01` and supplied a close-up screenshot marking the pier that was narrower than the arch. Export timestamp: `2026-09-19T10:51:45.9330813Z`; measured selected road width: 40 m. Absence of a sample under the historical name was no longer a blocker.

Local game-data root: `C:/Users/admin/AppData/LocalLow/Colossal Order/Cities Skylines II`. Starting at `ImportedData/一块板六车道_TrussArch01/一块板六车道_TrussArch01.Prefab`, recursive resolution of `.cid` sidecars and prefab references confirmed that all nine Geometry files below belong to this bridge's actual dependency graph, including section references through `Pricing_*` clones. The current sample was not inferred from filenames. Geometry files are in the `BridgeBuilder` directory under the game-data root.

Actual vertices were loaded read-only using the existing GeometryMetaprogram binary decoder. Complete lateral width is `maxX - minX`, converted to binary32. Decimal values below round-trip to their original floats; the export log's three-decimal formatting is not used for exact comparisons.

| Part / LOD | Vertices | minX (m) | maxX (m) | Complete width (m) | Width bits |
| --- | ---: | ---: | ---: | ---: | --- |
| Arch / Full detail | 150909 | -23.209644 | 23.209644 | 46.41929 | 0x4239AD5A |
| Arch / LOD1 | 104056 | -23.209644 | 23.209644 | 46.41929 | 0x4239AD5A |
| Arch / LOD2 | 7842 | -23.113575 | 23.113575 | 46.22715 | 0x4238E89A |
| Main pier / Full detail | 4332 | -13.208363 | 13.210926 | 26.419289 | 0x41D35AB4 |
| Main pier / LOD1 | 3065 | -13.208363 | 13.210926 | 26.419289 | 0x41D35AB4 |
| Main pier / LOD2 | 491 | -13.173695 | 13.194691 | 26.368385 | 0x41D2F274 |
| Separate Mesh 1 / Full detail | 294 | -14.713551 | 14.725269 | 29.43882 | 0x41EB82B4 |
| Separate Mesh 1 / LOD1 | 208 | -14.713551 | 14.725269 | 29.43882 | 0x41EB82B4 |
| Separate Mesh 1 / LOD2 | 109 | -14.607349 | 14.686207 | 29.293556 | 0x41EA5934 |

File mapping: the arch is `TrussArchBridge01 Section 31 (2) Piece[ LOD1/LOD2].Geometry`; the main pier is `TrussArch01-40-一块板六车道_TrussArch01 Mesh[ LOD1/LOD2].Geometry`; the separate part uses the same prefix with `Mesh 1[ LOD1/LOD2].Geometry`. Pier object CID: `b59c654c1101e60c678088483c61b3cc`. The main mesh and Mesh 1 have zero local positions and identity quaternion rotations. “Mesh 1” only distinguishes a separate mesh; its name does not establish that it meets the contract's definition of a base.

Full-detail Geometry SHA256:

- Arch: `654C61ED5DA802CF84A1FB1801C5283301BCB39D3C271003921785551B9648FD`
- Main pier: `7BBB48ED03DAEE3016F0CF25F62EFF5A1F54CE9A51B8A998C2AC6D44797A583C`
- Separate Mesh 1: `011326AD07B840E9B699C0BC44D75DBF5A1FACBFD9991EA8495637CE93E452B4`

### Conclusion: the equal-width check for pier and arch failed

Arch width minus main-pier width is `20f` (`0x41A00000`) at full detail and LOD1, and `19.858765f` (`0x419EDEC0`) at LOD2. This is not the historical envelope discrepancy of 0.01159668 m or an illusion caused by screenshot perspective. Scaling the entire pier is not an acceptable repair.

The export report recorded an arch increment around 31.019 m but a main-pier increment around 11.220 m, consistent with the wrong source-road-width fallback above: the 40 m target road width became the pier's source width, losing the `40 - 20` term actually used by the arch. The pier report's “generated arch width 26.419 m” was derived from that incorrect plan, not measured from the actual arch mesh. A later covered-bridge export at 18:58:14 overwrote `last-export-report.txt`; the blue-bridge report values were read before that overwrite, and its sample Geometry remained available.

### Component rules checked against the user clarification and AGENTS.md

- The pier is not one logical part. Columns and attached side parts entirely on one side of the centerline must translate rigidly, preserving their section, thickness and height; do not scale the whole pier laterally.
- Crossbeams reaching or crossing `x = 0` stretch against their own prototype spans and remain attached to translated columns. A noncrossing part must not stretch merely because its name contains “beam.”
- `TrussArch01Geometry.TryApply` already has two paths: unmarked vertices call `Spread`; mapped stretching vertices use offline coefficients. The coefficient path covers 224/4332 full-detail, 224/3065 LOD1 and 40/491 LOD2 vertices; the rest translate. Counts do not replace component-by-component validation.
- `PortalCoefficients.FromTopology` currently uses ±0.001 m to classify centerline crossing, and `TowerWidening.Spread` also uses a center tolerance. The contract requires actual contact/crossing of zero. Review whether this historical tolerance misclassifies any prototype part separately; do not assume it caused this 20 m gap.
- LOD2 uses the prototype full-detail vertex classification. Welding the coarse mesh into one connected component must not change columns into stretching parts. Component/LOD shape diffs were still incomplete, so the classification was not declared fully validated.

These are pre-repair audit findings: the component equal-width check explicitly failed.

## Additional prototype measurements from the installed game, not inferred from generated results

Bridge prototype: `TrussArchBridge01`; structural section: `TrussArchBridge01 Section`; arch mesh: `TrussArchBridge01Net Mesh`; support object: `TrussArchBridge01NetPillar`. Its two separate meshes are `TrussArchBridge01NetPillar Mesh` and `TrussArchBridge01NetPillarBase Mesh`. Prototype and LOD references come from approximately lines 359422–360260 of `asset-anatomy.txt`; hardcoded source constants are not presented as prototype measurements.

The original game archive was opened directly and read-only: `D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/Game/Blob_BridgesAndPorts.cok`. Geometry vertices were decoded from its entries using the same decoder and `maxX - minX` full-width convention as the generated sample. The game was not run and no synthetic test vertices were constructed.

| Prototype part / LOD | Vertices | minX (m) | maxX (m) | Complete width (m) | Width bits |
| --- | ---: | ---: | ---: | ---: | --- |
| Arch / Full detail | 150909 | -7.699951 | 7.699951 | 15.399902 | 0x41766600 |
| Arch / LOD1 | 104056 | -7.699951 | 7.699951 | 15.399902 | 0x41766600 |
| Arch / LOD2 | 7842 | -7.603882 | 7.603882 | 15.207764 | 0x41735300 |
| Main pier / Full detail | 4332 | -7.5981445 | 7.600708 | 15.198853 | 0x41732E80 |
| Main pier / LOD1 | 3065 | -7.5981445 | 7.600708 | 15.198853 | 0x41732E80 |
| Main pier / LOD2 | 491 | -7.5634766 | 7.5844727 | 15.147949 | 0x41725E00 |
| Separate PillarBase / Full detail | 294 | -9.203857 | 9.215576 | 18.419434 | 0x41935B00 |
| Separate PillarBase / LOD1 | 208 | -9.203857 | 9.215576 | 18.419434 | 0x41935B00 |
| Separate PillarBase / LOD2 | 109 | -9.097656 | 9.176514 | 18.27417 | 0x41923180 |

Exact archive filenames, each with the `.Geometry` extension:

- `TrussArchBridge01Net_8be3082637e01129ebf92cf68ab9402a`
- `TrussArchBridge01Net_LOD1_17e04f83068be63de50c2dde4aa09e2b`
- `TrussArchBridge01Net_LOD2_6c5ba0cb01d872b0963937f6e987bd57`
- `TrussArchBridge01NetPillar_a42c63261dc78b13d723cdff971612af`
- `TrussArchBridge01NetPillar_LOD1_7bc63680693ea6dda871ce1160cd7ba3`
- `TrussArchBridge01NetPillar_LOD2_641895873cba3df2a9970d6c6288efb9`
- `TrussArchBridge01NetPillarBase_89390445e2e541e60a5b7e398de767a8`
- `TrussArchBridge01NetPillarBase_LOD1_0b1fbd7dee15f15d70b0d4500d94eb1d`
- `TrussArchBridge01NetPillarBase_LOD2_2276ab09722eb24a18ae184b32d029b8`

Prototype LOD2 already has different outer bounds from the full-detail mesh. Its authored simplification differences must not all be attributed to this deformation. Prototype prefab `m_Bounds` also differs slightly from the compressed Geometry vertex bounds; these measurements must not be mixed.

An actual vertex diff between the full-detail prototype pier and old generated pier covered 4332 vertices and 32 welded groups. Of those groups, 26 translate rigidly and 6 cross the centerline with nonuniform displacement: beam ends follow the columns while their interiors stretch. Maximum y and z displacement is 0; maximum x displacement is approximately 5.610218 m. Grouping is diagnostic only. Runtime retains the original explicit vertex mapping and does not substitute whole-connected-component scaling for logical-part classification.

Prototype road-width conventions have not yet been normalized to the same boundaries: historical audit 12 m, recorded structural source width 20 m, variant report 22 m. Therefore this record does not claim a passed complete bridge-width allowance invariant or apply automatic audit compensation. The repair addresses the confirmed width-plan propagation defect; it does not append a 20 m compensation constant at runtime.

## Implementation and validation status

After the user requested an immediate fix, the following changes were made on `bridge/truss-arch-01`:

1. Retain the composer's structural width plan for the blue truss-arch bridge and reset it at the start of each new bridge to prevent reuse across bridges.
2. The main pier reads that plan and the existing prototype body/arch relationship instead of the fallback that computes `target - target`.
3. The separate PillarBase uses the same original plan; remove the unused inverse-plan helper.
4. Return failure before generating pier geometry when the plan is absent; add no explicit `throw`.
5. Do not change column/crossbeam classification data or introduce global scaling. Existing full-detail, LOD1 and LOD2 derivation paths receive the same plan.

`tools/Build.ps1` compiled successfully and `git diff --check` found no whitespace errors. No visual unit tests were run. `taskkill /F /IM Cities2.exe` was invoked, then process exit was verified.

Before cleanup, 138 ImportedData target directories, mod Geometry, export state and the registry were backed up to `C:/Users/admin/Documents/Codex/2026-09-01/qi/outputs/trussarch01-before-fix-20260919`. The user subsequently explicitly requested fixing on dev, deploying the development build, and merging pre-release only after acceptance. The workspace therefore returned to dev with this fix retained and the CoveredWood central-column fix added. DLL SHA256 for the combined build: `CB5FA8374C04CAD1A1BB79AF6258A0226D3818614450A4406918827B79CA45E1`.

The repository cleanup script successfully removed 73 mod-owned ImportedData directories, 196 geometry files and 2 state files while preserving 9 RBExportDep directories. Another 65 old pre-release pricing dependencies were outside the dev script's scope. A separate deletion was blocked by tool execution policy; retrying after the user again requested removal of all bridge-related files was still rejected before process creation. No alternative execution channel was used to bypass the restriction. Exact locations are recorded in `Pending-Generated-Pricing-Cleanup-20260919.txt`.

The preceding paragraph records the pre-deployment blocker. Because rejection occurred before process creation, it cannot be attributed to PowerShell ExecutionPolicy. The user subsequently deleted the directories manually; all 65 targets were individually verified absent. ImportedData Prefabs no longer referenced the six missing CIDs from the error. Re-running repository cleanup found no remaining mod-generated assets and retained 9 road-export dependencies.

After Cities2.exe was confirmed absent, previous installed files were backed up under `InstalledModBeforeDev` in the backup directory above. `tools/Install.ps1 -SkipBuild` installed dev. DLL, PDB, MJS, CSS and SVG SHA256 values were individually checked against the build outputs/source resources; the DLL hash appears above. Mod caches, saves and pre-release were unchanged.

At this historical deployment stage, the game had not yet been used to regenerate and validate the bridges. The user still needed to regenerate TrussArch01 and CoveredWood using the same road and inspect near/far views. Pre-release was not to be merged before acceptance.
