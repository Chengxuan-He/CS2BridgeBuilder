# Lupu native LOD transition investigation

## Scope and status

Current authorized generation change: retain LOD0 and LOD1 for all five Lupu parts (the main
arch and four tower groups), remove LOD2 and pause far bridgehead reconstruction. The bridge
implementer reports this code change complete; native visual and cold-load acceptance are not
established by this record. The earlier threshold investigation and single-level decision below
are historical evidence, not the current representation.

Before the subsequent two-level decision, the user's clarification on 2026-11-17 requested restoration of the GitHub-published LOD
behavior, not the local Lupu calibration experiment. The local calibration is cancelled; the
intermediate interpretation that authorized its restoration is superseded. It was never the
published LOD implementation. Code restoration, deployment and native visual acceptance
require separate verification.

The official [26.11.16 release](https://github.com/Chengxuan-He/CS2BridgeBuilder/releases/tag/26.11.16)
points to `a53b10636ef21a4a09cbd383c2a205d4d4fc04b9`; the 26.11.14 baseline is
`9ebef0e7d8cd9c74272be8bfbc3d9533a7aaa77e`. Neither contained Lupu or CalibrateLod.
Their common approach retains prototype bias/shadow values, rebuilds the derived LOD mesh
references and uses native forward selection. The inspected
[TowerFactory.Lods.cs at the release commit](https://github.com/Chengxuan-He/CS2BridgeBuilder/blob/a53b10636ef21a4a09cbd383c2a205d4d4fc04b9/src/BridgeBuilder/Bridges/Common/Geometry/TowerFactory.Lods.cs)
has no difference between these two baselines; it remaps the LOD mesh array after deriving
the corresponding geometry. Applying that selection behavior to Lupu is separate from its subsequent two-level representation decision, not evidence
that either release already supported Lupu.

The inspected source is mod `151335_2`, source COK SHA-256
`32b2bf0e9ffcf46b679218d3c403906de2e062940e9d13244af3f66e434ca310`.
The governing scope is [native LOD transition equivalence](../../specs/lod-spec.md#native-selection-and-source-preservation).

## Native evidence identity

The [native LOD source record](native-lod-formula-sources.md) retains the exact binary/extract hashes, version fields and formula locators inspected for this investigation.

## Cancelled local adjustment

The project derivation solves for one owned structural bias adjustment so that the combined
network's coarse effective rendering size matches the measured source-object coarse target.
It uses the actual selected road's composition at generation time, rather than a fixed donor
road or per-frame camera control. This is a custom inverse of the native forward behavior
identified above, not the published implementation or a native calibration algorithm.
The experimental opposite shadow-bias change compensated for the native ordinary
bias contribution to effective shadow bias. Geometry, the three LOD levels, source assets
and global quality settings were unchanged by that experiment. These properties do not
authorize retaining the cancelled adjustment.

A single bias cannot independently reproduce both source boundaries. Keeping the coarse
transition means retaining near geometry farther away; the measured main-span near/middle
difference is `259` faces. This tradeoff is explicit, not proof of identical full LOD behavior
or permission for multiple independent tunings or unmeasured styles.

## Corrected threshold evidence and limits

The fresh native evaluation reports network CalculateLodLimit `130`, hence finer minimum
`131`; the source value is `117`, hence finer minimum `118`. The earlier network minimum
`132` and associated approximately `5.04` factor contained an off-by-one error and are
superseded. The reported continuous source/network rendering-size ratio is approximately
`4.3802`; quantized thresholds and continuous ratios are distinct measurements, neither a
universal player-camera distance claim.

The earlier isolated native-comparison test did not establish that the custom inverse was
native code. Nor did it establish visual appearance, player quality/FOV behavior, shadows or
cold-load acceptance. Restoration must be verified against the current implementation and
candidate; prior calculation results are not proof of the restored build's game acceptance.

## Superseded continuous main-arch single-level decision

The measured continuous main-span face counts in `work/lupu-lod-20261117/source.json` are
`5081` for LOD0, `4822` for LOD1 and `420` for LOD2. The near/middle difference is `259`
faces; the far representation omits structural detail retained by the near mesh. These counts
refer to the continuous main structure, not the complete source bridge or its tower groups.

The earlier authorization kept that continuous structure at LOD0 through a single native
render resource and retained three source LODs for the four tower groups. That proposal has
been superseded by the whole-bridge two-level decision below. It did not authorize restoring
the cancelled bias calibration or changing the source package.

## Whole-bridge two-level decision

The subsequent user decision retains the original near and middle representations for all
five generated parts, excludes the far level and pauses far bridgehead reconstruction. The
bridge implementer's completion report records main-arch face counts `5081`/`4822`, combined
four-tower counts `41708`/`10316`, and complete structural totals `46789`/`15138` for
LOD0/LOD1 respectively. The implementation has `163` numerical resources and retains authored
bias `0`; this is a resource/geometry report, not an in-game performance measurement.

This is not a change to the original source assets or a custom bias scheme. Existing owners
are not automatically migrated or regenerated, and no new self-check flow is introduced.
Offline graph/resource verification, preview, native near/far appearance and cold-load
acceptance remain separate evidence categories.
