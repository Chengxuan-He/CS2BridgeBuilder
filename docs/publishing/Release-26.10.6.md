# Bridge Builder 26.10.6

## Changelog

- Store bridge names and metadata in Prefabs, removing the separate bridge registry.
- Persist external dependencies recursively as byte-identical, per-bridge CID copies; migrate healthy legacy bridges without changing their identity or geometry.
- Run self-check after game, mod and asset loading completes, including late mod loading. Recover missing references from already-loaded CID assets; retire bridges that cannot be recovered.
- Back up complete bridge directories recursively, handle name collisions, and include owned dependency copies in cleanup.
- Add a manual bridge self-check and localized success/restart notifications. Successful repairs also display a notification, without duplicate restart text.
- Improve double-deck railway seam handling during renaming and geometry recalculation, including Node Controller compatibility.
- Reduce repeated CID lookups and file reads, add phase timing diagnostics, and remove obsolete code and translations.

## Release scope

Publication to all existing GitHub remote branches and Paradox Mods entry 160320 was explicitly authorized on 2026-10-06. This release includes the accumulated local changes since commit 3be5fed.

Runtime build and targeted non-visual checks passed. Cold-start, old-save and geometry behavior still require in-game validation; publication authorization is not a claim of those results. The legacy synthetic geometry test harness has pre-existing compilation issues and was not executed.
