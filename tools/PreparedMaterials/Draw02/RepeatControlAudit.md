# Draw02 Repeat control reuse audit

## Result

No eligible replacement: all five packaged Repeat descriptors use a real authored
control channel. Four distinct existing control textures also fail the transparent
zero check at every mip. No candidate descriptor or texture was produced; savings
are zero bytes. Assets and common code remain unchanged.

## Native source proof

The inventory contains one prototype Surface,
`8a28ab8d9526e258efbcb81c3bbc936b`, SHA-256
`e8c7355d952fdd88921b83fc08e5bc66018457292182aa46bca9d79a896a9597`.
Its separate control-stack texture reference is zero, but its active VTSurface
`130d5d54b722fefb01af8fc4c81f96e4` supplies an authored 4096 x 4096
`_ControlMask`. Testing only the separate reference would incorrectly classify
this source as missing control.

The independent Unity probe opened the actual Game/BridgesAndPorts archives,
verified the native source digest, and decoded the source with the existing VT
decoder through `PrepareStructure.MaterialSource`. It observed BaseColor,
Normal, Mask and Control channels. Every inventory unit matched an existing
packaged Repeat descriptor. Native `SurfaceAsset.ReadHeader` and native dictionary
readers checked those descriptors and all four channel references.

## Full-mip validation

The probe opened each distinct packaged control payload, decompressed its native
texture data, uploaded the original mip chain, and read every texel of every mip
through a compute `Texture2D.Load` operation. No mip was regenerated or encoded.
All four controls have ten mips, and every texel in each mip has a nonzero channel.
Their maximum absolute channel value is approximately 0.99607843. Thus they
cannot be replaced with transparent zero even apart from the authored-source rule.

The local `Road/ZeroControl.Texture` was independently loaded and checked:
1 x 1, one mip, all channels exactly zero. Its actual SHA-256 was
`d002cc9d2fb6777d63a7b16bb306e05dca09bf521552371106c5d8c12c2bcdf4`.
The audit did not assume another bridge's verified hash. Complete per-texture
hashes, sizes, per-mip dimensions and nonzero counts are in the JSON evidence.

## Evidence and scope

- Successful report: `artifacts/prepared-materials/Draw02/repeat-control-audit-verified/audit.json`.
- Exit code and probe hashes: `evidence.json` in that directory; Unity exited 0.
- Native log: `native-audit.log` in that directory.
- Reproducible bridge-specific tools: `RepeatControlAudit.Editor.cs`,
  `RepeatControlAudit.compute`, and `Test-RepeatControlAudit.ps1` in this directory.

Run the wrapper with a fresh absolute `-Output` directory. It uses only the
Draw02 private Unity project. An initial probe run used the VT-tile decompressor
for a plain Texture payload and failed safely before producing candidates; the
verified probe uses the native Texture raw-length decompression path instead.
This audit introduces no runtime text or localization changes. No persistent
resource identifiers were changed, so there is no candidate CID mapping to apply.
