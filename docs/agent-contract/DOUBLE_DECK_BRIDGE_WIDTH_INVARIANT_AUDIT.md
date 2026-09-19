# Double-deck bridge width invariant audit

## Evidence

- Real-prefab anatomy capture: `2026-09-13 23:29:44 +08:00`.
- Capture SHA-256: `39442F73759F8A2E0C79A1BCD0B59C7E3CDFC21B2D4DD14F247F58A08E621E61`.
- Exact machine-readable measurements:
  [`double-deck-bridge-width-invariant-measurements.tsv`](double-deck-bridge-width-invariant-measurements.tsv).
- Reproducible command: `tools\AuditBridgeWidths.ps1 -DoubleDeck -AnatomyPath <retained-capture>`.

Every bridge width is the complete `maxX - minX` span of the union of the explicitly named structural
object and structural overhead sections. Road and track section geometry is excluded from those
bounds. Road/deck width is measured separately from the root network's active sections and is used
only as the road-width operand.

The root ownership role follows the archetype's `AuxiliaryNets` arrangement:

- `ExtradosedBridge01`: upper root road; lower network is the auxiliary.
- `ExtradosedBridge02`: lower root road/deck; upper road is the auxiliary.
- double-deck Suspension: upper root road; lower network is the auxiliary.

## Bitwise results

| Style | Archetype bridge/road | Generated bridge/road | Width increment | New bridge width | Result |
| --- | --- | --- | --- | --- | --- |
| Extradosed01 | `53.39534 / 20` | `73.39534 / 40` | `0` (`0x00000000`) | `73.39534` (`0x4292CA6A`) | Unchanged |
| Extradosed02 | `45.941 / 19` | `47.941 / 22` | `1` (`0x3F800000`) | `48.941` (`0x4243C396`) | Apply |
| Suspension | `37.500008 / 24` | `53.5 / 40` | `7.6293945E-06` (`0x37000000`) | `53.500008` (`0x42560002`) | Apply |

No result exceeds the `1.0f` limit. The two applicable results are folded into the immutable
archetype road-width parameters and the ordinary `target root deck - prototype root deck` generation
formula. No runtime audit delta or post-generation geometry correction is retained.

## Rail-node turnout

The retained capture shows the original `ExtradosedBridge01 Train Track` and both newly generated
lower train networks with zero edge-state rules, zero node-state rules and the `Train Track`
aggregate. The generated lower network therefore already carries the same transport-compatible state
table as the prototype, while the turnout remains visible in game. State-table copying is not a
supported explanation for the remaining visual result. Per the requested stop condition, no further
node, topology or variant-selection change is made in this pass.
