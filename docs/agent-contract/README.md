# Contract documents

## Contents

- [Shared sidewalk requirements](#shared-sidewalk-requirements)
- [Specialist contracts](#specialist-contracts)
- [Measured fixed-bridge rules](#measured-fixed-bridge-rules)
- [Style registration and UI](#style-registration-and-ui)
- [Implementation records](#implementation-records)
- [Documentation language and evidence labels](#documentation-language-and-evidence-labels)

[PROJECT_CONTRACT.md](PROJECT_CONTRACT.md) governs project-wide workflow, documentation and metadata.
[BRIDGE_GENERATION_CONTRACT.md](BRIDGE_GENERATION_CONTRACT.md) is the independent general bridge
contract and the routing authority for all bridge generation work. Read it for fixed and movable
bridges alike, then follow its links to the applicable specialist contracts.

Apply the latest explicit user requirements within their stated scope and keep overlapping
normative paragraphs and AGENTS.md summaries synchronized. File modification time alone does
not establish a new requirement. The material contract governs material reuse, preparation,
bounded runtime conversion and owner size reporting and the package byte limit; the general bridge contract retains
geometry, ownership and complete-Prefab comparison requirements. Implementation reports, handoffs,
candidate inventories and old acceptance records are evidence for their recorded build/input only;
they do not override these contracts or establish acceptance of a later material migration.

## Current material capability boundary

Complete multilayer adaptation is [not implementable under current requirements](MATERIAL_GENERATION_CONTRACT.md#current-multilayer-compatibility-limit).
Compatibility output does not establish complete fidelity. Saved bridges must remain
independent of Bridge Builder; dynamic-shader registration dependencies are not an
alternative. [Selected-road decals](MATERIAL_GENERATION_CONTRACT.md#source-preserving-selected-road-decals)
retain original artwork/UVs through derived native objects, with explicit hashed-alpha
blend differences rather than the removed shared white/yellow paint substitution.

## Shared sidewalk requirements

**Mandatory for every bridge generator:** roads may have no sidewalks, a sidewalk on only one
side, or sidewalks of unequal widths. Sidewalk-dependent components, including inner bridge
railings and bridge-authored sidewalk portions, must adapt independently to each side's actual
sidewalk presence, position and width in the selected road's **elevated-mode composition** and
upgrade state. Do not fabricate a sidewalk-dependent component on a side without a sidewalk or
assume symmetry. This applies to fixed and movable bridges, previews, saved assets and all LODs.
See [bridge generation contract section 25](BRIDGE_GENERATION_CONTRACT.md#25-adapt-sidewalk-dependent-components-to-the-actual-elevated-road)
for the authoritative rule and the distinction from unrelated structural or outer safety railings.
This is a general contract routing requirement, not a movable-bridge-only rule.

## Specialist contracts

Road appearance processing (prototype composition, source-density tiling, embedded-marking
removal and derived wear/background placement) applies only to movable bridges whose roads need
that processing. Fixed/non-movable bridges retain the selected native road materials, UVs and
markings; they require no prototype road-support table, road-material replacement or marking
cleanup under these rules. Structural source-UV/density rules still apply to every bridge.
A fixed running cap within a movable bridge remains part of that movable road-processing scope.

The [general source-mapping rule](BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode) preserves non-road source modes for all bridges. Movable roads requiring processing use
prototype-supported appearances tiled at original resolution/physical density without stretching
in every LOD. Fixed bridges retain selected native road materials and markings.

For movable bridges, reuse original non-road materials. Road composition/precomposition (offline material baking outside the game) has
two necessary correspondence conditions: verified actual receiver appearance and matching measured
prototype material at the target physical part. Follow [physical part correspondence](MATERIAL_GENERATION_CONTRACT.md#physical-prototype-part-correspondence). Test asphalt, sidewalk surfacing and wheel-track/rut effects independently
per prototype and physical domain. Other road types use compatible originals or corresponding native object road materials under
material section 4, preserving complete source layers and worldspace behavior. The
[complete material pipeline](MATERIAL_GENERATION_CONTRACT.md#complete-material-processing-without-propagated-exceptions)
returns explicit outcomes without propagated exceptions and supports valid unknown sources
without name/CID whitelist adapters. Supported road types require fixed-density tiling and all-LOD marking removal;
non-road source modes remain unchanged. This is a generation rule, not a reverse-engineering finding or proof that the
current implementation complies. Road processing must not mutate a shared structural Surface.

Prototype-supported road appearances must tile at original resolution and physical texel density,
never stretch, including fixed running caps, finite terminals and every LOD. Remove embedded road
markings from every road-material channel and mip; preserve intended selected-road paint through
the applicable native path (the separate skinned paint mesh for movable mechanisms). Necessary
bounded offline repeats may be authored from pure original road content with aligned PBR donor
coordinates and complete periodic mip chains; do not claim new periods as original evidence.
Domains sharing the same evidenced prototype appearance require coherent source/PBR, density, orientation and phase; intentionally different physical-part materials remain distinct. Wear uses the
same complete measured source and matching same-source background; translate the whole pattern's
measured visual center to actual native lane centers while preserving physical width, internal
relationships and longitudinal density. No vehicle lane means no wear. Non-road finite/stretch
modes retain their own source evidence. No full-span pixels, period cuts, arbitrary downsampling,
custom shaders that cannot independently cold-load or a relaxed package byte limit are permitted.

[Material generation](MATERIAL_GENERATION_CONTRACT.md) governs every bridge material path,
including preview, creation, export and all LODs. Its first rule prohibits creating new materials
when reusing an original material in full. It also requires original resolution and pattern
scale, repeat units tiled at original density rather than very large baked images, and durable
known, preparable constant materials prepared once offline and copied during bridge creation.
Section 4 separately permits bounded variable-content composition and necessary conversion of
selected third-party road/track inputs unavailable before runtime, prioritizing direct reuse and
descriptor-only conversion. These exceptions never cover missing known prepared assets.
Sections 2 and 5 require informational complete-owner material measurements without a per-bridge
ceiling and at most 55,000,000 bytes for the complete official SDK content ZIP. Prepared-directory
size, complete bridge size and package size are separate measurements. Native rendering
and persistence remain governed by their corresponding contracts.

The [CPU affine geometry execution rules](BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution)
apply to fixed and movable bridges. They define XYZ parameter matrices, sparse embedded-array
storage, independent numerical batches, native API thread boundaries and verification.
The [contributor generation step](../guides/Adding-Bridge-Styles-With-Coding-Agents.md#affine-matrix-generation-step)
connects these requirements to extraction, regeneration and runtime assembly.

[Generator code organization](GENERATOR_CODE_CONTRACT.md) governs base-class inheritance,
one concrete generator file per style, shared lifecycle ownership and style-specific hooks.

[MOVABLE_BRIDGES_CONTRACT.md](MOVABLE_BRIDGES_CONTRACT.md) is the bridge generation contract's normative movable-bridge
supplement, incorporated by section 21. It defines a general method derived from the implemented
pipeline: selected-road geometry conversion, physical motion domains, existing materials and
prototype UV projection, separate native skinned paint, native Prefab graph construction, selective persistence
and unlocks. **Read sections 16–24 for material, marking, binding, LOD, track and persistence requirements,
together with the material generation contract.** Section 15 records
encountered failures and required solutions. Acceptance is recorded
per style; the general method does not claim every registered generator is complete. Supporting
audits retain measurements and implementation history rather than overriding current requirements.
[Bridge asset persistence and reference policy](Bridge-Asset-Persistence.md) is the normative
supplement incorporated by bridge generation contract section 17. It distinguishes owned generated data,
unchanged native references, external dependency snapshots and temporary preview objects.
The repository and directory-level AGENTS.md files route agents to the applicable requirements.

[Bridge preview specification](Bridge-Preview-Specification.md) consolidates the existing preview
requirements, including complete approaches and open movable mechanisms, temporary resource
ownership, UI errors and human acceptance. Implementation history is maintained separately.

## Measured fixed-bridge rules

Use measured source geometry, assembly membership and LOD correspondence for fixed bridges. The general generation contract governs project requirements. Bridge-specific research records are maintained separately as implementation evidence.

## Style registration and UI

Bridge generation contract section 22 requires immediate UI synchronization and generator-router integration
whenever a new bridge style is created, with names in every supported UI language in the same
implementation increment.

## Implementation records

Record implementation and verification by stable style ID, build and tested scope. Keep admission and acceptance requirements in the contracts.

## Documentation language and evidence labels

All project documentation is written in English. Preserve literal source identifiers, paths
and diagnostic quotations, with English explanations. Runtime localization is separate.

For a contributor workflow, see [Adding a bridge style with a coding agent](../guides/Adding-Bridge-Styles-With-Coding-Agents.md).

The [persistence directory and migration specification](Bridge-Asset-Persistence.md#6-approved-owner-directory-layout)
places owned resources and dependency snapshots below each UUID. Creation and migration share
that layout, including VT and mid-mip resources. Native cold-load acceptance requires separate
in-game verification. Section 7 defines migration safeguards.

Dynamic material shaders are governed by [the scoped material requirements](MATERIAL_GENERATION_CONTRACT.md#dynamically-generated-and-independently-persisted-material-shaders): actual source generation, Player-capable loading and independent owner-local cold loading, without a continuing mod synchronization service. This permission does not relax geometry, texture-content or source-ownership rules.
