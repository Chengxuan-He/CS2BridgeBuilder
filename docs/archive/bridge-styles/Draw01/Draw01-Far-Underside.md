# Draw01 far underside

## Diagnosis

The inspected current saved bridge contains the native carriageway bottom on both leaves at
near and middle detail. The far native mesh has no independent central slab at Y = 9.5 after
the selected-road replacement. Its merged truss/atlas representation does not supply that slab.
This explains an LOD-dependent missing underside; the screenshots' exact active LODs were not
captured, so human visual confirmation remains required.

## Change

Integration repair (2026-10-23): the shared render assembler forwarded `ExtraStructure`,
but Draw01 no longer supplied it. `Draw01RenderAssembly` now builds the recorded LOD1
underside subset and attaches it to LOD2 before calling the shared assembler. The source
is the already validated native LOD1 render; no new measured constants are introduced.
`tools/Draw01UndersideCheck` exercises the actual embedded membership arrays against
native OBJ data for asymmetric, single-sided and absent sidewalks. Both leaf bones,
complete lateral coverage and original Y/Z are verified for near and middle subsets;
the middle subset also supplies the far supplement. Current in-game acceptance remains
pending, and existing saved bridges are not automatically rewritten or deleted.

Append the measured middle-detail slab subset to the existing far mechanism render. Preserve its
native material, packed attributes, winding, bones and Y/Z coordinates. Apply the same selected-road
carriageway and independent sidewalk spans used by the other LODs. Road paint never processes this
structural subset. Keep the native far truss geometry and its material unchanged.

Both leaves are covered: 176 and 160 carriageway faces plus 80 sidewalk faces on each leaf when
both sidewalks exist. The source carriageway spans Y = 9.5 through 9.800049 and absolute Z =
0.052679062 through 83.84961. The maximum subset is 496 triangles and 1,018 referenced vertices.
Compact unused vertices from the 18,772-vertex source by exact raw-channel copying and index
remapping. No extra Prefab, texture or material is created; one batch is appended to the far render.

The shared supplement and compaction options are opt-in. Only Draw01 supplies them; ordinary
structural generation keeps its existing vertex/index layout. Preview and persistent generation
use the same models, surface references and existing owned-resource writer.

## Verification and remaining LOD work

Inspected native OBJ face membership, both leaf bones, thickness and complete longitudinal ranges.
Referenced-position remapping preserves the selected original triangles exactly. Release build
passed and all 12 installed payload hashes matched. Two old Draw01 owners were backed up and
removed under the generation-update policy. No other style was selected for cleanup.

New generated-file and in-game near/far/open-pose acceptance remain pending human sample creation.
The existing atlas/paint road geometry is still repeated across LODs; native LOD thresholds depend
on actual index density, bounds and root surface-area bias. Native LodProperties offers one root
bias, which cannot independently align the two observed mismatched boundaries. Threshold parity
is not fixed by this change. Do not falsify counts or use a manager runtime patch to claim parity.

## Follow-up: original bottom UV defect (2026-10-23)

The latest user screenshot still exposes a yellow line on the underside. The previous
coverage repair must not be described as visual acceptance. Near/middle geometry coverage
passes, but the negative-Z leaf's original bottom UVs sample the painted asphalt atlas island.
See [Bottom atlas-coordinate correction](Draw01-Underside-Restoration.md#bottom-atlas-coordinate-correction-2026-10-23)
for measured evidence and checks. The far supplement now receives the corrected LOD1 UV0
before raw-channel compaction. Other attributes and the native surface remain unchanged.
Existing bridges are preserved; new generation and human all-LOD inspection are required
to confirm the installed repair in game.
