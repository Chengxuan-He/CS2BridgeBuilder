# Lift03 main and auxiliary network integration data

`Lift03NetworkData.g.cs` implements separate measured schemas for LiftBridge03 and LiftBridge03 Train Track. The latter remains the auxiliary network of one style, not a separate style. The source audit hash is recorded in the generated source.

The main network has 23 entries: two side occurrences with one ordinary-node branch each, and seven deck occurrences with ordinary-node, hidden moving-node and Bottom-hidden edge branches. These main edge branches are not half-length.

The auxiliary network has 21 entries. Side occurrences have only the ordinary-node branch. Each track/shoulder occurrence has four branches: ordinary node, hidden moving node, ordinary edge excluding FixedNodeSize, and a half-length FixedNodeSize edge. Both edge branches hide Bottom. Its median instead has three branches and retains full-length edges. The source's two permutations of the same exclusion list were compared as sets before using the common track/shoulder rule; no condition was removed.

Callers must supply one explicit role per selected-road occurrence. This class preserves selected sections and their own condition groups through the shared converter; it does not guess roles from names, widths or geometry.

Both native Bridge segment lengths are 200. Main water elevation is 20; auxiliary water elevation is 10. Only the main network appends measured state 51 on edges and nodes. Main placements retain the generated pillar at Z=5 and Z=0, the generated mechanism at the origin, and the selected main outside connection at Y=5. The auxiliary places only its selected outside connection at Y=8; it does not duplicate the main mechanism or pillar.

`ApplyAuxiliary` connects the generated lower network at native position (0,-10,0), preserves `m_LinkEndOffsets=true`, and records native inversion mode 0. This is the native arrangement, not an adjustable separation or a substitute prototype track. Selected direction controls still need integration with the existing request/generation path.

The change is authored and compiled on isolated branch `bridge/lift-03`, using the unchanged dev shared converter as a build dependency. Only the new style source and this document enter the style commit. Existing dirty worktree edits are retained separately.

No generator calls this class yet. Role assignment, both selected-road surfaces, structure/markings, skeleton integration, native signals, routing and cold-start acceptance remain unfinished. No existing bridge output is affected and no bridge cleanup is required. No game control or visual-generation unit tests are performed.
