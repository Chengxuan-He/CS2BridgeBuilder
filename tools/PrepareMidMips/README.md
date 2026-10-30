# Native MidMips preparation

`PrepareMidMips.py` prepares native cache membership for converted Surface CIDs.
It reads the installed game archives and the current packaged Surface descriptors.
It does not decode, encode, resample or generate texture pixels or mip levels.

Run with `--game-data`, `--materials`, `--output` and `--report` paths. The output
layout for the approved shared source is
`Shared/MidMips/<cacheCID>/TS512_MB3.MidMips` plus its native CID sidecar.
Use `--shared --per-surface` to emit one immutable cache per converted identity,
stored once in the reserved common packaged source. Each generated owner must copy
the required complete cache independently; the common source never belongs to a
generated bridge. Existing per-style Surface and texture payloads stay unchanged.
Without `--shared`, the producer can stage per-style diagnostic candidates. Use a
separate output directory when changing packing options so obsolete candidates
cannot be registered together.

Publish or verify from the repository root:

```powershell
python tools/PrepareMidMips/PrepareMidMips.py --game-data '<game>/Cities2_Data' --materials assets/Materials --output assets/Materials --report '<evidence>/prepared-common.json' --shared --per-surface
python tools/PrepareMidMips/PrepareMidMips.py --game-data '<game>/Cities2_Data' --materials assets/Materials --output assets/Materials --report '<evidence>/checked-common.json' --shared --per-surface --check
```

Check mode reconstructs exact expected bytes without changing material files.
Missing payloads, changed bytes, orphan files and orphan directories fail. Normal
preparation also fails on conflicting or orphaned output before any writes; it does
not silently remove old resources. Explicit migration must verify references and
preserve any still-required source before retiring stale resources. No authoring
JSON is needed by the runtime; the native cache header contains Surface membership.

Ordinary conversions resolve an exact original 276-byte VT header and optional
VTSurface reference. Paint resolves the exact layer-zero subset transformation
from native Surface `4b5500b1b5c270890d99149f7f62e51a`. Active bindings come from the
VT header, including when the serialized texture dictionary is empty. Explicit
zero-level VTTexture headers and empty stacks require no new MidMips registration.
Missing source evidence fails preparation; absence of cache membership alone is
never interpreted as proof of zero levels.

The native source cache supplies primary and duplicate identities. Matching source
headers must expose identical selected block bytes. The producer reads the exact
BC7 rectangles for every required layer and mid level, including each tile's next
mip used for trilinear filtering. It packs these into zero-initialized native tiles,
grouped by stack, dimensions and mip-bias override, with consecutive ordinals within
64-entry groups. Category ranges, group masks and tile indices are regenerated.
Unselected source slots and unused output padding are not copied.

`VerifyMidMips.py <report> <output-json>` independently checks native registration
index invariants, every selected block hash and all unused zero bytes. Repeat
preparation must produce the same carrier hash. Native `AtlasMaterialsDatabase`
registration and `VTTextureAsset.Load` checks remain separate shared acceptance
gates; these Python checks do not certify game rendering.
`TestCheckMode.py --game-data <game> --materials assets/Materials --work <evidence>`
checks missing payload, orphan file and orphan directory rejection in separate
negative fixtures and verifies that none writes a cache payload.

The October 28 user-approved exception allows this common read-only MidMips source
and raises the complete package ceiling to 55,000,000 bytes. It does not change the
10,000,000-byte generated-owner material limit. The 22 per-Surface caches total
87,819,578 uncompressed bytes in the common source; each owner copies only required
caches. Package compression and actual complete owner graphs remain separate shared
acceptance measurements. The earlier grouped 17,773,699-byte cache is a diagnostic
candidate only and must not be shipped beside the per-Surface caches.

Localization review: this is an offline authoring tool with developer diagnostics;
no player-facing text, localized keys, native texture names or runtime UI changes.
