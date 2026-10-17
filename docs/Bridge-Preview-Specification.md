# Bridge preview specification

## Contents

- [1. Scope and authority](#1-scope-and-authority)
- [2. Preview identity and entry points](#2-preview-identity-and-entry-points)
- [3. Complete bridge assembly](#3-complete-bridge-assembly)
- [4. Movable bridges](#4-movable-bridges)
- [5. Geometry and appearance fidelity](#5-geometry-and-appearance-fidelity)
- [6. Materials and temporary resource ownership](#6-materials-and-temporary-resource-ownership)
- [7. Rendering, framing and performance](#7-rendering-framing-and-performance)
- [8. UI and errors](#8-ui-and-errors)
- [9. Verification and change scope](#9-verification-and-change-scope)
- [10. Supporting records](#10-supporting-records)
- [Organization revision record](#organization-revision-record)

## 1. Scope and authority

This document consolidates the existing requirements for creation and management previews of
fixed, movable and supported double-deck bridges. It specifies required behavior, not a claim
that every implementation or visual check is complete.

Follow the [project contract](agent-contract/PROJECT_CONTRACT.md) and
[bridge generation contract](agent-contract/BRIDGE_GENERATION_CONTRACT.md), the
[movable bridge contract](agent-contract/MOVABLE_BRIDGES_CONTRACT.md), and the
[asset persistence policy](Bridge-Asset-Persistence.md). These contracts govern generation,
source isolation, ownership, error handling and acceptance. This specification does not authorize
new generation exceptions, custom shaders or changes to persisted bridge behavior.

Documentation baseline: `dev`, HEAD `3b6363ada9f1f147b1a660e20c0d6246a54f82dd`, 2026-10-16.
Existing working-tree changes are preserved. This addition requires no code change or deployment.

## 2. Preview identity and entry points

- Creation previews use the selected road or track, bridge style, supported deck arrangement and
  direction through the same recipe and generator route as formal creation. A catalogue icon or
  generic road model is not a substitute for the requested bridge.
- Management previews show the existing bridge by its exact UUID and stored arrangement. Borrow
  its asset graph read-only; do not regenerate it from a recipe or replace its persisted data.
- Every registered style participates in preview regardless of implementation completeness.
  Do not hide or disable preview, display unfinished-style notices, or substitute another style.
  A missing generator route raises ERROR through the ordinary failure path.
- Selection changes invalidate earlier requests. A late render must not overwrite the current
  selection. Include geometry-affecting options, including lower-deck direction, in preview identity.
- Search, filters and locale changes must not regenerate an unchanged bridge. Keep identity keys
  unlocalized; use the project's localization system for visible labels and dialogs.

## 3. Complete bridge assembly

The preview must show a coherent bridge, including its road surface, markings, structural members,
supports and visible attachments. Assemble native conditional pieces and actual object graphs;
do not stretch source network meshes as if they were ordinary object meshes.

Preserve fixed-segment order, lengths, requirements and placement rules. Evaluate each segment's
state independently. Respect native mesh states, stacked supports and placeholder selection.
A meshless parent can legitimately have visible children; an inactive mesh is not a missing asset.
Missing required geometry or material data is an error, not permission to show a partial bridge.

For double-deck bridges, show both decks with their stored separation, offsets and direction.
Use a real supported double-deck arrangement. Do not invent an extra deck or alter its height
for presentation convenience.

The current illustrative course uses a main-deck elevation of 30 m above flat presentation ground.
This is a preview setting, not a modification of the bridge's permitted construction elevations,
terrain behavior or saved geometry. The preview is not a city simulation or an audit sample.

## 4. Movable bridges

A movable preview must simultaneously show:

1. The approach bridge section on each end, with the relevant stationary structure and supports.
2. The complete central mechanism, including its fixed parts.
3. The moving parts in their open pose, with their attached road, markings and mounted objects.

Do not return a mechanism-only scene or an ordinary closed road spanning the opening. Do not add
stationary road geometry or ordinary intermediate supports inside the shipping opening.

Derive the crossing extent, transition alignment and approach arrangement from the generated
network and its measured native mechanism data. Do not assume one bridge family's span, hinge,
opening angle, lift height or section mapping applies to another. Presentation span selection
must preserve the native placement rules and maintain continuous fixed approach joins.

Evaluate the native procedural skeleton and its parent/connection relationships. Rotate bascule
leaves around their authored hinges; translate lift spans according to their native motion data.
Fixed towers, supports and control buildings remain fixed. Attachments follow their correct
parent transforms. Expand render and camera bounds to include the open pose.

Opening is temporary presentation state. It must not rewrite the generated prefab, drive city
traffic signals, open placed bridges, or imply that shipping compatibility has been verified.

## 5. Geometry and appearance fidelity

Use the generated bridge's geometry and native material behavior. Preserve source vertex formats,
material-slot ordering, UVs, normals, transparency and required LOD relationships. Follow the
existing movable conversion approvals; preview must not introduce additional approximations.

Perforated grating in an original bridge is not inherently damaged geometry. Distinguish it from
missing triangles, stretching, overlapping surfaces and material errors using original and actual
generated data. The current selected-road conversion replaces donor road surfaces with the selected
road's surfaces. This specification does not change that rule to retain donor sidewalks. Any such
change requires a separate explicit requirement and corresponding generation work.

Do not hide a generation defect with preview-only replacement geometry, depth adjustments,
material substitution or omitted components. A correct preview must not conceal an incorrect
persisted bridge. Conversely, diagnose preview-only failures independently from saved geometry.

## 6. Materials and temporary resource ownership

Reuse compatible native assets. Geometry motion alone does not justify creating new textures,
materials or shaders. Necessary native network-to-object conversions follow the persistence policy
and must document the actual representation change. All moving markings use existing materials and the approved atlas-pattern approximation,
without material descriptor creation or a pixel-baking fallback.

Private Unity material instances are permitted when preview bindings, animation or rendering state
must differ from shared game materials. Never mutate the game's shared instances. Such copies do
not authorize new custom shaders or permanent asset export.

Keep generated preview assets in their private temporary scope. Where serialized CID lookup cannot
resolve a private asset, retain its direct object references and balanced property leases for the
preview lifetime. Do not globally publish temporary assets to repair a lookup failure.

Destroy renderers and release render resources before disposing their owning meshes, surface leases
and temporary database. Apply cleanup on success, failure, cancellation, selection change and panel
closure. Borrowed game assets and existing bridges must survive preview disposal unchanged.

### Existing material selection

Preview and formal generation use the same existing-material selection. Do not create material
descriptors or bake appearance textures. See [the current policy](Movable-Road-Material-Reuse.md).

## 7. Rendering, framing and performance

- Frame the complete bridge with its open moving parts and both ends visible. Include attachments
  and deformed bounds; normalized network bounds are insufficient. Do not crop required geometry.
- Use an isolated presentation scene and camera, with a transparent background and readable colors.
  Preserve translucent and cutout surfaces; do not use color-key transparency or repaint materials.
- Preview lighting must be independent of the city. The existing independent-shadow requirement
  remains; any implementation limitation must be recorded rather than silently treated as compliant.
  Do not modify city lights, camera settings, global HDRP settings or engine shadow code.
- Submit rendering and GPU readback in the proper engine-frame order. Do not capture before the
  relevant rendering work has completed or reuse stale pixels as a successful result.
- Reuse valid results for unchanged inputs. Expensive repeated pixel/matrix work uses vendor-neutral
  GPU facilities; overlap independent work where thread affinity and resource ownership permit.
  Record measured timings when investigating stalls. This specification introduces no invented
  latency threshold and does not equate faster generation with visual correctness.

### Posed camera bounds

For procedural moving geometry, camera framing must use indexed vertices transformed by their own native bone indices. Applying every bone to the entire mesh bounds creates nonexistent space and is forbidden. Aggregate framing bounds in the fixed camera basis, including both approaches and mounted objects. Bulk skin transforms and bound reduction execute on the GPU. See [the framing correction](Movable-Preview-Framing-And-Material-Followup.md) for implementation and verification limits.

## 8. UI and errors

The preview frame contains no text in idle, loading, success or failure states. Loading uses the
explicit loading state. Stop it on failure and reject stale results. Keep the image accessibility
label without exposing broken-image fallback text.

Report operation failures through the existing localized dialogs and ERROR logging. Permanently
omit the old inline error footer below the action buttons. Messages and controls follow the same
behavior for every registered style, regardless of completeness.

Logs must preserve the request identity, failure stage and underlying exception. Identify the
missing prefab, material slot or resource when known. Do not replace an internal invariant failure
with a silent fallback, partial preview or false success.

## 9. Verification and change scope

Inspect the relevant original and generated geometry, component graph, native behavior and logs.
A successful build, an emitted image or valid indices alone does not establish visual acceptance.
Do not run synthetic visual-generation unit tests or use computer-control tools for this project.
Humans perform the in-game checks.

| Area | Required human check |
| --- | --- |
| Fixed bridge | Complete connected spans, supports, markings and attachments; no clipping |
| Movable bridge | Both approaches and central mechanism visible together; moving parts open; fixed parts stationary |
| Double deck | Both decks visible with correct native separation and selected direction |
| Appearance | Correct surface coverage, grating/transparency, colors and markings; no torn or overlapping surfaces |
| Bounds and LODs | Full open envelope framed; relevant generated near/far geometry consistent |
| Lifecycle | Repeated selection, cancellation, close/reopen and create/manage switching without stale images or leaked resources |
| Isolation | City lighting, source roads, saved bridges and formal asset files remain unchanged by preview |
| Failure | Ordinary error dialog and useful ERROR log; no inline footer, unfinished notice or partial success |

State separately what was inspected, implemented, built/deployed and visually accepted. Keep unresolved
appearance or animation findings explicit. Preview acceptance does not prove vessel coordination,
traffic clearance or cold-start persistence.

Preview-only or documentation-only changes remove no existing bridges. Code changes require the
project's build/deployment workflow; documentation-only updates require no shutdown or deployment.
Generation changes remain subject to the exact affected-style cleanup rules.

## 10. Supporting records

- [Preview rendering implementation and diagnosis](Bridge-Preview-Render-Path.md): chronological
  evidence; earlier implementation settings can be superseded by later entries.
- [Track preview ownership](Preview-Track-Ownership.md): source isolation and direction failure history.
- [Preview shadow lifecycle](preview-shadow-lifecycle.md): historical native-rendering investigation,
  not proof that the current shadow implementation meets acceptance.
- [Runtime UI and localization](Runtime-UI-Localization.md): selection, loading and dialog behavior.

## Organization revision record

Organization-only revision (2026-10-17): branch `dev`, HEAD
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd`; rollback reference
`refs/rollback/contract-organization-20261017`. Existing working-tree changes are preserved.
Original requirement text, examples, exceptions and revision records are retained. Existing
numbered sections and heading anchors are preserved; headings, placement and navigation are
organized without changing requirements or implementation acceptance.
