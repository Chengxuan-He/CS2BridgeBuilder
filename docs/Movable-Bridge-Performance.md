# Movable bridge performance and asset footprint

## CPU affine geometry update (2026-10-21)

The current coordinate path uses `AffineGeometry` for sparse affine parameter rows and
general XYZ matrices. Embedded typed arrays retain their BBN1 layout and measured bits;
this is CPU numerical work, separate from GPU texture composition. Independent movable
LOD plans run after main-thread input snapshots and finish before native asset construction.
Vertex batches contain up to 2,048 items; small batches and nested calls execute serially.
Parallel groups are bounded to at most `max(1, processorCount - 1)` workers.
These are implementation settings, not a requirement to parallelize dependent topology work.

`MovableRenderAssembly` reports `snapshotMs`, `parallelPlanMs`, `levels` and `totalMs`.
Its total ends before `CreateMovableRenderGraph`, so retain separate material, persistence
and preview-render timings. Compare the same style, roads, LODs and cold/warm cache state.
No measured end-to-end acceleration is established by the affine migration alone.

See the [authoritative execution rules](agent-contract/BRIDGE_GENERATION_CONTRACT.md#cpu-affine-geometry-execution)
and [contributor generation step](Adding-Bridge-Styles-With-Coding-Agents.md#affine-matrix-generation-step).
The sections below record the 2026-10-16 optimization and its measurements; their paused-style
status and historical material/paint pipeline are not the current implementation specification.

## Scope and requirement

Implemented on shared branch `dev`; recorded baseline:
`3b6363ada9f1f147b1a660e20c0d6246a54f82dd` (2026-10-16).
Only Draw01 currently calls the changed moving-render path. Other movable generators remain
paused. Existing unrelated working-tree changes are preserved.

Bridge generation contract section 24 now requires the total generated movable bridge Prefab size,
including dependencies, to be comparable to the original movable bridge. Root-file size,
texture subtotal and successful compilation are insufficient acceptance evidence.

## Measured pre-change bottleneck

The successful preview `tmp3515cf11-1842-4a66-a1c6-148a722ff934` recorded:

| Stage | Combined bake/readback/native-save time | Pages |
| --- | ---: | ---: |
| Moving road | 26.610 s | 8 |
| Moving markings | 54.344 s | 1,586 across 5 batches |
| Road LOD1 | 24.704 s | 8 |
| Road LOD2 | 26.476 s | 8 |
| Total of those four stages | 132.134 s | 1,610 |

Final preview rendering took approximately 4.685 s separately. The four timings do not isolate
GPU work from readback, CPU compression or IO. Native `TextureAsset.Save(compress: true)` uses
ZSTD level 11; that flag does not apply GPU block compression. The old marking path saved
three textures for every active receiver-triangle page, producing 4,758 texture saves.

## Implemented changes

| Change | Implementation | Preservation boundary |
| --- | --- | --- |
| Share road appearance across LODs | Per-render-graph cache keyed by identical triangle snapshot, surface references, closed-world matrix, density and page ceiling | All structural LODs, geometry, skeletons and bounds still build separately; no cross-owner resource sharing |
| Consolidate road atlases | Group by actual source surface, reuse one GPU evaluator per group, crop page dimensions to the occupied power-of-two extent | Same 32 texels/m density and chart padding; source material coefficients remain separate |
| Keep source sampling on GPU | Retain private linear GPU texture copies while native source leases are released; compute binds those textures directly | Source VT reconstruction, addressing and linear sampling retained; no production GetPixels/readback/reupload round trip |
| Merge marking receiver pages | Exact coplanar, same-bone generated road faces share 512-pixel projection tiles; GPU evaluates the union of their triangle footprints | No structural geometry classification, plane flattening, bone merging, material-batch merging or structural Z changes |
| Pack output on GPU | Half encoding produces native RGBAHalf bytes directly; normal/error/coverage summaries are reduced on GPU in bounded blocks | Final stored precision remains RGBAHalf; no new rendering shader and no resolution downgrade |
| Fold constant channels | GPU min/max of packed channel values identifies exact constants; persist one 1x1 texture per unique value within the bridge | Nonconstant textures retain resolution, mipmaps and channels; separate bridges retain independent ownership |
| Overlap independent stages | Bounded two-page queues for road and decals; async readback where supported overlaps the next GPU page with current native persistence | Unity and asset database mutation stay on their required thread; fallback readback uses the same GPU output |
| Parallel data packing | Independent channel byte copies use worker tasks | No Unity objects or asset database calls on worker threads; pixel evaluation remains GPU work |
| Add measurement | Texture writer logs texture count, constant reuse, serialized-byte subtotal and upload/mip/save time; debug logs record readback/packing | These counters are explicitly subtotals, not a total-dependency compliance verdict |

A road output pixel now transfers 40 bytes instead of 80; a decal output pixel transfers 24
instead of 80, plus small page summaries. This is a representation calculation, not a measured
end-to-end speedup. GPU-only coverage/diagnostic buffers are not saved. The code removes the
two duplicate road appearance passes in the measured three-LOD case; the old 51.180 s spent
in those two passes must not be presented as a measured new-build speedup.

The compute bundle remains embedded in the manager DLL. Saved bridges reference native
Default, DefaultCutout and DefaultDecal materials, not this compute bundle. No CUDA dependency,
engine shader replacement, animation controller or manager-dependent saved component was added.

## Size measurement and current limit

A read-only traversal of the native main moving render and its LOD resource references found
15 unique geometry/surface/texture/VT payloads totaling **53,798,139 bytes (51.31 MiB)**.
This includes three Geometry payloads and their reachable material/texture payloads. It excludes
the network, fixed supports, mounted objects and other presentation/configuration dependencies:
it is **not the complete original bridge size** and cannot establish compliance on its own.

A broader offline traversal encountered shared catalogue back-links and unresolved serialized
objects. Its aggregate is intentionally not used as a native size budget: including unrelated
catalogue entries would inflate the reference. A complete matching logical dependency traversal
is still required, with unresolved references explicitly accounted for.

`tools/Measure-BridgeFootprint.ps1 -Owner b<uuid>` reports every UUID-owned physical exported
file under ImportedData and the native BridgeBuilder asset directory, including dependency
snapshots and CID sidecars, with extension totals.
It is read-only and does not write files into bridge assets or backup folders. Its physical total
must be supplemented with referenced built-in payloads for the full logical comparison.

At deployment there was no Draw01 permanent bridge entry in export-state.tsv. A transient preview
cannot supply the completed Prefab/dependency export footprint. Consequently, neither the new
end-to-end elapsed time nor the comparable-total-size requirement has passed acceptance yet.
No arbitrary size ratio or unmeasured speedup is claimed.

## Verification and deployment

- Release C# build and strict Unity compute-bundle compilation passed.
- Road, Decal, Clear and Reduce loaded and reported supported with 8x8x1 thread groups on the
  available RTX 5060 / Direct3D11. This verifies resource loading, not other vendors or visuals.
- Final deployment backup: `artifacts/backups/movable-size-measurement-final-20261016-153859`.
  Eighteen original installation files were backed up; twelve installed payload hashes matched.
- Bundle SHA-256: `CBE92B3D527257687F2454BD192F5965D6D163ABBE9D603BDDABF27C43BBF542`.
- No existing Draw01 UUID required cleanup; unaffected bridge styles were preserved.
- No visual-generation unit tests, game automation or synthetic bridge creation were performed.
- Root/runtime instruction routing is below 32 KiB. Existing detailed rules and revision
  records were moved intact into the normative contract to remain below the 32 KiB limit.

## Required human sample

Restart the game, select Small Road and Draw01, allow the preview to complete, then use Create
so the complete persisted graph exists. Retain the generation log and resulting bridge UUID.
Measure preview and permanent-generation stages separately. Compare the full dependency totals
on the same serialization basis, and inspect markings, near/far views and movement. If the
complete exported graph remains disproportionately large, optimization remains open; do not
replace that measurement with the successful build or the native render-only subtotal.


## Material reuse update

See [Native Road Material Reuse](Movable-Road-Material-Reuse.md) for the implemented direct-material and zero-world-blend texture reuse paths, their limits, deployment evidence and pending runtime measurements. Earlier baking timings remain historical measurements, not measurements of this increment.
