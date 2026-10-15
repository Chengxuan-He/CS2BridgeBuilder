# Draw03 network integration data

`Draw03NetworkData.g.cs` records the independently measured DrawBridge03 network from game 1.6.2f1. Source audit identity is recorded in the generated source. The emitter compared all 33 section entries as eleven independent three-branch groups before applying those branch rules to selected-road occurrences.

The implementation preserves selected-road sections and their own condition groups through the shared subsection converter. It also applies the recorded Bridge fields, appends native edge/node states to a private selected-road clone, and returns the four measured object placements with caller-supplied private pillar/mechanism and selected-road outside connection.

Draw03 has segment length 152, water elevation 10, and two pillar placements at native Y=-6.550000190734863 with Z=5 and Z=0. These are independent measurements; Draw01/Draw02 offsets are not reused. Every placement rotation, mode and flag is retained.

The source was authored on `bridge/draw-03` in an isolated worktree. The unchanged shared subsection converter is supplied from dev for compilation, without copying another bridge's uncommitted source. Only this new Draw03 source and document belong to the style commit.

This class has no generator caller yet. Structure replacement, selected-road surface/marking conversion, complete routing, persistence and human motion/traffic acceptance remain incomplete. No existing bridge output changes through this uncalled class, so no bridge cleanup is required. No visual-generation unit tests or game control are performed.
