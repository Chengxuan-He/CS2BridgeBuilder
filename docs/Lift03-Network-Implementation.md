# Lift03 main and auxiliary network integration data
This report retains source measurements and stage-specific implementation evidence. Early
statements about missing callers, private branches or unfinished routing describe their recorded
stage, not the current ten-style router. Use the [documentation index](README.md),
[current contracts](agent-contract/README.md) and [shared handoff](../tools/PreparedMaterials/SharedCurrent.md)
for current requirements and delivery evidence. Recorded tests do not certify the new material scheme.


## Dual-network assembly stage (2026-10-20)

`Lift03NetworkAssembly.Apply` now consumes two private, direction-prepared selected
network clones and factory-owned pillar/mechanism objects. It resolves per-occurrence
roles, converts both section lists and adopts their generated dependencies into
the existing factory lifetime. It then applies the separately recorded Bridge fields,
adds state 51 only to the main network and connects the auxiliary at `(0,-10,0)` with
linked end offsets and inversion mode zero.

Only the main receives the two measured pillar placements and one mechanism.
Elevated support placements from both selected networks are replaced; their ground
objects and complete outside-connection rules remain. This deliberately preserves
the selected networks' connection behavior instead of imposing the original highway
Y=5 or train Y=8 connection on arbitrary user selections. The original full native
placement methods remain available as measured reference data. `StructurePlacements`
separates the original three structural entries without changing their fields.

Both section conversions finish before the target section arrays are assigned.
Created sections are adopted immediately so the enclosing generation lifetime can
dispose of them if the second conversion fails. The caller must invoke this stage
once on its private clones; it does not mutate source networks, add runtime update
dependencies or establish a second mechanism on the auxiliary.

Release compilation and all twelve localization checks pass. There is no new
user-facing text. This stage is not yet called by a complete Lift03 generator:
final dual-deck lifecycle integration, structural
geometry and native operation remain unfinished. No installed binary or saved
bridge changed in this helper increment. Game and cold-load acceptance remain
deferred to the complete batch.

### Selected-section role resolution

`Lift03SectionRoles` resolves the two decks independently from selected section data.
The recorded Highway Side 0 and Train Side 0 contain only Side-layer pieces; their
shoulders, lanes and medians contain Surface, Bottom or Top pieces. The resolver
recurses through subsection wrappers and requires at least one piece before treating
a section as side-only. It does not classify by localized names, widths or positions.
For the auxiliary, the occurrence's native `m_Median` flag selects the full-length
median branch; other non-side occurrences receive the four track/shoulder branches.
The main keeps its distinct three-branch deck rule.

An isolated C# harness links the production resolver and compares all 44 recorded
native occurrences (23 main, 21 auxiliary) against their original branch families.
Expected families come from the native hidden-layer and half-length branches,
independently of the resolver's layer classification. All cases and their nested
wrapper equivalents pass; five missing/cyclic external graph cases fail explicitly.
The fixture comes from audit SHA-256
`47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070`.
This checks role selection, not native section conversion or game simulation.

## Coordinated selected-network sampling (2026-10-19)

`Lift03RoadSampling` now constructs both selected decks in one private
`MovableNetworkContext`: six nodes, four edges and one mechanism owned by the central
main node. Each auxiliary node/edge has the corresponding main node/edge as native
Owner, with the reverse SubNet buffer. There are no independent single-deck samplers
or duplicate mechanism objects. Native `GeometrySystem.CalculateOffsets` reads the
owner edge's composition node offset, which is why these relationships are required.

Approach stations use the measured 200 m segment length. The main sampling origin
is at 20 m, and the auxiliary origin is 10 m below it. These constants and mechanism
placement are shared with `Lift03NetworkData`; final AuxiliaryNets still records
linked end offsets and native inversion mode zero. Both selected inputs must already
represent the requested deck directions. Each retains its own upgrade and explicitly
specified replaced structure layers. No prototype track replaces the selected input.

Each deck compiles its two central-node halves and central-node lane visuals at each
requested LOD. Its road-to-object pose is derived from the same staged mechanism
transform, retaining the actual inter-deck separation. Selected node objects remain
available separately per deck. The context and temporary mechanism are disposed on
failure and normal completion; no staged entities are persisted.

Validation: the pinned native audit confirms both segment lengths, elevations,
auxiliary offset/inversion, linked ends and every mechanism placement field. Release
compilation and localization checks passed. This sampling stage is not yet called by
a complete Lift03 generator. Native execution, linked endpoint geometry, combined
traffic signals, both decks' surface/paint binding and cold loading remain unverified.
Those checks accompany the complete implementation batch; this is not a claim of
playable dual-deck support.

`Lift03NetworkData.g.cs` implements separate measured schemas for LiftBridge03 and LiftBridge03 Train Track. The latter remains the auxiliary network of one style, not a separate style. The source audit hash is recorded in the generated source.

The main network has 23 entries: two side occurrences with one ordinary-node branch each, and seven deck occurrences with ordinary-node, hidden moving-node and Bottom-hidden edge branches. These main edge branches are not half-length.

The auxiliary network has 21 entries. Side occurrences have only the ordinary-node branch. Each track/shoulder occurrence has four branches: ordinary node, hidden moving node, ordinary edge excluding FixedNodeSize, and a half-length FixedNodeSize edge. Both edge branches hide Bottom. Its median instead has three branches and retains full-length edges. The source's two permutations of the same exclusion list were compared as sets before using the common track/shoulder rule; no condition was removed.

Callers must supply one explicit role per selected-road occurrence. This class preserves selected sections and their own condition groups through the shared converter; it does not guess roles from names, widths or geometry.

Both native Bridge segment lengths are 200. Main water elevation is 20; auxiliary water elevation is 10. Only the main network appends measured state 51 on edges and nodes. Main placements retain the generated pillar at Z=5 and Z=0, the generated mechanism at the origin, and the selected main outside connection at Y=5. The auxiliary places only its selected outside connection at Y=8; it does not duplicate the main mechanism or pillar.

`ApplyAuxiliary` connects the generated lower network at native position (0,-10,0), preserves `m_LinkEndOffsets=true`, and records native inversion mode 0. This is the native arrangement, not an adjustable separation or a substitute prototype track. Selected direction controls still need integration with the existing request/generation path.

The change is authored and compiled on isolated branch `bridge/lift-03`, using the unchanged dev shared converter as a build dependency. Only the new style source and this document enter the style commit. Existing dirty worktree edits are retained separately.

No generator calls this class yet. Role assignment, both selected-road surfaces, structure/markings, skeleton integration, native signals, routing and cold-start acceptance remain unfinished. No existing bridge output is affected and no bridge cleanup is required. No game control or visual-generation unit tests are performed.

## Coordinated selected-deck lifecycle entry (2026-10-20)

The common generator base now exposes an opt-in `BuildsAuxiliaryDeck` capability and
accepts one private auxiliary clone together with its original selected network and
direction. The shared preview/persistent composition path clones that deck before
`Apply` for opted-in generators. It skips the ordinary post-generation second-deck
composer and verifies that the generated main network references exactly that clone.
Both clones stay in the existing cloner ownership, failure cleanup, preview adoption
and persistence plan. Existing generators keep the default false capability and their
original second-deck path. No concrete Lift03 generator has enabled this path yet.

`Lift03RoadSampling` also accepts auxiliary direction. For the opposite direction it
reverses the two lower edge endpoints and curves without changing physical node/edge
ownership. Its central-node contributions swap StartNode/EndNode consistently at all
LODs. Road-to-mechanism conversion still uses the evaluated world-relative frame;
there is no second geometric rotation. `Lift03NetworkAssembly` records the matching
native Always/Never auxiliary inversion rather than changing shared selected assets.
The eventual generator must pass the same direction to sampling and network assembly.

Release compilation, 29 route checks, 82 recorded-variant checks and twelve-language
checks pass. Ten offline checks using extracted production ownership members exercise
shared-source rejection, unsupported generators, duplicate setup, missing/replaced/
duplicated auxiliary references, correct private attachment and direction retention.
They do not execute native network calculation or verify opposite-direction visuals.
No new user-visible text was added. Full Lift03 generator wiring, side-layout adaptation,
narrowing compatibility and native persistence remain unfinished. This increment has
not been deployed and does not modify healthy saved bridges.

## Correct elevated width baselines (2026-10-20)

`Lift03RoadWidths.py` checks the hash-pinned native audit across all four combinations
of Node and FixedNodeSize for each deck. The main road is 20 m: four 4 m highway
strips and two 2 m shoulders. The auxiliary elevated track is 10 m: two 4 m strips
and two 1 m shoulders. Its `Train Shoulder 2` section selects `Train Shoulder Piece 1`
in elevated mode; its ground-mode width must not be used. The checks exclude the
measured outward Side sections, evaluate conditional occurrences, count vertically
stacked Surface/Bottom pieces once, and retain the intersection-only zero-width median.
All eight cases pass with the expected occurrence multiplicities.

`BridgeMeasurements` now stores these two baselines as named constants and uses them
for the existing native-name table entries. The former 62 m and 50 m values counted
mutually exclusive branches. The 93 m recorded tower entry is unchanged. This is an
offline source-data correction, not runtime geometry inference or a generated-bridge
width audit. Actual output width/clearance measurements remain pending. Release,
82 recorded-variant checks and all twelve UI-language checks pass. No new user-visible
text was introduced, and no deployed binary or existing bridge was modified.

Generator integration also still needs the ordinary `LiftBridge03NetPillar` assembly:
the audited native object (118758) owns separate NetPillarTop, NetPillar and
NetPillarBase renders (79694, 79688, 79691). The already implemented mechanism column
and footing are not substitutes for those approach supports. Their own recorded
geometry and all-LOD assembly must be supplied before routing Lift03.

## Recorded dual-deck catalogue metadata (2026-10-20)

BridgeStyleVariant now accepts an optional measured auxiliary height for recorded
variants. This allows selection and the existing preview/formal deck-arrangement
calculation to recognize a dual-deck archetype without a live prototype Prefab.
The returned arrangement is a fresh placement-only descriptor; it contains no
fabricated network and must not be persisted. The coordinated generator supplies
the privately cloned selected auxiliary network through its existing lifecycle.
Live-donor variants and recorded single-deck variants retain their prior behavior.

The production selection harness now covers Lift03's measured -10 m auxiliary
height, double-deck-only selection, prerequisite availability and descriptor mutation
isolation. All 88 checks and Release compilation pass. This is preparation for
Lift03 routing, not a registered generator or game acceptance. No user-visible
text or localization keys change. Catalogue registration will be added with the
concrete generator; this helper-only increment is not deployed.

## Concrete coordinated generator route (2026-10-20)

Lift03BridgeGenerator now inherits the shared movable lifecycle and opts into its
owned auxiliary clone. The catalogue records LiftBridge03 as a road archetype with
its measured -10 m auxiliary offset, using the BridgesAndPorts prerequisite instead
of requiring the original network Prefab. Lift03 is routed explicitly alongside the
other nine movable styles; its existing double-deck-only UI rule and localized style
name remain unchanged.

Sampling uses both selected source networks and the same auxiliary-direction flag
that network assembly persists. Main and auxiliary road width deltas use their own
20 m and 10 m baselines. The shared frame uses the larger delta to clear both decks;
detailed road bases retain separate deltas. Both prepared road LOD sets use one
mechanism world matrix recovered from the evaluated main road-to-object pose, rather
than guessing a world height. Each retains its own appearance frame and partitions.
The render assembly combines them with the native structural plans and original
road bone. Object assembly attaches both decks' paint and selected objects once.
The mechanism columns/footings and separate ordinary approach pillars are assembled
from their recorded sources. Main and auxiliary unlock snapshots are applied to
their respective private networks before the existing persistence transaction.

Release compilation, 30 explicit route checks, 88 recorded-variant checks and all
12 UI-language checks pass. No new user-facing text was introduced. These are source
integration checks, not successful game creation or acceptance. Selected sidewalk
adaptation, prototype rail replacement, narrow-road clearance, independent deck-width
behavior in far structure, native generation and cold-load/asset-size verification
remain unfinished. This route has not yet been deployed; healthy saved bridges were
not changed. Human visual verification remains deferred until the batch is ready.

## Independent deck widths in the far supplement (2026-10-20)

The combined production-geometry harness now exercises nine upper/lower delta
pairs: (-0.25,-0.25), (0,0), (2,2), (8,2), (2,8), (-0.25,8),
(8,-0.25), (0,2), and (2,0), in metres. The structural delta is the larger
of the two, matching the generator. All eighteen detailed-LOD cases and nine
far-supplement cases pass fragment position/attribute reconstruction, original
index and source-position preservation, and disjoint road-removal checks.

For the far supplement, a separate fixture extracted from measured middle mesh
component IDs checks all 2,962 road-base vertices. Components 65/778/2826 follow
the upper delta and 66/779/2827 follow the lower delta; the expected displacement
uses source X sign and the selected deck delta, without calling DeckBases.Apply.
Every pair preserves source Y/Z. The supplement remains 4,488 result triangles
with 768 constructed vertices. This closes the specific concern that both far
road bases might inadvertently follow only the wider deck.

Reproduction uses the research lift03-combined-check/Check.csproj against
lift03-structure-check inputs, including deck-ownership.txt extracted from
lift03-main-1-0.npz. This harness links production geometry with a lightweight
Unity vector substitute and the extracted Plan implementation; it does not run
the Unity renderer. The remaining merged far superstructure, extreme narrowing,
selected-sidewalk adaptation, native generation, persisted resources and cold
loading are still unverified. No runtime code, installed binaries, saved bridges
or user-facing text changed in this audit; localization behavior is unchanged.

### Signed narrowing boundary coverage

The same harness now covers fourteen width pairs, adding (-9.99,-4.99),
(-9.99,0), (0,-4.99), (-3.3563232421875,-3.3563232421875), and
(-9.429199,-4.99). These reach the positive-road-width boundary for the measured
20 m upper and 10 m lower baselines and exercise recorded centre-crossing
thresholds. All 28 detailed cases and 14 far-supplement cases pass; detailed
output positions are additionally checked for finite coordinates. The runtime
thresholds in ValidateWidth are diagnostics only, not generation rejections.
No extra output-span guard was found in Lift03 geometry during this audit.

This demonstrates preserved signed mappings and assembly through the tested
boundaries, not absence of inverted or overlapping faces under extreme
narrowing. Such crossings remain subject to the contract's explicit reporting
rule. Native rendering and full selected-road sampling are outside this harness.
The next implementation work remains selected-sidewalk compatibility and the
complete generated-resource path; repeating these width cases is unnecessary
unless affected code changes. No localization keys or runtime files changed.

## Retained deck-edge surface audit (2026-10-20)

A fresh source-face audit of both detailed geometry levels records the upward
faces retained after RoadTops removal in components 65/66/778/779/2826/2827.
The research artifact lift03-retained-base-tops-audit.json contains exact source
face IDs, bounds and height levels; identification uses original measured meshes,
not a new runtime spatial classifier.

The middle fixed upper base's inner remaining ledges lie at X magnitude 8..10
and Y 19.799805..19.800293, below the Y=20 replacement road. Outer strips occupy
X magnitude 10..10.25 at Y=20; the higher outer lip occupies 10.25..10.449463 at
Y=20.399902. The lower fixed base's retained upward strips occupy X magnitude
5..5.4752197 at Y=10.100098, outside the native 10 m road envelope. This evidence
does not support removing the entire base or its outer safety edge when a
selected road has no Sidewalk sections. Detailed and far-supplement base movement
already follows each deck's own width, so these outside-edge locations translate
with that deck rather than reserving a fixed-width strip inside it.

Selected-road sidewalk geometry is classified by MovableSelectedRoadLayout and
bound through MovableRoadPreparation. Lift03 currently passes the same motion
partition and appearance regions for sidewalks and carriageway, with separate
upper-asphalt/lower-ballast recipes. Motion partition reuse follows their shared
native road bone and seams; material reuse still needs explicit compatibility
review, particularly a pedestrian surface on the auxiliary deck. This audit
therefore does not close selected-sidewalk compatibility. No runtime deletion,
material substitution, saved-asset edit, or localization change was made.

## Sidewalk material provenance decision (2026-10-20)

The recorded native network graph resolves the material question more narrowly
than the earlier hypothesis. LiftBridge03 uses Highway Side 0, Highway Shoulder 2,
Highway Drive Section 4 and Highway Median 0. Its auxiliary network uses Train
Side 0, Train Shoulder 2, Train Track Section 4 and Train Median 0. Neither has
an authored sidewalk section or a recorded separate sidewalk material recipe.
The upper asphalt and lower ballast are thus the measured prototype deck domains,
not evidence that a native sidewalk descriptor was accidentally dropped.
Do not substitute another bridge's concrete/grating material based on appearance
preference alone. Selected Sidewalk geometry still follows its evaluated footprint;
this finding does not prove its complete generated appearance or clearance.

The broader source inventory distinguishes DrawBridge01/02/03's explicitly named
bridge-specific Sidewalk 3.5 sections from the rail/highway Lift families and the
PedestrianDraw pavement paths. An exact ordinary `Sidewalk` prefix test is only
appropriate for selected-road classification; applying it to prototype custom
section names incorrectly hides the Draw family's authored sidewalks. The research
movable-native-sidewalk-family-audit.json retains all section names to make this
scope distinction visible. Prototype geometry still requires measured authored-part
classification, not runtime name inference.

No runtime material change is justified by this evidence alone. The next audit
must inspect bridge-authored portions and selected-road output together, rather
than treating shared motion partitions or absence of an ordinary section prefix
as a sidewalk-compatibility failure. Existing UV units, skeletons, localization,
persistence and installed files remain unchanged.

## Repository-owned structural verification tool (2026-10-20)

The combined geometry check is now reproducible through
[tools/Lift03StructureCheck](../tools/Lift03StructureCheck/README.md), without
hardcoded developer workspace paths. Its exporter verifies the two original
Geometry hashes, checks decoded cache dimensions, and writes the independent
2,962-vertex deck ownership fixture. It extracts the current production structural
Plan rather than using a simplified constructor. The runner rejects a stale
snapshot before geometry checks; deliberate snapshot corruption verified this
failure path and was restored afterwards.

All 28 detailed cases and 14 far-supplement cases passed with the repository
runner. The final checker build has zero warnings/errors. Twelve-language checks
pass; technical checker diagnostics are developer-only and no game UI strings
changed. Native rendering, material conversion, persistence and full far-shell
clearance remain outside its scope. Production DLLs and saved assets were not
changed or deployed by this tooling increment.

## Production result surface candidates (2026-10-20)

The repository structure checker can now export the actual zero-delta composed
mesh, including replacement fragments and original-face provenance. Both detailed
levels were exported and audited, rather than inspecting only removal masks.
All 42 existing geometry cases still pass. The output is diagnostic geometry,
not a materialized or skinned game asset.

An offline horizontal-face candidate scan of the composed output found 16 upper
faces in parts 102/815 and 60 lower faces in ten small components at each detailed
LOD. The upper candidates are structural edge pads: for example source faces
25337/25338 span X=-11.012451..-9.788574 at Y=20.032227 and
Z=83.70117..84.95508. They overlap the native 20 m envelope by about 0.211 m,
not a full-span residual road sheet. Their native role must be respected before
any clearance change; this scan alone does not authorize deleting them.

Lower candidates belong to parts 701/1414/2115/2815/3982/5152/6326/7490/7518/7546.
Each complete component has 48 vertices and 22 faces; its upper horizontal faces
are at Y=10.185059, with X magnitude approximately 4.103..4.745. They occur near
fixed ends, moving ends and the moving centre. These are retained discrete
components, not the already removed rails/sleepers. Their role and behavior for
selected road layouts remain a concrete follow-up; do not infer removability
from height alone or classify them as sidewalks by proximity.

Exact candidate result/source face IDs are recorded in research
lift03-result-road-clearance-candidates.json, with composed binary meshes from
`--export-detailed`. Candidate bounds are screening evidence, not an exact
intersection test against selected road triangles, nor a visual defect claim.
No production geometry, material, localization or installed resource changed.

## Lower-deck fixture ownership repair (2026-10-20)

The ten retained lower-bed fixtures identified above were reproduced as an actual
width-domain defect: with upper delta 8 m and lower delta 2 m, LOD0 vertex 57751
followed the shared structural delta instead of the lower deck. An independent
480-vertex-per-LOD check failed on the previous production mapping.

The measured deck-edge mapping now includes components
701/1414/2115/2815/3982/5152/6326/7490/7518/7546 in its lower domain. The emitter
verifies each component's 48 vertices, 22 faces, exclusive face ownership, lower-bed
height and original bone (four fixed bone-0 instances, six moving bone-3 instances).
Both detailed LODs retain the complete rigid components, UV/materials and skinning.
Exactly 480 vertices per LOD gain the lower-width assignment; every previous
railing assignment is unchanged. The native coarse mechanism does not carry these
separate fixtures, and the far road-base supplement does not add them.

All 28 detailed and 14 supplement cases now pass, including independent fixture
expectations across all fourteen width pairs. Release, 30 route checks and twelve
UI-language checks pass. There are no new game-facing strings. This fixes width
ownership; it does not establish the fixtures' clearance against every selected
road or authorize removing them based on the earlier candidate scan.

With Cities2 absent, the DLL/PDB were backed up under
`artifacts/backups/20261020-lift03-lower-fixtures-installed` and the current Release
files deployed to the existing active installation with matching hashes. Existing
bridge assets were preserved. Native visual and cold-load verification remain
pending with the rest of the batch.


## Complete native catenary replacement (2026-10-20)

This supersedes the preceding lower-fixture-only repair. Inspection of the
complete assembly identifies those ten feet as catenary supports, with mast
channels, insulators and cantilever arms. Four longitudinal wires and eight
droppers occupy the two native track centres (X approximately +/-1.456 m),
spanning Z approximately +/-79.254 m. The reviewed 208 component IDs are
recorded in Lift03TrackRemoval.py; both detailed LODs use the same authored
membership. Native source topology, fixed/moving bone ownership, disjointness
from tracks and exclusion of the six structural road bases are checked offline.

Remove the complete prototype railway facilities with the replaced tracks,
following the existing Lift01 approach. Selected-road facilities remain owned by
the selected-road generation path. The former special width mapping for the ten
feet is removed; ordinary deck-edge railings retain their original mappings.
This adds 13,832 / 4,921 removed triangles at LOD0 / LOD1. No new triangles,
materials, UV projection or skeleton assignments are introduced. Original
coarse geometry has no corresponding separate assembly; the far supplement
continues to select only its recorded supports and road bases.

An independent source-face fixture first failed on the previous production
implementation at LOD0 face 42578. It now verifies every catenary face is removed
and no structural fragment reintroduces it. All 28 detailed and 14 far-supplement
cases pass. Exported zero-width-delta results contain 242,200 / 136,383 triangles.
These checks do not establish selected-road replacement catenary behavior or
native visual acceptance; those remain batch verification items.

Release compilation through tools/Build.ps1, 30 exact-route checks and all twelve
UI-language checks pass. Direct dotnet build was not a valid substitute for the
repository build script because it lacked the complete game assembly references.
No new player-facing text was added. With Cities2 absent, DLL/PDB were backed up
under artifacts/backups/20261020-lift03-catenary-installed and deployed into the
existing active 160320_17 installation; installed hashes match build output.
DLL SHA-256: C90EE47A6EC686B26A164524604AC4869D4009878B4914F7691CDEFC6D574C88.
Existing generated assets were preserved; newly generated bridges use this repair.
