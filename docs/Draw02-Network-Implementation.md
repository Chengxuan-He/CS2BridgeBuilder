# Draw02 network integration data

## Catalogue and generator road-width agreement

The built-in DrawBridge02 catalogue entry and generator now share the measured 19 m
road-surface width. The retained audit identifies RoadPrefab 118590, four active
carriageway occurrences using piece 120675 (3 m each), and two sidewalk occurrences
using piece 120789 (3.5 m each). The two side pieces 79442 contribute 0.6 m each
outside this surface allocation. Each of the nine lateral occurrences has three
mutually exclusive network branches; those branches must not be summed as extra width.
The former catalogue value of 61 m rounded the tripled 20.2 m full composition.

`BridgeMeasurements.Draw02RoadWidth` supplies both the catalogue record and
`Draw02BridgeGenerator.NativeRoadWidth`. This removes divergent width inputs for
catalogue clearance/ranking and generator sizing. It does not change the generator's
previous 19 m sizing, serialized assets, structural tower measurement, or unmeasured
BXP variants. The review traced `BridgeStyleVariant`, `BridgeStyle.Selection.ExtraFor`,
catalogue logging and both shared generator bases. No localized text changes are needed.
This is source-width validation, not the separate generated-asset width allowance audit.

The DrawBridge02 network record from the installed game 1.6.2f1 is implemented in `Draw02NetworkData.g.cs`. Its audit SHA-256 is recorded in that source. All 27 section entries were compared as nine independent three-branch occurrences before emitting the rules. This does not generalize the mapping to other movable styles.

`ConvertSections` preserves each selected-road occurrence through the shared subsection conversion. `ApplyBridge` reproduces Draw02 segment, water-clearance and construction arrangement fields. `AddMechanismStates` appends the measured edge/node states to a private selected-road clone. `Placements` requires the generated Draw02 pillar and mechanism, plus the selected road outside connection, and retains the four authored placements.

Unlike Draw01, the two Draw02 pillar placements use native Y=-0.9449999928474426, with Z=2.5 and Z=0 respectively. No Draw01 dimensions are substituted. All rotations, placement modes, flags and state-match values come from the Draw02 record.

The component does not yet have a generator caller. Structure generation, selected-road moving appearance/markings, complete route integration, persistence acceptance and human motion/traffic checks remain outstanding. Existing outputs are not changed by this uncalled addition, so no bridge cleanup is required.

Development is isolated on `bridge/draw-02`. The existing shared `MovableSectionConversion` file is supplied unchanged from the dev worktree for branch compilation; it is not a Draw02-owned implementation and is not included in the style commit. No unrelated working-tree edits are moved or committed.


## Hardcoded render inputs (2026-10-19)

`Draw02SourceRenders` reconstructs nine private render inputs (mechanism, column and footing,
three LODs each) without resolving or deserializing the original prototype RenderPrefabs.
It applies `Draw02RenderData` and `Draw02MechanismData.ApplySkeleton` to fresh instances and
loads only the recorded built-in Geometry/Surface resources. `Draw02RoadSampling.Create` now
requires this owned input set. Keep the input set alive through geometry preparation; dispose
it after consumers finish. Disposal destroys only its private Prefabs/components, not resource assets.

Evidence: `all-movable-audit.json`, game 1.6.2f1, SHA-256
`47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070`.
Select exact RenderPrefab names DrawBridge02, DrawBridge02Pillar and DrawBridge02PillarBase,
with ` Mesh`, `_LOD1 Mesh` and `_LOD2 Mesh` suffixes. Preserve bounds, surface area, counts,
ordered Surface references, impostor and manual-VT flags. Convert each asset reference's four
signed GuidPart words to little-endian bytes and emit each byte low nibble before high nibble
for the Colossal CID string. Reverse decoding independently matched all recorded CID words;
bounds, counts and area also matched every selected audit entry exactly.

This is prototype-Prefab independence for this input stage, not missing-DLC independence.
Recorded native geometry/material resources, native default base and content prerequisite remain
required. The complete Draw02 generator/route is still unfinished, so no in-game execution or
cold-load acceptance is claimed. No existing generated assets were removed.
Release compilation and localization checks passed; twelve deployed payload hashes matched the
build. Original installed files were backed up under
`artifacts/backups/draw02-recorded-sources-20261019-103704`.


## Mechanism support assembly (2026-10-19)

`Draw02SupportAssembly.Create` now builds the stacked mechanism column or footing through the
existing owned movable-render writer, preserving original Surface references and authored vertical
stacking bounds. It consumes `Draw02SourceRenders` and the immutable displacement tables in
`Draw02SupportWideningData.g.cs`. Native object assembly and the concrete generator caller remain
unfinished; this helper alone does not enable Draw02 creation.

The source column consists of a front transverse wall, a ribbed rear wall, side returns and the
negative-X control-room projection. The front clear span ends at X = +/-14.75; the rear centre
rib face spans +/-0.75, with the other ribs carried on their respective sides. Footing transverse
interiors meet side returns at +/-15. These are Draw02 measurements, not Draw01 constants.
The offline metaprogram also identifies side-facing coarse wall triangles and propagates rigid
side displacement to coincident corner vertices, preventing cracks between duplicated vertices.
Runtime contains only indexed coefficients; it performs no spatial classification.

All six original meshes retain their topology and packed attributes. No support subdivisions were
introduced in this increment. Source triangles are 404/363/300 for the column and 586/526/200 for
the footing. The generated data records each Geometry SHA-256. Reproduce with
`tools/GeometryMetaprogram/Draw02Supports.py`, passing the native audit research directory, output
C# path and optionally the installed game's PipelinePlugin.dll path.

Offline checks cover zero-displacement identity, unchanged Y/Z, coincident-vertex displacement and
full-span width growth for half-width changes -0.5, 0, 2 and 8 metres. Native and +16 m full-width
plots were inspected at all three LODs. This does not certify arbitrary narrowing, rendered material
behavior, native stacking, motion or save/load. In particular, narrowing beyond the measured central
rib span requires further geometry review; these checks must not be described as all-road acceptance.

## Selected-road appearance preparation (2026-10-19)

`Draw02RoadSampling.PrepareRenderLevels` connects the three independently sampled road LODs
to the measured Draw02 motion partitions and `Draw02RoadAppearance` projections through
`MovableRoadPreparation`. Section provenance selects carriageway versus sidewalk; lane geometry
retains its own occurrence domain. Bounds include each corresponding road and lane LOD.
Paint is intentionally excluded from the lane batches for later original-bone skinned assembly.

The moving carriageway texture unit combines the measured grating panel and adjacent solid strips
using GPU texture bands, without adding receiver triangles for texture repetition. Negative fixed
carriageway UV direction is independently recorded rather than mirrored from the positive leaf.
Original structural Surface references are not changed by this preparation stage.

Compilation and all twelve localization checks passed. The complete mechanism structure, object
assembly and generator route remain unfinished; this entry point alone does not enable creation.
Per the updated user objective, human visual/motion acceptance will be performed together after
the bridge generation batch is implemented, rather than gating individual implementation stages.

## Native object assembly (2026-10-19)

`Draw02ObjectAssembly` assembles the prepared mechanism, column and footing with recorded native
components. The two vents and one wall light follow the negative-X control room rigidly; their
native parent, rotation and Y/Z coordinates remain unchanged. Ordinary prototype streetlights are
not dependencies of this assembly. Selected-road lamp producers supply their replacements at the
recorded Z stations -60, -20, 20 and 60 metres through `MovableRoadLights`. Draw01 uses the same
placement algorithm with its own recorded station inputs. Non-lamp objects retain sampled placement.

Road objects bind after the final mechanism exists so native effects belong to the correct root.
Paint uses the existing single-sided skinned writer and Draw02's original road-bone partitions.
The full generator still needs structural mesh preparation, network supports and route integration.
Release compilation, twelve-language checks and installed payload hashes passed; no generated
bridge or visual acceptance is implied by these checks.

## Network assembly and source lifetime (2026-10-19)

`Draw02NetworkAssembly` now applies the recorded section branches, bridge parameters, mechanism
states and three structural placements to the private selected-road clone. It preserves the selected
road's outside connections and ground attachments. `StructurePlacements` is separated from the
full recorded placement evidence so this path need not resolve a prototype outside-connection object.
Road-specific barrier adaptation and complete generator integration remain outstanding.

The shared movable generator now scopes render inputs through a disposable `RenderSources` owner.
Borrowed Draw01 inputs have no owner and are not destroyed. Reconstructed style inputs can retain
their private source Prefabs until sampling, render preparation and assembly finish, then dispose
them on success or failure. Generated assets retain their separate factory ownership.

Offline Draw02 connectivity inspection found 544/437/7 exact-position welded components at
LOD0/LOD1/LOD2. The far moving leaf and fixed platform merge several authored parts, so these
components cannot be used as whole-part widening groups. Structural replacement still requires
measured subdivision or permitted high-detail-derived geometry; no global width scaling is enabled.
Compilation, localization and deployment hash verification passed for this increment.

## Shared recorded-input reconstruction (2026-10-19)

`Draw02SourceRenders` now uses `MovableSourceRenders` for native-resource resolution and temporary
Prefab ownership. The same offline emitter supplies Draw03, Lift01–05 and PedestrianDraw01–02:
79 render descriptors in nine style input graphs. Run `tools/GeometryMetaprogram/MovableRenderSources.py`
with the recorded audit JSON and repository root. It verifies the audit digest, CID word/nibble
round trips and each part's LOD count against its existing measured render configuration.

Runtime reconstruction consumes hardcoded bounds, counts, surface area, ordered Geometry/Surface
CIDs and the style's recorded render/skeleton configuration. It does not load prototype Prefabs;
built-in resource prerequisites still apply. Disposal destroys only private temporary Prefabs and
components. These input classes are preparation for the unfinished generators, not completed routes.
Release compilation, all twelve localization checks and installed payload hash comparisons passed.

## Recorded road-top removal (2026-10-19)

`Draw02RoadTops.Remove` composes exact recorded triangle removals with a structural plan, retaining
its transformed positions, fragments and constructed vertices. Each record verifies all three original
vertex indices before removal. It removes 1696/1521/44 upward-facing road-top triangles at
LOD0/LOD1/LOD2, leaving 22968/13245/504 other main-mesh triangles unchanged by this stage.
Reproduce with `tools/GeometryMetaprogram/Draw02RoadTops.py` and the native research directory.
The offline emitter checks horizontal orientation and upward winding on actual source geometry.
Runtime has no height classifier. Side closures, original lane-divider removal, underside treatment
and structural widening still require their own measured plans before connecting this to a generator.

Inspection of the six largest near components confirms that topological connectivity joins the
longitudinal framework and base, and also joins fixed control-room/pier/platform pieces. Whole-component
stretching is therefore insufficient even at high detail. The remaining transformation work must
separate these authored roles. Compilation, localization and deployed payload checks passed for
the removal stage; it has not generated or visually validated a bridge.

## Transverse member mapping (2026-10-19)

`Draw02TransverseMembers` records the 18 isolated transverse braces and 18 directly-under-road
base strips in each of LOD0 and LOD1. The offline emitter identifies the native stations at
Z = +/-4, 12, 20, 28, 36, 44, 52, 60 and 68 metres, then emits indexed Float32 coefficients.
Braces stretch about the centre using each highest-detail member's own measured half-span.
Base strips use X + sign(X) * delta. LOD1 retains those LOD0 spans even where simplification
removed a 0.05 m outer cap; it does not renormalize the truncated member.

The maps cover 5488/3352 original vertices. Offline checks cover disjoint membership, all 36
expected roles, finite results, unchanged Y/Z, zero-delta identity and rigid base-half displacement
at half-width deltas -0.5, 0, 2 and 8 metres. This remains a partial structural map: longitudinal
frames, hinges, fixed structures and the merged far shell are not covered by this helper.
No LOD2 no-op or guessed coarse mapping is supplied. Build/localization/deployment checks passed;
complete generator routing and batch human acceptance remain outstanding.

## Isolated lateral members (2026-10-19)

`Draw02RigidParts` contains indexed rigid-side mappings for wholly lateral, exact-position welded
native components: 492/385/1 components and 15774/8258/60 vertices at LOD0/LOD1/LOD2.
The offline classifier runs on the original topology before any road-top removal, avoiding artificial
part separation caused by replacement masks. Mixed and crossing components are not assigned by
this map. The sole isolated far component is the fixed negative-X control room.

The separate near/middle glass mesh has 68 vertices and 34 triangles, entirely on that control-room
side; its complete native channels/topology remain intact while positions translate by -delta.
`Draw02RigidParts.py` checks source glass membership and per-part shape preservation, finite
translations, zero-delta identity and unchanged Y/Z at deltas -0.5, 0, 2 and 8. It writes a review
record listing each native component's vertices and bounds in the research directory.
These partial maps still need the mixed longitudinal/fixed/far structure maps before integration.
Release compilation, localization checks and deployment hash verification passed.

## Moving shell replacement fragments (2026-10-19)

`Draw02MovingShell` records source-triangle fragments for replacing the road-bearing upper skin
of the two primary moving shell assemblies. Native cross-section inspection at Z=40 confirms that
the near/middle shell includes inner curbs above the road, while the far proxy raises the entire
road to Y=10.5283203125. Top-only removal does not remove the proxy's residual side closures.

The offline recipe cuts the near/middle shell above the measured base top Y=9.699951171875.
Their outer railings are separate native components. For LOD2 it first preserves the outer strips
at X=+/-9.64990234375, including faces coplanar with those boundaries, then removes the raised
interior above the same base top. Original positions/channels remain untouched; recorded edge
interpolation supplies retained fragments through `MovableStructureModel`.

LOD0/LOD1/LOD2 replace 4332/3530/40 source faces, retaining fragments from 588/580/16 of them
with 880/876/24 edge vertices. Offline checks conserve source triangle area between the removed
and retained polygons. Runtime consumes only indexed tables, never these spatial tests.
This stage still requires the complete position transforms, fixed-shell replacement and original
lane-divider removal before generator integration. Build/localization/deployment checks passed;
batch visual acceptance remains separate.

## Constructed shell boundary displacement (2026-10-19)

The retained boundary vertices use measured side coefficients at all three LODs. The emitter
checks that all 880/876/24 construction points lie on the native base-top plane. Runtime
reconstructs their original X from the recorded source edge/barycentric coordinates, then adds
the recorded coefficient times the half-width delta through `Plan.constructedX`. It does not
interpolate widened endpoint positions to determine their X or classify geometry at runtime.

This distinction matters at the under-road end closures: LOD0's inner girders are connected
through the closure at Z=78.5..78.69921875, Y=9.330810546875..9.699951171875. The closure uses
the base sign mapping while each side girder translates rigidly. The centre-reaching faces
do not make the combined topology a single member to stretch. Interpolating endpoint sign
weights instead of applying the construction point's recorded coefficient misplaces 8/8/6
boundary vertices at LOD0/LOD1/LOD2; at delta=8 the largest errors are 6.29344/6.29344/5.05757 m.

Offline checks cover finite coordinates, unchanged Y/Z, zero-delta identity and exact side
displacement at deltas -0.5, 0, 2 and 8. Release compilation and all twelve localization checks
pass. This corrects the boundary stage only; the full Draw02 structural map and generator route
remain unfinished, and no generated-bridge visual acceptance is claimed.

The shell plan now composes the isolated lateral map, near/middle transverse map and recorded
primary-body side map. Primary-body mapping covers the retained lateral frames, inner girders
and base closures; near/middle additionally include the separate 2556+2556 / 2542+2544 vertex
under-road base assemblies. The far native proxy combines these roles in its primary shell.
Centreline positions remain centred. Source and constructed positions follow the same measured
side displacement. No runtime topology or nonzero spatial classification is used.

The transverse tables now also include both native hinge axles: 720 vertices each at LOD0,
296 each at LOD1, stretched using their common measured highest-detail half-span 10.656005859375 m.
These axles are not assigned the base sign mapping. The three composed source maps are disjoint
and cover 39454/27264/405 original mapped vertices. Fixed platform/control-room compounds,
original lane-divider removal and remaining structural members still need completion before routing.
Emitter geometry invariants, Release compilation and all twelve localization checks pass.

## Prototype road separation (2026-10-19)

The composed shell plan now removes the original lane separators at X=-3, 0 and 3 on each
leaf: six measured components per near/middle LOD, with 500/320 vertices per component and
1512/960 triangles in total. The far proxy contains no separate separator components. This
removal affects prototype road layout only; structural rails and independently sampled selected-road
geometry are not selected by this table. Identification is offline; runtime uses recorded face indices.

The plan also invokes `Draw02RoadTops.Remove`, covering 176/134/20 additional source road-top
faces outside the primary moving-shell removal. Offline composition checks confirm that none of
these road-top faces is restored by a retained shell fragment. Shell and separator removal totals
are now 5844/4490/40 original triangles before the additional road-top removal. Constructed edge
counts remain 880/876/24; removing separators adds no geometry or receiver cuts.

Release compilation and the twelve-language localization check pass. The fixed compound/platform
width maps and full generator integration remain outstanding; this is not a complete Draw02 route.

## Fixed-body width mapping (2026-10-19)

`Draw02FixedBodies.py` records the fixed under-road bodies and their joined lateral structures
from the original main meshes. Reviewed projections distinguish the continuous under-road body
from the separate axle and central linkage. Near/middle topology joins the fixed platform and
side structures; the far model has separate platform and lower-body groups. The map covers
3216/2685/452 vertices in 2/2/4 native groups. It retains 72/64/28 centre stations, including
near/middle stations encoded at +0.000001 m, instead of interpreting that residue as a positive side.

`Draw02MovingShell.Create` composes this indexed map with the existing lateral/transverse/body
maps. The maps are pairwise disjoint. Offline checks preserve Y/Z, centre stations, finite results,
zero-width identity and each side's rigid displacement at deltas -0.5, 0, 2 and 8. After road and
separator removals, 596/314/0 retained non-centre original vertices remain outside these maps.
The near/middle remainder contains the central linkages, bottom planes and transition lips.
Zero far-map omissions is coverage evidence only, not proof of all-LOD appearance or acceptance.
Fixed road-side closure replacement and selected-sidewalk adaptation still require completion.

## Complete indexed coverage and render assembly (2026-10-19)

The remaining transition lips use the base sign mapping. Bottom planes and central linkages
use their own measured highest-detail spans. The near/middle transverse tables now describe
44 members and 7548/4286 original vertices. The central linkage half-span is only
0.200000762939453125 m. The mandated own-span transform degenerates at the corresponding
negative delta, so shell construction explicitly rejects deltas at or below
-0.200000762939453125 m instead of publishing collapsed/inverted linkage geometry. This is
a measured narrowing limitation, not a claim of unrestricted selected-road width support.

Run `tools/GeometryMetaprogram/Draw02StructuralAudit.py` with the research directory. It rebuilds
the independent maps in a temporary directory, checks pairwise disjointness, composes road-top
removal and retained fragments, and verifies complete retained non-centre vertex coverage.
The resulting structure has 19524/11014/512 triangles before adding selected-road meshes.
At half-width deltas -0.1, 0, 2 and 8, every originally nondegenerate retained triangle remains
nondegenerate with consistent winding. Y/Z and zero-delta identity are preserved. These checks
do not prove sidewalk adaptation, appearance, motion or persistence.

`Draw02RenderAssembly` now combines prepared road levels with these structure plans and the
recorded control-room glass. `MovableRenderAssembly` holds the reusable raw-mesh lifetime,
bounds expansion and level assembly; the Draw02 wrapper supplies its 2/2/1 native mesh counts
and measured plans. Original source surface slots, skeleton, lane partitions, road transforms
and optional supplements remain intact. This render stage is not yet connected to a complete
generator: fixed road closure/sidewalk adaptation, approach network pieces and the ordinary
network pillar still require implementation before routing Draw02.

## Ordinary network pillar assembly (2026-10-19)

`Draw02PillarSources` reconstructs the nine measured native render inputs without requiring
the original pillar Prefab. `Draw02PillarAssembly` builds three owned LOD graphs and the native
pillar object. Top and column halves translate rigidly; the continuous footing stretches using
its measured highest-detail half-span of 10 m at every LOD. No triangles are cut or removed.
Original Surface references, raw geometry channels, content prerequisites, stack orders and
the footing's 0.4 m overlap are preserved. Authored vertical/depth bounds remain unchanged.
Generated LOD references are remapped before the temporary source graph is disposed.

| Part | LOD0 triangles | LOD1 triangles | LOD2 triangles |
| --- | ---: | ---: | ---: |
| Top | 184 | 164 | 150 |
| Column | 160 | 144 | 150 |
| Footing | 112 | 100 | 100 |

The geometry emitter verifies identity at zero delta, unchanged Y/Z, side rigidity or footing
scale, finite vertices and winding at half-width deltas -0.5, 0, 2 and 8 m. Regenerated source
matches the checked-in map. Release compilation and the twelve-language localization checks
pass. This completes the ordinary pillar assembly stage, not the full Draw02 generator route;
fixed approaches, road-side closures and sidewalk adaptation remain outstanding. Native visual,
motion and cold-load acceptance remains scheduled for the completed batch.

## Recorded approach resource graph and section layout (2026-10-19)

`Draw02ApproachSources.py` emits the nine native approach piece families (inner run/node,
outer run/node, edge run/node, intersection, side run/node) and their 25 render inputs.
The intersection has one level; the other families have three. Source geometry/material CIDs,
network dimensions, height ranges, offsets, surface heights, native costs, asymmetry flags,
content prerequisites and LOD links are recorded from the checked audit. Temporary input
construction now supports `NetPiecePrefab` through the same owned lifetime and built-in-only
resource resolution as other reconstructed renders; ordinary render callers are unchanged.

`Draw02ApproachLayout` assembles five structural-only sections from supplied factory-owned
pieces, preserving the native piece conditions and offsets. Its 27 occurrences retain the
nine lateral roles and their ordinary/fixed-node/half-length branches, inversion, median,
flip and hidden-layer flags. The structural overlay additionally requires elevation. Prototype
traffic and sidewalk pieces are excluded: selected-road conversion remains responsible for them.
The overlay replaces rather than appends to the selected road's structural overlay.

Release compilation and twelve-language checks pass. These recorded sources/layout are not yet
called by a complete Draw02 generator. Approach geometry widening still needs its own measured
part maps before passing generated pieces to the layout. Main-road sidewalk/closure adaptation
and final generator routing also remain unfinished; this stage is not visual acceptance.

## Fixed approach widening and assembly (2026-10-19)

All 25 native approach Geometry payloads were extracted from the recorded game package and
checked against their audit hashes. Decoded near/far cross-sections show two inner 3 m strips,
two outer 3 m strips, two 3.5 m edge strips and two 0.6 m side strips. The central intersection
piece has X stations -1/0/1 m. Runtime part selection uses emitted indexed tables, not bounds
or connectivity classification.

`Draw02ApproachGeometry` widens the inner run/node strips while retaining their centre seam.
Their local centre shifts by half the requested half-width delta in network placement, so the
local indexed map subtracts half delta at the measured -1.5 m seam and adds half delta elsewhere.
Combined with the section cursor, the centre remains fixed and non-centre stations move by
the full half-width delta. Outer/edge/side geometry remains unchanged locally and moves through
the section cursor. The intersection uses its recorded centre/side groups. Original triangle
counts, Y/Z, channels and material slots remain unchanged at every LOD.

`Draw02ApproachAssembly.Apply` creates nine owned native network-piece graphs, changes only
the inner strip widths, and applies the recorded structural section layout. Private source
graphs are disposed only after native LOD references have been remapped. The enclosing factory
owns partial generated graphs if a later family fails.

Offline checks cover all 25 inputs at half-width deltas -0.1, 0, 2 and 8 m: zero-delta identity,
finite vertices, unchanged Y/Z, centre alignment, unchanged outer geometry and triangle winding.
Near/far assembled cross-sections at zero and 8 m were visually reviewed offline. Build and
twelve-language checks pass. Main mechanism sidewalk/closure adaptation and concrete generator
routing remain outstanding; native rendering, motion and cold-load acceptance is not claimed.

## Selected sidewalk appearance frames (2026-10-19)

`MovableSelectedRoadLayout` derives each sidewalk occurrence and its lateral span from the
evaluated selected road in mechanism coordinates. Draw02 sampling now shares that layout with
physical/appearance partition selection and the material patch recipe. The left and right
sidewalk UV origins use their respective selected outer edges; the measured prototype gradient
0.3448246832512522 per metre remains unchanged. This preserves density while accommodating
asymmetric sidewalk widths instead of assuming both outer edges are at +/- (9.5 + delta).
The original symmetric layout yields the previous UV origins within numerical precision.
An absent sidewalk has no corresponding evaluated surface and does not acquire replacement
geometry from this layout class.

Release compilation and localization checks pass. This is appearance-frame adaptation only;
remaining prototype slab side closures and sidewalk-related railing geometry still require
measured adaptation before the complete generator is routed.

## Native barrier producer integration (2026-10-19)

The recorded Draw02 sidewalk section (118589) references `Sidewalk Piece 3.5 - NoTram`
(120789); its `NetPieceObjects` component (72428) uses `BasculeBridgeBarrier01 Placeholder`
(78485) for LevelCrossing. This is the same producer established for Draw01, not a guessed
Draw02-specific placeholder. `Draw02NetworkAssembly` now invokes shared `MovableBarrierRules`
before adopting converted sections. Ordinary traffic-sign producers on hidden fixed mechanism
nodes acquire the native crossing barrier while retaining their non-crossing branch and all
placement fields. Existing barrier producers remain unchanged. Newly copied producer paths
enter the factory-owned persistence graph; no runtime controller or mechanism child is added.
The shared helper is the former Draw01 implementation with only its class/location changed.

The structural investigation also confirms that the independent near/middle long parts around
X=+/-3 m are the already removed lane separators, not sidewalk rails. Outer railing components
remain separate in those LODs; the far shell preserves its recorded outer strips. Fixed-platform
side faces still need their measured closure treatment. Build and localization checks pass;
barrier behavior in a generated Draw02 remains part of the later native batch acceptance.

## Integrated generator route (2026-10-19)

`Draw02BridgeGenerator` now inherits `MovableBridgeGeneratorBase<MovableStraightSample>` and
is registered under the existing Draw02 style ID. The existing localized catalogue/preview/
creation/management paths use that same route. The native cross-section is four 3 m road
strips plus two 3.5 m sidewalks (19 m); outer 0.6 m structure strips are excluded from the
selected-road width delta. Sampling retains the measured 10 m closed-pose elevation.

The generator owns reconstructed Draw02 source renders through the shared lifecycle. It
assembles prepared road/material levels, measured main/glass plans, stacked mechanism supports,
native skinned paint and lamps, the ordinary network pillar, widened fixed approaches, and
native network/barrier rules. Preview and persistence share these stages. Source Prefabs are
disposed after assembly; generated assets remain in the factory's owned graph. The former
source-wrapper parameters were changed to explicit roots/LOD arrays to fit this lifecycle.

This is a connected implementation increment, not a completion or visual acceptance claim.
Fixed-platform side closures and selected sidewalk geometry remain under investigation. The
central linkage still rejects half-width deltas <= -0.200000762939453125 m; that narrowing
limitation must not be reported as general road-width support. Build and localization checks
pass; execution, native rendering/motion and cold-load checks remain unverified until the
requested batch acceptance. Other mechanism generators remain outstanding.

## Fixed-platform curb replacement (2026-10-19)

Offline near/middle views identify the raised fixed-platform curb risers and shoulder bevels
between the 9.800049 m road datum and 10 m sidewalk. `Draw02FixedCurbs.py` records their
64/40 triangle identities and original indices, balanced between the two fixed ends. Runtime
removes only those recorded faces after shell/top replacement; no height/bounds classifier runs
in the generator. Lower structural lips and supports remain intact. The far proxy has no separate
raised curb in this band and is unchanged by this operation.

The composed structural audit now includes curb removal, verifies it is disjoint from removed
road tops and retained fragments, and checks all three LODs for mapping coverage and winding.
The main structural output has 19460/10974/512 triangles before selected-road and paint additions.
Release and twelve-language checks pass. This addresses duplicate original fixed curb surfaces;
it does not establish completion of lower platform closure, sidewalk undersides or the documented
narrowing limitation. Native batch acceptance remains pending.

## Retain fixed exterior platform tops (2026-10-19)

The previous top-removal table selected entire upward planes. The fixed service platforms
outside the measured road corridor share the sidewalk top plane, so that table also removed
48 LOD0 and 40 LOD1 exterior platform triangles. These platforms are part of the fixed
structure, outside X = +/-9.5 m; selected-road conversion cannot replace them. This is distinct
from the unresolved adaptation of sidewalk slabs inside the road corridor.

`Draw02RoadTops.py` now retains those original faces. The native far proxy combines road and
exterior platform tops: eight exterior triangles remain whole, and twelve merged triangles
produce sixteen retained exterior fragments. Their edge construction and original face
ownership are recorded offline. Runtime appends those measured edge records to the existing
shell plan and translates the platform boundary by the same signed half-width delta as its
fixed structure. No runtime clipping against the prototype, new material, bone change or
selected-road topology change is introduced. Original channels remain raw; constructed edge
channels use the existing source-triangle interpolation path.

The composed audit now consumes the actual top-removal records and platform fragments. It
checks that exterior faces survive, fragments remain outside the road corridor with upward
winding, and all retained vertices have their expected width mapping. The structural totals
before converted road and paint are now 19508 / 11014 / 536 triangles for LOD0 / LOD1 / LOD2.
Half-width deltas -0.1, 0, 2 and 8 m pass finite-position, unchanged-Y/Z and face-orientation
checks. Offline top views at native width and +8 m per side were inspected for all three LODs.

Release compilation passes. This change affects Draw02 preview and new generation through
their shared mechanism assembly; it does not rewrite already saved bridge geometry, alter
persistence/migration rules or change other styles. No user-visible text changed. Full fixed
road/sidewalk closure adaptation, narrowing and native batch acceptance remain outstanding.

## Selected-layout fixed sidewalk nose slabs (2026-10-19)

`Draw02FixedSidewalks.py` identifies four independent sidewalk nose slabs at the two fixed
hinge ends. The near mesh has 44 slab faces and the middle mesh has 40, including the top
faces already replaced by selected-road geometry and the small bevels already removed by
the curb stage. Their source vertices are exclusive to these faces: no foundation, exterior
service platform or unrelated structure shares an indexed vertex with them.

The emitted `Draw02FixedSidewalks` plan now uses the selected left/right sidewalk spans
independently. The original 3.5 m section allocations are [-9.5,-6] and [6,9.5]; the measured
slab inset is retained proportionally within each selected allocation. Source Y/Z, UVs, raw
channels and bone ownership stay unchanged. The native allocation gives exact position
identity. A missing sidewalk removes only that side's slab faces. Already removed tops and
bevels remain removed, so this stage does not reintroduce duplicate road surfaces.

The native far LOD has no independent nose slabs. Its previously corrected common foundation
and exterior platform representation remains intact; no detailed near slab is inserted into
the far mesh. This is scoped to these measured sidewalk pieces, not a claim that all fixed
road closures or all sidewalk-dependent structures have been completed.

Sampling/material preparation and structure assembly now receive the same evaluated layout
instance from `Draw02BridgeGenerator`. Preview and formal generation use this common path.
The composed audit covers native, absent, left-only, right-only and unequal sidewalk spans,
checks that absent slab vertices are not referenced by output faces, and verifies unchanged
unrelated vertices, Y/Z and winding. No-sidewalk structural counts are 19480/10986/536;
one-sided counts are 19494/11000/536, before road/paint additions. The prototype-layout counts
remain 19508/11014/536. Offline native/left-only/unequal plots were inspected and Release
compilation passes. No user-facing text, persistence layout, source resources or saved assets
are changed. Native rendering and motion acceptance remain part of the deferred batch review.

## Selected-sidewalk runtime branch verification (2026-10-20)

`tools/MovableSidewalkCheck` links the production `MovableSelectedRoadLayout` and
`Draw02FixedSidewalks` classes. Native mesh fixtures and exclusive slab face sets
come from `Draw02SidewalkCheckInputs.py`, independently of the emitted C# table.
Thirty-six cases cover three LODs, six sidewalk layouts and both forward/reversed
road frames with nonzero translation. Cases include absent, left-only, right-only,
unequal and narrow sidewalks. Curb and similarly prefixed non-family sections are
excluded. Exact removal masks, unrelated structure, selected slab allocation,
source-plan immutability, indices and Y/Z are checked.

All cases pass. This establishes the existing fixed nose slab adaptation branches;
it does not establish all sidewalk-dependent approach/railing geometry, native
rendering or whole-bridge acceptance. The lightweight harness substitutes geometry
containers and only identity/180-degree rotations; it does not run Unity or the
native mesh assembler. No production changes were required by these results.

Reproduce with the native research fixtures:

```powershell
python tools/GeometryMetaprogram/Draw02SidewalkCheckInputs.py <research-directory>
dotnet run --project tools/MovableSidewalkCheck -c Release -- <research-directory>
```

Only technical verification output and English documentation were added. Existing
user-facing localization is unchanged.

## Approach structural allocation review (2026-10-20)

The nine reconstructed approach families are not nine sidewalk surfaces. The
measured BottomInner/BottomOuter/BottomEdge variants (families 0..6, nineteen LOD
meshes) lie below the selected road: maximum Y is -0.3000030517578125 m or lower.
The 3.5 m BottomEdge allocation is load-bearing understructure, not an authored
walkable slab. Families 7/8 are the 0.6 m exterior side/safety structure, with native
Y=-0.9499969482421875..1.5 m. Their occurrences sit outside the complete road
allocation in the recorded overhead section layout. Neither classification alone
authorizes deletion when an elevated road has no Sidewalk section.

Source evidence: Draw02ApproachSources, Draw02ApproachLayout, decoded meshes
79423..79445 in draw02-approaches, and the existing approach cross-section review.
The selected road surface comes from evaluated selected sections with native
Bottom/Side layers excluded; structural overhead pieces are added separately.
No removal of these load-bearing or outer safety families was introduced. This
review does not establish every mechanism railing or fixed composite surface as
sidewalk-independent; those require their own measured part evidence.
