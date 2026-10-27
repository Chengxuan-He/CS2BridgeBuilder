# Material Generation Contract

## Scope and authority

These requirements apply to all bridge material generation: fixed and movable bridges, every
part and LOD, preview, creation, regeneration and export. They supplement the
[bridge generation contract](BRIDGE_GENERATION_CONTRACT.md) and the
[movable bridge contract](MOVABLE_BRIDGES_CONTRACT.md). Material reuse, density, tiling and
offline preparation and the bounded runtime conversion exceptions in section 4 are governed here;
geometry, native rendering, ownership and persistence
remain governed by their corresponding contracts.

Apply sections 2 and 5 together: the generated-owner material limit and the complete mod-package
limit measure different graphs and neither replaces the other. Apply section 4's bounded runtime
exceptions together with the offline rule; they do not relax the prohibition on rebaking known,
preparable constants. Routing summaries and movable-specific material rules must preserve both
distinctions. Historical implementation or handoff records do not authorize superseded material
pipelines or establish compliance of the current output.

## 1. Full reuse of original materials must create no new materials

When an original material can be reused in full, reference that existing material directly.
Do not create any new material: no cloned SurfaceAsset, replacement descriptor, new material
identity, baked equivalent or duplicate texture payload. This rule takes precedence over the
offline preparation and per-style storage rules below; neither requires creating a replacement
for an unchanged original material.

Full reuse preserves the original shader/template, parameters, keywords, texture bindings and
sampling behavior. Sharing only textures while changing the material is not full material reuse.
Changes to bridge geometry or ownership alone do not justify creating an otherwise identical
material. Apply this rule to every LOD, preview, creation and export path.

Keep built-in materials as native references. Materials supplied by Bridge Builder or any other
mod must have byte-identical copies with their original CIDs under the current bridge owner,
including all required non-built-in texture and VT dependencies. A reference to the source mod
alone is insufficient. Such copies preserve the existing material identity; they must not create
a new material or modify the source. Reuse existing current-owner copies, never resources owned
by another generated bridge, even of the same style. Never read deleted bridge files.

During bridge self-check, supplement missing external material copies even when the source mod
is installed and the bridge's live references are valid. Traverse the serialized dependency graph
using the same persistence path as creation. Preserve bridge geometry, UUIDs, CIDs and live
references; repeat checks must not rewrite complete copies. Report supplementation as migration
and require restart. Missing sources and conflicting bytes follow the existing migration failure
policy. Copying files does not supply a third-party runtime class or shader implementation.

## 2. Preserve source resolution and pattern scale

Keep the generated bridge's material footprint as small as possible without compromising
material resolution. The complete material dependency graph persisted for one generated bridge
must not exceed **10,000,000 bytes**. The working budget is 5–10 MB; an output below 5 MB must
not be padded. The distributed mod has the separate total-package limit in section 5.
Reduce storage through original-resource reuse, shared identical resources and
source-density tiling and verified non-tiling reuse below; never reduce texture resolution.
A size target alone never authorizes pattern distortion.

Material generation must not reduce texture resolution. Preserve physical texel density and
pattern dimensions for genuinely tiling prototype mappings and selected-road materials
required by the surface-selection rules. The sole stretching exception is a mapping verified
as non-tiling in the applicable bridge prototype: retain its authored source UVs as geometry
changes, interpolating those UVs for permitted construction fragments. Its pattern may stretch
with that geometry; unchanged physical density is not an acceptance gate for that evidenced use.

Establish non-tiling status offline from the actual prototype mapping, not merely the texture's
wrap setting or apparent content. Record source identity/version, Surface and texture identities,
part/mesh/material slot, every affected LOD, authored UV domain, native sampling semantics and
the non-tiling axes. A mixed mapping retains original physical density on its tiling axes;
apply the exception only to its evidenced non-tiling axes. A failed mapping, missing repeat-unit
evidence, unsupported sampler or arbitrary nonperiodic chart does not prove non-tiling status.
Unknown cases remain unresolved and must not silently enter this exception. Selected-road/track
appearance and marking layout do not inherit this prototype-only permission.

Preserve original native textures, resolution, channels, color/normal semantics, complete mip
and VT dependencies and compatible material identity. This permission creates no new bottom
materials, arbitrary crops, per-sampler bakes, geometry transforms, topology or bone changes.
It changes only whether the verified non-tiling mapping must compensate for already-authorized
geometry deformation. Full reuse, independent persistence and both byte limits remain in force.

Outside that verified non-tiling exception, changing bridge dimensions, atlas layout, page count
or LOD must not lower this density or distort the pattern. Smaller texture pages are permitted only when they preserve the same
sampling density and appearance. Preserve the appropriate source detail for each LOD and all
required texture channels, color spaces, normal encoding and mip behavior. Native texture/file
encoding does not authorize resolution loss or any further pattern distortion.

## 3. Tile repeatable materials at their original density

Genuinely tiling prototype mappings and repeatable selected-road materials must use a repeat
unit tiled at original source density; section 2 governs verified non-tiling prototype uses. Do not bake
repeated copies into a very large image covering the bridge's full length or width. Texture
storage for a repeat unit must not grow merely because the generated bridge becomes larger.

When necessary offline extraction encounters a fractional source-texel span, keep the exact
measured source region and physical period. A single unit may be reconstructed on the next
whole-texel sampling grid, rounded upward independently in each direction so source resolution
is not reduced. When a supported native block format requires aligned dimensions, round that
single-period sampling grid upward to its block boundary; do not append padding to the sampled
repeat interval. Native lossy encoding must be identified and checked for channel, alpha and
normal errors, rather than described as lossless. This sampling conversion must not round the region itself, change the physical
pattern size, or expand into many repeated copies merely to make the source and output grids
commensurate. Preserve the authored mip inputs and channel interpretation; report reconstruction
and seam differences rather than claiming pixel-identical filtering.

Use an existing independent repeatable source texture where possible. Each genuine repeat
unit from a known bridge prototype or another preparable constant source that requires conversion
must be baked only once offline, and all sampling uses of that
unit must share the finished resource. Different genuine units, including units in the same
source atlas, may each receive their own necessary offline conversion. Do not extract or bake
separate pixel copies per triangle, receiver or sampling variation. An otherwise qualifying
unit from an unavailable-before-runtime selected third-party road is subject only to the
bounded unknown-input exception in section 4; its sampling uses must still share one result
within the current bridge owner.

A genuine repeat unit is an independently evidenced repeating pattern in the applicable real
prototype or selected-road source. Record its authoritative source identity/version, measured
region, physical period and scale, channel semantics, and tileable-boundary evidence covering
filtering and the complete mip chain. Its identity must be independent of receivers, triangles,
roads, LODs and per-use UVs. An arbitrary crop, a UV chart or a different sampling rectangle is
not by itself evidence of a distinct unit. Do not rename triangles, sampling variants or arbitrary
crops as new units to evade consolidation.

Retain the original atlas payload when its unchanged texels and supported native sampling can
serve the required uses correctly. Where a genuine atlas repeat unit needs extraction for native
repetition, prepare that unit once under section 4; retain its source-region provenance separately
from per-use sampling. Use a verified native representation that does not sample neighboring
parts. Preserve original resolution, pattern scale, orientation, phase, edges and mip behavior.

Non-repeating markings or local overlays must not force a repeatable base material into a
full-span bake. Preserve the repeating base and handle genuinely variable content through
compatible native material composition. Any necessary pages for non-repeating content must
remain bounded while preserving source density. This rule does not prescribe a universal
4096-pixel limit or permit downsampling to meet a page limit.

## 4. Persist constant reusable materials before runtime

### Identical resources must be reused, never baked repeatedly

Repeated baking of the same known, preparable texture or material resource is prohibited.
Reuse an existing compatible original resource directly. If a required conversion produces a
known shared constant resource, prepare it once offline and reuse that finished resource for every use of the same
source texture or genuine repeat unit with that required conversion. Different bridge styles,
roads, parts, triangles, LODs, previews,
creation requests or export destinations do not justify baking identical resources again.

Separate texture payload identity from material and sampling identity. Establish texture
equivalence from authoritative source content, channel semantics, color space, normal encoding
and mip data. Material descriptors retain their own shader parameters and texture bindings;
sampling records retain their own source regions and mappings. A necessary extracted repeat
unit also retains the genuine-unit evidence required by section 3; its derived payload identity
is not a new identity for the original atlas. Names, receiver identities,
output paths and different sampling coordinates do not establish a distinct source texture.
Do not rebake merely to discover that finished bytes are identical; resolve known equivalent
inputs before baking.

### Texture content and sampling parameters must be separate

Store texture content independently of how a particular mesh or material samples it.
The shared texture record identifies native resource CIDs, verified source content, format,
dimensions, channel or VT stack/layer, color space, normal encoding and the complete mip
dependency graph. For a necessarily extracted genuine unit, also record the source-to-unit
mapping and evidence, and share the unit's finished payload across its uses. The per-use
sampling record identifies the original shared texture or prepared unit and retains
the required source region, UV mapping, orientation, phase, wrapping, filtering, anisotropy
and mip/derivative behavior. Keep actual native texture-global settings on their native
resource; recording a different per-use value does not prove the renderer can override it.

Mesh UVs and native Surface parameters/bindings carry the supported sampling differences.
Different triangles, sampling regions or repeat configurations must not become different
texture content identities merely because their sampling records differ. Distinct genuine
repeat units may have separate necessary conversions under section 3; a region difference
alone does not qualify. Preserve exact measured
numeric values and channel semantics. A metadata file alone is not an implementation:
the actual native rendering and saved-asset path must apply each required sampling operation.

### One texture file for the same texture under different sampling methods

The same original texture, or the same offline-prepared genuine repeat unit, used with different
sampling methods must use **one shared texture payload file within each bridge style's resource
graph**. Different UV transforms, rotations, reflections, phases, triangular parameterizations,
repeat settings or sampling regions within that texture/unit must be represented by the applicable
per-use geometry UVs, native material settings or sampling metadata, rather than separate
resampled texture files. Distinct genuine units extracted as required from one atlas may have
separate shared payloads; each unit is prepared once, not once per sampling method. Preserve
sharing of unchanged source texels wherever one compatible payload serves the uses without
conversion. Compatible built-in textures remain
direct references and require no packaged copy. Apply this rule to all parts and LODs,
previews, creation, regeneration and export.

Consolidation must share the actual source texels. Putting many independently baked copies
inside one archive or container does not satisfy this rule. Do not merely alias unequal
existing texture files to one CID. Distinct channels or source contents must not be conflated,
and differing sampling records must not be discarded. Preserve section 2's mapping-specific
density/scale requirements, orientation, phase, region boundaries, normal interpretation and filtered mip behavior.
Implement the sampling through supported native rendering; this requirement does not authorize
custom runtime shaders, excessive geometry subdivision or runtime baking of known, preparable
constant resources. The bounded runtime exceptions below do not relax sampling consolidation.

Resource keys and authoring inventories must identify the shared texture separately from its
sampling records. Convert a required known immutable source texture or evidenced genuine repeat unit
once offline, when necessary, then reference that finished payload from every applicable sampler.
For a permitted unknown-input conversion, all applicable samplers within the current owner
must instead share that operation's single necessary conversion.
All roads, parts, triangles and LODs using that same unit share its preparation. Do not use a per-triangle
or per-sampler bake as a fallback to evade consolidation. An unsupported native sampling case
must be identified explicitly and resolved before claiming implementation completion.

The per-style packaging rule below may require physical copies of one prepared resource.
Create those copies byte-for-byte with the same CID; do not bake them again or assign replacement
identities. Likewise, per-bridge dependency snapshots remain byte-identical ownership copies,
not new baking operations. Preserve complete style-local dependency graphs and independent
bridge lifetimes. This reuse concerns original and immutable offline-prepared source assets;
it does not authorize caching generated bridge results.

Persist reusable constant materials and their required texture/dependency resources as durable
assets. A constant material is invariant within its explicitly identified source and recipe;
different roads or prototypes must not be treated as identical merely because their names or
surface categories match. Preserve source version, sampling, shader parameters and channel
semantics when identifying reusable assets.

If a known, preparable constant reusable material requires baking, bake it offline before runtime and include the
completed resource in the distributed asset package. Bridge creation must only copy the finished
material and its required resources, then bind them; it must not rebake, resample or recompress
their texture payloads. Preview, regeneration and export must use the same prepared resources.
First-use baking during loading, preview or bridge creation does not satisfy this requirement
for known constant resources. Missing or stale prepared resources do not qualify for a runtime
conversion exception.

### Bounded runtime composition and unknown selected-road inputs

Apply material preparation in this order to the actual selected road or track and its resolved
surface layout:

1. Reuse a fully compatible original material directly, without a new material or texture.
   Keep built-in references and persist byte-identical current-owner copies of non-built-in
   dependencies under section 1.
2. If only a native descriptor/template conversion is necessary, convert the descriptor and
   retain compatible original texture and complete VT/mip references. Do not bake pixels.
   Prepare descriptors for known inputs offline; a previously unknown selected input may
   receive its necessary descriptor conversion during the current operation.
3. Prepare known bridge-prototype constant materials and evidenced genuine repeat units
   offline once. All roads, parts, triangles, LODs and sampling uses of the same resource
   share the finished preparation; bridge generation only copies and binds it.
4. When the current selected layout or mixing masks produce a genuinely variable combination,
   necessary bounded runtime composition is permitted. Compose only the variable content
   actually needed by the selected road. Keep repeatable bases separate where native
   composition permits; do not bake repeated copies across the bridge merely to include an
   overlay or mask.
5. When a third-party road or track input is unknown before runtime, cannot be fully reused
   or handled solely through descriptor conversion, and cannot be prepared in advance,
   necessary bounded runtime texture conversion is permitted for that selected input.
   This exception concerns the actual required source and conversion, not every material
   supplied by that mod or every potential road combination.

Both runtime exceptions preserve source resolution, physical texel density, pattern dimensions,
all required channels, color spaces, normal interpretation and complete mip behavior under
sections 2 and 3. Produce supported native assets and persist their complete dependency graph
under the current bridge owner so the bridge remains usable without Bridge Builder. Share
identical conversion results within that owner and preserve current-owner resource reuse;
do not borrow or cache generated results across bridges. An independently created bridge may
perform its own necessary unknown-input conversion when no permitted prepared original resource
exists; do not introduce a cross-bridge generated-result cache to avoid that work. Preview, creation and export must
apply the same source-selection and conversion rules.

These are limited input-dependent conversion paths, not a general fallback for known constant
bridge materials, absent prepared keys, deleted files or an incomplete offline library. Do not
convert an entire known prototype at runtime, bake once per triangle or sampler, downsample,
or substitute a different pattern. Keep required offline preparations packaged and immutable.

Keep prepared source assets independent of generated bridge lifetimes. Follow the
[asset persistence policy](../Bridge-Asset-Persistence.md) for byte-identical dependency copies,
original CIDs and built-in references. Never source a reusable material from another generated
or deleted bridge, and never delete the prepared source when deleting a bridge. Prepared assets
must remain available without the player's original prototype files. This is a packaged source
asset workflow, not a cache of previously generated bridge results.

### Native resource implementation

Resolve the original native Surface and its Texture/VTTexture/VTSurface/MidMips dependency
graph by verified identities. Keep compatible original Surfaces and built-in texture graphs
as direct references, including their original channel bindings and mip chains. Multiple
parts may share the same texture while retaining distinct UVs and genuinely different native
material parameters. Do not infer source identity or equivalence from names or file counts.

If only a native material-template conversion is required, prepare the necessary descriptor
offline for known inputs, or during the current operation for a previously unknown selected-road
input under the bounded rules above, while preserving compatible original texture references; do not decode and rebake
the referenced images. Byte-identical non-built-in dependencies must be persisted per style
and per generated bridge owner under sections 1 and 5. Their snapshots retain the original
CIDs and complete dependency closure. The generated bridge must work after Bridge Builder
is uninstalled, without another generated bridge supplying its files.

For a texture conversion that is actually necessary, resolve a single shared conversion for the
source texture or genuine repeat unit, independent of per-use sampling records. An atlas may
contain multiple evidenced units; convert each only when native use requires it, and reuse each
finished result for all its samplers. Preserve all required original mip data and semantics;
a decoder returning only mip zero cannot prove preservation of the full mip chain. Metadata,
an offline diagnostic shader or a successful texture upload does not prove that the game's
persisted native material supports a sampling operation.

Verify native capability before replacing local-region repetition or triangle reflection
with original-atlas references. Whole-texture Repeat, vertex-only modulo and ordinary affine
UV scale/offset are not interchangeable with region-local repetition, per-fragment triangle
reflection, reflected-normal handling and region-edge filtering. Where the original atlas lacks
a compatible native local-repeat representation, use a genuine unit's necessary once-only offline
extraction if it satisfies sections 3 and 6. A previously unknown selected third-party road may
use the bounded conversion exception above for its required genuine unit. This permission does not establish periodicity for an
arbitrary structural chart. Verified non-tiling prototype uses retain authored UVs under section 2.
Other cases without an evidenced unit or supported native representation remain explicitly
unimplemented until resolved; do not silently substitute a different pattern
or restore per-triangle or per-sampler baked copies.

## 5. Per-bridge material directory and mod packaging

Measure the generated-owner material limit over the complete saved bridge's material dependency
closure, including descriptors, non-built-in texture/VT payloads, required mip dependencies and
their persistence metadata. Count each shared file once within that owner. Built-in dependencies
that remain original references require no copied payload bytes; list these references separately.
The prepared files for a style are a separate measurement, not proof that every generated bridge
fits its 10,000,000-byte material limit. Include selected-road dependencies and permitted runtime
outputs in the owner measurement. If a complete bridge graph has not been generated, report that
measurement as unavailable rather than substituting the prepared-directory size.

The complete compressed Paradox Mods deliverable must not exceed **50,000,000 bytes (50 MB)**.
Measure the actual official SDK content ZIP, including all included bridge styles, DLLs,
materials, dependencies, symbols, UI, licenses and archive overhead. The limit is not a
per-style or texture-only allowance. Splitting the same required payload into additional
downloads does not remove it from the aggregate delivery budget. Source checkout size,
unpacked build size and one generated bridge's asset size remain separate reporting scopes.
An oversized package must not be deployed as the final deliverable or declared ready for
human acceptance. Preserve source resolution, section 2's mapping-specific pattern scale, required LODs and independent
persistence while reducing size; the limit does not authorize missing assets, runtime baking
of known, preparable constant resources or depending on deleted/other-generated-bridge resources.

The authoritative prepared-material directory is `assets/Materials/<BridgeStyleId>/` in the
repository. Each bridge style with actual prepared payloads must have its own directory, using its exact stable, unlocalized
catalogue ID, for example `Draw01`, `Draw03` or `Lift03`. Do not combine different bridge styles
into one flat material directory or use a generated bridge UUID as a source-directory name.
If a style has multiple source variants, keep those in distinct subdirectories within that style.

Do not create or retain empty material directories in the repository, build output, installation
or distributed mod package. Create a style or variant directory only when it contains actual
material/dependency payloads. Do not use `.gitkeep`, documentation or dummy files to preserve an
otherwise empty material directory. A style that only references built-in original materials
and has no prepared external payloads must have no material directory. If no style has payloads,
omit the packaged `Materials/` root as well.

The distributed mod must carry these assets beside `BridgeBuilder.dll` under
`Materials/<BridgeStyleId>/`, preserving relative paths for populated directories only.
The following examples specify paths, not directories that must exist without payloads:

```text
Repository                            Mod package
assets/Materials/Draw01/               Materials/Draw01/
assets/Materials/Draw03/               Materials/Draw03/
assets/Materials/Lift03/               Materials/Lift03/
```

Store each finished material with all required native texture payloads, mip/VT data, CID
sidecars and other non-built-in dependencies. Preserve native file extensions and reference
identities; the prepared-material directory layout must not require recomposing or rebaking
known constant materials at runtime. Results allowed by section 4's bounded runtime exceptions
belong to the current generated bridge's native asset/dependency graph; do not write them into
the installed prepared-material source directory or share them with other generated bridges.
Even when two styles use identical resources, each style's prepared directory must contain its
required non-built-in resources rather than depending on another style's folder.
Variants within one style may reference a shared texture stored as
`Shared/<payloadCID>.Texture` with its CID sidecar, retaining the original CID for unchanged
resources or the prepared identity for a necessary conversion. Sampler variants of the same
texture or genuine unit must reference that single file under section 4, not retain independently
baked texture copies.
Store one byte-identical shared payload per CID within that style. The same one-payload rule
applies within each generated bridge's dependency snapshot; different bridge owners still
retain independent byte-identical snapshots. Validate the complete variant dependency graph before native
registration; shared lookup must stay within the selected style and must not fall through
to another style or generated bridge. Sharing storage must not resample or rebake the texture.

Both the project build and `tools/Build.ps1`, and subsequent installation/publication packaging,
must recursively include the prepared payloads without changing their bytes or flattening the
style directories. Exclude documentation, `.gitkeep` placeholders, temporary files and offline
authoring intermediates. Omit directories left empty after filtering. Verify relative paths,
payload hashes and the absence of empty or placeholder-only directories in the final package. Directory
presence alone does not establish that usable materials have been prepared or packaged.

Resolve the installed source directory relative to the loaded mod's installation location, never
a developer path, current working directory or another generated bridge. Treat it as read-only.
Copy required external resources into the new bridge's independent dependency snapshot under
the existing persistence policy; deleting a bridge must never delete packaged source materials.

Implementation status and packaging guidance are maintained in
[prepared-material documentation](../Prepared-Materials.md). Directory presence does not prove runtime use.

## 6. Verification

Verify source and output texel density, pattern scale, repeat seams and mip behavior across
affected dimensions and LODs under section 2. For verified non-tiling prototype uses, audit the
source/part/LOD/axis evidence, retained or interpolated authored UVs, unchanged texture resolution,
channels, material identity and mip dependencies. Report expected physical-density changes from
geometry stretching separately; do not record these uses as density-preserving passes.
Verify that known, preparable constant resources are prepared before runtime and
copied without baking, and that bridge deletion cannot invalidate another bridge's materials.
For each runtime conversion, distinguish a genuinely variable layout/mask combination from an
unknown-before-runtime third-party selected-road input. Record the actual selected sources,
why direct reuse and descriptor-only conversion are insufficient, the necessary converted
content and its complete current-owner dependency closure. Check source density, channels,
color/normal semantics and every mip; no runtime conversion may conceal a missing known
prepared resource or read another bridge's generated results.
For full original-material reuse, verify that the original material identity is retained and
no new material or texture assets are generated.
For sampling consolidation, enumerate every source texture, each necessarily extracted genuine
repeat unit and their uses. Verify unit provenance and tileable boundaries independently of
receiver geometry, and verify that all sampling variants of each source texture/unit resolve
to one physical texture payload per style and per generated owner,
and test each retained mapping against the applicable original source at region boundaries
and across mip levels. Verify all required channels and independently persisted bridges.
Count actual texture payloads and total SDK package bytes; a smaller file count alone does
not prove content sharing or compliance with the 50 MB total-package limit.

The consolidation acceptance evidence must include:

1. Complete source-to-unit-to-use coverage for every affected style, part and LOD, distinguishing
   original textures, evidenced genuine repeat units, Surface count, physical texture resources,
   VT channel views and per-use sampling records. Record the real-source measurements and
   tileable-boundary evidence for each unit; arbitrary crops and receiver-derived identities fail.
2. One physical payload per identical non-built-in source texture or necessarily prepared genuine
   unit in each style/owner graph, or direct built-in references with no new texture file.
   Distinct genuine units may have separate necessary payloads, but all sampling methods of the
   same unit share one. Different sampling methods create no extra pixel copies. A container of
   per-triangle or per-sampler resampled copies fails this check.
3. Actual native sampling checks against the applicable prototype or selected-road source:
   both directional texel densities and pattern scale under section 2's verified non-tiling
   exception, phase, orientation, region/repeat seams,
   normals and all required mip levels. Interior-only or mip-zero-only tests are incomplete.
4. Preview, saved creation, regeneration and export bind the same verified source/sampling
   representation. Native cold loading and independent dependency persistence are verified;
   deleting another bridge or uninstalling Bridge Builder must not invalidate the result.
5. No runtime baking of known, preparable constant sources; required offline conversions are
   reused rather than repeated. Runtime composition and unknown-input conversion are limited
   to the section 4 exceptions and the actual selected road, with identical outputs shared
   only within the current owner. Verify descriptor-only paths do not bake texture pixels.
   Preserve geometry, bones, original-compatible material identity and
   supported localization. Retire old resources only after their replacement and references
   are verified; do not delete healthy generated bridge assets to make a test pass.
6. Report both the prepared material bytes for each style and the complete generated-owner
   material dependency bytes, with original built-in references listed separately. Verify each
   generated owner's complete persisted material dependency graph remains at or below
   10,000,000 bytes; an unavailable complete-owner material graph is not a pass. This ceiling
   applies to material dependencies, not geometry or the entire generated Prefab; retain the
   separate complete-Prefab measurement and prototype comparison required by the bridge contract.
   Measure the complete official SDK content package at or below 50,000,000 bytes and verify
   that source, build and delivery material directories contain no empty placeholders.
   Report native checks separately from game visual acceptance; game near/far appearance,
   motion and actual gameplay confirmation belong to human acceptance.

Record implementation and measured performance separately; adding this contract is not evidence
that existing generators already comply.
