# Material Generation Contract

### Packaged textures belong to road running surfaces

A packaged `.Texture` must have a measured road-surface consumer. Check the actual
faces, appearance type, LOD and native Surface channel that references it. Asphalt,
walkable sidewalk paving, running grating, running caps and selected-road paint
qualify only on their measured road-facing domains. A structural platform, curb
sidewall, truss, cable or underside uses its original game or DLC texture references.
This requirement also applies when a road and a structural face share the same atlas.
A descriptor conversion needed for native rendering may bind original native channels.
It must never give a non-road consumer a dependency on a packaged texture.

Retain valid native UV projection, sampling and mip identities. A local repeating
atlas island that cannot be sampled at a requested width through its original
representation retains its authored coordinates. Record any resulting change in
physical density. Do not bake a structural repeat or wrap individual vertices into
an atlas chart to fill that gap. This supersedes earlier structural repeat permissions,
including the earlier Lift05 profile recipe. Independently persisted external owner
copies remain governed by the dependency persistence contract.


Follow [player-controlled generation](BRIDGE_GENERATION_CONTRACT.md#player-controlled-generation-without-structural-rejection-gates): do not reject a generation request
because of structural shape, width, signed scale, degeneracy, overlap, clearance or appearance
quality. Such checks belong to offline verification or non-blocking diagnostics. Material
no-match selects Original. Preserve actual file/schema/index, nonrepresentable arithmetic,
write and native API errors; never fabricate successful output. This applies to all bridge
families and LODs without changing historical dependency recovery.


## Scope and authority

This contract governs materials for fixed and movable bridges, every part and LOD, and
preview, creation, regeneration and export. Geometry and native behavior remain governed by
[bridge generation](BRIDGE_GENERATION_CONTRACT.md); movable appearance roles are defined in
[movable generation sections 17 and 24](MOVABLE_BRIDGES_CONTRACT.md#17-material-families-uv-density-and-texture-ownership).
Persistence follows the [asset persistence policy](../Bridge-Asset-Persistence.md).


Road appearance processing (prototype composition, source-density tiling, embedded-marking
removal and derived wear/background placement) applies only to movable bridges whose roads need
that processing. Fixed/non-movable bridges retain the selected native road materials, UVs and
markings; they require no prototype road-support table, road-material replacement or marking
cleanup under these rules. Structural source-UV/density rules still apply to every bridge.
A fixed running cap within a movable bridge remains part of that movable road-processing scope.

For movable bridges, the generation design is: **reuse original native textures for every non-road surface; compose
or precompose prototype-supported road appearance types; use compatible original or
corresponding native object road materials for other types under section 4; preserve source density, tile prototype-supported road appearances without stretching, and
retain evidenced mapping modes for non-road materials**. This is a generation requirement, not a finding about how the original game
converts roads. [Native repetition research](../Native-Movable-Material-Repetition.md) supplies
source evidence only. Current code and prepared resources are not proof of compliance.

Road composition requires both a verified actual receiver appearance type and its corresponding
measured prototype material at the target physical part. That matching part uses prototype appearance. Otherwise use the compatible
input or corresponding native object road material under section 4. Resolve this per type and domain, never for the whole road.
Examples of independently supported types include asphalt, sidewalks and wheel-track/rut effects.
One prototype may support only some of these; no universal support list is implied. Establish
support from that prototype's measured data. A supported layer does not authorize
replacing unrelated appearance categories. Preserve required masks and original domain boundaries,
except the expressly accepted network-layer differences of section 4's compatible road replacement.

### Physical prototype-part correspondence

Within the single selected elevated-road context, resolve the target's actual prototype part
before selecting its material. Both conditions are required: the selected receiver's complete
source appearance is identified, and a measured corresponding prototype material exists at that
physical position/part. A type label or bridge-wide support flag alone satisfies neither condition.
Apply this at every LOD to moving decks, fixed running caps, moving sidewalks, fixed sidewalk
platforms and external connecting approaches separately. Motion/bone assignment and material-part
assignment are distinct: a fixed cap inside a mechanism is not the moving deck's material domain.

Preserve different prototype materials in different parts even when they accept the same selected
input category. Same-category source/density/phase coordination applies to domains that share the
same evidenced prototype appearance; it must not erase intentionally different fixed paving,
concrete, grating or moving-road content. The Draw01 correspondence is moving carriageway asphalt,
moving sidewalk grating, fixed sidewalk paving and fixed running-cap concrete. This is an example
of measured part correspondence, not a universal appearance list for other bridges.

External approaches with no corresponding prototype road part retain their original selected-road
route. They must not inherit a material merely from proximity to the mechanism, its road type,
a shared section or renderer. Native network and skinned-object realizations remain distinct.
A no-match returns Original without throwing; required matched-resource failures remain explicit.

#### Lift03 shared deck appearance

For Lift03, the user's shared-deck rule supersedes the default requirement to retain
different appearances solely because the same road category occupies different decks.
Classify each actual receiver domain, including mixed road/track inputs, independently
of Main/Auxiliary identity. Both decks use Lift03's same prepared clean upper-road
paving and complete same-source wear for matching road receivers; both use Lift03's
same measured lower-road ballast for ballast receivers. Apply this at every LOD,
including the generated far road, even when the original far shell has no road top.
Preserve the shared family's source PBR, physical density, orientation and phase.
Centre wear on each deck's actual vehicle lanes with unchanged physical width; no
vehicle lane means no wear. Embedded prototype markings remain removed and selected
road paint remains separate. This material sharing changes neither deck height/pose,
motion partitions/bones nor structural and unrelated sidewalk/approach appearances.

### Complete material coverage of a replaced component

Evaluate the whole corresponding component, not only its upward-facing running surface.
Sidewalks, medians, fixed platforms and road caps may contain separate top, curb/edge,
vertical sidewall, bevel and end-cap material domains. Identify each actual source domain
and its measured prototype counterpart at that physical part; bind every matched domain
at every LOD. Do not leave an identified matching edge on selected-road brick/paving merely
because the top was replaced, nor treat a completed top-material route as complete component
coverage. Record original and generated face/submesh membership and actual visible bindings.

This is not a blanket instruction to give all edges the top material. Preserve the prototype's
distinct edge, closure and structural appearances. A median edge does not inherit a sidewalk
edge route from a shared atlas region, and an external approach does not inherit the mechanism's
materials. Where a domain has no measured corresponding prototype material, retain its compatible
Original route under section 4. Do not broaden source classification or invent correspondence
to hide a material mismatch.

Material routing must not omit or delete the corresponding geometry. Preserve required bottom,
sidewall, cap and supporting faces under the source-face coverage contract, including all LODs
and their stable renderer ownership. Verify shape/indices separately from PBR/alpha/mip behavior;
do not close an apparent material hole with duplicate geometry. Density, normal frames, bones,
embedded-marking cleanup and separate intended native paint retain their existing rules.

Identify target parts from immutable prototype evidence and construction coordinates, not runtime
names, height guesses or texture resemblance. Keep original shape, bone ownership, physical
boundaries and conditional visibility. Use the same part correspondence in every LOD, including
simplified meshes, without adding period-driven cuts. Matched road appearances preserve original
physical density, complete PBR/alpha/cutout and valid mip/filter behavior; remove embedded markings
while retaining the selected road's separate intended native paint. A successful classification or
bound resource count does not prove the intended visible part actually uses that material.

### Total material routing by part and actual source

Material matching/classification is a **noexcept decision**: when a measured corresponding
material route matches the actual source type, complete receiver domain and target part, select
that route; otherwise return the original source-material route. A missing correspondence,
unknown category, unmatched atlas region, absent optional mapping or mixed receiver not covered
by a whole-domain rule is an ordinary no-match, not a generation exception. Never infer complete
coverage from a root section named Sidewalk, one worldspace layer or a Surface family alone.
Prototype-supported appearances take priority only on their evidenced matching domains.

The common generation entry resolves the **elevated road-level branch once**, before width,
cloning and style processing, under [the all-bridge rule](BRIDGE_GENERATION_CONTRACT.md#select-one-road-level-branch-before-generation).
All geometry, bridgeheads, decks, materials and LODs consume that immutable context. No material
consumer may enumerate/rank alternative levels, fall back to ground or combine another branch's
fields. Missing correspondence selects Original within the elevated context, not a new branch.

Keep all physical edge/node contributions and native local requirements, upgrades and directional
states. They are not road-level alternatives. Head/mechanism flags do not permit reselection;
measured physical height and pose remain independent of road-level choice. Mechanism and native
head material realization still use their distinct renderers.

Keep routing separate from material realization. `Original` records the actual input Surface,
channels, evaluated source coordinates and provenance; it is not a generic asphalt substitute.
Reuse that material directly in a compatible native render context. A moving object may require
a source-based native descriptor conversion or section 4's evidenced corresponding object
material. The accepted network-blend/world-projection differences still apply, but do not change
an unrelated category or substitute a prototype patch merely to avoid a no-match. Preserve
unknown or mixed source decisions as Original; do not send them to a full-span bake or a
"missing material route" exception. If the selected source cannot actually be represented by a
persistable native renderer, report that separate representation failure with its source/context;
a successful routing decision is not proof that unsupported rendering works.

Branch by physical/rendering part before selecting the representation:

| Part | Material routing and realization |
| --- | --- |
| Moving deck and stationary running-surface regions inside its mechanism | Match the actual source/domain per LOD; realize a matching prototype route or the original-source route using the native skinned-object layout and existing bone assignment. |
| Native connecting bridgeheads and approach road pieces | Match their own actual composed occurrences and source faces per LOD. Keep network materials, network vertex encoding and native curve context; a matched replacement requires a compatible network descriptor and measured sampling. Do not bind an object Surface merely because the deck uses one. |
| Structure and unrelated external road | Retain their separate structural/original rules; neither a deck match nor a bridgehead match changes these bindings. |

The same evidenced prototype appearance on the two road branches must retain coherent category, PBR, physical density
and evidenced phase; do not copy one branch's UV encoding or assume their native mapping origins
are identical. Preserve all other source faces and conditional alternatives.

Noexcept applies to match selection, not to all generation. Do not catch every exception, return
success after a corrupt/missing required payload, suppress native IO/registration failures or
hide invalid geometry. Required resource loading, native realization and persistence remain
explicit failure boundaries with complete diagnostics. Absence of a match is not such corruption.
Preview, generation and export use this same routing policy at every LOD.

## 1. Full reuse of original materials must create no new materials

### Non-road materials and road composition are separate

Retain original prototype materials for carried structure, towers, trusses, underside,
railings, mechanisms and other non-road components, at their corresponding LODs. Retain
original shader/template, parameters, keywords, texture bindings, channels and dependencies.
Do not recolor, clone, rebake, extract replacement structure textures or assign a new Surface
merely because geometry dimensions or the generated owner changed. Structural texture derivation is prohibited. Fix unrelated mapping and native render-context problems separately;
a failed UV eligibility test alone does not authorize a replacement. Retain the source material
and attributes when no corresponding representation is available. Record appearance limitations
without blocking generation; actual resource or native API failures remain errors.

Classify road appearance by actual source type and measured target physical part. Only their
verified correspondence enters prototype composition/precomposition. All other road types use
compatible input or corresponding native object road materials under section 4. Asphalt, sidewalk surfacing and wheel-track/rut effects are
independent examples, not names for one blanket road category. Fixed structural platforms remain
structure; recorded running-surface caps retain their physical-domain rules. Selected rails,
sleepers, lane graphics, wires and attachments retain their separately specified original sources.

A shared Surface or atlas may serve both road and structure. Keep it unchanged for structural
faces for every non-road receiver,
and bind a supported type's road composite only to its road faces. Unsupported road
types follow section 4's compatible road replacement rule. Never edit the shared atlas,
change its global parameters or clear embedded markings on behalf of the road. Use measured
part/material-domain membership, original bones and all LODs. Material separation grants no
additional geometric cuts or topology changes. Keep markings separate under movable section 18.

### Identity and independent ownership

Whenever a required material can be reused in full, reference it directly. Do not create a
clone, replacement identity, baked equivalent or duplicate texture. A supported-type composition plan
may resolve an unchanged region to an original material without baking only when its mapping
and all-LOD marking content satisfy sections 2–3. Unsupported types bypass
prototype composition and use compatible originals or section 4's corresponding road replacement. Sharing textures while changing a descriptor is a
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

### Lane-aligned wheel-track appearance

Follow the general [wheel-track placement rule](BRIDGE_GENERATION_CONTRACT.md#wheel-track-placement-follows-actual-selected-lanes)
for movable roads requiring material processing. Test asphalt, sidewalk and wear support independently.
For a supported wear appearance, reuse the same complete measured wear source across lane uses,
with a matching background derived from the same road source. Keep BaseColor, Normal and Mask
source coordinates aligned, including any additional required channels. Preserve original physical
wear width, internal dark/light relationships and longitudinal density. Translate the whole
pattern so its measured visual wear center aligns with each actual selected native vehicle lane
center; do not retain an obsolete prototype lane-reference offset or center individual dark bands.
A dark band is not automatically an independent tire trace. Measure the complete pattern first.

Do not scale wear with lane/road width, infer lane positions from paint or distribute wear across
a stretched complete road cross-section. Do not place wear on shoulders, medians, curbs or other
regions without a corresponding vehicle lane. Those regions use the matching tiled background.
Apply the same placement and source-selection principles at every LOD, with that LOD's required
source resolution and native frames. No bridge-specific numerical width, center or source crop is
a universal constant. Keep selected geometry, bones and independent markings unchanged.
No full-span baking, period cuts or arbitrary structural derivatives are authorized.

## 2. Preserve source resolution and pattern scale

Follow the general bridge contract's [original UV projection and mapping rule](BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode).
That rule applies to fixed/non-movable and movable bridges, every part, material domain, LOD,
layer and axis. This section specifies material quality and size requirements in addition to
that common rule; it does not define a separate movable-only mapping policy.

Preserve native texture resolution, color space, normal encoding, channel correspondence, filtering,
anisotropy and complete mip/VT dependencies. Prototype-supported road appearances must tile
at original physical density, never stretch. For non-road or unsupported input appearances with
evidenced stretching axes, report the expected density change from retaining authored UVs;
do not call that density-preserving. A smaller page, different LOD or size limit never permits additional
resolution loss. Reuse original far-LOD resources only when they satisfy road tiling and marking-removal rules;
otherwise prepare a compliant road-only derivative at that LOD's source resolution. Do not
fabricate low-resolution replacements.

A newly converted VT Surface needs its own native reservation and texture parameter blocks
before publication, even when its source requires zero MidMips levels. Zero levels means no
intermediate cache file, not no virtual-texture initialization. Finish writing and closing the
descriptor before native registration/loading; keep original VT payloads unchanged. Initialization
must use the native streaming lifecycle, not a persistent mod-dependent rendering callback.
The native reservation must remain available when property references are released and later
reloaded for render-batch creation. Preserve it through the native pre-reservation API; a completed
tail read or a retained parameter-block field alone does not prove that the renderer can bind it.
Verify last-reference unload/reload and native batch reservation selection as well as source bytes.

Measure and report the complete persisted material graph of each generated bridge for information.
There is no per-bridge material byte ceiling or target range; do not reduce quality to meet one.
Section 5 retains the **55,000,000-byte** package limit. Meet that limit through valid reuse and
sharing, never omitted dependencies, downsampling or unauthorized pattern changes.

## 3. Tile repeatable materials at their original density

### Reuse source mapping before producing pixels

Keep the original atlas and native material where they express the required mapping. Original
mesh panels may reuse an internal region through duplicated seam vertices and finite UV traversals.
This does not require a new texture. Do not infer a global period from repeated UV triangles,
rounded dimensions, a chart rectangle or source wrap flags. Do not replace a large triangle's
UVs with vertex-only modulo and claim it repeats internally.

For extracted original repeats, establish the actual source pattern, region, physical period,
orientation and boundary behavior across all channels and mips. Repeated UV triangles, a crop,
source wrap flags or a receiver rectangle alone do not prove a filterable repeat. The road rule
below permits authored repeats from original road content. Non-road textures remain native. A sampling variant is not a new content identity.

Prepare each necessary known content unit once offline and share it across uses. Preserve measured
bounds and physical scale. Fractional source extents may use upward-rounded sampling grids and
required native block alignment without padding the physical repeat interval. The bounded two-period
rule below is the only multiple-period storage exception. Record reconstruction/encoding errors.

### Prototype-supported road surfaces must tile

For a movable bridge requiring road-material processing, a road appearance type supported by the corresponding
prototype must use that prototype's appearance through composition/precomposition, with tiling
at its original physical texel density and resolution. This includes asphalt, sidewalk paving
and other evidenced road-surface types, at every LOD and on moving and stationary road domains.
Do not stretch a finite source region to fit road width, lane width, length, caps or terminals.
A finite geometric domain limits where the material is drawn; it does not authorize stretching
its grain. Preserve selected geometry, lane layout, bones and independent markings. A Repeat
flag without continuous coordinates and the correct physical period/density does not satisfy this rule.
Non-road structure retains its own evidenced finite/stretch or repeating mapping; this road rule
does not authorize general structural remaking. Unsupported road categories follow section 4.

Reuse a compliant native material where possible. Otherwise prepare a bounded independent repeat
outside the game from the prototype's pure corresponding road content. Original nonperiodic
road grain may be assembled into an authored repeat, with the same donor coordinates in BaseColor,
Normal and Mask and corresponding handling of every other required channel. Record donor regions,
physical scale, new period, phase and operations explicitly; an authored period is not evidence
of an original prototype period. Do not include gray curbs, concrete caps or other categories in
an asphalt region simply because its descriptor is labelled Asphalt. Domains of the same actual
appearance must have coherent source/PBR parameters, density, orientation and phase across
moving/fixed seams and LOD transitions. Preserve genuine category boundaries.

Preserve base-level source texel density in both directions, including the complete Jacobian
for oblique projections; keep the original grain scale. Do not downsample to meet a byte limit.
Generate a complete legal native mip chain for the new periodic content. Use periodic filtering,
linear-light BaseColor RGB filtering, correctly decoded/filtered/normalized/re-encoded normals,
and the actual semantics of mask, alpha and control channels. Unchanged resources retain their
original mip bytes; a newly authored road repeat need not copy old finite-atlas coarse mips that
contain unrelated edges, markings or nonperiodic neighborhoods. Record source inputs, reconstruction,
compression and per-channel/mip errors. This permission is limited to preparing the required road
repeat, not arbitrary replacement of other original mip signals or a global tolerance increase.
Validate both seams, all mip/filter/aniso behavior, native frames/loading and complete dependencies.
Report owner measurements without a ceiling and retain the 55 MB package limit. No full-span pixels, per-receiver
texture copies or period-driven geometry cuts are permitted.

### Remove embedded road markings at every LOD

Draw02 moving-leaf exception: remove embedded markings, but do not add selected-road paint on moving leaves. Keep intended paint on fixed running caps and external approaches. This paint-only exception preserves receivers, bones and other styles. See the movable integrated-paint section.

Processed movable-road materials, whether reused, prototype-derived or composed, must contain no embedded road
markings at any LOD, including
caps, transitions, terminal regions and distant merged atlases. Remove their contribution from
all affected PBR channels and every mip, not only BaseColor mip zero. Use an evidenced same-category
donor or explicit offline composition recipe; preserve grain scale, normals and unrelated content.
Do not modify a shared source atlas used by structure. Bind a new road-only derivative where needed,
with its own CID and complete dependency graph. A finite original-road binding is not an exception.

Continue generating the selected road's intended markings from its actual layout through that
bridge's native paint path. Movable bridges use the specified independent single-sided native
skinned paint mesh on the original mechanism bones.
Do not delete this paint, change its layout or duplicate it with texture markings. Check near,
middle and far output: removing a line from one LOD or one cap is not completion.


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
only. It does not itself permit deliberate boundary blending or unrelated mip-signal replacement;
the separate prototype-supported road rule above governs filtering of newly authored road repeats.
Neither rule permits arbitrary resolution reduction, omitted channels, full-span baking,
new period cuts or increased budgets. Original assets remain unchanged; changed derivatives
receive independent CIDs. All normal-frame, alpha, filter/aniso, every-mip, native-load,
informational owner measurements and 55 MB package checks remain mandatory. No global error threshold or
automatic quality acceptance is implied.

### Offline source-atlas repeat materials

Do not cut movable road or structural triangles, or add seam vertices, solely to reset a repeated
atlas coordinate. Where such new seams would otherwise be necessary, bake an independent tiling
material from original road content only for an eligible running surface. Structural domains use their native material and authored atlas mapping.

Retain existing native seams and necessary cuts at actual shape, motion and distinct material-domain
boundaries. Preserve selected-road shape, lane layout, original attributes and bone ownership.
Road eligibility still requires the prototype-supported actual type/domain gate; this rule does not
admit unrelated appearance categories. Section 4 separately permits corresponding road replacement
without exact network-layer/worldspace reproduction. For non-road components, record why
this particular evidenced repeat domain needs an offline derivative to avoid new period-wrap seams.

Establish original source identity/version, all applicable LODs, complete repeat bounds and physical
period from prototype evidence for extracted repeats. Authored road repeats follow
[the supported-road rule](#prototype-supported-road-surfaces-must-tile) instead. Preserve density,
orientation and phase across neighboring uses and independent sampling axes/layers. Retain physical
terminal domains without stretching road patterns. Non-road finite axes retain their own evidence.
A crop is not automatically a repeat unit. Do not force a common layer period or turn unrelated
finite structural content into repetition.
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
as an informational owner measurement and against the 55,000,000-byte package limit. This rule specifies the required
route; it does not assert current feasibility, implementation or acceptance.

### No full-road pattern expansion

Never rasterize a pattern along the complete road/span into generated texture pixels. This covers
repeating, finite, non-repeating and mixed appearances, all channels, masks, worldspace layers,
markings and every LOD. Splitting the same expansion across charts, strips, pages or VT tiles is
also prohibited. Failure to find a period, Clamp addressing or an unsupported conversion is not a
fallback permission. The GPU maximum texture size is not a material budget; 4096 is not a universal
permitted size either. Smaller full-span images still violate the rule.

Express length through geometry and supported source mapping, with immutable texture content.
Evidenced non-road or unsupported-input stretching retains original UVs without a new
length-dependent image; prototype-supported road surfaces tile even within finite terminals. A
necessary local non-repeating result must be bounded by the actual local content, not the receiver
length. Do not hide failure through downsampling, omitted layers, arbitrary repeated crops or
excessive subdivision. Existing geometry and topology constraints remain in force.

### Independent material-layer sampling

These exact layer-composition requirements govern retained/composed source layers.
Section 4 governs accepted differences for corresponding native object road replacement.
Section 3's supported-road rule governs newly authored repeats and their periodic mip chains.
Neither permission authorizes unrelated material categories or structural changes.

Retained layers preserve their own coordinates, scale, phase, period, wrap and mip behavior;
eligible authored road repeats use their explicitly recorded new mapping and periodic mips. Do not force prototype road samples, selected-road inputs,
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
with offline authoring/baking tools before distribution, save the finished native material,
texture and complete mip/VT dependencies, and include them in the mod package. The game loads,
copies when ownership requires it, and binds those finished resources without rebaking,
resampling or recompressing their texture payloads.

Baking at game startup, asset loading, first use, preview, bridge creation or export is runtime
work, not precomposition. An in-memory or disk cache of an in-game bake is not a precomposed
material. The separate bounded runtime-composition exceptions below do not change this meaning.
Road precomposition remains subject to the prototype-supported-type gate; non-road bindings retain native texture references. Preserve applicable mapping rules and original
resolution, no full-road pattern expansion, independent persistence, owner size reporting and the package byte limit.

### Corresponding native road materials for movable objects

When an actual road appearance type is absent from the prototype domain, reuse a
compatible original material or replace the incompatible network material with the
corresponding native animated-object road material. This replacement is required
where it enables the movable road: asphalt remains asphalt, pathway/sidewalk paving
remains its evidenced paving category, and ballast remains ballast. Establish the
actual category from source texture identities and receiver coverage; a template
number, road name or a single supported layer is not a whole-receiver classification.
Unknown third-party sources without an evidenced corresponding material select the original-source
route under the noexcept routing rule above, never a generic asphalt fallback. Prototype-supported types still use prototype appearance.

The compatible replacement need not reproduce the original network's base/world
two-layer blend or dynamic worldspace projection exactly. A suitable original road
PBR family may supply the corresponding object appearance without that network
blend, and its projection may remain attached to the bridge during movement. Record
the selected family, omitted network blend and changed anchoring explicitly; do not
describe it as exact original-material reuse. This narrow exception overrides exact
network-layer/projection equivalence elsewhere in this contract for this road route.
It does not permit changing the appearance category, dropping markings/rails/sleepers,
replacing non-road structure, or erasing unrelated domains in a mixed receiver.

Preserve the chosen corresponding material's original physical density, orientation,
finite/stretch versus tiling behavior per axis, normal encoding/frame, alpha and
complete PBR/mip dependencies. Derive continuous coordinates in the actual receiver
frame; do not reuse a zero/default prototype patch or unevaluated network-packed UVs.
The world projection may be frozen in that frame, with no exact world anchoring
requirement. Preserve original road geometry, attributes not required by the mapping,
bones, all LODs, native animation and independent loading. Use native templates only,
with existing offline resources or necessary descriptor-only conversion; no full-span
texture baking, new period cuts, custom shader or runtime animation dependency.
Source assets remain unchanged; native descriptor changes receive their own identity.
Informational owner measurements, the 55 MB package limit and other quality requirements remain required.

### Prototype appearance composition

First dispatch each appearance type/domain by the prototype's measured support and actual source
coverage. A no-match returns the original-source route without throwing. Realize that route by
compatible original reuse or an evidenced corresponding native object conversion above.
Do not extend prototype support to admit it into prototype composition. The existing
measured descriptor-only track conversion is a native representation rule, not this composition
entry condition; it preserves source pixels and appearance and must not become a general fallback.

Only supported types that must use prototype appearance enter the following plan:

1. Resolve the measured prototype appearance, source masks, required input layers and physical
   domains. Keep unsupported neighboring types on their corresponding compatible road materials.
2. Reuse an identical resolved native result where available, or use descriptor-only handling
   where sufficient without baking pixels.
3. Bind a once-prepared composite, evidenced original repeat or eligible authored road repeat
   for known constant inputs; all supported road paving tiles at source density without embedded markings.
4. Compose genuinely variable local content only where the supported-type combination needs it,
   subject to section 3's prohibition on full-road rasterization.
5. If an actual selected third-party input required by that supported-type composition was
   unavailable before runtime, perform only the necessary bounded conversion. Unknown input
   alone does not admit an unsupported type into prototype composition or permit an unrelated replacement.

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

Measure the informational generated-owner material footprint over the complete saved bridge's material dependency
closure, including descriptors, non-built-in texture/VT payloads, required mip dependencies and
their persistence metadata. Count each shared file once within that owner. Built-in dependencies
that remain original references require no copied payload bytes; list these references separately.
The prepared files for a style are a separate measurement, not a substitute for each generated bridge's
complete informational material measurement. Include selected-road dependencies and permitted runtime
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
shared native cache or reviewed road paint source below, and must not fall through to another style or generated bridge.
Sharing storage must not resample or rebake the texture.

### Shared immutable MidMips source cache

The read-only cross-style sources are the three byte-identical road paint channels in
`Materials/Shared/Paint/<cid>.Texture` and the native cache in
`assets/Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` and its `.cid` sidecar,
shipped as `Materials/Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` beside the mod DLL.
The MidMips subtree contains only native cache dependencies. The Paint subtree contains only the
base color, flat normal and white mask referenced by a reviewed SkinnedPaint.Surface. Each
owner snapshots the exact dependencies of its Surface. Preserve native cache bytes, CID, tag-bearing filename,
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
in the informational generated-owner material measurement; count each common source file
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
2. **Road dispatch and composition:** verify noexcept matching and Original for unknown types,
   unmatched regions and mixed coverage; test deck and network-head branches separately at every LOD.
   Verify receiver classification and target-part correspondence independently, with same-type/different-part
   negative controls, external no-match approaches and actual visible bindings. Do not certify grating
   from a name or resource identity without full PBR/alpha evidence.
   Include each replaced component's top, edges/curbs, sidewalls, bevels and caps; test distinct
   sidewalk/median/approach counterparts and unmatched edges instead of accepting top-only coverage.
   Verify one common elevated context shared by geometry and materials, no alternative-level
   enumeration/fallback or downstream reselection, and all physical contributions retained.
   Missing material matches still select Original within that context.
   Distinguish routing no-match from resource/realization failure. Prove support per prototype/type/domain; unsupported types
   use compatible originals or section 4's corresponding native object road replacement, never an
   invented prototype support flag. Record accepted layer/projection differences. For supported types, record source layers,
   prototype/selected-input roles, masks, source frames,
   separate markings and whether the result uses direct binding, descriptor conversion,
   precomposition or bounded runtime composition. Do not label complete selected-road appearance
   preservation as a universal requirement. Verify the specified composite rather than a substitute.
3. **Mapping and road cleanliness:** prototype-supported road appearances tile at original
   resolution/physical density in every LOD; no width/length-dependent stretching, including caps.
   Verify complete projection Jacobians, actual uploaded UVs, shared phase/PBR across domains of the same evidenced prototype appearance, and independent layers. Non-road and unsupported-input mappings retain their own
   evidenced modes, including finite/stretch axes. Audit removal of embedded markings in all road
   domains/channels/mips while preserving independent selected-road paint. Verify complete wear
   source, matched background, measured visual-center lane alignment and fixed physical wear width.
4. **Layers and filtering:** test independent periods, finite/constant masks, differing resolutions,
   blend transitions, normals, alpha/cutout, source edges and all mip levels. Record unresolved
   per-layer tolerance validation; mip-zero/interior-only checks are insufficient.
   Verify semantic part ownership across independently selected native render-group LODs.
   Identical material payloads at near/middle levels do not rule out a missing-part window
   when the far version moves to another renderer; follow the movable contract's
   [material-batch rules](MOVABLE_BRIDGES_CONTRACT.md#material-selection-projection-and-render-batches).
5. **No span rasterization:** compare different bridge lengths and verify that base-pattern pixel
   storage does not expand with the receiver, even across multiple resources. Rejected period
   tests must not enter a full-road bake. Record every necessary local raster's content extent,
   dimensions and reason; hardware limits or small pages are not acceptance evidence.
6. **Reuse and lifetime:** known resources are prepared once offline; identical sampling uses share
   payloads. All preview/create/regenerate/export paths use the same representations. Verify cold
   loading without Bridge Builder, complete current-owner dependency closure, and independence
   from other generated bridges or deleted sources. Preserve healthy existing assets.
7. **Size reporting and package budget:** report prepared bytes and the complete persisted owner
   material graph separately, with no per-owner ceiling. The actual official SDK package must be
   at or below 55,000,000 bytes. List built-in references separately. Mark missing owner measurements unavailable. Keep the
   general contract's whole-Prefab measurement separate and reject empty material directories.

Record generation, decoding, composition, readback, VT/mip writing and persistence timing without
double counting. Native file checks and code review do not establish game appearance, animation,
traffic or performance acceptance. Human near/far, motion and cold-load checks remain explicit.
Changing this specification does not change installed code or certify current generators.
