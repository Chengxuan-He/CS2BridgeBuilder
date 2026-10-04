# CS2ModShared source snapshot

Captured from the local sibling `CS2ModShared/src` on 2026-10-04. That directory had no Git metadata,
so no upstream commit is claimed. The 21 C# files are preserved byte-for-byte; `sources.lock.json`
records their relative paths and SHA-256 hashes. This is shared project source, not bundled game assets.

Build.ps1, BridgeBuilder.csproj and applicable regression tools compile this snapshot. Changes to the
sibling directory cannot silently change a BridgeBuilder build. Keep these files in version control.

To update: review the upstream diff, copy only the intended C# changes, regenerate the sorted manifest
(path relative to this directory, slash separators, sha256), then run CheckSharedSources.ps1, the
non-visual regression checks and Build.ps1. Review source and manifest together. Do not merely refresh
the hashes to suppress an unexpected build failure. This pins shared source only; installed game
assemblies and the SDK remain separately required.