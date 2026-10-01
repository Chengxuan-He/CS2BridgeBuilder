# Bridge Builder 26.10.2

## English changelog

- Fixed the CRITICAL error: "System update error during PrefabUpdate->NetInitializeSystem: ArgumentNullException: Value cannot be null."
- Added startup recovery for legacy bridge files and independent unlock rules, preserving progression requirements without direct external bridge-prototype references.
- Improved missing-bridge cleanup and shared-junction protection. Cleanup attempts run once per save load; manual saving and autosaving remain unrestricted.
- Fixed long-path handling so file-access failures are not mistaken for damaged assets, and localized recovery notifications.
- Distinguished Bridge Expansion Pack base and Bridges & Ports editions; bridge styles are hidden when their required content is unavailable.

## Release scope and acceptance

Changes since source release 26.9.30 (`42299ab76ebcd081d41ccaf3ba09c3dd0f044ffe`).
The user confirmed the current crash issue resolved and authorized GitHub branch
synchronization and Paradox Mods publication on 2026-10-02. This does not establish
a universal fix for unrelated native/HDRP crashes. No new geometry changes are
included in this release preparation.

Startup migration preserves bridge identity and progression rules. It attempts
recovery before retiring confirmed invalid owned prefab files; access failures
are not permission to delete. Save-load cleanup remains ownership-scoped and
stops on unsafe shared junctions without retries or save interception. Restore
missing dependencies and reload if cleanup is blocked; retain the original save.
