# Saved owner material measurement

The runtime `BridgeDependencyPersistence.Save` observes the actual serialized
dependency traversal and writes a report only after saving succeeds. Reports live
at `ModsData/BridgeBuilder/material-footprints/<owner>.tsv` in the game data folder.
They record the generator assembly MVID, material roots, owner-local payloads and
CID sidecars, deduplicated bytes, builtin boundaries and unresolved dependencies.
Unexpanded external VT dependencies explicitly prevent a complete-graph result.
Prepared-directory totals are not a replacement for this persisted graph.

Read existing evidence without changing game files:

```powershell
dotnet run --project tools/MaterialFootprintCheck -- --audit-existing "<game data>" "<current BridgeBuilder.dll>" "<repository output.json>"
```

This mode inventories existing owners and uses PE metadata to obtain the supplied
DLL's MVID without loading the native game. It accepts only a matching runtime
save report whose graph is complete, then checks exact owner paths, current file
sizes, CID sidecars, totals and post-measurement modifications. It records current
file SHA-256 hashes. Its output must be outside the game directory. The audit does
not construct assets, start the game, rewrite owners, infer native dependency edges
or silently substitute an old sample. It rechecks runtime traversal evidence;
it is not an independent native graph parser or visual acceptance test.

The initial read-only result is
`artifacts/prepared-materials/shared-owner-audit/existing-owners.json`:
3,644 ImportedData files, six UUID owner directories, newest file October 24,
2026 at 05:18:41 UTC. No saved-footprint report directory exists. That
October 26 DLL has MVID `b6d3ecb0-5386-4665-a945-8400375d1fed` and SHA-256
`4BEFD3C8E992168AC076D22A42342807C49E0F0BF058960D4169E34B11677C05`.
No existing owner is certified for that build; actual material footprint remains
unmeasured. No user assets were modified during the audit.

After Lift03 source-union integration, the same read-only audit was repeated in
`shared-owner-audit/existing-owners-lift03-union.json` against DLL
`FF92682DB1446259743541D92855BD1F3C8069ADDDE2B70C515DF6B30DB8AD78`,
MVID `57879b87-ba19-412b-8175-0dc8f9e8d793`. The inventory is unchanged and no
current owner closure can be certified.

Running this project without arguments executes synthetic temporary-directory
tests for saved graph deduplication, builtin exclusion, missing sidecars and
unexpanded VT rejection. It also verifies the read-only report checker accepts
a matching complete synthetic report and rejects its missing physical dependency.
These fixtures never count as generated-owner acceptance. No runtime code or
player-facing localized text is changed by the audit command.
