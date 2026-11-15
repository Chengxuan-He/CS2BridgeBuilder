# Truss Viaduct measured construction

The canonical source is `BXP Truss Viaduct - Train` from the installed Bridge Expansion Pack 92245_36. Road and rail variants share the style. The train variant supplies a centered zero-width native carrier, avoiding a dependency on a selected road's number of drive sections.

## Recorded graph

The carrier is the original train median bottom piece (26 vertices, 93 indices), with its original material and asymmetric lengthwise realization. It carries a seven-mesh structural object at 14 m intervals and at matching intersection/dead-end nodes. Its two native object conditions and curve-rotation behavior are retained. A separate node pier is positioned at Y=-2.5 m; its first/middle/last stacking arrangement, original bounds and native terraforming flags are retained.

The structural support and native middle pier use the exact TrussArchBridge01 pillar geometry. The existing three-LOD measured membership tables are reused offline only after matching the actual geometry identities. This is reuse of identical source geometry, not borrowing another bridge's dimensions. Side columns translate rigidly. Recorded transverse memberships use their existing own-span coefficients. The separate footing translates by side. The six thin underside panels retain native artwork, original quaternion occurrences and their own-span width transform.

Runtime creates private geometry and explicitly owned LODs from original native render resources. It does not read the original bridge network, sections or object Prefab layouts to infer construction. Native render-resource names remain required inputs; this is distinct from independence from the original network authoring Prefab.

## Reproduction and evidence

`tools/GeometryMetaprogram/TrussViaductMaps.py` writes checksummed, uncompressed BBN1 displacement and frame tables. Its decoded source geometry and original Odin tokens are retained under `artifacts/four-new-styles/TrussViaduct/`. Source geometry decoding uses the repository GeometryMetaprogram and the installed native PipelinePlugin. Native source meshes have vertex counts 4332, 3065, 491, 294, two submeshes with 480/20 vertices, and a 26-vertex carrier.

The original resource graph supplies all structural materials. No new textures or material descriptors are authored. Selected-road materials, lane configuration and sidewalk behavior remain in the selected elevated network. The generated carrier is additional below-deck structure, not a replacement road layout.

## Acceptance limits

Compilation, table integrity and source-graph inspection do not establish native composition or visual acceptance. Human near/far rendering, curved placement, different road widths and mod-enabled cold loading remain pending. Check that the zero-width overhead carrier retains native curve and node object placement, that no repeated objects are missing, and that all three native pier levels transition correctly.

The measured table check passed for all twelve BBN1 resources: exact checksums and lengths, finite displacement/frame values, bit-identical zero-delta X positions, and finite outputs for half-width deltas -4, 8 and 20 m. This does not validate structural appearance at those widths. Reproduce with `tools/GeometryMetaprogram/TrussViaductCheck.py`; the report is `artifacts/four-new-styles/TrussViaduct/table-check.json`.
