# Bridge Builder 26.10.7a

- Fixed bridge creation failures caused by generated components and runtime dependencies missing the bridge UUID.
- Added legacy component migration: create UUID-named copies and update references before retiring unreferenced originals.
- Removed artificial asset-name truncation to preserve full names and UUIDs.
- Bridge creation failures now produce CRITICAL diagnostics with the underlying failure details.

If bridge self-check makes changes, restart the game for them to take effect.