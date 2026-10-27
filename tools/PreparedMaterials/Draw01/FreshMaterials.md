# Draw01 fresh material preparation

The fresh output is separate from the retired sampler-based resource tree. Do not
copy old Structure or Repeat products into this output. Formal `assets/Materials`
publication is deferred until the shared cleanup and new producer interface are ready.

## Completed native preparation

`PrepareFreshFixed.ps1` runs private style-scoped copies of the checked Cable, Road
and Lane descriptor producers. These read the original game archives and native
template evidence; no previous prepared Surface or texture is an input. The style
filter is the only change to each copied producer. Cable and Road precede compaction
because Road's producer verifies the newly authored Cable control identity.

`CompactFreshFixed.ps1` verifies both newly produced controls have the same CID and
SHA-256, then stores one physical payload in `Shared/`. It removes only those two
duplicate control files and their sidecars in the explicitly resolved private output.
It does not modify any Surface reference or formal asset.

The current fresh subset contains:

- One Cable Surface and one Road Surface.
- Twenty-six native Lane descriptor variants, deduplicated from 103 real sources.
- One 1x1 transparent control texture, shared by Cable and Road.
- Original built-in Road and Lane texture/VT references, preserving their native mip graphs.

This is 28 Surface payloads, one Texture payload, 58 files including CID sidecars,
and 14,580 bytes. These are **partial source material-directory** figures, not the
complete Draw01 material set, a generated bridge footprint or the official mod ZIP.

## Validation and saved output

Unity's native SurfaceAsset/TextureAsset readers accepted the fresh files. The
26 Lane descriptors retain native VT headers, appearance defaults and references.
The generated transparent control's complete single mip uploaded without changing
its bytes. Production `BridgePreparedMaterialFiles` and `BridgeSurfaceReferences`
then validated every descriptor against the compacted style-local dependency graph
and actual built-in CID catalogue. No unreferenced file, duplicate payload identity
or empty directory remains in this subset.

The private output is
`artifacts/agent-material-env/Draw01/fresh-native-materials/output/Draw01/`.
The same checked bytes are preserved outside the environment at
`artifacts/prepared-materials/Draw01/fresh-native/staging/Draw01/`.
The adjacent `fixed-lanes-native.log`, `fixed-graph.json` and `roots.json` preserve
native evidence, hashes and the explicitly incomplete root manifest.

The original structural inputs were independently reread from game archives:
13 Surface identities, eight descriptor contents and 12 built-in texture resources.
All source hashes match the existing verified catalogue. The three template-17
Surface identities have identical 141-byte descriptors and need only one compatible
descriptor conversion. Their original three Texture identities must remain shared.
The private `inputs/sources.json` and original Surface bytes contain no old baked
output. No original texture pixels were copied into the fresh output.

## Pending shared integration

Paint preparation must preserve the required original mip data. The previous
mip-zero decode followed by regenerated mip levels is not used as proof of this.
Road-repeat and structural sampling must use the new source-level shared producer,
not recreate the old per-sampler images. Template-17 object conversion requires the
measured source defaults and compatible native shader semantics. These shared
requests have been sent to the coordinator; the old large-resource fallback is not
an accepted fresh deliverable.

The complete graph, native sampling, saved-bridge lifetime tests and official SDK
package measurement remain pending. No formal materials were published or deployed.
No user-facing text changed, so existing localization behavior remains unchanged.
