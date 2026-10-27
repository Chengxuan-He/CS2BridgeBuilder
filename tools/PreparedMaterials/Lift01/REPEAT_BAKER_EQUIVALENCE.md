# Lift01 repeat baker equivalence audit

## Decision and scope

The retained Lift01 `PrepareRepeat.cs` hash
`D42974FD78C837817B12A3CD612941B26117D4DCA344F67C380433F9F63E30EF`
is output-equivalent to public hash
`53A6F2297B243BE8D7910D75612BEFC33147883652993F03A747FA24827AFC9D`
for the checked-in Lift01 inventory and its five existing products. It may be
included as an audited legacy hash for reusing that pipeline report. This is a
source and payload audit; no baker, Unity process or encoder was run for it.

The original private baker and the actual Unity Editor source both match the
reported legacy hash. All twelve pipeline components were checked against the
retained report. Only `PrepareRepeat` and `PreparedBakeStore` differ from current
sources; the other ten components match exactly, including source decoding,
pixel footprint, mip implementation, compute shader and native codec library.

## Output-affecting comparisons

| Area | Finding |
| --- | --- |
| Inventory and null bands | Lift01 records `bands: null` and `bandsNull: true`. The old reader restores null using the explicit flag; the current reader normalizes an empty array to null. Both produce the same no-band input for this inventory. |
| Sampler key | The complete key method is unchanged. Both versions exclude descriptor digest from the pixel sampler and retain it for the material key. |
| Source decoding | Source CID, descriptor digest, VT/native decoding and channel keys are unchanged. Lift01 has three authored 4096 x 4096 channels and no authored control layer. |
| Dimensions | Identical Float32 bounds and block alignment produce 432 x 860. Source density remains 216 x 215 texels/metre. |
| Pixel rendering | The complete `Render` method is unchanged. Source bounds, phase, sampler, channel bindings, linear render targets and dispatch dimensions are identical. |
| Missing control | Both bind the same opaque-black Unity texture. The new code only gives this constant channel a sampler-independent storage alias; width, height, channel index, source identity and pipeline remain in the key. |
| Mips and encoding | `MovableAtlasChannel`, compute kernels and `PreparePaint.Texture` match the old report. BC7 channel color spaces, full mip chains, native compression and texture header serialization are unchanged. |
| Sampling metadata | The retained control header is format version 5, 432 x 860, ten mip levels, BC7 UNorm, bilinear, Repeat and anisotropy 8. No new header or resampling occurs during alias reuse. |
| Surface descriptor | The complete `Surface` method is unchanged, including native template 14 for the Lift01 cutout recipe, channel identities and keywords. |
| Store and CID | Existing `Get` behavior and content-derived CID calculation are unchanged. The added alias path verifies receipts, rejects conflicting bytes and returns the exact prior payload. |
| Reuse-only | This adds a failure before a missing product's producer is called. It cannot change an existing payload. |

Both the old and current code reject nonempty bands that fail coverage checks.
The old explicit flag could reject contradictory metadata, while the new reader
ignores that flag; explicit empty arrays also have different invalid-input behavior.
This audit does not approve those malformed inputs or claim universal behavioral
equivalence for them. The measured Lift01 recipe contains neither case.

## Retained evidence

`artifacts/prepared-materials/Lift01/repeat-baker-equivalence.json` records the
component hashes, unchanged-method hashes, all five product hashes/CIDs and exact
inventory/report digests. `repeat-baker-equivalence.diff` retains the reviewed
source differences. Every product receipt was verified, and each payload matches
its already packaged file byte-for-byte.

Under the retained pipeline, the control product key is
`23cdf13946f87d2ca5a707d17ea772b0ac0eecd3719723bbe20bb3f9aa20d3b5`.
The new constant-control alias is
`55d0e256cb3e64c0a32151cfd9b7bfaef86f94d97a52fef724aaace9e899050f`.
The existing payload SHA-256 is
`2fbb17157022d502f993596e04e99775f1ec72c5ba581529b06eb7cd28509432`.
This audit calculates these keys without creating an alias or changing the store.

The original native-reader evidence remains `repeat-native.log`: one Surface,
four native textures and 1,985,664 mip bytes uploaded unchanged. This audit does
not replace game rendering, cold-load or human visual acceptance.

No runtime or user-visible text changed; localization behavior is unaffected.
