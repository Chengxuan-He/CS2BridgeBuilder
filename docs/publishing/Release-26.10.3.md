# Bridge Builder 26.10.3

## English changelog

- Removed automatic network deletion when loading a save. Missing bridges now produce a notification; saving remains unrestricted.
- Improved legacy bridge and null lane-reference recovery. Unresolved dependencies are retained rather than treated as confirmed corruption.
- Added a configurable recovery folder and an Open backup directory button. Retired asset backups retain their original Prefab and CID filenames.
- Startup validation runs once per game session and no longer repeats when returning to the title screen. Updated recovery messages across all 12 supported languages.
- Reorganized bridge generators into shared base logic and separate bridge-style folders for future maintenance.

## Scope

Changes since 26.10.2. Startup file retirement remains restricted to confirmed invalid owned assets with recovery copies. Save loading does not remove placed networks. Existing recovery copies remain at their original location when the configured directory changes.

Compilation and offline checks do not establish universal compatibility or a visual fix. This release does not regenerate bridges already stored in a save.
