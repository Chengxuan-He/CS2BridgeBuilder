# Material Generation Contract

## Scope and authority

This contract governs materials for fixed and movable bridges, every part and LOD, and
preview, creation, regeneration and export. Geometry and native behavior remain governed by
[bridge generation](BRIDGE_GENERATION_CONTRACT.md); movable appearance roles are defined in
[movable generation sections 17 and 24](MOVABLE_BRIDGES_CONTRACT.md#17-material-families-uv-density-and-texture-ownership).
Persistence follows the [asset persistence policy](../Bridge-Asset-Persistence.md).

For movable bridges, the generation design is: **reuse original non-road materials by default, with only the narrow offline repeat-material
derivation in section 3 to avoid new period-wrap seams; compose
or precompose only prototype-supported road appearance types; reuse original materials for
all other road types; preserve each source's original UV projection and
mapping mode**. This is a generation requirement, not a finding about how the original game
converts roads. [Native repetition research](../Native-Movable-Material-Repetition.md) supplies
source evidence only. Current code and prepared resources are not proof of compliance.

The sole road-composition entry condition is: the corresponding prototype supports the actual
road appearance type, so that type must use prototype appearance. Otherwise retain the original
input material without composition. Resolve this per type and domain, never for the whole road.
Examples of independently supported types include asphalt, sidewalks and wheel-track/rut effects.
One prototype may support only some of these; no universal support list is implied. Establish
support from that prototype's measured data. A supported layer does not authorize replacing or
composing unrelated unsupported layers. Preserve required masks and original domain boundaries.

## 1. Full reuse of original materials must create no new materials

### Non-road materials and road composition are separate

Retain original prototype materials for carried structure, towers, trusses, underside,
railings, mechanisms and other non-road components, at their corresponding LODs. Retain
original shader/template, parameters, keywords, texture bindings, channels and dependencies.
Do not recolor, clone, rebake, extract replacement structure textures or assign a new Surface
merely because geometry dimensions or the generated owner changed. The sole structural
texture-derivation permission is section 3's evidenced offline repeat material needed to avoid
new period-wrap seams. Fix unrelated mapping and native render-context problems separately;
a failed UV eligibility test alone does not authorize a replacement. Report unresolved
representation or failed validation explicitly, without a substitute or tolerance relaxation.

Classify road appearance by actual source type and physical domain. Only a prototype-supported
type requiring prototype appearance enters composition/precomposition. All other road types use
their original input materials. Asphalt, sidewalk surfacing and wheel-track/rut effects are
independent examples, not names for one blanket road category. Fixed structural platforms remain
structure; recorded running-surface caps retain their physical-domain rules. Selected rails,
sleepers, lane graphics, wires and attachments retain their separately specified original sources.

A shared Surface or atlas may serve both road and structure. Keep it unchanged for structural
faces except those independently eligible for section 3's narrow offline repeat derivation,
and bind a supported type's road composite only to its road faces. Unsupported road
types keep their original input materials. Never edit the shared atlas,
change its global parameters or clear embedded markings on behalf of the road. Use measured
part/material-domain membership, original bones and all LODs. Material separation grants no
additional geometric cuts or topology changes. Keep markings separate under movable section 18.

### Identity and independent ownership

Whenever a required material can be reused in full, reference it directly. Do not create a
clone, replacement identity, baked equivalent or duplicate texture. A supported-type composition plan
may resolve an unchanged region to an original material without baking. Unsupported types bypass
composition and retain their original input material. Sharing textures while changing a descriptor is a
conversion, not full reuse.

Keep built-in resources as original native references. Snapshot external resources, including
Bridge Builder resources, byte-for-byte with their original CIDs under each current bridge owner.
Copy all required non-built-in texture, VT and mip dependencies. Reuse complete current-owner
copies; never borrow another generated bridge's files or read deleted resources. Copies for
ownership are not newly authored materials and must not modify the source.

During self-check, supplement missing external dependency copies even when live references
resolve through an installed mod. Use the creation dependency traversal, preserve geometry,
UUIDs and CIDs, and do not rewrite complete copies. Report supplementation as migration requiring
restart. Missing sources/conflicting bytes follow the persistence migration policy. Copying
assets does not supply a missing third-party shader or runtime class.

## 2. Preserve source resolution and pattern scale

Follow the general bridge contract's [original UV projection and mapping rule](BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode).
That rule applies to fixed/non-movable and movable bridges, every part, material domain, LOD,
layer and axis. This section specifies material quality and size requirements in addition to
that common rule; it does not define a separate movable-only mapping policy.

Preserve native texture resolution, color space, normal encoding, channel correspondence, filtering,
anisotropy and complete mip/VT dependencies. Check density on tiling axes. On evidenced stretching
axes, report the expected density change from retaining the original UVs rather than calling it
a density-preserving result. A smaller page, different LOD or size limit never permits additional
resolution loss. Reuse original far-LOD resources; do not fabricate low-resolution replacements.

The complete persisted material graph of one generated bridge must not exceed **10,000,000 bytes**.
The working budget is 5–10 MB; smaller output must not be padded. Section 5 defines the separate
**55,000,000-byte** package limit. Meet these limits by reuse and sharing, not omitted dependencies,
downsampling or unauthorized pattern changes.

## 3. Tile repeatable materials at their original density

### Reuse source mapping before producing pixels

Keep the original atlas and native material where they express the required mapping. Original
mesh panels may reuse an internal region through duplicated seam vertices and finite UV traversals.
This does not require a new texture. Do not infer a global period from repeated UV triangles,
rounded dimensions, a chart rectangle or source wrap flags. Do not replace a large triangle's
UVs with vertex-only modulo and claim it repeats internally.

For road composition that requires a separate repeat unit, establish the real source pattern,
region, physical period, orientation and boundary behavior for all channels and mips. A receiver,
triangle or sampling variant is not a new repeat-unit identity. Native repetition of a region is
not by itself proof that an extracted rectangle filters seamlessly. Partial cells, mirrored
traversals and atlas gutters require their own source evidence.

Only where necessary for an eligible road path or the narrow structural rule below, prepare each
known genuine unit once offline. Reuse the finished unit for every applicable road, part and LOD. Preserve exact measured source bounds and
period. Fractional source-texel extents may use an upward-rounded sampling grid and required native
block alignment for one unit, without padding the physical repeat interval. The bounded
two-period exception below is the only exception to the ban on multiple-period storage.
Preserve source resolution and authored mip inputs; report reconstruction and lossy encoding errors
honestly. Unknown selected inputs follow section 4. Structural derivation is limited to the rule below.

### Integer-grid mip resampling and bounded two-period storage

For fractional lower-mip extents (for example 827 / 2 = 413.5), offline preparation
may resample original mip signals onto the legal integer grid of a native texture.
Preserve physical period, phase, source bounds, channel correspondence and independent
axes. Record per-level density ratios and reconstruction errors; physical-scale
preservation does not imply identical rounded samples. This is a narrow exception
to exact mip-grid preservation under section 2, not arbitrary downsampling.
Dimensions must follow the complete native chain, not independent per-level choices.
The current Texture2D path uses integer halving: 827, 413, 206, 103, 51, 25, 12, 6, 3, 1.

Alternatively, where necessary for this fractional-grid problem, exactly two copies
of one evidenced unit may be stored along the affected repeat axis. Keep the whole
chain consistent: base 1654 followed by 827 stores two physical periods at both
levels. Do not place an isolated 827-wide second level below a one-period 827 base.
Compensate UV and phase so one physical period advances half a texture interval;
verify derivatives, trilinear transitions and all PBR channels. Deeper levels can
still have fractional per-period extents: use necessary integer-grid resampling,
not recursive doubling. Storage must not grow with receiver length or sampler count.

This authorization permits necessary rounding/resampling and bounded two-unit storage
only. It does not permit deliberate boundary blending, unrelated mip-signal replacement,
arbitrary resolution reduction, changed source bounds, omitted channels, full-span baking,
new period cuts or increased budgets. Original assets remain unchanged; changed derivatives
receive independent CIDs. All normal-frame, alpha, filter/aniso, every-mip, native-load,
10 MB owner and 55 MB package checks remain mandatory. No global error threshold or
automatic quality acceptance is implied.

### Offline source-atlas repeat materials

Do not cut movable road or structural triangles, or add seam vertices, solely to reset a repeated
atlas coordinate. Where such new seams would otherwise be necessary, bake an independent tiling
material from the original atlas outside the game. This includes evidenced Lift05 repeating
structural profile domains. It is a narrow permission for verified repeating domains, not permission
to remake arbitrary structure or to bake every domain. Keep fully reusable original materials.

Retain existing native seams and necessary cuts at actual shape, motion and distinct material-domain
boundaries. Preserve selected-road shape, lane layout, original attributes and bone ownership.
Road eligibility still requires the prototype-supported actual type/domain gate; this rule does not
admit unsupported input types or omit their worldspace layers. For non-road components, record why
this particular evidenced repeat domain needs an offline derivative to avoid new period-wrap seams.

Establish original source identity/version, all applicable LODs, complete repeat bounds and physical
period from prototype evidence. Preserve density, orientation and phase across neighboring uses,
independent sampling axes/layers, finite axes, mirrors and terminal regions. A crop or UV domain is
not automatically a repeat unit. Do not force a common layer period or turn finite content into repeat.
Bake each necessary content unit once, shared across uses rather than per triangle or sampler.

Validate all PBR channels and masks, alpha/cutout, normal encoding and tangent-frame semantics,
source resolution, filtering/aniso behavior, seams and every mip against the original sampling.
Record reconstruction/compression and per-layer error; existing tolerance requirements remain in
force. Mip-zero, interior-only or file-format checks do not establish equivalence. Failed filtering,
phase, native representation or budget checks remain unresolved; they do not authorize new cuts,
arbitrary crops, lower resolution, full-span baking or a global error-threshold increase.

Leave original atlas, Surface and dependency files unchanged. Derived Surface/texture/VT resources
receive independent identities, never the original source CID for changed bytes. Unchanged shared
dependencies and byte-identical owner copies retain their own CIDs. Package the complete finished
native resource graph once; runtime only copies/binds it, without baking or a custom shader/controller.
Verify independent native loading and all affected LODs, and count the complete dependency graph
against the 10,000,000-byte owner and 55,000,000-byte package limits. This rule specifies the required
route; it does not assert current feasibility, implementation or acceptance.

### No full-road pattern expansion

Never rasterize a pattern along the complete road/span into generated texture pixels. This covers
repeating, finite, non-repeating and mixed appearances, all channels, masks, worldspace layers,
markings and every LOD. Splitting the same expansion across charts, strips, pages or VT tiles is
also prohibited. Failure to find a period, Clamp addressing or an unsupported conversion is not a
fallback permission. The GPU maximum texture size is not a material budget; 4096 is not a universal
permitted size either. Smaller full-span images still violate the rule.

Express length through geometry and supported source mapping, with immutable texture content.
Evidenced stretching retains original UVs without producing a new length-dependent image. A
necessary local non-repeating result must be bounded by the actual local content, not the receiver
length. Do not hide failure through downsampling, omitted layers, arbitrary repeated crops or
excessive subdivision. Existing geometry and topology constraints remain in force.

### Independent material-layer sampling

Road composition/precomposition must preserve each distinct layer's coordinates, scale, phase,
period, wrap and mip behavior. Do not force prototype road samples, selected-road inputs,
worldspace detail and blend masks to share one common period. Constant sampling is a valid
separate case. A finite mask does not force repeating layers into a finite full-span bake.

Within a layer, keep color/normal/mask/control correspondence and source channel-specific
transforms. These channels are not automatically independent layers. Preserve original blend
weights, normal frames and material parameters; blending separately lit meshes is not proof of
equivalent material blending. Keep marking geometry/layout separate from road-base pixels.

Apply the mapping and blend through a verified native, persistable rendering path. Metadata alone,
a shader keyword, a diagnostic shader or network-template support does not prove skinned-object
capability. Custom shaders and manager-dependent runtime rebinding remain prohibited. An unresolved
native layer representation is incomplete work, not permission to flatten the road into pixels.

### Period-detection error tolerance

Replace the universal 0.01-source-pixel cumulative phase cutoff with measured per-layer tolerances.
No replacement numeric threshold is established yet. Derive it from source-coordinate precision,
local boundary error and cumulative drift over supported extents, covering relevant dimensions,
channels, mip/filter behavior and mask transitions. Do not increase it merely until a case passes
or use tolerance to invent repetition, change scale or reinterpret source mapping.

Diagnostics distinguish constant sampling, nonperiodic addressing, no bounded candidate and
phase rejection. Record source/layer/channel, frequency, wrap, candidate period, threshold and
measured errors. A rejected layer must not trigger full-road baking or invalidate another layer's
valid mapping. Preserve independently measured source quantization rather than forcing one ideal lattice.

## 4. Persist constant reusable materials before runtime

### Meaning of precomposition

**Precomposition means material baking outside the game.** Bake the eligible road composite
or section 3's narrowly permitted structural repeat derivative
with offline authoring/baking tools before distribution, save the finished native material,
texture and complete mip/VT dependencies, and include them in the mod package. The game loads,
copies when ownership requires it, and binds those finished resources without rebaking,
resampling or recompressing their texture payloads.

Baking at game startup, asset loading, first use, preview, bridge creation or export is runtime
work, not precomposition. An in-memory or disk cache of an in-game bake is not a precomposed
material. The separate bounded runtime-composition exceptions below do not change this meaning.
Road precomposition remains subject to the prototype-supported-type gate; structural repeat
derivation follows section 3's separate narrow eligibility. Both preserve original mapping and
resolution, no full-road pattern expansion, independent persistence and both byte limits.

### Road composition and precomposition

First dispatch each appearance type/domain by the prototype's measured support. If the type is
not supported, bind its original input material and bypass composition/precomposition. Keep its
source UV projection and dependencies. Incompatibility with native rendering is an explicit
representation problem, not permission to compose or substitute another appearance. The existing
measured descriptor-only track conversion is a native representation rule, not this composition
entry condition; it preserves source pixels and appearance and must not become a general fallback.

Only supported types that must use prototype appearance enter the following plan:

1. Resolve the measured prototype appearance, source masks, required input layers and physical
   domains. Keep unsupported neighboring types on their original materials.
2. Reuse an identical resolved native result where available, or use descriptor-only handling
   where sufficient without baking pixels.
3. Bind a once-prepared composite or genuine repeat unit for known constant inputs.
4. Compose genuinely variable local content only where the supported-type combination needs it,
   subject to section 3's prohibition on full-road rasterization.
5. If an actual selected third-party input required by that supported-type composition was
   unavailable before runtime, perform only the necessary bounded conversion. Unknown input
   alone does not admit an unsupported type into composition.

Known constant road resources and descriptor conversions must be prepared offline and packaged.
Runtime copies and binds them; no first-use bake, resampling or recompression. A missing prepared
key, incomplete library or deleted file is not an unknown-input exception. Do not convert an entire
known prototype at runtime. These exceptions do not authorize remaking non-road prototype materials.

Both runtime exceptions preserve section 2's mapping-specific scale, original channels/resolution,
normal and blend semantics, all mips and native persistence. Record the actual source and why
reuse/descriptor conversion is insufficient. Share equivalent results within the current owner,
never through a cross-bridge generated-result cache. Preview and saved creation apply the same rules.

### Texture content and sampling parameters must be separate

Identify texture payloads by authoritative source content, format, channel semantics, color space,
normal encoding and mip data. Store per-use source region, UV transform, orientation, phase and
sampling separately. Different receivers, rotations, reflections, roads or LODs do not create new
texture identities. Keep native global sampler settings on the resource; metadata cannot grant
unsupported per-use sampler overrides.

Use one shared payload per identical source texture or necessary eligible prepared unit within each
style and each owner. Different source units may need distinct payloads, but each is converted once.
Do not resample per triangle/sampler, rebake to discover equal bytes, alias unequal data to one CID,
or pack duplicate baked images into a container and call that sharing. Built-in references require
no copied payload. Per-style and per-owner copies retain identical bytes/CIDs and are not rebakes.

### Native resource implementation

Resolve the complete Surface/Texture/VTTexture/VTSurface/MidMips graph by verified identities.
Preserve ordered stacks, channel bindings, implicit Surface-to-MidMips membership and matching
preprocessed mip counts. Provenance texture IDs are not necessarily loadable payload references.
A mip-zero decoder cannot establish full-chain preservation. Do not force mip counts to zero to
hide inconsistent resources. Complete dependencies before registration/publication.

Retain native network context for non-road NetPieces; do not convert them merely because an object
mesh writer is shared. Validate any required road conversion in its actual procedural-bone and
vertex-layout path. Native VT allocation transforms describe the registered texture, not arbitrary
atlas-island views. Full-texture wrap, region-local wrap and triangle reflection are not interchangeable.
Do not modify global compression settings or use runtime patches to repair persisted sampling.

Prepared assets remain immutable and independent of generated owners. Resolve installed sources
relative to the loaded mod, never a developer directory or another generated bridge. Deleting a
bridge must not delete prepared sources. Follow section 5 for packaging and source independence.

## 5. Per-bridge material directory and mod packaging

Measure the generated-owner material limit over the complete saved bridge's material dependency
closure, including descriptors, non-built-in texture/VT payloads, required mip dependencies and
their persistence metadata. Count each shared file once within that owner. Built-in dependencies
that remain original references require no copied payload bytes; list these references separately.
The prepared files for a style are a separate measurement, not proof that every generated bridge
fits its 10,000,000-byte material limit. Include selected-road dependencies and permitted runtime
outputs in the owner measurement. If a complete bridge graph has not been generated, report that
measurement as unavailable rather than substituting the prepared-directory size.

The complete compressed Paradox Mods deliverable must not exceed **55,000,000 bytes (55 MB)**.
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
and has no prepared external payloads must have no material directory. If neither a style nor
the shared MidMips source has required payloads,
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
Except for the shared immutable MidMips source cache below, even when two styles use identical
resources, each style's prepared directory must contain its required non-built-in resources
rather than depending on another style's folder.
Variants within one style may reference a shared texture stored as
`Shared/<payloadCID>.Texture` with its CID sidecar, retaining the original CID for unchanged
resources or the prepared identity for a necessary conversion. Sampler variants of the same
texture or genuine unit must reference that single file under section 4, not retain independently
baked texture copies.
Store one byte-identical shared payload per CID within that style. The same one-payload rule
applies within each generated bridge's dependency snapshot; different bridge owners still
retain independent byte-identical snapshots. Validate the complete variant dependency graph before native
registration; shared lookup must stay within the selected style, except for the explicit
shared MidMips source below, and must not fall through to another style or generated bridge.
Sharing storage must not resample or rebake the texture.

### Shared immutable MidMips source cache

The sole cross-style prepared-resource exception is
`assets/Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` and its `.cid` sidecar,
shipped as `Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` beside the mod DLL.
This reserved root contains only offline-prepared native MidMips dependencies, not shared
Surface, Material or Texture payloads. Preserve native cache bytes, CID, tag-bearing filename,
texture stacks and primary/duplicate Surface CID membership. The style's complete dependency
graph may reference this common source; other per-style resource rules remain unchanged.

Treat the installed common cache as read-only. Before native registration, verify each
Surface's required cache membership and matching preprocessed mip count. Include these implicit
Surface-to-MidMips edges in dependency traversal and package validation. Missing membership or
inconsistent cache/texture levels must fail explicitly; do not force a zero mip count or rebuild
known prepared resources at runtime.

Each generated bridge copies only the cache dependencies required by its own graph into its
independent owner-local snapshot, retaining original bytes and CIDs under the persistence
contract's native MidMips filename exception. Do not copy the whole source catalogue, share
generated-owner files, or depend on the installed cache after persistence. Count those copies
in the unchanged 10,000,000-byte generated-owner material limit; count each common source file
once in the complete 55,000,000-byte official SDK ZIP limit. This source-storage exception
grants no additional runtime baking permission or cross-bridge generated-result caching.

### Packaging and source resolution

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

Compare the actual original and generated material assignments by part, road/structure role,
LOD and axis. Do not certify the entire output from one preview, a material name, a reused
texture count or an eligibility flag. Required evidence includes:

1. **Non-road reuse:** original Surface identities, templates, parameters, textures and complete
   dependencies retained, except for separately justified section 3 offline repeat derivatives.
   For each derivative record its evidenced domain, avoided period cuts, new identity and complete
   sampling/quality/dependency validation. No unrelated non-road material remaking is allowed. Record
   byte-identical external ownership copies separately from conversions. Confirm changing road
   composition leaves structural bindings and source assets unchanged.
2. **Road dispatch and composition:** prove support per prototype/type/domain; unsupported types
   retain original materials and never enter composition. For supported types, record source layers,
   prototype/selected-input roles, masks, source frames,
   separate markings and whether the result uses direct binding, descriptor conversion,
   precomposition or bounded runtime composition. Do not label complete selected-road appearance
   preservation as a universal requirement. Verify the specified composite rather than a substitute.
3. **Mapping:** source tiling remains tiling, source stretching remains stretching, mixed axes,
   mirrors, folds, terminal domains and each LOD follow their own evidence. Verify final uploaded
   UVs and source-to-output sampling, not only intermediate formulas. Test both directional density
   on tiling axes; report expected density changes on stretching axes separately.
4. **Layers and filtering:** test independent periods, finite/constant masks, differing resolutions,
   blend transitions, normals, alpha/cutout, source edges and all mip levels. Record unresolved
   per-layer tolerance validation; mip-zero/interior-only checks are insufficient.
5. **No span rasterization:** compare different bridge lengths and verify that base-pattern pixel
   storage does not expand with the receiver, even across multiple resources. Rejected period
   tests must not enter a full-road bake. Record every necessary local raster's content extent,
   dimensions and reason; hardware limits or small pages are not acceptance evidence.
6. **Reuse and lifetime:** known resources are prepared once offline; identical sampling uses share
   payloads. All preview/create/regenerate/export paths use the same representations. Verify cold
   loading without Bridge Builder, complete current-owner dependency closure, and independence
   from other generated bridges or deleted sources. Preserve healthy existing assets.
7. **Budgets:** measure prepared bytes separately, the complete persisted owner material graph at
   or below 10,000,000 bytes, and the actual official SDK package at or below 55,000,000 bytes.
   List built-in references separately. Missing owner measurements are not a pass. Keep the
   general contract's whole-Prefab measurement separate and reject empty material directories.

Record generation, decoding, composition, readback, VT/mip writing and persistence timing without
double counting. Native file checks and code review do not establish game appearance, animation,
traffic or performance acceptance. Human near/far, motion and cold-load checks remain explicit.
Changing this specification does not change installed code or certify current generators.
