# Lift02 network integration data

The independently measured LiftBridge02 network contains nineteen section entries: six span occurrences with three conditional branches each and one intersection-only median. Every occurrence was checked before emission; this is not an unverified application of another bridge's mapping.

`Lift02NetworkData` requires an explicit role for every selected-road occurrence. Span roles retain ordinary, hidden moving-node and half-length connection branches. IntersectionMedian retains only `Intersection` with `FixedNodeSize` excluded. Source sections and their conditions remain independent through the shared subsection converter. Assigning selected-road roles remains generator work; runtime names, positions and widths are not used to guess them.

The native network has no additional edge/node state rules. Its Bridge segment length is 168.0 and water elevation is 10.0. All other Bridge fields are emitted from this network's audited record.

| Placement | Native object role | Position | Mode |
| --- | --- | --- | --- |
| 0 | LiftBridge02NetPillar | {'x': 0.0, 'y': -2.0, 'z': 2.0} | 19 |
| 1 | LiftBridge02NetPillar | {'x': 0.0, 'y': -2.0, 'z': 0.0} | 18 |
| 2 | LiftBridge02 | {'x': 0.0, 'y': 70.5999984741211, 'z': 0.0} | 16 |
| 3 | Train Outside Connection - Twoway | {'x': 0.0, 'y': 8.0, 'z': 0.0} | 0 |

The caller supplies the generated mechanism and the appropriate pillar placement object, preserving any native placeholder/replacement relationship. Where the native network has an outside-connection slot, the selected network supplies its corresponding object. No extra slot is invented for variants without one. Flags, rotations and placement modes are retained from the audit.

This class has no generator caller yet. It does not complete structure generation, moving road/marking conversion, generator routing, persistence acceptance or native traffic/shipping checks. No existing generated output is affected, so no bridge removal is needed. No visual-generation unit tests or game control are performed.

Authored on isolated branch `bridge/lift-02`. The shared converter is supplied unchanged from dev for compilation and is not included in this style commit. Pre-existing dirty edits remain in the main worktree.
