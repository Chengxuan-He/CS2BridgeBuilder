# PedestrianDraw01 native mechanism configuration

## Implemented data

The style-specific mechanism class records the native object components, three mesh
placements, authored child placements and complete procedural bone arrays for each
main-render LOD. Bind matrices, transforms, speeds, accelerations, connection IDs and
source IDs are emitted from each LOD independently. No inferred mirroring or cross-style
mechanism constants are used. Native empty attachment configurations remain empty.

Source evidence: game 1.6.2f1, all-movable-audit.json, SHA-256
47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070.
LOD references are followed from the native LodProperties component. Path IDs are offline
provenance only. The emitter asserts fields represented by fixed zero/false values.

## Assembly responsibilities and status

The caller supplies private prepared render graphs to Meshes and native content to
ApplyComponents. These feed the existing owned-object assembly path; there is no separate
asset writer. AuthoredAttachments retains the native placements as evidence. Ordinary road
attachments must be replaced by selected-road placements; bridge-specific attachments must
follow their measured transformed parents.

This data has no generator caller yet. It neither publishes a bridge nor changes existing
bridge output. Structure widening/removal, selected-road moving surfaces, markings, generator
routing and human motion/persistence acceptance remain required. Compilation is not acceptance.