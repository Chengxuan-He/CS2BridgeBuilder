# Non Network Spec

Required companion: [Non Network Contract](../contracts/non-network-contract.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](../contracts/reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="non-network-2-source-evidence-and-parameter-meanings"></a>

## Source evidence and parameter meanings

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

<a id="non-network-3-describe-the-virtual-bridge-prototype"></a>

## Describe the virtual bridge prototype

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

<a id="non-network-select-longitudinal-behavior-from-native-evidence"></a>

## Select longitudinal behavior from native evidence

Use the [complete installed native length inventory](../archive/audits/native-bridge-length-handling-audit.md)
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
width transformation. [General transverse mapping](generation-spec.md#generation-5-a-generated-tower-differs-from-its-archetype-in-width-alone) and [part placement](generation-spec.md#generation-8-stretch-or-translate-is-decided-by-one-thing-does-the-part-cross-the-bridges-centre) still determine measured transverse part
membership: piers retain their sections, side members translate and applicable transverse assemblies
use their recorded map. Do not reuse that X-axis decision as a Z-axis scaling rule. Record the two
stages and their shared interfaces separately, including source-material projection behavior.

All of this remains offline selection and existing-form immutable recipe data. It creates no virtual
Network files, runtime intermediate, additional conversion pass or road-kind prerequisite. Only normal
final Bridge Builder generation realizes the selected native graph and its ordinary length behavior.

<a id="non-network-5-staged-verification-and-acceptance"></a>

## Staged verification and acceptance

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

