# prepared materials evidence

[Behavior specification](../../specs/material-spec.md) · [Interface contract](../../contracts/material-contract.md)

Retained inspections, implementation stages and limitations. These records describe their stated inputs and versions; they do not establish current implementation or acceptance. Historical recipes are not competing current requirements.

<a id="current-requirements-and-evidence-scope"></a>

## Current requirements and evidence scope

The conversion entry below is the current material-realization design. Numbered
increment sections record historical coverage at that increment, not current missing APIs.
Native126 completed GPU atlas channel readback/release and native surface extraction
result APIs; remaining planner/chart work is tracked separately.

<a id="source-coordinate-density-investigation-native137"></a>

## Source coordinate-density investigation (native137)

This historical investigation rejected domain approximation and receiver-dependent baking. It preserved both coordinate fields in a bounded dynamic-shader sampling probe but did not achieve mod-disabled cold loading. The current material contract subsequently authorizes two custom programs and requires mod-enabled startup registration; this historical finding is not a prohibition on the active implementation. The measurements below concern the recorded source, not a claim about the currently installed build.

Compare the actual source vertex/shader coordinate protocol with the uploaded
converted geometry. Network longitudinal UV rates are evaluated before object
rendering; they must not be mistaken for already evaluated mesh coordinates.
Texture ST applies only when the source shader consumes it. Matching texture
resolution alone does not establish matching physical density.

For the inspected source, the original network fragment samples base UV.xy and
position.xz times world scale independently, then blends with base MaskMap.B.
Its base ST is identity and is not consumed by that shader. The retained base field
has approximately 0.125 UV/m laterally and 0.03125 UV/m longitudinally, giving
128 texels/m with its 1024x4096 image. The omitted 1024x1024 world layer has
0.4 UV/m, or 409.6 texels/m. Their ratio is not a legitimate automatic correction
of the base UV. Scaling base UV also moves/scales the base atlas's borders and bands.

The earlier finite-period composition investigation is recorded evidence only,
not an active implementation plan: nominal base/world periods of 32m and 2.5m
require a 160m joint period (65,536 pixels at world density). The source mask has
near-one rather than exactly-one values and mixed boundaries. Neither thresholding
it nor baking the receiver is authorized for this repair. If original base mapping
already matches, a new density/phase target must be specified before changing it;
a UV-only multiplier cannot reproduce two independent texture sample fields.

<a id="runtime-result-api-increment-native122"></a>

## Runtime result API increment (native122)

Road setup, private source decoding, composed resource writes/ownership transfer and native
cache lookup now expose result APIs consumed by shared generation and dependency persistence.
Native failures retain source/stage context instead of becoming an empty successful material or
cache relation. All ten movable mechanism callers propagate failed preparation to the existing
preview/formal rollback boundary.

Original object descriptors can be reused by actual shader identity and supported instance-buffer
protocol, including a new template ID using that same shader. This is not support for arbitrary
new shaders: shape/normal animation, geometry tiling and dilated protocols require additional
buffers not provided by the current object route. Known partial OriginalBase/TiledPaving and
world-only corresponding conversions are no longer accepted as full source reuse. Original
network descriptors survive composition unchanged; unsupported native network realization still
returns an explicit incomplete-representation result. Some previously approximated roads can
therefore fail instead of showing a silently incomplete material. The arbitrary-material goal
and all-internal-API noexcept migration remain unfinished; native122 is an incremental result
pipeline improvement, not renderer completion or game visual acceptance.

<a id="classification-atlas-and-vt-result-increment-native123"></a>

## Classification, atlas and VT result increment (native123)

Source classification, atlas texture binding and plane validation, native VT texture/surface
parsing, MidMips subset copying and generation-side mip-level queries now return explicit
results consumed by the production preparation path. A failed classification is propagated;
it is not treated as an ordinary unmatched material. Private decoded textures are owned before
binding and are released on failed preparation, including partially decoded surface stacks.
A valid cache without the requested source is distinguished from malformed cache bytes.

This remains an incremental implementation. Ordinary Texture decoding, some prepared-material
and projection helpers, the historical RequiredLevels wrapper and MidMips membership parser
still contain throwing APIs behind operation boundaries. Original network descriptors are
preserved, but arbitrary network/world/projection rendering on moving geometry is not yet
implemented. Unsupported representation is distinct from damaged resources; neither may be
reported as successful full-fidelity adaptation. Native parameter-calculator tests do not
constitute a parent/bone renderer test or in-game visual acceptance.

<a id="texture-cache-membership-and-patch-result-increment-native124"></a>

## Texture, cache membership and patch result increment (native124)

Ordinary Texture mip decoding and native decompression now return explicit failure results
with no private Texture output on failure. Native MidMips membership and required-level
readers no longer expose throwing compatibility wrappers. Exact historical owned-cache lookup
uses a result delegate through inspection/persistence, distinguishing failed lookup from valid
absence. Prepared-file preflight returns a result and no partial input list. Catalogue
lookup, registration, retention and shutdown expose explicit results to production callers.
Successfully registered immutable installed source handles survive a failed attempt for retry;
failed loads do not publish a completed Surface. Pending native cache refresh remains pending
until refresh succeeds. This is distinct from ownership of newly generated outputs.

Patch construction records invalid declarations as an explicit Error, propagated by derivative
patches and rejected by atlas composition before source loading or output creation. Prepared
vertex/tangent projection uses result APIs, and atlas consumers reject failed projection without
returning partial geometry/material output. Structural Retile and native network mapping expose
transactional result APIs: a failed operation disposes only its newly owned models and rolls
back appended outputs, preserving earlier live models. Prepared levels and patches carry
lookup errors to composition. All ten movable paint callers check the shared result API and
report failure instead of treating a missing paint assembly as success.

The next result-API increment also covers original-atlas wrap, UV frame and parent-frame
mapping, serialized Surface references, native structural preservation and the existing measured
prototype descriptor resolvers. Retile checks frame/projection errors before exposing outputs.
Dependency source reads, owner snapshots, cache filenames/paths/membership and persisted cache
relations return diagnostics. Relation reads validate temporary dictionaries before committing
edges; failures remain sticky for that request. Snapshot cancellation and IO failure return an
empty failed snapshot after releasing leases; the readiness callback cannot consume it.

At native125, internal result conversion was incomplete: GPU atlas channel readback, native network
surface extraction, embedded UV recipe/chart decoding and LOD material-slot planning retain
throwing internals behind operation boundaries. Offline-only packing and retired decal helpers
must not be confused with reachable generation paths. Source/schema errors and material capacity
failures remain material-pipeline work, even where originally expressed as argument exceptions.
Pure unrelated geometry/programming contracts are outside this material API conversion.

The independent persistent-asset requirement remains in force. Runtime synchronization of
network buffers to moving bridge leaves is prohibited, including as a workaround for unsupported
material protocols. The incomplete original-network rendering capability is unchanged by this
result-API increment; no arbitrary third-party renderer support or visual acceptance is implied.

<a id="genuine-repeat-unit-preparation"></a>

## Genuine repeat-unit preparation

The isolated producer fixture verifies distinct authored mip colors byte-for-byte, unchanged
full mip count, native Repeat settings and one conversion across different use aliases.
It is not a real-bridge rendering result. Real-unit outputs, alias equivalence, native graph
closure and complete generation remain to be verified per bridge. Structural regions lacking
an evidenced period still require a density-preserving native representation; no per-triangle
fold textures or deleted large prepared library may be restored as a fallback. No final
package-size or human-acceptance claim follows from a small partial library.

A concrete structural case is `LiftBridge04 Mesh`, mesh 0, LOD 0, face 4,
measured domain 2. The actual width plan with delta 2 changes the lateral endpoints from
approximately +/-5.1018066 m to +/-7.1018066 m. Its original Surface is
`6b973ac2133fa9bd8d897bc9eff3363f` (Default, VT). The original triangle fits the measured
46-by-1266-source-texel chart; the density-preserving generated triangle has a 1762.999-texel
edge, longer than that chart's 1266.835-texel diagonal. Translating or rotating the corrected
UVs inside this chart cannot fit that edge without reducing density. The exact original and
generated positions, UVs, loops and assembly identity are recorded in
`artifacts/shared-material-pipeline/structure-counterexample/receiver.json`.

The inspected compatible Default shader exposes affine UV scale/offset and whole-texture
sampling. Native VT transforms address the registered full source allocation, not a user-defined
subregion period; see `docs/archive/bridge-styles/Draw03/tools/PreparedMaterials/Draw03/native-layers.md` and its native metadata
inventory. A verified larger same-pattern domain or genuine period could make source reuse or
the new region route valid. Neither follows merely from this chart's rectangular shape. In their
absence, whole-atlas repetition samples unrelated regions, squeezing UVs violates the density
rule, and preparing this receiver's arbitrary crop would violate genuine-unit consolidation.
The merged structural LOD subdivision permission in bridge generation contract section 5 is
limited to representing authored parts' separate width transforms. This detailed single-part
face is not that case; movable contract section 24.6 also excludes physical texture-period cuts
for nonrectangular domains. Subdivision alone would not establish a repeating source pattern.
The original strip's BaseColor and Mask (including alpha) now provide strong evidence for a
404-texel repeating steel pattern. Normal detail is not byte-periodic; at the 404 seam its
G/A differences are within the original strip's adjacent-detail range. The source strip and
boundary measurements are recorded in the Lift04 inspection and
`artifacts/shared-material-pipeline/structure-counterexample/strip-boundary.json`.
This supports preparing and checking that evidenced unit, with the full 1266-texel source strip
as coverage, without pretending exact normal periodicity. The bridge's full-mip output and
production binding must pass before this case is counted as implemented; other uncovered
structural domains are not automatically covered by this one steel sample.

An original atlas island can have different samples at its two authored edges. Native Repeat
filters across the isolated unit's opposite edges, so exact equality to both original edge
samples is not a valid acceptance assumption. Preserve source density, channel interpretation,
pattern scale and the authored mip inputs; record edge differences for human inspection under
the accepted existing-atlas pattern differences. Lift01's measured normal endpoints differ by
0.619608 on its longitudinal period; this is distinct from missing normals, density loss or a
wrong channel binding. A periodic self-comparison alone does not establish original appearance.

The former shared SkinnedPaint route is historical: it reduced native line art to
constant white/yellow samples and incorrectly gave unknown rail artwork a full-width
white footprint. It is not the selected-decal implementation contract.

<a id="historical-offline-paint-preparation-not-the-selected-decal-route"></a>

## Historical offline paint preparation (not the selected-decal route)

The retained offline producer prepared `Paint/SkinnedPaint.Surface` from original
source `4b5500b1b5c270890d99149f7f62e51a`, preserving its first-layer VT backing,
dimensions and mip chain. These files and authoring diagnostics are historical
package evidence. They are not the runtime selected-decal workflow, do not justify
white/yellow substitution, and are not pending steps for the source-preserving fix.

<a id="offline-cable-preparation"></a>

## Offline cable preparation

The prepared cable Surface CID is `48419f3a3b00d6aab0d4ac878646c8b9`; each complete style-local
cable graph occupies 239 bytes including sidecars. The source Surface CID is
`106cc56e9f15a135bb6ea7b99d675c2d`, with SHA-256
`8ea1eb5ec3e434c92c7dd010f71aaab58e8cf37b1a28e48f2757412edf3ca219`.
The preparer pins both that source and the extracted template and fails on changed evidence.

<a id="offline-worldspace-asphalt-conversion"></a>

## Offline worldspace asphalt conversion

**Implementation limitation:** the historical recipe below transfers selected base
texture channels and measured UV/tangent inputs. It does not reproduce every network
shader layer, world-dependent operation or projection input. Zero control data is
not a substitute for the original shader's additional layer logic. Use of the recipe
is therefore not evidence of complete conversion for arbitrary network materials;
the current material contract requires explicit incomplete results where required
semantics cannot be represented. The result-API work does not close this renderer gap.

The shared native Road source uses network worldspace template 17. Its object conversion binds
the same albedo, normal and mask textures in template 0, with a zero control mask and native
octahedral tangents. This is a recorded conversion recipe, not an instruction to convert every road. Prepare it
only when required by a prototype-supported road composition; unsupported road types retain
their original materials. Preserve the source worldspace UV scale separately in geometry; do not bake a
bridge-sized asphalt texture. Match all three native texture identities before reusing the
prepared descriptor. A changed source must retain its own texture bindings.

`tools/PrepareRoad` reads the shared native Road CID `1d7dad9f64c9fe55a809f1080c39fb92`;
source SHA-256 is `99f48ff7b6296957b5170c7fdf14ce65912c1de8eb81b15ba7c38b5189be1572`.
Recipe `BridgeBuilder/SkinnedWorldspaceAsphalt/1` produces Surface CID
`88df662f6bc8058d3ecf8f7a5d7f2239`. Each style-local graph occupies 267 bytes including
sidecars. Its original texture channels are neither baked nor copied. Different custom texture
bindings retain their own descriptor until their offline conversion coverage is complete.

<a id="gpu-and-source-result-implementation-checkpoint-native126"></a>

## GPU and source-result implementation checkpoint (native126)

Atlas channel creation, readback and release use explicit results. All readback
requests are checked before copying any mip into the output texture; an incomplete
channel is never saved. Cleanup attempts each independently owned resource, appends
cleanup diagnostics to the original failure and retains failed releases for retry.
Unconfirmed GPU requests retain their input buffers until completion is known.
Persistent engine cleanup failures are reported, not described as successful release.

Native source compilation returns `Surface.Error` / `MovableClosedRoad.Result.Error`.
The shared movable generator checks sample failure before mechanism construction;
per-LOD preparation also checks the result before consuming its arrays. Original
native-road submesh copying and appending use result APIs and roll back only outputs
created by the failed operation. These changes do not supply missing network shader
inputs or establish arbitrary source-renderer compatibility. Material-slot planning,
embedded chart validation and some lane/assembly internals still require result
conversion; the material contract remains the target rather than a completion claim.

<a id="visible-compatibility-output-native130-implementation-direction"></a>

## Visible compatibility output (native130 implementation direction)

This policy supersedes native129's rejection of valid unsupported render protocols.
The common planner returns exact reuse, equivalent conversion, or processed native
compatibility output. Only actual resource/schema/native-operation errors are fatal.
Compatibility warnings identify source, selected receiving protocol and appearance
loss; they are not generation failures or claims of full fidelity.

- Network/worldspace surfaces: retain usable source base or world PBR channels and
  matching local UV interpretation in a native object descriptor. Report discarded
  blending and frozen world anchoring. Never bind network vertex shaders to object data.
- Transparent curves/cables and unknown shaders: choose an available native object
  alpha/lighting representation from source properties; preserve source texture/color
  where possible and report alpha, refraction or sorting approximation. No individual
  source identity selects this path.
- Projected lane color: map the original source color texture/alpha over measured
  receiver patches, following their existing bones and native source UV region. Use
  local source-resolution data, bounded footprint geometry and small physical paint
  clearance; do not reintroduce a bridge-wide GPU atlas or omit the color layer.

Native130 introduced no custom production shaders or continuing runtime synchronization; its outputs use the existing independent native persistence route. Historical full-equivalence/fail-closed descriptions
below remain evidence of earlier increments, not current compatibility acceptance.

<a id="common-source-to-native-material-conversion"></a>

## Common source-to-native material conversion

The original-shader experiment demonstrated a concrete failure on native template17:
CPU-evaluated Draw01 road geometry was bound to NetCompositionMeshLitShader. Its
composition matrix/selectors and vertex protocol were not supplied by the object
renderer. Descriptor and texture identity tests did not establish rendering. Remove
that blind binding route. The active custom-program work replaces the vertex protocol while retaining source shading; it must establish actual consumer rendering, not only descriptor identity. A source-layer selection may serve as
processed compatibility output, with its differences explicitly recorded; it is not
a successful full-equivalence conversion.

<a id="current-conversion-increment-and-limits-native129"></a>

## Current conversion increment and limits (native129)

The shared `MovableMaterialConversion` entry realizes evaluated road and lane
Surfaces through the same geometry-qualified plan. The curved/default shader pair
uses opaque, cutout, hashed and double-sided native object variants when render
queue, enabled pass state, texture-stack layout and source appearance requirements agree.
A disabled source-only transparent depth pass does not require an object equivalent.
Source descriptor dictionaries are copied in full before adding missing template
defaults: native `UpdateFloats`, `UpdateColors` and related methods replace their
whole dictionaries, rather than merging individual values. Existing prepared object
Surfaces use the same compatible direct-reuse branch. Failed new resources are
rolled back; source assets are never rewritten.

This is not arbitrary-material completion. Original network composition/worldspace
materials still lack an equivalent object receiver. Curved transparent materials
also cannot use an opaque target: their queue and transparency requirements differ.
The former partial cable-to-opaque adapter is removed, not represented as an
acceptable fallback. Consequently some valid sources previously accepted by the
native127 blind binding experiment now return an explicit incomplete-representation
result. They are not corrupt, and this result is not a successful rendering repair.
The projected-color omission remains a separate unresolved source-layer issue.

Validation separates native Surface serialization from shader rendering. Diagnostic
protocol shaders exercise the real conversion and native Surface IO; a real VT
Surface verifies its complete backing header and texture identities through a
byte-exact native write/reload. These do not certify game vertex/pixel behavior or
VT streaming initialization. Game visual validation remains required.

<a id="current-multilayer-compatibility-limit"></a>

## Current multilayer compatibility limit

The investigated persisted native object protocols do not fully express independent base/world-coordinate sampling and source-mask PBR blending. Authorized generated shaders provide these computations; Bridge Builder must restore their registration at startup. This dependency is permitted and must be tested with the mod enabled.

Compatibility materials remain usable approximations with diagnosed differences;
they are not a complete multilayer repair. Keeping texture dimensions or changing
one UV scale does not restore an omitted layer, its coordinate system or blend
weights. Generated custom programs are now authorized for implementation, subject to
the mod-enabled cold-load checks below. This conclusion is bounded to the investigated current game interfaces and
unchanged project constraints, not every possible future engine implementation.

<a id="generated-skeleton-decal-and-multilayer-pbr-programs"></a>

## Generated skeleton decal and multilayer PBR programs

The previous Player probe supplies reusable dynamic DXBC/container and six-texture sampling machinery. Its fixed reflection layout and diagnostic material-function pass are not complete native HDRP programs. Extend native instance/bone inputs and required passes before integration; mod-enabled startup registration must be implemented and tested.

<a id="source-preserving-decal-realization"></a>

## Source-preserving decal realization

Historically, native139 used single-sided hashed cutout. Later deterministic-cutoff and chart-coverage experiments also did not reproduce the original continuous decal blend. These are implementation history, not the source-alpha requirement below or verified visual acceptance.

The obsolete Strokes white-ribbon fallback and source-name exclusions were removed.
The current consumer groups by original source occurrence, preserves full texture-area
UVs and source ordering, clips to the existing receivers and binds their native bones.
It uses a derived Surface with original texture identities and a generated bone-aware
decal program; descriptor identity is not claimed. Native-pass and motion validation
must cover the actual consumer, rather than infer equivalence from preserved fields.
No original road face is removed. Restoring these generated programs currently requires
Bridge Builder to remain enabled, under the startup-registration requirement above.

<a id="projected-chart-coverage-across-mips"></a>

## Projected chart coverage across mips

The installed native144 chart-coverage implementation does not satisfy this rule:
it deliberately rescales lower-mip alpha, and its object cutoff is enabled where
native CurvedDecal line sources use continuous DBuffer blending. It remains an
unresolved implementation defect, not accepted appearance or a complete repair.
Reverting only its coverage change would restore the previously observed far-line
loss; disabling object alpha testing would expose opaque rectangles. Neither is a
completed fix. Do not deploy either regression merely to report a changed threshold.

The currently inspected native DefaultDecal DBuffer mesh path does not consume the
road procedural-bone protocol; CurvedDecal additionally requires lane-instance curve
inputs. A Surface swap alone is not an evidenced independently persisted moving
representation. Report this concrete capability gap without claiming that all future
native arrangements are impossible. Independent loading and the prohibition on a
continuing mod rendering/synchronization service remain unchanged.

<a id="native-rigid-source-decal-attachment-motion-limitation"></a>

## Native rigid source-decal attachment: motion limitation

The implemented source-decal route replaced cutout/renormalized-mip approximation with native
rigid decal children bound by persisted bone connection IDs. Its current-frame motion is not
accepted: the native relative-object pass consumes BoneHistory before the procedural upload
refreshes it for the rendered deck. It cannot establish the required shared motion sample. All retained source decal kinds use
one path, preserving original channels, authored mips, alpha blending and evaluated curve UVs.
The selected-road wear exclusion remains unchanged. Native static-decal protocol conversion is
not literal identity with a curved source descriptor; test its consumed fields and sampling.
Static preview parent transforms and successful independent cold loading do not prove dynamic
alignment. All moving geometry requires the same native motion module/sample under
movable contract section 19; an added tracking or compensation controller is prohibited. Earlier same-object skinned-paint results do not prove
this child route. There is no continuing manager synchronization. The authorized generated
shader replacement must provide mod-enabled restoration; startup registration requires Bridge Builder for this implementation, as specified above.

The native static decal mesh pass reconstructs receiver positions from depth and samples
object-local X/Z using MeshSize and TextureArea; vertex UVs do not replace that protocol.
Use correctly oriented closed projection volumes, native receiver layers and source projection
frames. Preserve the source sampling transform when clipping motion domains. Validate clipped
volume boundaries separately, especially oblique seams; do not present an untested silhouette
clip as an exact shader-space clipping predicate.

<a id="generated-program-registration-transaction"></a>

## Generated program registration transaction

This section records the superseded owner-local program protocol. Current programs belong to the mod; legacy owners follow self-check-only retirement under the [current compatibility boundary](../../contracts/self-check-contract.md#self-check-obsolete-custom-program-retirement). The original protocol description below is retained as evidence, not executable current guidance.

Compile the saved program package before publishing its material template. Load its
Unity container on the main thread, clone the source template's complete material
properties, keywords, queue and texture-stack description, and replace only the
shader with the corresponding generated protocol. Keep the original library entry
unchanged. A stable program-content/template identity selects the new entry; a hash
collision must not overwrite an unrelated template. Repeated owners may share the
immutable loaded program while each owner retains its own saved program dependency.
The registry retains the bundle and material for the native consumers' lifetime;
mod disposal must not unload a shader still used by saved or preview geometry.
Failed registration destroys only its unpublished material/container and returns a
diagnostic. Registration alone does not establish owner-file persistence or startup
restoration: both must be connected before enabling the converter route.

Persist each demanded generated package as an owner-local native `FileAsset` in
`MaterialPrograms`, with a versioned text envelope containing the source-template
identity and the complete binary program package encoded as Base64. The package
contains generated HLSL, native pass/reflection records and program bytes; it is a
shader dependency, not a cache-relation ledger. Do not save a second redundant
compiled bundle. Add this asset explicitly to the creation dependency roots because
a native Surface stores only its template integer, not a program CID. Preview uses
its private database and releases its file with the existing asset transaction.
Successful startup registration must precede Surface realization and dependency
inspection. Already-realized fallback materials require an evidenced native
reinitialization sequence; changing the shared fallback material is prohibited.

## Recorded preparation and preview limitations

Producer commands, fixtures, counts and conversion recipes below describe retained implementation
stages and tools. They are not a current complete-library inventory or permission to remake
non-road materials. A prepared structural conversion or generic road converter can remain a
recorded implementation gap under the current scheme. See the [code-path assessment](movable-nonroad-material-reuse-assessment.md)
and [shared integration handoff](../implementation/shared-material-integration-20261028.md) before using those tools.
No new runtime integration or acceptance is established by this documentation.

Sources with different color or scalar overrides still require conversion coverage; until that
migration is complete they retain their own descriptor and reuse the packaged zero-control
Texture. They must not silently inherit the prepared cable's color. This remaining descriptor
creation is not compliance with the constant-material preparation rule. Compatible native object
materials continue to return directly without loading or creating a prepared replacement.

Original Surface bytes and appearance properties remain unchanged. Private preview
materials additionally require draw-instance geometry modes: packed normals, actual
procedural bone inputs, native lane curve mode, VT binding and offscreen depth state.
These are not a replacement shader or permission to change source layer composition.
Do not describe all effective preview keywords as byte-identical to the source.
Keep a native-lane draw distinct from a procedural object carrying a preview pose.
Only a real native lane or an enabled CurveProperties tiling mode selects the curve
keywords; a plain object must not gain the lane-default mode merely because it has
bones. Procedural mode requires actual nonempty bone data. Preserve unrelated source
appearance keywords. Full network vertex/buffer compatibility and omitted template32
projected color remain unverified/incomplete; do not create a substitute surface from
its footprint alone.

## Recorded package layout

The following spelling is retained from the former implementation document; current package obligations use the material contract's `~Materials` discovery boundary.

The required package destination is `Materials/<BridgeStyleId>/` beside `BridgeBuilder.dll`.
Copy complete native payloads, mip/VT resources and CID sidecars unchanged, preserving relative
paths. Documentation and `.gitkeep` placeholders are not runtime payloads and must be excluded.
Build, installation and publication must retain only populated payload directories. Omit folders
left empty after filtering, including the packaged `Materials/` root if there are no payloads.


`BridgePreparedMaterials.Structure` also resolves these aliases for structural rectangles with
the same measured unit mapping. A triangular source domain with an unrelated normalized mapping
must not be registered as a rectangular alias. Nonperiodic structural coverage remains separate.

For a genuine rectangular unit containing several original structural domains, implement
`static partial void <StyleId>Regions(string sourceDigest, List<Region> regions)` in that style.
Match the original Surface byte digest and add `new Region(bindingKey,minU,minV,maxU,maxV)`
using its measured hardcoded coordinates. Select the structural descriptor semantics, not a
road descriptor with unrelated cutout settings. `MovableStructureTiles` first retains compatible
original materials, then uses these regions only when a complete original domain lies inside
the evidenced coverage. Where the original domain contains several periods, use the overload
`Region(bindingKey,minU,minV,maxU,maxV,coverageMinU,coverageMinV,coverageMaxU,coverageMaxV)`.
The first rectangle is the genuine sampling period; the second is the independently measured
original same-material domain. It does not expand the physical period or admit neighboring atlas
parts merely because a generated triangle reaches them. It converts source UVs into continuous true-unit
coordinates and verifies the Float32 gradient. Constructed faces retain their unconditioned
double coordinates; an integer phase in an old triangular domain is not treated as a period
of a different rectangular unit. This adds no triangles and creates no textures at runtime.


## Retained preparation implementation observations

This avoids the black partial-block edges observed with Unity's whole-chain compressor.


Textureless curved overhead-wire materials require a native object-template conversion after
curve geometry has been evaluated. Prepare the native cable appearance as a Surface and a linear
zero-control Texture in each style's `Cable/` directory. The zero control prevents the object
template's city-palette tinting without changing the original wire color.

`tools/PrepareCable` reads the native cable Surface from the installed content archive and the
offline-extracted `CurvedTransparent` template in `tools/PrepareCable/NativeTemplate.json`. It retains the source color override and resolves
metallic, smoothness and snow from source overrides or the measured template defaults. Recipe
and source digests determine the finished identities. Runtime compares all of those resolved
properties exactly before reusing the prepared Surface, independently of the incoming source CID.
An equal-looking name is not a match. Prepared assets are never added to an owner's destruction list.
