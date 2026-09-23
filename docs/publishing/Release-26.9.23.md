# Bridge Builder 26.9.23 release candidate

Repacked after the September 23 double-deck repairs:

- Gray double suspension sizes its towers and cables from the upper road rather than the lower root network.
- A-pylon double-deck cable assemblies retain their native x=0 attachment axis at full detail and both LODs.

This candidate supersedes the earlier same-date package. These two repairs were compiled and deployed;
their final in-game near/far acceptance has not been explicitly recorded. Earlier acceptance below
does not cover these later changes. Packaging does not publish to Paradox Mods.

The user accepted all bridge styles on 2026-09-23, following deployment of 26.9.22.
This release preserves that geometry and adds an explicit catalogue-loading state to the road,
bridge-style and created-bridge lists. A spinner is displayed until catalogue publication finishes;
empty-state messages are reserved for completed loads with no results.

Validation: compile the mod, run `node --check src/BridgeBuilder/UI/BridgeBuilder.mjs` and
`node tools/CheckCatalogLoading.mjs`, deploy and compare installed payload hashes. The new spinner
still requires an in-game UI check; prior bridge acceptance is not a claim about this new UI state.

Prepare a fresh candidate with `tools/StageRelease.ps1 -Destination <new-directory>` and run the
official ModPostProcessor on an isolated copy. Use only the resulting `publish-content` directory
for eventual upload. Keep the processing log and hashes alongside it. No upload is authorized by
packaging this candidate. Review the game version, screenshots and remaining metadata before release.

GitHub branches receive the accepted release tree while retaining their existing commit ancestry.
Local backup/rollback branches and uncommitted work in other worktrees are not release targets.
