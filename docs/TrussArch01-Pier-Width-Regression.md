# Blue deck truss-arch pier width regression

## Evidence and limits (2026-09-19)

The retained width audit is `agent-contract/BRIDGE_WIDTH_INVARIANT_AUDIT.md`, row
`TrussArch01`, with the machine-readable row in
`agent-contract/bridge-width-invariant-measurements.tsv`. Its generated sample was
`两块板六车道_TrussArch01` on a 40 m road. It measures the complete structural envelope,
not the main pier alone: prototype bounds `[-9.203892, 9.215492]`, generated bounds
`[-23.21549, 23.21549]`, and full-span increment `-0.01159668` (`0xBC3E0000`).
That historical result is not a new measurement of this regression and must not be
added to the pier's width as a new runtime correction.

The reviewed, committed metaprogram measurements distinguish the parts:

| Part | Prototype complete span (m) | Required relationship |
| --- | --- | --- |
| Arch section | 15.399902 | Receives the composer's structural width plan |
| Main pier body | 15.198853 | Generated main pier width equals generated arch width |
| Separate base | 18.419433 | Keeps the measured base-minus-arch allowance |

These are the existing `TrussArch01Geometry` measurements, not newly inferred bounds.
The current `ModsData/BridgeBuilder/asset-anatomy.txt` is dated 2026-09-14 16:10:01Z:
it contains the prototype but no generated `TrussArch01` sample. At inspection the
runtime registry had no bridge records and the last export report described deletion
of a Golden Gate bridge. Therefore a current generated-mesh audit remains pending.

## Cause and implementation

`BridgeTowers` correctly marks `TrussArchBridge01NetPillar` as a support, so
`selection.Tower` is empty. `Selection.ExtraFor` still reads the recorded prototype
road width (20 m) when sizing the arch. However, `FitTower`'s primary-support fallback
passes the selected road width as the tower's source width. Recomputing
`deckWidth - authored` in the factory then cancels to zero for the pier, irrespective
of the selected road. The pier/arch difference thus varies with the selected road;
it is not the small historical whole-envelope audit increment.

The blue pier now consumes the same structural width plan already passed from the
composer for its arch. Its existing immutable section/pier measurements still align
the main pier with the arch. The separate base uses that original plan directly,
preserving its own allowance rather than acquiring the main pier's envelope. Missing
plans reject generation instead of falling back to the wrong road baseline; plans
are reset at every bridge boundary.

The part delta enters the existing full-detail/LOD derivation path together. No vertex
classification, stretch/translation decision, material, y/z position, other bridge
style, or global structural allowance is changed. The now-unused inverse-delta helper
is removed. This is a width-plan propagation fix, not a runtime audit correction.

The user's ongoing `pre-release` workflow is retained; unrelated working-tree changes
and existing assets are preserved. No cache or save cleanup is part of this repair.

## Remaining validation

Compilation establishes loadability only. No visual unit tests are used. A human must
create and retain a new blue deck truss-arch bridge with the reported road, then retain
its component/geometry dump and compare the main pier and arch bounds at full detail,
LOD1 and LOD2. Also compare near/far views and a second road width to check that the
previous constant-width behavior is gone. The visual fix and new audit are not marked
complete until that evidence exists. Existing saved geometry is not rewritten.
