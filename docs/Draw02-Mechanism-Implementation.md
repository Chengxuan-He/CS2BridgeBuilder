# Draw02 native mechanism configuration

## Implemented configuration

`Draw02MechanismData.g.cs` reproduces the measured DrawBridge02 object component data,
three object mesh placements, authored attachments and procedural skeletons for all three
main-render LODs. The caller supplies private prepared render graphs and resolved native
attachment resources. `Meshes` and `ApplyComponents` are inputs to the existing
`TowerFactory.CreateMovableObject` assembly path; no parallel asset writer is introduced.

The native object has a 90 m node offset and a 16 second moving time. The positive-z leaf
uses connection 101 and the negative-z leaf uses connection 100. Their hinge coordinates
are z = +73.9800033569336 and -73.9800033569336, respectively, at y = 5.414000034332275.
These are distinct from the node offset. Bone hierarchy, complete bind matrices, signed
speeds, accelerations, connection/source IDs and native animation types are emitted directly
from each LOD record, without inferred mirroring or shared Draw01 constants.

## Provenance

Source: game 1.6.2f1 native records retained in `all-movable-audit.json`, SHA-256
`47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070`.
Object record 118748 references main render 79604; its LodProperties explicitly references
79605 and 79606. Resource IDs are extraction evidence only and are not runtime lookups.
The offline emitter asserts every native field represented by a fixed zero/false value.

## Integration and acceptance

The configuration compiles on `bridge/draw-02`. It has no generator caller yet and changes
no existing generated bridge output. Authored ordinary streetlight placements are evidence,
not permission to substitute them for selected-road lighting. The final caller must supply
selected-road attachments and transformed bridge-specific attachments.

Structural widening/removal plans, selected-road moving geometry, marking representation,
full generator routing and in-game acceptance remain required. This configuration alone
does not constitute a completed Draw02 generator or establish persistence/motion acceptance.
