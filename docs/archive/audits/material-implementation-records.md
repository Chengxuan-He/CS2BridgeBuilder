# Recorded material implementation evidence

The historical sections below retain evidence relocated from linked source documents; relocation performed no new audit or game test. The dated regression section records its separate verification scope. Statements describe only their recorded revision and inspection scope, not current acceptance. Unspecified original revision/artifact provenance remains unspecified. Current requirements remain in the originating contracts and specifications.

## Native118 through native121

Source: [docs/contracts/material-contract.md](../../contracts/material-contract.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> **Historical implementation status (not the current compatibility decision policy):** native118 does not satisfy this contract. Its CID-specific
> TiledRoad conversion and omitted network worldspace blend require a general complete
> replacement; the remaining throwing material paths require result-based handling. The
> native118 isolated descriptor tests establish only their recorded scope. Native119 adds
> non-throwing outer generation/material setup and cleanup boundaries, contains material
> failure-callback errors, and reports publication completion failures. These independent
> changes do not implement complete arbitrary-source adaptation or replace the remaining
> internal throwing APIs. Both the CID-specific conversion and omitted worldspace layer
> remain unresolved; native119 is not acceptance of this complete contract. Native121
> converts lane/cable material creation and its graph consumer to explicit result APIs,
> including native-read/save errors and incomplete adapters. All ten mechanism generators
> propagate an absent graph through their nullable result instead of throwing it again. It preserves direct reuse
> and existing complete lane descriptor-copy paths across source identities. Textured/VT
> cables without a complete adapter now return an incomplete-representation result instead
> of claiming their incompatible network descriptor is a finished object material. Complete
> unknown network-material representation and the other internal material APIs remain open.

## Draw01 physical part correspondence

Source: [docs/specs/material-spec.md](../../specs/material-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> The Draw01 correspondence is moving carriageway asphalt,
> moving sidewalk grating, fixed sidewalk paving and fixed running-cap concrete. This is an example
> of measured part correspondence, not a universal appearance list for other bridges.

## Native144 coverage and native decal capability

Source: [docs/specs/material-spec.md](../../specs/material-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> The installed native144 chart-coverage implementation does not satisfy this rule:
> it deliberately rescales lower-mip alpha, and its object cutoff is enabled where
> native CurvedDecal line sources use continuous DBuffer blending. It remains an
> unresolved implementation defect, not accepted appearance or a complete repair.
> Reverting only its coverage change would restore the previously observed far-line
> loss; disabling object alpha testing would expose opaque rectangles. Neither is a
> completed fix. Do not deploy either regression merely to report a changed threshold.
> 
> The currently inspected native DefaultDecal DBuffer mesh path does not consume the
> road procedural-bone protocol; CurvedDecal additionally requires lane-instance curve
> inputs. A Surface swap alone is not an evidenced independently persisted moving
> representation. Report this concrete capability gap without claiming that all future
> native arrangements are impossible. Independent loading and the prohibition on a
> continuing mod rendering/synchronization service remain unchanged.

## Static decal projection protocol

Source: [docs/specs/material-spec.md](../../specs/material-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> The native static decal mesh pass reconstructs receiver positions from depth and samples
> object-local X/Z using MeshSize and TextureArea; vertex UVs do not replace that protocol.

## Investigated layered protocol limit

Source: [docs/specs/material-spec.md](../../specs/material-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> The investigated persisted native object protocols do not fully express independent base/world-coordinate sampling and source-mask PBR blending. The two authorized generated programs supply these computations and require Bridge Builder to restore shader/template registration. This permitted startup dependency does not authorize a transform-tracking controller.

## Former representation overview

Source: [docs/specs/material-spec.md](../../specs/material-spec.md).

Recorded excerpt (historical wording; governing requirements remain in the source topic):

> The movable material scheme uses original non-road reuse and approved composition for
> prototype-supported running surfaces. Other sources pass through the common conversion
> and compatibility workflow; output is not necessarily equivalent to the original shader.
> [Complete multilayer adaptation remains unavailable](../../specs/material-spec.md#material-current-multilayer-compatibility-limit)
> under the current game's loading interfaces and mandatory mod-independent persistence.
> Selected-road decal artwork and UVs are preserved through native object materials;
> hashed-alpha coverage is not exact native DBuffer blending.
> Asphalt, sidewalks and wheel-track/rut effects are classified separately per prototype/domain.
> The original source mapping rule applies to non-movable bridges too. Report the complete package size without a hard ceiling. Complete owner material bytes are measured separately
> for information, without a per-owner ceiling.

## Non-Network native VT regression, 2026-11-17

This regression exercises the [native-template conversion scope](../../specs/material-spec.md#non-network-native-material-template-conversion).
The [final receipt](../../../work/landmark-preview-20261117/native-vt-final-receipt.json)
binds the results to DLL SHA-256
`64E34EEA12E2643605607ADCE18400CC4304599695DF50A4F66C33744AB0B0F1`.
The test used actual installed COK bytes, native PipelinePlugin decompression and the production
CPU mip parser. It did not execute native game rendering.

| Installed source | Complete BC7 mip pyramids checked | Recorded output |
| --- | --- | --- |
| Lupu, `151335_2` | 15 | [Source channel hashes](../../../work/landmark-preview-20261117/lupu-vt.log) |
| Nanpu, `162995_1` | 13 | [Source channel hashes](../../../work/landmark-preview-20261117/162995_1-vt.log) |
| GO, `166012_1` | 15 | [Source channel hashes](../../../work/landmark-preview-20261117/166012_1-vt.log) |

All 43 recorded channel checks passed without filtering or recompression. Nanpu's far source
records three active channels, while its near and middle sources each record five; the total
is not evidence of dropped channels. The receipt confirms that the previous GO fifteen-carrier
output hash multiset is unchanged and records 240,475,350 generated carrier bytes for the checked
outputs. These are source-reader/carrier regression results, not complete generated-owner sizes
or proof of rendered appearance. User preview, placement and cold-load acceptance remain pending.
