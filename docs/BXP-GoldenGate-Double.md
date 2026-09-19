# BXP double-deck Golden Gate catalogue entry

`GoldenGate` remains the single-deck DLC landmark. `GoldenGateDouble` is a separate
double-only style named 双层悬索桥（金门大桥）, displayed as Mod: BXP (localized prefix).
Only the recorded BXP Golden Gate Bridge Train/Subway donor identities match it;
auxiliary network children remain excluded by the catalogue's lower-deck scan.

Evidence from the existing BridgeBuilder asset-anatomy.txt:

- BXP Golden Gate Bridge Subway begins at line 72118; its AuxiliaryNets references
  BXP Golden Gate Bridge Subway Track, with y=-8.3 and InvertWhen=Never.
- BXP Golden Gate Bridge Train begins at line 84744 and carries its own train auxiliary.
- Both root prefabs have ContentPrerequisite=SanFranciscoSet and
  AssetPackItem=Bridge Asset Pack Filter. BXP is the owner; the DLC is a dependency.
  Availability requires both the subscribed, active non-builtin owning pack and the
  game's prerequisite check. Missing owner metadata fails closed.
- Existing tower-measurements.txt records GoldenGateBridgePylon Placeholder with
  structure width 33 and road width 25, explicitly carried by both BXP donors.
  The separate single-deck end pillar is not added to the double's tower selection.

The existing double-deck composer retains the donor's auxiliary offset and topology;
the upper road remains the width reference. No new geometry transform is introduced.
The shared authored Golden Gate portal keeps its existing railing/material rules.

Localization and catalogue admission/matching checks pass in all 12 locales, and the
Release build succeeds. These checks do not establish visual correctness: inspect
both decks, nodes, towers and LODs in the game after restart. No generated bridge,
save or mod-cache asset is deleted as part of this change.
