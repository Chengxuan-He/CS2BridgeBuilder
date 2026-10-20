# PedestrianDraw01 network integration data

The independently audited PedestrianDrawBridge01 network has nine section entries in three lateral groups. Every group has the same measured three conditional branches, checked before emission. The selected pathway's own sections and occurrence conditions remain independent through the shared subsection converter.

Native Bridge fields include segment length 144 and water elevation 10. The edge state and node state each set only native requirement 51; road-specific node requirements 24 and 31 are not added. The three object entries consist of two native pedestrian pillar placements at Y=-3 (modes 17 and 18), followed by the generated pedestrian mechanism (mode 16). There is no outside-connection object slot. Rotations, flags and other fields retain the audited values.

The caller supplies the appropriate native pedestrian pillar placement object and private generated mechanism. The original pillar placeholder's native replacement relationship must be preserved by the completed generator; this data class does not create or widen that replacement.

The implementation is isolated on `bridge/pedestrian-draw-01`. It has no generator caller yet and does not complete structure, surface, markings, persistence or native operation acceptance. Existing output is unaffected. No game control or visual-generation unit tests are used.

## Generator integration (2026-10-20)

PedestrianDraw01 is now routed through its own MovableBridgeGeneratorBase
implementation. It reconstructs native render inputs from recorded data, samples
the selected road, prepares three road levels and assembles the original mechanism,
three-level columns/footings, authored fixed vent and selected-road furniture/paint.
The ordinary built-in pedestrian pillar placeholder retains native replacement
relationships. No original PedestrianDrawBridge01 Prefab lookup or manager-driven
runtime synchronization is introduced.

Root 118833 has three conditional branches of one Pavement Path Section 8 between
two zero-width side sections. Its native width is therefore 8m, not the former
27m catalogue value. BridgeMeasurements and generator input now agree. Existing
UI catalogue/preview actions and all twelve localized names already include this
style; its stable ID now reaches an implementation instead of a missing route.

The far moving road inner boundary has 40 explicit measured height corrections
from 10.10009765625 to 10m; eight of these also align their central endpoint from
+/-0.06464385986328125 to +/-0.029998779296875. Fixed sheets, outer curbs and
underside endpoints are retained. Rebuilding the structural Plan preserves the
frame's edge/interior recipes, fragments, X positions and native bone attributes.

Release build, 28-route architecture validation, twelve-language checks and twelve
composed-plan cases pass. Original installed DLL/PDB were backed up with the game
absent; replacement hashes match the built files. No saved bridge was removed.
There are now eight routed movable styles, not eight accepted styles. Selected
sidewalk-specific adaptation, unrestricted narrowing, final persisted UV density,
native generation/animation and cold-load persistence remain unfinished or
unverified. Human acceptance remains deferred to the completed batch.
