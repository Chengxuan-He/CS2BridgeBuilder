# Lift03 authored UV traversal classification

## Source and classification

`BuildUvSampling.py` rereads all 13 original native Surface payloads from the
game archives, verifies their CIDs and SHA-256 values, parses every override and
binding, and checks native template defaults against the current-resource
template evidence. The 12 structural sources use Default templates 0/1 with
UV0, identity scale, zero offset and no mapping override. Their native shader
571 samples that affine coordinate directly; the atlas allocation is not a
region-local repeat transform. Original texture, Surface and VT/mip identities
remain unchanged. Each of the six LOD2 sources is inspected independently.

The hash-pinned original fixture supplies every source XYZ, UV and index stream.
The existing native chart files preserve physical XYZ-plus-UV edge connectivity;
the current schema-3 domain decoder supplies exact source-face membership.
Finite single traversals, including constant and line mappings, qualify for
source-UV retention. This is a mapping classification, not a claim that the
texture contains no repeated painted motif. Disconnected members may reuse an
atlas island without becoming a tiling mapping along either member.

Overlap with a road repeat region does not establish structural repetition.
Only the original physical traversal determines the classification. A source-native repeat is evidenced when
adjacent, consistently oriented coplanar faces reset UVs by a complete extent of
the same chart. The evidence records exact source triangles, UVs, gradients and
reset edges. Exclusion propagates only through the same physical UV-continuous
component, never to a disconnected member merely sharing an interned chart ID.
Large but inexact resets remain unresolved. Small atlas seam offsets are not
classified as repeats; notably, NetPillarTop LOD1's one-step U seam is retained.

The corrected overlap audit restores 104 near and 105 middle finite domains.
All original faces overlapping the two historical road repeat regions are
removed by `Lift03RoadTops.RemoveDetailed`: 640 near and 554 middle faces.
None belongs to any of the five structural fragment families, verified against
the current embedded resource bytes. Those two structural Region registrations
are retired; no replacement structural material is introduced. Evidence is in
`artifacts/material-remap/Lift03/structural-region-unreachability.json`.

Windows use the independently verified original template 2 / shader 550,
without overrides. The shared native-window audit's complete UV0 and COLOR
arrays are checked against the original Geometry hash, source XY and indices.
Only domains whose faces have finite local YZ coordinates in the authored unit
interval, constant W orientation and constant COLOR selection qualify. Preserve
the complete original channels, not just XY; the other 24 domains remain unknown.

## Runtime interface and construction

`Lift03UvSampling.For(render, mesh, domain)` returns the recorded decision. Its
exact switch covers 20 original streams; unknown streams return false and bad
domain indices on a known stream throw. The nested data holder lazily reads
`Lift03UvSampling.bin`, an ordinary checksum-protected BBN1 resource containing
I32 `[1, streamCount, domainCount, flags..., ...]`. A true flag means both native
mapping axes can retain source coordinates. False preserves the density path;
it does not certify a newly bakeable repeat unit.

Retain original source UVs and interpolate them using `Plan.Construction` for
permitted edge/interior vertices on their recorded Fragment source triangle.
Lift03 has no `UvCorrections`. Bent fragments do not inherit a density-remapped
coordinate outside the original chart. The LOD2 support supplement uses the
LOD1 source stream and classification. No geometry, bones, topology, textures,
material identities, source chart resources or domain resources change.

## Evidence and limits

`artifacts/prepared-materials/Lift03/non-tiling/classification.json` records all
606,619 source domains, source identities, exact membership, native semantics,
physical reset examples and the generated resource hash. Source domains are not
actual emitted Retile units. Common integration must measure the latter and the
complete SDK package separately. Expected non-tiling physical-density changes
are not density-preserving passes. Preview, saved generation, export, native
normal/mip rendering, cold load and human near/far/motion acceptance remain
separate checks.

Regenerate with a Python interpreter providing NumPy and Shapely. The script
uses retained source evidence and original archives only, never generated bridge
assets. Current native-template and Windows audit inputs are under
`artifacts/prepared-materials/shared-nontiling/`. Repeated regeneration must
produce identical C# and BBN bytes. Localization review: no UI text or asset
display text changes; new strings describe internal data invariant failures.
