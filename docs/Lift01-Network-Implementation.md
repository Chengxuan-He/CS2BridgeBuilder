# Lift01 network integration data

`Lift01NetworkData.g.cs` preserves the native LiftBridge01 network's independently audited section rules. Its eleven entries comprise two side occurrences with one branch each and three deck occurrences with three branches each. The original native layout uses train side, shoulder and track sections; that is evidence for these roles, not a requirement to replace the selected road with train sections.

| Role | All | None | Hidden layers | Half length |
| --- | --- | --- | --- | --- |
| Side | Node | FixedNodeSize | 0 | false |
| Deck ordinary node | Node | FixedNodeSize | 0 | false |
| Deck moving node | Node, FixedNodeSize | empty | -1 | false |
| Deck edge | empty | Node | Bottom (2) | false |

The caller must supply an explicit role for every selected-road occurrence. Source requirements, inversion, median and placement are retained by the shared subsection converter. Role assignment and supported selected-road layouts still require generator integration; the data class does not infer a role from a section name, position or width.

No extra edge/node states exist on the audited native network, so no Draw01/02/03 state list is appended. Bridge segment length is 168 and water elevation is 10. Native pillar placements are Y=-2.75 with Z=13.5 and Z=0. The mechanism placement is Y=77.87999725341797, and the outside connection is Y=8. All other placement fields are retained. The caller supplies the generated pillar/mechanism and the selected network's appropriate outside connection.

The change is authored on isolated branch `bridge/lift-01`. The unchanged shared section converter is supplied from dev for branch compilation. No unrelated dirty bridge files are carried into this worktree or included in the style commit.

This is measured network data and an explicit conversion entry point, not a completed generator. Structure generation, selected-road visible motion and marking conversion, route integration, cold-start persistence and human traffic/shipping acceptance remain incomplete. No caller exists yet; existing bridge output is unaffected and no bridge cleanup is required. No game control or visual-generation unit tests are performed.
