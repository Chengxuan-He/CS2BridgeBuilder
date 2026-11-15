# Lift01 Repeat control audit

## Scope and result

The one packaged Lift01 Repeat material qualifies for a reference-only replacement with the existing `Road/ZeroControl.Texture`. The verified candidate is integrated into the Lift01 packaged assets. No shared code was changed. No texture encoder or bake was invoked. The final candidate and native evidence are in `artifacts/prepared-materials/Lift01/repeat-control-verified/`.

## Source and pixel evidence

The real Bridges & Ports prototype Surface `bcdd054871e3bcb50f793ccefc50304d` has SHA-256 `da154acf2452319660b3729ff4ebc7f181fbf6524fdf0c3b8411b47d4990bbf4`, matching the Lift01 inventory. Native VT decoding exposes base color, normal and mask only; the control stack CID is zero and there is no authored control. The inventory covers one complete unbanded repeat unit with aliases for regions 0–7.

The existing control `f32289510ec139e30aa2a175f2edd914` has SHA-256 `2fbb17157022d502f993596e04e99775f1ec72c5ba581529b06eb7cd28509432`. Native TextureAsset parsing, native ZSTD decompression and exact GPU texel loads verified all 495,267 texels across all ten mips of its 432 × 860 BC7 texture. All four channels are exactly zero, including alpha. The existing Road zero texture was independently read and verified: one RGBA8 texel, all channels zero, CID `729893d593df5d52ba911bc1e7eab77b`, SHA-256 `d002cc9d2fb6777d63a7b16bb306e05dca09bf521552371106c5d8c12c2bcdf4`.

## Candidate and unchanged data

Repeat key: `f7fcada6949aa34822e7279596faf7e86b7d927afb4b60c5203810d673dcc0c3`.

- Old Surface CID: `6c310e7ea405c17fd22d97ca3d67a615`.
- Candidate Surface CID: `e57d4af362196d185266f2c60ff39f96`.
- Candidate SHA-256: `860764f97c47427d1d9c3e908238b165885c3da43881afa676592961d28be049`.
- Only the 16-byte `_ControlMask` CID at offset 141 changed. Every other byte in the 236-byte descriptor, including textures 0–2, remains identical.
- CID calculation matches PreparedBakeStore: first 32 hex characters of SHA-256 of UTF-8 `BridgeBuilder/RoadRepeat/1/<payload SHA-256>`.

The game SurfaceAsset reader and property dictionary readers accepted both the in-memory candidate and its saved disk readback, consumed every byte and resolved all four bindings to existing Lift01 resources. Identical-CID local copies were also checked for identical bytes. No authored control, pattern, UV, density, texture channel or runtime behavior was modified.

## Benefit and integration

The complete Lift01 inventory contains 30 Surface descriptors. Before integration, only the Repeat Surface referenced the old control; after replacement, all 30 were checked and none referenced it. The old Shared texture and sidecar were backed up before removal. The existing Road zero texture and CID were copied byte-for-byte into `Shared/729893d593df5d52ba911bc1e7eab77b.Texture` and its sidecar, making the Repeat dependency directly available to the Shared loader.

The style payload total changed from **2,983,886 to 2,983,833 bytes**, a net reduction of **53 bytes**: removal of 107 old bytes minus the required 54-byte Shared zero copy. The primary benefit is removing a **496,416-byte native mip payload** and its texture allocation. The zero resource retains its existing CID and represents four raw bytes. Comparing standalone texture payloads gives a 496,412-byte raw reduction; when Road zero is already loaded, the full 496,416-byte allocation is avoided. GPU allocator overhead and final Paradox package compression were not measured.

The machine report `artifacts/prepared-materials/Lift01/repeat-control-integration.json` records every before/after file hash, the exact changed/added/removed paths and the all-Surface reference scan. Backups under `artifacts/backups/Lift01/repeat-control-integration/` contain only the four original files. Native inspection of the integrated assets passed in `repeat-control-integrated-native/native-audit.log`, process 11772, exit code 0; the unchanged Surface was also written and read back through the native reader. All unrelated Lift01 files and channels retain their exact bytes. No build output or installation was modified, and no bake ran. This is not game visual acceptance.

## Reproduction and review

Run `tools/PreparedMaterials/Lift01/Test-RepeatControlAudit.ps1 -Output <new absolute candidate directory>`. It uses only Lift01's independent Unity environment. Final successful process: PID 61992, exit code 0. Evidence: `audit.json`, `benefit.json`, `native-audit.log` and the candidate `Repeat/` subtree. All audit processes exited.

The initial diagnostic attempt used the tile-only decompression helper against a native texture payload and failed before creating a candidate. The final tool uses the game's native CompressionUtilities ZSTD decoder instead. No assets were modified by either attempt.

Localization review: this is an offline diagnostic and candidate-resource change with no user-facing UI strings; existing localization behavior is unchanged. `git diff --check` passed for the Lift01 tools.
