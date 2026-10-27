# Draw02 structural semantic units

## Implemented production paths

`Draw02RepeatRegions.cs` registers three measured genuine units for the exact original Surface digest `e8c7355d952fdd88921b83fc08e5bc66018457292182aa46bca9d79a896a9597`. Unit bounds are stored as twelve exact F64 numbers in the lazy BBN resource `Draw02_StructureRepeatUnits.bin` (160 bytes). These regions use the existing moving-sidewalk, fixed-road and fixed-sidewalk native units only where the complete original domain lies in the corresponding measured source region. No bounding rectangle is used to invent a new periodic pattern and no coverage expansion is introduced.

The structural source is native template 1. Its three existing template-1 descriptors therefore remain required and are distinct from the road-composition template-14 variants. They share the same twelve texture payloads, including real Control, without baking or encoding again. These are active structural bindings, not obsolete descriptor files:

- `ec7aba3a4552ce9cd0ac0f63fb9dd6715bb3ecec97fc038c6c9de625e528b2cd`
- `1f7e74d5483eab77b53257e4a5c73bc86efa2e23efb3cae6474206fbd234dd86`
- `ac901cefe3531d3480478aa588fac413ce742728904414c0b28dd3bb82a2edce`

The production path still prefers an original compatible native Surface when `CanUseSource` succeeds. Necessary template-17 conversions use the existing verified descriptor-only mapping. This change preserves all original textures and creates no runtime pixels.

## Complete grouping and bounded coverage check

`GroupStructureUnits.py` enumerates all 38,210 original domains in the existing 45-mesh, all-LOD BBN/native-chart inventory exactly once. The `StructureUnits/1` audit passes 24 groups: 324 domains select three existing genuine units, 78 have wholly contained sampled receivers, and 37,808 are unresolved or have no changed receiver in the retained finite inventory. Zero samples is explicitly not proof of arbitrary-width compatibility.

`StructureRegionCheck` compiles the actual shared `MovableStructurePacking`, `MovableRepeatRegions`, `MovableUvChart`, Draw02 chart data and the new Draw02 region partial. Replaying the retained width-delta 8/24 plans produces:

| Disposition | Receiver triangles |
| --- | ---: |
| Original native mapping, including gradient checks | 182 |
| Newly routed existing genuine units | 14 |
| Unresolved | 14,824 |
| Total | 15,020 |

The check verifies actual source digest dispatch, native template 1 for the selected structural bindings, finite mapped coordinates and the public density-preserving region mapper. It does not substitute geometric bounding-box containment for native chart checks. It uses retained Float32 receiver plans; integrated generation with current phase reconstruction and arbitrary selected widths remains separate acceptance.

Evidence is under `artifacts/prepared-materials/Draw02/structure-units/`: `original-domains.json`, `membership.json`, `manifest.json`, `audit.json`, `production-region-check.json` and `graph-check.json`. The public audit verifies inventory bookkeeping and evidence hashes, not semantic completeness or successful generation.

## Resource size and remaining constraints

The official Draw02 source library remains **112 files / 6,017,894 bytes**, including 39 Surface descriptors and 17 Texture payloads. All referenced local CIDs resolve, each payload is stored once, and there are no orphan textures. No additional texture byte was added by this structural integration. This source-directory total is below the 10,000,000-byte working target but does not prove the complete generated bridge dependency footprint or final SDK package size.

Most unresolved receiver samples are in the main Draw02 atlas: 11,150 samples belong to groups with at least one out-of-chart receiver. Other groups include pillar/base atlases, road-network templates and low-detail atlases. Arbitrary original islands are not automatically repeating materials. The next implementation needs measured semantic base/detail units for those specific material families, or a supported exact nonperiodic mapping. It must not restore the removed per-triangle structure library or broaden coverage using only rectangular similarity.

No additional evidenced structural pattern was found in this bounded pass, so the batch reused the three already prepared genuine units and baked zero textures. The previous 953-key, 170 MB per-sampler library is not a valid fallback and was not restored. Whole Draw02 generation, preview/create/regeneration/export, cold load and independent-owner persistence, near/far/motion visuals and total SDK budget remain unverified. No UI/localization text changed. Shared integration owns the final mod build.
