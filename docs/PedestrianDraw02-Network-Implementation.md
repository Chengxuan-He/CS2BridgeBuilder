# PedestrianDraw02 network integration data

The independently audited PedestrianDrawBridge02 network has nine section entries in three lateral groups. Every group has the same measured three conditional branches, checked before emission. The selected pathway's own sections and occurrence conditions remain independent through the shared subsection converter.

Native Bridge fields include segment length 144 and water elevation 10. The edge state and node state each set only native requirement 51; road-specific node requirements 24 and 31 are not added. The three object entries consist of two native pedestrian pillar placements at Y=-3 (modes 17 and 18), followed by the generated pedestrian mechanism (mode 16). There is no outside-connection object slot. Rotations, flags and other fields retain the audited values.

The caller supplies the private generated mechanism. Network assembly resolves the original built-in `Pillar Pedestrian Placeholder` through the source-isolated factory resolver and verifies its built-in identity and placeholder component. It does not clone or widen this placeholder. Native `ObjectInitializeSystem` populates `PlaceholderObjectElement` buffers from the reverse references in `SpawnableObject.m_Placeholders`; copying the placeholder alone would lose its replacement candidates. Keeping the built-in reference preserves this relationship without depending on the original bridge prototype Prefab. Native pillar selection and cold-load behavior still require batch in-game acceptance.

The original data was integrated from `bridge/pedestrian-draw-02`. The current `PedestrianDraw02BridgeGenerator` calls this assembly after creating both mechanism LODs, all three column LODs, both footing LODs and selected-network attachments/paint. The catalogue's existing localized style ID now reaches this generator through the common router. Native section evidence records one 8 m pavement section between two zero-width side sections, counted once across conditional branches; both the generator and measurement catalogue now use 8 m rather than the former 27 m entry. Ordinary selected-network approach sections are retained. This integration is not visual or cold-load acceptance, and narrow-road support and selected-road furniture at motion gaps remain to be verified.

## Early width validation

The generator now calls the emitted base-width validator before source render
reconstruction, native road sampling or material allocation. Structural assembly uses
the same validator, so there is one measured limit rather than two independent constants.
The existing half-width limit remains -0.03180122375488281 m; this change moves the
failure earlier and does not claim support for narrower roads.

`PedestrianDraw02NarrowingReview.py` checks the exact 24 selected base/road bodies at
both detailed LODs, excluding removed road-top faces before checking normal reversal.
It records actual retained source-face IDs that invert at negative deltas. Thus deleting
the guard or considering only the replacement top surface would not repair narrowing.
These findings leave narrow-road geometry as explicit unfinished work. Existing valid
widths, saved assets, persistence and localization behavior remain unchanged; failures
use the existing operation failure path.

