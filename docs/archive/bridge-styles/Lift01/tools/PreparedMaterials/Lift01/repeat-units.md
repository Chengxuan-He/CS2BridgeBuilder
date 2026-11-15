# Lift01 genuine repeat unit

`PrepareRepeatUnits.py` prepares the real-source evidence and missing-key inventory
for the full-mip repeat pipeline. It does not bake pixels or invoke `PrepareRepeat`.
The older mip-zero-only producer is not a valid production entry for this unit.

The measured floor unit uses Surface `bcdd054871e3bcb50f793ccefc50304d`, source SHA256
`da154acf2452319660b3729ff4ebc7f181fbf6524fdf0c3b8411b47d4990bbf4`, and the original
UV rectangle `(0.57421875, 0.260009765625)` to `(0.6796875, 0.469970703125)`.
Faces 11144 and 11145 of real `LiftBridge01 Mesh`, mesh zero, cover 2 by 4 metres.
Their exact UV gradients yield 216 and 215 source texels per metre respectively,
requiring 432 by 860 top-level pixels. All eight runtime moving/fixed/half/side
sampling aliases share this one unit and retain separate direction and phase.

The actual prototype contains 96 rectangular occurrences of the same authored
region, covering 192 faces, with 138 shared physical boundaries. Eighty occurrences
are exactly 2 by 4 metres, four differ only in floating-point tail digits, and twelve
end occurrences use longitudinal spans of 4.216796875, 3.751953125 or 3.9921875 metres.
The evidence records each original physical span and directional density. Those
per-use scales do not create different texture contents or justify additional units.
The existing runtime aliases use the reference period; this inventory alone does
not establish that every original endpoint sampling variation is already retained.

Original color, normal and mask are three separate 4096-square VTTexture resources,
each with 13 original mip levels. The manifest records their actual CIDs, SHA256,
native format/filter/wrap/anisotropy, full tile-offset tables and tail presence.
The 85 compressed tile entries and complete 1,392-byte tail are checked without
discarding lower mips. Control is absent in the original and uses the prepared
transparent zero. The derived 432 by 860 native texture requires a complete
10-level output pyramid. Source level selection, normal semantics and edge filtering
must be validated by the full-mip producer; preserved source files and valid headers
alone do not establish converted pixel or mip-boundary correctness.

Evidence under `artifacts/prepared-materials/Lift01/repeat-units-v2/` consists of:

- `manifest.json`: source, period, channels, old Repeat key and eight uses.
- `prototype-evidence.json`: original mesh hash, all measured faces, gradients and
  physical boundary relationships.
- `missing-keys.json`: the one missing Repeat key and every old Structure key.

The 2,353 old Structure sampling keys have no matching prepared files. None maps
exactly to this floor rectangle; none is declared another genuine unit. Their
absence does not prove 2,353 textures are needed: compatible receivers can use
their original native chart and Surface. Receivers outside their evidenced chart
still need a supported representation. This floor unit cannot be assigned to
unrelated structural samplers as a fallback.

`ExportRepeatUnitInputs.py` exports the three original native Texture payloads and
the original template-1 VT Surface to the shared `Prepare-RepeatUnits.ps1` producer.
Template 1 retains the original single-sided cutout semantics for structural
receivers. Road composition has a distinct caller contract: every Lift01 floor
patch sets `repeat: true`, so `MovableSurfaceAtlas` selects cutout/double-sided.
`PrepareRepeatCallSemantics.py` matches the original `PrepareRepeat.Surface(true)`
and `MovableRoadMaterial.SaveRepeat(grating: true)` path with template 14,
`_Snow=0` and alpha-test/double-sided/SSR/packed-normal keywords. Only its descriptor
changes; the same four texture CIDs remain shared. The original has no control or emissive binding.
The finished candidate borrows the verified transparent control resource and
preserves all original float, integer, vector, color and keyword maps.

The actual producer output is retained under
`artifacts/prepared-materials/Lift01/repeat-units-v2/candidate/`. It contains one
Surface, three new full-mip textures and the borrowed zero texture: ten files with
CID sidecars, totalling 2,260,960 bytes. All eight uses retain the same unit key
`e1b5c319c5fe1af13e49a8fc18bbec63ca7540402deb7d9c7c262620ba7bcd0a`
for road composition. The original structural descriptor remains under
`c0fba02d50a074bb7d466ed3329236a74a67777913bedb37ce685f63bd582dbd`.
`SaveRepeatUnitCandidate.py` checks descriptor parameters, dependency closure,
payload identities and byte-identical preservation of the private output.

`RepeatUnitCheck.Editor.cs` reads persisted Surface/Texture headers using the
installed native readers and samples all pixels of all 30 channel/mip combinations.
Top-level pixel centers exactly match the original source. The maximum lower-mip
linear channel error is 0.002197296, within the measured RGBA8 quantization and
sampling tolerance. Native Repeat sampling agrees at period-shifted coordinates,
including both axes' seams. Color space, anisotropy, complete mip counts and the
original packed-normal keyword are checked. This is not a game rendering test.

The verified candidate is integrated into the formal library. A separate comparison at
the original atlas rectangle boundary detects different filtering: the isolated
Repeat sampler mixes the opposite unit edge, whereas the original atlas mixes its
neighbouring texels. The measured maximum differences are 0.086669922 for color,
0.325978488 for mask and 0.382345319 for the packed normal channel. Exact pixel
centers and native periodicity therefore do not establish original-boundary
equivalence. This existing-atlas pattern difference is retained for human observation
under the project acceptance rule; it does not invalidate the measured unit density
or complete channel/mip preservation.
The checker preserves these measurements separately rather than treating periodicity
as proof that the original atlas edges are unchanged. No repeat bake was rerun.

`IntegrateRepeatUnit.py` verifies every candidate hash and same-CID byte identity
before copying. Eight new files are added; the existing transparent zero and its
sidecar are reused unchanged. All 64 pre-existing files remain unchanged. The
complete Lift01 directory contains 74 files and 2,282,822 bytes. Its Repeat graph
is closed within the style and has exactly one payload per CID, with no empty folders.

`Lift01RepeatUnits.cs` maps the one verified legacy Repeat key, used by all eight
direction/phase samplers, to the template-14 unit key. The native game descriptor
reader consumes the corrected descriptor completely. No texture baking or encoding
is repeated. No Structure key aliases are added: none of
the 2,353 old structure sampling keys is an exact compatible rectangle of this
unit. This count does not imply that 2,353 prepared textures are needed; compatible
native chart paths remain available and unsupported receivers remain separate work.

`Lift01Regions` registers only the measured BBN floor rectangle under the exact
source Surface SHA256, binding the template-1 descriptor. `PrepareRegionReceiverProbe.py`
evaluates the production `Region.ContainsDomain` gate against actual geometry plans
for 24 inputs and six width deltas, totalling 144 scenarios. Source identity is
resolved from the measured render/slot map and checked against current source files.
None of the live structural domains is fully contained in this source's measured
floor rectangle: added region coverage is zero. The 3,131,454 receiver instances
across source-matching scenarios remain on other material paths; this count is not
a failure count because compatible original-source reuse is tested earlier by the
production dispatcher. Phase and gradient mapping are unreachable for this fixture
after the containment rejection. Arbitrary barycentric triangles are not recast as
rectangular units to increase coverage. Results are in `region-receivers.json`.

The shared agent owns main-build verification. Native preview, creation, regeneration,
export and cold-load acceptance remain unverified. No new runtime user-visible text
or localization keys are introduced.
