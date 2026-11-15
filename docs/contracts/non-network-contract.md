# Non Network Contract

Required companion: [Non Network Spec](../specs/non-network-spec.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="non-network-1-scope-and-required-sequence"></a>

## Scope and required sequence

This is a normative supplement to the [general bridge generation contract](reading-map.md#bridge-generation-contract).
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
[movable bridge contract](reading-map.md#movable-bridges-contract); static conversion does not implement motion.
This document specifies required work, not completed implementation or asset acceptance.

<a id="non-network-4-record-the-recipe-and-use-existing-generation"></a>

## Record the recipe and use existing generation

Offline extraction and measurement must emit immutable, reproducible hardcoded data in the same
form as existing real bridge prototype recipes, including every required reference, measured
parameter, placement rule, authored membership and LOD correspondence. Save only that recipe
representation for the virtual prototype, never an intermediate network asset.
Follow [general contract section 2](../specs/generation-spec.md#generation-2-the-archetype-is-hardcoded-never-copied) for embedded numerical data and shared affine execution. Runtime
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

The [material contract](reading-map.md#material-generation-contract), general contract's all-LOD and per-side
sidewalk requirements, and [persistence policy](reading-map.md#bridge-asset-persistence) apply unchanged.
Final generation follows [general contract sections 16](../specs/generation-spec.md#generation-16-preserve-native-construction-and-upkeep-pricing) and 19 for native fees and unlocks, with
their actual applicable source provenance recorded. Do not invent intermediate template donors,
fees, unlock conditions, manager totals or pricing-only assets.

Persist the final real bridge's owned derived geometry and native payloads with independent per-owner
dependency snapshots. The conceptual intermediate has no separately persisted network or shared
generated dependency; final bridge output and its required resource persistence remain unchanged. Independence
has the meaning in [general contract section 19](persistence-contract.md#generation-19-independent-native-persistence-and-additive-legacy-conversion); this does not introduce a mod-disabled requirement.
Preview of the final generated graph must traverse `NetPieceObjects`, nested objects and node supports through the
[native preview path](reading-map.md#bridge-preview-specification), including meshless carriers.

<a id="non-network-6-delivery-and-localization"></a>

## Delivery and localization

For an implementation, follow the normal style integration, localization, build and deployment
requirements; no separate conversion UI or extra player approval flow is implied. Review every
new runtime label/error path in all supported languages. A documentation-only specification changes
no runtime strings and requires no build, game shutdown or deployment. Keep source-specific evidence
under the bridge-style archive and link it without duplicating this normative workflow.

