# Bridge width invariant audit

## Baseline and evidence

- Pre-edit rollback baseline: branch `dev`, exact commit
  `ef70c50a6580c8eadd332d9a620e8c0798c1d035`; durable local reference
  `backup/dev-before-full-span-width-audit-20260910`.
- The superseded one-sided audit is invalid and is not used by this pass.
- Retained running-game evidence:
  `C:\Users\admin\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\BridgeBuilder\asset-anatomy.txt`.
- Evidence generation time recorded in that file: `2026-09-09 16:41:17Z`.
- Machine-readable result: `bridge-width-invariant-measurements.tsv`.
- Reproducible command: `tools\AuditBridgeWidths.ps1`.
- SHA-256 from two consecutive byte-identical runs:
  `5541DEB4CDBABCABA1F4591E5FDCDB1B20073709829A84E485901767E49EB542`.

The evidence contains a loaded matching archetype and a retained manually generated bridge for each
of the 13 audited single-deck styles. No bridge was created, deleted or judged in game during the
audit, and the game was not opened or controlled.

## Width definition and exact formulae

A bridge width is its complete left-to-right span:

```text
bridgeWidth = maxX - minX
```

Both endpoints are independently read from the retained model data and recorded. This is the input
measurement definition, not an additional correction formula. No result is obtained from one side's
x coordinate, `abs(x)`, or twice a one-sided boundary.

The audit itself executes only these two formulae, in this order:

```text
widthIncrement =
    (archetypeBridgeWidth - archetypeRoadWidth)
    - (generatedBridgeWidth - generatedRoadWidth)

newBridgeWidth = generatedBridgeWidth + widthIncrement
```

`widthIncrement` is the increment of the complete span. It is not a per-side displacement. Every
source token is parsed directly as IEEE-754 binary32 (`System.Single`), every operation is performed
on binary32 locals in the written order, and the output records round-trip text and raw hexadecimal
bits. There is no decimal promotion, rounding, display-value reuse, epsilon, tolerance,
normalization, forced zero or bridge-specific result override.

A bridge is skipped if and only if:

```text
abs(widthIncrement) > 1.0f
```

Positive zero is unchanged only when its raw bit pattern is `0x00000000`.

## Results

The brackets in the endpoint columns are the independently measured `[minX, maxX]`. Every width,
increment and result below includes its binary32 bit pattern; the TSV additionally records the raw
bits of both endpoints.

| Style | Archetype endpoints | Archetype bridge width | Archetype road width | Generated endpoints | Generated bridge width | Generated road width | Width increment | New bridge width | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `CableStayed` | `[-23.5, 23.5]` | `47` `0x423C0000` | `32` `0x42000000` | `[-27, 27]` | `54` `0x42580000` | `40` `0x42200000` | `1` `0x3F800000` | `55` `0x425C0000` | Apply |
| `CoveredWood` | `[-4.75, 4.75]` | `9.5` `0x41180000` | `8` `0x41000000` | `[-20.75, 20.75]` | `41.5` `0x42260000` | `40` `0x42200000` | `0` `0x00000000` | `41.5` `0x42260000` | Unchanged |
| `Extradosed03` | `[-28.07413, 28.07413]` | `56.14826` `0x426097D1` | `19` `0x41980000` | `[-38.57413, 38.57413]` | `77.14826` `0x429A4BE9` | `40` `0x42200000` | `-3.8146973E-06` `0xB6800000` | `77.148254` `0x429A4BE8` | Apply |
| `ExtradosedLarge` | `[-10.5, 10.5]` | `21` `0x41A80000` | `32` `0x42000000` | `[0, 0]` | `0` `0x00000000` | `40` `0x42200000` | `29` `0x41E80000` | `29` `0x41E80000` | Skipped |
| `Grand` | `[-31.59456, 31.59456]` | `63.18912` `0x427CC1A9` | `19` `0x41980000` | `[-45.59456, 45.59456]` | `91.18912` `0x42B660D4` | `40` `0x42200000` | `-6.999996` `0xC0DFFFF8` | `84.18912` `0x42A860D4` | Skipped |
| `GoldenGate` | `[-37.01401, 37.01401]` | `74.02802` `0x42940E59` | `25` `0x41C80000` | `[-44.51401, 44.51401]` | `89.02802` `0x42B20E59` | `40` `0x42200000` | `0` `0x00000000` | `89.02802` `0x42B20E59` | Unchanged |
| `Suspension` | `[-18.75001, 18.75]` | `37.500008` `0x42160002` | `24` `0x41C00000` | `[-26.75002, 26.75001]` | `53.50003` `0x42560008` | `40` `0x42200000` | `-2.2888184E-05` `0xB7C00000` | `53.500008` `0x42560002` | Apply |
| `SuspensionGolden` | `[-25.2, 25.2]` | `50.4` `0x4249999A` | `25` `0x41C80000` | `[-32.7, 32.7]` | `65.4` `0x4282CCCD` | `40` `0x42200000` | `0` `0x00000000` | `65.4` `0x4282CCCD` | Unchanged |
| `TiedArch` | `[-11.5, 11.5]` | `23` `0x41B80000` | `19` `0x41980000` | `[-23, 23]` | `46` `0x42380000` | `40` `0x42200000` | `-2` `0xC0000000` | `44` `0x42300000` | Skipped |
| `TrussArch` | `[-7.750001, 7.750001]` | `15.500002` `0x41780002` | `12` `0x41400000` | `[-21.75, 21.75]` | `43.5` `0x422E0000` | `40` `0x42200000` | `1.9073486E-06` `0x36000000` | `43.5` `0x422E0000` | Apply |
| `TrussArch01` | `[-9.203892, 9.215492]` | `18.419384` `0x41935AE6` | `12` `0x41400000` | `[-23.21549, 23.21549]` | `46.43098` `0x4239B953` | `40` `0x42200000` | `-0.01159668` `0xBC3E0000` | `46.419384` `0x4239AD73` | Apply |
| `TrussArch02` | `[-10.4, 10.4]` | `20.8` `0x41A66666` | `19` `0x41980000` | `[-20.9, 20.9]` | `41.8` `0x42273333` | `40` `0x42200000` | `0` `0x00000000` | `41.8` `0x42273333` | Unchanged |
| `TrussArch03` | `[-9.203892, 9.203892]` | `18.407784` `0x41934324` | `12` `0x41400000` | `[-23.20389, 23.20389]` | `46.40778` `0x4239A191` | `40` `0x42200000` | `3.8146973E-06` `0x36800000` | `46.407784` `0x4239A192` | Apply |

Exactly three bridges exceed the full-span threshold and are skipped: `ExtradosedLarge`, `Grand`
and `TiedArch`. Four are bitwise unchanged: `CoveredWood`, `GoldenGate`, `SuspensionGolden` and
`TrussArch02`. The other six have non-zero applicable full-span increments.

`CableStayed` now uses the correct retained-model inputs: the matching archetype is
`Cable-stayed Bridge - XL Road Divided - 8 Lanes`, its bridge span is `47 m`, and its active bridge
road span is `32 m`; the generated bridge and road spans are `54 m` and `40 m`. The prior parser
mistook mutually exclusive street-layout sections for the active archetype road and supplied `40 m`.
With the corrected full-span formula the exact increment is `+1 m`, so it is not skipped.

## Metaprogram application

No audit delta is retained or applied by runtime bridge-generation code. The generated samples in
this evidence already contained the superseded runtime adjustment, so the metaprogramming pass folded
that existing state and each applicable new full-span increment into the original final archetype
parameters in `BridgeStyleDefinitions.cs`, plus the corrected CableStayed prototype-road datum in
`BridgeTowers.cs`. The standalone runtime correction table was removed.

For a skipped bridge, the metaprogramming pass applies no new increment: `ExtradosedLarge` and `Grand`
had no prior adjustment to fold, while `TiedArch` retains its already-generated span as the original
final authored parameter. Bitwise-zero bridges likewise retain their measured generated span without
a new correction step. Runtime now performs only its ordinary target-versus-archetype generation
calculation using these final immutable parameters.

## Measurement selection

- Object boundaries are the union of the exact `min.x + objectOffset.x` and
  `max.x + objectOffset.x` values from the reviewed structural object identities and replacement
  meshes.
- Overhead boundaries are the union of every reviewed section interval
  `[offset.x - pieceWidth/2, offset.x + pieceWidth/2]`.
- `Outer` is the union of the object and overhead intervals, not the greater absolute endpoint.
- Road widths are complete top-level road-section allocations for the active straight bridge state.
  The CableStayed archetype's retained section collection contains mutually exclusive street-layout
  variants, so its reviewed active bridge road span (`32f`) is recorded as metaprogram input rather
  than summing those variants into the invalid `40f` result.

All identities and the single reviewed state-specific road input are explicit in
`tools\AuditBridgeWidths.ps1`. No runtime geometry guess, nominal generator formula, rounded survey
value, one-sided boundary or screenshot measurement is used as an audit result.
