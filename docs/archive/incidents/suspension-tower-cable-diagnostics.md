# Suspension tower and cable diagnostic history

## Evidence scope

These historical cases were formerly embedded in the development contract. The original record
supplied no date or implementation revision. Its measurements, round counts and earlier mesh
behavior are not current acceptance claims. Historical instructional wording records the reasoning
behind the effective [diagnostic obligations](../../contracts/development-contract.md#generation-7-a-fault-is-found-by-dumping-both-and-diffing-not-by-reasoning-about-it); it is not a competing policy.

<a id="generation-7-a-fault-is-found-by-dumping-both-and-diffing-not-by-reasoning-about-it"></a>

## A fault is found by dumping both and diffing, not by reasoning about it

Every fault in this project that survived more than one round was found the same way, and none of them
were found by thinking harder about the geometry. The method is written down because the alternative
kept being tried first.

<a id="generation-dump-both-normalise-diff"></a>

## Dump both, normalise, diff

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

<a id="generation-read-the-games-il-to-learn-what-a-field-does"></a>

## Read the game's IL to learn what a field does

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

<a id="generation-a-component-that-reads-as-cosmetic-may-be-structural"></a>

## A component that reads as cosmetic may be structural

`NetPieceTiling` reads as a texture setting and decides where a piece is laid out. `StackProperties`
reads as a rendering detail and decides whether the tower touches the ground. So a component is never
dismissed by its name, and "we carried the fields across" is never the same as "we carried it across".

<a id="generation-ruling-a-suspect-out-counts-and-a-shared-code-path-is-the-cheapest-way"></a>

## Ruling a suspect out counts, and a shared code path is the cheapest way

Two suspects were eliminated without a single experiment:

- The vertex-format path could not be the cable fault, because the towers go through the same
  `BuildModel` and the towers render correctly.
- `isPacked: false` could not be corrupting the normals, because `PackFloatAttribute` only fires at
  `dimension == 3` and the channel is declared `SNorm16x2`.

Write down what has been ruled out and why. An unexamined suspect gets re-examined every round.

<a id="generation-check-that-the-instrument-can-see-the-thing-before-trusting-its-silence"></a>

## Check that the instrument can see the thing before trusting its silence

`AssetAnatomy` capped mesh loading at 64 render prefabs **for the whole dump**, so the archetypes listed
first spent the budget and the generated bridge reached its own geometry with none left. Every vertex
layout in the file described a mesh already known to be right, and the generated cable's geometry was
never in the file at all. A round went into looking for a sign error in metadata that structurally could
not contain one.

So before concluding a dump shows nothing wrong, confirm it contains the thing. Budgets, caps, depth
limits and filters are all places where a diagnostic quietly stops covering what it is pointed at, and
they must be per-subject rather than per-run.

<a id="generation-a-check-that-reports-is-an-instrument-and-is-wrong-until-it-has-met-its-cases"></a>

## A check that reports is an instrument, and is wrong until it has met its cases

Validate diagnostic quantities against the actual authored parts and representations they measure:

- Compare the same mesh and part membership before and after transformation; do not compare a
  whole section including anchorages with one isolated output mesh.
- Distinguish a spanning member's intended width change from a side member's thickness.
- When sampled runs overlap or merge across the centre, inspect their recorded membership and
  transformations before interpreting the aggregate run length as thickness.
- A vertical member must receive one consistent authored-part mapping at every height. Merging
  runs caused by incompatible displacements within that member indicate a real defect; do not
  dismiss the measurement merely because another case produced a false positive.

Exercise applicable coarse-LOD and spanning-surface cases before promoting a diagnostic to ERROR.
Respect the movable no-special-width rule: diagnostic validation does not authorize a narrow-road
correction or an additional compatibility gate. Inspect the reported geometry before changing or
removing a check; an indirect measurement can expose a defect that aggregate width misses.

<a id="generation-say-plainly-when-a-fix-is-not-the-reported-fault"></a>

## Say plainly when a fix is not the reported fault

The cable stretch divided by the piece's declared width instead of its drawn span, putting the outer
edge 1.6 cm inside where it belonged. Real, worth fixing, recorded as rule 5 — and not what the
screenshot showed. A small thing found while looking for a large one is reported as what it is. Letting
it stand in for the answer costs the next round.

<a id="generation-when-a-fault-survives-several-rounds-change-category"></a>

## When a fault survives several rounds, change category

Five rounds of the floating tower were spent on the tower; the fault was on its parts. The cables took
three rounds of vertex arithmetic, then a missing component that was real and was not the cause, then a
4 mm arithmetic correction that was also real and also not the cause; the fault was a field nobody had
written, in a struct nobody had looked at. If two attempts inside one category have failed, the next
move is not a third — it is to dump the thing and diff it.

<a id="generation-two-archetypes-of-different-sizes-are-a-test-oracle"></a>

## Two archetypes of different sizes are a test oracle

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

<a id="generation-a-field-that-describes-the-result-is-computed-not-carried-across"></a>

## A field that describes the result is computed, not carried across

Rule 4 preserves source channels except for its explicitly required conversions. Bounds are not a
property of the source that can be copied and not a
property of the vertices that anything derives automatically — they describe the *result*, and the only
code in a position to compute them is the code that produced it.

So when carrying a thing across, sort its fields into three: copied, changed, and **computed from what
was changed**. The third pile is the one that gets forgotten, because nothing at the call site looks
wrong and the source has nothing to compare against. Anything describing an extent, a count, a hash or
a total belongs in it.

<a id="generation-a-default-constructed-value-is-an-assertion-not-a-blank"></a>

## A default-constructed value is an assertion, not a blank

`new SubMeshDescriptor(start, count, MeshTopology.Triangles)` leaves `bounds` at its default. That
default is not "unset, please compute" — it is a zero-size box at the origin, and the renderer believes
it. Every mesh this mod ever wrote asserted that it occupied no space.

This is a distinct hazard from a wrong value: a wrong value is a mistake somewhere, while a default is
a mistake nowhere, sitting in a constructor that reads as complete. When a constructor takes fewer
arguments than the struct has fields, the missing ones are decisions that have been made silently.

<a id="generation-read-the-flags-the-callee-is-passed"></a>

## Read the flags the callee is passed

The whole fault turned on one integer in the game's code:

    Mesh.SetSubMesh(index, descriptor, flags: 15)

15 is `DontValidateIndices | DontResetBoneBounds | DontNotifyMeshUsers | DontRecalculateBounds`. That
last bit is the entire mechanism — it is the reason Unity's usual "the mesh will work this out" does not
apply, and it is invisible from our side of the call. Assuming a well-known API behaves the way it
usually does is the same error as assuming a field means what its name suggests.

So when handing data to something that will build an object from it, read what it does with it, and
read the flags it passes on. A numeric flags argument in someone else's code is worth decoding in full.

<a id="generation-worked-example-the-cables-end-to-end"></a>

## Worked example: the cables, end to end

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


## Historical acceptance wording illustration

So: state what the evidence shows and what it does not. "The report says the tower is 38 m across" is a
fact; "the tower is fixed" is not, until a bridge has been built with it.
