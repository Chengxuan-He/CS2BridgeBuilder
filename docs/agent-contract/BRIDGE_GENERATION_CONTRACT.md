# Bridge generation contract

## Contents

- [Contract routing](#contract-routing)
- [1. Every generated bridge follows its archetype](#1-every-generated-bridge-follows-its-archetype)
- [2. The archetype is hardcoded, never copied](#2-the-archetype-is-hardcoded-never-copied)
- [3. A generated bridge differs from its archetype in geometry and road structure alone](#3-a-generated-bridge-differs-from-its-archetype-in-geometry-and-road-structure-alone)
- [4. The tower and the cables are derived geometry, never referenced geometry](#4-the-tower-and-the-cables-are-derived-geometry-never-referenced-geometry)
- [5. A generated tower differs from its archetype in width alone](#5-a-generated-tower-differs-from-its-archetype-in-width-alone)
- [6. Nothing is claimed as fixed until it has been seen working](#6-nothing-is-claimed-as-fixed-until-it-has-been-seen-working)
- [7. A fault is found by dumping both and diffing, not by reasoning about it](#7-a-fault-is-found-by-dumping-both-and-diffing-not-by-reasoning-about-it)
- [8. Stretch or translate is decided by one thing: does the part cross the bridge's centre](#8-stretch-or-translate-is-decided-by-one-thing-does-the-part-cross-the-bridges-centre)
- [9. The suspension family is the reference; every other family is held to the same list](#9-the-suspension-family-is-the-reference-every-other-family-is-held-to-the-same-list)
- [10. What is not generated is refused, and says why](#10-what-is-not-generated-is-refused-and-says-why)
- [11. A double deck bridge is built from a double deck archetype, or not at all](#11-a-double-deck-bridge-is-built-from-a-double-deck-archetype-or-not-at-all)
- [12. A bridge is modified only on its own Git branch](#12-a-bridge-is-modified-only-on-its-own-git-branch)
- [13. Stop the game, clean up affected bridge styles and deploy code changes](#13-stop-the-game-clean-up-affected-bridge-styles-and-deploy-code-changes)
- [14. Record the rollback baseline and preserve each archetype's width allowance](#14-record-the-rollback-baseline-and-preserve-each-archetypes-width-allowance)
- [15. Bridge lighting is copied from the archetype as behavior](#15-bridge-lighting-is-copied-from-the-archetype-as-behavior)
- [16. Preserve native construction and upkeep pricing](#16-preserve-native-construction-and-upkeep-pricing)
- [17. Persist external dependencies as identical-CID copies owned by each bridge](#17-persist-external-dependencies-as-identical-cid-copies-owned-by-each-bridge)
- [18. Bridge asset file ownership is determined only by b{uuid} text](#18-bridge-asset-file-ownership-is-determined-only-by-buuid-text)
- [19. Independent native persistence and additive legacy conversion](#19-independent-native-persistence-and-additive-legacy-conversion)
- [21. Movable bridges use generated selected-road surfaces and native mechanisms](#21-movable-bridges-use-generated-selected-road-surfaces-and-native-mechanisms)
- [22. New bridge styles require immediate UI synchronization, generator routing and multilingual names](#22-new-bridge-styles-require-immediate-ui-synchronization-generator-routing-and-multilingual-names)
- [23. GPU computation and parallel bridge generation](#23-gpu-computation-and-parallel-bridge-generation)
- [24. Movable bridge asset size](#24-movable-bridge-asset-size)
- [25. Adapt sidewalk-dependent components to the actual elevated road](#25-adapt-sidewalk-dependent-components-to-the-actual-elevated-road)
- [Detailed repository rule checklist (2026-10-16)](#detailed-repository-rule-checklist-2026-10-16)
- [Retained repository routing records: Current documentation baseline](#retained-repository-routing-records-current-documentation-baseline)
- [Organization revision record](#organization-revision-record)

This is the general normative contract for every bridge generator, fixed or movable. It is
separate from the [project contract](PROJECT_CONTRACT.md), which governs project-wide workflow,
communication and documentation. Read both together with applicable AGENTS.md instructions.
Bridge geometry, native behavior, generation, preview acceptance, persistence and lifecycle rules
are owned here; they are not optional appendices of the movable-bridge contract.

Split baseline (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/split-bridge-contract-20261017`. Existing working-tree changes are preserved.
The reference records committed history, not those local changes.

## Contract routing

- All bridge work: this contract and the [project contract](PROJECT_CONTRACT.md).
- Generator implementation and refactoring: additionally [generator code organization](GENERATOR_CODE_CONTRACT.md).
- Movable bridges: additionally [movable bridge generation](MOVABLE_BRIDGES_CONTRACT.md), incorporated by section 21.
- Persistence: additionally [asset persistence policy](../Bridge-Asset-Persistence.md), incorporated by section 17.
- Previews: additionally [preview specification](../Bridge-Preview-Specification.md).
- Sidewalk-dependent components in every bridge: section 25. Use each side's actual elevated-mode
  sidewalk presence, position and width, including absent and asymmetric sidewalks. Adapt inner
  railings and bridge-authored sidewalk parts accordingly; never impose the prototype's layout.

Existing section numbers are retained for audit continuity. Section 20 remains in the project
contract because it defines project metadata, not bridge generation. Unqualified numbered contract
references in bridge instructions refer here except section 20. This reorganization changes no
technical requirements or acceptance status.

## 1. Every generated bridge follows its archetype

A generated bridge is built from a real bridge of the same type — its archetype. Every parameter of
the result must be what the archetype has, not what seemed reasonable when the code was written.

This covers the whole prefab, not the parts that looked important: the components and their fields,
the sub-object placement (`m_Position`, `m_Placement`, `m_FixedIndex`, `m_Spacing`, `m_AnchorTop`,
`m_AnchorCenter`, `m_RequireElevated`), the number of mesh parts on each half of a placeholder pair,
the spawn probability, the pillar type and its offsets.

Where the code differs from the archetype deliberately, the difference is written down at the point of
difference with the reason. Anything else is a defect, whether or not it has been noticed yet.

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

## 2. The archetype is hardcoded, never copied

The archetype's parameters are read out of the game once and written into the source as data. They are
**not** copied from the archetype prefab at generation time.

The reason is that the archetype may not be installed. A road can be converted with none of the style's
content present, and the generator still has to produce a prefab that behaves like a bridge. Code that
copies from a prefab works until the prefab is missing and then produces something subtly inert.

Where this lives:

- [`BridgeTowerSpec`](../../src/BridgeBuilder/Bridges/BridgeTowerSpec.cs) — the tower archetype as
  plain numbers, with no dependency on the game, so the offline tests can hold it to what was measured.
- [`BridgeTowerTemplate`](../../src/BridgeBuilder/Bridges/BridgeTowerTemplate.cs) and
  [`BridgeCableTemplate`](../../src/BridgeBuilder/Bridges/BridgeCableTemplate.cs) — apply the specs
  to a prefab. The only files that need the game.
- [`BridgeTowers`](../../src/BridgeBuilder/Bridges/BridgeTowers.cs),
  [`BridgeCables`](../../src/BridgeBuilder/Bridges/BridgeCables.cs),
  [`BridgeMeasurements`](../../src/BridgeBuilder/Bridges/BridgeMeasurements.cs) — the measured widths,
  same rule.

**Recorded, or carried across — never recalled from another family.** Rule 2 says the archetype is
hardcoded rather than copied, and the reason is that the archetype may not be installed. That reason
does not apply to a thing already in hand. When the generator is modifying something — a sub-object
entry it is replacing, a piece or a mesh it is widening — the archetype *is* the input, and its
components and fields are carried across whole, changing only what the mod exists to change.

The distinction matters because getting it wrong has one shape and it has recurred four times: a value
measured on the suspension family, held as a constant, and applied to every family that reached the
same line. The tower-to-cable distances did it, the sub-object placement did it, the cable piece's
`NetPieceTiling` did it, and the tower parts' `StackProperties` did it. Each time the suspension bridge
looked right and something else came out as a structure it had never been.

Carrying a component across carries its **references** too, and those still name the archetype's
prefabs. Anything a carried component points at that is itself being derived has to be repointed at the
derived one — `LodProperties.m_LodMeshes` is the case that showed it, and it showed it in the worst way
available: the piece drew at the right width up close and at the archetype's width from far enough
away, because the level of detail the game swapped to had never been widened. A fault with a viewing
distance attached to it is not one a screenshot of the thing being worked on will show.

So: recorded data is for what must be reproduced when the archetype is absent. What is present is
carried, not recalled. Where both exist, the recorded value becomes the check on the carried one — see
`CheckStacking`, which is the floating tower turned into an assertion.

Measurements are taken by `TowerSelfTest` and `AssetAnatomy`, which write them to
`ModsData/BridgeBuilder/`. Re-measuring is how these tables get corrected; a number that cannot
be reconciled with a measurement is wrong even when the bridge looks right.

**Measured data is never deleted.** An entry that turns out to describe something other than what it was
recorded as — a support column read as a portal — is marked, not removed. Deleting it only means it
gets measured again.

### No Boolean operations with prototype geometry (2026-10-17)

Bridge generation must not perform any Boolean operation with the prototype bridge:
intersection, union, difference or clipping against its meshes, triangles or reconstructed
prototype surfaces is prohibited. Hardcoding a copy of prototype triangles does not make
runtime Boolean operations against that copy permissible.

All prototype-specific coordinates, boundaries, transforms and part membership required by
generation must be measured offline and hardcoded as immutable construction data. Runtime
must directly apply those recorded rules, never consult prototype geometry to discover
or recompute the result. Prototype meshes remain the offline reference for shape and LOD
complexity; permitted attribute/resource reuse and recorded structural transforms are not
Boolean operations. Actual selected-road composition remains an input, not a fixed road layout.
Necessary selected-road cuts at hardcoded motion seams and appearance boundaries are distinct
from intersecting that road with prototype faces and remain subject to section 4's complexity rule.

This rule applies to every bridge style, every LOD, preview and saved generation. Replace
prototype-face intersection with direct generation using the recorded construction rules;
do not merely move the same Boolean operation behind a helper or a hardcoded triangle table.

Pre-edit baseline: `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`;
rollback reference `refs/rollback/no-prototype-booleans-20261017`. Existing changes are preserved.

## 3. A generated bridge differs from its archetype in geometry and road structure alone

Everything else is identical: the components and their fields, the bridge behaviour, how the tower is
anchored, where the cables are drawn, the placement flags. Two things are allowed to differ, and they
are the two the mod exists to change —

- **the road structure**, because the point is to carry a road the archetype does not have;
- **the geometry**, because a road of a different width needs a tower and cables of a different width.

Anything else that differs is a defect. When a generated bridge and its archetype are dumped by
`AssetAnatomy`, the two should read the same line for line apart from what identifies an asset rather
than describes it — names, the four `m_GuidPart` fields, `version` (which `OnSerializing` stamps and
nothing else reads) — plus mesh contents and the road's own `m_Sections`.

## 4. The tower and the cables are derived geometry, never referenced geometry

Both are produced by modifying the archetype's mesh. Neither may point at the archetype's mesh
unchanged and neither may be built from nothing.

A mesh also declares the space it occupies, and that is not carried across either — it is computed.
`ModelImporter.Model.ToUnityMesh` calls `Mesh.SetSubMesh` with `DontRecalculateBounds` and then takes
`mesh.bounds` from the union of the submesh descriptors, so the descriptor is the only source there
is. A descriptor built from the three-argument constructor leaves that field at its default, and every
mesh this mod wrote declared a zero-size box at the origin. Each submesh's bounds are measured from the
widened vertices it indexes.

Modifying means the vertex positions and nothing else. Every other part of a mesh — its vertex
channels, their formats, the index buffer, the submeshes, the materials — is carried across exactly as
the source declares it.

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

So nothing is re-encoded. The raw vertex buffer is read, each attribute is lifted out of its stream at
the offset and width the mesh says it occupies, and handed on unchanged. Only the positions are
rewritten.

### Prototype-referenced mesh complexity (2026-10-17)

Generated bridge meshes must use the prototype bridge's meshes as their reference.
Excessive mesh subdivision or triangle cutting is prohibited. This applies to fixed and movable
bridges, generated road surfaces and structures, previews and persisted assets, at every LOD.
Use each corresponding prototype LOD's mesh organization and level of geometric detail as the
reference; do not carry near-detail tessellation wholesale into reduced-detail levels.

Necessary cuts for actual geometry, motion, material or selected-road boundaries remain permitted,
including the approved merged-part subdivision exception. That exception and permission to derive
LODs from high-detail geometry do not authorize excessive tessellation. Do not let repeated UV
tiling, marking conversion or intermediate clipping determine an unnecessarily dense final mesh.
Preserve required road composition, appearance, structural parts and bone ownership while producing
geometry appropriate to the prototype's corresponding LOD. Inspect the resulting topology and
triangle counts against that reference; compilation alone does not establish compliance.

Pre-edit baseline: branch `dev`, HEAD `3b6363ada9f1f147b1a660e20c0d6246a54f82dd`;
rollback reference `refs/rollback/prototype-mesh-complexity-20261017`. Existing local changes are preserved.

## 5. A generated tower differs from its archetype in width alone

### General width-only transformation rules

The only thing generation may change about a tower is how wide it is. Everything else — height, the
shape of the legs, the parts and where they sit, the materials, every component and field — is the
archetype's.

Concretely, `TowerWidening` moves vertices along x and nothing else. Whether a part is stretched or
translated is rule 8's question and only rule 8's — does it cross the centre line — and both answers
move the part's outer edge by half the extra width, so the two agree wherever parts meet.

An earlier version split by vertex position instead, at half the road: outside that boundary a vertex
moved rigidly, inside it a vertex scaled. It is kept here as the mistake it was. The boundary is a
plane through the model rather than a property of the part, so it can pass through a leg, and where it
did the leg was cut in two and splayed. See rule 8.

The translation alone was tried and is wrong. `sign(x)` is discontinuous at the centre line, so every
crossbeam spanning the middle was torn open by exactly the shift - invisible at four metres, and a
tower in pieces at forty. It reads as a width past which the mesh explodes, and it is not: it is a tear
that was always there, growing.

Height and depth are untouched by construction. Mesh part offsets move by the same shift as the
vertices they belong to, so the parts stay together. Bounds are derived from the archetype's bounds with
only x adjusted, never recomputed from the vertices — a pillar's authored bounds reach below what its
geometry draws, and that is how the game knows how far down it may be placed.

At the archetype's own width the shift is zero and the result is the archetype, vertex for vertex. That
is the property the tests check, and it is what makes "derived" mean something.

Two consequences worth stating, because both were got wrong:

- A tower is never scaled. Scaling thickens the legs in proportion; the tower stops being that tower.
- The tower stands the archetype's distance outside the cables, at every width. Measured on both of the
  game's suspension bridges, which are different road widths and agree to five decimals:

  | part | 5 lanes (road 24) | 4 lanes (road 20) | outside the cables |
  | --- | --- | --- | --- |
  | base | 18.75000 | 16.75000 | 5.27667 |
  | leg | 17.01078 | 15.01078 | 3.53745 |
  | top | 17.15220 | 15.15221 | 3.67887 |
  | cables | 13.47333 | 11.47333 | — |

  The tower's width is derived from this, not from the road. Solving
  `towerOuter + extra/2 == cableOuter + distance` gives the widening directly, and it comes out the same
  whichever part is measured because the archetype satisfies all three distances at once. The rule this
  replaces — the deck's width minus the road the tower was authored for — gives the same answer whenever
  the tower and the cables came from the same bridge, and only this one is right when they did not. With
  no cables to measure against, which is most bridge types, the road rule is what there is.

  This holds because the legs are carried out rigidly by half the extra width and the cable sheet is
  stretched from the span it draws, which moves its outer edge by the same half — two code paths that
  agree rather than one that enforces it. So it is also measured on the result and reported as a defect
  when it drifts. It can drift without anyone making an arithmetic mistake: the tower archetype is
  chosen by width from the recorded list and the cables come from whichever installed bridge carries
  that tower, and the same tower is carried by several. Let those resolve to different bridges and the
  distance becomes tower(A) minus cables(B), which is not this constant and never was.

- The cables keep their distance from the tower. Both are derived from the same archetype by the
  same extra width, so the gap between the cable’s outer edge and the tower’s is preserved exactly
  when both edges move by half the extra. A proportional stretch does that only if it divides by
  the distance being scaled — the span the mesh actually draws, not the width the piece declares.
  27 declared against 26.94664 drawn left the cables 1.6 cm inside the legs at a forty metre road:
  too small to see, constant, and in the one dimension the rule exists to get right.
- A continuous surface is not a portal, and rule 8 tells them apart without being told. Cables are one
  sheet reaching across the centre, so they are scaled; a portal's legs do not reach it, so they are
  carried. Both put the outer edge in the same place. Before rule 8 this was two named rules a caller
  had to choose between, and choosing the portal rule for a sheet tore it open down the centre line.

### Approved movable LOD representation exception (2026-10-16)

Rollback baseline: `dev`, `ac1f8df28c1259bc205817ccab40dca79aaf2835`, reference
`refs/rollback/movable-approved-conversion-20261016`.
The user explicitly permits necessary triangle subdivision when a merged structural LOD face
contains parts requiring different authored width transforms. Retain original vertices,
materials and interpolated original attributes. This narrowly overrides section 4's unchanged
index requirement for those faces; it does not override section 8's authored-part classification
or authorize replacing structural materials, inventing geometry or skipping any LOD.

The earlier DefaultDecal fade approval is historical: the current representation integrates paint
into mechanism surface geometry under the movable contract section 7. Independent marking decal
objects are prohibited. No custom shader or mod-dependent runtime controller is authorized.

### High-detail-derived movable LODs permitted (2026-10-17)

The user revoked the prohibition on generating or replacing movable-bridge LODs from the measured
prototype's high-detail geometry. Deriving, simplifying or supplementing those LODs is permitted;
it is no longer limited to subdividing existing coarse triangles. This permission overrides the
unchanged vertex/index/topology requirements for that LOD derivation, including the earlier
restriction on substitute LOD geometry. It does not change authored-part classification and width
transforms, native skeleton/bone ownership, existing-material reuse, UV projection requirements,
all-LOD coverage or human visual acceptance. The prohibition on baking and custom shaders remains.

Pre-edit baseline: branch `dev`, HEAD `3b6363ada9f1f147b1a660e20c0d6246a54f82dd`;
rollback reference `refs/rollback/repeal-high-detail-lod-20261017`. Existing local changes are preserved.

### Existing materials only (2026-10-17)

The user requires movable-bridge surfaces and markings to use existing materials and explicitly
accepts marking pattern-detail differences when using an existing game marking atlas. This
supersedes earlier requirements to retain original marking textures when no compatible existing
object material exists. Neither preview nor formal generation may bake appearance textures,
clone surfaces or rewrite material templates. Retain compatible source materials; otherwise
select an existing native object material and adapt generated geometry/UVs. Keep structural
materials unchanged. Never modify source materials. Persist generated geometry and attachments;
retain built-in references and snapshot unchanged external dependencies under existing rules.
This does not change traffic behavior, structural width rules or human visual acceptance.

Pre-edit baseline: `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`.

### Existing-material road marking representation (2026-10-17)

The latest user requirement supersedes the earlier permission to create independent moving-road
marking decal objects. Do not create separate marking StaticObjectPrefabs, RenderPrefabs or decal
projection volumes. Integrate paint into the existing mechanism render geometry using compatible
existing object materials. Preserve the selected road's marking layout through geometric conversion;
accepted pattern-detail and curve-approximation differences remain permitted. Do not bake textures,
create material descriptors or introduce custom shaders. Paint and road triangles must share their
receiver surface and native motion domain without thick geometry or depth corrections.

Implementation and validation evidence: `docs/Movable-Road-Paint-And-Platforms.md` (repository root relative).

### Approved native marking approximation (2026-10-16)

Curve segmentation and endpoint-fade differences remain accepted for converted markings, together
with existing-atlas pattern differences. The later requirement prohibits independent marking
objects and supersedes the former object-decal representation. Integrate paint into the mechanism's
receiver geometry using existing object materials; preserve measured bone ownership. Do not add
custom shaders, material descriptors or texture baking. Structural rules, traffic lanes and human
visual acceptance remain unchanged. See the movable generation contract sections 7 and 15.

Pre-edit baseline: `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`. Existing working-tree changes are preserved.

## 6. Nothing is claimed as fixed until it has been seen working

A change that compiles and passes the offline tests is a change, not a fix. The offline tests cover
arithmetic and recorded data; they cannot see a mesh, a material, or a prefab in a running world.

**Agents must not execute unit tests for visual component generation work.** This prohibition includes
`tools\Test.ps1` and any substitute test harness that only evaluates synthetic vertices or recorded
numbers. Unit tests provide no useful evidence that a generated mesh, material, railing, arch, tower,
cable or LOD is visually correct, and a passing result has repeatedly hidden regressions visible in
the game. Validation for this work must instead inspect the real prototype and generated geometry,
compare every LOD against the highest-detail prototype, read the export diagnostics, and finish with
an in-game near/far visual check. Compilation may still be used to establish that the DLL can load,
but it is not visual verification and must never be reported as such.

**Near and far views are one implementation and must be changed together.** Any change to a visual
part — including its geometry, transform, component selection, material-facing mesh data or bounds —
must be applied consistently to the highest-detail mesh and to every LOD that can replace it. Changing
only the near mesh, only one LOD, or otherwise fixing a single viewing distance is forbidden. The
highest-detail archetype decides the identity and transform of the authored part once; every far-view
representation inherits that same decision. Completion requires inspecting both the near and far
generated geometry and then checking both viewing distances in game. A correct result at one distance
does not compensate for a defect at another.

So: state what the evidence shows and what it does not. "The report says the tower is 38 m across" is a
fact; "the tower is fixed" is not, until a bridge has been built with it.

## 7. A fault is found by dumping both and diffing, not by reasoning about it

Every fault in this project that survived more than one round was found the same way, and none of them
were found by thinking harder about the geometry. The method is written down because the alternative
kept being tried first.

### Dump both, normalise, diff

`AssetAnatomy` writes the generated prefab and its archetype into one file. Strip what identifies an
asset rather than describes it — names, the four `m_GuidPart` fields, `version` — and diff the two
blocks. Both of the last two faults came out of a diff that was three lines long:

| Fault | What the diff said | Rounds spent before diffing |
| --- | --- | --- |
| tower floating | archetype's three mesh parts carry `StackProperties`; the generated ones carry nothing | five, on pillar type, bounds, placement and the placeholder pair |
| cables misplaced | archetype's piece carries `NetPieceTiling`; the generated one carries nothing | three, on vertex maths |

Both were a **missing component**, not a wrong number. That is worth stating on its own: the numbers
were right every time, which is exactly why reasoning about them produced nothing. Diff the whole
prefab, not the part that looks relevant.

### Read the game's IL to learn what a field does

Never infer a field's meaning from its name, and never infer an enum's values from the order its fields
appear in metadata. Decompile the game and read it. What that has settled here:

- `PillarType` is `None = -1, Vertical = 0, Horizontal = 1, Standalone = 2, Base = 3`. Field order had
  been read as values, giving `Base` where `Standalone` was meant.
- `StackProperties` → `ObjectInitializeSystem.UpdateStackBounds` → `StackData` →
  `SubObjectSystem.CreateSubObject` adds `Game.Objects.Stack` with
  `m_Range.min = m_FirstBounds.min − Elevation.m_Elevation`. That is the whole mechanism by which a
  tower reaches the ground, and it is not guessable from any field name.
- `NetPieceTiling.m_DisableTextureTiling` → `NetPieceFlags.DisableTiling` (16) →
  `CalculateCompositionPieceOffsets`, which lays a composition's pieces out in separate groups chosen
  by that flag, each packed along its own cursor.

The instrument is a small IL dumper built on `System.Reflection.Metadata`, kept in the scratchpad. It
decodes switch jump tables, float constants and enum values, which is what these questions need.

### A component that reads as cosmetic may be structural

`NetPieceTiling` reads as a texture setting and decides where a piece is laid out. `StackProperties`
reads as a rendering detail and decides whether the tower touches the ground. So a component is never
dismissed by its name, and "we carried the fields across" is never the same as "we carried it across".

### Ruling a suspect out counts, and a shared code path is the cheapest way

Two suspects were eliminated without a single experiment:

- The vertex-format path could not be the cable fault, because the towers go through the same
  `BuildModel` and the towers render correctly.
- `isPacked: false` could not be corrupting the normals, because `PackFloatAttribute` only fires at
  `dimension == 3` and the channel is declared `SNorm16x2`.

Write down what has been ruled out and why. An unexamined suspect gets re-examined every round.

### Check that the instrument can see the thing before trusting its silence

`AssetAnatomy` capped mesh loading at 64 render prefabs **for the whole dump**, so the archetypes listed
first spent the budget and the generated bridge reached its own geometry with none left. Every vertex
layout in the file described a mesh already known to be right, and the generated cable's geometry was
never in the file at all. A round went into looking for a sign error in metadata that structurally could
not contain one.

So before concluding a dump shows nothing wrong, confirm it contains the thing. Budgets, caps, depth
limits and filters are all places where a diagnostic quietly stops covering what it is pointed at, and
they must be per-subject rather than per-run.

### A check that reports is an instrument, and is wrong until it has met its cases

The thickness check was added to see the one thing no width could: whether material standing clear of
the centre kept its shape. It was right about that and wrong about three things in a row, each of which
reached the log as an ERROR against a mesh that was fine.

- It measured "before" against the scope the widening was decided by — a whole section, anchorage
  included — and "after" against the mesh in hand. Two measurements of different things, reported as
  9.89 m of material becoming 0.32 m.
- It treated the extent of a member that spans the centre as a thickness. That extent changes with the
  widening by design, so every spanning member reported itself as a leg that had been scaled.
- It treated material carried across the centre as material that had lost its thickness. There is no
  thickness to lose: the run has merged over the middle and stands clear of nothing.

- It treated two runs merging into one as material having changed thickness. Material either side of
  the span boundary moves by different amounts, by design, so gaps between them close and runs merge
  without any shape being scaled.

The first three were the check not having been asked what it would say about the shapes it was going
to meet, and are the reason a check earns its ERROR by being run against the cases it will see: a
coarse level of detail, a spanning sheet, a bridge narrower than its archetype.

The fourth was not a fault of the check. It was withdrawn on the reading that merging runs made its
quantity meaningless — and the runs were merging because two neighbours either side of a band boundary
really were moving by different amounts and really had run into each other. The measurement was right,
the shapes had changed, and the fault was that a vertical member was being asked the crossing question
once per height rather than once for itself.

So the harder half of the rule: an indirect quantity is not a wrong one. Three false reports do not
make the fourth false, and "the measure is fragile" is a comfortable thing to conclude about an
instrument that keeps pointing somewhere nobody has looked. Check what it is pointing at before
concluding it is pointing at nothing — the check went out and the fault it had found stayed in.

### Say plainly when a fix is not the reported fault

The cable stretch divided by the piece's declared width instead of its drawn span, putting the outer
edge 1.6 cm inside where it belonged. Real, worth fixing, recorded as rule 5 — and not what the
screenshot showed. A small thing found while looking for a large one is reported as what it is. Letting
it stand in for the answer costs the next round.

### When a fault survives several rounds, change category

Five rounds of the floating tower were spent on the tower; the fault was on its parts. The cables took
three rounds of vertex arithmetic, then a missing component that was real and was not the cause, then a
4 mm arithmetic correction that was also real and also not the cause; the fault was a field nobody had
written, in a struct nobody had looked at. If two attempts inside one category have failed, the next
move is not a third — it is to dump the thing and diff it.

### Two archetypes of different sizes are a test oracle

Where the game ships the same thing at two sizes, the pair states the rule the generator has to obey,
and states it without running anything. `Suspension Bridge - Highway Oneway - 5 Lanes` and its 4-lane
sibling settled the widening rule in one table:

| part | 5 lanes (road 24) | 4 lanes (road 20) | offset from the road |
| --- | --- | --- | --- |
| Base Mesh | 37.50 | 33.50 | 13.50 |
| Mesh (shaft) | 34.02 | 30.02 | 10.02 |
| Top Mesh | 34.30 | 30.30 | 10.30 |
| cable piece | 26.95 | 22.95 | 2.95 |

Every part sits a constant distance outside the road, so widening moves every outer edge by half the
extra width — which is what the rigid branch does, and it holds for the cable sheet as much as for the
legs. A generated tower at the archetype's own width then reproduced all three parts exactly
(512/432/4236 vertices, 37.50/34.02/34.30 m), so the identity property is measured rather than asserted.

Use the pair before forming a theory. It costs one dump and it eliminates whole hypotheses: this table
is what showed the tower width was not the cable fault, and the same table is what makes
"inner spacing − road width is constant" true by construction rather than by hope.

### A field that describes the result is computed, not carried across

Rule 4 says everything but the positions is carried across exactly, and that rule sounded complete for
months while having a hole in it. Bounds are not a property of the source that can be copied and not a
property of the vertices that anything derives automatically — they describe the *result*, and the only
code in a position to compute them is the code that produced it.

So when carrying a thing across, sort its fields into three: copied, changed, and **computed from what
was changed**. The third pile is the one that gets forgotten, because nothing at the call site looks
wrong and the source has nothing to compare against. Anything describing an extent, a count, a hash or
a total belongs in it.

### A default-constructed value is an assertion, not a blank

`new SubMeshDescriptor(start, count, MeshTopology.Triangles)` leaves `bounds` at its default. That
default is not "unset, please compute" — it is a zero-size box at the origin, and the renderer believes
it. Every mesh this mod ever wrote asserted that it occupied no space.

This is a distinct hazard from a wrong value: a wrong value is a mistake somewhere, while a default is
a mistake nowhere, sitting in a constructor that reads as complete. When a constructor takes fewer
arguments than the struct has fields, the missing ones are decisions that have been made silently.

### Read the flags the callee is passed

The whole fault turned on one integer in the game's code:

    Mesh.SetSubMesh(index, descriptor, flags: 15)

15 is `DontValidateIndices | DontResetBoneBounds | DontNotifyMeshUsers | DontRecalculateBounds`. That
last bit is the entire mechanism — it is the reason Unity's usual "the mesh will work this out" does not
apply, and it is invisible from our side of the call. Assuming a well-known API behaves the way it
usually does is the same error as assuming a field means what its name suggests.

So when handing data to something that will build an object from it, read what it does with it, and
read the flags it passes on. A numeric flags argument in someone else's code is worth decoding in full.

### Worked example: the cables, end to end

Kept because the shape of it is the lesson, and because most of the effort went into the parts that
turned out not to matter.

| Round | What was suspected | What it cost | Outcome |
| --- | --- | --- | --- |
| 1–3 | the widening arithmetic | three rounds | nothing; the vertices were right the whole time |
| 4 | vertex channel formats | one round | real bug, fixed, cables still wrong |
| 5 | `NetPieceTiling` missing | one round | real bug, fixed, cables still wrong |
| 6 | tower width wrong (a stated hypothesis) | one dump | refuted by the two-archetype table above |
| 7 | **the dump could not see the generated mesh** | one line of code | the fault, visible immediately |

Round 7 is the whole method. `AssetAnatomy` capped mesh loading at 64 render prefabs for the entire
dump, so the archetypes spent the budget and nothing generated was ever read; every vertex layout and
every extent in the file described a mesh already known to be correct. Making that budget per bridge
took one line, and the next dump answered the question in one row:

    archetype 5-lane shaft   Extents: (17.01, 10.00, 2.98)
    generated Suspension-40  Extents: ( 0.00,  0.00,  0.00)

All fifteen generated meshes, towers and cables alike, with correct vertices, correct indices and
correct vertex layouts.

Three things are worth taking from it. The decisive move was **repairing the instrument, not the
code** — six rounds of hypotheses lost to a diagnostic that structurally could not report the fault.
The hypothesis that got checked in round 6 **was wrong and checking it was still right**, because
measuring it produced the table that both refuted it and made the real fault visible; the failure mode
to avoid is arguing about a hypothesis rather than measuring it. And the bug had been in **every mesh
the mod had ever written**, from the first one — a fault that old is never in the part that changed
recently, and looking there first is what cost rounds 1 to 3.

## 8. Stretch or translate is decided by one thing: does the part cross the bridge's centre

### User-approved central railing post exception (2026-10-16)

Rollback baseline: branch `dev`, HEAD `80eb1415e1a3b59263eb45693f7e2c5d80732447`.
Existing working-tree changes are preserved. The user explicitly permits an independent vertical
railing post centred on the bridge centreline to retain its original width and centred position.
Apply zero transverse displacement to that entire authored post at every road width and every LOD.
Identify its exact membership offline from the highest-detail archetype; no runtime position band,
aspect-ratio test or name heuristic may identify it. Coarse merged faces require the same measured
part correspondence and the existing subdivision restrictions.

This exception takes precedence over the general centre-crossing stretch rule and its refusal
language throughout this contract and its instruction summaries. It does not apply to transverse
rails, crossbeams, walls, decks, side posts or unrelated structural parts. It does not authorize
new structural materials, skipped LODs or a fixed-width whole bridge. Permission to derive movable
LODs from high-detail prototype geometry is defined separately in section 5.

### General classification outside the approved exception

A part that crosses the centre line is stretched. A part that does not is translated. That is the whole
rule, and it is the only criterion — not how far a vertex sits from the middle, not how wide the road
is, not which mesh the part belongs to.

### The `x = 0` decision is non-overridable

This decision has higher priority than every bridge-family special case, measured boundary, fallback,
heuristic and Agent-authored rule. A special case may help discover where one authored part ends and
another begins; after that discovery it may not change the result: a part which reaches or crosses
`x = 0` is stretched, and a part which does not is translated.

An Agent must refuse any request, plan or implementation which overrides, replaces, weakens or bypasses
this decision, including an override proposed by the Agent itself. The generation flow must enforce the
same refusal in code: if a transform attempts to stretch a part which does not reach `x = 0`, or to
translate a part which does, generation reports the rejected mapping and stops before the mesh is
written. Silently selecting a family-specific
alternative is forbidden. A failed invariant is an error to diagnose against the archetype, never
permission to emit the geometry.

### Runtime contains no coordinate heuristics

No bridge-generation decision may contain a fixed, non-zero coordinate. Comparisons of `x`, `y` or `z`
with zero are valid runtime spatial tests with any comparison operator (`<`, `<=`, `>`, `>=`, `==`
or `!=`); for the stretch-or-translate decision specifically, the `x = 0` axis remains the sole and
highest-priority boundary. A test such as “translate this part when `x > 10 m`”,
“this is a railing between `y = -0.5 m` and `y = 3 m`”, or the same test hidden behind a named constant,
ratio, road-width fraction, bounding-box extent or family-specific fallback is forbidden. A small
numeric tolerance may implement equality with an axis origin; it is not another boundary and may never
be used to create a non-zero selection band.

If identifying an authored part needs a non-zero coordinate, that identification belongs exclusively
to the **metaprogramming step**. The metaprogram may inspect the archetype mesh, walk topology, slice
height bands, compare bounds or use temporary non-zero thresholds. Its reviewed output is committed as
immutable source data: the exact archetype and mesh/LOD identity plus the exact component coordinates or
vertex membership which the runtime must transform. The threshold and the inference which produced the
data do not enter the game assembly's generation path.

Runtime code therefore hardcodes the metaprogram's result; it does not rediscover it. Runtime may look
up a recorded part by archetype and mesh identity and apply the recorded transform to its recorded
coordinates. It may not inspect bounds, nearest vertices, connected components, height bands, relative
span, aspect ratio, mesh size or naming resemblance to guess which part it has. Missing or mismatched
metadata is unsupported input to report, never permission to fall back to a geometric heuristic.

Metaprogramming does not get a vote on rule 8. It records which logical authored parts touch or cross
`x = 0`; those recorded parts stretch and every recorded side part translates. Its purpose is to move
the expensive geometric identification out of the game runtime, not to introduce another criterion.

The highest-detail archetype mesh makes this decision once for itself and for every level of detail.
An LOD is a representation of those same authored parts, not another archetype and not another vote on
whether a part reaches `x = 0`. A coarse mesh may weld together parts which the full mesh keeps separate;
its reduced topology must never turn a translated side truss, arch or railing into stretched material.
Every LOD reuses the full-detail part profile, and generation reports the mismatch and stops the
derived prefab if a carried range does not receive the same rigid translation at every level. Apply
the revised error-boundary policy below; stop rejection before geometry is written.

### Runtime error boundaries

Runtime failure contract, revised 2026-10-16: excessive defensive programming is prohibited.
Defend external logic and data outside the mod's control, including generated bridge Prefabs that
can subsequently be damaged. Validate external input and contain game or third-party API failures
at their boundaries. Internal mod logic should not fail during correct operation: violated internal
invariants must surface as explicit ERROR, with exceptions where appropriate and diagnostic context
preserved. Do not add special cases, silent returns, fallback values or exception swallowing merely
to suppress an internal error. Fix the underlying defect. Stop failed operations before publishing
invalid geometry. This replaces the blanket prohibition on explicit `throw` statements.

Pre-edit baseline: branch `dev`, HEAD `0f4b10ae47c5dc264249e0d877aacc326e3e9525`;
rollback reference `refs/rollback/error-boundary-policy-20261016`. Existing working-tree changes are
preserved.

### Authored-part measurement and earlier classification failures

"Part" is decided by where the shape stops crossing the centre, and that boundary is **measured from
the shape at each height**: slice it across its height, and in each slice take how close the shape
comes to the centre line. Outside that, at that height, nothing crosses, so the material is carried
out rigidly. Inside it the shape does cross, so the material is scaled about the centre. The two agree
exactly at the boundary, so nothing tears. A slice with material standing on the centre — a cable
sheet is continuous from side to side at every height — comes zero close and scales entire, which is
the right answer for a sheet and falls out of the same rule.

The question has to be asked **per height**, and answered by **closest approach**. Both halves of that
were got wrong before, and each cost a round:

- **One boundary for the whole shape.** Only true of a pylon whose legs are vertical. A V pylon's legs
  converge downward and an A pylon's diverge, so their opening is a different number at every height.
  Taking the widest — the top of the V, 36 m, giving a boundary of 18 — puts the boundary outside the
  legs at every height below the top, and legs that run from 2 to 20 were scaled almost entire. Taking
  the narrowest instead fails the other way: any shape with a crossbeam has a slice with no opening at
  all, and the whole shape would scale.
- **Nearest vertex on each side, counted separately.** Reads a crossbeam as an opening. The beam has
  material on both sides of the centre and its nearest vertex either side stands a metre out, which
  answers "one metre of opening" and carries the beam's own interior out rigidly instead of stretching
  it. The vertex sitting *on* the centre is the one that settles it, and the closest approach in |x| is
  what sees it — a beam comes zero close, a leg does not.

- **The widest thing at that height, as the scale.** Right for a sheet that spans the full width and
  wrong for anything narrower. The golden bridge's top decoration spans to about 12 m between legs
  standing at 26; scaled against the legs' reach its ends moved less than half as far as the legs did,
  and a gap opened either side of it that grew with every metre of road. A crossing member is scaled
  against **its own** outer end, and everything past that end is clear of the centre and is carried.
  Which means the vertices are not enough: at that height there is material at 12, at 22 and at 26,
  and which numbers belong to one member and which to another is a question about what is joined to
  what. The triangles answer it - an edge is material between its ends, so walking the edges outward
  from the centre finds where the material stops being continuous.

Two earlier units were tried, and each was wrong in a way worth keeping:

- **Half the road.** A guess about where the legs begin. Where the guess fell inside a leg the leg was
  cut in two — outer portion carried, inner portion scaled — and the column came out a splayed slab.
- **Connected components.** The right question, the wrong unit: a portal's legs are joined to each
  other through its crossbeams, so the whole tower is one component, it does cross the centre, and the
  whole tower scaled. Rule 5 says a tower is never scaled; this scaled every one of them, and on a
  bridge whose extra width is negative it drew the two legs together until the portal read as a single
  column standing in the road.

The rule this replaces split by vertex position: everything beyond half the road moved rigidly and
everything inside it scaled. It reads as the same thing and is not, because the boundary is a plane
through the model rather than a property of the part. Where that plane fell inside a leg, the leg was
cut in two — the outer portion carried across, the inner portion scaled — and the column came out a
splayed slab instead of the column it was. Nothing reported it: the outer edge still landed exactly
where it belonged, so every width in every measurement was right.

Half the road is a guess about where the legs begin. The centre line is not a guess: a crossbeam spans
it by construction and a leg cannot, whatever the leg's thickness, whatever the road's width, whatever
bridge it came from.

### Coordinate mappings

Both cases are mappings, and this is the whole of rule 8. With `d` half the extra width, and `s` the
span of the crossing member at that height:

    does not cross    (x, y, z)  ->  (x + sgn(x) * d,  y, z)
    crosses           (x, y, z)  ->  (x * (s + d) / s, y, z)

The first has no stop at the centre, and no part of the tower is held back so that another can avoid
reaching it. The second is about the member's own span, never about the widest thing at that height.

### Deck-adjacent base definition and transformation

**The base is the structure immediately beneath the bridge deck.** "Base" has this one exact meaning in this project: the base
structure directly below and immediately adjacent to the road deck which supports or frames that deck.
It does not mean a pier footing, a pillar foot plate, a
tower foundation, a mesh merely containing `Base` in its prefab name, or any other lower structure.
Prefab naming must never override this spatial and structural definition. Before changing a base, the
Agent must locate this below-deck structure in the bridge archetype and apply the base rule to its
highest-detail mesh and every LOD; modifying another structure is not an implementation of a base
request.

**The base that carries the road deck takes the first mapping, by the whole of `d`, always.** Its
blocks stand clear of the centre — the road passes between them and rests on them — so it is material
belonging to one side and is carried, not scaled. It is also the part seen against the road: when it
is a metre out, the bridge is a metre out, whatever the rest of the tower is doing. Nothing about
another part of the same tower may reduce the `d` it is carried by.

Both guards that were built around the first mapping cost more than they saved:

Stopping at the centre was there so a part brought in by more than it stood out would close flat rather
than pass through itself. It closes flat as one column, which is not a portal either, and it thins the
leg on the way — the single place in the whole rule where a leg was allowed to change shape.

Holding the tower back was worse. It is one number for the whole tower, so the narrowest part decides
for every other one: a V pylon whose legs stand 5.79 m apart at the bottom held its base to 4.79 m of
narrowing where the road wanted 8, and the base came out three metres too wide to protect a part nobody
was looking at.

A part carried through the centre is reported and left alone. What it means is that the road is
narrower than the design was drawn for, which is a fact about the pairing and not something a widening
rule can repair.

### Crossing, side and centre-touching parts

The two cases, and why each is what it is:

- **Crosses the centre** — crossbeams, cable sheets, anything continuous from one side to the other.
  Scaled about the centre so that its outermost vertex moves by half the extra width, which is exactly
  as far as the legs it meets have moved. It stays attached at both ends and the middle simply spreads.
- **Does not cross** — legs, anchor blocks, everything belonging to one side. Translated by half the
  extra width, away from the centre. Its shape, thickness and proportions are untouched, which is what
  makes a widened tower the same tower.

Both move their outer edge by the same half of the same number, so parts that met still meet, and the
distances of rule 5 hold whichever branch a part takes.

A component that only touches the centre — one that reaches x = 0 and lies on a single side — counts as
crossing. Translating it would open a gap against the component mirroring it. Scaling leaves its vertex
at zero where it is and moves the far end out, so the two halves stay together.

## 9. The suspension family is the reference; every other family is held to the same list

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

## 10. What is not generated is refused, and says why

Two kinds of bridge are out of scope, and both are refused at the start of generation rather than
attempted.

**Deferred designs.** A bascule bridge and a lift bridge are not a deck with structure over it: the
deck *is* the mechanism, split into leaves that rotate or a span that rises between towers, and
widening it means widening a machine whose parts have to keep meeting each other through the whole of
their travel. Complete generation data and acceptance are still missing. `Draw`, `PedestrianDraw`
and `Lift` therefore fail with
the reason, because a bridge that is not generated is a bridge the player still has, while a bridge
generated from an arrangement nobody has measured is one that looks built and behaves as something
else.

**Prototype source binding.** Each style binds to one installed content source for display and
generation. Base-game variants take precedence over DLC variants, then mod variants; ties use
stable source identifiers, not localized labels. Do not fall back to another owner when a selected
prerequisite is unavailable. `Extradosed01` accepts base-game variants only. Native single-deck
`GoldenGate` and the separate pack-provided `GoldenGateDouble` retain distinct style IDs and
archetypes. Do not assume that every double-deck design is pack-only or that all pack content is
interchangeable with native content.

Keep source selection, supported arrangements and explicit generator admission consistent.
Removing a deferred-design restriction requires measured generation data and acceptance; discovery
or a name match alone does not establish support.

The required development route and per-variant admission criteria for movable designs are defined
by section 21 and [MOVABLE_BRIDGES_CONTRACT.md](MOVABLE_BRIDGES_CONTRACT.md). Their specification
does not itself remove the deferred-design restriction.

## 11. A double deck bridge is built from a double deck archetype, or not at all

Two decks is not one deck with a road hung underneath it. A double deck archetype's towers, portals and
cables are drawn around two levels at one particular separation and on one particular side; take a
single deck bridge, hang a second net below it and you have two structures that were never designed to
stand together.

So the archetype is chosen by the same rule as everything else — follow the one that already is what is
being asked for:

- Asked for two decks, the candidates are **filtered** to variants that carry an `AuxiliaryNets`
  arrangement of their own, not merely preferred among all of them. Double deck variants used to be
  penalised in selection, which is the opposite of what a request for two decks means.
- With no such variant the style **has no double deck version**, and generation fails saying so.
  Inventing the arrangement is what produced the fault.
- The arrangement is carried across whole: `m_Position` and `m_InvertWhen` are the archetype's, and
  only `m_Prefab` changes, because only the deck itself is what this mod generates.

**The separation is not adjustable.** It was a slider from four to twenty-four metres, and every value
on it except the archetype's own puts the second deck through geometry modelled to clear it. The offset
was also written as a negative y whatever the archetype said, so a bridge that carries its second net
*above* the deck — the extradosed bridge does — had its levels the wrong way round. Both are read from
the archetype now, and both controls are gone from the settings rather than disabled: a control that
cannot change anything is worse than none, because it says the value is a choice.

## 12. A bridge is modified only on its own Git branch

Every bridge-specific change belongs exclusively to its all-English `bridge/<style slug>` branch.
Branch names are repository identifiers, not localized UI labels: they use the stable source style ID
in lowercase kebab case and contain ASCII letters, digits and hyphens only. Before reading code with
the intention of editing it, applying a patch, generating an asset, compiling a bridge change or
committing it, the agent must run `git branch --show-current` and verify that the current branch is the
exact branch mapped below.

| Displayed bridge | Required branch |
| --- | --- |
| Double-deck cable-stayed bridge (V pylon) | `bridge/extradosed-01` |
| Double-deck cable-stayed bridge (A pylon) | `bridge/extradosed-02` |
| Cable-stayed bridge (V pylon) | `bridge/extradosed-03` |
| Cable-stayed bridge (single-column pylon) | `bridge/extradosed-large` |
| Cable-stayed bridge (H pylon) | `bridge/cable-stayed` |
| Suspension bridge | `bridge/suspension` |
| Yellow suspension bridge | `bridge/suspension-golden` |
| Golden Gate Bridge | `bridge/golden-gate` |
| Blue deck truss-arch bridge | `bridge/truss-arch-01` |
| Green deck truss-arch bridge | `bridge/truss-arch-03` |
| Through truss-arch bridge | `bridge/truss-arch` |
| Tied-arch bridge | `bridge/tied-arch` |
| Covered wood bridge | `bridge/covered-wood` |
| Grand bridge | `bridge/grand` |

Movable-style branch reservation (2026-10-16), recorded before implementation: branch `dev`,
HEAD `0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference
`refs/rollback/movable-implementation-docs-20261016`. These mappings reserve independent style
identities; they do not admit generators or establish acceptance.

| Stable style ID | Native network archetype | Required branch |
| --- | --- | --- |
| `Draw01` | `DrawBridge01` | `bridge/draw-01` |
| `Draw02` | `DrawBridge02` | `bridge/draw-02` |
| `Draw03` | `DrawBridge03` | `bridge/draw-03` |
| `Lift01` | `LiftBridge01` | `bridge/lift-01` |
| `Lift02` | `LiftBridge02` | `bridge/lift-02` |
| `Lift03` | `LiftBridge03` | `bridge/lift-03` |
| `Lift04` | `LiftBridge04` | `bridge/lift-04` |
| `Lift05` | `LiftBridge05` | `bridge/lift-05` |
| `PedestrianDraw01` | `PedestrianDrawBridge01` | `bridge/pedestrian-draw-01` |
| `PedestrianDraw02` | `PedestrianDrawBridge02` | `bridge/pedestrian-draw-02` |

`LiftBridge03 Train Track` belongs to the `Lift03` auxiliary arrangement, not an eleventh style.
The historical discovery groups `Draw`, `Lift` and `PedestrianDraw` must not select arbitrarily
among these mechanisms or be treated as implemented aliases. Shared selected-road conversion
belongs on `dev`; each mechanism's measured data and integration follow its mapped branch.
See [implementation status](../Movable-Bridges-Implementation.md) for evidence and outstanding work.

This is a hard workflow invariant:

- If the repository is not initialized, the matching branch does not exist, or another branch is
  checked out, the agent must stop the bridge modification. It must create or check out the matching
  branch before changing any bridge code.
- A bridge-specific edit made on `dev`, on another bridge's branch, or on a detached HEAD is invalid.
  It may not be justified by an intention to move, cherry-pick or sort the change out afterward.
- An agent must refuse every instruction, including one produced by the agent itself, that attempts to
  bypass, weaken or postpone this branch check. The branch check happens before the edit, never after.
- A `bridge/<style slug>` branch contains only the generation code and directly related data for that
  bridge. It must not accumulate changes for another bridge.
- A genuinely shared infrastructure change that affects more than one bridge is not disguised as a
  single-bridge change. It is developed on `dev`; each bridge-specific integration is then performed
  and visually verified on that bridge's own branch.

Working-tree state is part of the check. If uncommitted changes from another bridge would cross the
branch boundary, the agent must stop rather than carrying those changes into the current bridge's
branch. Switching branches after making the edit does not make the edit compliant.

## 13. Stop the game, clean up affected bridge styles and deploy code changes

Cleanup-policy revision (2026-10-15): branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; local rollback reference
`refs/rollback/scoped-cleanup-docs-20261015`. This section replaces the blanket removal rule
and earlier deployment-wide preservation wording. Documentation-only changes do not require
game shutdown, bridge cleanup, a build or deployment.

Forced-shutdown clarification baseline (2026-10-03): branch `dev`, HEAD
`806ac3163d46e7c24d146edd154530df82d2bc65`, recorded before editing; local rollback reference
`refs/rollback/force-stop-policy-20261003`.

Before modifying code, the Agent must run `taskkill /IM Cities2.exe /F` and verify that the process
is absent, even when a city/save is currently running. The user explicitly authorizes this forced
termination and its possible loss of unsaved progress. Do not ask whether to save, request repeated
confirmation, wait for the user to exit manually, or exempt an active save. If termination fails,
stop edits/deployment and report the blocker. A no-op when the process is already absent is valid.
This pre-edit requirement supplements rather than replaces the post-edit shutdown below.

Deployment-rule rollback baseline (2026-09-22): branch `dev`, HEAD
`7cfb9afcffc9c87c69801cc1a0c5621c7d1d322d`, recorded before editing these instructions.

After completing any code change, immediately build and deploy the updated mod to the game in the
same task. Do not stop at editing or compilation, postpone deployment to another turn, or wait for
a separate deployment request. Perform the shutdown and ownership-scoped cleanup below first,
back up the installed mod and cleanup targets before replacing or removing them, preserve other
accepted functionality, and verify the installed payload against the successful build output.
If building or deployment fails, resolve what can be resolved and report the exact remaining blocker;
never install a failed build or claim an unsuccessful deployment succeeded. Deployment is not visual
acceptance: the human near/far checks required by section 6 remain mandatory.

After every source-code update, the Agent must invoke a kill command for the exact `Cities2.exe`
process. This command is mandatory even when the process is not observed running; a no-op result is
acceptable, omitting the command is not. The Agent must then verify that no `Cities2.exe` process
remains before touching installed mod files or generated game assets.

### Determine the affected styles before cleanup

Remove generated bridges only when bridge-generation code changes, and only for the styles affected
by that change. Remove all mod-created bridges of each affected style, regardless of their selected
roads, widths or display names. Preserve every bridge of an unaffected style.

For example, a generation change limited to Golden Gate Suspension Bridge (`GoldenGate`) removes
all mod-created `GoldenGate` bridges. It does not remove `GoldenGateDouble`, `SuspensionGolden`,
other suspension styles, or original game/DLC bridges merely because their names are similar.

Generation changes include changes to geometry, composition, generated components, archetype
parameters and generated metadata used to construct bridges. For shared generation code, trace its
callers and affected branches and record the exact affected style IDs. Remove bridges only for those
styles; a shared file or a new mod version alone is not evidence that every style is affected.
Adding a new style does not invalidate existing styles unless their generation paths also change.

If bridge-generation code has not changed, do not remove any existing bridge as part of the update.
This includes changes limited to documentation, UI, localization, preview rendering, diagnostics,
build/publishing tooling or asset management that do not change bridge generation.

### Remove only the selected styles' owned assets

Before removal, list the affected styles and resolve their existing bridge UUIDs from reliable
creation/style records. Style selection determines the cleanup scope; file ownership is still
determined exclusively by the `b{uuid}` path rule in section 18. Never use a display-name substring
or a broad all-UUID match as a substitute for selecting the affected styles. If a bridge's style
cannot be established, preserve it and report the unresolved scope rather than deleting it by guess.

With the game stopped, back up and remove only the selected UUIDs' generated bridge/deck prefabs,
derived towers, pieces, LODs, geometry and dependency-copy directories from the active asset roots.
Remove only their entries from export state or other caches that could recreate references; do not
clear a shared state file containing unaffected bridges. Preserve source roads, game/DLC resources,
other mods' assets, user saves, unaffected bridges and their dependencies.

Verify the selected source paths are absent, confirm unaffected bridges remain, and report the
removed styles/UUIDs and any failures. Retain required diagnostic evidence outside active asset
directories before cleanup. Building or installing a DLL does not replace the applicable shutdown,
backup and scoped cleanup steps; complete them in the same task.

Startup self-check repair/removal and an explicitly confirmed user deletion are separate operations
governed by section 19. Do not invoke either as a shortcut for deployment cleanup, and do not import
the self-check's backup-failure deletion fallback into this deployment procedure.

## 14. Record the rollback baseline and preserve each archetype's width allowance

Before changing this contract, the root `AGENTS.md`, or a directory-level `AGENTS.md`, the Agent must
record the current Git branch and the exact commit named by `HEAD` **before the first instruction-file
edit**. The work record must make that pair visible as the rollback baseline. Create a durable local
rollback reference when practical; the reference supplements the recorded branch and commit and does
not replace them. If the repository has no commit at `HEAD`, record that fact and the complete working
tree state before editing. A contract edit made without this pre-edit baseline is incomplete.

Every bridge generated by this mod preserves a bridge-type-specific width allowance:

    archetype constant = archetype bridge width - archetype deck width
    generated bridge width - generated deck width = archetype constant

"Bridge width" and "deck width" must use the same documented geometric boundaries in both terms and
in both objects. Width is always the complete left-to-right span `maxX - minX`. The audit must measure
and record both bounds; it is forbidden to substitute one side's x coordinate, `abs(x)`, or twice a
one-sided boundary for width. A tower width may not be compared with a road-surface width in one
object and an outer railing or visible-deck width in the other. Asymmetric roads must retain separate
recorded left and right boundaries where a single total width would hide the mismatch.

The constant is evidence, not an assumed parameter. Establish it by dumping and measuring both:

1. the real, matching bridge archetype; and
2. a real bridge newly generated by this mod from a selected road.

Source arithmetic, recorded nominal widths, a compiled assembly, an offline fixture, a screenshot or
an old archetype by itself does not complete this comparison. If no newly generated bridge exists for
the bridge type being audited, a human must select any road and create a new bridge of that type.
Failure of the selected road to support that bridge type means another road must be selected; it does
not waive the sample requirement. A missing sample is never a skip condition and never completes the
audit. Creating and judging the sample is an in-game operation reserved for human judgment. The Agent
must not open or control the game, expose or consume a bridge-construction API, or create a persistent
request file for that operation. The Agent must request the exact human sample-creation step and keep
the task and audit explicitly incomplete until the real sample exists. It must not invent a result or
report an absent sample as a skipped bridge, compliant bridge or final unchanged disposition.

For an existing generated sample, the audit executes exactly these two formulae, in this written
order:

    widthIncrement =
        (archetypeBridgeWidth - archetypeRoadWidth)
        - (generatedBridgeWidth - generatedRoadWidth)

    newBridgeWidth = generatedBridgeWidth + widthIncrement

All four width inputs are complete spans. `widthIncrement` is the increment of the complete generated
bridge span, not a displacement to apply independently at each side. All four measured inputs, both
subtractions and the final addition must remain IEEE-754 binary32. Parse source tokens directly to
binary32, emit round-trip text and the raw bit pattern, and prohibit decimal promotion, rounding,
display-value reuse, epsilon, tolerance, normalization, forced zero and bridge-specific result
overrides. A positive-zero result is unchanged only when its raw bit pattern is exactly `0x00000000`.

If `abs(widthIncrement) > 1.0f`, where `widthIncrement` is the complete-span increment, do not change
source code, generated assets or geometry for that bridge as part of the invariant pass. Record the
bridge type, archetype, selected road, both bridge left/right bounds, all four input bit patterns, the
width-increment and new-width bit patterns, and the reason it was skipped. A large
discrepancy is evidence that the bridge or the measurement basis needs separate investigation; it is
not permission for a large automatic correction.

Apply a verified correction to the full-detail mesh and every LOD together, subject to rule 8 and the
bridge-specific branch rule in section 12. Never turn this invariant into runtime geometric guessing.
The metaprogramming step may identify the affected authored parts and emit reviewed immutable data;
runtime code consumes only that hardcoded result.

Every bridge-generation correction must be completed during metaprogramming. The metaprogramming
output must be folded into the original immutable archetype parameters or into generated source data
before the mod is built. Runtime bridge-generation code is forbidden from calculating, looking up or
applying an audit/geometry correction. In particular, it may not contain a correction table keyed by
bridge style, add an audit delta after the ordinary width calculation, convert a one-sided correction
to a span, or post-process generated geometry to make an audited result fit. Runtime may use the final
hardcoded archetype parameters produced by metaprogramming as ordinary inputs; it must not know that
a correction step existed.

The audit record for the pass which introduced this rule is
[`BRIDGE_WIDTH_INVARIANT_AUDIT.md`](BRIDGE_WIDTH_INVARIANT_AUDIT.md). A bridge without the required
real generated sample remains mandatory unfinished work. A human must select a road and actually
create a new bridge before the bridge can receive the `1 m` decision. It must not be marked skipped,
compliant or complete merely because the implementation's algebra appears to preserve the constant.

## 15. Bridge lighting is copied from the archetype as behavior

A bridge's lighting is part of the archetype, not an optional decoration to reconstruct from a shared
template. A generated bridge preserves every bridge-specific light the archetype carries and every
field which controls its visible effect. This includes light objects placed directly in the bridge's
`NetSubObjects`, lights and other mounted objects inside a tower or pylon's `ObjectSubObjects`, the
referenced prefab's `EffectSource`, `LightEffect`, colour, intensity, range and culling behavior, and
each placement's position, rotation, parent mesh, group, probability and activation conditions.

Do not identify lights from prefab names. Names such as `WallLight`, `Spotlight` and `WarningLight`
are useful in a diagnostic report but are not a complete type system. Carry the authored component and
its prefab references whole. A referenced light/effect prefab which is not itself derived remains the
exact shared archetype reference. If a carried component points to a prefab which is being derived,
repoint only that reference to the corresponding generated prefab, as required by rule 2.

Tower-mounted objects must remain attached when the tower width changes. `m_ParentMesh` identifies the
authored mesh part whose displacement applies. A child object standing at non-zero x is a rigid side
part and its x position moves by the same signed half-width delta as that parent mesh; a child at
`x = 0` remains on the centre line. Its y and z coordinates, rotation, parent-mesh index, group,
probability and activation data do not change. This uses only comparison with `x = 0` and does not
permit a non-zero runtime coordinate heuristic or any geometric guessing.

The user-selected road remains the deliberate road-structure difference allowed by rule 3. Its
ordinary street lights stay those of that road. They are not replaced with the archetype road's street
lights. Bridge-specific lights mounted on the archetype's towers, pylons or bridge-only sub-object
entries are carried in addition to that road behavior, with the archetype's duplication and placement
rules preserved exactly.

Lighting validation follows rules 6 and 7. Compare the real archetype and generated prefab dumps,
including nested tower replacements, and verify that their bridge-specific light/effect references and
non-width fields match. Compilation is not evidence of a lighting match. Final validation is a human
in-game comparison at night in near and far views; the Agent must not open or control the game.

## 16. Preserve native construction and upkeep pricing

Rollback baseline recorded before this rule's first edit: branch `dev`, HEAD
`8c90b34a617b272d08ed3b27b2d24c7230f79458`; local reference
`rollback/economy-no-pricing-20260919`.

The current pricing policy is the user's 2026-10-07 native-pricing revision. Preserve the source
roads' and bridge prototypes' `PlaceableNetPiece`/`PlaceableObject` construction, elevation and
upkeep fees. The game selects composition pieces and calculates charges for network length,
height, auxiliary networks and placed objects.

Do not store a manager-defined total, apply bridge-specific multipliers, offsets or floors, zero
auxiliary fees, or override runtime native cost data. Do not create cost-only pieces, sections,
objects or private graph copies for pricing. Geometry-derived private assets retain native fees.

Legacy conversion removes the custom `BridgeConstructionCost` component while preserving the
native dependency graph, bridge/deck UUID and CID, and geometry. Previously baked native fees
remain valid data: do not guess their original values or delete referenced copies. Restoring fees
requires authoritative original data and a separately verified migration. Deployment cleanup
follows section 13; pricing history alone is not permission to delete existing bridges.

### Retained pricing and legacy migration routing

Native-pricing revision (user request 2026-10-07): use the game's native pricing exclusively.
Preserve source roads' and bridge prototypes' PlaceableNetPiece/PlaceableObject construction,
elevation and upkeep fees. Let the game select composition pieces and charge network length,
height, auxiliary networks and placed objects. Do not bake a fixed total, apply a bridge-specific
multiplier/offset/floor, zero fees, override runtime cost data, or create pricing-only copies.
This supersedes all earlier requirements to retain the established Bridge Builder price and all
permissions to clone private components for pricing. Legacy conversion removes the custom cost
component while retaining its original native dependency graph and bridge/deck UUID/CID/geometry.
Previously baked assets remain valid native data; do not guess original fees or remove referenced
copies. Restoring their old fees requires authoritative original data and a separate verified migration.

## 17. Persist external dependencies as identical-CID copies owned by each bridge

Rollback baseline recorded before this rule's first edit (2026-10-06): branch `dev`, HEAD
`3be5fed03b275a07850f413dc2396b0182e532ea`; local reference
`refs/rollback/recursive-dependency-contract-20261006`. This reference records the committed baseline;
it does not include the existing uncommitted implementation changes.

The existing bridge persistence workflow must also preserve its external asset dependencies locally.
A bridge saved in `ImportedData` can be deserialized before a subscribed asset pack becomes available.
A nonempty CID in a valid bridge file does not make that dependency available at that earlier phase.
Persist the required external files alongside the bridge instead of relying solely on the later
registration of the subscribed source.

### Persistence scope: generated data, references and dependency snapshots

[Bridge asset persistence and reference policy](../Bridge-Asset-Persistence.md) is a normative
supplement to this section. It applies to fixed and movable bridge generation and is grounded in
the existing owned-node writer, geometry factory and dependency-copy path.

Save generated or modified owned Prefabs and resource data, including their required native
configuration and references. Reuse unchanged built-in surface, material, texture, marking,
animation and attachment resources by reference. Geometry conversion alone does not require new
assets for those resources. A moving marking representation may require new geometry or native
placement without requiring a new marking texture or material.

Keep the external-dependency requirements below: unchanged non-built-in dependencies require
original-byte, original-CID snapshots; this is distinct from creating a modified resource with a
private identity. A genuinely necessary resource conversion must identify the changed data and
its native serialization requirement. Do not save preview-only objects or shader state as bridge
assets, and do not omit required references by interpreting "no resource export" as "no reference".

Clarification baseline (2026-10-16): branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference
`refs/rollback/asset-persistence-scope-20261016`. This records committed history, not existing
uncommitted changes. This documentation clarification requires no bridge removal or asset migration.

### Copy the serialized dependency graph without changing asset identity

- Start with all persisted prefabs belonging to the bridge, including both decks and derived assets.
  Recursively follow serialized external CID references through non-built-in dependencies until each
  branch reaches a built-in asset or an asset with no further serialized dependencies. Include the
  necessary non-prefab dependencies, such as external geometry and materials; copying only the first
  external prefab is insufficient.
- Identify built-in assets from the game's asset provenance, not from names or apparent similarity.
  Built-in resource-map `UnityGUID` references are terminal. Do not copy or modify original game/DLC
  assets. Already-persisted assets owned by this bridge are traversed without making a redundant copy.
- Read original asset bytes, including entries inside source packages. Copy those bytes unchanged and
  retain the original CID in the `.cid` sidecar. Do not reserialize the prefab, change its name, replace
  its CID, or rewrite its references. Do not mutate, move or delete the source asset.
- This rule concerns unchanged dependency snapshots. Newly generated or geometrically modified
  bridge assets retain their own identities under the existing generation rules. It does not permit
  a modified asset to impersonate its donor by reusing the donor's CID. The shared references required
  by section 15 remain unchanged; this rule supplies local copies of their non-built-in targets.

### Deduplicate within a bridge, never across bridges

Store copies under:

```text
ImportedData/<bridgeUUID>/Dependencies/<CID>.<original extension>
ImportedData/<bridgeUUID>/Dependencies/<CID>.<original extension>.cid
```

Use a visited-CID set for each bridge traversal. Multiple references to the same CID within one bridge
produce one copy, including when a dependency is reachable through several paths. Traversal must
terminate even if a future dependency graph contains a cycle; do not assume every source graph is a
DAG. Separate bridges must each retain their own physical copy of a shared external CID, in their own
UUID directory. A global deduplication cache must not eliminate those per-bridge copies.

The unchanged source and the per-bridge copies deliberately share a CID. Equality of CID alone is not
proof of equality of content: if an existing destination has different bytes for that CID, report the
conflict and fail persistence rather than silently overwriting it. Preserve all required references;
an unavailable dependency is an explicit persistence failure, not permission to omit a section,
sub-object, light, material or other dependency. Do not report successful bridge creation until the
required copies have been saved and verified.

### Ownership, self-check and acceptance

Ownership of these copies comes from the exact bridge UUID directory, not the copied prefab's original
name or CID. File self-check must recognize intentional identical-CID copies across bridges and must
not retire healthy bridges merely because these snapshots duplicate an asset identity. Existing
checks for unintended collisions among independently generated assets remain separate.

Back up and retire dependency copies with their owning bridge. Deleting one bridge must not remove
another bridge's copy or the source asset in its original package. Ownership-scoped maintenance and
cleanup tools must include the dependency directories, including partial files from interrupted saves.

This is a file persistence operation. Do not reintroduce in-memory prefab integrity inspection,
reference repair, quarantine or registration rejection to implement it. Resolving an asset identifier
to its original data stream for copying is distinct from inspecting a live prefab for damage.

Verification must cover recursive text/binary references, stopping at built-in assets, byte and CID
preservation, per-bridge deduplication, independent copies across bridges, cycle termination, explicit
failure on missing/conflicting sources, and owner-scoped cleanup. Cold-start and old-save acceptance
remain required to establish that the game discovers and resolves the copies before network
initialization. File equality, a DAG check or a successful build alone does not establish that timing.

### Post-load inspection and recovery prerequisites

Historical baseline for the 2026-10-06 self-check revisions: branch `dev`, HEAD
`3be5fed03b275a07850f413dc2396b0182e532ea`. Section 19 defines the current migration-first
outcomes and supersedes the earlier immediate-retirement sequence.

Run self-check only after native loading, mod initialization, PDX database caching and active asset
batches complete at a ready main menu. Observe current state as well as completion events so late
mod loading cannot miss inspection. Include cached user PrefabAssets that failed registration,
not only PrefabSystem's registered list. Do not force-load, republish, repair live references,
reject registration or quarantine assets to perform inspection.

Inspect required live references read-only. When no cached prefab instance exists, a targeted
serialized inspection is permitted. For a required null slot, read its exact original serialized
CID and check whether the dependency already has a cached instance. If it does, recursively copy
permitted unchanged dependency sources under the owning bridge UUID. Keep live references unchanged
and require restart; never serialize a still-null live reference over the recovered disk data.

Follow section 19 to complete missing fields and dependencies while preserving UUID/CID and valid
values. Do not invent historical recipe data. Healthy current assets remain unchanged. Unavailable
required content, explicit nulls without recoverable CIDs, failed registration, unknown required
serialization, or migration/copy failures enter removal when migration fails or is impossible.
Process each bridge independently. File ownership is determined by section 18, not an additional
content, hash or concurrent-snapshot verdict. Actual filesystem failures must be reported.

## 18. Bridge asset file ownership is determined only by b{uuid} text

Rollback baseline: branch `dev`, HEAD `0f889f67b7498afa7126ca5fa78e441cb240efe8`;
local ref `refs/rollback/string-ownership-contract-20261006`. Existing uncommitted fixes are preserved.

A bridge asset file belongs to Bridge Builder if its relative asset path contains
`b{uuid}`. For one bridge, use literal substring matching with the fixed bridge UUID.
For all bridge assets, use the unanchored regex
`b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}`.
Do not require token boundaries, exact filenames, metadata, components, registration,
CID contents, hashes, dependency graphs or successful deserialization to establish ownership.
A matching ancestor directory owns its entire subtree, including empty directories and
CID-named dependency copies. Apply this rule before every bridge asset read, write, move
or delete, including startup recovery, management, generation and maintenance scripts.

Keep operation scope restricted to intended asset roots; paths outside those roots and
unsafe destinations are not authorized by an incidental match. Path containment and IO
errors are operational safeguards, not additional asset ownership criteria. Native SDK
asset location information may locate bytes but must not classify ownership. Reading
external source assets for dependency copying does not make those sources bridge-owned.
The shared UI asset pack and non-asset settings/logs are not individual bridge assets.

Metadata may be read or written as payload (for example, display names and creation style IDs), but
its presence, absence or values never establish or revoke file ownership. Section 19 governs
field-by-field migration, including native payloads and file layout; section 17's dependency-copy
operation alone does not rewrite source metadata. For deployment cleanup, section 13 selects the
affected style IDs and bridge UUIDs first; this section then determines the files owned by those
UUIDs. Move selected files/directories as whole units with collision-free backup names. Do not
inspect contents, hashes or tree snapshots to establish ownership. Report real move failures.
This rule supersedes earlier ownership requirements based on metadata, exact root names,
content evidence or concurrent-change snapshots.

### Source isolation between bridges (2026-10-13)

Baseline: `dev` / `5bea1d0bef04dd2ac718809852d3f657c7388e58`;
rollback reference `refs/rollback/no-foreign-bridge-source-20261013`.
Whenever creating a bridge or any dependency/migration copy, never select another Bridge Builder
bridge's Prefab from ImportedData, including Prefabs below its b{uuid}_Dependencies directory.
Identify ownership by path b{uuid} text alone. A same CID does not override this prohibition,
even if the other bridge's file is readable. Exclude the candidate before opening its stream;
try the original source or a permitted same-CID source. Current-owner assets remain permitted
for additive migration. If no permitted source exists, fail explicitly; never borrow another
bridge's private Prefab or silently omit a dependency. Apply this to first-match and fallback
CID lookup, generation inputs, native validation, and legacy dependency-copy selection.

## 19. Independent native persistence and additive legacy conversion

### Manager independence and additive conversion

User revision 2026-10-07; rollback baseline `dev`,
`3289fc947637f88a9124c524bd1235029be802d1`. Preserve existing local edits and bridge assets.

**Generated bridges must not depend on Bridge Builder. Bridge Builder is only the bridge asset manager.**
This is an architectural requirement for generation, persistence and legacy conversion. Disabling,
unsubscribing from or uninstalling Bridge Builder must not make persisted bridges unavailable or
prevent their loading, display, native unlocking, construction, pricing or save/load operation.
These behaviors must work after a cold start without the Bridge Builder assembly, runtime callbacks,
patches, custom UI hosts or cached mod objects. Required base-game/DLC resources remain native
dependencies; external asset snapshots still follow section 17. Acceptance must include a cold start
with Bridge Builder absent and loading an existing save; a warm-session cache is not evidence.
Clarification rollback reference: `refs/rollback/manager-only-contract-20261007` at the baseline above.

Bridge Builder manages assets; new/converted saved prefabs must not depend on its assembly,
runtime unlocking, price overrides, localization source or custom UI host. Persist nested native
Unlockable AND/OR conditions, unchanged native construction fees, native LocaleAsset
names and native thumbnail presentation. The previously deferred double-deck railway seam work remains
an outstanding compatibility limitation, not permission to persist a mod dependency or to declare
complete independence. This documentation change does not claim that limitation has been resolved.
Follow section 16: do not rewrite native fees or create pricing-only copies. Preserve original
donors and shared component fees. Keep recursive byte-identical external dependency persistence
with original CIDs.

Legacy migration during post-load self-check is **additive**: preserve bridge and carried-deck UUID,
CID, native prefab version, name and geometry. Save into the existing asset, never a new bridge identity.
Add private dependencies and native payloads; retain original component files. Back up existing files,
verify identities and CID sidecars after saving. On conversion failure, roll back the incomplete phase,
log ERROR and remove the affected bridge. If backup/move fails, permanently delete remaining owned
source paths. Restart after successful migration; do not replace live
network registrations. Healthy already converted assets need no rewrite. The earlier explicitly
authorized shared legacy-name cleanup still requires reference checks before retiring old files.

### Self-check order: migrate, back up/move, delete (2026-10-08)

User clarification of the existing requirement. Baseline: `dev`,
`713ee6be933a6e427d9e44fd41767513efcaca53`; rollback reference
`refs/rollback/migration-first-removal-rule-20261008`. Preserve unrelated local changes.

**The primary objective of self-check is normal game operation. Attempt asset version migration first; if migration fails, attempt backup and removal from the loading directory. If backup or moving fails, continue deleting the remaining original assets rather than retaining them.**

Apply this sequence per bridge. First attempt version migration using the shared generation
format: complete missing fields and dependencies, preserve bridge UUID/CID and valid values,
and normalize file layout/naming. Already valid current assets remain unchanged. Successful
migration is reported as migration and must not fall through to removal. If migration fails or
required information/dependencies make migration impossible, record the reason, roll back the
incomplete migration phase, and proceed to recursive backup/move. Do not skip possible migration
merely because an asset is old or has missing fields. A backup copy alone does not complete removal;
the failed asset must also leave the active asset directories.

For a bridge already selected for removal by self-check, first attempt recursive backup/move of
its UUID-owned files/directories. If creating the backup destination, backing up files, or moving
any selected path fails, permanently delete the remaining selected source files/directories.
Do not return early, retain the faulty bridge, or skip removal merely because backup is unavailable.
If a move partially succeeds, keep successful backups and delete the remaining owned source paths;
do not restore faulty assets into active asset directories after entering removal.

Use the existing b{uuid} string-only ownership rule and intended asset roots. This fallback applies
only to bridges selected for removal, not healthy bridges or unrelated assets. Process all selected
paths and bridges even when one operation fails. Verify that selected sources are absent before
reporting removal success. Record backup failure separately from deletion failure: a failed backup
followed by verified deletion is successful removal without a complete backup. If permissions or
another filesystem error also prevent deletion, report removal failure and the remaining paths;
never claim success or imply that the mod can override operating-system permissions.

Self-check still does not unregister live prefabs or promise to repair the current game session.
Require restart after removal; if removal fails, restarting alone does not resolve retained assets.

### Retained repository routing records: Post-load self-check

Contract section 19 defines migration-first self-check and its backup/removal fallback. Wait for
native loading, mod initialization, PDX caching and active asset batches; observe both current
readiness and completion events so late mod loading is covered. Include cached user PrefabAssets
that failed registration without force-loading or republishing them. Validation of live references
is read-only and is separate from dependency persistence under section 17.

For required null references, inspect the exact serialized CID. If its dependency is already cached,
copy permitted source bytes without modifying live references; require restart. Complete missing
native payloads and layout through migration before considering removal. Failed or impossible
migration enters section 19's per-UUID backup/move, then deletion fallback on backup/move failure.
Do not unregister live prefabs. Report actual removal failures and preserve healthy current assets.

Source isolation follows this bridge generation contract's 2026-10-13 subsection: exclude other bridges'
ImportedData Prefabs, including same-CID private dependencies, before reading. Use only original or
permitted sources, or fail. Ownership remains path-string-only. See the contract for its baseline.

### Explicit bulk deletion (2026-10-07)

Baseline `dev` / `3223c89f9573c6d6515a9fb1c1b078576e8eb723`;
rollback reference `refs/rollback/manual-bulk-delete-20261007`.
The confirmed settings action to remove all bridges must run the same sequential deletion pipeline
as individually deleting every Bridge Builder bridge. It is available in a ready main menu or loaded
city. Permanently delete UUID-owned files, not move them. In a city remove placed bridge networks and
wait for native entity cleanup before deleting files and unregistering the owned prefabs, including
clearing tool/menu references. Keep the dangerous-operation confirmation, with localized
"Continue removal" action and text describing permanent deletion and effects on other saves.
Self-check retirement is separate: it still backs up/moves files and requires restart, without live
prefab unregistration. Neither operation replaces the style-scoped deployment cleanup in section 13.

### Migration failure falls back to removal (2026-10-07)

Baseline: `dev`, `3223c89f9573c6d6515a9fb1c1b078576e8eb723`;
rollback reference `refs/rollback/migration-failure-removal-20261007`.
After an unsuccessful dependency, native conversion,
legacy naming or layout migration, roll back that phase's incomplete changes and retire the affected
bridge using UUID-string ownership and recursive backup/move. Process naming and layout per owner
so a failure does not remove unrelated successful bridges. Do not unregister live prefabs; require
restart. A successful fallback is removal, not migration success or incomplete retention. Actual
deletion failures remain explicit failures; a backup/move failure must first fall back to deletion.
Never claim files were removed when they remain.

### Field-by-field migration (2026-10-07)

Rollback baseline: `dev`, `3223c89f9573c6d6515a9fb1c1b078576e8eb723`;
reference `refs/rollback/fieldwise-migration-20261007`. Preserve existing local edits.

Version migration must complete missing fields individually, because legacy bridges may originate from different versions and lack different fields.
After migration, bridges must use the same file structure and naming conventions as bridges generated by the current version.

Inspect each persisted field/component independently on the root and its owned networks. A version
marker or one complete root must not skip checks of other fields, auxiliary networks, locales,
dependencies, names or layout. Preserve valid values, native unlock semantics, UUID/CID and geometry;
write only assets needing an upgrade. Unknown optional historical values remain empty rather than
invented; missing required information must produce an explicit migration failure followed by removal.
Use the same native persistence helpers and naming/layout rules as generation. Repeated migration
must make no changes after successful completion; report it as migration and require restart.

### Legacy naming migration exception

Legacy naming migration exception (user request 2026-10-07): self-check must replace reachable
pre-UUID sections reached through the canonical bridge root's CID dependency graph, plus their
derived pieces/LODs/geometry (never use section-name regexes or child-name prefixes)
with per-bridge copies named by appending "-b{uuid}" to the first space-delimited name token. Rewrite file references to new private CIDs, back up originals,
and remove old files only after no local prefab references remain. Do not mutate live prefabs; require
restart. This narrowly scoped legacy exception supersedes the UUID-only rule for those source files
and the ordinary byte-identical dependency-copy rule for this explicit rename migration only.
Rollback baseline: `dev`, `673bc85e84a83ab14c957a6a9565a013f69209e5` (existing edits preserved).

### Unified file layout and truthful migration notices

User revision 2026-10-07; baseline `dev`, `3289fc947637f88a9124c524bd1235029be802d1`;
rollback reference `refs/rollback/unified-migration-layout-20261007`. Preserve existing local edits.

After successful self-check migration or repair, an old bridge must use the same directory layout,
file naming rules, CID sidecars and dependency-copy layout as a newly generated bridge. Generation
and migration must share the layout implementation; do not maintain an old-only naming scheme or
skip layout migration merely because native component conversion already succeeded. Identity values
may differ between distinct bridges, but their naming and directory conventions must match. Preserve
the existing bridge and deck CID, UUID and geometry. Back up before relocation, handle collisions
without overwriting unrelated files, and leave a repeated migration unchanged. Retained historical
backup files belong outside the game's active asset directories, subject to shared-reference safety.

Startup self-check must distinguish healthy legacy migration from damaged-asset repair/removal.
Report migration as migration, never as repaired damage. When both occur, explicitly report both;
when migration fails, report that failure without calling a healthy bridge damaged. Localize all
new messages in every supported language and request restart after persistent changes.

## 21. Movable bridges use generated selected-road surfaces and native mechanisms

User-selected technical route, specified on 2026-10-15. Pre-edit baseline: branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference
`refs/rollback/movable-bridge-contract-20261015`. Existing local changes are preserved.

[MOVABLE_BRIDGES_CONTRACT.md](MOVABLE_BRIDGES_CONTRACT.md) is the normative supplement for draw,
lift, pedestrian and subsequently admitted movable mechanisms. Read it before their development.
Its sections 2–12 define the general generation, rendering, graph and persistence method; section 15
records encountered failures and mandatory solutions. The method derives from the implemented
pipeline; per-style dimensions and historical section tables are evidence, not universal rules.
Section 7 requires existing materials, prototype-based UV projection and paint integrated into
mechanism geometry, with no independent marking objects or texture baking. Section 11 requires
selective Section/Piece persistence, native unlock semantics and native-format reader compatibility.

Contract synchronization baseline (2026-10-17): `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`;
rollback reference `refs/rollback/movable-generation-contract-20261017`. Local edits are preserved.

Generate the visible moving road from the selected road's resolved closed-pose composition, convert
it into persistent object geometry, partition it and bind it to the measured native mechanism.
Preserve native traffic connectivity, crossing signals, watercraft clearance and manager-independent
operation. Do not substitute the prototype road surface or a mod-owned animation/signal controller.
Network mapping is specific to the measured bridge archetype and selected road/state. The supplement's
Large Road to DrawBridge03 mapping is an example, not a universal section or node/edge template.

The user's selected-road conversion requires new road-surface topology, skinning and, where needed,
network-to-object attribute conversion. This is a scoped construction operation on the generated
road surface, not permission to re-encode carried archetype structure meshes under section 4.
Section 8's structural widening and offline part-identification rules remain unchanged. Use measured
partition data; do not infer authored hinges or part membership at runtime.

Section 10's deferred status remains until each claimed variant has measured generation data and
the supplement's actual acceptance evidence. Documentation, discovery, compilation and preview
success alone do not enable a style. Sections 12–19 continue to govern branching, style-scoped
cleanup, width audits, native pricing, ownership, persistence and migration.

## 22. New bridge styles require immediate UI synchronization, generator routing and multilingual names

User requirement dated 2026-10-16. Pre-edit baseline: branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`. Existing working-tree changes are preserved.

Whenever a new bridge style is created, immediately synchronize the UI and connect the new style
to the generator router. Complete both in the same implementation increment. This applies to
fixed and movable bridges alike. A completed generator without a working creation-panel entry is
an incomplete style implementation; UI integration must not be deferred to another user request.

- Register the stable style ID consistently in the style definitions, catalogue, generator route,
  supported deck modes and UI bindings. With its prerequisites satisfied, the style must appear in
  the appropriate creation list and participate in search, filtering, selection and preview.
- Connect both creation actions to that style's actual generator. Keep management labels and style
  filters consistent with the creation panel. Preserve existing DLC and source-availability checks.
- Supply the style name in every supported UI language through the existing localization tables.
  Current coverage is English, Simplified Chinese, Traditional Chinese, German, Spanish, French,
  Italian, Japanese, Korean, Polish, Brazilian Portuguese and Russian. Extend coverage when the
  project's supported-language set grows; a Chinese-only label is insufficient.
- Use stable, unlocalized IDs for routing, persistence and selection. Resolve displayed names from
  the active game locale and refresh them when the language changes. Preserve user-entered bridge
  names. Keep English fallback for unsupported locales, but do not use fallback to conceal missing
  translations in a supported locale.
- Verify catalogue visibility, supported deck modes, preview selection, both creation actions,
  management labels and language changes together. Run the existing relevant localization checks;
  record any pending human UI verification explicitly. Compilation alone is not UI acceptance.

### Registered-style behavior parity for testing (2026-10-16)

All registered bridge styles must use identical UI and operation behavior regardless of implementation
completeness. This requirement applies to fixed and movable styles and overrides the previous rule
that retained completion-based admission safeguards.

- Never display a notice, warning, tooltip, badge, placeholder or other message stating that a bridge
  style is unfinished, incomplete, not ready or still under development. Its messages and notification
  behavior must be identical to those of completed styles.
- Do not disable creation, create-and-build or preview for any registered style because its
  implementation is incomplete or its generator is not connected. Do not hide such styles or prevent
  their requests from reaching the generator router. Apply the same ordinary input, DLC, source and
  deck-mode requirements to every style; completion status must not introduce an additional gate.
- A creation or preview request for a registered style without a connected generator must explicitly
  fail with an **ERROR** identifying the requested style ID and the missing generator route. Surface
  it through the same operation-failure handling used for completed styles and record an ERROR-level
  diagnostic. A silent return, warning-only log, disabled control, substitute bridge or reported success
  is prohibited. The error describes the actual routing failure, not the style's completion status.
- Use the same request dispatch, progress, success and failure handling for all registered styles.
  Report success only when the requested operation actually succeeds. This rule does not authorize
  publishing invalid or partial assets, or claiming that an unverified generator is complete.

Pre-edit baseline: branch `dev`, HEAD `0f4b10ae47c5dc264249e0d877aacc326e3e9525`;
rollback reference `refs/rollback/registered-style-ui-parity-20261016`. Existing working-tree changes
are preserved. Implementation status remains documented accurately in development records.

## 23. GPU computation and parallel bridge generation

User requirement dated 2026-10-16. Pre-edit baseline: branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`. Existing working-tree changes are preserved.

Computationally expensive repeated operations, including bulk matrix calculations, pixel processing
and image algorithms, must use GPU execution. Assume the user has a GPU capable of meeting
Cities: Skylines II's system requirements. GPU availability does not imply NVIDIA hardware or
CUDA availability: use game/engine GPU facilities or other APIs compatible with supported GPU
vendors. Do not make CUDA or another vendor-exclusive facility the required computation path.

During bridge generation, run independent steps in parallel wherever possible. Identify their data
dependencies and synchronize only where a downstream step needs the results. Preserve required
ordering, resource ownership and deterministic results where generation depends on them. Respect
Unity/game API thread affinity and graphics-context requirements; parallel execution does not
permit calling main-thread-only APIs from worker threads or racing shared asset mutations.
This is a runtime generation requirement, not authorization to delegate work to coding agents.

This documentation update establishes the implementation rule; it does not claim that every
existing generator has already been migrated to GPU computation or parallel execution.

## 24. Movable bridge asset size

User requirement dated 2026-10-16. Pre-edit baseline: branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`. Existing working-tree changes are preserved.

The total size of a generated movable bridge Prefab, including its dependencies, must be
comparable to the corresponding original movable bridge. The root Prefab file alone is not
the measurement. Count the root, generated networks/objects, every LOD and geometry payload,
surfaces, textures, required presentation assets and external dependency snapshots. Deduplicate
logical dependencies by identity within one bridge; also report physical files/bytes, including
CID sidecars and any duplicate copies. Built-in resources remain references under section 17:
include their reachable payloads in the logical comparison without exporting duplicate files.

Compare like-for-like content and serialized payload sizes using a declared compression basis.
Do not compare a complete game/DLC archive with one generated bridge, use shared UI catalogue
back-links to inflate the reference, or compare compressed bytes with decoded texture memory.
Record both serialized size and decoded texture memory separately where available. Explain
selected-road differences. Unresolved references or an absent generated sample leave the total
comparison incomplete; a texture subtotal or render-only subtree cannot establish compliance.

Reuse identical appearance resources across a bridge's LODs; avoid per-triangle texture/prefab
explosion and redundant constant textures. Do not satisfy the size requirement by omitting
required dependencies, dropping LODs, silently reducing appearance quality or introducing a
manager-owned runtime reconstruction dependency. Measure a human-created persisted bridge
before claiming this requirement is met. No arbitrary acceptance ratio is implied by this rule.

## 25. Adapt sidewalk-dependent components to the actual elevated road

User requirement dated 2026-10-17. Pre-edit baseline: branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/elevated-sidewalk-contract-20261017`. Existing working-tree changes are preserved.

This rule applies to every bridge generator, including fixed and movable bridges. Roads do not
necessarily have sidewalks on both sides, and their sides are not necessarily symmetric: a road
may have no sidewalks, a sidewalk on only one side, or sidewalks of different widths on its two
sides. Resolve the selected road's actual elevated-mode composition and applicable upgrade state;
do not infer sidewalks from its ground-mode appearance, lane count or the prototype bridge.

Only sections in the `Sidewalk` family count as sidewalks. Curbs and all other section
families are not sidewalks and must not be classified as sidewalks when resolving either side's
sidewalk presence, position or width.

Adapt every sidewalk-dependent component, including inner bridge railings and bridge-authored
sidewalk portions, to the presence, position and width of the actual elevated sidewalk on each
side independently. A side without a sidewalk must not receive a fabricated sidewalk or a
component whose purpose depends on that sidewalk. Do not mirror one side onto the other, assume
equal widths, or reserve a prototype-width sidewalk strip in the carriageway. Distinguish these
components from structural or outer safety railings that do not depend on sidewalk presence;
absence of a sidewalk alone does not authorize removing unrelated bridge structure.

The same requirements apply to sidewalk portions embedded in a composite bridge component,
including fixed bases, end platforms and transitions next to a movable mechanism. A component's
fixed or structural role does not exempt its sidewalk portion. Identify that portion from measured
archetype membership and adapt its presence, position and width independently on each side to the
selected elevated road's `Sidewalk` sections. If a side has no such section, do not retain or generate
that sidewalk portion. Preserve unrelated load-bearing structure and outer safety components;
do not remove the whole composite component merely to remove its sidewalk portion. Apply this
rule to the highest-detail mesh, every LOD, preview and persisted geometry.

Apply the resolved layout consistently to preview, formal generation and all LODs. Preserve the
selected road's native lane semantics and use measured archetype component membership when
adapting bridge-owned geometry. This rule governs sidewalk-dependent presence and placement; it
does not authorize guesses about structural parts or changes to unrelated geometry. Verify roads
with zero sidewalks, one sidewalk and unequal left/right sidewalk widths when claiming support.

## Detailed repository rule checklist (2026-10-16)

Moved without removing requirements from root AGENTS.md to keep the automatic instruction
chain below 32 KiB. Baseline: `dev/3b6363ada9f1f147b1a660e20c0d6246a54f82dd`.

The numbered references below point to the corresponding sections in this bridge generation contract.

1. Every generated bridge follows a real archetype of the same type. Preserve the complete prefab,
   component, placement, mesh-part, spawn and pillar behavior unless the deliberate difference is
   recorded at the point of change. See contract section 1.
2. Archetype parameters are measured once and hardcoded. Runtime generation must not depend on the
   archetype prefab being installed. Existing input components are carried across whole except for
   the exact modification requested. See contract section 2.
3. A generated bridge may differ from its archetype only in generated geometry and the user-selected
   road structure. See contract section 3.
4. Towers and cables are derived geometry owned by the generated bridge; they are never references to
   shared archetype geometry. Each generated bridge receives its own bridge-prefixed structure
   prefabs. See contract section 4.
5. A generated tower differs from its archetype in width alone. Preserve all other measured
   relationships and prefab behavior. See contract section 5.
6. Compilation, synthetic numbers and offline tests cannot prove a visual fix. Agents must not run
   unit tests for visual component-generation work. Inspect the real prototype and generated geometry,
   read export diagnostics, and finish with in-game near- and far-view checks. Every visual change must
   update the highest-detail mesh and every LOD together. See contract section 6.
7. Diagnose by dumping the archetype and generated object, normalizing the data and diffing them. Read
   the game IL when field behavior is unclear. Do not substitute visual guesses for evidence. See
   contract section 7.
8. Stretch versus translation is decided only by whether the authored part reaches or crosses
   `x = 0`. This rule has priority over every family special case, heuristic and agent-authored rule,
   and every attempted override must be refused, except for the user-approved central railing post
   exception in contract section 8. Runtime spatial comparisons may compare `x`, `y` or
   `z` with zero using equality or inequalities; they may not use a fixed non-zero coordinate to infer
   geometry. Non-zero geometric inference is allowed only in metaprogramming, whose reviewed result is
   committed as immutable, hardcoded runtime data. Runtime geometry guessing is forbidden. See
   contract section 8.
9. The suspension family is the reference checklist; every family must preserve the equivalent
   archetype relationships rather than inherit suspension-specific values. See contract section 9.
10. Unsupported generation is refused with a reason. It must not silently fall back to an unrelated
    archetype or partial bridge. See contract section 10.
11. A double-deck bridge is built only from its double-deck archetype. Preserve its main/auxiliary net
    roles, positions, inversion behavior and fixed deck separation. See contract section 11.
12. A bridge-specific change is made only on the exact all-English `bridge/<style-slug>` branch listed
    in contract section 12. Check `git branch --show-current` before reading code with intent to edit.
    Shared infrastructure is developed on `dev`; it is not disguised as a single-bridge change.
13. After a code update, invoke the exact `Cities2.exe` kill command even if it is a no-op and verify
    the process is absent. If bridge-generation code changed, remove all mod-created bridges only
    for the affected style IDs and their UUID-owned artifacts. Preserve unaffected styles. If no
    bridge-generation code changed, remove no existing bridges. Documentation-only edits require
    no shutdown, cleanup, build or deployment. See contract section 13.
14. Before editing any contract or `AGENTS.md`, record the current branch and exact `HEAD` commit as
    the rollback baseline. Every generated bridge must preserve its archetype's measured constant in
    `bridge width - deck width`. Width is always the complete left-to-right span `maxX - minX`; a
    one-sided x coordinate, `abs(x)`, or twice a one-sided boundary is not width. The audit uses only
    `widthIncrement = (archetypeBridgeWidth - archetypeRoadWidth) - (generatedBridgeWidth -
    generatedRoadWidth)` and `newBridgeWidth = generatedBridgeWidth + widthIncrement`, evaluated as
    bitwise-exact binary32 without rounding, tolerance or a forced result. `widthIncrement` is the
    increment of that complete span. If `abs(widthIncrement) > 1 m`, skip that bridge and record the
    evidence. Establish every input by comparing a real archetype with a real newly generated bridge;
    never infer it from source
    arithmetic alone. A missing generated sample is never a reason to skip: the required new bridge
    must be created and measured, and the pass remains incomplete until that real sample exists.
    Creating and judging the sample is a human game operation; the Agent must not open or control the
    game or issue an API/request-file bridge-construction request. See contract section 14.
15. Bridge-generation code must never calculate, look up or apply an audit correction at runtime.
    Width and geometry corrections are completed during metaprogramming and folded into the original,
    immutable archetype parameters or generated source data. Runtime consumes only those final values;
    it must not know an audit delta exists. See contract section 14.
16. Bridge-specific lighting is archetype behavior. Preserve every authored bridge light, its effect
    prefab and fields, placement, parent mesh, rotation, group, probability and activation conditions.
    Side-mounted tower lights follow the same rigid width displacement as their parent mesh; lights on
    `x = 0` remain centred. Ordinary lighting belonging to the user-selected road remains part of that
    road structure. See contract section 15.
17. Preserve native construction, elevation and upkeep fees. The game calculates charges for the
    generated network and its objects. Do not add a custom scalar total, multiplier, offset, floor,
    runtime price override or pricing-only prefab. See contract section 16.

## Retained repository routing records: Current documentation baseline

Error-boundary policy revision (2026-10-16): branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference
`refs/rollback/error-boundary-policy-20261016`. Existing working-tree changes are preserved.

Backup-content rule revision (2026-10-16): branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`. Existing working-tree changes are preserved.

Documentation revision (2026-10-15): branch `dev`, HEAD
`0f4b10ae47c5dc264249e0d877aacc326e3e9525`; rollback reference
`refs/rollback/scoped-cleanup-docs-20261015`. Contract section 13 is the authority for deployment
cleanup: generation changes remove only affected styles' bridges; without a generation change,
remove no existing bridges. Documentation-only edits require no game shutdown, build or deployment.

## Organization revision record

Organization-only revision (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/contract-organization-20261017`. Existing working-tree changes are preserved.
Original requirement text, examples, exceptions and revision records are retained. Existing
numbered sections and heading anchors are preserved; headings, placement and navigation are
organized without changing requirements or implementation acceptance.


## Approved native tiled road material descriptor (2026-10-17)

The user explicitly permits a new native object-material descriptor that references existing
tileable road textures to eliminate atlas-repeat geometry cuts. This is a scoped exception to
the existing-material-only rule: do not create or bake textures, introduce custom shaders, modify
source assets, or change structural and marking materials. Keep one descriptor per required
appearance within each bridge owner and share it across LODs. Preview descriptors belong only to
the preview's transient database. Persist formal descriptors with private identities and original
texture references; native cold loading must not depend on Bridge Builder. Unchanged built-in
textures remain references, and external dependencies retain the normal snapshot policy.

Use continuous object-local UVs and the native sampler for whole-texture repetition; do not apply
vertex-wise fractional UVs or cut triangles at repeat boundaries. This exception does not permit
repeating a mixed atlas as though it were a tileable road texture. Other geometry, marking,
sidewalk, LOD and acceptance requirements remain unchanged.

Baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`; rollback reference
`refs/rollback/native-tiled-road-material-20261017`. This permission does not establish visual or
cold-load acceptance.

## Approved GPU composition from existing textures (2026-10-17)

The user explicitly authorizes composing new textures from existing textures. For converted
movable-road appearance, this supersedes the earlier existing-materials-only, no-new-texture,
no-baking and no-new-descriptor restrictions, including the narrower tiled-asphalt exception.
It does not authorize custom runtime shaders, independent marking objects, depth offsets,
prototype mesh Boolean operations, or source-asset mutation.

- Compose road appearance and paint using existing native texture inputs on a vendor-neutral GPU.
  Save owner-local textures and native object-material descriptors; do not bake per marking or
  create separate marking Prefabs. Preserve needed color, alpha, normal and mask channels.
- Paint must not cut the receiving road. Texture repeat boundaries must not subdivide geometry.
  Evaluate repetition and paint in pixels. Only physical boundaries, native motion seams and
  required appearance-domain boundaries may partition the road.
- Build each of the three LODs from its corresponding native mesh input and measured bridge
  construction rules. Do not feed the painted/tiled near mesh into every level. Retain the last
  authored road-piece LOD where the native piece supplies fewer levels. Share composed appearance
  across levels; use mip filtering rather than replicating paint geometry.
- Keep prototype structural meshes, skinning, thickness, sidewalk adaptation and LOD thresholds.
  This appearance permission does not authorize deleting structure to meet a triangle budget.
- Bound atlas dimensions and share resources within each owner. Generate mipmaps on the GPU,
  respecting color space and tangent-space normals. Persist textures and descriptors under the
  bridge's identity; preview resources belong to the transient preview database. Delete failed
  generation resources. Preserve the existing external dependency snapshot rules.
- Verify stage times, per-LOD triangles, total serialized size including dependencies, reload and
  near/far visual appearance. Compilation alone does not establish acceptance or size parity.

Instruction-edit baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.
Rollback reference: `refs/rollback/gpu-road-composition-20261017`.

## Native separated movable paint revision (2026-10-18)

The user now requests separate native markings and prototype-based asphalt with preserved UV scale.
This supersedes the prohibition on independent marking objects only for native decal child renderers
attached to the mechanism's recorded motion connections. Keep each disjoint projector in its own native object-space clipping bounds. The native shader
clips against one box per object: grouping disjoint volumes is prohibited unless a compatible native
per-instance clipping representation is verified. Supply its normal and tangent attributes and bind
each projector to its motion connection. Keep geometry as closed projection volumes; never render those volumes
as road surface triangles. Only projection volumes may be partitioned at physical motion seams;
markings and texture repetition must never cut receiving road geometry.

Remove full-road transparent paint overlays. Reuse the native decal material and measured native
paint patches. Asphalt uses a repeatable unit extracted from the prototype atlas, including its
albedo, normal, mask and control channels. Preserve the measured per-metre UV gradients, direction
and phase on both leaves. BC block alignment may round the extracted pixel dimensions up; do not
stretch a low-resolution whole-span atlas over the road. Other appearance domains retain their
existing path. Keep native asset persistence and owner-scoped rollback/cleanup for the complete graph.
Preview, placement, opening/closing, LOD transitions and cold loading require human game verification;
a successful build does not establish visual acceptance.

Revision baseline: `dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`.

## Preserve original road skinning (2026-10-18)

The user revoked moving the road onto marking child transforms. Keep Draw01 road geometry in its original procedural skeleton and skinning path, with all three LODs, UVs and materials retained. Markings must adapt to the original road bones; never move road receivers into history-driven child objects to compensate for decal lag. Native independent persistence remains mandatory, without runtime synchronization patches. Structure/underside surfaces use the original native descriptors, texture references and VT resources without receiver-only material copies or descriptor rewrites. Single-sided skinned paint replaces the old projectors; disabling structural decal reception is no longer required. The earlier rigid road child implementation is withdrawn. A native decal shader without procedural skinning must not be treated as supporting bone-bound paint merely by assigning indices.

Revision baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a.

## Authorized native skinned paint exception (2026-10-18)

The user explicitly permits native skinned paint surfaces and minimum anti-coplanar clearance. Draw01 keeps its original road skeleton and receiver topology. Marking polygons alone may be partitioned at motion seams and bound with the road bone indices/bind poses. Use a native procedural-skinning material, single-sided paint, and 0.001 m clearance; no custom shader or runtime synchronization patch. Paint is an additional mesh on the same native mechanism object, with matching StackProperties, authored bounds and object transform, not a history-driven child projector. Preserve source marking layout/color; do not cut the road. A compact paint mesh remains present across road LOD transitions. This supersedes earlier prohibitions on overlapping marking surfaces and the native child-projector requirement for this implementation. Preview, motion, far views and cold loading still require verification.

Revision baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a.

## Accepted movable generation baseline (2026-10-18)

Read [MOVABLE_BRIDGES_CONTRACT.md sections 16–23](MOVABLE_BRIDGES_CONTRACT.md#16-accepted-generation-pipeline-and-source-map) before implementing or modifying movable bridges.
Those sections consolidate the accepted current pipeline and supersede conflicting historical
paint/material recipes: original structural Surface/VT references; measured non-VT repeating road
and grating units plus bounded native VT composites; separate single-sided native skinned paint
with 0.001 m clearance on the original road bones, matching stack metadata and transforms.
No receiver cuts for markings, no child projectors, no structural receiver-only material copies,
no moving the road onto marking bones, and no runtime synchronization dependency. Acceptance is
specific to the latest Draw01 visual repair; new styles still require their own archetype data and
acceptance. Baseline: dev/d21433ed2a9ee233c8525ce6a0fe2515f973dc7a plus preserved working changes.

Prior long instruction records are preserved in [retained movable instruction records](MOVABLE_INSTRUCTION_RECORDS.md).
Use the current accepted contract for conflicting appearance/marking decisions.


Persistence layout revision (2026-10-19): branch `dev`, HEAD
`d21433ed2a9ee233c8525ce6a0fe2515f973dc7a`, preserving prior working changes.
Follow Bridge-Asset-Persistence.md sections 6–7 for owner-local short filenames, VT/mid-mip
placement and backed-up file-only migration. Legacy sibling dependency directories remain valid
migration inputs. Layout conflicts preserve healthy bridges and report incomplete migration;
they do not authorize damaged-asset retirement. Native cold-load acceptance remains separate.
