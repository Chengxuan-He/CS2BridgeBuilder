# Moving road GPU baking

BridgeBake.compute contains generation-only road and decal kernels. Unity 2022.3.62f2
builds their Windows asset bundle; the normal mod build embeds assets/bridgebake in
BridgeBuilder.dll. Generated bridge assets do not reference this bundle or its compute
shader. Persisted surfaces use compatible native object templates under the
[movable material rules](../../docs/agent-contract/MOVABLE_BRIDGES_CONTRACT.md#17-material-families-uv-density-and-texture-ownership).
Moving paint uses the separate single-sided skinned Default path, not child DefaultDecal projectors.

Follow the [material generation contract](../../docs/agent-contract/MATERIAL_GENERATION_CONTRACT.md):
reuse compatible originals directly, prepare constant converted resources offline, and restrict
generation-time baking to genuinely variable nonperiodic content. Repeatable materials use
source-density repeat units rather than full-span images.

Run Build-GpuBake.ps1 with a dedicated -ProjectPath under the task's work directory
whenever the compute source changes, then run the normal Release build/deployment.
The script uses the installed Unity editor and requires its valid local license.
Do not pass an existing unrelated Unity project: this dedicated build project has a
minimal package manifest with the built-in AssetBundle module. The current variable-material
baking path requires GPU support and has no CPU fallback; this implementation detail does not
override the contract's permission to choose a measured, correct CPU path. Bundle/kernel loading
failures remain explicit operation errors for requests that use those kernels.

GPU work includes chart barycentrics, texture filtering, world-space material blending,
normal evaluation/encoding, curved-decal projection, endpoint fading and channel packing.
CPU work includes geometric partitioning, uploads/readback, array transfer into native
TextureAsset serialization, native compression, and saving. These remaining operations
can still stall a synchronous generation request; GPU migration is not a claim of an
asynchronous UI or a measured frame-time guarantee.

The C# reference evaluators retain the previously audited equations for inspection;
the production atlas and receiver paths dispatch MovableGpuBake instead of invoking
their per-pixel evaluation loops. Float GPU arithmetic and hardware filtering need
human visual acceptance against native materials.

## Offline native appearance units

`PrepareRepeatUnits.Editor.cs` and `RepeatUnit.compute` consume
`BridgeBuilder/RepeatUnits/3` manifests in an independent authoring project. These files
are separate from the runtime `BridgeBake.compute` bundle. A unit describes an original,
evidenced finite appearance domain or repeat period; its resolution and physical extent
must not grow with a generated receiver's road length.

- `repeatAxes` is `UV`, `U`, `V`, or `None`. `None` produces a native Clamp texture;
  a repeating unit uses native Repeat and guards its finite axis.
- `finiteSourceGrid: true` locks finite axes of one affine source region to original
  mip-zero texel centres, including the guard region. `finiteGridMip: N` additionally
  aligns the origin and dimensions to `2^N`, preserving that finite-axis grid through
  mip N. This may increase the unit footprint and must pass the owner budget. It does not round repeat boundaries or
  change physical periods. Fractional repeat regions retain their exact source bounds
  and use the reported unit sampling density. Multiple bands currently require the
  explicit resampling path; use separate existing physical domains where appropriate.
- Every product reports `contentMin`, `contentMax`, `axisSampling`, and density ratios.
  Use these actual bounds for runtime finite-axis UVs; do not assume `0.25..0.75`.
  Aligned PBR channels must agree on these normalized bounds.
  Reports include actual mip dimensions, source pixel origins/extents, and source LODs;
  `sourceLodByMip` is mip-major with `sourceLodBandCount` entries per mip. NPOT mip
  dimensions participate in the sampling footprint, rather than assuming exact halving.
- `normalTransform` is an optional row-major orthogonal 3 by 3 transform from the
  measured original uploaded tangent frame to the actual target mesh frame. Bands may
  specify their own transform and `flipU`/`flipV`. A UV mirror does not establish a
  tangent-frame mirror; mirrored bands require an explicit measured transform, which
  may be identity. Identity preserves packed RGBA normal channels without decoding and
  re-encoding them. Do not infer it from the UV Jacobian alone.
- Runtime `Patch(..., surface: finishedSurface, preparedUnit: true, repeatU: ..., repeatV: ...,
  tangent: measuredTangent)` directly binds the completed native asset and final UV
  projection. Both repeat flags may be false. Finite coordinates must remain inside
  the guarded texture domain; no runtime crop, repeat extraction, or texture bake occurs.

The extractor samples source mip levels using the actual source-to-content density and
preserves authored source filtering and wrap behavior. `RepeatUnitSamplingCheck.Editor.cs`
checks source-grid texels, LOD selection, explicit normal transforms, UV mirror independence,
and finite Clamp output. These checks do not certify arbitrary atlas-edge equivalence:
coarse source mips may contain neighbouring atlas content that a smaller repeating texture
cannot reproduce. Validate each finished unit's filtered boundaries, normal angles,
compression error, and complete persisted footprint before integration.


### Binding offline repeat units without period seams

Use the completed-unit Patch interface above for evidenced repeat domains. Do not
add source-atlas wrap cuts or UV-reset-only appearance partitions. Keep actual
motion, shape and distinct material boundaries and original finite terminal domains.
A finished unit uses continuous coordinates in its own texture space; do not apply
an atlas-region inverse transform to those coordinates again. Independent axes
retain their measured phase and finite guards. Sources stay unchanged and derived
resources receive new identities. Failed mip/filter or byte checks remain pending.

`projectTangent: true` is an explicit alternative to a constant `tangent` override
for `sourceUv` or `preparedUnit` patches. Use it only with measured frame evidence
that projection and receiver normal determine the source material frame. It preserves
receiver normals; constant and projected overrides are mutually exclusive. Prepared
units otherwise retain the input tangent, color and UV.zw. UV mirroring alone does
not authorize changing the tangent frame.

### Two-axis source footprint experiment

For a controlled comparison, `/3` units may opt into `sourceGradientSampling: true`.
The producer then calls native `SampleGrad` with separate source UV derivatives for
the actual width and height of each output mip, retaining the source filter and
anisotropy. Band derivatives include their measured span, repetition and orientation.
The default scalar max-axis `SampleLevel` mode is retained as a diagnostic control;
it does not establish equivalence when NPOT dimensions produce unequal axis scales.
The mode participates in the content key. Reports identify `sourceSampling` and both
source texels per output pixel; `sourceLodByMip` remains a scalar comparison only,
not a claim about the native anisotropic sampler's chosen taps or mip levels.

`RepeatFootprintCheck.Editor.cs` compares the producer to an independent native GPU
`RepeatFootprintProbe.compute` over every mip, with a scalar-LOD negative control.
This checks the reconstruction footprint. It does not prove that a repeating unit
can reproduce neighbouring atlas islands at boundaries, or that later native
compression, full PBR sampling and byte limits pass. Revalidate the actual source
unit after changing this option; do not raise quality tolerances to accept it.

### Offline piecewise source panels

`panels` optionally defines a complete genuine semantic unit using measured offline
triangles. Each panel contains `target: [u0,v0,u1,v1,u2,v2]` in normalized unit space,
`source: [u0,v0,u1,v1,u2,v2]` in original atlas coordinates, and an optional measured
`normalTransform: [9]`. The producer derives each affine source Jacobian and samples
that panel once into the shared finished unit. These triangles are authoring data:
they never become receiver clipping planes or generated bridge topology.

Panels cannot combine with `bands` or affine `finiteSourceGrid`. Finite axes use the
existing guarded content interval. Dimensions use the largest measured directional
source-texel gradient, rounded upward, so a smaller patch cannot lower another patch's
resolution. The complete source mip chain, source sampler and explicit frame handling
remain in force. A GPU uncovered-sample check fails incomplete recipes rather than
sampling a fallback crop. `RepeatPanelCheck.Editor.cs` covers four panels with distinct
Jacobians, finite guards, complete mips and an incomplete-recipe negative control.
A structural/road source still needs its own coverage and filtered PBR validation;
these tests do not certify arbitrary source panels or approve new appearance errors.


Explicit finite panel guards use `guardPanels`: each entry names `adjacentPanel`
and supplies six-number `target` and `source` triangles. A guard must share a
complete measured outer edge, lie outside all content panels on that supporting
edge, and terminate at a finite unit boundary. Internal holes and repeat-axis
extensions are rejected. Sampling inherits the adjacent panel's full affine UV
projection, derivatives and normal frame; the thin guard is not independently
fitted. Source coordinates are checked against that affine mapping (a numerical
recipe consistency check, not a material-quality tolerance). Guards participate
in content identity. Missing coverage still fails with mip, count and first pixel.


`contentMin` / `contentMax` locate the original content inside a completed guarded
texture. They are not the complete valid finite sampling domain. Successful full
extraction also reports `finiteSamplingMin` / `finiteSamplingMax` (currently zero
and one on both axes); every output texel and mip must have been authored, including
explicit panel guards, or extraction fails. Existing completed products from the
same extraction rules can retain their bytes and obtain this metadata from their
validated recipe and full-mip output evidence; do not rebake solely to add fields.
At runtime `Patch.WithFiniteSampling(min,max)` explicitly consumes that declaration.
Only PreparedUnit accepts it; defaults remain content bounds for unproven products.
The declared domain must contain content and stay within the texture. Finite UVs
must remain strictly inside zero and one and inside the declared domain. Coordinates
are never clamped, snapped or assigned an epsilon. Geometry still uses its measured
appearance/physical domains. This metadata does not waive filtering or image-quality
acceptance and must not be used for arbitrary source-atlas regions.


For a measured exterior region with more than three vertices, `guardPolygons`
uses the same `adjacentPanel`, flattened `target` and `source` arrays. Validation
proves a simple convex polygon sharing a complete outer content edge before fan
triangulation; individual fan triangles need not each share that whole edge.
Interior holes remain forbidden. Each generated guard triangle inherits the exact
adjacent affine projection and native normal frame.

`allowClampedSourceGuards` is an explicit option for source UV extrapolation beyond
[0,1] in those guard polygons. Every source channel's actual native header is read,
including cached-product runs, and must specify Clamp. The producer preserves its
filter and anisotropy; it neither changes the sampler nor clamps UVs itself. Reports
include `sourceWrap`, `sourceFilter`, and `sourceAnisotropy`. Ordinary content panels
must still address the evidenced source atlas domain. This is not a quality waiver.

When authored panels already include the complete finite texture guards, provide
`panelContentMin` and `panelContentMax`. Panel targets then address final texture
coordinates directly: the producer does not add another finite-axis doubling or
q=saturate(2u-.5) remapping. The content window is reported exactly as authored;
output density is computed from the full panel gradients and full mip processing
is unchanged. Omitting these two fields preserves existing recipes and padding.
This option removes accidental double padding, not measured guard pixels.

For measured paint replacement in a bounded unit, `paintEvidence` identifies the
hashed recipe and `paintMipRules` supplies every original source mip. Each rule
contains `sourceMip`, `width`, `height`, and ordered `masks`; each mask contains
`sourceTexelRectHalfOpen`, its matching `sourceUvRect`, `donorSourceTexelX`, and
`donorSourceUvX`. The donor is an original texel center. At each contributing
integer mip, test the original source UV against that mip's half-open rectangles,
replace only U with the last matching donor, preserve V, and sample that original
mip. Blend these contributions at the declared scalar source LOD. BaseColor,
Normal and Mask use the same recipe; Control bypasses it. Outside both contributing
masks, retain the original SampleLevel call byte-for-byte. Sources must retain
their native Trilinear sampler; SampleGrad composition is rejected because it is
not covered by this operation. No source file is rewritten. The supplied evidence
must prove the measured masks and donors, not infer missing source pixels.
New phase/filter/native-output validation is still required; earlier whole-atlas
mask-isolation measurements do not certify the new bounded output.

Piecewise reports include row-major `sourcePanelJacobianByMip` (four values per
panel, mip-major) and `sourcePanelMaxSingularByMip`. Units are original source
texels per output texel. Two short column vectors do not establish sufficient
sampling in every direction. Explicit `fullDirectionalDensity:true` enlarges the
base grid uniformly by the greatest panel singular value when it exceeds one,
then applies the existing integer/block alignment. It preserves the measured
source affine and physical period and uses a distinct cache marker. The default
is unchanged. This is a density bound, not a filtering-equivalence or mip-quality
claim, and does not permit lowering resolution to meet package budgets.

### Bounded two-period rectangular storage

`twoPeriodAxis: "U"` or `"V"` opts one evidenced rectangular repeating axis into
exactly two stored periods. The chosen axis must repeat; bands and panels are
currently rejected. Omission retains the existing one-period path and cache keys.
The full base dimension doubles on that axis, while source bounds and physical
period remain unchanged. Each native mip contains two periods; deeper per-period
texel extents may still be fractional. The compute gradient includes the period
count. Reports contain `periods` and per-period density/footprint values.

The generated Surface divides its original UV0Scale and UV0Offset on that axis by
two. Receivers keep their previous unit coordinates and phase; do not also divide
receiver UV. A direct Texture-only validation must instead apply this Surface
transform explicitly once. New payload keys include the bounded-storage marker and
period count. This is neither deliberate seam blending nor a quality/budget pass.
