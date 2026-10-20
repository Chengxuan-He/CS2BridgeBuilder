# Lift04 mechanism render configuration

The generated render-data class configures fresh private render graphs for the mechanism,
column and footing. It preserves each native level's StackProperties, BaseProperties,
LOD references and content prerequisite. The caller supplies generated geometry and applies
the separately recorded procedural skeleton to each main-render level. Do not apply this
configuration to shared native renders or to a render already carrying copied components.

| Part | Native render records in LOD order |
| --- | --- |
| Mechanism | 81026, 81027, 81028 |
| Column | 81029, 81030, 81031 |
| Footing | 81032, 81033, 81034 |

The names Column and Footing identify measured object mesh slots; they are not runtime name
matching or permission to infer geometry. Footing is distinct from the road-adjacent deck base.

Source audit: game 1.6.2f1, SHA-256 `47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070`.
The extractor follows actual LOD references and checks the complete component list. No runtime
archetype lookup, width inference or new material is introduced.

This class has no generator caller yet. It changes no existing bridge output. Compilation
does not establish geometry, native animation, shipping or cold-start acceptance.

## Measured side components and glass (2026-10-20)

Lift04RigidParts records disconnected side-part translations for all three native
levels: 922/851/130 parts, covering 36,732/26,051/1,576 vertices. Crossing and
merged components still require separate authored-part plans; these maps do not
select sidewalks or remove road faces.

Lift04Glass records the four native mesh-1 panes at LOD0 and LOD1 (16 vertices,
eight triangles each). Both retain fixed bone 0 and original triangle ordering.
LOD1 stores one vertex of its first pane at index 15, so its side map is explicitly
recorded rather than inferred from four-vertex ranges. Source Geometry hashes,
matching pane geometry across levels, side membership and rigid triangle edges
are checked offline at four width deltas. Runtime consumes only immutable index
maps; no original prototype Prefab lookup or geometry classification is added.
There is no glass mesh at LOD2. Existing structural Surface references remain
unchanged. These helpers are not yet connected to a Lift04 generator and do not
establish rendering, animation or persistence acceptance. No user-visible text or
localized asset names are changed.

## Three-level columns and footings (2026-10-20)

Lift04Supports.py checks all six source Geometry hashes and emits index-specific
column transforms and the measured footing span. Each column mesh contains two
longitudinal supports. Each support has two rounded side lobes joined across the
centre at X=+/-2.60003662109375. All cross-centre triangles use only those inner
boundary coordinates. Translating each side preserves every non-crossing triangle
and the curved column cross-sections; the native connector triangles extend without
new cuts. The separately authored footings span X=+/-12.162353515625 and stretch
about the centre. Neither object is substituted for the mechanism's deck base.

LOD0/1/2 columns contain 576/530/176 vertices and 512/460/128 triangles; footings
contain 1088/1022/136 vertices and 848/762/116 triangles. All six meshes pass
four-width checks (-1, 0, 2, 8m half-width delta), unchanged Y/Z and positive
triangle winding. Column side triangle edges remain unchanged. Native and widened
near/far plan views were inspected. More severe narrowing remains compatibility
work; the connector's positive-span requirement is explicit.

Lift04SupportAssembly reads only the reconstructed recorded renders and sends all
three levels through CreateMovableRenderGraph, preserving native surfaces, stack
metadata and Y/Z bounds. There is still no Lift04 generator caller. No healthy
saved assets are changed, no runtime prototype lookup is introduced, and no new
user-facing text requires translation. Native lifecycle acceptance remains deferred.

## Recorded transverse beam families (2026-10-20)

Lift04Crossbeams.py identifies three distinct authored families in the native
high-detail mesh: 30 lower beams with half-span 3.9650269m, 12 tower beams with
half-span 5.1018066m and 13 upper beams with half-span 5.2906494m. LOD1 retains
all three families with reduced topology; LOD2 retains the 12 tower and 13 upper
beams and omits the lower beams. Correspondence includes height and longitudinal
position, since tower members at different heights share the same Z bounds.

The generated immutable index ranges cover 1760/1463/400 vertices across the three
levels. Each member stretches by its own span; original bone ownership, Y/Z,
indices and material slots are retained. These records are disjoint from the
side-component translations. The emitter checks source hashes, complete component
face ownership, high-detail/LOD bone correspondence and four width deltas.
Native and widened front views were inspected. This helper does not yet complete
the remaining merged frames, deck bases, road replacement or generator integration.
No user-facing text changes are introduced.

## Moving deck base (2026-10-20)

Lift04DeckBase.py identifies native components 97/96/85 at LOD0/1/2 by audited
membership and matching bounds X=+/-5, Y=7.503418..9.800049,
Z=+/-79.84961. Every face belongs to original moving bone 2. The generated
immutable sign ranges cover 6592/4903/112 vertices and preserve the contract's
base mapping x + sign(x)*delta. This is the road-adjacent structural base, not the
separate column or footing. Exact centre vertices remain fixed; small nonzero
native coordinates are retained rather than snapped to a guessed centre plane.

Source hashes, closed component membership, original bone ownership and winding
are checked at delta 0, 0.5, 2 and 8m. Y/Z and all excluded vertices remain unchanged.
Native and widened front profiles were reviewed for all three levels. Unrestricted
narrowing is not established by these widening cases. Original road-top faces are
still present in this stage; the later measured replacement mask must remove only
the intended road surface, retaining undersides and sidewalls. No marking cuts,
new material copies or user-visible strings are introduced. The helper remains
unconnected until the remaining Lift04 structure and road plans are assembled.

## Detailed road-bed replacement and motion partition (2026-10-20)

Lift04RoadTops.py decodes the source binary Geometry and records 168 moving and
32 fixed road-bed top triangles per detailed level. The moving faces retain bone 2
and cover X=+/-4, Z=+/-79.849609375; fixed faces retain bone 0 and cover the two
80..96m end spans. Their projected areas are checked against those dimensions.
LOD1 triangles 17291, 17292 and 17294 contain a native simplification vertex at
Y=9.75927734375 instead of 9.71875; they are explicitly included rather than lost
by an equal-height test. Other shoulders, sides and undersides are excluded.
Runtime verifies each recorded triangle's original indices before removal.

Lift04RoadPartitions records the matching original motion domain and the 0.150390625m
joint gap at each end. It is distinct from the network's 168m segment length:
the mechanism's authored node offset is 96m. Road geometry must retain this native
mesh-space domain and original bone 2 instead of being assigned to a counterweight.
LOD2 combines the road and shoulders at Y=9.800048828125, so this detailed mask
intentionally rejects LOD2; its measured shell reconstruction remains unfinished.
These helpers are not yet wired into a complete generator. No user-facing text,
material references, persisted identities or existing saved bridges are changed.

## Counterweights and central housings (2026-10-20)

Lift04Machinery.py records two counterweights spanning X=+/-5.272217 on original
bone 1 and two central upper housings spanning X=+/-1.2853088 on fixed bone 0.
These are separate from the moving road's bone 2. Each complete crossing part
stretches about its own span, preserving Y/Z and all original bone attributes.
The far mesh retains both simplified counterweights and omits the central housings.
Recorded membership covers 528/527/48 vertices for LOD0/1/2. Source hashes,
component closure, LOD correspondence, exact bone ownership and four width cases
pass; native and widened front views were inspected.

The currently emitted rigid, crossbeam, deck-base and machinery maps are disjoint.
They cover 45612/32944/2136 of the main mesh's 60636/45780/3212 vertices. The
remaining 15024/12836/1076 vertices include joined frames, fixed road bodies and
upper shells; this count is an implementation inventory, not completion percentage
or a claim of accepted geometry. Glass and separate support meshes have their own
maps. No user-facing strings or existing generated assets change in this stage.

## Joined fixed tower frames (2026-10-20)

Lift04TowerFrames.py records components (1,622)/(1,583)/(5,155) in LOD0/1/2.
They contain 2500/2369/178 vertices, all on fixed bone 0. The connected source
contains side columns plus bottom transverse connectors. Exactly 96/96/32
triangles cross X=0; every crossing triangle uses only X=+/-5.1018066 endpoints.
Therefore translating the two indexed sides preserves every non-crossing triangle
edge while extending the bottom connectors. It does not scale the column profiles.
No vertex interpolation, triangle cuts or replacement material is required.

The emitter checks source hashes, component closure, bone ownership, crossing
endpoint coordinates, unchanged side triangle edges and winding at delta -1, 0,
2 and 8m. Native and widened front views were inspected for all three levels.
These maps remain separate from upper shells and road-adjacent bases. The original
LOD topology and all Y/Z values remain unchanged. The complete Lift04 generator
is still unfinished; no saved asset or user-facing localized text changes here.

## Tower headers and lower connecting sheets (2026-10-20)

Lift04Connectors.py records the two tower headers with native half-span 7.264282
and the three detailed lower sheet components with half-span 3.9650269. The tower
headers remain on bone 0 at every level. The long lower sheet belongs to moving
bone 2, while both short end sheets belong to bone 0. LOD2 omits the lower sheets
in the original source and retains simplified headers. Original surfaces, sheet
indices and bone ownership are preserved; these are not replacement road tops.

The index tables cover 544/520/24 vertices. Source hashes, complete face ownership,
LOD bounds, bone identities and four width cases pass. Native/widened plan views
were reviewed. Together with the side, crossbeam, deck-base, machinery and tower
maps, these cover 48656/35833/2338 vertices without overlapping assignments.
Remaining merged frames, fixed road shells and upper shells still need their own
plans before generator integration. No new UI text or persisted identities are
introduced, and no existing saved bridge is modified.

## Column capitals and upper shells (2026-10-20)

Lift04ColumnCapitals.py records the two column capitals, components
(42,618)/(42,579)/(2,152), with 1688/1515/240 vertices. Their rounded side profiles
translate with the columns; the centre remains fixed and connecting faces extend.
The near/middle source has explicit centre fan vertices while the far source
bridges the centre directly. The generator preserves both native constructions.
These capitals are separate from the road-adjacent base and separate foundation.

Lift04UpperShells.py records components (43,623)/(43,584)/(3,153), containing
576/548/96 vertices. Their side edge profiles translate, central fan vertices stay
fixed, and the transverse shell panels extend. Every vertex in both maps retains
fixed bone 0. No Y/Z coordinate, index, material or vertex attribute is replaced.

Both emitters validate source hashes, complete component face ownership, exact
crossing endpoint sets and all noncentral triangle edge vectors at delta -1, 0,
2 and 8m. Winding checks pass. Native/widened capital plans and shell front views
were inspected at all three levels. No user-visible text changes are introduced.
These remain intermediate structure helpers until the complete Lift04 render and
road pipeline is connected; compilation does not establish native acceptance.

## Moving side and end frames (2026-10-20)

Lift04MovingSideFrames.py records detailed components (0,292,1002,1003) and
(0,288,931,932). Their 4596/3596 vertices remain on original bone 2. All 364
centre-touching faces per level use only X=0 and +/-5.3602295. The side geometry
translates rigidly and the connecting faces extend with the original centre fixed.
LOD2 merges these structures into a different assembly and is not accepted by
this detailed helper.

Lift04MovingEndFrames.py handles (21,1004)/(21,933)/(12,84), including the merged
far assembly. The measured inner side-post boundary is X=+/-5.1359863. Original
vertices outside that boundary translate; interior vertices stretch between the
boundaries. Offline subdivisions at those two planes separate the regimes without
stretching post profiles. The 2328/1864/304 original vertices retain Y/Z and bone 2.
LOD0/1 each add 80 edge vertices and replace 40 faces; LOD2 adds 64 and replaces
16. Added vertices use native edge interpolation through MovableStructureModel,
including bone and surface attributes. No marking-driven cut or custom shader is
introduced. This is scoped structural LOD subdivision, not road-top triangulation.

Source area conservation and four-width winding checks pass. Native and widened
end-frame profiles were inspected. A temporary harness compiles the actual current
Plan implementation and all emitted Lift04 width helpers: 12 LOD/width cases pass
source Y/Z and index preservation, constructed vertex validation and disjointness
of the detailed 200-face road removal mask. This does not execute the Unity mesh
attribute writer, native animation or asset persistence. Fixed road bodies, remaining
upper details and final road/renderer integration remain unfinished. No new
user-visible text is introduced.

## Fixed road bodies and far shoulders (2026-10-20)

Lift04FixedBodies.py records components (269,880)/(266,820)/(83,165), covering
2396/2092/234 vertices on fixed bone 0. The measured spans run from +/-80 to
+/-96m. Side geometry translates with the road base while original centre vertices
remain fixed; Y/Z and native material slots are retained. Widening and winding
checks pass at four deltas. The detailed road-top mask remains a separate stage.

Lift04FarShoulders.py subdivides the fourteen measured far top faces at X=+/-4.
Only their outer shoulders are emitted: 42 triangles and 56 edge-interpolated
vertices. The central bed is removed for selected-road replacement. Adjacent
outer platform strips, vertical walls and undersides remain untouched. The original
one-metre shoulder width and projected area remain constant at all tested widths.
The helper appends to the existing edge-only structure Plan without losing its
frame fragments or constructed positions; source indices are checked explicitly.
Native and widened retained-shoulder plans were inspected.

The composed Plan harness now covers fixed bodies plus far shoulder fragments in
all 12 LOD/width cases. LOD2 contains 120 constructed vertices (64 frame + 56
shoulder); detailed levels retain 80 each and their disjoint 200-face road masks.
The remaining upper details and full road/material/generator integration are not
complete. No user-visible text or existing asset files are changed.

## Complete main-mesh width assignment (2026-10-20)

Lift04UpperDetails.py supplies the remaining detailed platform rings and offset
beams, 396/332 vertices on fixed bone 0. Platform opening sides translate beyond
the measured X=+/-1.5881958 inner boundary; the middle strip stretches. Offset
beams use their own asymmetric span -5.158325..5.2872314, retaining the authored
centre offset. Sixteen source faces per level are subdivided with 32 interpolated
edge vertices. Native/widened top views were inspected. The native far mesh omits
these details.

Lift04StructureWidthPlan combines all measured width tables, end-frame recipes and
upper-detail recipes. Every original main-mesh vertex is assigned exactly once:
60636/45780/3212 vertices across LOD0/1/2. This proves map coverage, not visual or
native operation acceptance. Added-vertex fragment indices are offset when the two
independent detailed recipes are combined. Original source indices and Y/Z remain.
Lift04StructurePlan then applies the distinct detailed road mask or far shoulder
reconstruction. Final constructed counts are 112/112/120. Twelve actual composed
Plan cases pass at delta -0.5, 0, 2 and 8m; road replacement adds exactly 200/200/14
removed source triangles without overlapping structural fragments.

The main structural width/removal pipeline now has a callable combined entry, but
road appearance, selected-sidewalk compatibility, unrestricted narrowing, final
render assembly and generator routing still require work. No original prototype
Prefab is needed by these helpers. No user-visible strings or saved assets change.

## Prototype road appearance and render assembly (2026-10-20)

Lift04RoadAppearance records four repeating source-unit projections, separating
fixed and moving spans and their longitudinal directions. All 200 measured LOD0
road-bed faces address UV rectangle (0.4697265625, 0.8251953125) to
(0.62548828125, 0.9814453125), a 638 by 640 pixel region of the 4096 atlas.
The source Surface's original texture hash matches the VTTexture header, and its
serialized preprocessed texture CID resolves to a66819e9780aa14044c36a85fd77c781.
The native debug-texture reader confirms row-major tiles followed by smaller mips.
The crop is blue-grey: an earlier research expectation of asphalt was incorrect,
not evidence of a missing texture or broken source reference.

The measured representative moving unit spans Z=64.72265625..68.529296875;
fixed units span four metres. Lateral density is 160 pixels/metre, longitudinal
density is respectively 167.6018471 and 159.5 pixels/metre. Sixteen source triangles
from both sides and both ends agree with the periodic projections to below 0.002
pixels (the research OBJ positions have decimal rounding). Repetition uses these
representative units, rather than reproducing every original irregular strip length
and phase reversal. This is not evidence that every authored UV coordinate is
unchanged. The native crop was inspected. No painted markings are baked into it.

Lift04RoadPreparation connects the measured physical partition, four appearance
regions and selected-road compiler for all LODs. It retains three native bones;
physical road ownership remains fixed bone 0 and moving bone 2. Appearance-region
indices are independent and never replace those physical bone indices.
Lift04RenderAssembly connects the measured main structure and detailed glass plans
through the shared native render builder, requiring the reconstructed source names
and mesh counts 2/2/1. It does not look up original prototype Prefabs.

These helpers are not yet routed through a complete Lift04 generator. Original
track removal, selected-sidewalk compatibility, unrestricted narrowing, object and
network assembly, persistence and native execution remain unfinished. Existing
saved bridges and installed binaries are unchanged. Release compilation and the
12-language checks pass; no user-visible strings were introduced.

## Object assembly (2026-10-20)

Lift04ObjectAssembly connects the three recorded render slots in native order:
mechanism, column and footing. It retains the recorded mechanism components,
original skeleton ownership and native standing-object Y/Z dimensions; only the
full X support span increases by twice the half-width delta.

The authored attachment list contains 44 fixed-tower objects: two
WallLightShortRange01, two ACVentWall04 and forty LadderMiddle02. Their original
parent connection 0, rotations, group indices and probabilities remain unchanged.
Each attachment translates with its measured parent's side in X; no attachment is
stretched or moved to the moving deck bone. Sources use the original resolver and
reject foreign generated-bridge Prefabs. Selected-road objects are attached after
creating the final mechanism so native effects belong to that owner. Selected-road
paint uses the shared native skinned-paint path and the existing physical partition
(fixed 0 / moving 2), not the four appearance-region indices.

Release compilation and all twelve localization checks pass. These are assembly
helpers, not a routed generator or evidence of native animation/persistence. Native
track geometry still needs removal before final selected-road integration. No new
user-visible text, installed binaries or saved bridge assets were changed.

## Native track replacement (2026-10-20)

Lift04TrackRemoval.py records the reviewed rail, third-rail support/fastener and
track-bed strips that must be replaced by the selected network. Offline plan and
end views show four running rails, two elevated central rails and their hardware.
The recorded component sets contain 262/204 parts and 20202/11421 vertices at
LOD0/1, covering 11894/5306 triangles. LOD1 omits or simplifies small fittings;
its mapping is independently pinned to its actual topology, not copied by index.
Both levels retain fixed bone 0 and moving bone 2 in the source evidence.

The generated runtime mask consists of 34 immutable face ranges per detailed LOD,
with source topology hashes. Runtime does not classify geometry by height, bounds
or names. The deck base, fixed bodies, structural shell and underside are excluded.
LOD2 has no independently authored rail components in this region and retains the
existing far shoulder/road-top reconstruction path.

Lift04StructurePlan combines this removal with its separate 200-face detailed
road-top mask. The actual Plan harness passes all twelve LOD/width cases: detailed
road/track removal adds 12094/5506 distinct faces, far road replacement adds 14;
none overlaps the structural subdivision masks. Constructed vertex counts remain
112/112/120. Original position Y/Z, source topology and structural width assignments
remain intact. This is selected-network track replacement, not marking-driven
triangle cutting. No existing saved bridges or installed binaries change.
