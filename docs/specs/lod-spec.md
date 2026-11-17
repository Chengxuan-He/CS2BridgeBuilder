# LOD specification

Read this complete specification with the [generation contract](../contracts/generation-contract.md). It governs all bridge LOD work; material, mechanism and geometry obligations remain applicable.

## Native selection and source preservation

Object and network renderers can derive different effective LOD transitions from the same
authored bias. Copying a source bias, including zero, is not proof of equivalent near/far
behavior after conversion. Compare the source's native transition calculation with the final
network composition, including the actual selected road and structural index density, native
bias aggregation and threshold quantization. Retain every source LOD and its part coverage
under the applicable generation and source-representation requirements.

Use the actual prototype's native LOD parameters and the game's native selection implementation.
Every formula used to document or justify LOD behavior must meet the
[native LOD formula provenance requirements](../documentation-standards.md#evidence-and-normative-text),
including verified local Game.dll identity and exact native class/method evidence. A project
derivation, fit or inverse calculation is not a native formula and does not authorize an adjustment.

Preserve the actual prototype's authored LOD bias, shadow bias and complete LOD chain,
according to the applicable generation and source-representation requirements. Rebuild LOD mesh references to the
corresponding owned derived meshes and leave selection to the native forward calculation.
Do not compensate for object-to-network or road-composition differences by reverse-solving
or rewriting bias from road density, piece counts or a target distance, or by applying an
opposite shadow-bias adjustment. Moving that compensation offline, hardcoding its result or
renaming it does not change this restriction. Keep source assets and global quality settings
unchanged; no per-frame LOD controller is permitted. The game's normal native selection and
player-controlled graphics/LOD quality settings remain available.

## Native calculation sources and notation

The following are forward calculations transcribed from the inspected native implementation,
not a project inverse, fitted distance rule or permission to change authored parameters.
The local Game.dll SHA-256 is
`F076DF4B70AA0F21A80345E03A052CA2273BC6754F41DAB7E63D837837A43745`;
assembly/file/product versions are all `0.0.0.0`. The [native source record](../archive/audits/native-lod-formula-sources.md)
identifies the exact extracts, hashes and line ranges. Recheck them when the binary changes.

Lengths and bounds sizes use native world metres; surface area uses square metres. Index
counts are index entries, not triangle counts. Biases, logarithms, quantized limits and LOD
indices are dimensionless. `m` below is the native `metersPerPixel` rendering-size quantity,
not a camera distance. Preserve native float operations, constants, branches and ordering;
mathematical notation does not authorize replacing bit operations with a fitted logarithm.

### Mesh bias preparation

Source: `Game.Prefabs.MeshSystem.OnUpdate`, extract lines 453–510, with
[identity and locator](../archive/audits/native-lod-formula-sources.md#mesh-bias-preparation).
When the native LOD buffer exists, authored `LodProperties` contributes:

```text
B = component.m_Bias
S = S_existing + component.m_Bias + component.m_ShadowBias
```

`S_existing` is the previously prepared native shadow bias, not assumed zero.
Without that buffer, the small-mesh branch applies only when maximum bounds dimension
`d` is positive and at most 5 metres:

```text
B = 1.2 * pow(d / 5, 3)
S = max(S_existing, 1.5 * B)
```

When `prefab.surfaceArea > 0`, let `(x,y,z) = bounds.max - bounds.min`
and `A = prefab.surfaceArea`. Native area/shape corrections then apply:

```text
a = log2(sqrt(clamp(A / (2 * (x*y + y*z + z*x)), 1E-06, 1)))
c = log2(sqrt(clamp(3 * max(min(x,y), min(y,z), min(z,x)) / (x+y+z), 1E-06, 1)))
B = B - a
S = S - (1.5 * a + c)
```

These are native preparation steps, not custom shadow compensation. Preserve the actual
native branch and previously prepared fields instead of applying all branches unconditionally.

### Network composition density and bias

Source: `Game.Prefabs.NetCompositionMeshRefSystem.CompositionMeshRefJob.CalculatePieceData`,
lines 161–205, with [identity and locator](../archive/audits/native-lod-formula-sources.md#composition-density-and-bias).
For the actual supplied composition-piece buffer, use each piece's prepared MeshData:

```text
F = sum_i(I_i / max(1, zMax_i - zMin_i))
B_net = sum_i(B_i) / N          when N != 0
S_net = sum_i(S_i) / N          when N != 0
```

`F` is index density per native metre (the denominator has a one-metre floor),
`I_i` is the piece index count and `N` is piece count. Both biases start at zero
and remain zero for an empty buffer. Bias averaging is arithmetic per piece, not weighted
by triangles, length or area. Include the actual selected-road and structural pieces in the
native buffer; do not substitute structure-only density for the combined composition.

### Object and network rendering size

Source: `Game.Rendering.RenderingUtils.GetRenderingSize` and
`GetShadowRenderingSize`, lines 137–208, with
[identity and locator](../archive/audits/native-lod-formula-sources.md#rendering-size-and-quantization).
For a non-stacked object with native bounds sizes `(x,y,z)` and index count `I`:

```text
R3(x,y,z) = (x+y+z) * (1f / 3f)
R3I(x,y,z,I) = (x+y+z) * 0.57735026 * rsqrt(I)
```

The unweighted overload supplies the initial rendering size; the indexed overload supplies
lower-mesh boundary calculations. For network width `w`, height range size `h`
and composition index density `F`:

```text
L = max(8*w, 4*h)
R2(w,h) = R3(w,h,L)
R2F(w,h,F) = R3I(w,h,L,F*L)
ShadowR2(w,h) = R3(w,h,max(2*w,2*h))
```

For stacked Object lower-mesh boundaries the same native overload maps dimensions
and index density as follows; `meshSize` is the lower mesh's bounds size, while `boundsSize`
is the root group's size:

| Stack direction | Two-dimensional bounds input | Index density |
| --- | --- | --- |
| Right | boundsSize.z, boundsSize.y | I / max(1, meshSize.x) |
| Up | max(boundsSize.x,boundsSize.z), min(boundsSize.x,boundsSize.z) | I / max(1, meshSize.y) |
| Forward | boundsSize.x, boundsSize.y | I / max(1, meshSize.z) |
| None | Full boundsSize through R3I | Uses I directly, without density conversion |

The unindexed stacked overload uses the same dimension mapping through R2; its shadow
overload uses ShadowR2. Do not apply the unstacked object formula to a stacked source.

### Quantized limits and batch boundaries

Source: `Game.Rendering.RenderingUtils.CalculateLodLimit`, lines 75–84, and
`Game.Rendering.RequiredBatchesSystem.RequiredBatchesJob.InitializeBatchGroup`,
lines 510–712; see [rendering source](../archive/audits/native-lod-formula-sources.md#rendering-size-and-quantization)
and [batch source](../archive/audits/native-lod-formula-sources.md#batch-boundaries).
The exact limit helper is:

```text
m = m * pow(2, -bias)
t = m * m
Q(m,bias) = (255 - (asint(t * t * t) >> 23)) & 0xFF
```

`asint` reinterprets float bits as a signed integer; it is not a numeric cast.
Do not replace this discrete expression by a continuous distance formula. For Net groups,
the initial ordinary boundary uses Q(R2(w,h), B_net); the initial shadow boundary uses
Q(ShadowR2(w,h), S_net). Object groups instead begin with MeshData.m_MinLod and
MeshData.m_ShadowLod. With `K` lower-mesh references:

```text
x = min(initial ordinary boundary, 255 - K)
s = clamp(initial shadow boundary, x, 255)
for j = K-1 down to 0:
    emit lower-mesh batch using the current x and s
    x = clamp(Q(lower rendering size, root ordinary bias) + 1, x + 1, 255 - j)
    s = max(s, x)
emit the final root-mesh batch using the resulting x and s
```

The lower Net size uses that lower composition's index factor, with root width/height.
The lower Object size uses root bounds, lower mesh bounds and lower index count with
its stack direction. Zone uses its native ZoneMeshHelpers-generated count and root bounds
instead of reading a lower Object mesh. For Decal/Impostor lower meshes, the native branch substitutes one
quarter of the root index count. Lane handling uses lower index count divided by the lower
mesh's longitudinal bounds length with the same one-metre floor. These branch conditions
and the coarse-to-fine iteration matter: `CalculateLodLimit` alone is not the next batch's
minimum; the +1 and monotonic clamps must remain visible in reports.

## Acceptance

Acceptance compares source and generated effective thresholds, all retained LODs and shadow
behavior with differing selected-road compositions. Check preview, placed near/far rendering
and persisted reload separately; formula reconstruction and isolated native tests do not prove
camera-distance or visual acceptance. The [native source record](../archive/audits/native-lod-formula-sources.md) identifies the inspected implementation; numerical reconstruction does not establish game acceptance.

## Per-level triangle disclosure and reference scope

Before importing a non-Network prototype, record the triangle count of every source and generated LOD for each structural part. Identify the exact source version, mesh identity, retained or omitted levels, material partitions and corresponding generated parts. Distinguish a unique mesh from instantiated copies, a main span from towers/supports, and one variant or conditional piece from the whole realized bridge. State selected road, span count and placement multiplicities before giving an assembled total; do not sum mutually exclusive variants or every conditional section. A missing authored lower mesh is not a zero-triangle LOD.

Compare like-for-like scopes against the [installed non-mod bridge inventory](../archive/audits/native-bridge-lod-inventory.md). Approximately 5,000 triangles is a comparison reference only, not a hard limit, automatic simplification target or eligibility gate. Disclose visual/structural differences and costs rather than omitting members to meet that number. Do not substitute one main-mesh count for complete bridge cost or infer gameplay performance from triangle counts alone. Retain source evidence and report offline geometry, native rendering and performance acceptance separately.
