# Draw02 network integration data

The DrawBridge02 network record from the installed game 1.6.2f1 is implemented in `Draw02NetworkData.g.cs`. Its audit SHA-256 is recorded in that source. All 27 section entries were compared as nine independent three-branch occurrences before emitting the rules. This does not generalize the mapping to other movable styles.

`ConvertSections` preserves each selected-road occurrence through the shared subsection conversion. `ApplyBridge` reproduces Draw02 segment, water-clearance and construction arrangement fields. `AddMechanismStates` appends the measured edge/node states to a private selected-road clone. `Placements` requires the generated Draw02 pillar and mechanism, plus the selected road outside connection, and retains the four authored placements.

Unlike Draw01, the two Draw02 pillar placements use native Y=-0.9449999928474426, with Z=2.5 and Z=0 respectively. No Draw01 dimensions are substituted. All rotations, placement modes, flags and state-match values come from the Draw02 record.

The component does not yet have a generator caller. Structure generation, selected-road moving appearance/markings, complete route integration, persistence acceptance and human motion/traffic checks remain outstanding. Existing outputs are not changed by this uncalled addition, so no bridge cleanup is required.

Development is isolated on `bridge/draw-02`. The existing shared `MovableSectionConversion` file is supplied unchanged from the dev worktree for branch compilation; it is not a Draw02-owned implementation and is not included in the style commit. No unrelated working-tree edits are moved or committed.
