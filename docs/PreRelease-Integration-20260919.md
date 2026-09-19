# Accepted bridge fixes: pre-release integration

The user accepted TrussArch01 and the revised CoveredWood centre uprights in game,
then requested their integration into pre-release, installation, and an MIT license.

- Accepted dev fixes: `3313fea`.
- MIT license commit: `85259d3`; copyright attributed to BridgeBuilder contributors.
- Integration target before merge: `b0fbec59052cb0b24c410bd0efa84d9bdc4936de`.
- The only merge conflicts were comments in TowerFactory. Existing pre-release UI,
  rendering and other functionality were retained.
- TrussArch01Geometry, CoveredWoodGeometry and CoveredWoodColumnData match the
  accepted dev versions. TrussArch01 width-plan logic already existed on pre-release.
- CoveredWood uses the measured pre-fix column-top bounds, -0.80877763 to
  +0.80877763 m, throughout each centre upright; full and LOD1 share the mapping.
  The archetype's LOD2 has no centre uprights and remains unchanged.
- `tools/Build.ps1` succeeded. No visual unit tests were run, and the game was not
  opened or controlled for verification. User acceptance applies to the dev models;
  this merge preserves their geometry code but is not a new in-game validation.
- Built DLL SHA256:
  `E363776DE152506411CFBD64D66A3C10CFEA56648D70B4B9106EF6C61029D805`.

The mandatory Cities2.exe kill command was invoked and process absence verified.
Before cleanup, generated data and the installed dev mod were backed up to:

`C:/Users/admin/Documents/Codex/2026-09-01/qi/outputs/pre-release-accepted-bridges-20260919`

Cleanup removed 30 owned ImportedData directories, 6 generated geometry-related
files and 1 export-state file, preserving 9 road-export dependency directories.
A second cleanup found no remaining targets. Remaining Prefab files did not
reference the removed geometry CIDs. Mod caches and saves were not modified.

The MIT license covers this project's original code; game assets and third-party
dependencies retain their respective licenses. No pricing, unlock or unrelated
generation algorithm was changed as part of this integration.
