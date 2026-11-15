# Lift05 source repeat-unit evidence

`PrepareRepeatUnitEvidence.py` exports original-resource evidence and old-call
mapping to `artifacts/prepared-materials/Lift05/repeat-units-v2-evidence/` without
reading or regenerating any old baked material payload.

`repeat-unit-candidates.json` follows the `BridgeBuilder/RepeatUnits/2` envelope but
keeps the measurement-stage candidate separate from executable trial input. Its
one candidate is the fixed-road clean
strip used by original recipe regions 0–3. Mesh 0 faces 557 and 558 establish the
actual source UV affine and physical scale; they are not synthetic unit triangles.
The strip spans UV `(0.41015625, 0.402099609375)` to `(0.4375, 0.5546875)`, or source
pixels `(1680, 1647)` to `(1792, 2272)` in the 4096-square original atlas. Its measured
physical dimensions are approximately 0.7164202985 by 4 metres. The source position
quantization accounts for the tiny difference between measured density and the
existing recipe's nominal 3-metre affine, both of which are recorded separately.

The original code selects this clean strip inside the complete native island to
exclude baked railway tracks. Its `repeat: true` flag records the previous caller's
intent, while the source-mip and edge/normal measurements below quantify the actual
result. Existing-atlas pattern differences are accepted under the root AGENTS
policy and are retained for human observation rather than hidden by a periodic
self-comparison. No receiver-derived or triangle-reflected alternative has been
created. The moving-region recipe is unreachable under the current
selected-road source policy and is retained only as source-face evidence.

`source-evidence.json` records the original source Surface CID/SHA, original face
vertices and UVs, inverse-UV metric, native channel identities and archive hashes.
The main atlas channels have complete 13-mip VTTexture containers at 4096 by 4096;
the LOD2 atlas channels have complete 10-mip containers at 512 by 512. Every tail
and tile-table offset is bounded and consumes the complete original payload. These
sources have no standalone original authoring Texture resource or VTSurface
reference. The three intermediate mip levels require a global MidMips lookup by
source Surface, stack and layer even though the VTTexture header has no MidMips CID.
The remaining tail data is inside the VTTexture payload. Container-consumption
evidence alone does not establish a complete source pyramid.
The glass source has four original standalone Texture resources with complete mip
counts, but it requires no new repeat conversion and remains directly referenced.

The shared repeat-unit entry accepts native VT Surface descriptors with supported
object templates. `tools/PrepareRepeatUnits/ExtractVtSources.py` recovers its full
ordinary Texture inputs from original VT blocks plus the global MidMips carrier,
without resampling or recompressing the BC7 content. The older mip-zero VT decoder
must not be used to manufacture replacement mips.

`FindAuthoringTextures.py` searches all built-in `Content/**/Blob*.cok`, rather than
only the Game and BridgesAndPorts packages. Its report
`all-builtin-authoring-search.json` records 19 inspected archives and 519 standalone
Texture CID entries. None of the six original authoring Texture CIDs used by the
main and LOD2 Lift05 atlases exists there; no LiftBridge05 filename candidate exists
either. The four unrelated original glass Texture resources do not substitute for
these atlas channels. The private trial therefore uses recovered full VT mip input,
not fabricated mip-zero input.

## Private trial and boundary gate

Evidence and the original trial output reside in
`artifacts/prepared-materials/Lift05/repeat-unit-trial-v2/`. The full 13-mip original
BC7 chains for all three channels were recovered using
`BridgesAndPorts/MidMips.cok`, entry `TS512_MB3.MidMips`. The first recovered mid mip
matches the independent trilinear tile copy byte-for-byte. `extraction.json` retains
every source mip hash and MidMips provenance. Full extracted atlas inputs remain
inside the private environment and must not be distributed as prepared materials.

The offline trial produced three 112-by-625 textures, each with 10 authored-source
mip samples and periods `(1, 1)`. With the Surface, transparent-zero dependency and
CID files, the private candidate is 10 files totaling 328,226 bytes. This excludes
the separately validated 15,078-byte native descriptor library. No minimum package
size is imposed; extra bytes must not be added to reach a nominal size range.

`RepeatUnitCheck.Editor.cs` and `RepeatUnitCheck.compute` read the persisted product
through the native game binary headers and original decoder, confirm its owner
dependency closure, density, color spaces and sampler settings, and compare it
against the recovered original atlas at every output mip. The 30 channel/mip checks
have a maximum pixel-center difference below 0.002198 and zero periodic
self-comparison difference. However, original-atlas boundary differences reach
0.040894 for color, 0.274510 for normals and 0.150989 for mask. This fails the
strict atlas-equality comparison. A periodic self-comparison is not evidence that
the original filtered boundaries were retained. Under the accepted existing-atlas
pattern-difference policy, these measured differences remain explicit human visual
acceptance observations. No structural repeat unit was inferred from this road-strip
experiment.

## Installed road binding

`InstallRepeatUnit.py` installs the measured pixel payloads without rebaking them.
The fixed-road caller's old cutout semantics require a separate native Surface:
template 14, `_Snow=0`, `_ALPHATEST_ON`, `_DISABLE_SSR_TRANSPARENT`, `_DOUBLESIDED_ON`
and `_TANGENTSPACE_OCTO`. The original trial Surface used the prototype's template 0;
that descriptor is not installed as a replacement for road cutout behavior. Both
representations reference identical pixel payloads. The shared transparent-zero
control is reused once in the style directory.

`Lift05RepeatUnits.cs` maps old Repeat key
`3b59afd3aa0ad2d963d6c6c7ba95cb1d0e33ccd1eec3f4b19e1a324a77551222`
to prepared unit
`e56ec4c072e5cd227a3d38319d3733d6337a0e25c4825b4d476e233dbf65cc41`.
The four fixed-region receivers retain their existing UV orientation and phase.
No structure alias is added: of 915 old structure samplers, 184 are rectangular,
but none has the same source rectangle as this unit. The other 731 triangular
samplers are not reinterpreted as rectangular coordinates.

The formal Lift05 directory has 68 files totaling 343,315 bytes, including the
previous native descriptor library. The installed game reader consumes all 30
Surfaces (27 VT) completely. `installed.json` records hashes, the local four-channel
dependency closure and exact road parameters. `installed-native-check.log` records
the native-reader result. Public build, broader structure handling and game visual
acceptance remain coordinated integration work.

## Actual structural region coverage

The Lift05 `ReceiverCheck` harness links the shared `MovableRepeatRegions` predicate
and exercises the measured region against actual geometry-plan receiver units, with
the original render/material source SHA resolved before checking containment. Across
11 source inputs and six width deltas (66 cases), 23,795 receiver units were tested;
36 cases share the candidate source digest. No complete structural source domain is
contained by the clean road strip, and the new region mapping adds zero covered
units. The geometric density, packing and native footprint checks all pass.

Evidence is retained in `artifacts/prepared-materials/Lift05/region-coverage/`,
including the predicate and geometry-assembly hashes, per-case counts and log.
The harness reports 19,768 units not directly reusable under its original-domain
test before broader part-chart handling; this is not a claim that all those units
fail the separate original-chart path. What is established is that this road unit
covers none of them. Therefore no unreachable template-0 structural descriptor or
`Lift05Regions` registration is added, and no triangles are misinterpreted as a
rectangular sampler. Additional structure regions require separate real-source
evidence; they cannot be inferred from this strip's bounding box.

`call-mapping.json` retains the one old Repeat alias and all 915 old Structure
sampler keys separately from source texture identity. Structure samplers do not
become repeat units automatically. A receiver fully contained by its genuine
physical-part source chart can retain its original Surface; receivers outside that
chart require a proven semantic unit and a verified native sampling path. The old
per-sampler material library is not a fallback.

The validated source-level native library in `rebuild-native-v1/output/Lift05`
remains unchanged. Installation adds only this measured unit and its bridge-specific
alias; no public code, game installation, geometry or user-facing/localized text is
changed.
