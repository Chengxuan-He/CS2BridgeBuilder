# Lift04 network integration data

The independently measured LiftBridge04 network contains nineteen section entries: six span occurrences with three conditional branches each and one intersection-only median. Every occurrence was checked before emission; this is not an unverified application of another bridge's mapping.

`Lift04NetworkData` requires an explicit role for every selected-road occurrence. Span roles retain ordinary, hidden moving-node and half-length connection branches. IntersectionMedian retains only `Intersection` with `FixedNodeSize` excluded. Source sections and their conditions remain independent through the shared subsection converter. Assigning selected-road roles remains generator work; runtime names, positions and widths are not used to guess them.

The native network has no additional edge/node state rules. Its Bridge segment length is 168.0 and water elevation is 10.0. All other Bridge fields are emitted from this network's audited record.

| Placement | Native object role | Position | Mode |
| --- | --- | --- | --- |
| 0 | Subway Pillar Placeholder | {'x': 0.0, 'y': -3.0, 'z': 0.0} | 17 |
| 1 | Subway Pillar Placeholder | {'x': 0.0, 'y': -3.0, 'z': 0.0} | 18 |
| 2 | LiftBridge04 | {'x': 0.0, 'y': 68.75, 'z': 0.0} | 16 |

The caller supplies the generated mechanism and the appropriate pillar placement object, preserving any native placeholder/replacement relationship. Where the native network has an outside-connection slot, the selected network supplies its corresponding object. No extra slot is invented for variants without one. Flags, rotations and placement modes are retained from the audit.

This class has no generator caller yet. It does not complete structure generation, moving road/marking conversion, generator routing, persistence acceptance or native traffic/shipping checks. No existing generated output is affected, so no bridge removal is needed. No visual-generation unit tests or game control are performed.

Authored on isolated branch `bridge/lift-04`. The shared converter is supplied unchanged from dev for compilation and is not included in this style commit. Pre-existing dirty edits remain in the main worktree.

## Network assembly (2026-10-20)

Lift04NetworkAssembly now consumes the recorded section conversion and placements.
It requires a mechanism owned by the current factory, adopts generated section
assets, applies the measured Bridge fields and preserves selected-road ground and
outside-connection placements while replacing elevated supports.

The two support occurrences use the original built-in **Subway Pillar Placeholder**,
not the ordinary Pillar Placeholder used by some other styles. Its native identity
is retained for spawnable-pillar reverse references. The placement remains at Y=-3
in modes 17 and 18; the mechanism retains its separately recorded Y=68.75 placement.
No extra edge or node state rule is introduced.

Lift04SectionRoles selects the recorded one-branch exception only for median
occurrences that explicitly require Intersection (RequireAll or sole RequireAny).
Other selected-road occurrences receive the three native span branches. Original
section conditions still pass through the shared converter. Missing source sections
fail explicitly; names and widths are not used to infer roles.

Release compilation and twelve-language checks pass. The complete Lift04 generator
is not routed yet; track replacement and compatibility work remain. No game,
persistence or human acceptance claim is made by this assembly increment.

## Incremental generator route and local deployment (2026-10-20)

Lift04BridgeGenerator now connects reconstructed sources, selected-road sampling,
road appearance, all-LOD structure and glass, track replacement, supports, native
attachments, skinned paint, network conversion and recorded unlocks. The existing
localized Lift04 catalogue entry reaches this route for preview and creation.
There are nine routed movable styles; Lift03 remains unrouted. Routing does not
establish native operation or human acceptance.

The measured elevated width is **10 m**, replacing the old 42 m table entry that
counted conditional alternatives. Native network 118829 uses two Subway Track
Section 4 occurrences (piece 120985: 4 m) and two Subway Shoulder 2 occurrences,
whose Elevated branch selects Subway Shoulder Piece 1 (120981: 1 m). The outward
Subway Elevated Side Piece 0.5 is excluded from the road envelope. The structural
road bed occupies X=-4..4 and the retained shoulders extend to X=-5..5.
The native aggregate is Bridge, and the original icon is LiftBridge04.svg.

The catalogue now records Lift04 directly against the built-in BridgesAndPorts
content prerequisite. Review also found that previously routed Lift02, Lift05 and
PedestrianDraw01 were still missing their recorded catalogue registrations; those
entries are now present, removing the original network Prefab prerequisite. Their
existing measured dimensions, localized style names and generators are retained.

Release compilation, 29 exact-route checks, 82 recorded-variant checks and all
12 UI languages pass. The last composed structure check passed twelve width/LOD
cases. These checks do not run native generation or cold loading. Selected-sidewalk
compatibility, unrestricted narrowing, full resource-size measurement, persistence,
traffic/shipping and native animation remain unfinished. Human acceptance stays
at the batch stage requested by the user.

Cities2 was confirmed absent before local deployment. Twelve managed/UI/dependency
outputs were backed up and copied into the existing active installation
`.cache/Mods/pdx_mods/160320_17`; all twelve installed hashes match Release output.
Original files are in `artifacts/backups/20261020-092804-lift04-installed`.
DLL SHA-256: B48D9ADD372A423B0B1342907DFBFF68B2A7ADCDFE827D21835C539DD9E4A3E8.
PDB SHA-256: E6713425BC279E1E541060ED4E6967B2F8FA66B4E2DE1575A9014DEBFB6B326F.
No healthy generated assets were removed and no game UI automation was used.
This is a development deployment, not a Paradox Mods release.
