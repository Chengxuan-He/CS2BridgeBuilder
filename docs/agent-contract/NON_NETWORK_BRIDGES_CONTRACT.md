# Non-Network virtual bridge prototype contract

## 1. Scope and required sequence

This is a normative supplement to the [general bridge generation contract](BRIDGE_GENERATION_CONTRACT.md).
For a bridge supplied as a static object or another non-Network asset, use this sequence:

1. Establish the source's documented parameters and measured construction data.
2. Record its measured structure and roadway dimensions directly in the same hardcoded recipe form used for existing real bridge prototypes.
3. Consume that recipe directly through the existing Bridge Builder generation workflow.

Here, Network-ization is a conceptual intermediate result represented only by hardcoded construction
data. It saves no network files: do not produce an intermediate `.Prefab`, `.cok`, Network root,
sections, pieces, lanes, entities or registered prefab. Its roadway dimensions describe geometry without restricting road type;
do not bind it to a road, rail or pedestrian template or introduce a road-kind eligibility gate.
Bridge Builder later combines this virtual prototype with the player's selected network to create
the actual bridge. Placing an unchanged static bridge over an unrelated road is not that generation.
This contract specifies fixed bridges first. Moving structures additionally require the
[movable bridge contract](MOVABLE_BRIDGES_CONTRACT.md); static conversion does not implement motion.
This document specifies required work, not completed implementation or asset acceptance.

## 2. Source evidence and parameter meanings

For each source, record the mod ID, exact version, description URL, retrieval date, relevant short
description quotations with original units, and the installed package/prefab identities and hashes.
Keep description evidence and measured values separately attributable. A current description must
not silently describe an older installed version. Preserve source wording and explain it in English.
Use the description's explicitly stated parameter semantics as authoritative authoring intent, then
corroborate them against the actual geometry. Names and thumbnail proportions are not measurements:
for example, a name containing `224m` alone establishes neither total length nor span.

Maintain the following conceptual parameter record per asset within the existing recipe form;
it does not define or authorize a separate runtime schema.

| Parameter | Meaning and required evidence |
| --- | --- |
| Total length | Entire authored bridge extent between documented endpoints, including approaches when applicable; record which parts are included. |
| Span | Distance between identified bearing/support stations for one span; distinguish centre-to-centre span from clear opening and record unequal spans separately. |
| Repeat length | Measured longitudinal module interval and its origin/phase; do not equate it with total length or span without evidence. |
| Roadway width | Usable running surface between explicitly identified left/right boundaries; record sidewalks, tracks and medians separately where present. |
| Structure width | Complete outer structural extent, with both bounds; do not substitute roadway width or twice a one-sided extent. |
| Reference deck dimensions | Measured deck boundaries and length used by the virtual prototype's width mapping; no chosen native composition or road type is implied. |
| Placement and frame | Units, axis directions, source scale/rotation, deck datum, support stations, end connections and the transform into the virtual prototype's construction frame. |

Record original values and unit conversions without rounding away source precision. Explicitly reconcile
description/geometry differences: identify version mismatch, different boundaries, units or authored
transforms with evidence. Do not silently overwrite either value, average them, or distort geometry
to make them agree. An ambiguous or missing required parameter remains pending authoring evidence;
continue independent work, but do not fabricate a usable recipe from a plausible number.

## 3. Describe the virtual bridge prototype

Inspect the real prefab graph and all reachable structural objects, render parts and LODs. Record
local and accumulated transforms, authored member membership, material slots, geometry identities,
attachment/stacking behavior and end interfaces. Check that dumps cover the entire graph. Preserve
the source files and live prefabs unchanged; derived assets receive their own identities.

Describe the bridge's measured deck dimensions, coordinate frame, spans, structure, all LODs,
material references, supports, endpoints and placement semantics as construction data. Separate
bridge-owned structure and running-surface domains so final generation can adapt or replace the
surface through the existing selected-road path without layering competing decks. No operational
traffic network or native template donor is required to establish this virtual prototype.

Record each support's actual role: span-local, repeating, endpoint or shared span boundary, with
its transform, attachment and stacking semantics. These are abstract placement descriptions at this
stage, not a requirement to instantiate `NetPieceObjects` or `NetSubObjects`. Preserve nested render
parts; do not assign duplicate roles or infer anchoring merely from appearance or prefab name.

Derive endpoints, repeat origin, support stations and placement rules from the source record.
Verify adjacent virtual spans meet continuously without duplicated boundary supports, missing
members or a repeated end cap in every span. Any required partitioning and endpoint representation
must be measured offline and preserve the corresponding source faces and attributes at every LOD.
Do not invent a longitudinal deformation rule merely to fit an arbitrary segment length.

### Select longitudinal behavior from native evidence

Use the [complete installed native length inventory](../design/generation/Native-Bridge-Length-Handling.md)
as the reference, including ordinary bridges, fixed sequences, auxiliary decks and movable crossings.
Select by the measured construction roles and native fields, not by the word "arch", a thumbnail,
source total length, road kind or the list of styles Bridge Builder already supports. Record the
exact reference root/section/piece identities, installed version and native consumer for the selected
behavior. Recheck that evidence when the native format or consumer changes.

| Measured source role | Required final native representation and length rule |
| --- | --- |
| Continuous spanning structure, arch, cable sheet or deck | Use the corresponding native composition-piece normalization against its recorded reference length and the actual composed edge/node curves. Preserve applicable section requirements, offsets, inversion, flipping, half-length and source mesh/LOD attributes. Reference mesh length, preferred segment length and realized span are separate values. |
| Piers, bearing blocks, rigid end structures and shared boundary supports | Separate their measured all-LOD face ownership from the deforming span. Place them through the evidenced node/course/fixed-index or piece-object endpoint rules; retain their sections, object transforms and stacking semantics. Do not stretch the whole source object longitudinally to make the span fit. |
| Ordered unequal spans or mirrored half-spans | Record the evidenced `FixedSegments` count ranges, lengths, states and curvature rules, together with the matching section selections and support indices. A fixed sequence is a native course layout, not an exact-length lock. Do not add one merely to conceal missing longitudinal adaptation. |
| Repeated objects or finite endpoint attachments | Record native `NetPieceObjects` spacing, phase range, offsets, orientation, requirements, minimum length and probability. Let the actual native composition/secondary-object path place them; requested spacing is not a constant-distance grid and an endpoint is not an untrimmed root coordinate. |
| Moving crossing with fixed approaches | Follow the movable contract's measured crossing object, native mechanism/bones, state selection, approaches and waterway-node placement. Do not scale the mechanism to an arbitrary whole-course length or infer fixed segments merely because it moves. |

Native `Bridge.m_SegmentLength` ordinarily establishes a preferred edge range rather than exact
length. `FixedSegments` can initialize ranged elements; even an equal minimum/maximum element
is subject to native course deficit allocation and validation tolerances. Preserve the measured
native behavior. Do not approximate a hard lock by epsilon ranges, introduce a mod length gate,
or assert that an authored total length or clear span remains constant after native deformation.

`NetPiecePrefab.m_NodeOffset` contributes to native endpoint geometry with other edge/node offsets,
width, slope and intersection conditions. It is not an unconditional fixed reservation at each end.
Span geometry, rigid endpoints, repeated supports and preview must consume the same actual native
course geometry. Preserve `NetDividerPiece.m_PreserveShape`, vertex matching/movement and other
applicable node semantics; a shape-preserving node piece is not a general edge-length lock.

Longitudinal normalization by the native network is distinct from Bridge Builder's transverse
width transformation. General contract sections 5 and 8 still determine measured transverse part
membership: piers retain their sections, side members translate and applicable transverse assemblies
use their recorded map. Do not reuse that X-axis decision as a Z-axis scaling rule. Record the two
stages and their shared interfaces separately, including source-material projection behavior.

All of this remains offline selection and existing-form immutable recipe data. It creates no virtual
Network files, runtime intermediate, additional conversion pass or road-kind prerequisite. Only normal
final Bridge Builder generation realizes the selected native graph and its ordinary length behavior.

## 4. Record the recipe and use existing generation

Offline extraction and measurement must emit immutable, reproducible hardcoded data in the same
form as existing real bridge prototype recipes, including every required reference, measured
parameter, placement rule, authored membership and LOD correspondence. Save only that recipe
representation for the virtual prototype, never an intermediate network asset.
Follow general contract section 2 for embedded numerical data and shared affine execution. Runtime
must not rediscover structure from meshes or require the developer's original authoring prefab.
Required resource availability and permitted actual-input carry remain governed by that contract.

The conceptual intermediate has zero runtime creation overhead: do not introduce a standalone
virtual-prototype schema, class, factory, serialization, registration, cache, network construction
or additional conversion pass. Normal Bridge Builder generation consumes the existing-form recipe
directly. Its ordinary data decoding and generation work remain necessary; this requirement does
not claim zero total generation cost or remove existing request-local data ownership rules.

Pass this virtual prototype recipe through existing Bridge Builder style generation. The player's
selected network supplies the actual elevated composition, lanes and native network behavior;
apply the existing width transformation rules without a prototype-imposed road-type restriction.
Real resource/API failures remain explicit errors, not a promise that unsupported operations succeed.
Map the recorded support roles into the final native graph, including applicable `NetPieceObjects`
and node `NetSubObjects`, and verify native spacing/endpoint semantics rather than assuming a
constant-distance grid. Preserve native lane connectivity and avoid duplicate supports. Preserve
the documented width allowance on matching boundaries. Same-style width variants remain one style;
check for an existing native or supported equivalent before adding a style ID. New styles require
the normal router, catalogue, preview, management and complete localization integration.

The [material contract](MATERIAL_GENERATION_CONTRACT.md), general contract's all-LOD and per-side
sidewalk requirements, and [persistence policy](Bridge-Asset-Persistence.md) apply unchanged.
Final generation follows general contract sections 16 and 19 for native fees and unlocks, with
their actual applicable source provenance recorded. Do not invent intermediate template donors,
fees, unlock conditions, manager totals or pricing-only assets.

Persist the final real bridge's owned derived geometry and native payloads with independent per-owner
dependency snapshots. The conceptual intermediate has no separately persisted network or shared
generated dependency; final bridge output and its required resource persistence remain unchanged. Independence
has the meaning in general contract section 19; this does not introduce a mod-disabled requirement.
Preview of the final generated graph must traverse `NetPieceObjects`, nested objects and node supports through the
[native preview path](Bridge-Preview-Specification.md), including meshless carriers.

## 5. Staged verification and acceptance

| Stage | Required evidence |
| --- | --- |
| Source record | Version-matched description parameters, units, boundary definitions, complete prefab/LOD inventory and explicitly resolved discrepancies. |
| Virtual prototype | Existing-form hardcoded data with reproducible dimensions/frame, adjacent-span seams, support roles/counts/transforms and all-LOD correspondence; no intermediate network files or runtime creation/conversion stage. |
| Bridge Builder generation | Source-width identity, selected-width transforms, final native composition and lane connectivity, realized support placement, all-LOD member/material coverage and zero/one/asymmetric sidewalk cases. Check reference, shorter and longer native courses, repeated spans, sequence state boundaries, endpoint/node offsets and rigid-support dimensions; separate authored dimensions from realized dimensions. |
| Preview and persistence | Piece-object and node-object coverage without duplicate supports; saved reference closure, independent snapshots and unchanged source assets. |
| Human game acceptance | Actual generated sample, near/far appearance, repeated spans and endpoints, usable native traffic, fees/unlocks, cold start and existing-save behavior with required content available. |

Use actual composed edge and node curves for length checks, including relevant slope/intersection cases.
Check native length warnings and placement behavior without replacing them with mod acceptance gates.
Compare every retained structural face and LOD across the deforming-span/rigid-support interfaces;
count endpoint halves and shared supports by their distinct authored roles, not coincident positions.
Record automated results separately from human acceptance and identify missing samples explicitly.
These stages verify implementation; they must not become structural/appearance rejection gates for
player-selected widths, overlap, clearance or signed scales. Preserve genuine missing-resource,
schema, arithmetic, native API and persistence failures under the general error contract.

## 6. Delivery and localization

For an implementation, follow the normal style integration, localization, build and deployment
requirements; no separate conversion UI or extra player approval flow is implied. Review every
new runtime label/error path in all supported languages. A documentation-only specification changes
no runtime strings and requires no build, game shutdown or deployment. Keep source-specific evidence
under the bridge-style archive and link it without duplicating this normative workflow.
