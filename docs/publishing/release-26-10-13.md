# Bridge Builder 26.10.13

- Fixed bridge creation failures caused by stale dependency records pointing to deleted bridge folders.
- Prevented bridge creation, dependency copying and migration from using another bridge's Prefabs in ImportedData; permitted same-CID sources are tried instead.
- Unified dependency source selection during persistence and validation.
- Added an English contributor guide with developer workflows and troubleshooting prompts, embedded in the README.
