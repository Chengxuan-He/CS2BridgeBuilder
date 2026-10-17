# Movable Preview Framing and Material Follow-up

Current appearance selection: [Existing Material Reuse](Movable-Road-Material-Reuse.md). Earlier
baking/descriptor-generation descriptions below are historical and superseded by this revision.

## Preview correction implemented

The previous bounds calculation applied every skin matrix to the entire mechanism bounding box. This included impossible positions from vertices that actually belong to other bones. It expanded and displaced the camera frame.

The preview now reads the native UInt32 bone index of each indexed vertex, applies that vertex's skin matrix on the GPU, and reduces both local and camera-space bounds. Removed/unreferenced vertices do not contribute. The orthographic camera frames the union in its fixed isometric basis, including approaches and mounted objects. The generated bridge geometry, original assets and simulation animation are unchanged.

The GPU bundle compiled successfully. Release build and deployment succeeded; 12 installed files matched the build output. The 18 previous installation files were backed up under `artifacts/backups/preview-posed-centering-20261016-233717`. This increment changes preview framing only and removes no bridges. In-game visual confirmation remains pending.

## Actual material-reuse result

The subsequent human-created Small Road Oneway / Draw01 sample is `bb2da17ce-8408-4a6e-8ded-561b9f42c202`.
The log at 23:32:32.901 records zero reused materials, zero reused original texture sets, two baked pages and 6157 ms in the appearance stage. The preceding preview recorded 6982 ms. Thus the previously deployed zero-mask optimization does not solve this sample's appearance cost.

The user's current requirement is full existing-material reuse, choosing the source road or native bridge as appropriate. This supersedes treating new descriptors with borrowed textures as full material reuse.

The intended source assignment is:

| Component | Existing material source |
| --- | --- |
| Ordinary stationary approaches | Selected road |
| Authored bridge structure and its LODs | Corresponding native bridge |
| Generated moving deck and sidewalk surfaces incompatible with network shaders | Native bridge object materials, with explicit authored UV correspondence |
| Markings | Existing compatible native marking material; borrowing textures into a new descriptor alone does not satisfy full material reuse |

The final two assignments are not implemented by this increment. Do not claim that all materials are reused or that baking has been removed. The native bridge surface is an atlas with alpha-cutout regions: blindly assigning it to the selected road's existing UVs can create unintended holes. Full reuse requires resolving both the source material and its UV/frame correspondence, including normal-map tangent orientation and near/far material differences.

## Sidewalk inspection

The actual saved Small Road / Draw01 sample `b51e5ca7a-af67-4cda-a652-b4dfe8627ecb` remains the geometric evidence source. On its highest-detail generated road, each sidewalk has 84 upward-facing horizontal triangles, zero downward-facing horizontal triangles, and approximately 557.1975 square metres of top coverage. This does not indicate one-sided inverted winding.

A direct triangle-intersection comparison of native retained structure and generated road at the sidewalk height found zero coplanar overlap area in the inspected three LODs. The original bridge uses an alpha-cutout metal grating; that fact alone does not establish that the generated sidewalk is correct. The reported appearance remains unresolved. No speculative depth offset, replacement shader or unverified UV remapping was introduced.
