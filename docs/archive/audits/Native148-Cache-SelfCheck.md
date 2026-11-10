# Native148: self-check without a persisted cache ledger

This is an implementation/verification record, not an additional project contract.

## Implemented behavior

Creation no longer writes `BridgeCacheEdges.txt`. Current resolution never reads it. No
replacement metadata, relationship file, duplicate cache, alias or changed cache layout
is introduced. Existing payload bytes and CIDs are retained.

Self-check waits for the existing main-menu/native asset-batch barrier. It then prepares
one ephemeral cache membership inventory before any owner dependency migration or
structural validation. Preparation enumerates only registered MidMips assets and reuses
cache paths from the existing scoped owner inventory. It reads membership headers and
stream lengths, not cache pixel payloads. Repeated registered physical paths are skipped.
There is one preparation attempt per inspection, shared by every owner; a lookup cannot
implicitly start another preparation. No global all-asset/CID index is constructed.

Exact Surface-CID native atlas registrations and prepared provenance remain available to
resolution. The native lookup uses the installed registration dictionary, bounded atlas
ancestors, `TryGetMidMipMask`, and the entry's grouper cache. Owner-only saved Surfaces
can instead resolve through their original VT headers and the prepared ephemeral inventory,
without a live Surface handle. VTSurface layers take precedence over VTTexture tails.
Zero-mid-mip headers do not invent a cache dependency.

Shared preparation failure logs ERROR and aborts the whole inspection before owner
mutation. There is no new per-owner cache-not-ready deferral. After completed preparation,
a required absent cache is an explicit dependency failure under the existing migration
policy. The separate pre-existing inaccessible-file/identity handling remains unchanged.
A corrupt or inaccessible cache encountered during the shared inventory can therefore
abort the whole inspection, even if another owner might not need that cache; it does not
cause that preparation error to retire owners.

An obsolete ledger is backed up byte-for-byte and removed only after that owner's full
inspection and migrations succeed. Failure retains it unused. No player bridge or obsolete
ledger was removed by the offline tests or deployment. Runtime cleanup awaits a subsequent
successful user-run self-check.

## Verification

- Production and frozen builds passed, including existing geometry/resource/material guards.
- Source-linked persistence tests passed: normal creation performs no cache traversal or
  ledger write; owner-only VT snapshots; zero mip; missing required cache; primary and
  duplicate Surface identities sharing one cache; two owners sharing one inventory; failed
  preparation attempted only once; no late lookup build; no extra owner files; corrupt
  cache preparation failure; obsolete ledger ignored and original-only backup/removal.
- Lane-cache boundary regressions passed with a real prepared native cache payload.
- All-language localization checks passed; no user-facing translation was added.
- A Unity test host loaded the final postprocessed mod DLL and actual installed engine
  assemblies. The native dictionary/VTAtlassingInfo/ObjectEntry/grouper path resolved one
  real Surface identity from a real cache payload, without a Surface handle. An absent
  identity did not borrow a neighbouring cache; an uninitialized native index returned a
  diagnostic. This test reconstructed native registration containers; it did not start Cities2
  or claim live-game startup acceptance.
- No live-game header-pass duration was measured. Self-check now records `cacheTraversals`
  and `cacheHeaders`; preparation cost belongs to the existing discovery interval. Dependency
  save's historical global/index counters retain their creation-path meaning.
- Scoped documentation/source whitespace checks passed. Frozen scoped files match the
  primary working files exactly. All seven evidence links in this report resolve. Of 69
  local links in the four edited documents, 68 resolve; README's unchanged sibling-repository
  link `../CS2RoadPrefabExporter` is unavailable on this machine. It was not rewritten as
  part of the cache change.

Evidence: [build](../../../artifacts/material-remap/shared/native148/build.log),
[persistence](../../../artifacts/material-remap/shared/native148/dependencies.log),
[native identity](../../../artifacts/material-remap/shared/native148/native-identity.log),
[localization](../../../artifacts/material-remap/shared/native148/localization.log),
[scope](../../../artifacts/material-remap/shared/native148/scope.json),
[deployment](../../../artifacts/material-remap/shared/native148/deployment-native148.json).

## Deployment

Local candidate **native148** was installed with Cities2 absent. All 964 installed manifest
files were verified; only DLL and PDB changed, and their prior bytes were backed up.
No game was started, no saved bridge files were changed, and nothing was published/pushed.

DLL SHA256: `A63C815D899649E6513ED2A1D706E2728B02FC47D892AD6D4C41413C678803DB`.

Official SDK ZIP: **77,170,919 bytes**, versus **77,167,231 bytes** for native147. The existing
55 MB publication gate remains unmet; this was local deployment, not publication.

The separately edited deletion design is not implemented by this change.
