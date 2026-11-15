# Generation source observations

## Scope and evidence status

This record preserves concrete source observations and historical failures relocated from the generation and movable specifications and contracts. The original passages did not consistently record extraction dates or build identities; none are invented here. These observations establish only their stated inputs and versions, not current all-style acceptance. Requirements remain in the linked specifications and contracts; historical corrective wording below records the reasoning at the time rather than creating policy.

## Historical archetype fidelity failures

Source: [generation-spec.md](../../specs/generation-spec.md).

Faults that came from breaking this rule, each found only by looking at a screenshot:

| Deviation | What it looked like |
| --- | --- |
| `m_RequireElevated` forced true (archetype: false) | props silently reclassified; the flag also sorts the road's own pillars |
| placeholder given 3 mesh parts (archetype: 1) | the game measures the placeholder's height to place the tower, so it was placed by a number 10 m too large |
| `SpawnableObject` built fresh, probability left at 0 | the replacement never won, the placeholder stayed, and the placeholder has no base — a tower hanging in the air |
| component added to `components` directly | no back reference to the owning prefab; `PlaceholderObject.Initialize` threw and the prefab never initialised |
| tower parts given no `StackProperties` (archetype: First/Middle/Last) | no `StackData`, so no `Stack` on the placed tower: drawn at the height it was modelled at, hanging above the ground by the elevation |
| cable piece given no `NetPieceTiling` (archetype: `m_DisableTextureTiling` set) | the composition packs the piece in among the road’s own surface pieces instead of laying it out across the width — cables the right width, in the wrong place |
| every generated mesh left `SubMeshDescriptor.bounds` at its default | `ToUnityMesh` calls `SetSubMesh` with `DontRecalculateBounds` and takes `mesh.bounds` from the descriptors, so each mesh declared a zero-size box at the origin while its vertices, indices and layout were all correct |
| stretch/translate split by vertex position, at half the road | the boundary is a plane through the model rather than a property of the part, so where it fell inside a leg the leg was cut in two — outer portion carried across, inner portion scaled — and the column came out a splayed slab, while its outer edge landed exactly where it belonged |
| one family’s measured distances applied to every family | the distances were held as three constants, so an extradosed tower — whose 21 m section is narrower than its 31 m road and encloses nothing — was sized to stand a suspension bridge’s 3.53745 m outside it |
| the sub-object binding rebuilt from one family’s recorded table | every bridge got the suspension bridge’s `EdgeMiddle` — one tower at the middle of each span — so an arch bridge’s pier stood in the middle of an arch instead of at the node between two, and the golden bridge got one per span where it carries them at the ends of its course |
| only the first entry naming the source tower replaced | a bridge that names its tower several times kept the rest at the donor’s own width, so a forty metre deck wore three fifty metre structures beside one forty metre one — which reads as invented pillars and is the opposite |
| only the chosen structure derived, when a style names several | the golden bridge carries a pylon at each end of its course and a pier at every node between; deriving the pylon alone left three fifty-metre structures beside one forty-metre one, which reads as invented pillars and as a tower too narrow — what it was being compared against was the donor’s own structure at the donor’s own width |
| a double deck bridge built as a single deck one with a second road hung under it | the archetype selected was a single-deck variant (double-deck ones were actively penalised), so the two levels were two structures that had never been designed to stand together |
| the second deck’s offset written as a negative y whatever the archetype said | the extradosed bridge carries its second net above the deck, so its two levels came out the wrong way round |
| the deck separation offered as a 4–24 m slider | a double deck bridge’s structure is drawn around two decks at one separation, so every value but the archetype’s put the second deck through geometry modelled to clear it |
| stretch-or-translate asked of connected components | a portal’s legs are joined to each other through its crossbeams, so the whole tower is one component and the whole tower was scaled — legs thickened or thinned in proportion, and on a bridge whose extra width is negative the two legs drew together until the portal read as one column standing in the road |
| the metadata dump named two archetypes | a fault in a family the file does not cover is a fault nothing can be diffed against, which is rule 7 defeated by its own instrument for the second time |
| one family’s piece and mesh components applied to every family | the suspension cable piece’s `NetPieceTiling` and the suspension tower’s `StackProperties` were held as templates and put on every widened piece and mesh; an arch section is not a cable sheet, and the through arch came out as a repeating discontinuity along its own length |
| carried-across components kept their references to the archetype’s prefabs | `LodProperties.m_LodMeshes` named the archetype’s own coarse meshes, so a widened piece drew correctly up close and snapped back to the width it was authored at as soon as the game swapped to a level of detail — a fault with a viewing distance attached to it |

## Large Road and DrawBridge03 six-lane reference

Source: [movable-spec.md](../../specs/movable-spec.md).

The following reference was extracted read-only from the local game's
`Cities2_Data/resources.assets`, with installed `Game.dll` behavior inspected separately. The
installed game reported `1.6.2f1`. These observations are a reference case, not universal dimensions
and not proof that Bridge Builder already implements the conversion.

Compare `Large Road` (ordinary undivided six-lane road) with `DrawBridge03` (six-lane drawbridge).
`Large Road Divided` is a separate input and must not be substituted for this case.

| Source section index | Large Road | DrawBridge03 counterpart | Target entry indices |
| --- | --- | --- | --- |
| 0 | Road Side 0 | DrawBridge03 Side 0.5 | 0, 1, 2 |
| 1 | Sidewalk With Parking 7 | DrawBridge03 Sidewalk 3.5 | 3, 4, 5 |
| 2 | Car Drive Section 3 - Transport Tram Option | DrawBridge03 Car Drive Last Section 3 | 6, 7, 8 |
| 3 | Car Drive Section 3 | DrawBridge03 Car Drive Second Section 3 | 9, 10, 11 |
| 4 | Car Drive Section 3 | DrawBridge03 Car Drive First Section 3 | 12, 13, 14 |
| 5 | Road Median 0 | DrawBridge03 Median 0 | 15, 16, 17 |
| 6 | Car Drive Section 3 | DrawBridge03 Car Drive First Section 3 | 18, 19, 20 |
| 7 | Car Drive Section 3 | DrawBridge03 Car Drive Second Section 3 | 21, 22, 23 |
| 8 | Car Drive Section 3 - Transport Tram Option | DrawBridge03 Car Drive Last Section 3 | 24, 25, 26 |
| 9 | Sidewalk With Parking 7 | DrawBridge03 Sidewalk 3.5 | 27, 28, 29 |
| 10 | Road Side 0 | DrawBridge03 Side 0.5 | 30, 31, 32 |

In this reference, the left five sections are inverted, the central section is the median, and the
right five are not inverted. Preserve the actual source direction flags. The 11-to-33 mapping is
three conditional entries per lateral section, not an increase in lane count. General conversion
must map actual section roles and resolved lane positions, not require exactly eleven source entries.

Without transport, bicycle or stop upgrades, both networks use `Car Drive Piece 3` and
`Car Drive Lane 3` for the six vehicle lanes. The lane position within that piece is `(0, -0.2, 0)`.
For the centered straight reference, lateral lane centers are
`-7.5, -4.5, -1.5, +1.5, +4.5, +7.5` metres. No transverse scaling of those lanes is needed.

The ground reference composition is 32 m: two 7 m sidewalk/parking sections plus six 3 m lanes.
Elevated selection replaces each sidewalk/parking section with a 3.5 m sidewalk and selects a
0.5 m side extension on each side. Its surface allocation is 25 m and its full lateral composition
is 26 m. DrawBridge03's elevated reference has the same allocation, with bridge-specific side and
bottom pieces. Do not compress a 32 m ground road into a 26 m bridge or hardcode a 3.5 m sidewalk
for other roads. Ordinary and NoTram sidewalk pieces share pedestrian lane definitions in this
case; that does not establish tram compatibility for the drawbridge.

The table explains native correspondence. The generated bridge must still derive its visible road
surface from the selected road, not replace it wholesale with DrawBridge03's road textures or mesh.

## DrawBridge03 fixed-node branches

Source: [movable-spec.md](../../specs/movable-spec.md).

The following branch table describes only the native fixed-node pattern observed in DrawBridge03.
It is not a mandatory three-branch template for all drawbridges, lift bridges or other mechanisms.
Each variant must use the conditions established by its own archetype evidence:

| Branch | Required conditions | Excluded conditions | Hidden layers | Half length |
| --- | --- | --- | --- | --- |
| Ordinary segment | Original applicable conditions | FixedNodeSize | Original visible behavior | false in the reference |
| Moving-span node | Node, FixedNodeSize | Original applicable exclusions | -1 | false |
| Connecting edge | FixedNodeSize | Node | Original visible behavior | true |

## DrawBridge03 placement and hinge measurements

Source: [movable-spec.md](../../specs/movable-spec.md).

For DrawBridge03 specifically, `m_NodeOffset = 96 m` and the main object's recorded mesh bounds
extend from `z = -96 m` to `+96 m`. Hinge positions are approximately
`(0, 4.045972, +75.383911)` and `(0, 4.045972, -75.383904)`. These are different measurements:
the fixed-node extent, model bounds and moving-leaf extent must not be equated. Rounded values in
this prose must not become implementation constants; use the full measured transforms and bind data.

## DrawBridge03 leaf identity

Source: [movable-spec.md](../../specs/movable-spec.md).

DrawBridge03's positive-z leaf uses connection ID 101; its negative-z leaf uses 100. Their rest
rotations/bind matrices differ. Names such as LeftSide/RightSide do not mean the two traffic
carriageways. Do not split three lanes onto each bone or manufacture opposite angle signs from names.

## Movable generation failure observations

Source: [movable-spec.md](../../specs/movable-spec.md).

Related requirements: [Movable Contract](../../contracts/movable-contract.md#movable-15-observed-failures-and-mandatory-prevention-part-2).

These rules generalize failures encountered in the implemented bridge. Evidence is linked in
[section 14](../../contracts/movable-contract.md#movable-14-evidence-references-and-maintenance). They prescribe the generation method, not universal dimensions or proof that every
other bridge has passed acceptance.

| Observed problem | Established cause or diagnostic distinction | Required solution and verification |
| --- | --- | --- |
| Outer walls, underside plates or railing seats vanished | A coarse road-height/removal predicate included non-road source faces | Classify exact authored source faces at every LOD; retain structural supports independently of sidewalk presence and remove obsolete fragments when restoring whole faces. |
| Geometry existed but disappeared during a distance transition | The same physical part migrated between independently selected renderer LOD groups | Keep per-part renderer ownership stable within native batch capacity; verify all independent LOD combinations and actual native thresholds without changing metadata. |
| A median absent from the prototype blocked generation | Motion assignment required a section/face whitelist | Compose the selected road and clip against measured physical domains; exercise unseen sections without adding name exceptions. |
| Road/sidewalk appeared fragmented although triangles existed | Model-container count did not register every material batch | Match native one-model-per-material layout and verify counts at every LOD; inspect rendered output as well as saved triangles. |
| Road parts followed the wrong leaf or split laterally | A longitudinal motion domain is not a traffic carriageway | Clip at measured seams; each triangle belongs to one bone, each leaf carries its full transverse layout. |
| Native grating was mistaken for damaged sidewalk | The prototype deliberately has transparent metal grating | Inspect prototype geometry/materials first; preserve the intended appearance while diagnosing actual missing batches separately. |
| Ordinary approach floated above its pillar | The native pillar was installed without the prototype network Bottom layer | Derive the fixed base and node pieces at every LOD, preserve the native support interface and separate fixed/moving conditions; never raise the pillar to hide the missing base. |
| Fixed base top appeared absent | Sidewalk platforms and carriageway concrete cap are different domains | Retain structural platforms, exclude duplicate road coverage, and assign cap appearance to the generated fixed carriageway; preserve Z. |
| Repeated pale lines unrelated to road markings | The repeated asphalt UV rectangle included a pale atlas gutter | Select verified asphalt interior with filtering margin and preserve physical UV density; inspect a newly generated bridge. |
| Missing or asymmetric sidewalks became symmetric prototype strips | Lateral role selection used prototype curb positions | Use actual selected pedestrian occurrences and geometry; do not fabricate a missing side or mirror its width. |
| Texture scale or direction differed from prototype | Network UV encoding and one global projection did not reproduce object-space atlas domains | Use measured UV-per-metre scale, separate side/leaf origins and mirrors, and verify all LODs. |
| Projection caused invalid tangents | Per-vertex axis projection can vanish or disagree in handedness | Derive per-triangle frames, preserve one handedness, handle singular UV derivatives explicitly without suppressing validation. |
| Paint looked like raised blocks and inflated the graph | Decal projection volumes were rendered as surface geometry; per-marking objects multiplied nodes | Use compact single-sided source-layer meshes on the original mechanism with bounded clearance; never cut road receivers for decals ([sections 18–19](../../specs/movable-spec.md#movable-18-marking-layout-mesh-and-native-material)). |
| Generation took seconds to minutes and dependencies grew | Historical appearance baking incurred texture upload, mip and save work; redundant wrappers/marking nodes multiplied registration | Reuse structural materials, extract bounded repeat units, share appearance across LODs and remove redundant nodes; measure GPU, VT and persistence costs separately ([section 22](../../specs/movable-spec.md#movable-22-performance-and-diagnostic-requirements)). |
| Moving lights duplicated or failed at a seam | Network producers and moving visuals had separate ownership/placement | Preserve native traffic/light functions while suppressing duplicate stationary visuals; use measured native lamp mounts where approved, retaining selected model, lateral placement and orientation. |
| Preview omitted ends, remained closed or was off-centre | Complete open-pose assembly and transformed bounds are required | Assemble fixed ends plus moving centre and fit the camera to all transformed bounds; inspect actual panel output. |
| Persisted mechanism was scheduled for removal | The helper parser rejected valid typed anonymous Odin values | Support the native format in the shared reader, replay real Prefabs/layout migration without identity changes, then check native cold loading. |
| Section/Piece and unlock counts exceeded the prototype | Some copies change producers/upgrade references; other wrappers and equal gates are redundant | Copy only changed nodes, share equivalent conversions within the owner, preserve condition semantics, inline the outer unlock gate and deduplicate equal nested groups. |
| Native network structure lost a worldspace layer or used object-atlas density rules | A network Surface was converted despite remaining in a compatible native NetPiece render context | Preserve the measured native network Surface, encoded channels and composition context under [section 17](../../specs/movable-spec.md#movable-17-material-families-uv-density-and-texture-ownership); test the complete preview/saved path rather than globally bypassing object conversion. |
| Width changes remapped a positive-V network's transverse atlas coordinate | Longitudinal native distance mapping was mistaken for a two-dimensional repeating atlas | Preserve the authored transverse mapping and evaluate longitudinal density through the evidenced native mapping context; do not apply the generic polar-frame V as an unproven substitute. |
| A finite profile axis was forced to repeat with its other axis | A whole-domain boolean could not express mixed finite/repeating sampling | Retain the evidenced finite axis and independently preserve density and phase on the repeating axis, including constructed fragments and every LOD. |
| A finite source domain was excluded merely because a neighboring chart crossed a repeat rectangle | Atlas overlap was used as physical source-part evidence | Classify the domain's actual source faces and sampling footprint; preserve distinct closure/terminal faces and do not expand an arbitrary crop to hide a true crossing. |

## Six-lane extraction procedure

Source: [movable-spec.md](../../specs/movable-spec.md).

Related requirements: [Movable Contract](../../contracts/movable-contract.md#movable-14-evidence-references-and-maintenance), [Movable Contract](../../contracts/movable-contract.md#movable-14-evidence-references-and-maintenance-part-3).

Reproduce the six-lane reference by resolving the two named RoadPrefabs from the installed resource
map, following sections to selected pieces and `NetPieceLanes`, then inspecting DrawBridge03's
StaticObjectPrefab, `NetObject`, `MoveableBridge` and render-prefab procedural animation data.
Decode enum values against the installed assemblies. Follow `NetCompositionHelpers`, mesh-reference
selection, lane generation and the systems listed in [section 9](../../specs/movable-spec.md#movable-9-waterway-and-traffic-compatibility) to distinguish data from behavior.
Retain full-precision data in the implementation evidence; rounded examples here are explanatory.

## Draw01 road LOD observation

Source: [movable-spec.md](../../specs/movable-spec.md).

Draw01 currently prepares three road render levels from corresponding native road inputs and
shares generated appearance resources. Original structure slots and native far structure remain
separate from road conversion. Another prototype may have a different graph; measure it.

## Movable catalogue and acceptance status

Source: [movable-spec.md](../../specs/movable-spec.md).

The catalogue includes 190 original Geometry files / 210 mesh streams,
including stationary approach and network-pillar parts. Additional native network
appearance groups occupy the same section with identical dimensions, offsets and
requirements; construction/upkeep components remain on the original group only.

Complete generated-output density, size and all-style build audits remain outstanding.
Prior Draw01 underside seam checks and source-frame numerical tests do not substitute
for those audits. Native near/far rendering, movement and cold-load observations are
also pending. No blanket all-style acceptance is recorded.

## Draw01 historical implementation acceptance

Source: [movable-contract.md](../../contracts/movable-contract.md).

The selected-road conversion pipeline and its observed failures define these requirements.
Draw01's recorded working path and user-confirmed normal appearance are evidence for the tested
build and input, not acceptance of subsequent material or generation changes. That evidence does
not establish full traffic, shipping, every road/LOD combination or mod-independent cold-load
acceptance. Other mechanisms require their own measured data and implementation evidence;
registered catalogue entries alone do not establish working generators. Completion status must
never disable a registered style or add unfinished-style notices ([bridge generation contract section 22](../../specs/generation-spec.md#generation-22-new-bridge-styles-require-immediate-ui-synchronization-generator-routing-and-multilingual-names)).

## Suspension tower and cable measurements

Source: [generation specification](../../specs/generation-spec.md).

  | part | 5 lanes (road 24) | 4 lanes (road 20) | outside the cables |
  | --- | --- | --- | --- |
  | base | 18.75000 | 16.75000 | 5.27667 |
  | leg | 17.01078 | 15.01078 | 3.53745 |
  | top | 17.15220 | 15.15221 | 3.67887 |
  | cables | 13.47333 | 11.47333 | — |


## Declared versus drawn cable width

Source: [generation specification](../../specs/generation-spec.md).

  27 declared against 26.94664 drawn left the cables 1.6 cm inside the legs at a forty metre road:
  too small to see, constant, and in the one dimension the rule exists to get right.

## Draw03 complete deck-base truss membership

Source: [generation specification](../../specs/generation-spec.md).

**Draw03 complete deck-base truss exception:** the user requires the moving deck-base
trusses to stretch rather than translate. Near/middle measured parts 0 and 1 form one
positive-leaf base; part 88 is the negative-leaf base. Use their full authored span
[-12.5, 12.5] m: `x' = x + (x / 12.5) * delta`, preserving Y/Z, topology, materials
and original bones. Do not stretch the positive halves independently.
The complete moving under-deck truss (five longitudinal frames,
diagonals and connectors, 1,498 measured components per near/middle LOD) uses the
same full-deck scale. This overrides both side-part translation and independent
centre-member stretching for these recorded vertices. Use `Draw03Truss.py` data;
do not classify components at runtime.
Fixed approaches are explicitly included: First300, Second300, Last300 and
Edge350 structural families scale their local X and native piece widths by
`1 + delta / 12.5` in every LOD. The Side50 railing remains rigid and follows
the resulting native layout. Do not put the entire width increment in First300,
or apply the old independent central-brace mapping afterward. Piers and mechanism
axles retain their separate mappings. All Y/Z, materials, bones and topology remain.
The far merged moving shells already use measured span stretching; retain that recipe.
Store membership and measured values in embedded arrays, with CPU affine execution.

The complete Draw03 fixed-truss stretch includes the 92 fixed-bone end
components embedded in the main mechanism mesh, not only the independent approach
NetPieces. Apply the shared deck-span affine map to their longitudinal beams,
diagonals and bearing/connection plates in both detailed LODs. Preserve bone zero,
materials and topology. The native far merged shell has no separate bearing details;
retain its continuous shell representation. Do not classify masonry foundations or
ordinary network pillars as trusses merely because they are stationary.


## Historical centre-clamp and tower-limit failure

Source: [generation specification](../../specs/generation-spec.md).

Both guards that were built around the first mapping cost more than they saved:

Stopping at the centre was there so a part brought in by more than it stood out would close flat rather
than pass through itself. It closes flat as one column, which is not a portal either, and it thins the
leg on the way — the single place in the whole rule where a leg was allowed to change shape.

Holding the tower back was worse. It is one number for the whole tower, so the narrowest part decides
for every other one: a V pylon whose legs stand 5.79 m apart at the bottom held its base to 4.79 m of
narrowing where the road wanted 8, and the base came out three metres too wide to protect a part nobody
was looking at.


## Suspension reference-family evidence

Source: [generation specification](../../specs/generation-spec.md).

Suspension bridges were the initial reference family. Their verified behavior supplies a checklist
for other families; it is not a claim that they are the only supported or accepted designs.
Each family must satisfy the corresponding checks against its own archetype before it is complete.

What was established, and how each was settled:

| Property | Settled by |
| --- | --- |
| the tower reaches the ground | `StackProperties` on each part — First / Middle / Last, direction Up |
| the tower is placed by the right branch | `PillarType.Standalone` = 2, read from the enum, not from field order |
| the swap happens | placeholder with 1 part, replacement with 3, `SpawnableObject` probability 100 |
| the cables sit where the road runs | `NetPieceTiling` on the piece; without it the composition packs it among the road's own surface pieces |
| every mesh declares its volume | `SubMeshDescriptor.bounds` computed from the widened vertices it indexes |
| legs keep their shape, beams stay attached | rule 8: crossing the centre decides stretch against translate |
| the tower stands the archetype's distance outside the cables | measured on two bridges of different road widths, then used to size the tower |

Nothing on that list is suspension-specific in principle, and most of it is already family-agnostic in
the code: rule 8 asks the geometry, the stacking and the tiling are the archetype's own components, the
submesh bounds are arithmetic. Two things are not.

**Measured numbers belong to the tower they were measured on.** The distances of rule 5 were first held
as three constants and applied to every tower with an overhead section. Six of the game's families have
one and two of those are the envelope the road runs between; the rest are something else entirely — an
extradosed bridge fans its cables from a low pylon over the deck and its section is 21 m against roads
of 31 and 61, a lift bridge's section is its lifting mechanism, the grand bridge's is a stiffening
truss. Sizing one of those towers to stand 3.53745 m outside its section is sizing it against a bridge
it has nothing to do with. So the distances are recorded per tower, and a tower is sized against its
cables only when **both** hold: the section is recorded as the outer envelope, and the distances were
measured on that tower.

**Unmeasured is a state, not a gap to fill in with a plausible number.** The two- and three-lane
suspension towers almost certainly share the five-lane's distances. Almost certainly is not measured,
so they have no entry and are sized by the road — which is what every tower was sized by before any of
this was measured, and is therefore not a regression. A family with no recorded archetype falls back
the same way and says so in the report.

That is the whole shape of extending to a new family: measure it, record it under its own key, and let
the fallback carry it until then. What must never happen is a family borrowing another's numbers
because the code had nowhere else to look.


## Historical double-deck separation controls

Source: [generation specification](../../specs/generation-spec.md).

**The separation is not adjustable.** It was a slider from four to twenty-four metres, and every value
on it except the archetype's own puts the second deck through geometry modelled to clear it. The offset
was also written as a negative y whatever the archetype said, so a bridge that carries its second net
*above* the deck — the extradosed bridge does — had its levels the wrong way round. Both are read from
the archetype now, and both controls are gone from the settings rather than disabled: a control that
cannot change anything is worse than none, because it says the value is a choice.


## Grand reference-road correction

Source: [generation specification](../../specs/generation-spec.md).

The measured Grand reference-road correction is a specifically authorized exception to the
1 m audit limit: use the native 19 m reference, without a runtime correction. It does not waive the limit for other cases.


## Historical cross-family component reuse

Source: [generation specification](../../specs/generation-spec.md).

The distinction matters because getting it wrong has one shape and it has recurred four times: a value
measured on the suspension family, held as a constant, and applied to every family that reached the
same line. The tower-to-cable distances did it, the sub-object placement did it, the cable piece's
`NetPieceTiling` did it, and the tower parts' `StackProperties` did it. Each time the suspension bridge
looked right and something else came out as a structure it had never been.


## Historical derived-LOD reference failure

Source: [generation specification](../../specs/generation-spec.md).

prefabs. Anything a carried component points at that is itself being derived has to be repointed at the
derived one — `LodProperties.m_LodMeshes` is the case that showed it, and it showed it in the worst way
available: the piece drew at the right width up close and at the archetype's width from far enough
away, because the level of detail the game swapped to had never been widened. A fault with a viewing
distance attached to it is not one a screenshot of the thing being worked on will show.


## Historical movable persistence acceptance wording

Source: [movable contract](../../contracts/movable-contract.md). The former acceptance table required a fresh cold start and an existing save with Bridge Builder absent, with game/DLC content retained. Current material requirements instead require Bridge Builder enabled for startup program registration; this historical row does not override that current scope.
## Elevated road-width state observations

Source: [road width design](../../specs/generation-spec.md).

The shipped Sidewalk With Parking 5 selects Sidewalk 3.5 for Elevated/Tunnel/Raised/Lowered
(requirements 11/12/13/14, checked against the native enum and serialized Prefab).
For the inspected Medium Road Divided, elevated surface width is
3.5 + 3 + 3 + 2 + 3 + 3 + 3.5 = 21 m; ground default is 24 m.
The inspected five-lane highway uses 2 + 5 * 4 + 2 = 24 m.
These are source examples, not hardcoded exceptions. Harbor and bicycle variants follow
their actual requirements; the parking sidewalk's bicycle-5 alternative excludes Elevated,
while its 3.5 m elevated alternative can itself select bicycle-3.5.

The retained factory log that reported Medium Road Divided as 24 m consistently passed that
value through composer and factory. It rules out corruption during parameter transfer, but
does not validate using ground state for an elevated bridge. A prototype width and selected
road width must describe matching boundaries and states before calculating their difference.

## Historical submesh-bounds failure

Source: [generation specification](../../specs/generation-spec.md).

A mesh also declares the space it occupies, and that is not carried across either — it is computed.
`ModelImporter.Model.ToUnityMesh` calls `Mesh.SetSubMesh` with `DontRecalculateBounds` and then takes
`mesh.bounds` from the union of the submesh descriptors, so the descriptor is the only source there
is. A descriptor built from the three-argument constructor leaves that field at its default, and every
mesh this mod wrote declared a zero-size box at the origin. Each submesh's bounds are measured from the
widened vertices it indexes.


## Historical packed-vertex and component failures

Source: [generation specification](../../specs/generation-spec.md).

That last point is not a detail. A net piece declares:

    Position:Float32x3@0  Normal:SNorm16x2@1  Tangent:Float32x1@1  TexCoord0:Float16x2@1

Two components of signed normalised 16-bit for a normal, because it is octahedrally packed; one float
for a tangent, because it is an angle about that normal. Reading those back through Unity's convenience
accessors gives unpacked `Vector3` and `Vector4`, and writing those out declares three and four floats
where the shader expects two shorts and one float. The renderer walks each vertex at a stride it
computes from the declared layout, so from the first vertex on, every channel is read from the wrong
place. Positions survive — they are `Float32x3` either way and come first — which is why the geometry
was the right size and everything about it was wrong, and why the cables drew as shards lying over the
deck.

A derived render prefab is not only its mesh. The archetype puts components on these prefabs — the
stacking that lets a tower reach the ground, the tiling flag that decides which group a cable piece
is laid out in — and they are as much a part of what the thing is as its vertices. Both were missed
the same way: the fields were carried across and the components were not, so the geometry measured
correct and the result was in the wrong place. They are recorded and applied like every other
archetype parameter, by rule 2.


## Historical phase tolerance status

Source: [movable specification](../../specs/movable-spec.md).

Replace the universal 0.01-pixel phase cutoff with a measured, explicitly documented per-layer
tolerance; its new numeric value remains pending validation.

This records the former unresolved tolerance, not a validated current value.
## Draw01 paint composition source observation

Source: [movable specification](../../specs/movable-spec.md).

`Draw01ClosedRoad` currently passes an empty marking-region list
to `MovableSurfaceAtlas.Compose` and attaches paint separately afterward.
## Native mid-mip cache observation

Source: [movable specification](../../specs/movable-spec.md). The inspected configuration used three matching mid-mip levels. The actual native configuration determines the required count; this observation does not authorize truncating another configuration.
