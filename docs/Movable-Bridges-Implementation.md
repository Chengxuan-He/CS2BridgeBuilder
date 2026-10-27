# Bridges & Ports movable bridge implementation

This file retains version-specific implementation evidence, not competing specification revisions.
The current router includes all ten movable generators. Earlier statements about absent routes
describe their recorded implementation stage and must not be used as current admission rules.
Use the current contracts for requirements and Prepared-Materials.md for remaining material migration.

## Reproducible native render-input check (2026-10-20)

`tools/GeometryMetaprogram/MovableRenderInputCheck.py` now verifies all nine newly
routed styles against the recorded native graph and the current installed game
packages. It regenerates SourceRenders in an isolated temporary tree and compares
the complete descriptors with production, then checks source vertex/index totals,
mesh-to-surface slot counts and single-submesh layout. Finally it reads the actual
Geometry/Surface package entries and checks each CID sidecar, byte count and SHA-256.

The initial 79-descriptor check covered mechanism/column/footing inputs only.
The check now discovers every MovableSourceRenders subclass within the nine style
folders, including independent ordinary pillars and approach NetPiece inputs. It
regenerates all seventeen classes through their recorded emitters, compares every
emitted companion source (including approach section data), and verifies generic
Create<NetPiecePrefab>/Create<RenderPrefab> descriptors as well.

Result: all 167 render descriptors and 302 distinct direct native resources pass.
The additional 88 descriptors comprise Draw02 approaches (25), Draw03 approaches
(15), Lift01 approaches (3), and nine ordinary-pillar descriptors each for Draw02,
Draw03 and Lift01–Lift03. Every resource CID, size and SHA-256 matches its installed
package entry. The reconstruction check covers source metadata, geometry counts,
surface slots, recorded components and the emitted source graph. It does not prove
that generated output contains no temporary Prefab references or verify output
asset size, animation, rendering or independent cold loading. Those remain separate
integration/acceptance requirements.

The same tool now traverses the native SurfaceAsset v1 VT header. Of 135 source
Surface assets, 78 use VT; all 74 distinct referenced VTTexture/VTSurface payloads
resolve in the installed Game/BridgesAndPorts VT packages. Full payload reads pass
ZIP CRC checks; header versions, dimensions, stack counts, expected resource type
and VTSurface material back-references also pass. Total referenced VT payload size
is 473,467,076 bytes across the batch, deduplicated by CID. This is native source
resource size, not generated bridge size or a per-bridge packaging requirement.
Built-in inputs remain native shared resources.

The parser follows SurfaceAsset.ReadVTHeader, SurfaceAssetVTHeader/PerStackData,
VTTextureAsset.ReadHeader and VTSurfaceDataHandler.Data from the inspected native
implementation. Original Texture GUIDs in VT headers are authoring provenance;
requiring every such Texture asset would incorrectly reject shipped VT materials.
This check validates referenced payload presence and basic binary structure, not
texture decoding, streaming/mid-mip caches or generated Surface persistence.

The production BridgeSurfaceReferences parser was additionally exercised through
DependencyCopyCheck against all 135 actual source Surface payloads. Independent
expected reference lists combine the recorded texture-property bytes, the native
material library's stack property lists and explicit VT header offsets. All 135
comparisons pass, including ordinary templates 0/2/17/33 and VT templates 0/1.
The existing recursive-copy, original-CID, cycle, missing-payload, owner-local,
idempotence and malformed-binary cases also pass. Built-in resources normally
terminate dependency traversal before parsing; this test establishes parser
compatibility with these native formats, not proof that a generated output graph
has already been persisted or loaded. It does not copy game resources into the mod.

Research reproduction: `export-movable-surface-fixtures.py` writes original bytes
and `.refs` oracles under `movable-surface-dependency-fixtures`; run
`dotnet run --project tools/DependencyCopyCheck -c Release -- FIXTURE_DIRECTORY`.
Game resources, saved bridges, runtime code and localization tables are unchanged.

Run with `python tools/GeometryMetaprogram/MovableRenderInputCheck.py
RESEARCH_DIRECTORY GAME_DATA_DIRECTORY`; the research directory must contain the
retained `all-movable-audit.json`. No runtime code or assets are changed. All twelve
UI-language checks also pass; the new offline tool contains technical diagnostics
only and introduces no game UI text. This increment requires no binary deployment
and changes no non-movable bridge.

## Native section ancestry and Lift04 fragment verification (2026-10-20)

The offline section audit now traverses nested NetSectionPrefab references for
Lift01–Lift05 and PedestrianDraw01/02, including the separate Lift03 railway
network. All referenced sections resolve in the recorded native source graph
(SHA-256 `47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070`).
Lift02 includes three nested train sections; the pedestrian networks include
nested pavement-path sections. None of these eight root graphs contains a
Sidewalk-named section. Their recorded families are railway/subway/highway/alley
shoulders or whole-width pavement paths. This is source-network evidence only:
it does not classify every rendered component or prove selected-road clearance.
Do not remove maintenance shoulders or outer safety structures merely because
the selected road has no Sidewalk section. The structural compatibility audit
remains necessary; absence of a source Sidewalk name is not its acceptance test.

The Lift04 structure harness now checks geometric fragment invariants in addition
to replacement counts. At native width, all 56 LOD0, 56 LOD1 and 16 LOD2 structural
source faces have coplanar, consistently oriented fragments whose summed area
matches their source face. The initial summed-area check was subsequently extended
with the independent coverage checks below. At half-width deltas 0, 2 and 8 metres, the combined
production plans preserve source topology and original Y/Z channels, produce
112/112/120 constructed vertices and replace 12094/5506/14 road/track faces.

The same fragment checks now cover Lift05 (160/124/44 source faces per LOD) and
PedestrianDraw01 (144/127/48). Their combined production plans also pass at
half-width deltas 0/2/8; PedestrianDraw01's far boundary reconstruction preserves
the expected forty adjusted source vertices and leaves width outputs unchanged.
The harness Plan copies match current production verbatim and fixture buffers match
the decoded native caches after float32 conversion.

An independent Python convex-clipping audit reads the C# plans' exported fragments,
projects each into its source triangle's barycentric frame, and checks containment,
positive winding, total area and all pairwise intersections. Across all three styles
and three LODs, 775 source faces and 2,299 fragment pairs pass; measured positive-area
overlap is zero. The checker first verifies coincident, disjoint and partially
overlapping synthetic triangles with known areas. Numerical limits are 1e-4 for
barycentric containment/area and 1e-6 for overlap area. This establishes native-width
fragment coverage within those tolerances, not arbitrary-width structure clearance,
attribute interpolation, animation or rendering.

Research reproduction uses `lift04-structure-check`, `lift05-structure-check` and
`pedestrian01-structure-check` to export `fragments0/1/2.json`, followed by
`check-native-fragment-coverage.py`; `native-fragment-coverage.json` records counts
and maximum overlap. No production geometry, non-movable code, game assets or
user-visible text changed in this verification increment.

Fixture positions and indices match the decoded mesh caches after conversion to
native float32. The harness Plan class was checked verbatim against the current
production class; style recipes and composition plans are linked directly from
production sources. Unity vector operations remain arithmetic stubs. These checks
therefore establish CPU geometry properties, not native rendering, serialized
resource completeness, animation or independent loading. Human acceptance stays
deferred until the batch is ready.

Reproduction artifacts are in the existing research workspace:
`audit-movable-sidewalk-sections.py`, `movable-sidewalk-section-audit.json` and
`lift04-structure-check/Check.csproj` (run with its directory as the fixture
argument). This increment changes no runtime behavior, non-movable bridge,
installed binary, saved asset or user-visible text; localization is unaffected.

## Current executable verification coverage (2026-10-20)

After the removal of special movable-width diagnostics, the following production-
linked checks were rerun successfully. Their boundaries matter: none creates a
native game bridge or proves serialized resource completeness.

| Check | Verified scope | Not established |
| --- | --- | --- |
| RecordedVariantCheck: 88 assertions | All nine newly routed styles; immutable measured identity, selection without an original network Prefab, prerequisite availability, Lift03 dual-deck separation and mode selection | Actual render reconstruction, source resource availability and game generation |
| Lift01SectionRolesCheck: 5 checks | Selected section layer roles, recursive subsections, invalid external graph rejection and source preservation | Native composition execution |
| Lift02SectionRolesCheck | Intersection-only median versus span roles, requirements preservation and missing section rejection | Native network assembly or other styles' role mapping |
| MovableMaterialSlotsCheck | Reference-only canonicalization, 70 preserved provenance records, 64 repeated slots reduced to one batch | Full generated LOD-group batch count and native rendering |
| MovableSidewalkCheck: 36 Draw02 combinations | Three LODs, absent/one-sided/unequal sidewalks, transformed frames and unrelated structure preservation | Other mechanisms or native rendering |
| MovableSidewalkCheck: 24 Draw03 combinations | Detailed joint removal, sidewalk/frame combinations and carriageway preservation | Far-level joint behavior, other mechanisms or native rendering |

The small-sidewalk cases in the existing sidewalk fixture exercise the explicit
selected-sidewalk requirement; they are not narrow-road special adaptation.
The fixture links only Draw02FixedSidewalks and Draw03JointLayout, so it cannot
prove structural sidewalk compatibility for Lift01–Lift05 or PedestrianDraw01/02.
Those seven styles do construct MovableSelectedRoadLayout for road appearance,
but that alone does not demonstrate adaptation of all carried structural parts.
This coverage gap remains active work. No non-movable code, runtime strings,
installed binaries or saved assets changed during this verification increment.

## Narrow-road adaptation withdrawn by the user (2026-10-20)

The user now requires no special road-width adaptation for movable bridges and
explicitly excludes non-movable bridges from this change. The ordinary measured
width-difference mappings remain. The seven per-style ValidateWidth overrides,
their shared hook, thirteen centre-crossing diagnostic constants and matching
metaprogram output have been removed. The dedicated PedestrianDraw02 narrowing
review tool has also been removed after backing up its original.

This removes the dedicated narrowing diagnostics, not the native source-part
maps, road bones, material processing or finite-input validation. Geometry/seam
validity checks remain; no minimum road width, automatic widening, clamping or
replacement geometry was added. Extreme narrow-road compatibility and the
investigations below are no longer completion gates for this batch. They remain
historical evidence, not a claim that arbitrary narrow inputs are supported.

Non-movable bridge code and rules were not changed. Existing persisted bridge
assets were not regenerated or migrated. Original edited files are backed up in
artifacts/backups/20261020-movable-no-special-width.

Verification: all thirteen affected metaprograms regenerate matching construction
sources after whitespace normalization; comparison against backups confirms that
only the diagnostic constants were removed from those geometry sources. Release
compilation, thirty route checks and all twelve UI-language checks pass. No new
user-facing strings were introduced. With Cities2 absent, DLL/PDB were backed up
under artifacts/backups/20261020-movable-no-special-width-installed and deployed
to 160320_17 with matching hashes:
DLL `5F281C32A49DD6C652B74DE5CCD562C8B8E4375B3BE540A71A5FA212B4526948`;
PDB `07C8DC65B2676D82B067666FD57EA05E70399826EF0CA569EB2CA8AAEAD0F3B7`.
Native game acceptance remains deferred to the completed batch.

## Native cable material conversion implemented (2026-10-20)

The cable counterpart search below exposed a real appearance fallback but treated
the historical no-descriptor rule too narrowly. The later approved converted-road
appearance policy already permits owner-local native object descriptors. The
measured textureless cable therefore no longer requires an unrelated object
material donor: MovableCableSurface creates a native Default descriptor with the
actual cable color and native metallic/smoothness/snow values, plus a persisted
zero control map to disable unintended palette regions.

MovableRoadAssets applies this conversion only to the measured cable CID and
shares one pair across all its LODs, including duplicate source descriptor
instances. Other lane mappings, original structure slots, geometry, road bones
and marking behavior are unchanged. Preview lifetime and failed-generation
resource ownership use existing mechanisms. See Movable-Road-Material-Reuse.md
for exact coefficients, provenance and persistence scope.

Release compilation, 30 route checks, all twelve UI-language checks and the new
native-payload coefficient audit pass. No UI text was added. Native visual and
cold-load acceptance remain pending; coefficient preservation is not full shader
equivalence. PedestrianDraw02's narrow-road partition remains unfinished.

With Cities2 confirmed absent, DLL/PDB were backed up under
artifacts/backups/20261020-native-cable-surface-installed and deployed to the
existing active 160320_17 installation. Installed hashes match the build:
DLL `5FA23B29F08F559E513D0BB43AB0C186DD4525AF83DD1145EF70048D1715C66A`;
PDB `25D78849E5295A46BE964AB7C12CC5038F038A71338666822DAAAA3BA522E742`.
Healthy generated bridges and all other installed files were preserved.

## Narrowing audit: facing and height do not resolve ownership (2026-10-20)

The previous coverage audit now separately evaluates upward-facing triangles and
their barycentric heights. At width 4 m, all sampled cross-bone conflicts disappear
when downward-facing triangles are excluded. At width 2 m, however, 1,808 of
100,000 samples remain covered by upward triangles from different bones; their
heights are exactly equal in the decoded source data. Both detailed LODs agree.
Thus neither backface exclusion nor a highest-surface rule resolves the general
signed-width case.

An additional deterministic convex-clipping script,
research/audit-pedestrian02-face-intersections.py, checks all cross-bone
upward-facing source-triangle pairs, restricted to the selected road's lateral
span. Its clipping sanity checks cover coincident, disjoint and partially
overlapping triangles of independently known area. At width 2 m it finds 16
positive-area pairs in each LOD, with pairwise intersection areas summing to
5.626530170440674 square metres. This sum is explicitly pairwise, not a union
area. At widths 4 m, 8 m and 12 m it finds no such pairs. Source Geometry hashes
are checked before decoding; exact face IDs, bone pairs, heights and individual
areas are recorded in pedestrian02-face-intersections.json.

This strengthens the source diagnosis beyond random sampling but does not
implement a replacement motion partition. The current source contains genuinely
coplanar competing fixed/moving ownership after extreme narrowing; a unique
partition cannot be recovered merely by choosing the visible upper triangle.
No new runtime geometry classifier, priority rule, width clamp, structural bone
change or generated-asset modification has been introduced. No user-visible
text changed; localization behavior is unaffected.

## Narrowing audit: polygon ownership is insufficient (2026-10-20)

The reproducible research script audit-pedestrian02-overlaps.py verifies both
native Geometry hashes and compares deformed source-triangle coverage with the
recorded boundary-loop even/odd classifier. It samples 100,000 positions at each
of six signed half-width displacements, independently for LOD0 and LOD1.
Both levels produce identical results:

| Half-width displacement | Selected width | Multi-bone triangle samples | Polygon/triangle disagreement samples |
| --- | --- | --- | --- |
| -1.91149202 m | approximately 4.177016 m | 0 | 0 |
| -2 m | 4 m | 308 | 288 |
| -3 m | 2 m | 6,664 | 31,098 |
| -3.99 m | 0.02 m | 0 | 0 |
| 0 m | 8 m | 0 | 0 |
| 2 m | 12 m | 0 | 0 |

Conflicts are between fixed bone 0 and moving bone 1 or 2, not between the two
moving leaves. For example, at displacement -2 m, (x,z) approximately
(0.01999225,60.42902484) belongs to source triangles of bones 0 and 1;
(0.08269402,-59.65885405) belongs to bones 0 and 2. Thus the narrowing defect
is not just a sorted-station implementation problem. Signed deformation folds
the original triangulated surface, and an even/odd boundary polygon can cancel
coverage retained by overlapping source triangles. Polygon-only comparisons
would be an inadequate oracle for a replacement partition.

These are sampled coverage results, not an exhaustive geometric proof or visual
acceptance. They rule out blindly sorting stations or replacing the guard with
the deformed boundary-loop classifier. A repair must define consistent generated
road and attachment ownership without changing original structural bone binding
or silently assigning overlap precedence. This remains unfinished. The full
results are in research pedestrian02-overlap-audit.json. No production code,
installed binaries, saved bridges or user-visible strings changed in this audit.

## Remaining narrow-road partition rejection reproduced (2026-10-20)

A direct C# check links the current PedestrianDraw02RoadPartitions.g.cs and the
installed Unity math library, substituting only the Partition container. Widths
0.02 m, 2 m and 4 m throw the explicit hinge-station ordering exception; widths
4.18 m, 4.2 m, 8 m and 16 m produce finite frames. This is a production-method
reproduction, not a game-generated sample. RoadPreparation and ObjectAssembly
both call this method, so the earlier removal of structural output-span guards
does not provide unrestricted narrowing for this style.

The measured left station -1.91149902 (weight -1) reaches the fixed centre
station -0.0000070000533 at half-delta approximately -1.91149202 m, corresponding
to total selected width approximately 4.17701596 m. The generated balanced
slab tree assumes monotonic stations; simply deleting its exception would leave
an invalid ordering and a zero denominator at the collapse boundary. The paired
PedestrianDraw02RoadPartitions.py emitter contains the same guard. Correcting
the full signed motion partition, consistently for road and attachments, remains
implementation work. No clamp, station sorting or arbitrary bone precedence
was introduced by this audit. Research pedestrian02-partition-check contains the
reproduction. Existing production binaries and bridge assets remain unchanged.

## Native cable counterpart scan (2026-10-20)

A direct scan of installed Content/**/Blob.cok, independent of the earlier
candidate cache, inspected small (at most 2048-byte) Surface/Material payloads
with supported object templates: 16,839 template-0, 1,268 template-1, 346
template-11, 624 template-14 and three template-35 files. None exposed an explicit
near-neutral _BaseColor below 0.08 per channel through the simple color-property
encoding used by the native cable. Research native-dark-object-materials.json
records the scan counts. This is a bounded search, not proof that no suitable
textured, VT, larger or default-color object asset exists. No arbitrary donor
material or new descriptor was substituted. Cable appearance remains open.

## Cable material equivalence remains open (2026-10-20)

The four native train/tram contact/catenary render inputs share Surface CID
106cc56e9f15a135bb6ea7b99d675c2d. Its original asset is
Powerline Cables_207901033b5d7c049b968f36a8a91ad9.Material in Content/Game/Blob.cok.
The 83-byte payload has SHA-256
8ea1eb5ec3e434c92c7dd010f71aaab58e8cf37b1a28e48f2757412edf3ca219,
template 30 (CurvedTransparent), shader 570, no texture references and native
_BaseColor approximately (0.018868, 0.018868, 0.018868, 1). This also confirms that
the preceding shader-570 deformation evidence applies to these actual cables.

MovableSurfaceAssets.ExistingObject does not classify template 30 as an object
surface and has no explicit mapping for this CID. It therefore returns the
prototype material supplied by MovableRoadAssets. The geometry conversion repair
must not be presented as faithful cable appearance: the native solid dark cable
color is not preserved by that fallback. Current material policy documents this
fallback; it is not evidence of a matching native object material.

A first inspection of the existing material candidate cache found no verified
counterpart. The cache is not an exhaustive proof of absence, and textureless
VT headers must not be mistaken for simple color-only materials. Research
movable-cable-material-audit.json records exact payload evidence. Further source
material identification remains necessary; no guessed material mapping, new
surface exception, or shader change was introduced by this audit. Installed
binaries and saved bridges are unchanged.

## Repeated material-slot batching repair (2026-10-20)

MovableLaneGeometry appends material slots for each lane batch/tile. Lift03 also
concatenates independently prepared deck slots. BuildMaterialMeshes previously
grouped exclusively by numeric slot, producing separate models for repeated
references to the same Surface. This could exhaust the native 16-batch graph
limit solely from repeated cable/rail occurrences, even when their material was
identical. Existing later descriptor conversion did not merge those models.

BuildMaterialMeshes now canonicalizes exact Surface object references to their
first slot before grouping. Equal names, equal descriptor values, or equal CIDs
on different objects do not authorize merging. The operation applies only to
already converted road/lane models; original structural model slots retain their
order. It retains every triangle, occurrence, source-face identity and bone,
without welding, simplifying or cutting geometry. Existing per-model index and
bone validation remain responsible for the resulting geometry.

MovableMaterialSlotsCheck exercises production canonicalization, deliberately
value-equal distinct descriptors, 70 independent face/bone/occurrence records,
64 repeated cable slots, empty input and null rejection. The 64 cable slots now
map to one batch. This demonstrates the duplicate-slot defect and its local
repair; it does not prove every selected road fits under the graph-wide limit.
The native 16-batch guard remains unchanged, including genuinely different
materials and structural/road batch totals.

The 612-vertex cable deformation check, Release compilation, 30 route checks and
all twelve UI-language checks pass. No UI strings changed. With Cities2 absent,
DLL/PDB were backed up under artifacts/backups/20261020-movable-material-slots-installed
and deployed to the existing active 160320_17 installation with matching hashes.
DLL SHA-256: C1F6ED4DF33FE83455042C95DB05B459D355572B9F6B21A822679A93D5F8F7AE.
Existing generated bridges remain untouched. Native batch counts, exact cable
material equivalence, persistence and human acceptance remain to be verified.

## Neutral-wind cable conversion implemented (2026-10-20)

The shared evaluator now accepts hanging/swaying cable batches in the native
closed, neutral-wind pose. This resolves the unconditional conversion rejection
recorded below; the earlier entry remains diagnostic history. Native LaneSystem's
static sag stays in the supplied curve and is not applied a second time.

The extracted SG_CurvedShader 570/10 vertex program computes sine phases from
world position/time, multiplies them by HangingDistances (CB3[40]), and adds the
result only to the curve's X/Z control points before evaluating position and
derivative. With zero wind these offsets vanish; Y control points, curve
weights and native dilation remain unchanged. BatchDataSystem's HangingDistances
are multiplied by sampled wind magnitudes. The explicit saved mesh therefore
uses the existing evaluator's undisturbed curve, not a guessed straight cable.
This conversion does not reproduce time-varying wind sway in the saved object.
No native dynamic-wind equivalence is claimed.

Non-decal lane triangles continue through MovableRoadPreparation and
MovableRoadAssets with their original lane occurrence and selected-road motion
partition. Existing object conversion handles materials and original road bone
binding; this increment neither introduces a child cable object nor a runtime
synchronization/shader dependency. Source-path inspection is not proof that all
selected-network attachments and cold-loaded material references work in game.

The repository-owned MovableLaneDeformationCheck links production evaluation and
actual Unity math types. Before the repair its hanging case threw the recorded
NotSupportedException. Afterwards all 612 vertex cases pass: independent analytic
sag, endpoints, reversed direction, normalized/orthogonal tangent frames,
handedness, dilation UVs and ordinary/hanging parity. Decal-volume rejection is
retained. Static geometry verification does not establish dynamic wind, complete
cable attachments, native rendering, persisted reload or batch acceptance.

Release through tools/Build.ps1, 30 exact-route checks and all twelve UI languages
pass. No new player-visible strings were introduced. With Cities2 absent, DLL/PDB
were backed up under artifacts/backups/20261020-hanging-lane-installed and deployed
to the existing active 160320_17 installation. Both installed hashes match output.
DLL SHA-256: 1743A2E3D5986923AD88F79551E99067B4FCBDEDB2ED65864BBA0DA044F65A23.
Healthy saved bridges were not modified. Human acceptance remains deferred.

## Open selected-network cable conversion defect (2026-10-20)

Native reference-graph inspection found a shared implementation gap that route
and width checks do not exercise. MovableLaneVisuals preserves the native
CurveProperties.m_HangingSwaying flag, but MovableLaneDeformation.Evaluate throws
NotSupportedException for every non-decal batch carrying it. MovableLaneGeometry
calls that evaluator for each mesh vertex. Such selected-road cable batches
therefore cannot currently complete conversion. Do not treat cable removal from
the original structure as sufficient replacement support.

| Native lane | Lane ID | Render ID | CurveProperties ID | Reachable native bridge roots |
|---|---|---|---|---|
| Train Catenary Cable Lane | 121042 | 121041 | 73106 | Lift01, Lift02, Lift03 auxiliary and main |
| Train Contact Cable Lane | 121044 | 121043 | 137994 | Lift01, Lift02, Lift03 auxiliary and main |
| Tram Catenary Cable Lane | 121098 | 121097 | 180736 | Lift05 |
| Tram Contact Cable Lane | 121100 | 121099 | 47056 | Lift05 |

These are reverse-reference paths in the decoded native all-movable-audit.json,
recorded in research movable-hanging-lane-audit.json. Reachability is not proof
that every conditional network selection produces the batch. Conversely, any
other bridge using a selected road that produces these batches uses the same
unsupported evaluator. No runtime failure was invoked for this audit.

Native LaneSystem already writes static cable sag into Curve.m_Bezier: it places
intermediate control points at one-third/two-thirds of the endpoint chord and
subtracts horizontal chord length times UtilityLaneData.m_Hanging times 4/3
from their Y coordinates. BatchDataSystem separately computes HangingDistances
from endpoint distances and hanging ratio, then multiplies by sampled wind
magnitudes. ManagedBatchSystem sets BatchFlags.Hanging for the corresponding
CurveProperties. Static sag and wind displacement are distinct; do not apply
sag twice or assume deleting the rejection preserves native deformation.

The next implementation must verify the native curved-shader handling of these
inputs and provide a persistent, original-road-bone-compatible representation.
Skipping cable batches, relying on a mod update loop, or substituting straight
wires would not complete the selected-road requirement. No runtime source,
installed binary or generated bridge asset changed during this audit. Human
batch acceptance remains deferred; this is a concrete implementation defect to
resolve first, not an acceptance-only item.

## Current batch baseline (2026-10-20, 20:21 local)

All ten movable styles have concrete generator routes: Draw01/02/03,
Lift01/02/03/04/05 and PedestrianDraw01/02. Draw01 alone has prior user
acceptance; the other nine remain under compatibility and native verification.
Earlier counts and incomplete-route notes below are historical increments.
This summary does not override later per-style evidence or claim acceptance.

The current complete Release output passed 30 route checks, 88 recorded-variant
checks and all twelve UI-language checks. With Cities2 absent, twelve managed,
UI and dependency files were backed up and deployed into the existing active
`.cache/Mods/pdx_mods/160320_17` installation. Every installed file hash matches
the Release output. The native Burst binary, package metadata and all saved
bridges were preserved. This is local development deployment, not publication.

- Original installation backup: `artifacts/backups/20261020-202138-movable-batch-installed`.
- DLL SHA-256: `4113281D3DF19C3055128F693E0AED53760D5F7881A8C7F0BC99679F955E12E6`.
- PDB SHA-256: `1743EEC3DB3427BC75C16B1F543223FCF88D785EF51596916B958CDD5E3F7264`.

This supersedes the prior Lift04-only installed baseline and includes Lift03's
coordinated dual-deck route plus subsequent signed-width compatibility work.
Pending work includes selected-sidewalk compatibility, complete generated-resource
size/reference checks, native creation and cold-load verification. Human motion,
appearance and traffic/shipping acceptance remains deferred to the completed
batch. Compilation, source-route checks and deployment do not prove those outcomes.

## Lift05 incremental generator route (2026-10-20)

Lift05 now connects reconstructed source data, selected-road sampling, native road
appearance, all-LOD structural/glass rendering, supports, original mechanism,
skinned paint, section conversion and unlocks through its own generator route.
The native intersection-only median uses its recorded exception; other occurrences
use the three span branches while the common converter preserves selected-road
conditions. The built-in pillar placeholder retains native reverse references.

The measured road width is 8 m: two 3 m tram strips and two 1 m shoulders. The old
30 m entry counted incompatible branches and is corrected in the shared table and
generator constant. Evidence: native section IDs 121114/121115 and the fixed
carriageway X=-4..4; zero-width side sections and intersection median add no width.

There are now six routed movable styles (Draw01, Draw02, Draw03, Lift01, Lift05,
PedestrianDraw02), but only Draw01 is user-accepted. Release compilation, 26 exact
route checks, 42 recorded-variant checks and all twelve UI languages pass. Lift05
still requires selected-sidewalk and narrow-road compatibility, full resource-size
measurement, native generation/persistence verification and later human batch
acceptance. Routing is not a claim that these remaining requirements are satisfied.
No new UI labels were needed: the registered localized Lift05 entry now reaches
its concrete generator. Earlier status entries below are historical increments.


Current appearance selection: [Existing Material Reuse](Movable-Road-Material-Reuse.md). Earlier
baking/descriptor-generation descriptions below are historical and superseded by this revision.

## Lift01 increment deployment (2026-10-20)

The latest game Modding.log identifies the active installation as
`.cache/Mods/pdx_mods/160320_17`, rather than the hidden `Mods/.BridgeBuilder`
copy. With Cities2 confirmed stopped, the twelve managed build/UI/dependency
outputs were installed into that existing active directory and every SHA-256
was checked against the Release output. No second installation was created;
existing native Burst binaries, package metadata and generated bridges were
preserved. Original overwritten files are backed up under
`artifacts/backups/20261020-025249-lift01-installed`.

Installed `BridgeBuilder.dll` SHA-256:
`E5B1F0A2D6C4BA294C714469DBC8F3DD7C985E37ACAD762742656B1A6622C5AF`.
This is a local development deployment, not a Paradox Mods release. The game
has not subsequently been launched for validation; native generation, motion,
persistence and visual acceptance remain unverified. Human batch acceptance
remains deferred as requested. Lift05's next outstanding implementation stage
is the main mechanism's authored-part width mapping and road conversion;
its existing support, glass and counterweight helpers do not complete it.

## Lift01 increment (2026-10-20)

Lift01 now has an incremental generator route and recorded catalogue variant.
The shared lifecycle connects source reconstruction, selected-road sampling,
three-LOD mechanism/paint, stacking supports, ordinary pillars, approaches,
native network branches and unlocks. Five movable styles are routed; Draw01
remains the only user-accepted style. Release compilation, 25 route checks,
42 recorded-variant checks, five section-role checks and 12-language checks pass.
Selected-layout compatibility, unrestricted narrowing, complete resource/cost
measurement and native generation/persistence remain incomplete.

The far mechanism assembler now combines the measured tower-core supplement with
six native road-end lips, retaining fixed bone 0 and moving road bone 4. The lips
add 288 original triangles to the 12,982-triangle tower contribution. Near/middle
levels already retain those same authored parts. Far base profile reconstruction
now replaces destructive seam trimming: native caps and underside remain, with
the inner rim and end stations matching the detailed road envelope. Full
selected-layout compatibility and native persistence verification remain
unfinished. See [Lift01 integration](Lift01-Network-Implementation.md); this is not
an additional accepted style or evidence of successful native generation.

## Current implementation status (2026-10-19)

Draw03 now has an incremental generator route and recorded catalogue variant. Its
source reconstruction, road/material preparation, all-LOD mechanism, paint, supports,
ordinary pillars, approaches, network components and unlock data are connected through
the shared generation lifecycle. Release compilation, 24 route checks, 32 recorded-
variant checks and 12-language checks pass. There are now four routed movable styles:
Draw01, Draw02, Draw03 and PedestrianDraw02; only Draw01 has user acceptance. Draw03
sidewalk/narrow-width compatibility and native generation/persistence verification
remain incomplete. This is not an additional completed or accepted style.

Draw03 approach assembly now connects all fifteen transformed native render inputs
to six structural sections and 33 recorded network occurrence branches. Only the
first strip on each side gains width; outer strips retain native dimensions and
move through the section cursor. Selected-road intersection lanes are not duplicated.
Release, localization and generator-structure checks pass. Final Draw03 routing,
selected-sidewalk compatibility and unrestricted narrowing remain incomplete.

Draw03 fixed approaches now have a five-family, three-LOD geometry plan. The
road-bottom base uses its measured stationary centre seam, side members translate
rigidly and central V braces retain own-span stretching. All original vertices and
triangles remain. Four-width checks, complete original/widened offline sections,
Release compilation and localization checks pass. Network layout/assembly,
selected-sidewalk compatibility, unrestricted narrowing and final routing remain open.

Draw03 central approach braces now have all-LOD literal maps for sixteen stations.
The local transform accounts for the first strip's moving origin, preserving each
complete mirrored brace's own-span stretch. Four-width placement/winding checks,
offline original/widened views, Release compilation and localization checks pass.
The remaining connected end/longitudinal shell requires separate part mapping;
this stage does not yet add the full approach assembler or final Draw03 route.

Draw03 ordinary-pillar assembly now connects every top/column/footing LOD to the
recorded native object recipe. Far top/column shells restore measured rigid-side
boundaries and retain their original surfaces: 350/454 resulting triangles versus
300/400 original triangles. Original/widened offline views, area/winding and four-width
checks pass. Five approach families also reconstruct their fifteen native render inputs,
including native fees and texture-tiling flags. Their geometry/layout integration,
width/sidewalk compatibility and the final Draw03 generator route remain unfinished.

Draw03 ordinary-pillar inputs now reconstruct all nine native top/column/footing
renders and their object/LOD/stack/base/content settings from the recorded audit.
Detailed top/column maps and all three rigid footing maps retain native triangles.
Original and widened offline views, rigid-side/winding checks, Release build and
localization checks pass. Far top/column assembly and ordinary approach geometry
remain unfinished; this stage does not yet add the Draw03 generator route.

Draw03 now assembles all three native column and footing render levels. The far
shells use measured inner side-body envelopes from the detailed source, preserving
native vertices and interpolating original channels only on necessary boundary
subdivisions. Far column triangles increase from 800 to 994; footing triangles
increase from 200 to 320. Source-area, winding and rigid-side checks pass at four
widths, and original/widened far views were inspected offline. Native Y/Z bounds,
surface slots and stacking metadata remain intact. The final generator route,
ordinary approach/pillar integration and width/sidewalk compatibility remain open.

Draw03 now has road appearance and preparation stages. A reviewed 968x164-pixel
paint-free crop from its native road atlas repeats at the original UV gradients;
sidewalks retain their own native crop and independently anchored outer edges.
Fixed inner/outer units retain separate 8 m / 7.958984375 m longitudinal periods.
Direct decoding checks 186 native top faces, with transverse agreement below 1e-7
and longitudinal phase error below one source texel. The shared conversion receives
the recorded three-bone carriageway/sidewalk partitions at all LODs. Far supports,
selected-sidewalk structural compatibility and final routing remain unfinished.

Draw03 detailed column/footing plans now preserve rigid side bodies and the measured
central panel seams. Near/middle retain all 4,056/2,433 column and 1,408/844 footing
triangles, with no cuts or new vertices. Native centre residues are explicitly
stationary. Four-width edge/winding checks, widened offline views and Release build
pass. Far support boundary reconstruction and the complete support/render route remain
unfinished; this stage does not add a completed generator.

Draw03 now has a far structural plan and a three-LOD mechanism render assembler.
The native 448-triangle far shell becomes 536 structural triangles after measured
wall-boundary subdivision and road-interior removal, retaining exterior shoulders,
control rooms and original bone ownership. No detailed mesh is copied into far LOD.
The assembler connects main and glass plans to the shared native surface/road pipeline;
road preparation, support renders, width compatibility and final routing remain unfinished.

Draw03 now composes its near/middle main-mesh structural maps through
`Draw03DetailedStructure`. Road replacement removes 312 measured native road/curb
faces per level while retaining exterior edge profiles. The composed offline audit
covers every surviving vertex exactly once and passes four-width winding checks:
126,836 near and 76,102 middle triangles before new selected-road/paint geometry.
This does not complete selected-sidewalk compatibility, narrowing, far LOD or routing.

Draw03 fixed platforms now preserve lateral supports rigidly and stretch the measured
central wall between X=+/-10.37841796875 m. Near/middle maps cover 3,236/2,951 original
vertices. Near requires no subdivision; middle restores the same part boundary through
20 source faces and 30 interpolated edge vertices. Offline widened views, area/winding
and four-width checks pass. Original road-region replacement and far/full assembly remain open.

Draw03 now has eight end-strip maps per near/middle LOD, covering 540/512 vertices.
Source inspection and four-width checks preserve original moving/fixed bone ownership.
The five current main-mesh mapping families are disjoint and, together with six remaining
components, account for every original vertex and triangle. Remaining near/middle
components contain 4,276/3,955 vertices: fixed support platforms and native road regions.
Their separate structural/selected-road handling and the complete far assembly remain open.

Draw03's road-adjacent bases now have literal left/centre/right maps for 19,436 near
and 19,858 middle vertices. Original centre-seam residues remain stationary; side
vertices translate without changing Y/Z or bone ownership. Four-width checks and
offline plan views pass. A -0.5 m half-width test exposes inverted centre connections;
narrowing below the measured 0.24432373046875 m side distance remains unresolved.
The complete main/far render assembly and generator route are still outstanding.

Draw03 now records 152 central members, longitudinal frames, end housings and connection
plates in each of the near and middle meshes (8,388/7,287 vertices). Own-span transforms preserve original
bones and complete triangle ownership. The smallest span is 0.5532913208007812 m;
unrestricted narrowing is not implemented. These tables compile but are not yet a full
render assembly or a new generator route. See the current Draw03 network stage record.

Draw03 now has own-span maps for four fixed rollers and both leaf axles, covering
23,016/16,937 near/middle vertices. Offline inspection identified the positive axle's
49.6-micrometre authored seam; its two halves are no longer treated as independent rigid
side parts. Corrected rigid totals are 130,422/98,439/78 vertices. Original bones and
triangle winding are preserved. Full main-mesh and far assembly remain incomplete.

Draw03 main-mesh side mapping now covers 130,818/98,731/78 vertices across near,
middle and far LODs. Middle side fragments retain detailed same-side/same-bone envelope
correspondence. Four-width checks preserve triangle edges and unmapped vertices; offline
three-LOD views were inspected. The 176 detailed crossing components and four merged far
components still require separate plans before the complete generator can be assembled.

Draw03's separate glass submesh now has immutable rigid-side maps for 704 near and
160 middle vertices, retaining 360/88 source triangles and fixed bone 0. Four-width
edge checks and offline views pass. Far LOD has no independent glass submesh. The main
structure and complete render assembly remain outstanding; this is an unintegrated stage.

Draw03 now has measured carriageway and sidewalk motion partitions. Native near top
faces establish leaf ends at +/-80.015625 m, fixed starts at +/-80.041015625 m and
separate central half-gaps of 0.02740478515625 / 0.02741241455078125 m. Object assembly
uses the original-bone carriageway partition. Road preparation, structural widening and
full generator integration remain incomplete; no new generator route is admitted here.

Draw03 object assembly now separates twelve fixed flagpole/wall-light attachments from
eight native lamp records, using four unique longitudinal stations for selected-road lamps.
Its measured road width is corrected from the repeated-branch total 78 m to 25 m, and the
audited sidewalk barrier producer is connected through shared native node rules. Native
attachment fields were checked against record 30008. Main/support geometry, physical seams,
road appearance and full routing remain incomplete; no new completed style is claimed.

Draw03 now has a network assembly stage joining its measured section branches, bridge
properties, mechanism states and three structural placements. Factory ownership is retained,
and selected-network outside connections keep their full original rules instead of inheriting
the prototype road's Y=5 anchor. Release and localization checks pass. Render construction,
road/paint conversion, barrier correspondence and full generator routing remain incomplete;
this stage does not add an accepted style or a new generator route.

Draw02 now adapts its four fixed sidewalk nose slabs to independently evaluated left/right
sidewalk spans, removing a side's slab when its sidewalk is absent. The near/middle source
membership is 44/40 faces; the native far proxy has no separate slabs and retains its common
structure. Material preparation and structural assembly share one selected layout. Composed
native/absent/one-sided/unequal layout checks and Release compilation pass. Other fixed
closures and the narrowing limitation remain open; this is not complete Draw02 acceptance.

Draw02's top-removal records now preserve the fixed exterior service-platform tops at every
LOD. Near/middle retain 48/40 original triangles; the far mesh retains eight whole faces and
sixteen measured exterior fragments from twelve merged faces. The composed structural audit
passes at half-width deltas -0.1/0/2/8, with structural totals 19508/11014/536 before road and
paint. This corrects an overbroad removal mask, not the remaining in-corridor sidewalk/closure
adaptation or narrowing limit. See the latest section of `Draw02-Network-Implementation.md`.

Draw02 and PedestrianDraw02 now have recorded catalogue variants. Their identity,
road/structure dimensions, network kind and icon no longer require the original
network Prefab to be registered. No synthetic network is created, registered or
saved. The original built-in BridgesAndPorts content resource supplies the live
DLC availability gate; geometry, surfaces, default base, section dependencies and
unlock inputs remain required native resources, validated by their existing resolvers.
When that content resource exists, the catalogue uses the recorded variant instead
of rediscovering copies of the original network. Other styles retain their current
catalogue behavior until their generation and unlock inputs are similarly independent.

Selection, source labels, UI icons, migration icon recovery and catalogue diagnostics
support these donor-free records. The installed-prototype material audit only traverses
variants with an actual prototype; generated render validation is unchanged. Attempts
to use recorded variants through a donor-copying generator fail explicitly. Both routed
generators already override native unlock capture and reconstruct their mechanism and
network components from recorded tables. Geometry limitations are not waived by this
catalogue change.

`RecordedVariantCheck` compiles the real variant/selection implementation with native
API boundary substitutes. Its 22 checks cover absent original networks, dimension and
icon metadata, selection/filtering, prerequisite availability changes, single-deck
restrictions and retained native-variant behavior. They do not prove native DLC detection,
game rendering or cold-load acceptance.

Draw02 and PedestrianDraw02 now obtain native unlock snapshots from recorded
dependency inputs rather than their original network entity. `ApplyUnlocks` is a
generator hook used by the existing composition/persistence path; fixed styles
and the accepted Draw01 route retain their previous capture behavior. The new
resolver accepts only the exact recorded built-in PrefabID and uses initialized
native progression graphs, retaining all/any flags and private persisted groups.
It does not flatten OR conditions or copy the current unlocked state.

`MovableUnlockInputs.py` emits source-specific tables for all ten movable styles
and Lift03's auxiliary train network. The native component dependency methods and
`PrefabInitializeSystem.LateInitializePrefab` determine their inputs: sub-objects
and auxiliary networks are ignored as unlock dependencies; sections, aggregate,
service, UI group, content, asset pack and effects remain included where declared.
The emitter validates every reference against the original root/component audit
and a hashed typed-reference extraction. Native `IsUnlockable` filters the inputs
before snapshotting. Draw02 has 34 implicit entries and one explicit requirement;
PedestrianDraw02 has 15 implicit entries and two explicit requirements. Duplicate
native entries are retained at capture and deduplicated by the existing serializer.

Release compilation, 53 snapshot/recovery regressions, the generator structure
check and 12-language localization checks pass. The buffer-based regressions do
not prove live native dependency equivalence or cold-load acceptance. The other
movable styles' catalogue and variant availability still depend on the original network; full source
independence and the remaining geometry/assembly work are not complete. This
increment does not deploy or change existing saved bridge assets.

The three currently routed movable generators (Draw01, Draw02 and PedestrianDraw02)
now supply their measured native aggregate name and icon URI directly. Their common
lifecycle resolves the shared native `AggregateNetPrefab` and applies the icon before
ownership transfer; composition no longer overwrites either field from the original
network donor. Native audit roots 118584 / 118590 / 118834, UIObject records 70051 /
70052 / 70066 and aggregate reference 120422 verify these exact values against audit
SHA-256 `47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070`.
The selected network's UI group and priority remain unchanged. Resource identifiers
are not localized text; the existing 12-language UI check passes.

This removes two runtime donor reads, not the entire prototype dependency. Catalogue
availability, variant selection, remaining donor-based unlock capture and auxiliary arrangements
still require coordinated reconstruction before original network Prefabs can be absent.
Those checks remain enabled; no availability or unlock bypass was introduced. The
architecture checker now recognizes the three movable subclasses and the existing
source-resolution helper without treating their routes as visual acceptance.

Lift01's two tower roof grids now share the joined-part plan with the 52 U frames. Outer
bars translate rigidly and central connecting strips widen between measured asymmetric
inner boundaries. The joined plan also covers both rounded end platforms, whose curved
ends translate rigidly outside X=+/-9.082763671875 m. Combined counts are 28,225 / 18,714
original vertices and 1,088 / 752 subdivided original faces for near/middle LODs. Area,
rigidity and winding checks pass. Both top housings now retain their distinct measured
near/middle bounds. Final assembly, far-LOD and width compatibility remain unfinished.

Lift01 tower bodies now also include twelve roof sheets and four sloped connecting panels,
all on fixed bone 0; with the top housings the table covers 24 complete bodies /
1,668 near and 1,360 middle vertices in total.
The connector table now includes 26 independent vertical truss webs: 152 complete parts /
13,316 near and 11,800 middle vertices. These webs use their own span; the separate joined
centre columns remain stationary. Narrowing checks use a noncollapsing sample per member;
narrow selected-road compatibility is still unresolved. No geometry cuts were introduced.

Lift01 now has measured own-span transforms for the two middle crossbars, two arched lower
crossmembers and two counterweight shells (1,024 / 890 near/middle vertices). Native bone
checks distinguish fixed bone 0 from counterweight bone 3; the runtime stage does not alter
skinning. Envelope, membership and four-width winding checks pass. Complete assembly and
far-LOD correspondence remain unfinished.

Lift01 now separates the twelve crossbars and two centre columns in joined tower groups
280/308 using original face ownership. Near/middle each contain 1,152 crossbar vertices
and 672 stationary column vertices. `Lift01TowerFrames` widens only the crossbars and
retains column width, without new triangles. Source envelopes and four-width winding checks
pass. Full structural assembly and far correspondence remain incomplete.

Lift01's detailed glass submesh now has a separate rigid-side plan: 40 quads / 160 vertices
at both near and middle LOD, with identical original ordering and fixed bone 0. The left and
right 80-vertex ranges translate without changing window shape, topology, skinning or material
slots. `Lift01Glass.py` verifies source correspondence and four-width edge invariants. Far LOD
has no glass submesh; complete main-mesh/far integration remains outstanding.

Lift01 now includes own-span transforms for 126 independent crossing connection plates and
bracket bodies (12,692 / 11,176 near/middle vertices). Offline checks establish exact cross-LOD
bounds, whole-face ownership, disjointness from other completed groups, Y/Z preservation and
winding across four width samples. `Lift01Connectors` preserves topology and original channels;
joined tower members and far-LOD treatment remain separate unfinished work.

Lift01's 52 joined U frames now have measured near/middle plans. Side uprights translate
rigidly and the connecting beams stretch between their original inner upright faces.
The offline recipes retain all original vertices, subdividing only 916 / 635 boundary-crossing
triangles. Area, winding, rigid-side and four-width checks pass; native geometry comparison
plots were inspected. `Lift01JoinedFrames` is not yet connected to a complete render assembler.
Remaining components and far-LOD correspondence still need implementation.

Lift01 now has immutable own-span transforms for 132 complete transverse members at
near/middle LOD (16,778 / 15,696 vertices): 50 lower web plates, 30 upper beams and
52 transverse tie plates/sheets. These exclude joined frames with side uprights,
road bases and rigid side groups. Whole-face membership, exact cross-LOD envelopes and
four width/winding checks pass. See the Lift01 implementation document for measured spans.
The tables await the complete render assembler; far geometry and joined frames remain open.

Lift01 road-adjacent base shells now use the required `x + sign(x) * delta` mapping.
The two fixed shells and the single moving shell are reviewed crossing groups 3, 13 and 487.
Six adjacent end lips (30, 31, 32, 33, 493, 640) use the same base mapping; all nine parts
have identical near/middle bounds. `Lift01Bases.py` emits 4,240 / 4,026 vertex memberships;
recorded centre vertices stay fixed, and each side translates without Y/Z changes. Checks at
delta -0.5, 0, 2 and 8 m preserve winding and nondegenerate source faces. The nearest noncentre
base vertex is 2 m from the centre, so delta <= -2 m remains an explicit unsupported narrowing
case rather than collapsing the base. These maps do not scale piers or substitute their footing
for the road-adjacent base. Complete structural coverage and far-shell adaptation remain open.

Lift01's longitudinal native rails and fasteners now have a separate whole-face removal table.
Offline authored side-group ranges identify six rails (two moving and four fixed) and 1,920
fasteners. Every fastener maps uniquely to a reviewed sleeper's longitudinal footprint, with
exactly eight per sleeper. The near/middle tables remove 41,536 / 32,204 faces; no selected
face shares vertices with retained groups or overlaps the horizontal road-top mask. Side-group
records are reproducibly emitted by `Lift01RigidParts.py` and consumed by `Lift01Rails.py`.
This prepares replacement by selected-network track visuals; full structural assembly and
far-shell handling remain unfinished.

Lift01's reviewed native track sleepers are now separate from structural crossing members.
`Lift01RigidParts.py` retains the offline crossing-group records; `Lift01Sleepers.py` identifies
the 240 detailed trapezoidal sleepers and verifies identical middle-LOD envelopes. Its emitted
`Lift01SleeperRemoval` masks remove 6,240 near / 4,320 middle triangles without cuts, shared
structural vertices or overlap with the road-top masks. This is replacement of original track
decoration by selected-network visuals, not removal of the road-bearing shell. Longitudinal
rails, far-shell closure and full render integration remain outstanding.

Lift01 main-mesh side-part tables now cover 396,744 near and 291,054 middle vertices.
Offline topology plus coincident-position grouping identifies 9,926 wholly side-contained
groups at each level. Each middle group retains its nearest detailed envelope, side and
bone-group signature in authored order; the largest native simplified-bound difference is
0.05078125 m. Mixed-bone face groups are retained as signatures, not assigned a replacement
bone. `Lift01RigidParts.py` emits immutable vertex ranges, and `Lift01RigidParts.g.cs` applies
only the recorded +/-half-width displacement. Four delta checks (-0.5, 0, 2, 8 m) preserve
Y/Z, rigid displacement and untouched crossing vertices. This is partial structural coverage:
crossing members, bases, glass and far merged structure still require separate measured plans.

Lift01 now has callable network and object assembly stages. Its network assembly accepts explicit
selected-occurrence roles, adopts the converted graph, retains selected outside connections and
installs only its recorded structure placements. Object assembly retains the two audited fixed
control-room wall lights, translates their measured sides and binds selected-road objects/effects
and skinned paint after the final mechanism exists. Its caller must still supply the measured
physical partition and all widened render graphs; no incomplete Lift01 generator route is claimed.

This section supersedes the historical status statements below. Draw01, Draw02 and PedestrianDraw02 now have
generator routes. Draw01's latest appearance repair was accepted by the user; that acceptance
does not cover Draw02 or the remaining eight mechanisms. Draw02 has an integrated generator
with unresolved fixed-platform closure and narrowing limitations, not completed support.
The catalogue labels already cover all twelve supported UI languages.

PedestrianDraw02 now connects recorded source renders, selected-network sampling, two mechanism
LODs, three column LODs, two footing LODs, original-skeleton paint and root-owned native effects.
Its generic built-in pedestrian pillar placeholder retains the native reverse replacement
references. The recorded pathway cross-section is 8 m, shared by generation and catalogue
measurement. Native approach sections are retained. The route is integrated but not accepted:
narrowing limits, sidewalk/structural compatibility, furniture placement at measured gaps, full
resource-graph size and native near/far/motion/cold-load behavior remain open batch checks.

Single-network sampling adapters now also exist for Draw03, Lift01/02/04/05 and
PedestrianDraw01/02. `MovableSamplingAdapters.py` reads the retained, SHA-pinned
native audit and emits their source render identities, approach stations and water
elevation. It factors each recorded mechanism placement out of the corresponding
network assembly data, verifying every original numeric/boolean field before the
change. Sampling and final placement therefore use the same method. Lift01's
77.87999725341797 m, Lift02's 70.5999984741211 m and Lift04/05's 68.75 m mechanism
Y offsets remain distinct from the 10 m water elevation; the shared sampler computes
the road-to-object frame through the native staging context.

The caller explicitly supplies replaced structural layers and the selected upgrade.
The adapters do not assume that every track or pathway replaces the same layers.
They use the existing disposable sampler and its per-LOD compiler; they do not create
city construction requests or publish standalone sampled geometry. These are callable
input stages, not complete generator routes. Lift03 remains a coordinated dual-network
task and is deliberately absent from this single-network emitter. The older
PedestrianDraw02 closed-road helper remains untouched until its consumers are migrated.

This increment was compiled against the installed game. Native sampling execution,
motion, appearance and independent cold loading remain unverified and will be checked
with the complete batch. No new user-visible strings or localization keys were introduced.

The accepted working-tree baseline was preserved locally in `a342394`; no remote push was made.
Shared private sampling was added on `dev` in `62d59e3`. `MovableSamplingMechanism` consumes
explicit source identities and style-owned mesh/component constructors. `MovableStraightSample`
retains native geometry/lane calculation, source occurrences, object-space transforms, per-LOD
compilation and disposal. It contains no style switch, inferred road masks or animation controller.
It is a single-network sampling primitive, not a dual-deck Lift03 implementation.

Draw02's branch integration `5fd162c` (`dev` integration `6dfa943`) adds
`Draw02RoadSampling` with its measured 144 m approach stations, 10 m water elevation and
original mechanism placement. The placement is shared with `Draw02NetworkData.Placements`
without changing its serialized values. `Draw02BridgeGenerator` now calls this adapter and
assembles its own measured mechanism, supports, native skinned paint, ordinary pillar, fixed
approaches and barrier/network rules through the shared preview/persistence path. Sampling
and assembly share a disposable reconstructed source graph. Compilation and routing alone
do not establish new playable bridge support.

Outstanding geometry prerequisites remain substantive:

- Draw02: finish fixed-platform surface/curb side closure and selected sidewalk geometry;
  resolve the recorded central-linkage narrowing limit and verify the integrated native path.
  Its measured structure maps, fixed approaches, supports and appearance/motion regions are
  now connected; see [the current stage record](Draw02-Network-Implementation.md).
- Draw03: complete original-part widening maps, replacement surface/curb membership,
  far-LOD shell closure, fixed approaches and source-specific appearance/motion regions.
- Lift01/02/04/05: complete rail/deck versus structure membership at every LOD, deck/weight/pulley
  ownership, end closure and selected-track attachment conversion.
- Lift03: a coordinated main/auxiliary sampling stage now exists in `Lift03RoadSampling`;
  complete its dual-deck assembly, surface/paint conversion and native execution checks.
  The two decks share one context and one mechanism; they are not unrelated samples.
  `PrepareDeck` now connects either deck to the common road/material preparation pipeline,
  selecting its own evaluated road, road-to-object pose, occurrence layout and LOD compiler.
  The common pipeline accepts these inputs explicitly; the existing single-network entry
  delegates without changing its processing. Style-owned appearance patches, partitions
  and complete render assembly still need implementation before this becomes a generator.
- PedestrianDraw01/02: complete mechanism/column widening and all-LOD road boundaries;
  PedestrianDraw02's existing top-face and footing data do not supply those missing plans.
  PedestrianDraw02 now has emitted detailed-column displacement tables for LOD0/LOD1:
  88 corresponding parts per level (8 central plates, 16 diagonal braces, 8 horizontal
  beams, 8 columns and 48 side brackets). All 3332/3076 vertices are assigned once;
  side columns/brackets translate rigidly and crossing members stretch by their own span.
  The emitter checks identical part roles/end/bounds between levels and four width deltas
  (-0.5, 0, 2 and 8 m), preserving Y/Z and side-member widths. The 121-vertex far shell
  now has a separate offline subdivision at the detailed columns' inner X boundaries
  (-4.058837890625 and 4.0587158203125 m). Its two side envelopes translate; the merged
  central envelope stretches between those boundaries. Fifty original faces are replaced
  using 100 measured edge vertices, producing 192 triangles instead of 92. Original
  surfaces and native vertex attributes remain in the structural plan; interpolation
  supplies the new vertices. Source-area conservation, winding and rigid side displacement
  pass at the same four widths. Near/far offline projections at zero and +8 m were reviewed.
  `PedestrianDraw02RenderAssembly.CreateColumn` now builds the complete three-LOD support
  graph through the native persistence factory, preserving authored Y/Z stacking bounds.
  The complete PedestrianDraw02 generator and its mechanism widening are still outstanding.
  Its road motion partition is now emitted separately from the structural maps:
  `PedestrianDraw02RoadPartitions` uses 14 measured transverse slabs and linear Z edges
  to retain the interlocking hinge notches, fixed end platforms and the small centre gap.
  Both native LODs have identical projected loops. All 552 road-face centroids match
  their recorded bone ownership, and 8,000 independent point-in-polygon comparisons
  pass at half-width deltas -0.5, 0, 2 and 8 m. The partition clips outside the measured
  road footprint, retains explicit gaps, and moves side stations without moving the
  recorded centre stations. It does not classify prototype geometry at runtime.
  `PedestrianDraw02RoadPreparation` now couples the physical partition to selected-road
  conversion and the style's appearance patches. Native fixed faces 6708/6709 and moving
  faces 8556/8557 define separate interior asphalt repeat rectangles. Their measured U/V
  gradients remain independent of generated bridge width. Four directional patch leaves
  distinguish positive/negative fixed platforms and moving leaves; the appearance tree
  uses sampled-road coordinates while physical binding uses object coordinates. The
  source rectangles and their complete coverage are checked by
  `PedestrianDraw02RoadAppearanceCheck.py`. Structural assembly and the generator route
  are still missing; compilation does not prove native sampling, shading or cold loading.
  `PedestrianDraw02RigidParts` now records main-mesh side-part vertex ranges for both
  levels: 534 parts / 25,488 vertices in LOD0 and 550 parts / 19,155 vertices in LOD1.
  The counts differ because the native simplified mesh merges and splits some rail
  segments. Every LOD1 side vertex is covered by same-side LOD0 part bounds without
  tolerance expansion. Four width checks preserve each side part's rigid displacement,
  Y/Z and all unassigned crossing vertices. This is a partial structural map; crossing
  members, road-adjacent bases and joined shells still need separate measured plans.
  `PedestrianDraw02TransverseParts` adds 156 corresponding transverse members per LOD,
  including the two reviewed hinge covers (11,352 / 9,767 vertices). Near/middle membership is paired offline; X extrema match
  exactly and the maximum other bound difference is 0.009521484375 m. Each member
  expands by twice the half-width delta using its own recorded span, preserving Y/Z.
  Four width checks pass. `PedestrianDraw02Bases` separately records 24 road bodies
  and road-adjacent base groups (2,772 / 2,659 vertices), with explicit left/centre/right
  membership. Centre residues remain stationary and side vertices translate; no base
  width scale is used. Checks pass at half-width deltas -0.01, 0, 2 and 8 m.
  The closest non-centre station is 0.03180122375488281 m from zero; this current
  displacement cannot safely narrow beyond that station and explicitly rejects such
  input. Broader narrowing support remains unresolved, not verified by the width tests.
  `PedestrianDraw02JoinedSupports` now handles the four joined groups: two end supports
  and two hinge cradles. Offline source projections identify the cap's inner column
  bevel at X=+/-3.63031005859375 m and the cradle's inner arm flange at
  X=+/-2.03131103515625 m. Side envelopes translate while central spans stretch;
  each LOD retains 556 original vertices and introduces 172 measured edge vertices
  across 92 source faces. Original surface area, winding and Y/Z preservation pass.
  `PedestrianDraw02RenderAssembly.CreateMechanism` combines all four maps, removes
  only the recorded 276 road-top faces per LOD, and preserves the added-vertex X values.
  Coverage checks against the emitted C# tables assign all 40,168 / 32,137 main vertices
  exactly once; no support fragment intersects a removed road face. The separate glass
  mesh preserves its six quads and native LOD-specific ordering (right-side vertex
  ranges 12..23 in LOD0 and 3..14 in LOD1). Both material slots and native channels
  flow through the existing render graph factory. The complete generator route,
  ordinary approaches/supports and native execution remain outstanding.

Native Draw02 LOD2's broad top is at Y=10.52832, while its near road datum is Y=9.800049.
The existing native UV and cross-section inspection shows an opaque shell with retained side/end
neighbors. Top-face removal alone is not a complete replacement. Do not use the candidate face
lists as a complete semantic mask, stretch entire bone groups, or register placeholder generators.

Validation of this increment: Release compilation succeeded. No game construction or visual
unit test ran. Draw01, fixed bridges, saved assets and self-check were not changed or removed.
Human motion, near/far, shipping and independent cold-load acceptance for new styles is pending.

## 1. Status and scope

Updated: 2026-10-16. The requested scope is all ten audited Bridges & Ports movable mechanisms,
using the selected-road surface conversion design. Implementation is not complete. Shared runtime
conversion primitives and a native moving-road asset construction path have been implemented, built and deployed. They are not connected to an
admitted movable generator: all ten complete generators remain outstanding. Existing fixed bridges
have not been removed or rewritten. Updating documentation accompanies implementation; it is not a
replacement for implementing the requested feature.

The shared offline geometry reader also supports a native rigid-skin inspection command. It has
decoded 88 real geometry files covering the ten mechanisms' object meshes and LODs. This is
implemented metaprogramming support, not a runtime movable-bridge generator.

The [project contract](agent-contract/PROJECT_CONTRACT.md) and
[bridge generation contract](agent-contract/BRIDGE_GENERATION_CONTRACT.md) and
[movable bridge contract](agent-contract/MOVABLE_BRIDGES_CONTRACT.md) remain normative.
This record describes progress, not exceptions to either contract.

## 2. Independent mechanisms

These IDs are registered runtime catalogue entries and retain the branch assignments in project
contract section 12. Names are provided in all twelve supported languages. Installed, available
native sources supply their cards; Lift03 appears in double-deck mode and the other nine in
single-deck mode. Creation and preview use the ordinary controls without completion-based gates
or unfinished-style notices. Requests reach the shared generator router, which reports an ERROR
identifying the style when its generator route is absent. Preview diagnostics now retain the
underlying error as well as the ordinary preview failure status. Catalogue admission does not
constitute implementation of a generator.
All ten variants have audited native metadata; all ten still need implemented conversion and
human acceptance. A mechanism category does not supply another variant's mapping rules.

| Reserved style ID | Native root | Native network kind | Required branch | Generator status |
| --- | --- | --- | --- | --- |
| `Draw01` | `DrawBridge01` | Road | `bridge/draw-01` | Not implemented |
| `Draw02` | `DrawBridge02` | Road | `bridge/draw-02` | Not implemented |
| `Draw03` | `DrawBridge03` | Road | `bridge/draw-03` | Not implemented |
| `Lift01` | `LiftBridge01` | Train track | `bridge/lift-01` | Not implemented |
| `Lift02` | `LiftBridge02` | Train track | `bridge/lift-02` | Not implemented |
| `Lift03` | `LiftBridge03` | Road with auxiliary train track | `bridge/lift-03` | Not implemented |
| `Lift04` | `LiftBridge04` | Subway track | `bridge/lift-04` | Not implemented |
| `Lift05` | `LiftBridge05` | Tram track | `bridge/lift-05` | Not implemented |
| `PedestrianDraw01` | `PedestrianDrawBridge01` | Pathway | `bridge/pedestrian-draw-01` | Not implemented |
| `PedestrianDraw02` | `PedestrianDrawBridge02` | Pathway | `bridge/pedestrian-draw-02` | Not implemented |

The audit contains eleven network roots because `LiftBridge03 Train Track` is an auxiliary root.
There are ten actual native `MoveableBridge` owners. Preserve the auxiliary arrangement as part
of `Lift03`; do not offer the auxiliary network as an independent bridge mechanism.
Historical groups `Draw`, `Lift` and `PedestrianDraw` are not interchangeable aliases for these IDs.

## 3. Backup completed before implementation

The requested backup is named `26.10.16桥梁备份` and is stored under the current task's `outputs`
directory, outside the game's active asset directories:

```text
C:\Users\admin\Documents\Codex\2026-10-15\c-users-admin-documents-codex-cs2bridgebuilder\outputs\26.10.16桥梁备份
```

The game was forcibly closed using the required command and its process was verified absent
before copying. The snapshot contains:

| Payload | Verified files | Scope |
| --- | ---: | --- |
| Bridge assets and supporting data | 243 | Complete `ImportedData`, `BridgeBuilder`, `ModsData/BridgeBuilder` and `ModsSettings/BridgeBuilder` trees |
| Installed mod | 18 | Package `.cache/Mods/pdx_mods/160320_17`, identified by the last `Modding.log` as the loaded Bridge Builder installation |

Every copied file was checked against its source using SHA-256. The added backup README was removed
on 2026-10-16: backups contain original source files only, without added documentation or metadata.
Empty directories in the bridge/data trees were also retained.
This was a copy, not a cleanup operation. The backup operation removed or replaced no source
assets or installed files. The later tool-change deployment is recorded below. The complete
`ImportedData` snapshot also retains shared files; backup inclusion does
not establish Bridge Builder ownership. City saves are not included.

Restore only while the game is closed. Review source and destination paths before
restoring; do not merge a historical export-state snapshot into a newer collection indiscriminately.
There is no automatic restore command or scheduled restore operation.

## 4. Implementation findings

### 4.1 Composition and network semantics

`BridgePreviewComposition` uses native composition selection in an isolated world. Its optional
`trackOccurrences` mode now registers separate section and piece entities for each source occurrence
and returns the complete source array path with each resolved piece. Repeated prefab references
therefore remain distinguishable. Existing preview callers retain the default cached path.
The path identifies the source condition chain; a persisted conversion manifest and full effective
predicate records still need integration.

`MovableSectionConversion.TryConvert` builds private wrapper sections per source occurrence and
measured branch. Source and mechanism requirement groups remain at separate native selection levels,
preserving conjunction between independent `RequireAny` groups. It preserves the selected section,
lanes, direction, median and offset; combines hidden layers; and applies only the explicitly supplied
half-length override. It supplies source/branch occurrence records and returns owned wrappers to the
caller. It does not choose a universal branch table or publish assets. Per-mechanism callers are
still required, including preflight of combined conditions and branch coverage.

`BridgePreviewNetMesh` invokes native composition mesh generation. Its optional surface output now
retains the original `SurfaceAsset` slot mapping through native material merging; preview material
copies are never returned as persistent surfaces. Existing callers do not request this output.

`MovableClosedRoad.TryCompile` composes the selected road with occurrence tracking and generates each
visible occurrence independently while retaining the complete composition width and offsets.
`MovableStraightRoadEvaluation.TryEvaluate` resolves the straight edge's packed positions and UVs
into explicit geometry, retaining original surface references and triangle provenance. It follows
the native vertex program's matrix selectors and its nonnegative-rate/negative-fraction V rule.
It refuses node, divider and terrain-driven selectors and unmeasured material shader paths.
Compilation requires ordinary elevated edge flags, before moving-span hiding conditions.
The returned occurrence records clear temporary ECS entity IDs before the private world is disposed.
This is currently the highest-detail base-surface stage. The compiler now also extracts actual
primary and secondary lane visual batches through the native lane jobs. It retains source render
Prefabs, original surface slots, centred curves, native tiling, curve matrices and rejected-submesh
conditions. Decal volumes remain distinct from curved meshes. These batches are inputs to conversion;
they are not projected marking triangles. LOD selection and mounted objects remain outstanding.
Its result alone cannot be published as a complete movable road.

Source-road closed-surface evaluation precedes moving-node visual suppression. Network conditions
must preserve conjunction across separate `RequireAny` groups, native substitution precedence,
lane functionality and occurrence-specific flags. The six-lane mapping remains an example only.

### 4.2 Structure and replacement road surface

The audit headers for all ten highest-detail primary render prefabs show a principal opaque mesh
with one submesh and a separate window mesh. They do not identify road triangles. Material-slot
deletion therefore cannot establish a correct separation of the prototype road from its structure.
Review and record exact road/structure membership and fixed/moving interfaces offline, with matching
assignments for every LOD. Preserve the original structure's raw non-position channels.

Generated road triangles may be clipped at recorded seam frames and acquire the required object
attributes and skinning channels. This scoped road construction does not permit runtime inference
of the prototype's structural membership or replacement of its complete opaque mesh.

### 4.3 Native skeleton persistence

The inspected primary render meshes use `BlendIndices:UInt32x1`; the main opaque layouts do not
declare a conventional four-weight blend channel. Preserve the game's actual representation and
verify its decoding instead of assuming ordinary Unity four-weight skinning.

`ProceduralAnimationProperties.BoneInfo` contains rest position, rotation, scale, bind pose,
parent index, motion type, speed, acceleration, connection ID and source ID. Geometry skinning
channels and this separate native component must be handled together.

The existing exporter constructs models with an empty `ModelImporter.Model.BoneInfo` array.
That fact alone does **not** prove that native bones are absent from a persisted render prefab:
the native procedural component is a separate part of the asset graph. Check the entire export
and load path before changing the exporter. No exporter defect has been established solely from
this empty array, and no such fix has been made.

The new offline `--skin-report` command reads the actual `UInt32x1` channel, reports per-bone
vertex bounds and triangle membership, and writes an OBJ inspection mesh grouped by raw bone
index. Existing geometry inspection commands do not opt into the new skin-layout restriction.
The decoder now follows native file flags (`Meshopt = 1`, `ZSTD = 2`) and derives index element
width from the mesh flag instead of the vertex count. These are file-format corrections; no
existing runtime geometry coefficients or generated bridge files were regenerated.

All 88 extracted object/LOD geometry files matched the earlier audit's source hashes and decoded
successfully. This includes both skinned primary geometry and unskinned support geometry. The
tool project builds with zero warnings and zero errors. No visual-generation unit tests were run.

Actual main-mesh examples: `DrawBridge03` has zero mixed-bone triangles; `LiftBridge01` has 576,
`LiftBridge03` has 384, and `PedestrianDrawBridge02` has 96. Preserve these authored structural
triangles and investigate their role. The rule prohibiting a newly baked rigid road triangle from
crossing independently moving regions does not authorize deleting native cross-bone structure.
Neither a shared bone index nor a welded island alone identifies a complete widening part.

### 4.4 Materials, markings and mounted objects

Native mechanism surface headers reference object material templates, while source road pieces
can use network templates and network-dependent inputs. Matching textures or a template number
does not prove that the source appearance survives object skinning. Establish the exact material,
UV, packed-normal/tangent and deformation conversion for each supported source path.
Follow the normative [asset persistence policy](Bridge-Asset-Persistence.md): reuse compatible
original surfaces and textures; snapshot unchanged external dependencies. Only a demonstrated
resource change requires a new private material/surface asset. Moving marking geometry or native
placement does not automatically require new marking textures or materials.

Lane-generated markings require their own persistent moving representation; they are not
necessarily triangles in the road surface mesh. Preserve selected-road attachments and native
mechanism lights/barriers, including their different visibility and parenting rules. Unsupported
features must fail explicitly before output publication.

The installed `AssetDatabaseResources` material library has now been decoded directly. Its 40
entries identify template 17/33 as `NetCompositionMeshLit`/`NetCompositionMeshLitCutout`, using
`BH/NetCompositionMeshLitShader`; template 0/1 uses `BH/SG_DefaultShader`. The network shader's
declared keyword list has no `_GPU_ANIMATION_PROCEDURAL`, while the default object's list does.
The network shader also declares `_WorldspaceAlbedo`, `_WorldspaceNormalMap`, `_WorldspaceMaskMap`
and `_WorldspaceUVScale`, absent from the default object shader. Color-mask property names differ.
These are measured incompatibilities, not evidence that swapping templates preserves appearance.
World-space texture baking and lane/decal conversion remain implementation work.
The straight-edge UV evaluation has since been implemented from disassembled native vertex program
106. Material appearance remains separate: native fragment program 128 blends the sampled base and
world-space textures using the source mask's blue channel, including tangent-frame treatment of the
world-space normal. Dropping these inputs would change the selected road's appearance.

### 4.5 Implemented road construction primitives

`MovableRoadGeometry.TryBind` takes already evaluated object-space road triangles and an externally
supplied measured binary seam tree. It clips against each seam-local zero plane, interpolates road
attributes and preserves material, occurrence and source-triangle indices. Every output triangle
receives one native bone array index. Coplanar triangles are assigned once; shared seam vertices are
duplicated across bones. Invalid frames, attributes and non-finite results return explicit failures.
This does not infer mechanism membership or perform network shader deformation.

`MovableRoadModel.TryBuild` encodes these newly constructed triangles into a native importer model:
Float32 positions, packed SNorm16x2 normals, packed Float32x1 tangents, Float16x4 UVs, UNorm8x4 colors
and UInt32x1 rigid bone indices. Material slots remain separate submeshes with explicit bounds.
Out-of-range material/bone indices, colors and half-float UV overflow fail before publication.
The caller owns the returned disposable model. Native skeleton components, surface assets, LODs,
render bounds and persistent geometry are assembled by the native asset construction path below; a complete generator must still supply its verified inputs and attach the result to its mechanism.

These primitives reside in `src/BridgeBuilder/Bridges/Common/Movable` on `dev`. No bridge-specific
branch or generation route has been implemented by this shared-infrastructure change. The primitives
are compiled but have no admitted generator callers; they must not be described as usable bridges.

### 4.6 Native moving-road asset construction

`MovableRoadAssets.TryCreate` now joins measured seam binding and rigid model encoding to the
native `GeometryAsset` creation path. It preflights the complete source LOD graph, procedural
skeletons, resolved object surfaces, owner identity, geometry-name collisions and supplied motion
bounds before writing geometry. Missing LODs, cycles and unrelated extra levels fail explicitly.
Each generated render Prefab carries native components, including the procedural bone array, and
its LOD references point to the corresponding generated Prefabs. Prepared road and structural
surfaces remain references. Optional evaluated lane meshes now share the binding/model path and
convert only their curved material templates, retaining original texture/VT references.

The compatibility guard accepts the measured `SG_DefaultShader` template family (0, 1, 13, 14,
35 and 36). This checks the object representation only, not appearance equivalence. Network
templates 17/33 remain explicitly unsupported at this boundary until their actual visual conversion
is implemented; changing a template ID is not implemented as a substitute.

The caller can include already evaluated marking triangles in the same binding/model pipeline.
This does not extract lane markings, bake world-space material inputs or supply per-style road/structure
membership. Local render bounds contain the closed geometry; native object initialization and
stacking contribute the separate culling envelope. Closed containment alone does not establish
visibility throughout the movement cycle.

`TowerFactory.CreateMovableRenderGraph` adds every resulting render Prefab to `TowerFactory.Created`
(formerly named `CreateMovableRoad`, before adding fixed structural render graphs).
Consequently, a generator using this entry point joins the existing `TryBuildBridge` save plan:
`PrefabAssetWriter`, external dependency snapshots and native independence validation remain the
only Prefab persistence path. No separate registry, writer or runtime animation controller was added.
No style generator calls this entry point yet, and no native object has been attached to a generated
movable network by this change.

Preview construction uses `PreviewGeometry` and writes no permanent assets; converted preview
surfaces live in its disposable transient database. Failed construction
rolls back geometry and converted lane surfaces allocated by that call, reports rollback errors and destroys its private
Prefab objects. Native `GeometryAsset.CreateDataFromGeometry` copies importer buffers; the caller's
models are disposed for both preview and permanent construction, independently of asset unloading.
The source meshes, surfaces and external dependencies are not changed.

### 4.7 Measured structural road replacement

`MovableStructureModel.TryBuild` consumes explicit source positions, per-slot source indices,
per-triangle road-removal masks and positions produced by the measured widening plan. It verifies
source coordinates and index streams exactly before applying the plan. It rejects Y/Z changes and
non-finite coordinates, unsupported skin-index layouts and indices outside the supplied native bone
array. It does not discover road membership or widening parts. Empty material slots
remain in place, and normals, tangents, UVs, colors and bone indices are copied as raw attributes.
Retained cross-bone structural triangles are unaffected unless individually included in the supplied
road-removal mask. An incorrect classification cannot be made correct by these identity checks.

`MovableRoadAssets.Level` can now supply one structural plan for every native mesh in that LOD,
plus the prepared native procedural skeleton. All structural models and converted road geometry
are assembled into the same generated render Prefab. Original structural surfaces precede road
surfaces in unchanged slot order. Counts, surface area, preview capture, persistent geometry,
generated LOD references and failure cleanup use the complete model group. The supplied motion
bounds must contain both structure and road. A generator must provide the measured bone placements
and bind poses appropriate to its widened structure; the helper does not infer them.

The path is connected to native asset construction, but no per-mechanism road-removal plans or
generator callers have been admitted. The original road-only mode remains available for a separate
moving-road object. Its use does not remove the archetype road from a different structural object.

`MovableSurfacePixels.TryRead` reads the network shader's seven base/world/emission texture inputs
and material coefficients into a CPU snapshot. Ordinary textures use balanced asset leases;
virtual payloads use the native `VTSurfaceDataHandler.FillDebugMaterial` and `VTTextureAsset.Load`
reconstruction paths. A dummy texture reference without either payload fails explicitly. GPU
readback preserves linear samples, restores the previous render target and sRGB-write state, and
destroys private reconstructed textures. No surface, texture or material is saved by this operation.
Shader-declared white/bump defaults apply only to absent bindings, not unresolved explicit assets.
The measured network shader does not itself declare `ENABLE_VT`; reconstruction capability is not
evidence that assigning a VT-only material to that shader is supported.

`MovableSurfaceEvaluation` now supplies CPU bilinear sampling with native texel-centre addressing
and repeat, clamp, mirror and mirror-once wrapping. It evaluates the measured pre-weather blending
of base/world albedo, mask channels and tangent-space normal inputs; it also carries emission and
control-mask samples. `MovableSurfacePixels` now retains control-mask inputs, source color masks and
the snow coefficient. These computations create no material or texture assets and have no generator
caller yet. The evaluator deliberately preserves the blended normal's length rather than silently
normalizing it. It is not a complete appearance baker: native object material output,
decal compositing and weather-response equivalence remain unresolved. The subsequent atlas increment
below implements UV layout and CPU sampling without claiming those remaining steps.
In particular, the network shader blends tangent normals before weather processing; object normal
encoding must not silently normalize that intermediate vector and claim identical wetness behavior.
The new native data reconstruction support does not establish visual equivalence or generator support.

The combined increment passed the Release build and was deployed with all 12 installed payload
hashes verified. Both installation replacements took fresh, verified 18-file backups. Version
`26.10.13.0` and all existing bridge assets were retained. The game remained closed. See the task
output `Movable-construction-deployment-20261016.md` for final hashes and backup paths.

The Release build passed and all 12 installed payload files were verified against the build output for this increment. A fresh installation backup was verified before replacement. No existing bridges were removed. No visual-generation unit tests or automatic game
construction were run. The source/API inspection and compilation do not demonstrate an in-game
save/reload, successful animation or any of the ten generators' acceptance. See the accompanying
`Movable-native-assets-deployment-20261016.md` output for deployment evidence.

## 5. Remaining implementation work

### Selected-road appearance atlas construction (2026-10-16)

`MovableSurfaceAtlas.Bake`, exposed through `MovableClosedRoad.Result.BakeAppearance`, now creates
per-material texture pages from evaluated selected-road triangles. It places each non-degenerate
triangle in its own local chart, subdivides oversized triangles along their longest edge, retains
winding and source occurrence/triangle identifiers, and writes atlas UVs without altering the
retained normal/tangent frame. Page size, physical texel density and padding are explicit inputs;
they are appearance settings, not archetype geometry measurements.

Sampling uses the original interpolated road UVs and closed-world positions with
`MovableSurfaceEvaluation`; rewritten atlas UVs are used only by the output triangles. Padding uses
the nearest triangle edge. Each page retains source material coefficients, albedo, mask, emission,
control samples and the full unnormalised tangent normal. Pages are yielded individually so their
consumer can release each evaluated pixel buffer before requesting the next; source snapshots are
reused for pages of the same material occurrence. Native structure meshes do not enter this path.

This stage constructs CPU appearance; native serialization is described below. There is no implicit
normal-length discard and no frozen lighting or weather. The inspected network fragment program
blends its unnormalised base/world normal toward the wet normal before normalization. A conventional
object normal-map encoding cannot be described as equivalent merely because the dry normalized
direction matches. Exact output fidelity, mip filtering, decal compositing, actual visual
comparison and the complete generator callers remain required. No engine shader was changed.

The Release build and scoped diff checks passed. Deployment verified 12 installed payload files
against build output after verifying an 18-file original installation backup. No existing bridges
were removed and no visual-generation unit tests or game construction were run. The task output
`Movable-atlas-deployment-20261016.md` records paths and hashes. This confirms deployment of the CPU
atlas stage only, not a completed material or movable-bridge generator.

### Native road appearance output (2026-10-16)

`MovableRoadAssets` now sends network-template inputs through `MovableSurfaceAtlas` and
`MovableNetworkSurfaces`. Each page creates a private native `Surface` using `Default` or
`DefaultCutout`, five linear RGBAHalf texture assets, and explicit atlas UVs. Native texture
serialization retains the mip chain and uses lossless ZSTD payload compression. This is a
network-to-object representation conversion, not automatic copying of unchanged materials.
Structural materials remain their original references. The owning bridge's normal dependency
traversal covers the new surface-to-texture references.

Preview output uses the disposable transient database. Formal output uses the user database.
Every allocation enters the caller's rollback list before later operations can fail. Native
`TextureAsset.SetData(Texture)` acquires data and object leases; `Unload()` releases the raw buffer
and private Unity texture. Source textures are not destroyed. Appearance assets can be allocated
before all LOD geometry models are prepared; they are rolled back if the full graph fails.

The writer encodes unit normal directions. It records the maximum change from the raw blended
normal length, rather than claiming exact wetness equivalence. The current object snow coefficient
is 1; weather fidelity, mip/chart filtering and projected decal appearance still need resolution
and human verification. No engine shader, shader bytecode or game assembly was changed.

The Release build passed and all 12 installed payload files matched their build hashes after a
verified backup of all 18 original installation files. Version remains `26.10.13.0`. No bridge
assets were removed: there are still no style-generator callers of this shared movable builder,
and fixed bridge generation is unchanged. See `Movable-native-surfaces-deployment-20261016.md`
in the task outputs. This deploys the native writer, not ten complete bridge generators.

### Current source investigation (2026-10-16)

Draw01-specific implementation has begun on its reserved `bridge/draw-01` branch. The new
`Draw01MechanismData.g.cs` records the actual game 1.6.2f1 main object's six component roles,
three mesh placements, twelve authored child placements and the three complete native LOD
skeletons. Full extracted floating-point values and the source-record SHA-256 are retained.
The data's `Create` method calls `TowerFactory.CreateMovableObject` with caller-prepared render
graphs and attachments. Its component callback preserves native node offset, lift offsets,
movement time, pillar and standing-object parameters, and DLC prerequisite. It does not locate
or clone the native mechanism prefab at runtime.

These are reference-pose measurements, not completed width/structure transformations. The
generator must still supply the measured widened structure, selected-road moving mesh/markings,
correct ordinary road attachments and complete network composition. The authored attachment
factory records all twelve entries for comparison; it does not authorize using prototype
streetlights instead of the selected road's lighting. Draw01's actual router remains unconnected.
The other nine complete generators remain required. Release build and all twelve installed
payload hashes passed after an eighteen-original-file backup; no bridges were removed.
See `Draw01-mechanism-deployment-20261016.md` in task outputs. No human acceptance is claimed.

Offline inspection of the actual Draw01 main mesh now covers top, side and end projections at
all three LODs, with explicit face-ordinal candidates retained in the research record. The central
upward road plane is at extracted Y=9.800049 in LOD0/LOD1, while LOD2 replaces much of the top
with planes at Y=10.021484 and Y=10.047119. The coarse underside also has different topology.
LOD0's Y=10 candidates extend to X=10.600098, beyond the ordinary side surface, demonstrating
that height alone includes unrelated fixed structure. These measurements must not become
runtime classification rules or automatic removal masks. Curb faces, UV/material identity and
authored-part correspondence still require explicit classification. The report and native
geometry projections are in `Draw01-road-structure-inspection.md` in task outputs. No production
geometry or installed payload changed during this inspection.

The same inspection now includes native UV0 decoded through the installed ZSTD/meshopt routines:
29,840/18,772/1,118 vertex records at LOD0/1/2 respectively, matched to their actual OBJ index
streams. LOD0/1 share one VT surface; LOD2 has a different VT surface and rearranged UV islands.
Its road classification therefore cannot reuse near-view UV regions. Per-vertex UVs and exact
face correspondence are retained in the research NPZ records; the output report includes the
UV plots and source surface CIDs. Texture-content identification and reviewed replacement masks
remain unfinished. This extraction changes no runtime shader, geometry or generated bridge.

Native texture reconstruction now supplies the actual main 4096x4096 albedo and LOD2 512x512
albedo for this review. The main albedo is an external VTTexture while LOD2 embeds its layer in
VTSurface; decoding follows the native offsets, marked ZSTD payloads, BC7 blocks and tile borders.
The output UV figure overlays candidates on those textures, with source payload hashes in the
inspection report. This resolves access to the source imagery, but does not approve a removal
mask: the coarse atlas includes structural/cutout imagery that must remain correctly associated
with retained faces. No runtime assets were modified by this offline inspection.

An additional native attachment audit traversed all ten owners and 140 authored subobject
placements. `SubObjectSystem` passes `m_ParentMesh % 1000` to `RenderingUtils.FindBoneIndex`;
this field can encode a procedural connection ID, not a local bone-array index. Draw01 maps
100/101 to local bones 1/2, whereas Draw02 and Draw03 map them to 2/1. A universal numeric
bone-index substitution would bind objects to the wrong leaf. Native `RelativeBoneSystem`
resolves the retained connection after loading without a manager callback.

This establishes a native moving-child attachment path worth evaluating for decals. It does
not yet prove that curved lane batches can be serialized as child decal objects. Native
`DefaultDecal` uses template 18/shader 587, whereas curved templates 20/32 use shader 572.
Curve coordinates, partition boundaries, placement flags, layer filtering, render priority
and native material compatibility must be resolved before choosing this representation.
Pre-weather texture compositing must not be claimed equivalent to the native decal pass.
The selected-road moving geometry route remains unchanged. Detailed source hashes and all
authored parent-field groups are in `Movable-native-attachment-binding-audit.md` in task outputs.

`TowerFactory.CreateMovableObject` now assembles a mechanism from prepared render entries,
the style's recorded component callback and supplied child placements. It copies mesh placement,
rotation and required state, retains the object's circular flag, and preserves each child's
object reference, position, rotation, parent field, group and probability. It does not substitute
the fixed suspension-tower template or discover a live archetype. The object joins the existing
`Created` list only after successful assembly; failure destroys its private components/object and
rethrows. Unresolved procedural connection IDs fail explicitly using the native lookup semantics.

`RelativeObjectSystem.GetRelativeTransform` confirms that native `BoneHistory` matrices transform
attached child positions and orientations, including transformed submesh placement. The mod does
not need a frame-update callback for such a native attachment. Concrete decal conversion and the
ten complete style callers are still required; this API alone does not generate an entire bridge.
Release build and installation verification passed for 12 payload files after an original-only
18-file package backup. No bridge removal was needed because no style currently calls this entry.
See `Movable-object-deployment-20261016.md` in task outputs. Human acceptance remains outstanding.

The installed shader export now includes 2,592 DXBC programs from `BH/SG_CurvedShader` (resource
570), in addition to the earlier object, network and decal exports. Disassembled vertex programs
4, 7 and 10 establish distinct default, tiling and dilated geometry paths. Curved lane geometry
cannot be converted by applying its object transform alone. Tiling uses its own interpolation,
UV and out-of-span collapse behavior. `MovableLaneDeformation` now implements these three closed-pose
paths. `MovableLaneGeometry` reads native raw attributes, evaluates non-decal lane meshes and resolves
their projected heights against the selected road's receiver triangles. `MovableClosedRoad.Result`
calls that compiler, retaining original surface slots and separate decal batches. Hanging/swaying
attachments still require their own persistent native representation; they are not silently frozen.

`MovableDecalProjection` implements curved receiver coordinates, native endpoint blending and volume
fade. Reinspection of native fragment `572/24` corrected two differences: horizontally coincident
endpoints now use the native local projection instead of being rejected, and height/endpoint fades
remain independent. `Sample.ChannelOpacity` follows the native per-channel operation
`1 - pow(clamp(1 - materialOpacity * heightFade, (1 - endFade) * 0.001, 1), endFade)`.
The albedo, normal and mask channels have separate material opacities; a single multiplied fade
cannot replace them. Camera-distance fade is excluded from persistent texture data.
The Release build passed and 12 installed files matched the build after a verified 18-file backup;
no existing bridges were removed. See `Movable-decal-projection-deployment-20261016.md` in task outputs.
This helper is not yet connected to texture evaluation or compositing. `MovableSurfaceAssets.ConvertCurved`
converts the measured 16/31/34 curved templates to 0/1/35 after geometry deformation, retaining texture
and VT payload references and source defaults that differ between templates. This private surface is
a real template conversion, not an unchanged-resource copy. Network world-space blending uses
the separate appearance output described above. Preview surfaces use a disposable native transient database.

`MovableRoadAssets.Level` now accepts the evaluated lane result for that LOD. The asset builder binds
lane triangles through the same measured seam partition and native bone array as the road, checks
their closed-pose render bounds, and emits them as a separate model with explicit material slots.
It prepares every LOD model before allocating converted curved-lane surfaces. A source curved surface is
converted once within this render graph and reused by its LODs; no other bridge shares the new asset.
Structural surface slots and prepared road surface slots retain their original order and identities.
`ConvertCurved` now has an asset-builder caller. All models participate in geometry counts, surface
area, preview capture and persistent native Geometry output. Failed construction removes only
resources allocated by the call, including converted lane surfaces in their owning database.

Decal batches are not lane meshes and cannot be dropped from this input: a result still containing
unprojected decals explicitly fails before publication. Complete road appearance preparation must
project those batches before using this construction path. The API requires a separately prepared
lane result for each LOD; it does not silently reuse the highest-detail lane geometry at every
distance. Full style-generator callers, decal compositing, appearance fidelity and human acceptance remain outstanding.

This lane integration passed the Release build and scoped diff checks. All 12 installed payload
hashes matched the build after a verified 18-file original installation backup. No existing bridges
were removed: there are still no complete movable-generator callers, and fixed generation did not
change. See `Movable-lane-integration-deployment-20261016.md` in the task outputs. No visual-generation
unit tests or automatic game construction were run.

### Binary surface persistence integration (2026-10-16)

`BridgeSurfaceReferences` now reads `.Surface` and `.Material` binary dependencies. The existing
`BridgeDependencyPersistence` calls it for both external and already-owned surfaces, and both the
snapshot traversal and `BridgeNativePresentation.Validate` consume the parsed references. This is
connected to the current bridge save path, rather than an unused movable-only helper. Source bytes
and CIDs remain unchanged; built-in payloads stop traversal and are not copied.

Read-only extraction of 209 actual audited native surfaces (114 ordinary, 95 VT) supplied fixtures.
The parser matched the independent extraction of 571 required reference edges. Every edge resolves
in the installed content packages. Native VT bypasses 248 original texture identifiers that have no
standalone payload; the graph follows their actual VTSurface/VTTexture representation instead. The
nonvisual checks also covered original-byte copies, owned-surface traversal, missing VT failure,
Hash128 byte order, nulls, malformed data, source isolation and native locale registration.

This persistence update does not change bridge geometry or invalidate existing styles. The earlier
lane helpers still have no complete style-generator caller. No game construction, visual-generation
unit tests or native traffic/animation acceptance were performed. The ten complete generators remain
required work; these checks establish file-graph behavior, not movable-bridge completion.

The Release build passed. Deployment verified all 12 payload files against build output and all
18 original installation files in a fresh original-file-only backup. Version `26.10.13.0` remains
unchanged; no existing bridge assets were removed. See the task output
`Movable-surface-reference-deployment-20261016.md` for the installation path, backup and hashes.

The installed `NetCompositionHelpers` propagates hidden section state to `CompositionState.Hidden`.
`LaneSystem`, when creating node lanes with hidden endpoint compositions, removes `m_HideLaneTypes`
and retains native lane, curve and traffic components. This is evidence for using the native hidden
node mechanism instead of deleting traffic lanes or creating a mod-owned visibility controller.
Complete conversion must still prove that every selected section receives the correct native state.

| Extracted program | SHA-256 |
| --- | --- |
| Network fragment `581-128.dxbc` | `6D33F83FDF2DF18EB2935B707334E74E9387EAADF7CCB087ED98DDEF684DF95E` |
| Curved default vertex `570-4.dxbc` | `D95BDC48CB7E947F2A5DD67E255409B1249891A755EE753E72516A4E4CE736ED` |
| Curved tiling vertex `570-7.dxbc` | `487C67B291CE37C76593259B6698F521D19FA5B584133662318993C2F539FB7C` |
| Curved dilated vertex `570-10.dxbc` | `1E51D4F0D9D10B5CDBCCCB3C40306E94EB9762D41D0B513BAD0C0217592E3E26` |

These are read-only source-program extractions. No game shader, shader bytecode, engine code or
runtime patch was modified. Build success does not establish material equivalence or completion.

| Stage | Current evidence or implementation | Remaining work |
| --- | --- | --- |
| Native inventory | Ten owners and eleven network roots audited; 88 object/LOD geometry files hash-checked and decoded | Verify immutable implementation records and bind data against those assets |
| Style identity | Ten runtime catalogue IDs, localized in twelve languages; normal creation/preview dispatch and explicit missing-route errors | Implement the ten actual generators on their assigned branches |
| Selected-road composition | Optional occurrence tracing and private conditional section conversion implemented | Integrate measured rules, complete predicate manifest and caller validation |
| Persistent closed surface | Native straight-edge base composition and position/UV evaluation implemented | Verify native surface compatibility; convert only necessary resource data; supply LODs, markings and mounted objects |
| Structure separation | Raw-channel-preserving triangle replacement implemented and connected to complete LOD asset construction; 88 source files decoded | Identify and record exact per-mechanism replaced-road triangles, widening plans and all LOD correspondence |
| Motion | Generic seam clipping and native rigid road model encoding implemented | Supply measured partitions, persist native skeletons, implement bounds and mounted objects |
| Network integration | Per-archetype conditions and crossing metadata audited | Apply exact node/edge/auxiliary rules without losing lanes or duplicating stationary visuals |
| Persistence | Complete prepared road LOD graph can create native geometry and join the existing owned Prefab save plan through TowerFactory | Supply compatible converted visuals and per-style callers; save/reload the complete bridge without manager callbacks |
| Build and deployment | Shared runtime code and offline tool compile; 12 installed payload files hash-verified | Complete generators and game acceptance remain outstanding |
| Acceptance | No generated movable samples | Human-created samples, width audit, near/far motion, shipping/traffic, cold-start and lifecycle checks |

There is no claim of a successful animation, navigable crossing or manager-independent generated
movable bridge. Missing implementation is not an installation failure or a missing user approval.
Human acceptance is a later requirement and must not be used as a reason to skip independent
implementation work.

## 6. Documentation and cleanup during implementation

Update this record and the relevant bridge documentation in the same increment as implementation.
Record the exact style, supported input/state, source evidence, changed code, build/deployment result
and outstanding acceptance. Update the README and model description when support actually changes;
do not remove deferred wording merely because a catalogue entry or a compiled route exists.

Shared conversion infrastructure belongs on `dev`; mechanism-specific data and integration belong
on the branches reserved in bridge generation contract section 12. This reservation has not created or switched
any branch. Preserve existing working-tree documentation changes when preparing branch work.

Adding movable styles alone does not invalidate fixed styles. Before any deployment cleanup,
trace the changed generation paths and record the exact affected style IDs and existing UUIDs.
Remove only their UUID-owned assets. This increment adds unused movable construction primitives and
optional composition tracing; existing callers keep their previous behavior. No admitted style's
generation output changes, so the cleanup scope is empty. No existing bridges were removed. The
runtime source changes require the project build/deployment workflow even while admission is deferred.

The required Release build and deployment were completed on 2026-10-16 without changing the
user-specified project version. Twelve payload files were installed into the active
`.cache/Mods/pdx_mods/160320_17` package and their SHA-256 values matched the build outputs.
The previous package had already been backed up. No duplicate local mod installation was created.
The game remained closed; a successful startup has not been observed. A future package refresh
can replace these cache files. This is deployment verification for the tool-change workflow,
not evidence that a movable bridge generator has been deployed. The subsequent runtime-infrastructure
deployment also verified 12 files and took a fresh verified 18-file installation backup under the
task's outputs. It retained version `26.10.13.0`; the original requested bridge backup remains intact.
See `Movable-runtime-deployment-20261016.md` in the outputs for that installation's hashes.

## 7. Evidence locations

The current research deliverables are local task outputs, not runtime dependencies:

```text
Task output directory:
C:\Users\admin\Documents\Codex\2026-10-15\c-users-admin-documents-codex-cs2bridgebuilder\outputs

Generic design: Movable-bridge-section-conversion-design.md
Complete audit index: Movable-bridges-complete-mapping-audit.md
Split audit and evidence: movable-bridges-audit/
Decoded skin audit index: Movable-bridges-skin-audit.md
Split geometry inspections: movable-bridges-skin-audit/
Deployment evidence: Movable-bridges-deployment-20261016.md
Original-file backup directory: 26.10.16桥梁备份/
```

The underlying resource audit describes game version `1.6.2f1`. Scratch decoded data is retained
in the task's `work/bridge-research/all-movable-audit.json`; decompiled native classes are under
`work/bridge-research/Game/`, with `GeometryAsset.cs`, `SurfaceAsset.cs` and `MaterialLibrary.cs`
beside them. These local paths are research provenance, not portable implementation asset paths.
Resource-file path IDs must not become runtime identity constants.

See [geometry inspection workflow](Movable-Bridge-Geometry-Inspection.md) for the new offline
command, its native format assumptions and the distinction between measurement and classification.

## Draw01 road-top boundary evidence (2026-10-16)

Actual mesh-0 inspection now records 400 / 342 / 56 road-top face ordinals for LOD0 / LOD1 / LOD2. Sixteen LOD0 fixed building-platform trim triangles are explicitly excluded. Exact-position edge adjacency yields 296 / 292 / 56 neighboring faces, including structural railings and supports; these are not automatically removed. Moving carriageway and sidewalk seams have different longitudinal positions and cannot share a single full-width partition. The external Draw01-road-top-face-map.md report retains source hashes and complete horizontal face lists. This is measured input for the still-unfinished replacement plan, not generator completion; curb/end-cap handling and structural widening remain required. No runtime code, installed files or existing bridges changed in this evidence increment.


## Draw01 road-face source integration (2026-10-16)

On branch `bridge/draw-01`, `Draw01RoadSurfaceData.g.cs` now records the measured road-top faces (400 / 342 / 56) and fixed inner-curb walls (24 / 16 / 12), separately for all three LODs. The offline emitter verifies source OBJ hashes and the sets are disjoint from retained platform trim. Source topology counts are recorded; invalid LOD requests throw. No runtime spatial classification is introduced. Moving curb/end-cap classification and complete structural widening remain unfinished, and no generator calls this data yet. No published bridge generation behavior changed, so existing bridges were preserved. Release compilation passed; 18 original installation files were backed up and 12 installed payload files matched build hashes. This is source-data progress, not completed movable bridge support or in-game acceptance.

## Draw01 concrete network conversion (2026-10-16)

`Draw01NetworkData.g.cs` now calls the shared section converter with the measured Draw01 three-branch rule for every selected-road occurrence. The emitter checks all seven native lateral groups against that rule. It also supplies native Bridge fields, additive edge/node states and all four object placements. Source occurrence conditions and lateral roles remain preserved. This is specific to Draw01; other mechanisms require independent rules. Native bottom/side piece replacement and the complete generator call chain remain unfinished. The class has no generator caller yet, so existing output is unchanged and no bridge was removed. Release build passed; 18 original installation files backed up and 12 deployed files hash-verified. See the external Draw01-network-conversion-implementation.md report for evidence and limits.

## Draw01 separate network pillar assembly (2026-10-16)

`Draw01PillarData.g.cs` now assembles the native separate pillar owner and configures its top/column/footing render graphs. It preserves the zero anchor offset, three mesh slots, stack order/direction, native footing BaseProperties, LOD references and content prerequisites. The six coarse renders do not receive high-detail stack/base components absent from the prototype. Nine original pillar geometry payloads were extracted and hash-checked for the subsequent width work. These are separate from the main mechanism support meshes. No complete generator caller or widened pillar geometry has been delivered yet. Release build passed; 18 originals backed up and 12 installed files hash-verified. Existing bridges were preserved. See external Draw01-network-pillar-implementation.md for exact hashes and remaining integration.

## Draw01 network pillar geometry decoded (2026-10-16)

All nine original pillar position buffers and the three cap index buffers have been decoded. Independent column and footing meshes are centered and must preserve transverse shape. The cap combines a cylindrical center connection with tapered lateral supports in one welded mesh; whole-mesh stretching would widen the center connection. LOD2 has different native extents, so independent per-LOD bounding-box scaling is not valid. The external Draw01-network-pillar-geometry.md report includes actual position and topology views plus exact measured extents. Authored cap-part correspondence and runtime width coefficients remain pending; this inspection introduced no runtime changes or deployment.

## Explicit selected-road to mechanism frame (2026-10-16)

The shared asset builder now requires an explicit RoadToObject Pose per level and transforms both evaluated road and non-decal lane geometry before seam binding and motion-bound validation. Normals/tangent directions rotate; UVs, colors, handedness and source provenance are preserved. Appearance evaluation remains in the original road frame and authored structure is not transformed again. This closes a coordinate-frame gap in the existing build pipeline; it does not complete decals or create a style caller. No generator currently constructs these levels, so existing bridge output is unchanged. Release build and all 12 installed payload hashes passed; 18 originals backed up. No bridges removed. See external Movable-road-frame-integration.md for the calling sequence and validation limits.

## Native decal mesh-pass and clipping review (2026-10-16)

Extracted DefaultDecal shader 587 and compared its DBufferMesh path with curved shader 572/60. Default mesh decals still reconstruct scene-depth receivers and project through instance matrices; a material-template swap with custom triangle UVs is not sufficient. Curved endpoint opacity differs from default height-only fading. Removed an incorrect unit-cube preclip in MovableDecalProjection: native BatchDataHelpers scales the authored mesh dimensions, while the actual curved shader uses curve-relative width/height/end tests retained by the implementation. No engine shaders changed. Release build passed, 18 original installation files backed up, and 12 installed payload hashes verified. Full decal conversion and generator routing remain unfinished. See external Movable-native-decal-conversion-audit.md for bytecode hashes and limits.

## Curved decal material inputs (2026-10-16)

The shared texture reader now exposes a separate template-20/32 curved-decal path with the same native VT reconstruction and cleanup as network readback. MovableDecalSurface evaluates albedo, tangent-space normals, metallic/smoothness and four independent opacity outputs from native source maps and projection. Alpha-source selectors use mask B for normals and mask G for metal/smoothness; BaseColor alpha affects albedo only. Channel-enable flags and per-channel endpoint transmission are retained. Native BatchDataSystem supplies curve properties only through the lane update path, so an unchanged curved material cannot be assumed valid as a bone-parented ordinary object. The source remains a conversion input stage, not a completed output representation or generator. Build passed, 18 installed originals backed up, 12 deployed payload hashes verified; no existing bridges removed. External report: Movable-decal-material-channels.md.

## Draw02 measured network implementation (2026-10-16)

`Draw02NetworkData` now implements the independently audited 27 section entries as nine equivalent
three-branch occurrence rules, native Bridge fields, appended edge/node states and all four object
placements. The private generated pillar/mechanism and selected-road outside connection are supplied
by the caller. The native pillar offsets differ from Draw01 and are preserved exactly.

The change was authored and compiled on isolated branch `bridge/draw-02` (commit `707c550`), then
integrated into dev as `9b58edf`. Only the new Draw02 source and its implementation document were
committed; all pre-existing dirty changes were retained. Shared conversion was supplied unchanged
for the isolated compile. The integrated Release build also passed. Eighteen original installed files
were backed up in `artifacts/backups/draw02-network-20261016-035154`, and all twelve deployed payload
files matched their build hashes. Backup directories contain original files only.

There is no caller of this class yet, so this increment changes no existing generated bridge output
and removed no bridges. It does not complete Draw02 structure generation, moving markings, generator
routing or native lifecycle acceptance. See `Draw02-Network-Implementation.md` for the implemented
scope. No game launch or visual-generation unit test was performed.

## Draw03 and pedestrian draw network implementations (2026-10-16)

Three additional independently measured network-data classes are implemented: Draw03 (33 entries),
PedestrianDraw01 (9 entries) and PedestrianDraw02 (9 entries). Each emitter checked all native
occurrence groups before producing its section branches. Draw03 retains its segment length 152
and pillar Y=-6.550000190734863 / Z=5 or 0. Both pedestrian variants retain only node state 51
and three object placements; no road-specific node states or outside-connection object is added.

Each source was authored and compiled on its own isolated style branch, then integrated into dev.
Shared conversion was supplied unchanged for isolated compilation. Existing dirty changes were
not copied between style worktrees or included in the style commits.

| Style | Style commit | Dev integration | Original-only deployment backup |
| --- | --- | --- | --- |
| Draw03 | 1f06575 | 1f3b1ec | artifacts/backups/draw03-network-20261016-035311 |
| PedestrianDraw01 | 6d66156 | f07795b | artifacts/backups/pedestrian01-network-20261016-035412 |
| PedestrianDraw02 | fb0b955 | 1a9fefb | artifacts/backups/pedestrian02-network-20261016-035435 |

All isolated and integrated Release builds passed. Every deployment backed up and verified 18
original files and verified all 12 installed payload hashes. No existing bridges were removed:
these three new classes have no generator callers and do not alter existing output. Their network
data is implemented, but structure/appearance conversion, routing, persistence acceptance and the
native traffic/shipping cycle remain unfinished. No game control or visual-generation unit tests
were performed. See each style's Network-Implementation document for its exact scope.

## Lift01, Lift02, Lift04 and Lift05 network implementations (2026-10-16)

Four independently audited lift-network data classes are implemented. Lift01's 11 entries use
explicit Side/Deck roles; its deck edge hides Bottom and is not half-length. Lift02, Lift04 and
Lift05 each have 19 entries using explicit Span/IntersectionMedian roles; the median retains its
single intersection-only branch. No additional edge/node states are introduced, because all four
native records have empty state-rule lists. Every variant keeps its own Bridge and placement fields.
Lift04/05 retain three native object slots without inventing an outside connection.

| Style | Style branch commit | Dev integration | Original-only backup under artifacts/backups |
| --- | --- | --- | --- |
| Lift01 | 61729fa | 9aad8f0 | lift01-network-20261016-035614 |
| Lift02 | 8cd42fe | 8cd42fe | lift02-network-20261016-035722 |
| Lift04 | 394c879 | f74097b | lift04-network-20261016-035727 |
| Lift05 | 011ab52 | de053cd | lift05-network-20261016-035732 |

Each change was authored and compiled in its isolated mapped style branch, then integrated into
dev without committing pre-existing dirty changes. Each integrated Release build passed; each
deployment backed up and hash-verified 18 original files and hash-verified 12 installed payloads.
The shared section converter was supplied unchanged for isolated builds and excluded from style
commits. No existing bridges were removed because the new classes still have no generator callers.

Explicit source-role assignment, structure and moving road/marking conversion, complete routing,
persistence and native-cycle acceptance remain unfinished. Lift03 main/auxiliary network data is
not covered by this increment. No game control or visual-generation unit tests were performed.

## Lift03 main and auxiliary network implementation (2026-10-16)

`Lift03NetworkData` now records separate main and auxiliary schemas. All 23 main entries and 21
auxiliary entries were checked before emission. The main exposes Side/Deck roles; the auxiliary
exposes Side/TrackOrShoulder/Median roles, retaining four branches for tracks/shoulders and three
for its median. The two native exclusion-list orderings on equivalent track branches are treated
as the same condition set; no exclusions are removed.

Main/auxiliary Bridge fields, main-only state 51 rules, four main placements and one auxiliary
outside connection are implemented. The native auxiliary link uses position (0,-10,0), linked
end offsets and inversion mode 0. Selected lower-network direction still needs request-path
integration; the measured data does not invent an adjustable deck separation.

Authored and compiled on `bridge/lift-03` at `71e38be`, integrated into dev at `3b39dc6`. Both
Release builds passed. Eighteen installed original files were backed up and verified under
`artifacts/backups/lift03-network-20261016-035930`; all twelve installed payload hashes matched.
No existing bridges were removed. The new class has no generator caller and cannot alter their
output. All ten styles now have network-data source classes, but this is not complete mapping
integration or generator delivery: explicit selected-road role resolution, structure/appearance,
markings, routing, native traffic/animation and persistence acceptance remain unfinished.


## Native mechanism configuration increment (2026-10-16)

Nine additional style-specific mechanism classes join the existing Draw01 configuration. All ten
styles now have object component, mesh-placement, authored-attachment and per-LOD procedural bone
configuration. PedestrianDraw02 has two main-render levels; the other nine have three. Native
per-LOD source IDs and bone types are preserved independently. Lift02 has no authored attachment
component. See the per-style `*-Mechanism-Implementation.md` records.

The nine additions were compiled on their exact style branches, integrated into dev through
`2e98f4b`, built and deployed. Each deployment verified 18 backed-up original files and 12 installed
payload files. No bridge cleanup was required because these classes have no generator callers yet.
Structure removal/widening, selected-road moving surfaces and markings, full routing and human
acceptance remain unfinished; this increment does not establish completed movable generators.


## Native mechanism render graphs (2026-10-16)

All ten styles now have per-style RenderData configuration for the mechanism, column and
footing, covering 88 native render records in total. The source records supply StackProperties,
BaseProperties, LOD links and content prerequisites independently per level. Procedural skeletons
remain the responsibility of each style's MechanismData; geometry must already be generated.

Lift01 preserves the main stack's 4.5 m start overlap. Footing end overlaps and scaling flags
remain style-specific. Lift03 additionally preserves BaseProperties on its footing LOD1.
PedestrianDraw02 has two mechanism levels, three column levels and two footing levels.

Each change was compiled on its exact style branch, integrated into dev through `68d9fec`, and
built/deployed with 18 original backup files and 12 installed payload hashes verified per update.
The new render configuration has no generator callers, so no existing bridge output or cleanup
scope changed. This does not complete structural widening, selected-road conversion, markings,
full generator routing or human acceptance. See the per-style Render-Implementation records.


## Selected-road object composition and binding (2026-10-16)

Closed-road compilation now extracts native object-placement rules through the installed
composition job in a disposable world. A shared attachment binder maps resolved placements
to the measured road partition using native connection IDs, retaining prefab/group/probability
and leaving inverse-bind conversion to the game. See [Movable road objects](Movable-Road-Objects.md).

The code was built/deployed with original-file backups and payload hash verification. Native
spacing/theme/traffic-sign resolution and the final binder caller remain unfinished; these
helpers do not establish complete generation or motion acceptance. No existing bridges were removed.


## Native candidate placement increment (2026-10-16)

MovableRoadObjectPlacement now implements the candidate-placement portion of game 1.6.2f1
SecondaryObjectSystem.CreateSecondaryEdgeObjects. It consumes actual edge geometry, endpoint
composition/geometry, selected-network geometry data and composed object rules. It preserves
minimum length, even spacing, endpoint contributions, avoidance-spacing parity, randomized curve
offset, native curve-length evaluation, tangent blending, local offsets and the native in-loop
probability update. It does not infer endpoint contributions from a bridge's fixed node offset.

The resolver callback receives parent/local/object transforms and shares the same random stream
by reference. This matters because native placeholder selection consumes random values between
placements. Generating all coordinates first with an independent random stream would change later
selection and placement. The callback must resolve native requirements, theme, replacements,
traffic-sign/light needs and probability before producing bound ObjectSubObjectInfo entries.

The placement code was built and deployed on dev. The original-file backup contains 18 files;
12 installed payload files were hash-verified. No existing bridge output changed because the
final generator does not yet call this stage. It is not complete attachment generation: the
resolver callback, generated lane context, native anchoring/elevation and final assembly remain
required. No synthetic visual tests or game operation were performed.


## Native object selection and placement resolution (2026-10-16)

MovableRoadObjectSelection implements the native secondary-object selection stage using read-only
prefab data. It preserves default-theme requirements, AND-of-OR requirement groups, SelectOnly and
IgnoreExplicit semantics, randomized group indices, cached randomization groups, weighted ties,
placement probability and native traffic-sign/light/lane-direction scoring. No name-based choice
or prototype-light substitution is used.

TryResolve consumes a placement candidate and explicit traffic needs, resolves a concrete native
ObjectPrefab, applies the selected orientation flip, native top/center/ground anchoring and native
rotation symmetry. Its ObjectSubObjectInfo is ready for the existing road-frame/bone binding stage.
The saved placement has probability 100 because selection has already applied probability; applying
the original probability again would produce a second independent omission. Resource references are
retained rather than copied into new materials or textures.

Selection was derived from game 1.6.2f1 SecondaryObjectSystem.CheckRequirements and the two
CreateSecondaryObject overloads. The inspected decompiled source SHA-256 is
`c3196ba86b10b5d43518a607b684cc6afacbf1c3b927bd9bc5e200d3031fe21e` after UTF-8 text reading.
The native requirement set for this path contains the default theme only. The shared selector
retains this rule rather than inventing additional requirements.

Both successful increments were built and deployed with 18 original backup files and 12 deployed
payload hashes verified. One missing namespace compile error was corrected before deployment;
the failed build was not installed. CS0649 warnings remain on the traffic-needs fields because
their actual native-lane context producer is not yet connected. They are not evidence that empty
traffic needs are valid: callers must provide measured/generated native traffic context.

The placement visitor, selector and binder still require final orchestration, lane-derived traffic
needs and any applicable per-edge replacement selection. Tree/entity state, full attachment lifecycle
and human motion/night/cold-load acceptance remain unverified. This increment does not create a
finished movable generator or remove any existing bridge. No engine shader or simulation system was
modified; the generation-time selection code emits native prefab placements.


## Edge traffic needs and object orchestration (2026-10-16)

MovableRoadTrafficNeeds now calculates native edge-object traffic needs from an explicitly
prepared lane world. It reads closest eligible vehicle/pedestrian lanes, master/slave links,
merge/turn/forward flags, incoming/outgoing lane aggregation, speed transitions, public transport
restrictions, roundabouts and bicycle restrictions. Connected-edge traversal and district rules
use the native EdgeIterator and AreaUtils paths. It writes no network or simulation data.

MovableRoadObjects.ResolvePlacements connects native spacing candidates to this calculation and
the prefab selector, returning resolved ObjectSubObjectInfo placements in the closed-road frame.
The lane world and prefab source world remain distinct: prefab component-presence flags are read
in the source world, while all lane lookups use the prepared world's owner/endpoint entities.
No source-world entity is substituted for a remapped lane-world entity.

The caller must prepare actual endpoint compositions, edge/node geometry and native node/edge
lanes. Empty fabricated endpoint buffers cannot establish the absence of incoming traffic. The
current BridgePreviewLanes staging path does not yet provide that complete endpoint context,
so the final closed-road/generator caller remains unfinished. Per-edge SubReplacement selection
and its tree-age state also remain outstanding; this entry point does not establish their support.

The shared code was built and deployed twice, with 18 original backup files and 12 installed
payload hashes verified per successful deployment. The remaining nine CS0649 warnings concern
traffic-needs fields not assigned by the copied edge-object path (including crossing/vehicle
masks and secondary speed/sign fields). Node-object signal generation is a separate native path;
this edge calculator does not claim to implement it. No warnings were hidden with pragmas or
invented nonzero values. Existing bridge output and cleanup scope are unchanged.

Human visual, traffic, shipping and cold-load acceptance remain pending. These callable stages
are not a completed movable bridge generator.

## Stationary object suppression in moving network nodes (2026-10-16)

Native `NetCompositionSystem.InitializeCompositionJob.AddCompositionObjects` reads each selected
piece's `NetPieceObject` buffer. `NetCompositionHelpers.TestObjectFlags` tests composition and
section requirements, not hidden mesh layers. Consequently, hiding all road layers does not by
itself suppress stationary lamps or signs.

`MovableSectionConversion` now uses `MovableSectionObjects` for the measured branch requiring
`Node` and `FixedNodeSize` with all layers hidden (-1). This is an explicit moving-node branch,
not a rule for arbitrary layer masks. Ordinary and bottom-only connecting branches retain their
original object behavior.

The pass copies only pieces containing `NetPieceObjects`, removes that component from the private
copy, and copies only parent sections whose references change. Other piece components, including
lanes and native pricing, remain intact. Geometry, surface and object assets retain original
references. Source graphs are not mutated; shared references within one conversion are reused.
Malformed null/cyclic external subsection graphs fail conversion with cleanup of owned allocations.

The common conversion output and all ten network adapters now return `PrefabBase[]` for owned
nodes, rather than a sections-only array. This shared API adjustment ensures changed pieces are
not omitted from the eventual save plan. No per-style measured mapping values were changed.
The final generator assembly must pass the entire array into the existing owned-prefab writer
and preview lifecycle. Those generator callers are still absent; the API is not evidence of a
successful end-to-end saved bridge. The existing tower writer accepts arbitrary owned Prefabs.

The selected-road object extraction occurs before suppression, so its rules remain available for
moving object placement and bone binding. This pass does not suppress lane-generated markings;
that requires the separate persistent moving-marking path. No engine code or shader was replaced.

Compilation and deployment are verified; game-cycle and cold-start acceptance remain pending.
No existing bridge output reaches this new conversion path yet, so no existing bridge was removed.

## Movable preview ownership correction (2026-10-16)

The actual preview entry point allocates `BridgePreviewSession.Name` as `tmp{uuid}` and passes
that name to the shared generation pipeline. Permanent creation uses `b{uuid}`. Inspection found
that moving-road render construction, network appearance conversion, curved-surface conversion
and mechanism assembly all required a permanent UUID, making the temporary path fail before
geometry creation even when all required inputs were supplied.

These four paths now use `MovableAssetOwnership`. With no preview context, the existing permanent
`BridgeAssetInfo.MatchesOwner` rule remains authoritative. With a preview context, the owner must
be a valid `tmp{uuid}` and the generated name must contain that identity. The permanent identity
recognizer, dependency ownership, migration and cleanup rules have not been broadened.

Preview material resources still use the preview's transient native database, and preview geometry
still uses `PreviewGeometry.Capture`; neither path writes generated geometry into the user database.
Failed render construction now recognizes temporary resources during rollback and additionally
requires their database to be the active preview database before deleting them. Permanent rollback
continues to require the permanent owner. Only assets allocated by the failed call are candidates.

Validation: Release compilation succeeded (the existing nine traffic-selection field warnings remain),
18 original installed files were backed up and verified, and all 12 installed payload files matched
the build. Backup: `artifacts/backups/movable-preview-ownership-20261016-042742`.
No currently routed bridge output uses these new movable helpers, so no existing bridge was removed.
This corrects an inspected code-path failure; it is not evidence of a rendered movable preview or
completed generator. Final generator assembly, moving markings and native in-game acceptance remain
outstanding. The saved-asset writer already accepts arbitrary owned Prefabs, but future generator
integration must transfer every converted Section and Piece to its existing ownership list.

## Native moving-decal resource writer (2026-10-16)

Added `MovableDecalAssets.Create` for receiver samples already evaluated by the curved-decal
projection/material stages. It writes native DefaultDecal (template 18) resources, not road
albedo composites. The sampled outputs are packed as follows:

| Native texture | Encoded channels |
| --- | --- |
| BaseColorMap | Evaluated RGB and albedo opacity |
| NormalMap | Unit normal in the destination projector tangent frame, encoded XY, alpha 1 |
| MaskMap | Metallic, enabled metal/smoothness opacity, normal opacity, smoothness |

The native alpha-source selectors use mask B for normals and mask G for metal/smoothness;
base color and scalar multipliers are neutral because those values were already evaluated.
Source channel-enable flags are preserved. Both enabled mask channels must have identical opacity,
as native DefaultDecal has one shared mask-opacity input. The caller must explicitly supply normals
in the destination projector frame; curved-source tangent normals are not silently reused.

The resource writer allocates one changed native surface and three changed textures. These are
private resources because their coordinates and opacity channels contain evaluated curved-decal
results. It does not duplicate unchanged source materials/textures. Generated resources enter the
caller's rollback list, while temporary preview output uses only the transient preview database.
The existing owner distinction (tmp UUID versus permanent b UUID) is enforced.

This stage is compiled and deployed but is not yet called by a generator. It requires a receiver-page
builder, native projector volumes and bone-parented placements. Destination receiver height must
remain in the projector's full-opacity plateau, otherwise native height fade would multiply the
already evaluated source height fade again. Source-to-projector normal basis, clipping at leaf
seams, Other receiver layers, renderer priority, camera fading and texture filtering still require
integration and verification. The road builder continues to reject unprojected decal batches;
this increment does not remove that failure or claim full moving-marking support.

No shader or engine code was replaced. Native DBuffer use preserves the rendering stage in which
markings are applied; this alone does not prove full appearance equivalence. No visual-generation
unit tests or game automation were run. Release build passed with the existing nine selection-field
warnings. Backup `artifacts/backups/movable-decal-resources-20261016-042945` contains 18 verified
original files; all 12 deployed payload hashes match the build. No existing bridges were removed.

## Seam-bound decal receiver sampling (2026-10-16)

`MovableDecalReceivers.Sample` now samples curved decal batches on actual selected-road receiver
triangles after `MovableRoadGeometry.TryBind` has split them at measured seams. It retains each
triangle's native bone index, returns receivers from mechanism coordinates to the selected-road
frame, and subdivides generated receiver triangles to the requested texel density/page limit.
Each emitted page carries its planar projector pose, span, receiver provenance, separate material
channel samples and native curve-projection records. Zero-opacity pages are omitted. These are
new road triangles; this operation does not classify or transform authored bridge structure.

The page producer is not yet wired into complete projector creation. It deliberately retains
projection direction data rather than silently treating curved normals as default-projector normals.
Receiver-edge filtering and subtexel marking coverage require further work before emission can be
accepted. A page per receiver face is an initial representation, not a claim of acceptable final
asset count or draw-call cost; compatible coplanar receivers should be grouped during integration.

Additional bytecode evidence affects the conversion design: in native curved DBufferMesh 572/60,
lines 329-339 compute the camera-distance factor multiplied by height fade; lines 465-474 include
that value inside the transmission exponent. Native default DBufferMesh 587/40 applies that fade
as a direct opacity multiplier. Thus baking endpoint opacity at full distance visibility and
letting DefaultDecal multiply its camera fade does not preserve the source response at other
camera distances. This limitation is now established from the shader programs, not just an
untested appearance concern. The native resource writer must not be mistaken for a complete exact
conversion. A correct final representation must resolve this difference without manager callbacks.

Release compilation and deployment passed. Backup
`artifacts/backups/movable-decal-receivers-20261016-043140` contains 18 verified installed originals;
all 12 installed payload hashes match. The existing nine selection-field warnings remain. No
visual-generation tests or game automation were used, and no existing bridges were removed.
The final generator routes and full in-game acceptance remain incomplete.

## Native curve parameter ownership audit (2026-10-16)

[The native parameter-path audit](Movable-Decal-Native-Parameter-Path.md) confirms that ordinary object batches cannot acquire LaneProperty curve buffers merely by adding CurveProperties. It also separates saved surface properties from native template registration. These paths must not be used as unproven shortcuts around the measured distance-fade mismatch. No runtime changes or asset deletion were made in this audit.


## All-style main-mechanism topology evidence (2026-10-16)

[The topology inventory](Movable-Mechanism-Topology-Inventory.md) covers 49 actual native mesh
streams and 1,917,588 triangles for all ten styles and their exported main-mechanism LODs.
Exact face sets, bones, bounds and source hashes are retained for indexed-edge and exact-position
edge connectivity. The stark LOD differences show why raw connectivity IDs cannot substitute for
reviewed logical-part correspondence: Draw01 main LOD0 has 392 position-connected sets while LOD2
has 10; Lift01 main LOD0 has 10,743 while LOD2 has 32. This supplies reproducible evidence for
structural classification without inventing runtime coordinate heuristics. Semantic classification,
road removal completeness and widening plans still require review.

The native decal audit also now traces ObjectSubLanes through LaneSystem and RelativeObjectSystem:
render-only bone interpolation does not establish a moving world-space lane curve. No runtime
code changed in this investigation, so no deployment or bridge cleanup was performed.

## Draw01 rigid control buildings across LODs (2026-10-16)

[Control-building face metadata](Draw01-Control-Building-Mapping.md) now records 2176/1006/62
main-mesh faces and the complete 64-face glass mesh in both near and middle LODs. Offline top,
side and end views confirm the retained positive-X building, platforms, stairs and support feet.
All are rigid side structures; they must not stretch with the road. Data was developed on
bridge/draw-01 (d8d025a), integrated into dev (05f6eb8), built and deployed with 18 original-file
backup checks and 12 payload checks. No runtime spatial rule was introduced. The complete
structural plan and generator caller remain absent, so existing bridges were preserved.

## Draw01 moving road solids across LODs (2026-10-16)

[Moving road replacement data](Draw01-Moving-Road-Replacement.md) now records 1888/1472/72
removal faces for the two moving leaves in main mesh 0. Near and middle data include complete
carriageway and sidewalk slabs, raised profiles and closures, rather than only the top planes.
The far representation retains 44 shared underbody faces and all 84 reviewed lateral-envelope
faces. Actual native geometry was inspected with the removal sets applied at all three LODs.
The original position channels were also verified bit-for-bit against the native Geometry assets:
70 far-LOD centerline vertices have tiny native X residues and exact-zero mirrored counterparts.
Future widening must use explicit centerline identity, not the sign of those residues.

Data was authored on bridge/draw-01 (`63d88f5`) and integrated on dev (`96da540`). Release
compilation passed with the existing nine CS0649 warnings. Deployment backed up and verified
18 installed originals under `artifacts/backups/draw01-moving-road-removal-20261016-045753`
and verified all 12 installed payload files. The data still has no final generator caller;
no existing generated assets changed and no bridges were removed. Fixed approach replacement,
complete widening/partition plans, generator assembly and native in-game acceptance remain open.

## Draw01 fixed approach road faces (2026-10-16)

[Fixed road replacement data](Draw01-Fixed-Road-Replacement.md) adds 124/102/36 explicit
bone-0 road faces for main mesh 0. With the moving-road sets, the per-LOD removal totals
are 2012/1574/108 faces. Actual native end/plan projections were inspected; all previously
recorded road tops are covered and no control-building faces overlap. The coarse walls
extending into the structural pier remain; only their separately modeled road-facing curb
and end-return faces are removed. Fixed and moving data remain distinct from widening.

Authored on bridge/draw-01 (`2437e3c`), integrated on dev (`38a3bea`). Release compilation
passed with the same nine CS0649 warnings. Backup
`artifacts/backups/draw01-fixed-road-removal-20261016-050135` contains 18 verified installed
originals; all 12 deployed payload hashes match. No final generator caller exists for these
data classes yet, so no existing bridge output changed or required removal. The complete
generation and native acceptance requirements remain outstanding.

## Draw01 partial-face replacement finding (2026-10-16)

Actual widened-frame inspection now pairs 88 spanning near/middle members and retains their
highest-detail spans rather than independently classifying simplified LOD geometry. The
candidate frame data covers 19536/12248/542 retained moving vertices, but is not final.
It exposes four far-LOD mixed faces (24, 193, 351, 516): each includes both the old road end
and the structural end surface. The existing 72-face moving-road removal set is therefore
not a complete far road replacement by itself. Its whole-face mask remains useful, but needs
four explicit partial replacements. Keep the region below the measured Y=9.419189 boundary;
do not remove the lower structure or lower an original vertex silently.

Native raw-channel inspection verifies identical normal, tangent and bone bytes at each
clipped edge's endpoints. New boundary UVs require interpolation; untouched corner channels
remain unchanged. In particular face 193 has a different packed normal at its untouched side
corner, so a whole-face attribute override is wrong. At the time of this finding, MovableStructureModel.Plan supported only whole-face removal
and original-vertex positions. The explicit fragment implementation below supersedes that limitation. Fixed-end mixed faces require equivalent review. This finding supersedes any
interpretation that the static face masks alone completed road replacement. No runtime code,
installed payload or existing bridge changed during this inspection.


## Draw01 measured fragment construction and inspection (2026-10-16)

The shared MovableStructureModel.Plan now accepts measured EdgeVertex and Fragment records.
Original vertices retain their Y/Z coordinates and raw nonposition channels. Appended boundary
vertices copy identical edge channels and interpolate UVs in their existing layout. Replacement
triangles remain in their original material slot; bounds and vertex ranges include the new
vertices. No runtime part classification or cut-plane discovery is introduced.

[Draw01 fragment data](Draw01-Road-Fragments.md), integrated as dev commit `50c45a7` from
bridge/draw-01 `2b7c25a`, supplies four far moving-end replacements. Direct application of these
records to the native exported mesh produces 1122 vertices from 1118 originals. Faces
24/193/351/516 keep their winding and lower structure; the new upper edge is Y=9.419189453125.
Actual native-face projections were inspected at original width and a full width increase of
8 m. This is offline inspection, not C# execution or human game acceptance.

The preceding deployment used `artifacts/backups/draw01-road-fragments-20261016-051105`
(18 original files; 12 payloads). All 12 installed payload hashes were rechecked against Release
output during this inspection. No runtime code or existing bridge was changed by the inspection.

Fixed-approach correspondence remains separate. High/middle-detail fixed supporting faces
extend to Y=9.800049 from Y=9.199951, so the moving-end cut height cannot be applied across the
fixed bridgehead. Far outer walls, pier ends, inner returns and rear caps merge several authored
parts. Retained fragments must follow the high-detail structure rather than a height-only
candidate filter. Complete structural plans, selected-road appearance/markings, final generator
assembly and native acceptance for all ten styles remain outstanding.


## Draw01 fixed-boundary incidence (2026-10-16)

Actual native boundary inspection covers both fixed ends and all three main-mesh LODs.
Exact whole-edge matching finds 8/8/24 road-to-retained interfaces. Accounting for differently
subdivided collinear edges finds 56/54/26 interval incidences (48/46/2 non-identical edge pairs).
Counts represent incidences, not unique authored parts. The offline overlap tolerance is 1e-7 m;
no tolerance-based discovery enters the runtime generator.

Near/middle center support joins carriageway undersides at Y=9.5, Z=85.892578125. Inner sidewalk
walls join road bottoms at Y=9.800048828125; outer walls and building details join at Y=10.
Far center caps instead reach Y=9.800048828125 at Z=83.708984375/90.099609375, while sidewalk
interfaces reach Y=10.047119140625. Far face 173 and retained 311/313 also share overlapping
positive-side road-edge intervals. This confirms that whole-edge or one-to-one face matching is
insufficient and that a single fixed cut height would discard retained structure.

The complete interval inventory is recorded in the Draw01 fixed-road interface audit. It is
input evidence for the remaining fixed partition, not a completed fragment plan. Runtime code,
installed files and existing bridges are unchanged in this inspection.


## Draw01 fixed rear-cap asymmetry (2026-10-16)

Actual longitudinal sections distinguish the high-detail front center support (Y=9.5) from
its rear at Z=90 (Y=9.199951171875). The far center envelope reaches Y=9.800048828125 at both
ends and therefore cannot use one uniform high-detail structural cut. The positive-X rear
cap also retains a control-building-side slope from X=6.2816162109375/Y=9.199951171875 to
X=6.5/Y=10. Native face 3757 and its exact negative-Z counterpart 11651 establish that slope
at both bridge ends. A transverse mirrored clipping recipe would remove authored structure.

This is measured profile evidence for the unfinished fixed-fragment construction. The native
UV atlas comparison remains diagnostic only because its shader sampling orientation is not
verified. No runtime code, deployed payload or generated bridge was changed in this pass.


## Measured interior structural vertices (2026-10-16)

MovableStructureModel.Plan accepts optional TriangleVertex records after appended edge vertices.
This represents a measured boundary turn strictly inside an original triangle, which edge-only
records cannot express. Internal validation checks barycentric weights and original-triangle
membership. Constructed positions use widened source corners; original channels remain unchanged.
New UVs interpolate in the native layout, while other channels require identical source bytes.
Output counts, bounds, index ranges and motion-bound checks include the new vertices.

Draw01 far face 317 (indices 534/536/537) provides the native case: the measured rear-cap turn
at (6.2816162109375, 9.199951171875, 90.099609375) is strictly interior. Native raw decoding
confirms identical normal/tangent/bone bytes and varying UVs. The construction can represent
this point without new material resources. Full fixed-cap fragment triangulation remains open.

Release compilation and deployment passed (nine existing CS0649 warnings). Backup
`artifacts/backups/movable-interior-fragments-20261016-052144` contains 18 verified originals;
all 12 installed payload hashes match. Existing edge-only APIs preserve their output ordering.
No final generator caller exists yet, so no existing bridges were removed. This shared capability
is not completion of Draw01 or of the other nine movable generators.


## Draw01 fixed rear cap fragments (2026-10-16)

[Measured rear fragments](Draw01-Fixed-Rear-Fragments.md) now replace ten far main-mesh cap
triangles at both ends with fourteen retained triangles and twenty explicitly measured edge
vertices. The center and negative-X rear caps retain their high-detail structural upper boundary
Y=9.199951171875. Native edge channels confirm equal normal/tangent/bone bytes; new UVs use the
existing interpolation path. Actual native source and retained polygon projections were inspected.
Positive-X sloped rear caps 317/646 remain separate, as do the unfinished front and inner returns.

Authored on bridge/draw-01 commit `3a032b2` and integrated on dev `740a959`. Release compilation
passed with nine existing CS0649 warnings. Deployment backed up 18 original files under
`artifacts/backups/draw01-fixed-rear-fragments-20261016-052446`; all 12 installed payload hashes
were verified. No final generator calls these data yet, so no existing bridges were removed.
This data increment does not establish full generation or native in-game acceptance.


## Draw01 rear control-building slope fragments (2026-10-16)

[Rear slope records](Draw01-Fixed-Rear-Slopes.md) complete the measured rear-cap tables for
far faces 317/646. Native high-detail faces 3755/3756/3757 and 4641/4642 establish the retained
sloped support and upward building wall at positive X; the negative-Z end has the same logical
arrangement. The recorded profile retains the coarse outer corner where it enters the building.
Four edge vertices and four interior vertices emit eight triangles with the original winding.
Native normal/tangent/bone bytes are identical across contributing corners; UVs retain Float16.

Together with the preceding table, all twelve far rear structural cap triangles now have
explicit replacement records: 24 edge vertices, 4 interior vertices and 22 retained triangles.
Front caps, inner returns, complete structural widening, selected-road assembly and final
Draw01 routing remain unfinished. The other nine movable styles still require full generators.

Style commit `9acee7d`, integrated code commit `539499c`; documentation follow-up `bcc6ce1`
integrated as `d791731`. Release compilation passed with the existing nine CS0649 warnings.
Deployment backup `artifacts/backups/draw01-fixed-rear-slopes-20261016-052658` contains 18 verified
originals; all 12 installed payload hashes match. No final caller or existing generated output
changed, so no bridges were removed. Offline native-fragment inspection is not in-game acceptance.


## Draw01 central front cap fragments (2026-10-16)

[Central front records](Draw01-Fixed-Front-Fragments.md) replace eight far main-mesh faces
with twelve retained triangles and sixteen measured edge vertices. The structural ceiling is
the high/middle carriageway underside at Y=9.5; the far pier's own +/-83.708984375 Z planes
are preserved. The taller sidewalk inner walls are separate from this center-cap operation.
Native normal/tangent/bone bytes match along every construction edge, and source winding is
preserved. The actual native source and retained polygons were inspected at both ends.

Authored on bridge/draw-01 `62e8ccc`, integrated on dev `bbb017a`. Release compilation passed
with the same nine CS0649 warnings. Backup
`artifacts/backups/draw01-fixed-front-fragments-20261016-052853` contains 18 verified originals;
all 12 installed payload hashes match. No completed generator consumes these tables yet, so
no existing bridge output changed and no bridges were removed. Sidewalk front caps, inner
returns, full structural widening/road partition, final routing and native acceptance remain
open; none of the ten movable styles is claimed complete.


## Draw01 inner approach-wall fragments (2026-10-16)

[Inner-wall records](Draw01-Fixed-Inner-Fragments.md) replace far faces 119/282/447/606,
retaining the high/middle wall-top elevation Y=9.800048828125. Four measured edge vertices
replace only the old raised road-facing corners; the original lower and opposite corners stay.
Adjoining lower faces 118/281/446/605 remain, and separate curb faces are already included in
whole-road removal. Native edge attributes and source winding were inspected for all four.

Authored on bridge/draw-01 `3c6894c`, integrated on dev `0d0b350`. Release compilation passed
with the existing nine CS0649 warnings. Backup
`artifacts/backups/draw01-fixed-inner-fragments-20261016-053048` holds 18 verified originals;
all 12 installed payload hashes match. This unused data table changes no existing generator
output, so no bridges were removed. Sidewalk front caps, complete structural widening,
selected-road partition, final generator assembly and native in-game acceptance remain open.


## Draw01 assembled main-mesh road replacement (2026-10-16)

[Road replacement assembly](Draw01-Road-Replacement-Assembly.md) adds the remaining measured
sidewalk front caps while preserving their outer structural lips, and combines all six fragment
tables with both whole-road masks through Draw01RoadReplacement.CreateMainMeshPlan. Source
identity resolution and complete structural widening remain the caller's responsibilities.

Actual far-mesh construction parsed from the current source tables confirms 64 edge vertices,
12 interior vertices, 36 replaced source faces, 108 whole removals and 62 fragment triangles.
The resulting main mesh has 1194 vertices and 574 triangles. Source main mesh 0 has one submesh.
No whole-removal/replacement overlap, duplicate source replacement, invalid interior weights or
reversed/zero-area retained triangles were found in this actual-source inspection. These are
geometry records and inspection results, not proof of C# runtime or in-game acceptance.

Authored on bridge/draw-01 `39b49cc`, integrated on dev `2fe161a`. Release compilation passed
with nine existing CS0649 warnings. Backup
`artifacts/backups/draw01-road-replacement-assembly-20261016-053403` contains 18 verified originals;
all 12 installed payload hashes match. No complete generator consumes the helper yet, and no
existing bridges were removed. Structural widening, selected-road appearance/markings, full
assembly/routing, all other movable variants and native acceptance remain outstanding.

## Draw01 main-mesh widening assembly (2026-10-16)

[Moving-frame widening](Draw01-Moving-Frame-Widening.md) records 32326 moving vertices across
the three LODs, with 88 reviewed high/middle transverse-member pairs. The own-span mapping
of the 0.40000152587890625 m central half-span rejects a nonpositive resulting span; a full
width reduction of 4 m exposed inversion in offline native inspection and is not supported
by that mapping. This is not unrestricted narrower-road support.

[Fixed-frame widening](Draw01-Fixed-Frame-Widening.md) now covers 10530 original fixed
main-mesh vertices and 128 glass vertices at each of the two detailed LODs. Fixed buildings
share 28/24 original vertices with the other frame faces; the unified mapping moves each
vertex once. Native fixed-frame projections and an actual-source +8 m widening were inspected.

CreateMainMeshPlan now copies source positions, applies moving and fixed mappings, and then
constructs the road-boundary fragments. Its former caller-supplied widened-position argument
is replaced by the half-width increment. Repository caller search found no existing external
caller requiring migration. Inspection of the actual source tables and native face indices
found no moving/fixed membership overlap or uncovered retained vertex at any LOD. Separate
glass still requires its fixed helper; pillars, footings and attachments remain separate work.

Moving commits `7d3b6ee`/`2064a89` were integrated as `37588b6`/`4873073`. Fixed mapping
`610a97b` and assembly `ad6a150` were integrated as `e580b45` and `d01ba50`. Latest Release
compilation passed with the nine existing CS0649 warnings. Backup
`artifacts/backups/draw01-main-widening-assembly-20261016-054538` contains 18 verified original
files; all 12 installed payload hashes match. No final generator consumes this assembly yet,
so no existing generated output changed and no bridges were removed. Selected-road appearance,
markings, final generators for all ten variants, native motion/traffic/shipping and cold-start
acceptance remain incomplete. These are implementation increments, not completed bridge support.

## Draw01 stacked column and unskinned structural export (2026-10-16)

[Mechanism column widening](Draw01-Mechanism-Column-Widening.md) now supplies a complete
per-LOD structural plan for the 288/259/131-vertex native DrawBridge01Pillar meshes. These
are the mechanism's stacked middle renders, not the ordinary DrawBridge01NetPillar. The
far mesh has interior stations created by simplification; its nine displacement coefficients
inherit the reviewed high-detail front/rear panel spans. The 0.27947408216308495 m maximum
projection distance is source simplification, not a new runtime threshold or position change.
All original triangles and non-position channels remain carried through the common builder.

Native support inspection also established that these meshes have no bone-index channel.
MovableStructureModel now permits zero bones for an unskinned structure rather than requiring
an invented skeleton. Existing BlendIndices validation still rejects any index without a
corresponding bone, and negative counts remain invalid. The existing MovableRoadAssets caller
continues to supply its real skeleton count; no existing registered style uses this new column
plan yet. This change does not introduce a new channel or alter any shader.

Style commit `5482c16` was integrated as `f8ffa13`; the shared exporter adjustment remains in
the existing development working tree. Release compilation passed with nine existing CS0649
warnings. Backup `artifacts/backups/draw01-column-widening-20261016-054935` contains 18 verified
original files; all 12 installed payload hashes match. No existing bridges were removed.

The separately decoded footing contains protective rails and posts as well as its walls:
40 exact-position welded groups at each of the first two LODs collapse to two far-LOD groups.
This rules out using whole connected components as authored parts or applying the column
mapping to the footing. Footing correspondence, ordinary support, final road/marking assembly,
all ten final generators and human native acceptance remain incomplete.

## Draw01 footing part review (2026-10-16)

Actual native footing inspection now records all 40 near/middle welded-set correspondences:
two wall assemblies, eight bent protective rails, two central posts and 28 side posts.
The two far-LOD connected sets merge these parts. Neither whole-set scaling nor a per-vertex
sign mapping can replace the remaining authored-part decomposition. The reproducible offline
inventory is `work/bridge-research/draw01_footing_parts.py` in the research workspace; its
native projections and exhaustive correspondence report are retained in the chat outputs.

The protective rail's front/back straight-run endpoints are at absolute X=6.6650390625 and
6.6082763671875 respectively, before its rigid bends and longitudinal returns. X=4 is an
interior subdivision, not a measured part boundary. The centered post has a measured half-span
of 0.1361236572265625 m, exposing a stricter potential narrowing limit under the existing
own-span rule. No new guard or transform was installed from these candidate observations.
This increment adds source evidence; it does not claim a completed footing implementation,
require a deployment or remove any bridge. The full ten-style objective remains incomplete.

## Fixed render graphs use the existing ownership path (2026-10-16)

[Fixed render graph construction](Movable-Fixed-Render-Graphs.md) adds an explicit structure-only
Level constructor to the shared builder. It skips road baking/binding while retaining source
mesh validation, original surfaces, native components, LOD remapping, bounds/counts, geometry
serialization, preview capture and failure rollback. Native unskinned support renders receive
no fake skeleton. Existing skinned indices and any carried skeleton remain validated.

TowerFactory.CreateMovableRenderGraph collects both forms in the ordinary owned-prefab list.
No external caller used its former name, and no final movable generator is connected yet.
The work is shared on dev; no fixed style generation or existing asset is changed. Empty
external geometry is rejected and both forms use the same destination collision check.

Release build passed with nine existing CS0649 warnings. Latest backup:
`artifacts/backups/movable-render-graph-validation-20261016-055329` (18 original files verified).
All 12 installed payload hashes match. No bridges were removed. Saved-asset/runtime acceptance
and the full ten-style objective remain incomplete.

## Draw01 column assembly enters owned resource construction (2026-10-16)

[Draw01 render assembly](Draw01-Render-Assembly.md) now resolves the native column's two LOD
references, preflights all three source names and foreign-bridge ownership before reading any
geometry, prepares the recorded per-LOD plans and passes all three levels to the shared factory.
Private geometry reads are released on success or failure. Authored Y/Z stacking bounds and
native materials/components are retained; only the measured X expansion changes.

Native audit records 79598/79599/79600 and LodProperties 151034 confirm the exact source names
and LOD order. Comparison with decoded native geometry exposed small original bounds/vertex
differences. Static supports no longer run the procedural motion-envelope containment test;
their actual submesh bounds are still computed from emitted geometry, while their authored
stacking bounds remain preserved. Procedural renders retain the full motion-envelope check.
See [fixed render graphs](Movable-Fixed-Render-Graphs.md) for exact source evidence.

Style commit `2be2d63` was integrated as `cebebb5`; the shared bounds adjustment remains in the
development working tree. Release compilation passed with the existing nine CS0649 warnings.
Latest backup: `artifacts/backups/movable-static-authored-bounds-20261016-055607`, with 18 original
files verified and all 12 installed payload hashes matching. No existing bridges were removed.
The final Draw01 generator still has no caller of this assembly; other required structural and
road/marking work, all ten final routes and human native acceptance remain incomplete.

## Draw01 mechanism render assembly (2026-10-16)

Draw01RenderAssembly.CreateMechanism now combines three prepared selected-road levels with
their measured structure plans. The main mesh executes both widening tables and whole/partial
prototype-road replacement. The independent near/middle glass keeps its 128 vertices and 64
triangles, translated with the control building. Far LOD has only the opaque mesh. Original
structural surface ordering is preserved before appending selected-road/marking surfaces.

The method preflights all native names and foreign ownership before reads, releases temporary
geometry in finally, and forwards the caller's road geometry, material references, partition,
render bounds, skeleton, lanes, poses and appearance settings unchanged into the shared graph
builder. It rejects structure-only or already assembled input rather than omitting the selected
road or widening a structure twice. Unprojected decal rejection remains in the common builder.

Native records 79586/79587/79588 and their actual OBJ streams confirm mesh/surface counts 2/2/1,
main vertex counts 29840/18772/1118 and the separate 128-vertex, 64-triangle glass at LOD0/1.
Style commit `fdaeb3b` was integrated as `1e0dfd0`. Release build passed with nine existing
CS0649 warnings. Backup `artifacts/backups/draw01-mechanism-assembly-20261016-055741` contains
18 verified originals; all 12 installed payload hashes match. No existing bridges were removed.

The assembly still needs its final generator caller and prepared road inputs. Complete footing,
ordinary supports, selected-road appearance/decals, attachments, measured partitions/motion
bounds and final native traffic/shipping/cold-start acceptance remain required. No final movable
style is claimed complete by this increment.

## Draw01 footing transfer rejects proximity-only correspondence (2026-10-16)

Actual native candidate inspection found 204 ambiguous nearest-surface mappings at each of
LOD0/1: overlapping posts, rails and walls can have distinct displacement fields at the same
location. Unrestricted projection visibly leans side posts and was rejected. Restricting
transfer to the 40 recorded corresponding sets removes 200 ambiguities per level but leaves
four wall T-junctions. Near vertices 348/404/1424/1480 and middle 389/445/1390/1446 lie at
X=+/-3.20001220703125, Y=2, Z=+/-77.693359375, on another source triangle's edge through X=4.
Competing coefficients of approximately +/-0.80000305 and +/-1 would split that source seam.

The actual-source +8 m candidate also exposes the central post becoming a wide plate under
the own-span rule. Far nearest-projection uniqueness does not establish preservation of that
part in its merged mesh. The candidate therefore remains offline and is not admitted as
runtime widening data. The research script `draw01_footing_candidate.py` and complete JSON
retain the evidence; the chat output report includes both rejected and part-constrained plots.
The next step is continuous wall-interface mapping and explicit far-part correspondence.
No runtime code, installed payload or existing bridges changed in this inspection increment.

## Draw01 footing cap junction resolved offline (2026-10-16)

Native faces 158/186 and their opposite-end counterparts establish the footing cap's 4 m
transverse half-span. The +/-3.20001220703125 vertices of the adjoining top triangles are
T-junctions on those edges, not endpoints defining the cap span. The candidate now uses X/4
on that cap edge, preserving the raw geometry/topology and resolving all four near and four
middle ambiguities. The separately stacked column retains its own 3.2 m front-wall span.

The candidate's high/middle vertex roles are 336/336 wall, 1216/1196 rail, 560/428 side-post
and 40/40 center-post vertices. Far nearest-source assignment gives 302 wall, 80 rail and
22 side-post vertices, with none assigned to the center post. This exposes the next unresolved
question: how the merged far geometry/UV/material represents that post after its own-span
enlargement. Zero nearest-projection ties is insufficient evidence. The far mapping remains
unaccepted; no runtime data or installed payload changed. Exact triangle/junction evidence is
retained in the chat output `Draw01-footing-cap-continuity.md` and the reproducible candidate
script. The complete movable-generator objective remains unfinished.


## Draw01 footing far material and UV evidence (2026-10-16)

Native LOD2 uses a distinct baked VTSurface rather than the detailed albedo layout.
The original UV and triangle audit identifies a concrete central-post displacement mismatch
inside far triangle 287: resolving nearest-face ties does not make the candidate compatible
with the detailed own-span mapping. See [the material/UV review](Draw01-Footing-Far-Material-Review.md).
No runtime footing mapping, structural topology change, new material or shader was introduced.
The current structural position-only constraint still requires a representation decision for
merged far-detail parts. Other generator integration work remains available.


## Draw01 ordinary-column render assembly (2026-10-16)

Native extraction inspected all nine ordinary-pillar renders, separately from the crossing
mechanism. The middle column is a single centred cylinder at each LOD. Its three-level owned
render graph is now implemented in `Draw01NetPillarAssembly.CreateColumn`; see
[ordinary-column evidence and scope](Draw01-Ordinary-Pillar-Column.md).
Style commit `f5a8a64` was integrated into `dev` as `75ea203`. Release compilation passed
with the nine existing TrafficSignNeeds CS0649 warnings. Deployment backed up and verified
18 original installed files at `artifacts/backups/draw01-ordinary-column-20261016-061244`,
then verified all 12 installed payload files. No existing routed generation output changed
and no existing bridges were removed. The final generator still does not call this assembly;
complete ordinary-pillar top/footing and other mechanism/road integration remain unfinished.
No in-game acceptance was performed.


## Fixed-node attachment placement path (2026-10-16)

Native inspection confirms the crossing-node owner uses CreateSecondaryNodeObjects, not ordinary
edge repetition. Shared MovableNodeObjectPlacement now implements its candidate transform arithmetic;
[scope and evidence](Movable-Node-Attachment-Placement.md) distinguish that from still-missing node
selection/aggregation and full endpoint context. Release compilation and 12-file deployment passed;
18 original files were backed up at artifacts/backups/movable-node-object-placement-20261016-061447.
No final generator calls the new method and no existing bridge output changed or was removed.


## Native node streetlight aggregation (2026-10-16)

MovableNodeStreetLights now calls the node candidate-placement stage and preserves native duplicate
rejection, layer ordering, spacing count, four-contribution correction, distance/priority arbitration,
swap-back removal, hasStreetLights and AlwaysLit semantics. See [scope and native evidence](Movable-Node-Streetlights.md).
The final generator still lacks prepared node context and does not call this chain. Native sign/light
and utility handling remain incomplete; ordinary edge traffic needs are not equivalent.
Final Release deployment verified 18 originals and 12 payloads at
artifacts/backups/movable-node-light-state-20261016-061701; nine existing CS0649 warnings remain.
No existing bridges were removed and no visual acceptance is claimed.


## Native node sign and signal needs (2026-10-16)

MovableNodeSignNeeds now calculates native contribution masks, connected-edge target records,
node lane sign requests and vehicle/pedestrian signal-group needs. See [scope and native evidence](Movable-Node-Sign-Needs.md).
Final Release compilation passed without warnings; no suppressions were added. Deployment verified
18 original backup files and 12 payloads at artifacts/backups/movable-node-sign-filter-20261016-062013.
Final generator calls, prepared node context, utility dispatch and persistent signal initialization
remain outstanding. No existing bridges were removed and no in-game acceptance is claimed.


## Signal ownership and prefab persistence constraint (2026-10-16)

Native source inspection establishes that node-owned TrafficLight instances receive group masks
from SecondaryObjectSystem and are updated through the node's direct SubObject buffer. Persisting
them as mechanism children does not preserve those relationships. See [signal ownership evidence](Movable-Signal-Ownership.md).
The generic object binder and hidden-node strip-all policy must distinguish network-owned signal
producers from moving visual attachments before final routing. No blanket retention or custom
controller was implemented. This read-only audit changed documentation only; no bridges were removed.

## Native network signal producer retention (2026-10-16)

The shared hidden-node conversion now preserves TrafficLightObject rules, including native signal
placeholders, instead of deleting every NetPieceObjects entry. Retained entries preserve original
conditions, transforms, ordering and references. Only changed pieces/sections are cloned. The
selected-road attachment resolver excludes those retained rules, while direct misuse of its selector
raises ERROR. TrafficSignObject alone does not retain a rule because marking placeholders carry it.
See [signal ownership](Movable-Signal-Ownership.md) and [road object handling](Movable-Road-Objects.md).

Release compilation passed without warnings. Deployment verified 18 original files and 12 payloads
at `artifacts/backups/movable-native-signal-producers-20261016-062949`. All ten network adapters use
the shared conversion, but none has a final generator caller, so existing generated assets remain
unchanged and no bridges were removed. Full node/road/mechanism assembly and human signal, motion,
shipping and cold-start verification remain outstanding. This increment is not completed support.

## Node object resolution and utility arbitration (2026-10-16)

MovableNodeObjects now connects actual connected-edge contributions to node candidate placement,
streetlight arbitration, sign needs, concrete selection and MovableNodeUtilities. Utility handling
preserves the native connected-edge, isolated-node and node-lane aggregation rules. Signal candidates
participate in native nearest selection and randomization but remain network-owned. Utility emission
uses its supplied world transform rather than composing its local elevation a second time.
See [node object resolution](Movable-Node-Object-Resolution.md) for caller requirements and limits.

Release compilation passed without warnings; 18 backup originals and 12 deployed payloads were
verified at `artifacts/backups/movable-node-object-resolution-20261016-063341`. No existing bridge
output invokes these helpers and no bridges were removed. Full prepared node context and final
generator routing remain absent; this is not a completed movable bridge or in-game acceptance.

## Moving attachment lighting and elevation (2026-10-16)

The native road lighting update visits direct objects only; nested mechanism lamps do not inherit
road StreetLight state through that path. Removing network lamp producers also clears Road.IsLit.
An actual resource scan found 81 native StreetLightObject static prefabs with 110 direct effect
entries, all using forbidden LightsOff, not an independent Night requirement. See the
[attachment state audit](Movable-Attachment-State.md), which also traces Lowered elevation to collision
behavior. The present child-binding representation does not yet preserve these native relationships.
This evidence rules out simply treating the node resolver's returned lighting booleans as completed
persistence. No runtime code, assets or installed files changed during this audit.

## Native moving streetlight ownership conversion (2026-10-16)

MovableStreetLights now creates private empty lighting placeholders for native network IsLit/AlwaysLit
production, retains selected lamp geometry as bone-bound children, and transfers unchanged effect
references to the road-owned mechanism root with native bone connection IDs and StreetLightObject.
Private lamp copies remove original SpawnableObject membership to prevent effect-free replacements
from entering ordinary road placeholder selection. See [native lighting representation](Movable-Native-Lighting.md).

Release builds passed without warnings; final deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-private-lamp-membership-20261016-064228`. Existing bridge output is unchanged
because no final movable generator calls the updated binding path. No bridges were removed. Initial
and cold-start lighting state, power transitions, motion and final generator integration remain unverified.

## Native network geometry pipeline (2026-10-16)

`MovableNetworkGeometry` now binds and invokes all nine installed native GeometrySystem jobs in
their dependency order, including both node-calculation iterations, in a prepared private world.
It borrows explicit initialized terrain data, preserves native lookup access modes and disposes
temporary intersection/height buffers. See [network geometry context](Movable-Network-Geometry.md).

The native field inventory was audited and Release compilation passed without warnings. Deployment
verified 18 original files and 12 payloads at
`artifacts/backups/movable-native-network-geometry-20261016-064946`. No existing generator calls this
helper, so no existing bridges were removed. Prepared-world construction, runtime invocation and
final generator integration remain outstanding; this increment does not establish completed bridges.

## Native primary lanes and reference maintenance (2026-10-16)

`MovableNetworkLanes` invokes primary node/edge generation followed by native owner references,
skipped-connection repair and master/slave lane ordering inside a prepared private world. It
preserves native SubLane path methods instead of constructing references with default flags.
All five native job field declarations were audited. See
[network geometry context](Movable-Network-Geometry.md#primary-node-and-edge-lanes).

Release compilation passed without warnings. Deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-native-primary-lanes-20261016-065155`. No existing generator calls the new
stage; no bridges were removed. Complete context construction, overlaps, secondary lanes, final
generation routes and human acceptance remain outstanding.

## Native overlaps and secondary lane stages (2026-10-16)

`MovableNetworkLanes` now exposes full-context native overlap calculation and secondary lane creation
with native reference maintenance. Seven job declarations were audited, including the exact native
full-query mode and container ownership. See
[overlaps and secondary lanes](Movable-Network-Geometry.md#overlaps-and-secondary-lanes).

Release compilation passed without warnings; 18 originals and 12 payloads were verified at
`artifacts/backups/movable-native-secondary-lanes-20261016-065329`. No final generator calls these
stages and no existing bridges were removed. The context builder still needs intervening native
connection/signal/object initialization, complete imported data and final generator integration.
This increment does not establish completed movable bridges or in-game acceptance.

## Connected native network calculation and signals (2026-10-16)

The private network calculation now has one entry point connecting geometry, primary lanes,
references, overlaps, native signal initialization and secondary lanes. The signal job's 25 fields
and native mechanism-detection path were audited. It retains native TrafficLights/LaneSignal state;
no custom signal controller or persisted mod component was introduced. See
[signal initialization](Movable-Network-Geometry.md#signal-initialization-and-calculation-entry-point).

Release compilation passed without warnings. Deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-native-network-sequence-20261016-065457`. The caller must still construct
the complete private topology, imported prefab/composition graph and mechanism object ownership.
There is no final generator caller or in-game acceptance yet; no existing bridges were removed.

## Selected-road prefab import for the private network (2026-10-16)

`MovableNetworkPrefabs` now imports 47 explicit native component/buffer record types, remapping
prefab entity references and rebuilding archetypes in the temporary world. It preserves cyclic
graphs, original-prefab provenance and PrefabData enabled state, and excludes foreign bridge paths
before reading payloads. See [private prefab import](Movable-Network-Geometry.md#private-prefab-import).

Release compilation passed without warnings. Deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-network-prefab-import-20261016-065648`. This is temporary preparation, not
asset persistence. Complete private topology/composition construction, final generator integration
and runtime acceptance remain unfinished; no existing bridges were removed.

## Native selected-state composition construction (2026-10-16)

The private pipeline now creates node and edge compositions using the imported native archetypes,
requested state flags and native section selection. Before geometry, it initializes all pending
compositions through the installed NetCompositionSystem job, retaining native lanes, carriageways,
crosswalks, objects, areas and road/track/terrain attributes. The explicit prefab import inventory
now contains 58 records. See [composition construction](Movable-Network-Geometry.md#native-node-and-edge-composition-construction).

Release compilation passed without warnings. Deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-native-composition-context-20261016-065850`. Complete topology, mechanism
ownership, final generation routes and runtime acceptance remain unfinished. No existing bridge
output changed and no bridges were removed.

## Private native topology construction (2026-10-16)

The shared network pipeline now creates native-archetype nodes, edges and mechanism objects,
maintains endpoint/middle connectivity and initial Owner/SubNet/SubObject relationships, and runs
native NetComponentsSystem after geometry. This supplies native signal-component derivation before
lane/signal initialization. ObjectData import brings the explicit record count to 59. See
[private topology](Movable-Network-Geometry.md#private-network-topology-and-required-components).

Release compilation passed without warnings. Deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-native-network-topology-20261016-070105`. Measured style arrangements,
other required instance initialization, final generator integration and runtime acceptance remain
unfinished. No existing bridge output changed and no bridges were removed.

## Context output connected to visual and object conversion (2026-10-16)

MovableLaneVisuals can now consume actual private node/edge lane entities, their overlap-derived
render state and a common closed-pose origin. MovableRoadObjects can consume the same context's
initialized NetCompositionObject rules. Both use imported-prefab provenance and reject foreign
sources. See [context consumers](Movable-Network-Geometry.md#consuming-the-calculated-context).

Release builds passed without warnings. The final deployment verified 18 originals and 12 payloads
at `artifacts/backups/movable-context-object-rules-20261016-070314`. Existing preview behavior is
unchanged and no bridges were removed. Final arrangements, generator invocation, remaining surface
and marking conversion, structure/persistence integration and runtime acceptance remain unfinished.

## Unregistered mechanism metadata in the private context (2026-10-16)

StageMechanism can prepare an assembled generated mechanism without publishing it to the city or
resolving a live DLC prototype. Its archetype comes from its own native components; native NetObject
and MoveableBridge initialization supplies network/movement records. The caller must provide the
generated geometry and complete placement records. See
[unregistered mechanisms](Movable-Network-Geometry.md#unregistered-generated-mechanisms).

Release compilation passed without warnings. Deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-unregistered-mechanism-context-20261016-070538`. This metadata stage is not
full object initialization or a completed generator. Final arrangements, generated record production,
render/marking/structure integration, persistence and runtime acceptance remain unfinished. No existing
bridges were removed.

## Generated render metadata increment (2026-10-16)

Added private RenderPrefab metadata staging and native scalar MeshData calculation to the shared
movable network context. The calculation uses the generated bounds and surface area, preserves
native LOD/flag behavior and stages LOD dependencies without creating GPU batches or new materials.
See [network geometry](Movable-Network-Geometry.md) for source evidence, ownership and scope.

Release compilation and deployment passed: 18 original files and 12 payloads verified under
`artifacts/backups/movable-render-metadata-20261016-071524`. Existing bridge outputs are unchanged.
The helper has no final generator caller; generated mechanism initialization and all ten complete
movable generation routes remain outstanding. This increment is not completed bridge support.

## Mechanism geometry staging integration (2026-10-16)

StageMechanism now derives its own native geometry and placement metadata from the assembled
mechanism and its generated render graph. The caller no longer supplies these records. Render
staging uses the private world's native registry; the installed native object-geometry routine
writes only private render/LOD entities. Placement and movement records use native component
initializers, preserving authored offsets, flags and fees. See the network geometry record for
scope, source evidence and remaining integration work.

Release compilation and deployment passed, verifying 18 originals and 12 payloads under
`artifacts/backups/movable-native-mechanism-geometry-20261016-071803`. No existing bridges were removed.
The final style generators have not yet called this staging path; full selected-road appearance,
all style routes and runtime acceptance remain unfinished. This is not a completed feature delivery.

## Temporary network orchestration connected (2026-10-16)

Added MovableNetworkContext to execute explicit per-archetype topology through the existing native
mechanism, geometry, composition, lane and signal helpers. The same context exposes lane visuals,
object rules and composition pieces to the surface compiler. BuildingConfigurationData entity
references and theme identity are remapped into the private world; constructor failures dispose it.
See [network geometry](Movable-Network-Geometry.md) for the sequence and lifetime requirements.

Release builds passed. Final deployment verified 18 originals and 12 payloads at
`artifacts/backups/movable-context-configuration-20261016-072220`; the initial orchestration deployment
is `artifacts/backups/movable-network-context-20261016-072137`. No existing bridges were removed.

This connects helpers to a calculation entry point, not the final bridge-style router. Per-style
arrangement input, full road appearance conversion, final generator invocation and human acceptance
remain outstanding. All ten complete generators are still required by the active objective.

## Context-driven road surface compilation (2026-10-16)

Connected edge/start-node/end-node surface compilation to the temporary network geometry. Native
composition matrices and synchronization parameters now drive explicit vertex positions, normal/
tangent frames and mapping-distance UVs. The implementation retains source SurfaceAsset references
and does not replace the engine shader. Terrain-sampled vertices remain an explicit unsupported
branch rather than a flat fallback. See the network geometry record for inspected shader evidence.

Release compilation passed. Final deployment verified 18 originals and 12 payloads under
`artifacts/backups/movable-context-surface-20261016-072602`. No existing bridges were removed.
No final style generator calls this path yet. In-game validation, remaining appearance conversion,
terrain cases, measured style integration and complete persistence acceptance are still required.

## Closed-road assembly to render-level handoff (2026-10-16)

Added a context compilation entry that merges explicitly selected edge/node contributions with
correct material and provenance offsets, retains numeric contribution ranges and independent
composition/object-rule records, and compiles lane visuals against the combined receivers.
MovableRoadAssets.Level can now consume the resulting closed-road object directly. Unprojected
decals remain visible to the existing failure check; no markings are silently dropped.

Release compilation and deployment passed, verifying 18 originals and 12 payloads under
`artifacts/backups/movable-road-contribution-ranges-20261016-072904`. Existing bridges were retained.
The complete ten-style objective is still open: final style inputs/routes, remaining decal and
appearance work, and the runtime/cold-start acceptance matrix are not complete.

## Draw02 native main-mesh inspection (2026-10-16)

Completed an offline projection and horizontal-face candidate inventory for DrawBridge02 main mesh
0 at all three extracted LODs. Source OBJ hashes, triangle ordinals and measured planar extents are
retained in the chat research inventory; the user-facing report is
`Draw02-road-structure-inspection.md`, with `Draw02-native-geometry-projections.png`.

The far proxy merges top detail and has different large planar heights from LOD0/LOD1. Neither a
single height-based mask nor Draw01's removal table is valid evidence for Draw02. Material/UV
classification and logical part correspondence must precede runtime mask generation. Mesh 1,
ordinary supports and pillar/footing render graphs remain separate work. No runtime code, installed
payload or existing bridge was modified in this inspection.

A user clarification is pending on whether native planar-decal camera fading is acceptable for
converted markings. Until answered, the stricter existing appearance requirement remains in place;
no unresolved decal batch is discarded and no fidelity relaxation is assumed. Independent geometry
work continues. The full ten-style implementation objective is not complete.

## Closed-network attachment integration (2026-10-16)

`MovableNetworkContext.Objects.cs` connects the prepared network context to the existing edge and node attachment resolvers. Edge placement now receives its real composition, endpoint geometry, selected network metadata, native elevation and owner seed. Node placement uses the complete connected-edge traversal, lane-derived signs, streetlight aggregation and utility lookup. Both use `PseudoRandomSeed.kSecondaryObject`, matching native `SecondaryObjectSystem`; every context node/edge already carries that seed. Source-world theme identities remain in source-prefab selection, while private-world identities remain in traffic calculations.

Placements are translated to the same caller-supplied origin used by surface and lane extraction only after native placement and traffic-needs evaluation. Rotations, parent IDs, groups and probabilities remain unchanged. The node result retains its streetlight flags. Utility prefab resolution uses the context's original-source map and source-isolation check. No city network is created, no new runtime animation controller is added, and no asset persistence format changes.

Release compilation succeeded. Deployment backed up 18 original installation files to `artifacts/backups/movable-context-objects-20261016-074210` and verified all 12 installed payload hashes. These new context methods have no final style-generator caller yet; they do not change published fixed-bridge generation or invalidate existing bridges, so none were removed. Complete style assembly, structural replacement, moving decal fidelity and human in-game acceptance remain outstanding. Compilation and deployment are not evidence of those missing results.

## Native edge lane-object completion (2026-10-16)

The edge attachment resolver now calls `MovableEdgeLaneObjects.Visit` after composition-object placement, using the same selector and random stream. The new non-Temp construction path follows the installed `SecondaryObjectSystem.CreateSecondaryEdgeObjects`: skip secondary lanes; collect utility junctions by path node and utility type; select track start/end objects with native tangent orientation and composition surface-height offset; arbitrate utility junctions against continuing edge lanes; finally emit eligible utility objects with native anchoring. Original-prefab lookup uses the prepared context's source-isolation resolver. Private ECS identities remain temporary.

Retained network-signal candidates now execute native prefab/probability/group selection before visual emission is skipped. Previously the edge resolver returned before selection, unlike the node resolver, changing the random stream for later visual objects. Signal ownership and live lane masks remain native network responsibilities.

Compatibility review confirmed that the importer already remaps TrackLaneData, UtilityLaneData and ObjectGeometryData, and updated the only ResolvePlacements caller. The implementation intentionally covers the prepared non-Temp network, not interactive editor replacement/age state. Final moving-object suppression, partition binding and bridge-specific generator calls remain required; this change alone does not establish rail, utility or movable-bridge acceptance.

Release compilation and deployment succeeded after resolving compile-time type references. Backup `artifacts/backups/movable-edge-lane-objects-20261016-074437` contains 18 original files; all 12 installed payload hashes match the build. Failed compilation attempts did not install files. No existing published generator calls this path, so no existing bridge output was invalidated and no bridge assets were removed. No visual-generation unit tests or game automation were used. Human in-game behavior remains unverified.

## Stationary track-end producer suppression (2026-10-16)

`MovableSectionObjects` now derives private lane/piece copies where a moving-node section contains a track end object that the original closed-network pass materializes as a moving attachment. Only `TrackLane.m_EndObject` is cleared on that private lane. Track type, width, curvature, fallback lane, pathfinding and all other components remain copied; `NetLaneInfo` position and anchor flags are preserved. Signal-capable end objects remain native network producers. Unchanged lanes are reused, repeated changed lanes are deduplicated within this conversion, and the new private prefabs join the existing conversion output/rollback list. Ordinary and connecting branches still reference their original sections.

Source-isolation checks now run before reading section, piece, lane and attachment components in this conversion. Other bridge-owned sources fail explicitly. This is the external-data boundary required by the contract, not an additional internal fallback.

Utility node objects are deliberately not cleared. The native `RoadConnectionSystem` also uses `UtilityLaneData.m_NodeObjectPrefab` for building electricity, sewage and water connection objects. Treating that field as decoration alone would alter functional native connections. A complete movable utility ownership policy remains required before binding extracted utility objects or suppressing their stationary producers; the extraction helper alone does not establish that they should move.

The ten measured network converters reference this shared conversion, but no final style generator currently calls those converters. Fixed generators are unaffected and no existing bridge was removed. Both source increments built and deployed successfully. Final backup: `artifacts/backups/movable-section-object-source-check-20261016-074740`; preceding backup: `artifacts/backups/movable-track-object-suppression-20261016-074725`. Each contains 18 original installation files, with 12 installed payload files verified against the successful build. In-game duplication, connectivity, motion and cold-load behavior remain unverified; full style assembly is still incomplete.

## Native bind-pose field mapping correction (2026-10-16)

The offline MonoBehaviour extractor inferred Matrix4x4 field order from the managed DLL, which differs from the native serialized Matrix4x4f element order. Directly emitting those labels placed inverse-rest translations in the bottom row. All ten mechanism emitters and 106 generated bind-matrix initializers now transpose the field indices once while preserving every original binary32 value. The change was authored on each reserved style branch and integrated into dev.

Native render tracing also established that the normal object LOD path shares the primary SubMesh skeleton allocation; differing animation fields in native LOD records are retained, not normalized. Serialized source IDs resolve against connection IDs before runtime bone sorting, while generated vertex skin indices use original bind indices.

Verification: all 106 initializers have affine last rows and preserve original float32 payloads exactly. Release build and 12 deployed-file hashes passed; 18 prior installation files were backed up in `artifacts/backups/movable-bind-pose-field-order-20261016-080232`. No existing bridge was removed: these skeleton application methods have no final generator callers. This fixes generation data, not the still-missing ten final generator routes or human animation/cold-load acceptance.

## Native mechanism placement and road frame (2026-10-16)

The private network context now accepts the prepared mechanism's original NetSubObjectInfo,
its owner's world transform and a native traffic-signal state. It stages the mechanism first,
then applies its local placement and the native aligned Standalone-pillar top anchor using
initialized ObjectGeometryData and PlaceableObjectData. Standing-object and stack-derived
bounds are therefore included; no universal 10 m correction or raw-mesh maximum is used.
All ten audited B&P mechanisms use this Standalone placement branch.

The animation target is obtained from the game's TrafficLightSystem.UpdateMoveableBridge after
anchoring. The context retains the created mechanism entities and exposes RoadToMechanism,
which maps CompileRoad's origin-relative world coordinates into the anchored mechanism's local
frame using the inverse position and rotation. The resulting Pose can be supplied directly to
MovableRoadAssets.Level. The calculation is private construction-time data; it adds no runtime
controller, shader or persisted manager dependency.

Call-chain review found no external MechanismInput constructors or final movable generator
callers. Its sole CreateMechanism call was updated with the new input contract. Fixed generators,
existing saved assets, persistence and UI behavior are unaffected; no bridges were removed.
The final generators still need to supply measured node/edge arrangements, surface contributions,
partitions and complete render assemblies before this path can be accepted in game.

Release builds passed after both increments. The latest deployment backed up 18 original files
under `artifacts/backups/movable-anchored-road-frame-20261016-080848` and verified all 12 installed
payloads against the build output. The preceding placement increment used
`artifacts/backups/movable-native-mechanism-placement-20261016-080822`. Build and hash checks do
not establish actual road alignment, animation or cold-load acceptance.

## Native composition-state selection (2026-10-16)

Private network construction now runs the installed CompositionSelectSystem's
SelectCompositionJob and CreateCompositionJob after connections, ownership and mechanism
SubObjects have been created, before NetCompositionSystem initialization and geometry calculation.
It no longer requires callers to invent final edge/start-node/end-node flags. EdgeInput carries
an optional native Upgraded component instead. The old manual CreateComposition method was
removed after its sole caller was replaced.

The prefab importer now includes NetGeometryEdgeState, NetGeometryNodeState and FixedNetElement
buffers. Each imported geometry prefab receives an empty private NetGeometryComposition cache;
city composition entities are never copied. Native selection applies source state rules,
handedness, elevation, connected-edge node matching and mechanism NetObject flags, including
FixedNodeSize. Native creation shares matching compositions and handles obsolete upgrade flags.
The reflection runner now supplies either an EntityCommandBuffer or its ParallelWriter according
to the actual native job field type. Selection and creation receive fresh handles, and command
playback finishes before composition initialization.

The inspected installed source is Game.Net/CompositionSelectSystem.cs: SelectCompositionJob,
GetEdgeFlags, GetEdgeStates, GetNodeStates, GetNodeFlags and CreateCompositionJob. Imported state
records and FixedNetElement contain no additional entity references requiring bespoke remapping.
The change is construction-time orchestration in a disposable world; it does not patch the engine
or introduce a saved runtime dependency.

Release build and deployment passed. Latest backup:
`artifacts/backups/movable-remove-manual-composition-20261016-081050`; 18 original files backed up
and 12 installed payloads verified. The preceding selection increment used
`artifacts/backups/movable-native-composition-selection-20261016-081024`. No final movable route
yet calls the context, so no existing generated bridge is invalidated or removed. Execution with
real selected roads and full bridge acceptance remain unverified until the final generators are
connected; compilation is not evidence of runtime reflection or geometry success.

## Draw01 closed-node sampling connection (2026-10-16)

Draw01ClosedRoad now constructs and owns a disposable MovableNetworkContext. It uses two straight
144 m sampling approaches around the central node; 144 m is the recorded native segment pitch,
not an assumed moving surface length. The staged mechanism supplies its native 90 m fixed-node
offset. Source-road state selection, node/edge geometry, lanes, crossings and secondary lanes run
through the existing native private-world pipeline after the mechanism is attached.

The compiled surface contains edge 0's EndNode and edge 1's StartNode contributions. Approach edge
surfaces are excluded from the object input. Lane visuals are extracted for the central node, and
Objects() resolves its native secondary objects while the private context remains alive. The
sample stores RoadToObject from the actually anchored mechanism transform. Construction failure
disposes the private world; normal ownership ends through IDisposable. Source theme, terrain,
elevation, upgrades, handedness and random seed are explicit caller inputs. The caller must supply
the visible selected-road source before moving-network layer suppression.

The closed target uses native signal group 1 and Ongoing state, matching Draw01's first lift-offset
entry. This is sampling metadata, not a custom animation controller or city construction request.
No shipping lane is fabricated: navigation compatibility still depends on the final generated
network and native crossing behavior in the real city.

The implementation was committed on bridge/draw-01 as 2be8775 and integrated into dev as dfc5acc.
Release build and all 12 deployed-file hashes passed. Backup:
`artifacts/backups/draw01-closed-node-road-20261016-081219` (18 original installed files).
No existing bridge was removed because the final Draw01 route is still absent. The sampler has
not yet been executed in game. Measured road partitioning, decals, remaining support geometry,
final assembly/persistence and human acceptance are still required; this is not completed Draw01
or all-movable-bridge support.

## Explicit measured road gaps (2026-10-16)

The shared road partition now has an explicit immutable `Partition.Gap` leaf. A gap is
specified by the caller's measured seam tree, not inferred from road bounds, hinges or
vertex proximity. Clipping still tests only seam-local zero planes and preserves source
triangle provenance and interpolated attributes on retained polygons. Coplanar polygons
retain the existing positive-child convention, so a style must orient its boundary tree
consistently when deciding which side owns an exact boundary.

The base-road binding still rejects an entirely empty result. Mesh-based lane markings
use the same partition and may produce no surviving triangles when fully excluded; that
case creates neither an empty lane mesh nor unused lane material slots. This does not
remove the separate rejection of unprojected decal batches. Future decal receivers must
consume the retained bound road triangles, not the pre-clipping surface.

Selected-road object anchors are checked against the same gap leaves. A placement in a
gap raises an explicit error containing the object index, name and transformed position.
The binding stage does not silently remove or relocate road objects; the measured style
placement must resolve them before binding.

Compatibility review found two partition consumers: road/lane triangle binding and road
object binding. Both were updated. The shared path applies to every supplied LOD. No
per-style partition constants were introduced, and no final movable generator calls this
assembly yet. Existing fixed-style generation, persistence formats, asset identities,
materials, native animation and pricing are unchanged. No existing bridges were removed.

Release build and deployment succeeded. Eighteen original installation files were backed
up and twelve installed payload files were verified against the build. Backup directory:
`artifacts/backups/movable-measured-road-gaps-20261016-082124`.
No visual-generation unit tests were run. Actual selected-road insertion, per-style seams,
near/far motion and human game acceptance remain unverified. This is shared construction
support, not completion of any movable bridge generator.

## Native section occurrence provenance (2026-10-16)

The closed-node network path now retains the original top-level `NetSectionPrefab` for
each emitted piece occurrence. `NetCompositionPiece.m_SectionIndex` is a compact index
of selected roots that actually emitted pieces, not an index into `NetGeometrySection`.
Native `GetCompositionPieces` increments it only after a nonempty selection and reverses
root traversal for an inverted composition. Repeated references to one piece prefab can
therefore belong to different source section occurrences.

`MovableNetworkLanes.SectionRoots` replays the installed native selection helper one root
at a time in the same traversal order. The helper retains responsibility for inversion,
flip, subsection selection, requirements and alternate piece selection. The replay checks
piece identities and compact section indices against the initialized composition before
returning original root indices. It does not replace packing or regenerate the initialized
piece offsets. A mismatch is an explicit internal error, not a guessed mapping.

`MovableNetworkContext.Pieces` attaches the checked original root section reference and
an original-root occurrence path. `CompileRoad` preserves it while combining contributions;
triangle occurrence indices, contribution offsets and material indices remain independent.
The reference denotes the top-level root before subsection selection, not a claim that the
root itself supplied the final mesh. The existing straight-preview path keeps its textual
source paths and may leave the new optional root reference null. Fixed preview callers do
not consume it and retain their existing behavior.

This closes a provenance loss in the native closed-node path. It does not yet assign road
roles, choose per-style partitions, attach lane visuals to receiver occurrences or connect
final movable generators. No runtime geometric classification or section-name heuristic
was introduced. Existing materials, asset serialization and native traffic data are unchanged.
No existing bridges were removed because no final movable route uses this assembly yet.

Release build and deployment succeeded after correcting a read-only NativeArray local
initialization error. Eighteen original installation files were backed up; twelve installed
files matched the successful build. Backup:
`artifacts/backups/movable-section-provenance-20261016-082357`.
Native execution and human in-game acceptance remain unverified; no visual-generation unit
tests were run.

## Draw01 measured road-role partitions (2026-10-16)

`Draw01RoadPartitions` supplies separate carriageway and sidewalk trees in mechanism-local
coordinates. The native high-detail boundary evidence is decoded from main geometry mesh 0,
SHA-256 `f9cc32a1e42568aa26b3248ad2cdb6fee7de23d3dfeaa0b56e7714c51e0d1abe`.
The source bone array is fixed root 0, negative-Z leaf 1 (connection 100), positive-Z leaf 2
(connection 101). Bone indices and connection IDs are not interchanged.

| Surface role | Negative fixed end | Negative moving start | Negative moving end | Positive moving start | Positive moving end | Positive fixed start |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Carriageway | -83.740234375 | -83.58984375 | -0.05267906188964844 | 0.05267906188964844 | 83.58984375 | 83.740234375 |
| Sidewalk | -78.150390625 | -78.05078125 | -0.03000020980834961 | 0.03000020980834961 | 78.05078125 | 78.150390625 |

Each tree covers fixed road, an end gap, a moving leaf, the central gap, the other moving
leaf, the other end gap and fixed road, in increasing Z order. Every split transforms the
recorded seam to local Z=0. No coordinate threshold identifies source road roles at runtime.
The supplied closed-node geometry determines the finite outer extent; these trees do not
create extra approaches or derive their extent from the hinge position.

All generated road LODs use these same highest-detail logical seams. The native far proxy's
flattened sidewalk/carriageway boundaries do not reclassify road ownership. Carried structural
LOD replacement still needs its own complete measured plan. A source section/occurrence mapper
must explicitly choose the correct role; there is no default that assigns the whole road to
one tree. Mixed-role pieces and lane/attachment receiver correspondence remain unfinished.

Developed on `bridge/draw-01`, commit `7fa86fd`, and cherry-picked to `dev` as `cc5c7c9`.
Release build and deployment succeeded; eighteen original installed files were backed up and
twelve installed payload files verified. Backup:
`artifacts/backups/draw01-road-role-partitions-20261016-082609`.
The class has no final generator caller yet, so no existing bridge assets were removed.
No visual-generation unit tests were run. Runtime execution, all-LOD seams, selected-road
fidelity and human opening/closing/shipping acceptance remain unverified. This is not a
completed Draw01 generator or completion of all ten movable styles.

## Occurrence-specific partition assembly (2026-10-16)

`MovableRoadAssets.Level` now accepts separate road-piece and lane-visual occurrence
partition arrays. Both arrays are copied when the level is constructed. `TryBind` selects
the supplied measured tree using each triangle's retained Occurrence value; missing entries
or invalid trees raise explicit errors. Uniform partition callers retain their previous
behavior. No material-name, position or width heuristic chooses a road role.

Road occurrence IDs survive native surface evaluation, contribution merging, appearance
chart subdivision, texture-atlas material remapping and road-to-mechanism frame conversion.
Lane occurrence IDs are visual batch indices from MovableLaneGeometry. They are a separate
index domain, even when numeric values happen to match. A piece-partitioned road with lane
mesh triangles requires an explicit lane mapping; it cannot silently reuse the road array
or fall back to a single uniform tree. Unprojected decals still fail separately.

Draw01RenderAssembly preserves both maps when adding measured structural plans to all three
road levels. This style-specific propagation was committed on `bridge/draw-01` as `6e3e338`
and integrated into `dev` as `748b966`. Shared binding/asset changes were made on `dev`.
Fixed support levels contain no road map and use their existing path. No final movable
route calls the new assembly yet, so no existing bridges were removed.

The source-role-to-partition mapper, mixed-role pieces, lane receiver correspondence,
object role mapping, full decal conversion and final generator remain unfinished. Array
support is not evidence that a road has already been correctly classified or generated.
No native animation, prices, persistence identities or unchanged materials were modified.

Release build and deployment succeeded. Eighteen original files were backed up and twelve
installed payload files verified. Backup:
`artifacts/backups/movable-occurrence-partitions-20261016-082747`.
No visual-generation unit tests were run. Runtime and human visual/motion acceptance remain
outstanding. This increment does not establish complete support for any movable style.

## Draw01 native section role constraints (2026-10-16)

Fresh extraction of the installed resources.assets covers all 21 Draw01 root section
entries, four unique root sections and 64 decoded prefab/component objects. See
[Draw01 section role evidence](Draw01-section-role-evidence.md).

The sidewalk's Boarding Lane 0 uses ParkingLane with zero slot dimensions. Native
NetInitializeSystem marks it Virtual as well as Parking; a ParkingLane component is
not sufficient to reject the native sidewalk as physical parking. Median Intersection
Piece 0 allocates zero width but contains real geometry and no lane component. The
carriageway, sidewalk and median surface pieces share the same material reference.
Consequently lane presence, declared width and material identity cannot supply a
universal motion-role classification. Bottom/side structural layers must remain distinct
from generated selected-road surface receivers. A curb-containing piece may need an
explicit construction partition or source-triangle mapping within the occurrence.

The current importer already carries ParkingLaneData, and the inspected movable helpers
do not contain a blanket ParkingLane rejection or a per-piece zero-width filter. No such
special case was added. The next mapper must preserve these native cases and keep lane
visual batch correspondence separate from composition-piece indices. The audit changes
implementation evidence, not generator completion. Runtime code, installed payload and
existing bridges were unchanged; no build or game automation was performed.

## Preserve original lane identity in visual batches (2026-10-16)

`MovableLaneVisuals.Batch.SourceLane` now retains the original NetLaneGeometryPrefab
alongside its render prefab. Both the straight composition extraction and private network
context extraction converge on the same constructor, which supplies lane.Prefab for every
part and tile. This retains native lane-component semantics even when several lane types
share a render asset. It does not treat a lane index as a composition-piece index or infer a
motion role from a material or prefab name.

The private-context path resolves the original lane through MovableNetworkPrefabs and
checks source ownership before reading it. The new reference is transient generation
input, not an ECS entity, copied asset, persisted component or new material. Non-decal
geometry compilation and the retained decal batch list consume the same batch objects.
No native lane flags, curves, tiling, visual selection or saved identity changed.

Receiver review also confirmed that MovableDecalReceivers already inherits BoundTriangle.Bone
and preserves receiver occurrence/source-triangle provenance through page subdivision.
Therefore that stage must not be replaced by lane-batch-index lookup. This is not proof of
complete decal material conversion: native distance-fade fidelity and final asset assembly
remain unresolved. SourceLane alone also does not identify a unique road occurrence when
one lane prefab is reused in multiple sections; explicit receiver correspondence remains
required.

The shared edit was made on dev. Release build and deployment passed; 18 original installed
files were backed up and all 12 installed payload files verified. Backup:
`artifacts/backups/movable-lane-source-provenance-20261016-083511`.
The router still contains no movable final generators, so this unconnected helper change
invalidates no existing generated bridge; no bridge files were removed. No visual-generation
unit tests or game automation were performed. All ten complete generators and human
visual/motion/persistence acceptance remain outstanding.

## Preserve composition lane-to-piece correspondence (2026-10-16)

The private native network compiler now retains composition lane provenance. Native
NetCompositionHelpers.AddCompositionLanes (lines 1417-1568 of the inspected game source)
assigns ordinary lane indices while traversing composition pieces and each NetPieceLane
buffer, reversing the buffer traversal for inverted sections. It appends aggregate master
lanes afterward. NetCompositionSystem.InitializeCompositionJob copies that sequence into
the composition buffer without sorting it.

MovableNetworkLanes.LanePieces mirrors only that correspondence traversal, checking the
initialized native lane index and prefab against each source entry. It does not reproduce
lane grouping, selection, direction, placement or geometry. Appended master lanes retain
-1 because they do not belong to one source piece; unexpected entries raise ERROR rather
than receiving a guessed source.

CompileSurface retains the local map. CompileRoad offsets ordinary piece indices into the
merged road's occurrence space and stores one map per contribution in Result.LaneOccurrences.
Thus identical lane prefabs in different pieces do not collapse to one occurrence. Master
sentinels are not offset. These arrays contain no temporary-world entity references and
remain usable after disposal. The old standalone straight-edge compiler explicitly leaves
this provenance null; null is unavailable evidence, not permission to infer a mapping.

This is composition-lane provenance, not visual-batch provenance. Actual node lanes and
secondary lanes still require their native path-node/end-point correspondence to select a
contribution and composition lane. A visual batch index must never directly index these
maps. Motion-role classification, mixed road profiles and final decal conversion remain
unfinished.

Shared changes were made on dev. Release build and deployment passed. Eighteen original
installed files were backed up; twelve installed payload files were verified. Backup:
`artifacts/backups/movable-composition-lane-provenance-20261016-083700`.
The complete movable generators remain absent from the router, and no fixed generation
path calls this compiler; no existing bridge files were removed. No visual-generation unit
tests or game automation were run. Native runtime execution and human acceptance remain
unverified; this increment is not complete movable bridge support.

## Native path-node correspondence boundary (2026-10-16)

Further native source inspection established that PathNode.GetLaneIndex includes segment
bits, node connection matching can replace the selected node-composition index with an
edge lane index, and secondary lanes receive fresh secondary-marked IDs without persisted
left/right source-lane references in the inspected creation path. Therefore the new
composition provenance arrays cannot be indexed directly by arbitrary path-node or visual
batch numbers. See [native lane correspondence](Movable-native-lane-path-correspondence.md).

Projected decals already retain actual receiver bones through MovableDecalReceivers; that
path remains appropriate for attachment provenance. Non-decal secondary geometry still
needs its native construction/receiver correspondence. No guessed index mapping, code
change, deployment or bridge removal was performed in this investigation. Complete
movable generators, material fidelity and human acceptance remain unfinished.

## Five lift styles: native surface candidate coverage (2026-10-16)

Decoded binary32 positions and index buffers for mesh 0 of all five lift styles at all
three LODs. Each index buffer matches the previous native bone-group OBJ extraction.
The reproducible intermediate script is work/bridge-research/lift_surface_candidates.py;
its candidate membership is retained in lift-native-road-plane-candidates.json.
See [lift surface candidates](Lift-native-surface-candidates.md) for source hashes,
counts and bounds. This is native geometry inspection, not a visual-generation unit test.

The data rules out a universal flat-height or largest-area classifier. Lift03 retains
separate upper/lower near moving surfaces, while Lift01 far geometry lacks the large
exact-horizontal near-road candidate at the same datum. Multiple moving-bone groups also
include structural faces. The extraction deliberately does not classify extra mesh parts,
sloped faces, pillars or footings; those still require semantic review. No candidate is
accepted as an authored part or runtime removal mask merely from area, height or bone.

No runtime code, installed payload or existing bridge changed. Complete per-style
replacement plans, generator assembly, material/marking fidelity and human acceptance
remain unfinished. These measurements do not claim completion of any movable generator.

## Lift01 far inner-top identification (2026-10-16)

Actual native bone-4 cross-sections at Z=0 were inspected for all three LODs. Far mesh
triangles 342, 343, 730 and 731 form the inner top, sloping from Y=10.240234375 at the
center to Y=10.2001953125 at its ends. The preceding horizontal-candidate instrument
excluded them because their stored Y span is 0.0400390625. This resolves that instrument's
missing-road-plane observation without treating missing candidates as missing geometry.
See [Lift01 cross-section review](Lift01-native-deck-cross-section-review.md).

The far shell meets this higher proxy surface, so replacement with near-datum road geometry
requires explicitly owned transition/closure geometry rather than a four-face deletion
alone. No runtime mask or structural mutation was introduced from this partial review.
Runtime source and installed files were unchanged. All ten generators and acceptance
remain unfinished.

## Lift01 measured closure rim (2026-10-16)

The far inner-top's four triangles have six exact-position boundary edges. Each matches
one retained triangle: 334, 341, 344, 722, 728 and 732. Endpoint coordinates and distinct
source vertex indices are recorded in the Lift01 cross-section review. This supplies
measured structural endpoints for road-owned closure geometry without moving retained
vertices. It does not supply the selected-road contour or permit joining a moving deck
to a fixed approach across a gap.

Existing MovableStructureModel fragments remain on their measured source triangle, so
that path is not an off-plane road-to-shell closure builder. The new closure must belong
to generated road geometry and be integrated consistently at every LOD. This investigation
changed evidence only; no runtime code or installed payload changed. No complete generator
or human acceptance is claimed.

## PedestrianDraw02 closed-node road sampler (2026-10-16)

Added PedestrianDraw02ClosedRoad on bridge/pedestrian-draw-02 (commit 1eb12ec), integrated
into dev as d30af49. Its sampling approaches use the independently recorded 144 m native
Bridge.m_SegmentLength. The prepared PedestrianDraw02 mechanism supplies its own 76 m
NetObject node offset and native pillar anchoring; no Draw01 node extent or fixed object
height is substituted. The private native context compiles the central node's two halves,
extracts its actual lane visuals, and retains the full road-to-mechanism pose. Node objects
remain resolvable while that context is alive. Exceptions dispose the context before escaping.

This is a sampling stage, not a city-construction request or final generator route. The
caller must supply the original visible selected network before moving-span hiding and the
prepared native PedestrianDraw02 mechanism. Main render replacement, width/road partitions,
attachments, appearance, saving and final routing still need assembly; no completed style
is claimed by this class.

Also inspected all five pedestrian main-mesh LODs: PedestrianDraw01 has three, while
PedestrianDraw02 has two. Exact native binary indices match the bone-group OBJ exports.
See [pedestrian surface candidates](Pedestrian-native-surface-candidates.md). PedestrianDraw01
far moving tops occur at Y=10.10009765625 versus near/middle Y=10, while PedestrianDraw02
retains Y=10 candidates at both LODs. Those groups still need semantic/UV review and are
not accepted removal masks.

Release build and immediate deployment passed. Eighteen original installed files were
backed up, and twelve installed payload files verified. Backup:
`artifacts/backups/pedestrian02-closed-node-road-20261016-084452`.
No final route calls this new class yet, so no existing generated bridges were removed.
No visual-generation unit tests or game automation were performed. Native runtime execution
and human near/far, motion, shipping and cold-load acceptance remain unverified.

## PedestrianDraw02 nonrectangular motion interfaces (2026-10-16)

Native top-region inspection shows a fixed central tongue inside the moving wings. Its
near positive start is Z=59.3623046875, X=+-1.9114990234375, while the moving wings reach
Z=64.470703125 with inner boundaries X=+-1.9168701171875. The moving central end is
Z=59.3564453125. Therefore one longitudinal hinge cut cannot classify this road surface;
measured two-dimensional boundaries and gaps are required. See the
[PedestrianDraw02 top-region review](PedestrianDraw02-native-top-region-review.md).

Both LODs were inspected; the exact-horizontal plot intentionally excludes nonhorizontal
faces and is not a hole detector. No guessed rectangular partition was emitted. This pass
changed evidence and documentation only; complete generators and acceptance remain open.

## PedestrianDraw02 exact top boundary loops (2026-10-16)

Extracted exact-position boundary loops for fixed and both moving Y=10 candidate groups
at both main LODs. Near moving leaves each form one 32-edge loop; near fixed candidates
form two 26-edge platform loops and two separate eight-edge islands. Every boundary vertex
has degree two and no candidate edge has more than two incident faces. Ordered native
coordinates are recorded in [top boundary loops](PedestrianDraw02-top-boundary-loops.md).

This replaces extrema-only reasoning with complete candidate outlines but does not classify
excluded sloped faces or establish authored-part widening. Far horizontal-filter outlines
must not become road-hole masks. No runtime code, installed payload or bridges changed.
Complete road/structure integration and all generator acceptance remain outstanding.

## PedestrianDraw02 material/UV separation evidence (2026-10-16)

Resolved both main LODs' shared native surfaces and decoded the actual main albedo VT tiles
and UV0. The moving Y=10 candidates share one atlas rectangle, but that rectangle also
contains vertical platform/end closures. Its near selection includes 64 nonhorizontal
faces; LOD1 includes 68. Thus neither material identity nor a UV rectangle can be promoted
wholesale to a deletion mask. The main template is opaque; old top faces cannot be retained
on an assumed transparent cutout. See [material and UV review](PedestrianDraw02-material-uv-review.md).

No runtime code or installed resources changed. Full face classification, measured widening,
road-owned closures, generator assembly and human acceptance remain unfinished.

## PedestrianDraw02 complete top-face membership (2026-10-16)

The reviewed upward road-atlas membership contains 276 triangles at each main LOD:
108 fixed, 84 on the positive moving leaf and 84 on the negative moving leaf. Exact
projected XZ triangles are unique and form a bijection between LODs. The complete
boundaries also match exactly: two 26-edge fixed platform loops and one 32-edge loop
per moving leaf. There are no branch vertices or nonmanifold projected edges within
these selected regions.

This supersedes the earlier horizontal-only road coverage. Twelve LOD1 road triangles
include raised vertices at Y=10.050048828125; omitting them would create artificial
holes. Separate control-building atlas islands are not road surfaces. Shared-atlas
vertical end faces remain outside this top-face membership and require explicit
closure ownership before removal. Native height variation is preserved, not flattened.
See [complete membership](PedestrianDraw02-complete-road-membership.md).

The exact triangle IDs, source hashes and cross-LOD correspondence are retained in the
intermediate measurement. This is offline evidence, not a runtime UV/height classifier.
No runtime code or installed assets changed. Full authored-part widening, two-dimensional
road partitions, closures and final generator assembly remain unfinished.

## PedestrianDraw02 road edge adjacency (2026-10-16)

The complete top-road boundary has 116 edges at each main LOD. Ninety-six have one
exact non-road triangle neighbour: 36 extend above the road datum, while 60 extend
downward. Atlas membership splits the downward group into 44 inside the road atlas
and 16 elsewhere; neither group is automatically a deletion mask.

Of twenty edges without identical endpoint neighbours, sixteen fixed-platform edges
are fully covered by exact collinear non-road edge intervals with different subdivisions.
They are not evidence of holes. Four moving-tip side edges retain uncovered intervals
in this edge-only check; interior contact, bevels, overlap and other mesh entries still
require inspection. See [road edge adjacency](PedestrianDraw02-road-edge-adjacency.md).
No runtime or deployed payload changed. This resolves an incorrect possible closure
assumption but does not complete structural replacement or any final generator route.

## PedestrianDraw02 moving-tip face contact (2026-10-16)

Exact rational Y=10 intersections with the native constant-X side triangles resolve most
of the four remaining edge-only contact intervals. Contact occurs inside triangle faces,
not solely along matching edges. Near residual intervals are approximately 13–15 micrometres;
LOD1 residuals are approximately 0.03467 m. These are authored mesh-0 differences and must
not trigger automatic source welding or filler geometry. Other mesh entries remain outside
this contact check. Exact intervals and triangle references are retained in the extraction;
[road edge adjacency](PedestrianDraw02-road-edge-adjacency.md) now includes the results.

The shared road partition API already accepts arbitrary seam transforms and binary trees,
so the measured nonrectangular interface requires style data and assembly, not a new generic
clipping abstraction. No runtime code, deployed payload or generated bridge changed in this
measurement pass. Complete generator integration remains outstanding.

## PedestrianDraw02 measured top-road removal code (2026-10-16)

Implemented `PedestrianDraw02RoadReplacement.WithTopRemoved` on
`bridge/pedestrian-draw-02`, commit `f774985251760ccfacb10c88a34c17feb4954b98`, integrated
into `dev` as `f016804`. Both native main LODs are covered in the same source increment.
The offline emitter retains source Geometry SHA-256 identities, 276 exact triangle records
and 380 referenced native vertices per LOD. Runtime validates the single main-mesh slot,
source counts, selected triangle topology and original positions before changing the mask.
It does not classify by UV, height, material, connected components or animation bone.

The method accepts a structural plan and returns a new plan with the measured road tops
removed. It preserves supplied X deformation, other removal decisions, constructed vertices
and retained fragments. A fragment attempting to restore a fully removed road top raises an
explicit error. Glass is a separate native mesh and is outside this method's input. Road side
and end closure remain separate from top removal; no inferred filler or source welding was
introduced. Native LOD1 raised road vertices are included in the recorded membership.

This is a callable replacement stage, not a completed generator. No final route currently
calls it. Structural widening and closure plans, selected-road partitions, appearance,
attachments and final assembly remain required before publication. No fixed-style path or
persisted asset format changed, and no existing generated bridges were removed.

Release build and deployment passed. Eighteen original installed files were backed up to
`artifacts/backups/pedestrian02-road-top-removal-20261016-090126`; twelve installed payload
files matched build output hashes. No visual-generation unit tests or game automation ran.
Native execution, human near/far and motion acceptance, shipping and cold-load behavior
remain unverified. Source compilation does not establish those outcomes.

## PedestrianDraw02 footing render assembly (2026-10-16)

Implemented both footing LOD widening plans and their native render graph assembly on
`bridge/pedestrian-draw-02` (`2b2bcaa2f97273561620821b3ac490f47d889bf6`), integrated on
`dev` as `195f50e`. The centre-crossing native block's lateral boundary displacement is
recorded per vertex; the LOD1 bevel midpoint inherits the near boundary rule. Raw channels,
indices, surfaces, components and Y/Z coordinates are preserved. The existing asset factory
handles derived geometry, remapped LOD references and owned output collection. Source ownership
is checked before geometry access. See [support implementation](PedestrianDraw02-support-implementation.md).

Release build/deployment passed, with eighteen original installed files backed up and twelve
payload hashes verified. Backup: `artifacts/backups/pedestrian02-footing-render-20261016-090411`.
The final generator still does not call this assembly, so no existing bridge was removed.
Column/main mechanism mapping and the remaining road/attachment/final generation pipeline are
unfinished. No visual-generation unit tests or game automation ran; human acceptance remains open.

## PedestrianDraw02 column near/LOD1 correspondence (2026-10-16)

Inspected all three native column LODs and extracted exact-position-welded components
as an offline part-identification aid. Near and LOD1 each contain 88 components with
unique matching extent records: 16 diagonal braces, 8 horizontal beams, 8 centre joint
plates, 16 end plates, 32 end gussets and 8 pier columns. The first three roles reach
or cross the centre; side roles translate rigidly. Small centre plates require their
own span, not a brace's span. LOD1 has newly reduced vertex positions in some parts.

LOD2 consists of two merged shells (48 and 44 triangles). Its connectivity does not
supply a new widening decision. Detailed per-height spans and far-proxy correspondence
remain necessary before column code is emitted. See
[column part correspondence](PedestrianDraw02-column-part-correspondence.md).
No runtime code, installed payload or saved bridge changed in this inspection.

## PedestrianDraw02 column LOD2 surface evidence (2026-10-16)

The column far render uses a separate baked surface, unlike near/LOD1. Its preprocessed
layer texture IDs are zero; pixel data is stored in its native VTSurface. Extracted the
512-by-512 original BC7 albedo and decoded all 121 native UV0 entries, then rendered an
orthographic original-mesh diagnostic with RGB and alpha shown separately. This does
not emulate the native shader or establish generated visual acceptance. See
[far material review](PedestrianDraw02-column-far-material-review.md).

A question about permitting necessary subdivision of merged LOD triangles is pending;
no authorization has been inferred and no topology has changed. Exact per-face conflict
between carried index preservation and near-part widening still requires evidence before
any exception is applied. No runtime, installed payload or existing bridge changed.

## Native decal minimum smoothing (2026-10-16)

Investigated whether native decal renders with serialized zero smoothing permit exact
endpoint-opacity conversion without the pending distance-fade decision. Native MeshSystem
initializes smoothing to 0.001f and only accepts larger component values, so Crosswalk Line 2,
Subway Sleepers Lod and Train Sleepers Lod do not actually have zero smoothing at runtime.
ManagedBatchSystem and BatchDataSystem forward that initialized value; the current lane
batch extractor already preserves it correctly. Eight audited decal renders and their
serialized/effective values are recorded in the
[native parameter-path audit](Movable-Decal-Native-Parameter-Path.md).
This rules out a proposed shortcut before implementation. No runtime code, deployment or
bridge removal occurred; no marking was omitted and complete generator work remains open.

## Native stationary lane-visual suppression (2026-10-16)

Traced full hidden section layers through native piece flags and CompositionState.Hidden
into new node-lane and secondary-lane creation. The native systems remove rendering
components while retaining lane/curve/traffic data. This supports the existing visible
sampling followed by hidden moving-node composition, without blanket renderless lane copies.
The whole composition must be hidden; visible ordinary pieces clear the aggregate flag.
Existing-lane reuse, upgrades and reconnections remain distinct acceptance cases.
See [stationary lane-visual suppression](Movable-stationary-lane-visual-suppression.md).
No runtime source, installed payload or bridge asset changed. Full generator assembly and
marking conversion remain outstanding; this is native path evidence, not runtime acceptance.

## PedestrianDraw02 native object assembly (2026-10-16)

Implemented `PedestrianDraw02ObjectAssembly.Create` on `bridge/pedestrian-draw-02`
(commit `c93892476d755781a68676786ac69c161b1aec04`), integrated on `dev` as `8de6de9`.
The entry requires prepared main-mechanism, column and footing render graphs plus already
bound selected-road attachments. It connects the three native mesh slots in their recorded
order, applies the measured mechanism components and appends the two original rooftop vents.
The vent placements move rigidly with their fixed side control rooms; original rotations,
parent IDs, probabilities and Y/Z values remain unchanged. Foreign bridge-owned attachment
sources are rejected. Native content references and the non-circular object flag are retained.

The native `StandingObject.Initialize` copies `m_LegSize` into ObjectGeometryData and derives
leg offsets from the recorded gaps. Object assembly therefore adds the full width delta to
`m_LegSize.x`; it retains Y/Z size, zero transverse gap and the longitudinal gap of 117 m.
This updates the geometry-dependent support span, not animation timing, placement offsets,
fees or the bridge's native traffic behavior. No custom shader, controller or asset format
was introduced. The existing factory collects the object in the normal owned save plan.

This is a final-object assembly stage, not the final bridge generator. No route currently
calls it; complete render/road/attachment inputs are still required. Missing inputs do not
produce a prototype-road or mechanism-only substitute. Main/column geometry, road appearance,
partitions and native runtime acceptance remain unfinished. No existing bridges were removed.

Release build and immediate deployment passed. Eighteen original installed files were backed
up to `artifacts/backups/pedestrian02-object-assembly-20261016-091720`; twelve installed payload
files matched the build. No visual-generation unit tests or game automation ran. Human near/far,
motion, shipping and manager-independent cold-load checks remain unverified.

## Private network assembly increment (2026-10-16)

`PedestrianDraw02NetworkAssembly.Apply` now joins the measured network operations into one
post-sampling stage. It requires the prepared pillar and mechanism to belong to the current
factory, converts the selected clone's section occurrences, applies the recorded Bridge
parameters and mechanism states, and installs the three recorded native object placements.
It follows the existing selected-road prop policy: replace elevated supports while retaining
non-elevated road objects, including outside-connection objects. The mechanism placement keeps
its recorded non-elevated-only requirement.

`TowerFactory.AdoptMovableSections` includes the conversion's complete prefab result in
`TowerFactory.Created`, after checking generation ownership. The result includes private
object-producing pieces as well as section wrappers. The existing generation system collects
this list for native asset saving, dependency copying, publication and preview disposal.
No additional asset format, shader, controller or runtime component is introduced.

The style change was authored on `bridge/pedestrian-draw-02` at
`c85f6abeddcb43ff0ff6778a4e377087484f66d6` and integrated on `dev` as `ac1f8df`.
The shared factory change remains in the existing shared movable working tree.
Release compilation passed. Deployment backed up 18 original installed files to
`artifacts/backups/pedestrian02-network-assembly-20261016-092504` and verified all 12
installed payload files against the build. No generated bridges were removed: caller tracing
shows the new network assembly is not yet invoked by a final generator route and no existing
style's generation output changes.

This is an implemented assembly stage, not a completed bridge generator. All ten final movable
routes remain absent. Structure/road preparation, road appearance and marking completion,
final orchestration, and human in-game/cold-start acceptance remain required. No runtime,
visual, shipping or persistence acceptance is claimed from compilation or deployment.

## Native marking graph integration (2026-10-16)

The user approved necessary subdivision of merged structural LOD triangles and native
DefaultDecal distance fading. The approval is recorded in bridge generation contract section 5 and both
instruction scopes; rollback baseline is dev/ac1f8df28c1259bc205817ccab40dca79aaf2835.
Work is now ordered around completing Draw01 first, documenting the working method only after
that implementation is complete, and then applying it to the remaining nine styles.

MovableRoadAssets now invokes MovableDecalGraph for the compiled closed-road marking snapshot.
It samples native curved-decal inputs on already clipped, bone-bound road receivers, converts
separate albedo/normal/metal/smoothness channels into native DefaultDecal surfaces, and creates
thin native projector objects. The projector cube is newly generated geometry; original
structural mesh attributes are not passed through its encoder. Material pages use at most
128 by 128 samples, with receiver subdivision controlling spatial coverage.

The marking graph returns its receiver-derived bone identity and closed-pose placement to
TowerFactory. CreateMovableObject appends the generated children using native procedural
connection IDs and the owning mesh transform. It does not apply inverse bind matrices a
second time. Generated surfaces, textures, projector geometry, renders and objects participate
in the normal allocation rollback, owned prefab save list and preview disposal. Other road
LOD levels must share the compiled marking snapshot; each structural render retains its own
measured mesh and native LOD graph. The marking child objects use native DefaultDecal distance
fading, as explicitly approved, rather than attempting to reproduce curved-road fade exactly.

The previous unconditional rejection of nonempty decal batches was removed only after this
producer was connected. Missing surfaces, unsupported batch layouts or ambiguous bone
connections still fail through normal ERROR handling. No decals are silently discarded.
Preview object materials now receive native projector size, texture area, layer and fade
properties; DefaultDecal retains its native depth/blend state. Static bridge object receivers
use the native Other decal layer.

Release compilation and deployment passed. Latest installation backup:
artifacts/backups/movable-native-marking-preview-20261016-093406. Eighteen original files were
backed up and twelve installed payload files were hash-verified. No existing bridge was removed:
no routed fixed generator calls the movable render construction path, and the preview-only
follow-up does not change generation. No custom shader, runtime animation controller or native
engine patch was added.

This closes the unprojected-marking rejection in the shared assembly path, not Draw01's final
route or the ten-style goal. Runtime projection, normal orientation, seams, rendering cost,
near/far appearance, bone movement and cold-start persistence still require verification on a
human-created bridge. Draw01 still needs its complete prepared structural inputs and final
orchestration. No complete movable bridge is claimed by this increment.

## Approved structural subdivision support and Draw01 inspection (2026-10-16)

MovableStructureModel.Plan now accepts explicit constructed-vertex X coordinates. Previously
new vertices were restricted to interpolation of the already widened original corners, which
could not express distinct authored-part transforms inside a merged far triangle. The new input
changes only appended X coordinates; original vertices remain present and appended Y/Z remain
barycentric source values. Runtime does not classify parts or infer split planes.

MovableStructureAttributes uses the native MeshNormal.Unpack and MeshTangent.Unpack functions
for new packed normal/tangent values and re-encodes only those appended values with the native
MathUtils encoders. Original attribute bytes remain untouched. New UNorm8 colors and existing
floating-point UV channels interpolate in their source formats. Incompatible tangent handedness,
invalid directions and differing bone identities still fail rather than being invented.

Release compilation and deployment passed. Backup:
artifacts/backups/movable-lod-subdivision-attributes-20261016-093826. Eighteen installed originals
were backed up and twelve deployed payload files were hash-verified. No existing routed bridge
uses this movable structural assembly, so no existing bridge was removed. No visual-generation
unit tests were run.

Actual Draw01 mechanism-footing LOD2 geometry was inspected using the retained native position,
index, UV and albedo data. Its 400 original triangles were partitioned into 6,618 candidate
polygons along detailed authored-part bounds. The largest source-face partition contains 92
polygons. Maximum pre-deformation area discrepancy was 7.815970093361102e-14 square metres in
the offline double-precision calculation. This establishes source-face coverage only, not a
correct transform or acceptable runtime metadata.

Neither candidate is accepted. Closest-surface group transfer produces visible discontinuities
in the baked far texture after an 8 m full-width increment. Axis-projected surface selection
still has 1,369 polygons without a detailed surface ray hit and introduces visible holes in
the widened result. Untextured silhouette/area checks alone conceal these defects. No candidate
is emitted into style runtime data or connected to the generator. The next step must establish
far material/part correspondence rather than repeating a nearest-surface transfer.

Evidence in the chat workspace includes draw01_footing_subdivision.py,
draw01_footing_subdivision_texture.py, both candidate JSON records and the corresponding
Draw01-footing-subdivision images. The texture plots show native albedo/alpha without the native
shader, lighting or alpha-test evaluation; they are not in-game acceptance. Draw01's complete
creation route and the remaining nine styles remain unfinished.

## Draw01 mechanism object assembly (2026-10-16)

Draw01ObjectAssembly now combines the prepared mechanism, column and footing render graphs
with the native object components. It preserves the measured mesh slots, procedural attachment
connections, movement parameters and network node offset. StandingObject's full X support span
changes by the full road-width increment; its Y/Z extents and longitudinal gap remain native.

The measured attachment table is separated into four road streetlights and eight structural
attachments. The assembly now consumes the selected-road producer's closed-pose placements and
per-placement partitions, binding them after final root creation, without resolving or retaining
the prototype streetlights. Six structural wall lights and two
roof vents retain their original references, rotations, parent IDs, groups and probabilities.
Both control buildings are on positive X; all eight attachments follow their recorded rigid
positive-side displacement. Generated moving-marking children continue to be appended by the
shared TowerFactory path. The factory owns the resulting object for persistence and preview
disposal; no new engine code or shader is introduced.

Style commit: `bridge/draw-01/49c9e47`; integration: `dev/afa4b4d`. Release build and deployment
passed. Backup: `artifacts/backups/draw01-object-assembly-20261016-095013`; eighteen original
files and twelve deployed payload files were verified. Existing bridges were not removed:
no routed generator calls this new assembly yet. In-game behavior remains unverified.

The native footing material review also read both complete Surface records and checked the
far VTSurface layout against VTSurfaceDataHandler. Both materials use template 0 (Default),
with no serialized scalar/vector/color overrides. The default UV scale is (1, 1), offset
(0, 0). Far albedo is the first active layer, BC7 format 108, with a 512-pixel tile and
278,784 padded bytes; the recorded tile-data offset is 4,674. These findings support the
existing extraction layout, but do not constitute shader-rendered or in-game acceptance.
Evidence: `work/bridge-research/draw01_footing_material_check.py` and its JSON output in the
chat workspace.

Draw01 still lacks accepted complete footing/ordinary-pillar construction and final generator
orchestration. This increment is not a completed bridge. Finish Draw01 before documenting a
reusable proven method or expanding the remaining nine styles.

## Draw01 network assembly (2026-10-16)

Draw01NetworkAssembly applies the recorded section conversion, Bridge parameters and edge/node
states to the private selected-road clone after visible-road sampling. It adopts every converted
prefab, including private object-producing pieces, into TowerFactory's existing persistence and
preview-disposal list before assigning the converted section array.

The native placement table now exposes its three structural placements separately from its
prototype outside-connection sign. The assembly replaces ordinary elevated supports with the
two recorded pillar placements and the mechanism placement. It retains the selected road's
ground attachments and outside connections, including outside connections that also require
elevation; it does not add the prototype's sign or change the mechanism's native unrestricted
elevation flag. Existing selected-road state rules remain before the appended native rules.

Style commits: `33f698b`, `3c7a792`; dev integrations: `65bc9d0`, `139c058`. Release compilation
and deployment passed. Latest backup: `artifacts/backups/draw01-network-outside-20261016-095217`.
Eighteen original files and twelve installed payload files were verified. No existing bridge
was removed because the new assembly has no routed caller yet. This is implementation work
toward Draw01, not evidence of working in-game creation, traffic, movement or persistence.

The final router still contains only fixed-style generators. Draw01's final orchestration must
sample the visible selected road before replacing its sections, construct all prepared structure
objects, then call its object/network assemblies through the normal save or preview transaction.
The fixed BridgeGeneratorBase.Apply path leaves sections unchanged and cannot substitute for
that ordered movable pipeline. Ordinary-pillar top/footing and accepted mechanism-footing LOD
construction remain required inputs; no substitute structure or incomplete route is introduced.

## Draw01 ordinary footing construction (2026-10-16)

The ordinary network pillar's footing is now constructed at all three native LODs. Its ring
and shoulder were inspected independently of the column and the crossing mechanism's footing.
Measured height-dependent outer spans produce immutable per-vertex X coefficients; runtime
does not infer part identity or geometry thresholds. All original indices, materials and
non-position attributes remain intact. The existing owned render-graph writer preserves the
native stacking/foundation components, remaps LOD references and owns rollback/persistence.
See Draw01-Ordinary-Pillar-Column.md for measurements, geometry hashes and remaining acceptance.

Style commit `724d83d` is integrated as `dev/aa1b38e`. Release build and deployment passed;
backup: `artifacts/backups/draw01-ordinary-footing-20261016-095551`. Eighteen original files and
twelve installed payload files were verified. No existing bridge was removed, because no
routed generator uses this new assembly. Original and +8 m geometry views were inspected
offline; no visual-generation unit test or in-game acceptance is claimed.

Draw01 ordinary-pillar top construction, accepted mechanism-footing construction and complete
generator orchestration remain unfinished. The full ten-style objective remains active and
the other nine styles are deferred until Draw01 is complete.

## Draw01 mixed-road partition transport (2026-10-16)

Road binding now accepts a complete map keyed by compiled occurrence and pre-atlas triangle
ordinal, in addition to the existing whole-occurrence mapping. This is needed because a selected
piece can contain both road and sidewalk/curb surfaces with different Draw01 longitudinal seams.
The map is copied into MovableRoadAssets.Level, retained by Draw01RenderAssembly and used after
appearance subdivision and frame conversion. Missing keys fail explicitly; no geometry or
material-based role inference is added. Lane visuals continue to require their own mapping.

The ordinal is local to the compiled native piece mesh, accumulated across material slots,
not the original asset's triangle index. Mapping native source semantics through composition
still needs implementation. See Draw01-section-role-evidence.md for the distinction and the
producer/consumer correspondence review. This change supplies transport, not a completed
section mapper or generator.

Draw01 forwarding commit `48d6ead` was integrated as `dev/3aff055`; shared implementation remains
in the preserved dev working tree. Release build/deployment passed, with backup
`artifacts/backups/movable-source-triangle-partitions-20261016-095821`. Eighteen original files
and twelve installed files were verified. No existing bridge was removed. In-game creation,
animation and persistence remain unverified; no full movable bridge is claimed.

## Native source-face correspondence (2026-10-16)

BridgePreviewNetMesh can now run a separate native provenance pass for movable-road sampling.
Each source face receives three exact half-float digit bands in temporary UV.x values, with
duplicated temporary vertices. Material groups, source UV.y and all position/normal/tangent
inputs remain unchanged. The unchanged native GenerateCompositionMesh method handles winding,
half-length filtering and material grouping. The actual rendered road uses only the original
generation pass, never the temporary provenance mesh.

This avoids splitting material groups: the native implementation derives repetition rates by
accumulating UV.y and longitudinal differences across each material group's surviving triangles.
Changing that grouping to tag faces could change road appearance. The provenance pass checks
that emitted triangle counts, material grouping, positions and UV.y/z/w match the original pass
exactly before returning original piece/mesh/submesh/face identities. Unknown or ambiguous digits
raise ERROR. The temporary mesh and entity are disposed, and the original material buffer is
reacquired after structural ECS changes.

Both closed-road compilation paths now retain these identities by global occurrence and compiled
triangle ordinal, including across combined node contributions. This supplies the correspondence
needed to apply measured source-face roles; it does not supply the role measurements themselves.
Ordinary preview callers do not request the additional pass. No engine method or shader is replaced.

Release build and deployment passed; latest backup:
`artifacts/backups/movable-source-provenance-inputs-20261016-100320`. Eighteen original files and
twelve installed payload files were verified. No routed existing bridge uses this movable sampling
path, so no existing bridge was removed. Native execution of this new provenance path and its
performance still require verification; compilation does not establish correctness in game.

For the current goal only, complete the first bridge, then write a verified general method and
apply it to the remaining styles. Keep unnecessary first-bridge names and parameters out of the
method document; retain style-specific evidence separately. This is a task-specific instruction,
not a permanent project contract. The previously added contract wording was removed at the user's
request. Documentation correction baseline: branch `dev`, commit
`3aff055e1983dce46940082ca6a755606e0db91a`.

## First measured selected-road face mappings (2026-10-16)

Four native road geometries now have explicit source-face roles: CarDriveLane3,
RoadIntersectionMiddle3, RoadIntersectionMiddle2 and Sidewalk3.5. The first three use the
carriageway partition, including the zero-allocation-width median's visible geometry.
Sidewalk3.5 uses the carriageway partition for its 32 lower road-strip triangles and the
sidewalk partition for its 96 pavement/curb triangles. Its original topology already separates
these bands. Measurements and limitations are recorded in Draw01-section-role-evidence.md.

The shared table is keyed by original geometry identity and face ordinal. Draw01RoadPartitions
uses the retained native composition correspondence to build the complete mapping for a sampled
snapshot. Unknown source geometry fails explicitly; the table is not claimed to cover arbitrary
road layouts. Lane and object mappings remain separate work.

Style commit `529bb78` was integrated as `dev/800ea98`. Release build/deployment passed; backup:
`artifacts/backups/draw01-native-road-face-roles-20261016-100736`. Eighteen original files and
twelve installed payload files were verified. No existing bridge was removed because no final
movable generator route calls these stages. Complete construction and in-game acceptance remain
unfinished; the first-bridge-first execution order remains specific to the current goal.

## Selected-road LOD input assembly (2026-10-16)

Draw01ClosedRoad now prepares the three render-level inputs from one closed-road snapshot.
Every level receives the measured source-face mapping, a separate lane-batch partition array,
the road-to-object pose, and derived closed-pose render bounds (see the boundary correction below). The shared lane snapshot also keeps
native decal generation attached to the already partitioned road receivers.

Non-decal lane batches with an exclusive car or pedestrian component use the corresponding
carriageway or sidewalk seams. Track, parking, mixed-role and unidentified non-decal batches
raise an explicit error rather than inheriting the road-piece index. Decal batches contribute
no lane triangles: their partition-array slots are unused and their actual bone assignment
continues to come from the receiver geometry. This does not establish compatibility for all
custom lane layouts; native in-game extraction and motion remain unverified.

Style commit `7197716` was integrated as `dev/333c431`. Release build and deployment passed;
backup: `artifacts/backups/draw01-road-lod-inputs-20261016-101230`. Eighteen original files and
twelve installed payload files were verified. No existing bridge was removed. The complete
generator route still needs the remaining structural assembly and orchestration; this increment
does not establish a creatable bridge.

The remaining first-bridge-first instruction was also removed from the nested runtime AGENTS.md.
That sequencing requirement remains specific to this goal, not a permanent repository rule.

## Ordinary cap render assembly (2026-10-16)

The ordinary network pillar now has render builders for its cap, column and footing at all three
native LODs. The cap uses offline coefficients derived from its highest-detail outer span at
each height; it does not stretch the separate cylindrical column. Source topology, materials,
raw attributes and native stacking behavior pass through the owned structural render path.
The detailed measurements and limitations are recorded in Draw01-Ordinary-Pillar-Column.md.

Style commit `5f516a8` was integrated as `dev/06c8d8c`. Release build/deployment passed;
backup: `artifacts/backups/draw01-ordinary-cap-20261016-101450`. Eighteen original files and
twelve installed payload files were verified. No existing bridge was removed. The cap conversion
is not yet called by a complete generator; mechanism footing and final orchestration still remain.
Offline geometry inspection does not prove complete bridge creation or in-game acceptance.

## Ordinary-pillar object assembly (2026-10-16)

`Draw01NetPillarAssembly.Create` now calls the cap, centred-column and footing graph builders,
then creates the native ordinary-pillar object from their owned roots. Recorded mesh placements,
PillarObject behavior and the content prerequisite are preserved. Existing render-level native
stack/base/LOD components are reused without adding duplicates. A missing render result fails
before constructing the object; partial graphs remain visible to the enclosing preview lifetime.

The previously untracked Draw01PillarData source was preserved in
`work/bridge-research/Draw01PillarData.before-object-assembly.cs` before integrating its identical
tracked copy. Style commit `be6c4b7` was integrated as `dev/eb53420`. Release build/deployment
passed; backup: `artifacts/backups/draw01-ordinary-pillar-object-20261016-101656`. Eighteen original
files and twelve installed payload files were verified. No existing bridge was removed. The
complete bridge still requires mechanism-footing conversion and final orchestration; this
object assembly does not prove a working bridge or in-game persistence.

## Exact mechanism-footing source footprints (2026-10-16)

An offline source-face clipping diagnostic now retains exact near/far barycentric correspondence
inside problematic far triangle 287. Its central-post footprint recovers the required -1/+1
displacement coefficients, unlike the earlier whole-face nearest-surface candidate. The source
footprints overlap in depth, so this evidence identifies the next operation as explicit visibility
ownership rather than more bounding-box cuts. It is not an accepted runtime mapping or a full
far-LOD conversion. Details and reproducible evidence are in Draw01-Footing-Far-Material-Review.md.
No runtime code or installed files changed in this diagnostic increment.

## Mechanism-footing visibility partition (2026-10-16)

The face-287 diagnostic now resolves overlapping detailed footprints using affine source-depth
comparisons and explicit polygon cuts. It retains the original face area within 2.52e-11 square
metres. Of 14.3886792754 square metres, 3.9664500858 have no projected detailed surface and remain
unassigned. The +8 m visualization exposes gaps between independently displaced baked regions;
the candidate is not admitted as runtime conversion. This is one-face evidence, not proof about
the complete textured far mesh. The user declined replacing merged far parts with simplified
detailed geometry/native material references: retain the existing subdivision-only exception and
continue investigating complete-mesh coverage and native texture/alpha. No runtime or installed
files changed in this diagnostic increment.

## Far-footing moving-background and depth correspondence (2026-10-16)

The all-400-face comparison now moves previously unassigned background polygons instead of
leaving them fixed for visualization. Large end gaps diminish, but the candidate remains invalid.
An exact nearest-depth comparison removes some opposite-side source assignments: conflicts at
unassigned-region boundaries decrease from 473 to 420 under the same offline coordinate grouping.
Remaining local holes, conflicting assignments and texture displacement prevent runtime emission.
No material replacement, frozen far-width fallback or engine change has been introduced. The
reproduction and evidence are recorded in Draw01-Footing-Far-Material-Review.md.

## Same-side far correspondence (2026-10-16)

The offline correspondence now retains original source-face IDs and excludes strictly opposite-X
faces from side projections. All 400 far faces were recomputed. Boundary conflicts decrease from
420 to 402; the maximum displacement-coefficient disagreement falls from 2.0 to 0.9795764648.
The textured candidate still has local defects and remains outside runtime generation. Removing
invalid donors exposes rather than silently assigns additional unmatched area. Far widening,
original materials and the subdivision-only restriction remain mandatory.

## Closed-road environment input assembly (2026-10-16)

`MovableSamplingEnvironment` captures the native city's default theme, traffic handedness and
BuildingConfigurationData used by LaneSystem. It owns zero-filled native height arrays for a
private flat terrain input. Both height levels retain at least a 2x2 grid; native TerrainUtils
clamps outside samples, so the field remains flat across the entire sampling arrangement.
The current city's actual terrain is neither sampled into the asset recipe nor modified.

`Draw01ClosedRoad.Sample` now supplies these inputs at the recorded native 10 m water elevation,
while accepting the generation seed and optional upgrade from its caller. Native geometry/lane
job Execute methods run synchronously in the private context before the temporary terrain arrays
are disposed. The returned context retains the sampled network and supports subsequent node-object
extraction. This is input assembly around existing native calculations, not an engine replacement.

Style commit `f73c48d` was integrated as `dev/8ecd0b4`. Release build and deployment passed;
backup: `artifacts/backups/draw01-sampling-environment-20261016-104039`. Eighteen original files
and twelve installed payload files were verified. No existing bridge was removed. Native runtime
execution remains unverified; the complete generation route still requires remaining structural
mapping, motion/culling verification, attachment assembly and final orchestration.

## Final-root road attachment binding (2026-10-16)

Draw01 object assembly now creates its final root with structural attachments and generated
markings, then binds and appends selected-road objects. This removes the ordering dependency
where pre-bound road lights needed a root on which to store native effects before that root was
created. Each placement now supplies its own measured partition, distinct from road and lane
occurrence indices. No common carriageway partition is silently applied to sidewalk attachments.

TowerFactory adopts every generated lamp copy, including partial results if a later native effect
schema fails, into the ordinary save/preview graph. Root-owned native effects and existing child
placements are retained. Per-placement role resolution and in-game lighting still need validation.

Style commit `02635e5` was integrated as `dev/cee9f9b`. Release build/deployment passed; backup:
`artifacts/backups/draw01-root-attachment-binding-20261016-104338`. Eighteen original files and
twelve installed payload files were verified. No existing bridge was removed. The complete
movable generator route remains unfinished; this increment does not establish bridge creation.

## Native object producer provenance (2026-10-16)

Road-object extraction now retains the original source piece and composition-local occurrence
after native spacing overrides and streetlight merging. A private incremental native trace tags
an unused existing-entry longitudinal field, restores it, and compares every result field/order
against the primary native composition. Actual placement data and native prefab identities are
unchanged. Trace resources are scoped to the private world and always destroyed.

The surviving source rule now travels with placement candidates through placeholder resolution
and node-light arbitration into a parallel Sources array on MovableNodeObjects.Result. Utility
and lane-derived candidates remain explicitly without a composition rule; they are not silently
classified as carriageway attachments. Mixed-piece object roles still require measured mapping.

Both increments built and deployed successfully. Latest backup:
`artifacts/backups/movable-selected-object-origins-20261016-104912`; previous trace increment:
`artifacts/backups/movable-object-rule-provenance-20261016-104809`. Each verified eighteen original
files and twelve installed payload files. No existing bridge was removed. Native runtime trace
execution, final per-placement partition selection and complete generator routing remain pending.


## Native object producer rule indices (2026-10-16)

The shared object provenance replay now retains the exact original NetPieceObject rule index as well as the source piece occurrence. It replays native rules in their native order, including inversion, and verifies the restored output against whole-composition native output. Both extraction entry points and selected node-object source records retain this identity. See [road-object evidence](Movable-Road-Objects.md#exact-source-rule-indices-2026-10-16).

Release build and deployment succeeded; backup `artifacts/backups/movable-object-rule-indices-20261016-110633`, 18 originals and 12 installed payload files verified. No existing bridge was removed. The new provenance is not yet exercised by a complete movable generator. Per-placement motion mapping, unresolved far footing geometry, final routing and human acceptance remain incomplete; this increment does not claim a completed bridge.


## Selected attachment partition assembly (2026-10-16)

The first measured native road attachment rules now select the corresponding
carriageway/sidewalk motion trees. Draw01 object assembly obtains selected objects
and the road-to-object transform from the same live closed-road sample instead of
requiring externally supplied parallel placement/partition arrays. Source identities
include the original piece, rule index, placeholder and geometry CID. Unknown
producers still require explicit measurement; this does not establish all-road support.

Utility-lane node objects retain native network ownership. They are no longer also
baked into the moving mechanism, while native utility selection/group consumption
is preserved. The native producer remains active after mesh hiding and piece-object
suppression. See [attachment and utility evidence](Movable-Road-Objects.md#measured-attachment-partitions-and-native-utility-ownership-2026-10-16).

Three Release deployments passed, with 18 original files and 12 installed payload
files verified each time. Latest backup:
`artifacts/backups/movable-native-utility-ownership-20261016-111246`.
Draw01 branch commit `71ebc3a` was integrated into dev as `f0e177f`.
No existing bridge was removed. Unresolved far footing geometry, final generator
orchestration/routing, broader input mapping and human acceptance remain outstanding.


## Sampling mechanism before final render assembly (2026-10-16)

`Draw01ClosedRoad.Sample` now creates an owned, temporary `Draw01SamplingMechanism`
from the recorded main, column and footing render roots. This removes the entry
point's requirement for the final road-bearing mechanism before the selected road
has been sampled. The temporary root uses `Draw01MechanismData` for native NetObject,
MoveableBridge, PillarObject and StandingObject configuration. Its standing width
receives the same full requested width increment.

The temporary render graphs contain copied native scalar metadata and components,
including private remapped LOD references. Their bounds retain native Y/Z stacking
reach and adjust the X envelope by the requested half-width delta. They contain no
geometry or surface assets and are not renderable output. The private
`MovableNetworkPrefabs.StageRender` path projects their scalar MeshData;
`ObjectInitializeSystem.InitializePrefab(ObjectGeometryPrefab, ...)` consumes that
metadata to calculate object geometry and stacking. This does not run a native mesh
loading/rendering system or create a city network.

The temporary Prefabs are marked HideAndDontSave, never added to TowerFactory's
owned output or the asset writer, and retained until the private network world is
disposed. Failure during construction destroys allocated temporary components and
Prefabs. Successful sampling transfers their lifetime to Draw01ClosedRoad; disposal
first releases the private world, then those temporary objects. Original source
components, LOD arrays and assets are not modified. Foreign bridge-owned render
sources are rejected before their metadata is copied.

The recorded roots were confirmed in the native audit as DrawBridge01 Mesh,
DrawBridge01Pillar Mesh and DrawBridge01PillarBase Mesh. The main root and LOD1
native bounds have Y = [0, 16.399999618530273] m; they describe the closed geometry,
not a full opening envelope. The native standing/stack path described below supplies additional
object bounds. Complete movement and culling still require in-game verification; temporary
sampling bounds must not be presented as that verification.

Style commit `a935e44` on bridge/draw-01 was integrated into dev as `dabb03a`.
Release build/deployment passed. Backup:
`artifacts/backups/draw01-sampling-metadata-20261016-111825`;
18 original files and 12 installed payload files verified. No existing bridge was
removed. The sampling entry point has no final generator caller yet and has not
been exercised in game. Far footing mapping, motion/culling verification, complete routing
and human acceptance remain mandatory outstanding work.


## Native stacking and generated render bounds (2026-10-16)

The previous `MotionBounds` input conflated local RenderPrefab metadata with final object
culling. The field is now `RenderBounds`; it still writes the native `RenderPrefab.bounds`.
It is not a replacement animation envelope or a custom culling implementation.

Native source evidence in the extracted game code:

- `Game.Prefabs/ObjectInitializeSystem.cs`, StackOrder.Last branch: for an upward standing
  object, the initializer raises the working mesh maximum Y to at least `m_LegSize.y + 0.1f`
  before calculating the last stack range. The recorded native leg size is 80 m vertically.
- The same initializer's `UpdateStackBounds` records native start/end overlaps and stack ranges.
- `Game.Objects/SubObjectSystem.cs` sets instance stack ranges from those prefab ranges and
  elevation. `Game.Objects/ObjectUtils.cs` combines stack range and object geometry in
  `CalculateBounds`; the 16.4 m closed render height is not the final culling height.
- `Game.Rendering/PreCullingSystem.cs` and `Game.Objects/SearchSystem.cs` use the stacked
  `CalculateBounds` path when Stack and StackData are present.
- `Game.Simulation/TrafficLightSystem.cs`, `UpdateMoveableBridge`, selects the native target
  height from `m_LiftOffsets`. `Game.Rendering/ObjectInterpolateSystem.cs` handles the recorded
  LookAtAimForward bones. None of these native systems or shaders is replaced.

`Draw01ClosedRoad.PrepareRenderLevels` now takes a width delta instead of unexplained external
motion boxes. Each of the three LOD inputs starts with the source render bounds widened on X,
then includes the evaluated selected-road and non-decal lane vertices in mechanism coordinates.
`Draw01RenderAssembly` additionally includes all transformed structural plan positions, covering
small differences introduced by native compressed geometry. These operations compute extents of
known output geometry; they do not classify parts or infer widening rules. Authored stacking and
standing components remain intact. Static supports retain their existing authored-bound path.

Compatibility inspection found only one reader outside the shared render builder, in the same
style's render assembly; it was updated with the field rename. The final generator still does not
call the preparation method. No existing routed style calls this path, so no existing bridge was
removed. The style change is commit `6210cef`, integrated into dev as `c30fbac`; shared render
infrastructure remains part of the existing dev working tree.

Release build and deployment passed with backup
`artifacts/backups/movable-render-bounds-20261016-113309`: 18 original files and 12 installed
payloads verified. No visual unit tests were run. Actual private-world sampling, generated output,
opening/closing visibility, save/reload and human near/far acceptance remain unverified. This
increment removes an incorrect bounds-input requirement; it does not complete the bridge.

The shared builder additionally expands each procedural render's bounds from the final emitted
road and lane triangles after atlas clipping and bone partitioning, plus the generated structural
plan positions. This avoids treating pre-conversion extrema as exact bounds of newly interpolated
floating-point vertices. It computes the output extent instead of adding a tolerance or suppressing
a containment error. Static support bounds are unchanged. A follow-up Release deployment passed:
`artifacts/backups/movable-emitted-bounds-20261016-113449`, with 18 backed-up originals and
12 installed payloads verified. This was a code-path correction; no generated in-game sample has
yet demonstrated its culling behavior.

## Shared mechanism placement (2026-10-16)

Private closed-road sampling now obtains the mechanism's NetSubObjectInfo from
`Draw01NetworkData.MechanismPlacement`, the same measured constructor used by the persisted
network's `StructurePlacements`. The Sample entry point no longer asks its caller to supply a
second placement record. Position, rotation, placement kind, index, spacing and all anchoring/
requirement flags retain their recorded native values. Each call returns a new placement record;
the temporary sampling object and final generated object retain separate references and lifetimes.

The source call-chain review still finds no final generator caller for Sample, and the router still
contains only the 20 fixed-style routes. This change removes one duplicate input from the pending
integration; it does not connect or complete a movable style. The unresolved mechanism footing
mapping still prevents complete assembly. No UI availability or failure handling changed.

Style commit `be3f36e` was integrated into dev as `80eb141`. Release build/deployment passed with
backup `artifacts/backups/draw01-shared-placement-20261016-114255`: 18 original files and 12
installed payloads verified. No existing bridge was removed because no routed generator uses
this stage. Actual sampling, creation, motion and independent persistence remain unverified.

## Generator integration and user-authorized far-footing deferral (2026-10-16)

Draw01 now has a connected generator route through selected-road sampling, mechanism/support
render construction, native object assembly and network conversion, returning to the existing
preview and persistence paths. The far-footing defect is deferred by the user's latest instruction;
its retained vertex-coefficient mapping preserves native topology and Y/Z but is not accepted
geometry. Other movable styles are paused. See [Draw01 generator integration](Draw01-Generator-Integration.md)
for the exact temporary behavior, deployment evidence and pending human verification. Release build
passed; 18 originals backed up and 12 installed payload files hash-verified. No Draw01 UUID exists
in the current export state, so no existing bridge was removed. Connected code is not yet a verified
in-game creation, animation or persistence result.

## Draw01 node-surface preview repair (2026-10-16)

The first new human preview reached native closed-road sampling, then failed because an unused
provenance calculation read the edge-only NetCompositionLane buffer from a node composition.
Removed the unused LanePieces/LaneOccurrences data chain; native SubLane visual extraction,
marking compilation and source-face correspondence remain active. Release build and deployment
passed, with 18 originals backed up and 12 installed files hash-verified. No existing Draw01
UUID was present for cleanup. See [integration and repair evidence](Draw01-Generator-Integration.md).
A new in-game preview is pending; this is not complete movable-bridge acceptance.

## Small Road / Draw01 surface selection repair (2026-10-16)

The latest preview advanced to surface partitioning but incorrectly included ordinary Bottom/Side
structure in the road bake. Draw01 now explicitly excludes those replacement layers while retaining
surface geometry and native lane extraction. Also added measured ordinary Sidewalk Piece 3.5 sign
and streetlight producer mappings, whose indices differ from NoTram. Release build and final deployment
passed; 18 originals backed up and 12 payload files verified. See
[the repair evidence](Draw01-Generator-Integration.md). In-game retest remains pending.

## Draw01 underground lane visual separation (2026-10-16)

Small Road reached lane partition selection but its underground water-pipe mesh had entered the
visible moving-road batch. Native resource and MeshSystem/RequiredBatchesSystem inspection confirmed
Pipeline/SubPipeline rendering. The shared extractor now omits those render parts from the moving
road bake while retaining native network utilities. Names and geometry thresholds are not used.
Release build and deployment passed; 18 originals backed up and 12 installed files verified.
See [integration repair evidence](Draw01-Generator-Integration.md). Human retest remains pending.

## Draw01 deterioration-decal repair (2026-10-16)

The latest failure reached road appearance assembly and rejected native template 19
(CarLane / CurvedDecalDeterioration). Fresh-lane zero-wear conversion is now admitted
from native shader evidence. Road-page matrix calculations are hoisted out of the
pixel loop and decal receiver pages use the requested density instead of allocating
maximum-size pages for every triangle. See [Draw01 integration](Draw01-Generator-Integration.md)
for evidence and limits. Release build and 12-file deployment verification passed;
preview timing and complete in-game creation remain unverified. Other styles remain paused.

## GPU conversion and lamp placement update (2026-10-16)

Road/decal pixel evaluation and normal/channel encoding now dispatch GPU compute kernels.
The compute bundle is generation-only and embedded in the manager DLL; saved assets still use
native materials. Readback, native compression and serialization remain synchronous. The user
approved native longitudinal mounting stations for selected-road lamps, resolving the reported
central-gap placement without changing structural Z or bypassing the gap check. See
[Draw01 integration](Draw01-Generator-Integration.md) for implementation and verification limits.
Draw01 has not yet passed human creation/animation/persistence acceptance; other styles remain paused.

## Performance and footprint revision (2026-10-16)

The subsequent optimization replaces the earlier synchronous, per-LOD/per-triangle bake path.
Identical road appearances now share resources across LODs; same-bone coplanar marking faces
share tiled pages. Sources remain on GPU, half encoding and constant reduction run on GPU,
and bounded readback queues overlap evaluation with native saving. Constant textures are
shared within the bridge, and road atlases group actual source surfaces and crop unused space.
Native asset mutation remains thread-affine. No LOD or structural Z modification is introduced.

See [performance and size evidence](Movable-Bridge-Performance.md) for the measured old 132.134 s
bake total, implementation details, final build/deployment verification and the unresolved
complete dependency-size comparison. A successful pre-change preview was observed in the log;
new-build timing, full permanent creation and size acceptance still need a human-created sample.
Bridge generation contract section 24 now requires comparable total size including dependencies.


## Native marking resource reuse (2026-10-16)

All extracted moving marking batches now use original texture references and native segmented
object decals. The user approved curve approximation and endpoint-fade differences. The old
marking-page bake is removed from the generation graph; road-surface baking is unchanged.
See [native marking reuse](Movable-Native-Marking-Reuse.md) for native evidence, persistence,
deployment and outstanding human acceptance. No new runtime speed or visual result is claimed.

## Fixed platform and generation-cost increment (2026-10-17)

Implemented opaque fixed-platform material regions at every generated road LOD, grouped native
paint volumes and generation-stage timings. See [evidence and pending acceptance](Movable-Fixed-Platforms-And-Generation-Cost.md).

## Prefab graph compaction (2026-10-17)

Removed per-occurrence mechanism condition wrappers and retained native root-entry conditions and shared section references. Together with the preceding marking grouping, the recorded input is expected to decrease from 523 to 76 owned Prefabs; a new saved sample is required to measure the result. Structural render hierarchy is preserved, but native decal children remain an explicit difference from the prototype. See [graph evidence and limits](Movable-Prefab-Graph-Compaction.md).


## 2026-10-17: road paint inlay and native fixed platforms

Independent marking object creation has been removed. All LODs now partition road receiver triangles into painted and unpainted surfaces using existing native bridge materials. Fixed sidewalk platforms retain their native geometry and UVs. See [repair evidence and remaining in-game verification](Movable-Road-Paint-And-Platforms.md). Release build and 12-file deployment checks passed; visual acceptance remains pending.


The fixed carriageway cap now uses measured native concrete coverage. Converted road UVs now follow prototype object-space scale, orientation and sidewalk origins, rather than the selected network shader UV convention. See the follow-up section in `Movable-Road-Paint-And-Platforms.md`. Build and deployment passed; new in-game verification remains pending.


The 10:42 projection regression is repaired by triangle-derived tangent frames with shared handedness. The source frame is retained where planar UV projection has zero area. Geometry validation remains enabled. Build/deployment passed; game replay is pending. See `Movable-Road-Paint-And-Platforms.md`.


Prototype UV inspection now separates left/right carriageway orientation and positive/negative leaf longitudinal mirrors. Commit `15cd2ff` is deployed. This remains measured-region projection, not exact per-face native UV duplication; see the explicit limits in `Movable-Road-Paint-And-Platforms.md`.


## 2026-10-17: persistence reader compatibility and original asset recovery

The user confirms normal bridge appearance. Startup retirement was caused by the text index rejecting native typed anonymous Vector3/Quaternion entries in ProceduralAnimationProperties, not an invalid write format. The reader is fixed, complete saved-graph layout checks pass, and the original 202 files for the affected UUID have been restored unchanged. See [persistence evidence](Movable-Persistence-Odin-Compatibility.md). Cold-start game confirmation is pending.


## Network and unlock persistence audit

All 12 private sections, 28 pieces and 5 unlock groups in the restored sample were reviewed. Network conversion retains unchanged references and copies only changed producers and their parent sections. Equivalent native unlock groups now share an owned node, and the outer gate is embedded directly on each network. See [per-file audit and verification limits](Movable-Network-Unlock-Persistence-Audit.md). Existing saved files were not rewritten.
