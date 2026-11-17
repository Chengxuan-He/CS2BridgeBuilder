# Development and release procedures

These procedures implement the [development contract](../contracts/development-contract.md).
Relocation preserves mandatory order and all existing prerequisites; it introduces no separate policy.

<a id="repository-release-workflow"></a>

## Release workflow

The release version is specified by the user. Never invent or automatically increment it.
When the user requests a release, complete these four steps in order:

1. Remove redundant and dead code, then build and run the applicable checks using the user's version,
   including one round under the [cross-environment testing contract](../contracts/reading-map.md#cross-environment-testing-contract).
   Do not run this suite on every game startup. Assign one agent per concrete environment; report
   actual results and complete all locally testable cases. Unavailable external coverage does not block GitHub upload or publication or require separate approval; follow the [local coverage rule](../specs/cross-environment-spec.md#local-coverage-and-delivery). Diagnose actual product failures separately.
2. Commit and upload the release code to every existing GitHub branch; verify the remote results.
3. Upload that version to Paradox Mods with a short changelog. Verify that the corresponding version
   is available; an accepted upload that is still processing is not a completed release.
4. Download the corresponding published version from Paradox Mods and replace the local Bridge Builder
   installation with it. Back up the previous installation and verify the installed files against the
   downloaded package. A local build is not a substitute for the Paradox Mods package in this step.

<a id="repository-workflow"></a>

## Workflow

1. Inspect the branch, preserve unrelated changes and read all applicable instructions.
2. Inspect the real archetype and generated output before transformation changes. Missing samples
   leave visual acceptance pending; continue independent implementation without claiming a fix.
3. Work on a suitable branch using offline identification and immutable measured construction data.
   Apply each visual change consistently to the full-detail mesh and every LOD. Follow the
   [generation material branch](../specs/generation-spec.md#generation-core-actual-sequence-and-branches): custom materials only for movable-bridge road and rail running surfaces; otherwise retain prototype materials, subject to the necessary [non-Network native-template exception](../specs/material-spec.md#non-network-native-material-template-conversion).
4. Build/deploy coherent code increments under [bridge generation contract section 13](../contracts/development-contract.md#generation-13-deploy-coherent-increments-and-preserve-healthy-assets), backing up
   installed originals and verifying hashes. Preserve healthy assets; scope recovery to demonstrated
   incompatibilities. Documentation-only work requires no build, shutdown or deployment.
5. Report automated evidence separately from human near/far, motion and cold-load acceptance.

<a id="generation-current-repository-checklist"></a>

## Current repository checklist

1. Extract and hardcode construction data from the correct real prototype; generation must not
   require another player to possess the developer's original prototype files.
2. Supply the necessary resource graph through permitted sources and independent persistence.
3. Generate the selected road, native motion, attachments and all LODs with correct part transforms.
4. Preserve native attributes, [material contract section 2](../specs/material-spec.md#material-2-preserve-source-resolution-and-pattern-scale)'s mapping-specific density and original road bones; do not cut receivers for paint. Check the [custom-material boundary](../specs/material-spec.md#custom-materials-are-limited-to-movable-bridge-roads): only movable-bridge road and rail running-surface materials may be customized, other prototype materials are retained or use the scoped non-Network native-template conversion with source appearance preserved, native dependency snapshots are complete, and owners contain no shader copies.
5. Integrate routing, UI and all supported languages; preserve fees, unlocks and identities.
6. Build/deploy coherent increments and scope any necessary recovery to affected owned assets.
7. Verify actual geometry, saved references and native behavior; label pending visual/cold-load checks.

