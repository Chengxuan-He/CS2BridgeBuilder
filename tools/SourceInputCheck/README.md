# Source input ownership checks

`Source131Check.Run` exercises the compiled production Prefab-presence checks with
actual native non-built-in PrefabAsset handles under another UUID's Prefabs,
Dependencies, legacy dependency folder, and an external-mod folder. It covers
network import, section objects and lighting; null still fails. Run in an isolated
Unity Editor project with `-gameManaged`, `-modAssembly`, and a fresh `-outputRoot`.
No game or player assets are used. This is not complete native network generation.

Run `dotnet run --project tools/DependencySourceCheck` for source-linked real-file
snapshot traversal, all eight supported source asset kinds, source removal in a
temporary fixture, independent target bytes/CIDs, repeat calls and failure cases.
The native database is doubled in that suite; global enumeration is forbidden.
`tools/LegacyNameCheck` verifies that other-owner inputs are never retired and that
already UUID-named inputs are not pre-UUID naming-migration candidates.
