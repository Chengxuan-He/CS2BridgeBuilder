# Dependency persistence and self-check implementation

## Dependency traversal

Creation and post-load self-check both pass an explicit `BridgeCreationDependencies`
context to the saver. Serialized CIDs form the forward graph. Material producers add
Surface-to-MidMips edges, including known-empty membership. Prepared materials carry
offline edges; selected native surfaces resolve their native cache entry through exact
metadata or package URI. Generated cache handles are recorded when produced.

Resolution uses recorded handles, packaged sources and exact CID lookup in registered
databases. Creation does not build an all-asset index or scan caches to reverse-discover their
Surface memberships. Valid other-owner sources may supply independent target snapshots.
Missing required dependencies, corrupt metadata and conflicting destination bytes remain
explicit failures. Built-in assets terminate traversal; external snapshots preserve bytes
and CIDs independently for each owner.

`BridgeCacheEdges.txt` is obsolete and is neither read nor written. Exact native
Surface-CID atlas lookups and prepared provenance supply cache identities. If necessary,
a shared ephemeral MidMips membership map is built once for the entire inspection, not
once per owner. The shared preparation completes after the native loading barrier and
before any owner migration or structural check. Preparation failure logs an ERROR and
aborts the inspection before mutation, rather than marking individual owners cache-not-ready.
No additional file, cache copy or layout alias is persisted. A successful
complete owner inspection backs up and removes its old ledger; failed inspection retains
it solely as historical data, not as a resolver input. This is the required target behavior;
implementation and native acceptance must be verified separately.

## Self-check discovery and migration

Self-check takes one user Prefab registration snapshot to identify bridge roots. It lists
the immediate children of relevant storage roots and enters only directories belonging to
the selected owners. That shared path inventory supports backup, layout and retirement;
it is not a global CID index and does not eagerly deserialize every Prefab per owner.
Dependency traversal, presentation validation and legacy name migration follow root CIDs.

Migration precedes damaged-owner retirement. File-only naming and layout operations retain
their backup, conflict and rollback behavior. Physical path aliases are normalized before
layout comparison, so an already-correct destination cannot be mistaken for a duplicate
source and deleted. Unowned shared legacy source files remain retained: deciding that no
other owner references them would require the prohibited global consumer scan.

## Diagnostics and verification

Dependency save logs retain exclusive traversal, resolution, source-read, destination
preflight, write, verification, footprint and remaining-time buckets. The historical
`globalIndexBuilds`, `midMipIndexBuilds`, `midMipCacheReads` and related time fields remain
compatible log fields; the forward path leaves them zero. A material footprint includes
native payloads and CID sidecars, separately from packaged-resource size.

The game log for candidate86 on 2026-11-05 at 16:10:23 reports 18 owners checked in
2,565 ms: discovery 74, dependencies 764, validation 25, persistence 1, portable migration
707, names 813, layout 178 and other work 3 ms. It reports zero removals, migrations,
pending-restart copies, removal errors and migration errors. This is one observed run,
not a general timing guarantee or proof of every bridge's visual/cold-load acceptance.

Reproducible source-linked checks live in `tools/DependencySourceCheck`,
`tools/AssetLayoutCheck`, `tools/SelfCheckLifecycleCheck`, `tools/LegacyNameCheck` and
`tools/NativeLocaleCheck`. Dependency fixtures prohibit global enumeration and exercise
explicit native/prepared edges, missing files, foreign owners, conflicts and restart
relations. Saved-owner replay accepts an external game-data directory and writes only
isolated test output. Local logs, user assets and backup trees are not GitHub source inputs.

The [persistence policy](../../agent-contract/Bridge-Asset-Persistence.md) and
[generation contract](../../agent-contract/BRIDGE_GENERATION_CONTRACT.md) remain normative.
