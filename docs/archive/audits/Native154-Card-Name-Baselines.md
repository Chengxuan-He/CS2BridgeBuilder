# Native154 card-name text baselines

## Change

The shared road, bridge-style and saved-bridge card now derives its text area and both margins from the measured outer label width. Each margin is 8% of that width. Explicit pixel widths and margins replace reliance on percentage flex margins; the inner area cannot flex-shrink. Previously the animation trusted the inner element width without establishing its actual left inset. This was a layout vulnerability, not a measured explanation of every Cohtml rendering behavior.

Long names start at the left inner baseline, with the entire left fade band empty. Hover or selection starts motion without an initial hold; the text enters the left fade band continuously. The final glyph aligns with the right inner baseline. Both endpoint holds remain three seconds. Short and exactly fitting names remain centered and stationary.

## Verification

The production JavaScript function passed deterministic frame tests for immediate travel, both endpoint holds, reverse motion, resize, long-to-short transitions and idle cleanup. Explicit inset and right-baseline calculations passed at width scales 0.5, 1, 1.25, 2 and 3, including exact fit and 0.125-pixel overflow. Existing shared hover/selection routing and 12-language UI checks passed. These are nonvisual layout-calculation tests, not an actual Cohtml screen acceptance test. Game appearance remains for manual verification.

Release compilation and the existing build resource guards passed, with two pre-existing nullable warnings in MovableSurfaceAtlas. Frozen UI checks, candidate/source UI hash equality and git diff whitespace checks passed. There are no new or changed localized strings.

## Deployment

Native154 was installed with Cities2 absent. All 964 manifest files were verified; only DLL, PDB, MJS and CSS changed, and all four installed originals were backed up. No saved bridge assets were modified. No game was started, no GitHub push or publication was performed.

DLL SHA256: `29B526BBE695DFAB02732B598B6084F8BB61EF1BA173969798D2EF62D78D9BD5`.

Evidence: `artifacts/material-remap/shared/native154/` contains frozen source, build/UI logs, candidate and deployment receipts. The official SDK ZIP is 77,172,057 bytes; the existing 55 MB publication gate remains unmet. This task did not publish the mod.
