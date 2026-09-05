# Project contract and Agent instructions

Rules this mod is held to. They are not style preferences; each one is here because breaking it
produced a bridge that was wrong in a way nothing reported, and finding out why cost a round of
guessing.

This document is the normative project contract referenced by the repository-wide `AGENTS.md`.
Every Agent must read the sections routed by that file and obey them before changing the project.

The numbered rules cover archetype fidelity, generated geometry, diagnosis, runtime safety and the
branch workflow. Later rules have the same force as the original ones: each records a failure mode
which must not be reintroduced.

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

- [`BridgeTowerSpec`](../../src/BridgePrefabGenerator/Bridges/BridgeTowerSpec.cs) — the tower archetype as
  plain numbers, with no dependency on the game, so the offline tests can hold it to what was measured.
- [`BridgeTowerTemplate`](../../src/BridgePrefabGenerator/Bridges/BridgeTowerTemplate.cs) and
  [`BridgeCableTemplate`](../../src/BridgePrefabGenerator/Bridges/BridgeCableTemplate.cs) — apply the specs
  to a prefab. The only files that need the game.
- [`BridgeTowers`](../../src/BridgePrefabGenerator/Bridges/BridgeTowers.cs),
  [`BridgeCables`](../../src/BridgePrefabGenerator/Bridges/BridgeCables.cs),
  [`BridgeMeasurements`](../../src/BridgePrefabGenerator/Bridges/BridgeMeasurements.cs) — the measured widths,
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
`ModsData/BridgePrefabGenerator/`. Re-measuring is how these tables get corrected; a number that cannot
be reconciled with a measurement is wrong even when the bridge looks right.

**Measured data is never deleted.** An entry that turns out to describe something other than what it was
recorded as — a support column read as a portal — is marked, not removed. Deleting it only means it
gets measured again.

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

## 5. A generated tower differs from its archetype in width alone

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
derived prefab if a carried range does not receive the same rigid translation at every level. Mod code
must not throw to enforce this rule; rejection is an explicit result handled before geometry is written.

This is also the repository-wide runtime failure contract: code under `src/BridgePrefabGenerator`
must not contain an explicit `throw` statement. Unsupported input, failed validation and violated
generation invariants return an explicit failure result; the caller records the reason and stops the
affected prefab before allocating or publishing persistent geometry. Catching exceptions raised by the
game or third-party APIs remains required so one external failure cannot unwind the simulation update.

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


Both cases are mappings, and this is the whole of rule 8. With `d` half the extra width, and `s` the
span of the crossing member at that height:

    does not cross    (x, y, z)  ->  (x + sgn(x) * d,  y, z)
    crosses           (x, y, z)  ->  (x * (s + d) / s, y, z)

The first has no stop at the centre, and no part of the tower is held back so that another can avoid
reaching it. The second is about the member's own span, never about the widest thing at that height.

**底座是紧贴桥梁下方的构件。** “Base” / “底座” has this one exact meaning in this project: the base
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

The suspension bridges are the family this mod was built against and the only one whose generation has
been seen working end to end. That makes them the reference implementation and, more usefully, a
checklist: whatever was established for them is what any other family has to satisfy before it can be
called done.

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
their travel. None of that has been measured. `Draw`, `PedestrianDraw` and `Lift` therefore fail with
the reason, because a bridge that is not generated is a bridge the player still has, while a bridge
generated from an arrangement nobody has measured is one that looks built and behaves as something
else.

**Superseded packs.** Bridge Expansion Pack content is skipped as a donor **where the base game covers
it**, which is what it was folded into the game for: offering both shows the player the same bridge
twice, and deriving from the pack's copy binds a generated bridge to an asset that can be uninstalled
while the vanilla one cannot.

The exclusion is by duplication, not by provenance, so it stops where duplication stops. Every double
deck suspension bridge installed is the pack's — the game's own suspension bridges are all single deck
— and skipping those as well removed not a duplicate but the only archetype there is for two decks.
Rule 11 then refused to build one, correctly, for a reason the exclusion had created. A pack bridge
offering a capability the base game has no archetype for is kept.

A different width is not such a capability: generating any width from a narrower archetype is what this
mod is. A second deck is, because it is a different arrangement rather than the same one stretched.

Both lists are data, in `BridgeStyleDefinitions`, with the reason attached. Adding to either is how a
design is taken out of scope; removing from either is a claim that it has been measured.

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
| Blue deck truss-arch bridge | `bridge/truss-arch-01` |
| Green deck truss-arch bridge | `bridge/truss-arch-03` |
| Through truss-arch bridge | `bridge/truss-arch` |
| Tied-arch bridge | `bridge/tied-arch` |
| Covered wood bridge | `bridge/covered-wood` |
| Grand bridge | `bridge/grand` |

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

## 13. Every code update stops the game and removes generated bridges

After every source-code update, the Agent must invoke a kill command for the exact `Cities2.exe`
process. This command is mandatory even when the process is not observed running; a no-op result is
acceptable, omitting the command is not. The Agent must then verify that no `Cities2.exe` process
remains before touching installed mod files or generated game assets.

With the game stopped, the Agent must remove **all bridges created by this mod**. This includes every
mod-owned generated bridge prefab, derived tower, piece and LOD prefab, generated geometry asset and
the export state which can recreate references to them. Use the repository cleanup procedure and
verify its ownership-scoped targets are absent afterward. A bridge may not be retained because its
input road, style or generated name appears unchanged.

Killing the game and removing generated bridges are post-update requirements, not optional visual
verification steps. Building or installing a DLL does not satisfy them, and neither requirement may
be postponed until a later session.
