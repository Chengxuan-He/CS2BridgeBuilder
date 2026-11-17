# Bridge Builder 26.11.18

## Status

Published on 2026-11-18 as public Paradox Mods revision **22** of mod **160320**, with user version **26.11.18** and server processing complete. The official SDK downloaded and installed that revision into the native `160320_22` cache; the active playset reports it subscribed and enabled.

All 953 published payload files match the uploaded package by SHA-256. The previous revision 21 was backed up before installation, and all 957 previous cache files remain unchanged. The game was absent during installation.

- Uploaded SDK package: 80,524,543 bytes; SHA-256 `B0E5B39D2E900E1FF2BC6AF18124591FD583F62015DD01ACFCEB480FA306135D`.
- Installed `BridgeBuilder.dll`: SHA-256 `B94F82EAC37375CC2B6C08B116F7582C416A404FA1E0D285EC391E8D1B434C43`.
- Release source commit: `1cb9cadef343dcca2c6450dfdea464647deb304c`, verified on all 18 existing GitHub branches before upload.
- Local receipts: `work/release-26.11.18/final-public/details.json`, `verified/installation.json`, `verified/backup.json` and `candidate-manifest.json`.

Release build, root regression checks, localization checks, two independent Windows harness/filesystem rounds and package exclusion checks passed. Native GPU rendering and cold-load acceptance were not run; AMD and GeForce NOW coverage was unavailable. These are coverage limits, not successful test claims. Test fixture cleanup was blocked by automatic policy; the isolated fixture directories were retained.

## Changelog

- Add GO 224m Viaduct Bridge, Nanpu Bridge and Lupu Bridge styles from their source mods, with dedicated geometry and previews.
- Improve native bridge material conversion, texture mip preservation and UV handling.
- Correct required-content checks, including San Francisco Set availability, and keep styles hidden until all registered sources are available.
- Correct generated bridge names and localized landmark map names while preserving player-entered names.
- Clear native construction-tool references before bridge deletion to prevent stale-prefab failures.
- Preserve native unlock settings consistently when saving newly generated bridges.

The [short changelog](Changelog-26.11.18.txt) is the upload text. The maintained [mod description](mod-description.md) lists optional integrations and source-content requirements.
