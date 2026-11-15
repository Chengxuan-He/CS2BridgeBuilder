# ExtradosedLarge central cable generation repair

## Evidence (2026-09-19)

The failing preview was reported in `Logs/BridgeBuilder.log` at 00:29:19 and
00:29:30. `6-Lane Extradosed Bridge -21 Piece` received the full road-width change
of -21 m and acquired inverted bounds. This is a generation defect, not an audit
result or a request to change the audit formulas.

The retained real prototype dump is
`ModsData/BridgeBuilder/asset-anatomy.txt` (lines 327702–327805):

- Section: `6-Lane Extradosed Bridge`.
- Piece: `6-Lane Extradosed Bridge Piece`; `m_Width = 21`, `m_Length = 128`.
- Geometry: `6LaneExtradosedBridgePillarCables_af25cafa516cc754fb0c41aae8698375`.
- Geometry asset ID: `28ab220c7162571f5effdf1fd5de7c6a`.
- Dumped x bounds: `-0.299219` to `0.299219`; 968 vertices and 1980 indices.
- Section and piece offsets: `(0,0,0)`; section `m_Median = true`.
- Packed vertex layout: Position Float32x3, Normal SNorm16x2,
  Tangent Float32x1, TexCoord0 Float16x2.

These dumped decimal bounds identify the thin central sheet; they are not
bitwise audit inputs. The configured 21 m section width is not its mesh span.
The companion `6LaneExtradosedBridgePillar Placeholder` is already recorded in
`BridgeTowers` as a central, single-column support whose width does not follow
the road.

`ModsData/BridgeBuilder/tower-measurements.txt` lines 142–147 independently show
the same section and central pylon reused by six BXP road/rail variants with
different declared road widths. The source section remains 21 m in each variant.
Those nominal road widths are corroborating prefab relationships, not a new
geometric width audit.

## Source change

`BridgeStyleDefinitions.PreservesOverheadGeometry` records the exact style and
section identity. It does not classify all median sections alike: several other
bridge families also mark their full-width cable frames as median sections.

The central cable section is copied into bridge-owned geometry with its authored
vertices, composition width, packed channels, placement and LODs unchanged.
Preview copies are owned by the `tmp` session; formal creation builds separate
copies under the new bridge identifier. No audit increment is forced to zero,
looked up at runtime, or applied to an existing generated bridge.

Invalid narrowing is rejected before the affected geometry is captured or written.
A failed piece stops the section, and a failed overhead section stops composition
instead of publishing a partial bridge or silently substituting the donor.

## Verification boundary

The source compiles against the installed game assemblies. No game was opened or
controlled, no saved bridge was deleted, and mod-cache structure was not changed.
Visual confirmation of the repaired preview and the formal bridge at near/far
distances remains a human game check; compilation is not that confirmation.
