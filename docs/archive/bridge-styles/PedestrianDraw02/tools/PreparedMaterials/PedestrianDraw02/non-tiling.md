# PedestrianDraw02 verified non-tiling mapping

## Native mapping evidence

`BuildUvSampling.py` verifies original Surface archive bytes and CIDs for all nine mesh streams. Native template 0 (`Default`, shader 571) selects UV0 with identity scale and zero offset; source descriptors contain no UV overrides. Finite authored source mappings retain their coordinates. Template 2 (`Windows`, shader 550) uses local UV0.YZ, orientation W and COLOR selection: four domains in each mechanism LOD qualify, while six remain unresolved.

Repeat exclusion tests each original domain's source faces against the existing genuine road rectangle. Other faces in the same chart cannot veto source retention. The rectangle, prepared repeat payloads and selected-road mappings are unchanged.

## Authored non-road closures sharing road atlas pixels

`tools/GeometryMetaprogram/PedestrianDraw02ClosureUv.py` reads the original Geometry through the native decoder. It measures six source shell closures in each mechanism LOD: the positive and negative fixed platform end walls, moving-leaf centre end walls, and adjoining underside lips. The 24 faces per LOD form twelve finite half-cap quads. Each quad has four UV corners, consistent orientation, one shared triangle edge and exactly one rectangle's UV area; it traverses its authored atlas section once.

The fixed and moving end walls share two edges with the reviewed top-road boundary. Each underside lip shares two edges with its corresponding end wall. None of the closure faces belongs to the independently measured 276 top-road faces. Exact XYZ-plus-UV triangle correspondence determines LOD1 membership from the full-detail source, without receiver dimensions or observed failure IDs.

`ClosureUvParts.json` records the source Geometry/decoder hashes, exact source faces, XYZ/UV coordinates, native Surface identity, top-road exclusion, adjacency and single-traversal evidence. Sharing pixels with a genuine road repeat does not turn these finite, separately authored non-road closure mappings into tiled roads. Only domains wholly within these verified closure faces bypass the road-region exclusion. No crop is expanded and no source face, material, bone or topology is changed.

## Generated data and verification

The runtime adapter remains `PedestrianDraw02UvSampling.For(render, mesh, domain)`, using a lazy embedded BBN with nine streams and original domain indices. Unknown renders return false; invalid mesh/domain indices for known renders raise internal errors. Schema-3 domain recipes preserve face order.

Current static classification:

- 47,761 retained domains: 47,675 other Default mappings, 78 authored closures, and eight local Windows mappings.
- 216 original top-road domains retain the existing repeat/density classification and are removed from the carried structural mesh by the road-replacement stage.
- Six Windows domains remain unresolved.
- The BBN remains 192,040 bytes; no texture pixels or descriptors are added.

The original closure repair flipped 24 flags and covered the 72 retained observer failures. The additional side-strip review changes exactly 30 more flags (16 near, 14 middle), recorded in `AdditionalClosureUv.json`. These are fixed platform end walls, internal end walls, moving centre end walls and underside lips, not top-road faces. The source Geometry hashes, exact XYZ/UV triangles, downward/vertical orientation and exclusion from the complete road membership are verified by `BuildAdditionalClosures.py`. They were excluded only because their finite source footprint overlaps the old road crop. That crop does not establish repetition on these closures. Other flags, source-union chart data, texture payloads and geometry remain unchanged. This is a source/metadata result; the coherent production replay must verify final uploaded output separately.

Both native-part extraction and BBN regeneration are deterministic. `VerifyClosureUv.py` verifies exact transitions and protection of every remaining top-road overlap; its default baseline is the resource before the additional 30-face repair. `ClosureSampling.Tests.csproj` compiles the actual runtime adapter and checksum reader against the embedded resource; it checks all 47,983 domains, 78 closure memberships, 216 protected top-road domains, six unresolved Windows domains and invalid-index behavior.

The existing standard `GenerateUvCharts.py` invokes classification after final source-union integration. Regenerate the authored closure evidence explicitly from the original research directory and native `PipelinePlugin.dll` using `PedestrianDraw02ClosureUv.py`, then run `BuildAdditionalClosures.py` and `BuildUvSampling.py`. No Unity session is required for source extraction or these checks.

No user-visible text or localization behavior changed. Native rendering, complete persisted-owner material bytes, final SDK package measurement and human near/far, motion and cold-load inspection remain separate acceptance gates.
