# Bridge model render-graph audit

## Scope and provenance

This record preserves the movable-mechanism render observations previously embedded in
[Bridge model design](../../specs/movable-spec.md#movable-8-skeletons-animation-and-attachments). The original passage
identifies highest-detail primary render prefabs but does not give an inspection date, game build,
source hashes or a per-prefab inventory. Those provenance fields remain unspecified; this relocation
performs no new code, geometry or in-game inspection and does not broaden the original coverage.

## Recorded primary render and animation observations

In the audited highest-detail primary render prefabs, each mechanism has one principal opaque
mesh with one submesh, plus a separate window mesh. This does not establish which triangles are
road, structure or markings. Removing the opaque submesh would remove structure as well; a
reviewed triangle-level separation must identify the prototype road contribution before replacing
it with the selected road. Repeat the correspondence review for every LOD.

These primary meshes use `BlendIndices:UInt32x1`. Native procedural bone rest transforms, bind
poses, hierarchy and motion fields live in `ProceduralAnimationProperties`. This must be traced
together with the geometry's skinning channels. An empty `ModelImporter.Model.BoneInfo` array in
an existing export helper alone does not prove that the saved native animation graph lacks bones.
Likewise, a valid skinning layout does not establish material compatibility for a baked network.

## Interpretation and limits

The recorded observations motivate triangle-level source separation and tracing the complete native
animation graph. They do not establish all-LOD coverage, material compatibility, motion acceptance
or cold-load success. Current implementation requirements remain in the
[movable specification](../../specs/movable-spec.md) and
[movable contract](../../contracts/movable-contract.md).
