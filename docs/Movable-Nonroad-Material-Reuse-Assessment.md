# Non-road material reuse and supported-type road composition

## 1. Normative scope and implementation assessment

The generation scheme is specified in [movable section 17](agent-contract/MOVABLE_BRIDGES_CONTRACT.md#17-material-families-uv-density-and-texture-ownership)
and the [material contract](agent-contract/MATERIAL_GENERATION_CONTRACT.md): reuse original
non-road materials; compose/precompose outside the game only road appearance types supported by the prototype;
retain original input materials for other road types; preserve each
source's original projection and tiling/stretch mode. This is a generation requirement, not
an original-game finding or a claim that current code already complies. Source evidence remains
in [native material repetition](Native-Movable-Material-Repetition.md).

Road composition has one entry condition: prototype support for the actual appearance type.
Asphalt, sidewalks and wheel-track/rut effects are tested separately for each prototype.
Unsupported types bypass composition and retain original input materials. Supported-type
composition uses the established prototype/input/mask rules. It is not a universal
requirement to retain the selected road's complete appearance. The code assessment below records
remaining non-road conversion paths, not permission to keep them as compliant fallbacks.

An original Surface or atlas can serve road and structure faces together. Separating their
sampling assignments must never mutate that shared original Surface, its texture pixels or its
bindings. The unaffected structural faces can retain the original reference while the road
faces use the independently selected compatible material or a necessary permitted conversion.
Identifying road faces and preserving all LOD/bone assignments are required; a material slot
name alone does not establish that separation. This requirement grants no new topology-cutting rule.

Three questions must be evaluated separately:

1. **Appearance:** is the non-road source appearance intended to remain unchanged?
2. **Identity:** can the original Surface, shader/template, parameters and dependency identities
   remain compatible with the actual native render context?
3. **Sampling:** after authorized geometry changes, do its UVs still preserve the required
   finite chart, repeat density, phase, orientation, normals and mip behavior?

Changing width can require UV adaptation even when no texture pixel should change. Under the
[general bridge source-mapping rule](agent-contract/BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode),
which covers both fixed and movable bridges, the actual appearance source retains its evidenced
mapping mode per part, LOD and axis: tiling stays at source density; finite/stretch mapping retains
or interpolates authored UVs. This includes a selected road's own evidenced mapping where its
appearance is required, not permission to inherit the prototype's classification. A failed containment or gradient test
is not evidence of non-tiling. Original panel resets do not authorize adding new texture-period
cuts. Only prototype-supported types use prototype appearance through composition; unsupported
types keep original materials.

Fixed native NetPiece graphs and skinned object graphs are different material contexts.
An original network Surface can be retained where its graph preserves native composition,
NetPieceTiling and LOD semantics; the same shader is not automatically compatible with a
moving object. Selected rails, sleepers, lane graphics and paint remain distinct selected-input
requirements where applicable. They must not be silently replaced by prototype structure
materials or omitted from owner-local external dependency snapshots. Built-in dependencies
remain native references; external dependencies retain their required independent copies.

## 2. Current code paths, not blanket reuse acceptance

The inspected code has several routes:

| Path | Current behavior | Meaning for compliance |
| --- | --- | --- |
| `MovableStructureRetile.Append`, native-network early return | Retains the exact passed Surface when the fixed graph capability and measured render/mesh/CID gate pass. | Direct native network reuse is contextual, not a global non-road bypass. |
| `PreserveSourceUv` / retained-axis dispatch | Skips density remapping for source-evidenced finite domains, or preserves the evidenced axis while handling the other. | UV handling is separate from changing texture content. The eventual Surface may still pass through `NativeSurface`. |
| `MovableStructurePacking.Unit.CanUseSource` | Checks finite coordinates, source chart containment and representable gradients; rejects repeat-only coordinates. | It is a particular sampling eligibility test, not a universal proof for every structural domain or all mip footprints. |
| `MovableStructureTiles.Build`: `CanUseSource` | Chooses `NativeSurface(source)` and original-coordinate mapping. | The name “original” in counters is not alone proof of the initial Surface CID: style-specific native resolvers can select a converted descriptor. |
| `MovableStructureTiles.Build`: region match | Loads `BridgePreparedMaterials.RepeatUnit` for an evidenced region. | Current non-road processing still includes derived prepared materials. |
| `MovableStructureTiles.Build`: remaining unit | Resolves a prepared `Structure` key. | This is not direct original reuse; a missing prepared key is not permission to invent a material or silently keep an invalid mapping. |

See [structure dispatch](../src/BridgeBuilder/Bridges/Common/Movable/MovableStructureTiles.cs),
[UV retile handling](../src/BridgeBuilder/Bridges/Common/Movable/MovableStructureRetile.cs),
[sampling eligibility](../src/BridgeBuilder/Bridges/Common/Movable/MovableStructurePacking.cs)
and [object material compatibility](../src/BridgeBuilder/Bridges/Common/Movable/MovableSurfaceAssets.cs).
A future implementation must compare actual source/output assignments and UVs by part and LOD;
this code review does not certify all non-road output as identical to the prototype.

The design assessment itself supplies no additional permission beyond the current contracts. The 55,000,000-byte official package limit and
10,000,000-byte generated-owner material limit remain separate. Native independent persistence,
original resolution, permitted topology, no custom shaders or manager-dependent rebinding,
and no full-span pattern rasterization remain required. The independent-layer implementation
and its measured per-layer error tolerance are still unresolved work.
