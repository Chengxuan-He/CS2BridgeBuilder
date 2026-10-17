# Draw01 footing far-LOD material and UV review

## Scope and status

This is native resource evidence, not generated-bridge acceptance. The review resolves why the
previous nearest-surface displacement candidate cannot establish consistent central-post widening
across all LODs. No runtime footing mapping is admitted by this report.

## Native resources

Both surfaces use native material template hash `0` (`Default`). Their identities differ:

| Levels | Surface CID | Source surface | SHA-256 |
| --- | --- | --- | --- |
| LOD0/LOD1 | `7d4cadeea169f7fea7346da830d9c49d` | `DrawBridge01PillarBase_5986b7c7862c387efa361643abbcd3a2.Surface` | `682247b4b7e5499148515cd5c85ff9b5c35e4ccf9b85f9844751836574c7a076` |
| LOD2 | `28fdf51684995e85c497cd6683defa8a` | `DrawBridge01PillarBase_LOD2_99820a2b3f39368075a47570628bf0dc.Surface` | `1fc89418b45dac97003635c634b267f43135210d7b9ce0d7b58732daa21bfe0a` |

The surfaces are in `Content/BridgesAndPorts/Blob.cok`. The actual virtual-texture payloads are
in `Content/BridgesAndPorts/VT.cok`, not the surface package:

- Detailed albedo VTTexture: `1011a4b81e3aa59dbb634eccabe35e1a`,
  `DrawBridge01_BaseColor_010f42df245c243c5b08084103271c9c.VTTexture`.
- Far VTSurface: `f5bca65be6d1b5080c7f9bec67301c80`,
  `DrawBridge01PillarBase_LOD2_99820a2b3f39368075a47570628bf0dc.VTSurface`.

Decoded BC7 samples show a dedicated far atlas with baked railing/post detail. UV0 is decoded
from the original native Geometry channel rather than reconstructed from the OBJ export.
The decoded albedo includes varying alpha; alpha alone is not proof of a particular native
shader clipping behavior. No material, texture, shader or installed game asset was modified.

## A concrete displacement mismatch

Far triangle 287 has original vertex indices `295, 335, 292`:

| Vertex | X | Y | Z |
| --- | --- | --- | --- |
| 295 | 6.53515625 | 3.3531494140625 | 76.984375 |
| 335 | 2.3353271484375 | 0.46680450439453125 | 77.30859375 |
| 292 | -3.19525146484375 | 3.47662353515625 | 76.984375 |

At Y = 2.5, both nominal detailed central-post boundaries, X = +/-0.1361236572265625,
lie inside this single far triangle. The current candidate's vertex displacement coefficients
are approximately `0.98051280, 0.35175684, -0.47940478`. Barycentric interpolation at the two
boundary locations produces approximately `-0.0199857783` and `0.0208596784`.

The detailed central post's own-span mapping requires coefficients `-1` and `+1` at its
actual outer boundaries. With an 8 m full-width increment, its width becomes approximately
8.27224731445 m. The distance between the corresponding two sampled far-face points becomes
approximately 0.43562914133 m instead. These are diagnostic points on the simplified far
surface, not a claim of exact high-to-low surface coincidence or a rendered acceptance result.
They expose the failure of the proposed nearest-surface coefficient transfer.

A triangle with unchanged topology and UVs applies an affine displacement to its entire interior.
It cannot independently apply a central post's own-span stretch and neighboring parts' distinct
rigid/rail mappings where those representations share the same triangle. An unchanged baked
atlas does not recover the missing independently movable boundaries.

## Implementation consequence

Do not emit the current candidate as accepted runtime data merely because all nearest-face ties
have been resolved. The cap T-junction fix remains valid evidence; it does not resolve this
separate merged far-detail issue.

The user subsequently approved necessary subdivision of merged structural LOD triangles on
2026-10-16, preserving original vertices, materials and interpolated attributes. The earlier
position-only restriction is therefore no longer a blocker to that specific operation; see
bridge generation contract section 5 and movable contract section 13. This approval does not authorize
replacing the far model with the detailed model or changing its baked appearance. A correct
authored-part mapping is still required. The two later subdivision candidates failed offline
texture inspection and have not been emitted as runtime geometry. Other mechanism, network
and road construction work can proceed independently.

## Reproduction

### Exact source-face footprints

The follow-up `draw01_footing_projected_parts.py` clips the detailed triangles' actual projected
edges against far face 287, retaining barycentric coordinates in both meshes. It uses the face's
dominant projection axis Z and the positive-Z physical assembly. This replaces the earlier
bounding-box planes for this diagnostic; it does not rely on a centroid nearest-part decision.

There are 81 nonzero projected source-face intersections in this far triangle. Four belong to
the detailed central post. Interpolating its recorded detailed coefficients at these clipped
boundaries yields -1.0000000000000009 through 0.9999999999999987, recovering the required -1/+1
post mapping up to floating-point error. The previous whole-triangle interpolation did not.

The resulting post, protective-rail and wall footprints overlap in depth. They are not an accepted
non-overlapping partition of the baked far atlas. Treating every footprint as visible would
duplicate regions; selecting just the nearest centroid would repeat the earlier failure. The
next required operation is explicit visible-source ownership, followed by uncovered-region
handling and textured inspection. Original far-face barycentric coordinates are retained so an
accepted subdivision can interpolate its native attributes without replacing the source mesh.

Evidence: `draw01-footing-exact-footprints.json` and
`Draw01-footing-exact-part-footprints.png`. These measurements cover one diagnostic far triangle,
not all 400 far faces, and emit no runtime conversion data.

### Affine depth ownership follow-up

`draw01_footing_visibility.py` resolves the diagnostic footprint overlaps with affine depth
comparisons, splitting polygons where the winning source changes. It does not choose ownership
from a centroid or fill uncovered cells with a nearest part. For the native outward projection
direction of face 287 (negative Z), its 761 resulting cells retain 14.3886792754 square metres
of source area with a residual of -2.52e-11 square metres. Detailed projected surfaces cover
10.4222291896 square metres; 3.9664500858 square metres remain explicitly unassigned.

The widened diagnostic still shows gaps between independently displaced baked regions. This
candidate is not suitable for runtime emission. In particular, depth ownership alone does not
reconstruct the background hidden behind a detailed post in a flattened far representation.
Unassigned cells are shown in gray and held fixed solely to expose the unresolved area, not as
an accepted transform. A full-mesh textured assessment would still be needed to distinguish
holes from areas covered by neighboring far triangles or native alpha behavior.

Evidence: `draw01-footing-visible-footprints.json` and
`Draw01-footing-visible-footprints.png`. The user declined reconstructing merged far parts from
detailed geometry or changing their material references. Keep the existing subdivision-only
exception. The next investigation must cover the complete far mesh and native texture/alpha
before attributing isolated-face gaps to missing full-model coverage. The contract is unchanged.

### Complete far-mesh footprint diagnostic

`draw01_footing_full_visibility.py` extends the affine footprint/depth calculation to both
physical ends and all 400 original far faces. It produces 18,987 polygon cells. Original surface
area is 1451.2614227471 square metres, including 99.6241051281 square metres without assigned
detailed projected surfaces; maximum per-face area residual is 1.45e-10 square metres.

`draw01_footing_visible_texture.py` rasterizes the complete positive-Z end using the original
far UVs and decoded original albedo/alpha. Neighboring far faces cover some isolated-face gaps,
but the +8 m candidate still has gaps at the railing ends and displaced texture fragments.
It remains rejected. This is an offline albedo/alpha inspection without native lighting or
shader clipping, not in-game acceptance or validation of every viewing direction.

The user's clarification remains mandatory: far parts must widen according to the same authored
part rules as detailed parts. Unassigned cells held fixed in this diagnostic are not an allowed
runtime fallback. They require a valid mapping before any runtime metadata can be emitted.
Neither unchanged far width nor replacement far geometry/materials is accepted.

Retained outputs: `draw01-footing-full-visible-candidate.json.gz`,
`draw01-footing-full-visible-summary.json`, and `Draw01-footing-visible-texture-candidate.png`.

### Moving-background and nearest-depth diagnostics

Keeping unassigned cells fixed creates artificial end gaps. A diagnostic extension now applies
the earlier continuous far-vertex displacement field only to the 2,385 unassigned polygons;
measured part footprints retain their own coefficients. Most large end gaps disappear, but small
holes and texture displacement remain. This is a comparison candidate, not accepted part mapping.

Boundary inspection found 473 conflicting known-part coefficient assignments at unassigned-cell
vertices (coordinates grouped to six decimal places for this offline diagnostic). Some conflicts
come from selecting a globally frontmost surface along a projection axis, including an unrelated
opposite-side part. `draw01_footing_nearest_depth.py` instead compares squared distances to the
original far plane. It splits along both affine factors of the distance difference, retaining exact
per-cell ownership rather than making a centroid decision.

The complete nearest-depth run covers all 400 faces with 19,366 cells and maximum per-face area
residual 1.45e-10 square metres. Under the same boundary grouping, conflicts fall from 473 to 420;
the largest coefficient difference remains 2.0. The positive-end native-albedo inspection still
shows local holes and displaced details, so this candidate is also not admitted. The unassigned
projected area remains 99.6241051281 square metres and needs a justified part/background mapping.

Retained research: `draw01_footing_full_nearest_depth.py`, `draw01_footing_depth_review.py`,
`draw01-footing-full-nearest-depth-summary.json`, `draw01-footing-depth-review.json`, and
`Draw01-footing-depth-texture-candidate.png`. Both comparison candidates widen the far mesh;
neither freezes the far model as a final solution. No runtime code or original material changed.

### Same-side correspondence follow-up

The retained source-face provenance now permits distinguishing opposite-side projection matches
from actual neighboring-part conflicts. `draw01_footing_side_depth.py` rejects strictly opposite-X
source faces for an X-axis projection of a far face wholly on one side. Centre-crossing faces
remain eligible. This is an offline correspondence constraint, not runtime part discovery.

All 400 faces produce 17,456 cells with maximum per-face area residual 1.45e-10 square metres.
Removing invalid opposite-side candidates increases explicitly unassigned area to
100.8901827789 square metres instead of concealing it behind an unrelated donor. Under the same
six-decimal boundary grouping, conflicting boundary vertices decrease from 420 to 402, and maximum
coefficient disagreement decreases from 2.0 to 0.9795764648. This removes the previously observed
full opposite-direction disagreement but does not prove that every remaining source assignment is
correct. Native-albedo inspection still shows local gaps and displaced details, so no runtime
footing data is emitted.

Retained evidence: `draw01-footing-side-depth-summary.json`,
`draw01-footing-side-depth-review.json`, `draw01_footing_side_depth_review.py`, and
`Draw01-footing-side-depth-texture-candidate.png`. The unassigned-region extension is still only
a moving diagnostic field; a justified mapping is required before delivery.

### Signed subdivision-boundary audit

`draw01_footing_boundary_audit.py` examines the retained full-mesh same-side candidate
at a complete width increment of 8 m. It distinguishes an actual separating displacement
across an edge from a tangential mismatch or overlapping displacement. A coefficient
disagreement alone is insufficient to identify a crack.

For each original face, the diagnostic pairs oppositely directed subdivision edges
whose endpoints match after rounding to six decimal places. It projects the difference
between their widened endpoint positions onto the original outward in-plane edge normal.
Positive separation greater than 0.1 mm is reported. These rounding and reporting values
are offline diagnostic tolerances, not runtime classification parameters.

| Measurement | Result |
| --- | ---: |
| Original far faces inspected | 400 |
| Complete paired subdivision edges | 27,881 |
| Unmatched edges or junctions, not evaluated | 14,764 |
| Paired edges with positive local separation | 980 |
| Separations between extension and measured regions | 545 |
| Separations between two measured regions | 435 |
| Inverted candidate cells at this width increment | 0 |
| Largest local separating displacement | 3.3573669957 m |

The largest measured-to-measured separation is 2.8770266500 m inside original far
face 287. It occurs between detailed wall group 0 and side-post group 110, including
source faces 159 and 111. The boundary lies at original X = 1.863861083984375 m.
The wall spans the centre and stretches; the separate positive-side post translates.
Selecting one projected surface at each original far location cuts the background
wall at the post footprint, so the two regions pull apart when transformed independently.
This is a different case from the central-post/rail coefficient disagreement, which
can produce overlap rather than a hole.

These are local boundary measurements, not measurements of visible holes in the whole
bridge: other native far faces may cover the separated areas. Unmatched edges and
T-junctions remain outside this diagnostic. The zero inversion count does not establish
correct texture placement, native shader behavior or visual acceptance.

The next mapping investigation must account for background coverage behind translated
parts, including regions already assigned a detailed source. Adjusting only unassigned
extension cells cannot resolve the 435 measured-to-measured separating boundaries.
This evidence does not authorize reconstruction, new background triangles, material
replacement, frozen far geometry or an exception to authored-part widening. The current
candidate remains research data and is not emitted into the runtime generator.

Evidence: `draw01-footing-boundary-audit.json`, with source face IDs, source groups,
both edge endpoints and signed endpoint separations for every reported edge.

### Whole-mesh coverage and remote projection rejection

`draw01_footing_gap_coverage.py` probes the midpoint between each paired edge's
two widened positions against the complete candidate mesh at the same physical end.
Of the 980 separating-edge probes, 957 have projected geometry coverage and 23 do
not. The largest uncovered probe corresponds to a local separation of 0.9280285083 m.
The earlier 3.357 m local displacement must therefore not be described as a visible
3.357 m hole. This diagnostic ignores alpha, clipping, culling and lighting and does
not inspect every point inside each separating region.

Inspection of the uncovered cases exposes another incorrect correspondence: original
far face 92 is near Z = -92.27 m, but its projected source group 658 is a front-side
post at Z between -77.3828125 and -77.19921875 m. Same-end filtering alone does not
exclude a roughly 15 m projection through the assembly.

`draw01_footing_depth_envelope.py` measures a conservative proximity envelope for each
far triangle. For each detailed triangle, it computes the distances from all three
far corners to that closed triangle, then takes the largest corner distance. Distance
to a convex triangle is convex, so this bounds the distance everywhere on the far
triangle. Taking the smallest such bound over detailed triangles supplies a recorded
witness face and a conservative bound without inventing a distance threshold.
The measured per-face bounds range from 0.001220703125 to 4.6564804139 m; face 92's
bound is 4.6304968713 m, with detailed witness face 541.

The follow-up candidate clips source projection regions to this envelope before
nearest-depth selection. This is a correspondence hypothesis: proximity does not prove
texture ownership, and a remaining candidate is not accepted merely because it is close.
The clipping uses the original far geometry; it creates no replacement far mesh or material.

| Bounded candidate measurement | Result |
| --- | ---: |
| Original far faces | 400 |
| Subdivision cells | 17,875 |
| Explicitly unassigned original area | 120.0323071464 square metres |
| Maximum original-face area residual | 1.45e-10 square metres |
| Complete paired edges | 28,559 |
| Separating edges | 1,007 |
| Inverted cells at +8 m complete width | 0 |
| Separation-midpoint probes without same-end geometry | 15 |
| Largest uncovered-probe local separation | 0.8221701415 m |

The probe set changes when clipping changes the subdivision, so the change from 23 to
15 uncovered probes is not a hole-area reduction metric. Native-albedo inspection still
shows local gaps and displaced baked details. This candidate remains rejected for runtime
emission. Unassigned areas still use the explicitly diagnostic moving extension during
comparison, not an approved authored-part mapping.

Reproduction: run `draw01_footing_depth_envelope.py`, then
`draw01_footing_bounded_review.py`. Retained evidence includes
`draw01-footing-depth-envelope.json`, `draw01-footing-bounded-depth-summary.json`,
`draw01-footing-bounded-boundary-audit.json`, `draw01-footing-bounded-gap-coverage.json`
and `Draw01-footing-bounded-depth-texture-candidate.png`.

### Original extraction files

Research files are under the chat workspace's `work/bridge-research`:

- `draw01_footing_albedo.py`: native marked-ZSTD/BC7 decoding of the two extracted payloads.
- `draw01_footing_uv.py`: original UV0 decoding, native face selection and atlas overlay.
- `draw01-footing-lod0-uv.npz`, `draw01-footing-lod2-uv.npz`: decoded positions, indices and UVs.
- `draw01-footing-uv-evidence.json`: exact selected face geometry and UVs.
- `draw01-footing-candidate.json`: prior candidate displacement records, not runtime data.

The accompanying `Draw01-footing-native-uv.png` overlays the native triangles and highlights
central front faces. Inspection is offline; human in-game validation remains outstanding.

### Native alpha at uncovered boundary probes

The widening requirement applies to every LOD, including merged far components. A fixed-width
far model is not an acceptable fallback. The subdivision-only restriction and original native
material references remain in force.

A follow-up checks the original atlas at the original midpoint of each of the bounded candidate's
15 uncovered boundary probes. Original face barycentric coordinates interpolate the extracted
native UV0; the diagnostic reads the nearest texel from the decoded native RGBA atlas. Eight
samples have alpha 255, four have alpha 254, one has alpha 188, one has alpha 128 and one has
alpha 0. In particular, the largest uncovered-probe separation (0.8221701415 m, face 57) maps
back to a native texel with alpha 255. These gaps cannot all be dismissed as originally transparent
texture regions.

This is a point-sampling diagnostic, not a shader visibility test: texture filtering, mip levels,
native clipping, depth and backface culling remain outside it. It neither measures the full visible
hole area nor establishes a valid replacement mapping. The candidate remains rejected; no runtime
geometry, original asset, material or installed mod file was changed by this investigation.

Evidence: `work/bridge-research/draw01-footing-uncovered-native-alpha.json` in the chat workspace,
including original face IDs, part groups, original positions, interpolated UVs, RGBA samples and
local separations. Further correspondence work must resolve both unassigned moving regions and
measured part boundaries while preserving authored-part widening at every level.

### Boundary constraints before extending unassigned regions

The next diagnostic groups measured candidate vertices within each original far face at six
decimal places and examines both endpoints of the 15 uncovered-probe edges. Of those edges,
12 join a diagnostic extension region to a measured region, and three join two measured regions.
Seven of the 12 extension edges have mutually consistent measured endpoint coefficients to
floating-point precision. The other five have an endpoint with distinct measured coefficients;
the coefficient spreads are approximately 0.1126443157 or 0.1202014616. All three measured-to-
measured edges also have conflicting endpoint constraints.

A continuous interpolation over an unassigned region cannot satisfy two different displacement
values at one boundary point. Averaging those values would change at least one measured part's
required transform and is not a permitted repair. This conclusion applies to the current candidate's
ownership assignment, not to every possible mapping of the original far mesh. It directs the next
investigation toward correcting correspondence and boundary ownership before extending missing
regions. No fixed-width regions, extra background geometry or replacement materials are introduced.

Evidence: `work/bridge-research/draw01-footing-uncovered-boundary-constraints.json` records each
original face, boundary endpoints, measured groups, coefficient extrema and whether the neighboring
region is diagnostic. The six-decimal grouping is an offline diagnostic tolerance, not a runtime
classification rule; complete edge conformity and T-junction coverage remain unverified.

### Native silhouette comparison before any widening

An identity-pose comparison now renders the extracted LOD0 and LOD2 geometry with their own
native albedo. No vertices are displaced. The diagnostic repeats direct and vertically flipped
texture sampling separately, making an accidental texture-orientation assumption visible rather
than silently changing the atlas. Direct sampling aligns the detailed wood/concrete regions;
flipped sampling visibly places unrelated atlas regions on them. This observation supports the
direct sampling used by the preceding diagnostics; it is not a native-shader acceptance result.

The original far geometry already fills some detailed railing openings. In a recorded orthographic
window X = [-7, 7] m and Y = [2.1, 3.4] m on the positive-Z assembly, the 1,200 by 600 projection
contains 66,500 sampled window pixels. LOD0 covers 42,056; LOD2 covers all 66,500. Thus 24,444
pixels (approximately 36.8% of this window) have far geometry without detailed projected geometry.
Their sampled native far alpha ranges from 36 to 255. These counts measure projected geometry,
not shader visibility; they do not use alpha clipping, backface culling or lighting.

Consequently, at least some unassigned far projection regions are intrinsic simplification
surfaces. Searching indefinitely for detailed faces whose projected footprint covers them cannot
supply their ownership. They need explicit, evidence-based treatment consistent with the native
simplification and neighboring authored-part transforms. This does not authorize freezing those
regions, changing materials, inserting background geometry or averaging conflicting measured
part transforms.

A separate exact-index connectivity audit finds 39 islands among the 400 original far triangles.
Several large islands contain both wall-height and railing-height geometry and cross the centre.
Index islands therefore cannot simply substitute for the detailed authored-part identities.
This inspection uses exact existing vertex indices, with no spatial merge tolerance, and emits
no runtime classification.

Reproduction: `work/bridge-research/draw01_footing_native_projection.py`. Evidence files are
`draw01-footing-native-projection-comparison.json`, the four native projection NPZ files, and
`draw01-footing-native-index-islands.json` in the same research directory. The comparison image
is `Draw01-footing-native-projection-comparison.png`
in the outputs mirror. The image is an offline diagnostic, not an in-game validation screenshot.

### Face-normal projection candidate: rejected

`draw01_footing_normal_depth.py` replaces principal-axis projection with an orthonormal frame
whose third axis is each original far triangle's geometric normal. It rotates both source meshes
into that frame for clipping/depth comparison, retaining original face indices and barycentric
coordinates in the output. Physical-end and opposite-side filtering use original coordinates.
The existing measured Euclidean depth envelope remains applicable under this rigid frame change.
No native vertices, attributes, materials or installed files are modified.

The complete run processes 400 faces in approximately 312 seconds. It retains 108,253 polygons,
corresponding to 239,327 triangles under straightforward fan triangulation, versus the original
400 far triangles. Maximum original-face area residual is 1.98e-10 square metres. Unassigned
area is 95.8403918811 square metres, but smaller unassigned area does not establish better
appearance or correct ownership.

| Whole-mesh diagnostic at +8 m complete width | Result |
| --- | ---: |
| Matched subdivision edges | 200,349 |
| Unmatched edges/junctions | 32,794 |
| Separating edges | 1,612 |
| Measured-to-measured separating edges | 808 |
| Extension-to-measured separating edges | 804 |
| Inverted cells | 0 |
| Separation-midpoint probes without same-end geometry | 21 |
| Largest uncovered-probe local separation | 1.8632125408 m |

Offline native-albedo inspection shows clear holes and displaced baked details. The candidate
is rejected for runtime emission. These probes remain finite geometry samples, not visible-hole
area measurements; their population differs from earlier candidates. Zero inverted cells and
complete original-area accounting do not establish a successful width conversion. Polygon merging
might reduce tessellation cost, but cannot by itself repair the demonstrated ownership/coverage
errors, so count optimization is not the next repair step.

Changing projection direction alone does not resolve this asset. The next investigation must
address the original far simplification surfaces and their baked part boundaries rather than
repeat nearest-depth projection variants. No constant-width fallback, replacement far mesh or
material change is admitted.

Reproduction: run `draw01_footing_normal_depth.py`, then `draw01_footing_normal_review.py` in
`work/bridge-research`. Retained evidence includes `draw01-footing-normal-depth-summary.json`,
`draw01-footing-normal-boundary-audit.json`, `draw01-footing-normal-gap-coverage.json`, the
compressed candidate records, and `Draw01-footing-normal-depth-texture-candidate.png` in outputs.

## Width-domain audit: central-post collapse is not a far-projection defect

The extracted highest-detail central post (group 100, faces 100–109) has native X bounds
`[-0.1361236572265625, 0.1361236572265625]`. Its complete width is therefore
`0.272247314453125 m`. The retained candidate applies the contract's centre-crossing rule:
`x' = x + (delta / 2) * x / 0.1361236572265625`, where delta is the complete bridge-width change.

This yields the following exact endpoint relationship, independently of any far partition:

| Complete bridge-width change | Signed separation of the post's original right and left endpoints |
| --- | ---: |
| 0 m | 0.272247314453125 m |
| -0.25 m | 0.022247314453125 m |
| -0.272247314453125 m | 0 m |
| -0.5 m | -0.227752685546875 m |

Negative signed separation means endpoint order reverses, not a valid negative physical width.
The actual extracted high-detail face 100 and middle-detail face 96 both have zero 3D area at
the third row. Thus neither additional far subdivision nor seam filling can fix this particular
failure while retaining the same central-post transform. This is separate from the unresolved
positive-width far material mapping. It is not evidence that every supported road exercises this
width change; actual requested road widths still need to be traced through the final generator.

The offline audit now distinguishes two quantities. With displacement restricted to X, signed
area relative to an original face normal is affine in delta. Its zero can indicate a rotated face,
so it is not automatically a collapsed triangle. The instrument also measures the triangle's
remaining 3D area at that root. In the bounded far candidate, the first positive signed-area root
is approximately +10.555362 m, but its residual 3D area is approximately 3.011473 times the
original area; reporting that as a zero-area collapse would be incorrect. The normal-projection
candidate has the same distinction. The high/middle central-post roots have zero residual area.

Reproduction: `work/bridge-research/draw01_footing_width_domain.py`; measured results are retained
in `draw01-footing-width-domain.json`. The diagnostic uses extracted native meshes and the
retained candidate coefficients, not synthetic geometry. It emits no runtime geometry, introduces
no minimum-width guard, and does not alter the contract or mark any bridge complete.

Next work must resolve the centre-post width domain explicitly before claiming arbitrary-road
support. The far candidates remain rejected for their previously demonstrated coverage defects;
the signed-area correction does not rehabilitate them. Runtime routing, end-to-end persistence,
movement, and human near/far acceptance remain incomplete.

## Approved central-post rule applied to offline candidates

The user subsequently approved preserving the original width and centred position of independent
vertical central railing posts. Bridge generation contract section 8, root/runtime instructions and the movable
contract now record this narrow exception at baseline `dev/80eb1415e1a3b59263eb45693f7e2c5d80732447`.
The preceding collapse measurements describe the superseded candidate, not the approved transform.

`draw01_footing_central_rigid.py` applies zero transverse displacement to the measured native groups
100 and 638 (faces 100–109 and 638–647), retaining the historical candidate files. The change covers
40 high-detail and 40 middle-detail vertex records. None of the original far vertices had been
assigned to these posts by the old nearest-source candidate; updating far original vertices alone
would therefore miss this correction. The measured subdivision cells must be updated instead:
377 cells in the bounded-depth candidate and 2,435 in the face-normal candidate now carry zero
post displacement. Unknown extension cells remain explicitly diagnostic, not accepted part mappings.

Re-running the native-triangle width-domain diagnostic removes the old central-post zero-area roots.
Neither high nor middle candidate has an area root within one metre of narrowing. Their next
geometric collapse is at approximately -8 m complete width change, on wall/cap faces 158 and 142
respectively; that separate constraint has not been reclassified or waived. This is an offline
measurement, not a guarantee about arbitrary road widths or in-game appearance.

The revised bounded-depth far candidate remains rejected. At +8 m complete width, the existing
finite probe instrument finds 1,148 separating subdivision edges, 39 uncovered same-end geometry
probes, and maximum uncovered local separation 0.8221701415 m. The native-albedo diagnostic still
shows holes. Preserving the central post does not repair the other rail/post/wall interfaces.
No runtime mapping, complete generator, deployment or human acceptance is claimed by this change.

Retained evidence: `draw01-footing-central-rigid-update.json`,
`draw01-footing-central-rigid-width-domain.json`,
`draw01-footing-central-rigid-boundary-audit.json`,
`draw01-footing-central-rigid-gap-coverage.json`, and the separate compressed revised candidates.
The inspected image is `Draw01-footing-central-rigid-texture-candidate.png` in task outputs.

## Collapsed-boundary strip experiment: rejected

An offline experiment checked whether preserving both sides' existing displacement coefficients
and filling their separation with boundary-derived strips could resolve the visible holes.
Each strip has zero area at original width and uses interpolated original boundary UVs. At changed
width its two sides follow their respective candidate transforms. Positive separation intervals
are clipped before strips are formed; overlapping intervals are not deliberately filled.

These are additional faces, not subdivision of existing positive-area source faces. They exceed
the current subdivision-only authorization and were never emitted to runtime or deployed. The
experiment does not alter original materials, but that alone does not establish preserved appearance.

| Offline variant at +8 m complete width | Added strips | Uncovered probes out of 1,148 |
| --- | ---: | ---: |
| Fill every matched separating boundary | 1,148 | 0 |
| Fill only the 39 originally uncovered probe boundaries | 39 | 1 |

The complete-strip native-albedo image removes the obvious holes in this view but introduces a
wide horizontal wood-coloured patch below the railing, approximately around X=-4 m. Its boundary
UV mapping has rank one: extending a sampled boundary line into an area stretches its baked
appearance across that area. Reusing original material references cannot correct this artifact.
The sparse variant retains visible holes near the upper railing despite covering almost all
midpoint probes. Thus the finite, dominant-axis, same-end coverage instrument is insufficient to
establish visible closure. Neither result is an accepted mapping or a permission request.

The experiment retains six-decimal edge matching from the diagnostic, ignores native shader
clipping/backface culling/lighting, and does not cover unmatched T-junctions. Its results do not
prove that all possible subdivision-only mappings are impossible. They do rule out promoting
these strips, or the zero-missing-probe statistic, as a completed repair.

Reproduce with `draw01_footing_seam_experiment.py` and then the same command with
`--uncovered-only` in `work/bridge-research`. Separate compressed candidate files and coverage
reports use `seam-experiment` and `uncovered-seam-experiment` suffixes. Inspected output images are
`Draw01-footing-seam-experiment.png` and `Draw01-footing-uncovered-seam-experiment.png`.
Further work must address correspondence between baked material regions and logical parts;
adding boundary faces without that correspondence is not a solution.

## Native-material and part-ownership comparison: transformed surfaces occlude posts

`draw01_footing_material_roles.py` renders unchanged native albedo and candidate ownership at the
same coordinates and camera. Its `--near` mode uses the actual high-detail triangles and revised
central-post coefficients. Category colours identify the candidate's recorded source group; they
are not classifications inferred from texture colour. Both modes retain raster ownership and depth.

The comparison reveals an additional defect beyond gaps: several far post regions become hidden
by other widened regions. In the 1200 by 600 diagnostic, positive-end group 110 (post originally
around X=2 m) has 2,029 visible far pixels before widening and only 13 after +8 m complete width.
Its high-detail counterpart has 1,834 visible pixels in both renders. Group 120 falls from 2,075
to 67 far pixels, while its high-detail count changes only from 1,834 to 1,703 due to rasterization
and occlusion. These counts describe this view, not physical part area or acceptance.

A separate barycentric ray intersection confirms a concrete case without nearest-pixel matching.
Both hits below are at generated `(x, y) = (6, 1.2)` with complete width change +8 m; the camera
looks along increasing Z from inside the positive-end assembly:

| Candidate region | Original far face / cell | Original source point X | Unchanged Z | Sampled native RGBA | Widened normal Z |
| --- | --- | ---: | ---: | --- | ---: |
| Wall group 0, mapped from high face 150 | 369 / 209 | 3.744167622163704 | 77.20727200487309 | (89, 85, 81, 255) | -0.998902445851039 |
| Side post group 110, mapped from high face 111 | 287 / 631 | 2.000000000000002 | 77.22819743037881 | (70, 63, 53, 255) | -0.9939718036765955 |

The wall is about 0.0209254 m closer than the post at their destination overlap. Both winding normals
face this camera, and both sampled alpha values are 255. Thus this example cannot be dismissed as
an unmatched-edge probe, transparent texel, or oppositely facing triangle. This is still an offline
geometric/material observation rather than native-shader or in-game validation.

The source post point was visible at `(2, 1.2)` before widening. Its translated geometry remains
present, but another transformed native far surface now covers it. Increasing subdivision density
without changing these regions' transforms and depth does not change those intersections. Filling
holes also cannot restore this post's visibility. No depth offset or reordered draw rule has been
introduced: original Y/Z preservation and the prohibition on custom runtime rendering remain in force.

This result narrows the next investigation to whether the measured ownership/transform correspondence
can retain native far depth ordering. It does not prove that every possible correspondence is
impossible, nor authorize changing depth or substituting high-detail geometry for the far mesh.
Retained measurements include `draw01-footing-post-visible-pixels.json`,
`draw01-footing-post-occlusion.json`, `draw01-footing-exact-occlusion-ray.json`, and
`draw01-footing-exact-occlusion-surface.json`. Inspected output images are
`Draw01-footing-material-role-correspondence.png` and
`Draw01-footing-near-material-role-correspondence.png`.

## Independent correspondence check and depth decision

`draw01_footing_occlusion_correspondence.py` rechecks the preceding two points against the original
high-detail mesh and both retained partition candidates. The original high-detail ray at `(2, 1.2)`
hits post face 111 first, at Z=77.19921875 with width coefficient 1. At
`(3.744167622163704, 1.2)` it hits wall face 150 first, at Z=77.3828125 with coefficient
0.5639580944590741. These are different authored parts; classifying the wall point as a side post
merely to avoid the overlap would contradict that evidence.

Both far candidates assign the two original points to those same high-detail source faces. After
+8 m complete width, the bounded candidate puts the wall in front of the post by 0.0209254255 m;
the face-normal candidate puts it in front by approximately 0.0207227580 m. Changing projection
direction did not resolve the depth order. Subdivision that preserves these surface positions,
part transforms and interpolated attributes cannot change the intersections at this location.
This is a local incompatibility for the established correspondence, not a universal proof about
every unexamined surface mapping.

The subsequently rejected proposal was limited to correcting depth of affected native merged structural
LOD surfaces using measured highest-detail part surfaces as evidence. It would retain the original
far mesh as its source, all LODs, native material/texture references, UVs and other attributes; it
would not replace the LOD with simplified high-detail geometry or introduce a custom shader.
No such depth modification is implemented or authorized by this report. Its full geometry,
transition continuity, normals, bounds and appearance would still require offline inspection and
human acceptance before claiming a completed bridge.

Reason approval was requested: bridge generation contract section 5 states, "TowerWidening moves vertices along
x and nothing else." The approved subdivision exception expressly does not authorize invented
geometry or replacement structural materials. The central-post exception only changes the transverse
post transform. Neither existing permission allows changing structural Z coordinates. The request
required an explicit user decision; another subdivision-density experiment is not a repair
for the documented pair. Exact results are retained in `draw01-footing-occlusion-correspondence.json`.

## User decision: depth correction prohibited

On 2026-10-16 the user explicitly rejected Z-depth correction. This is a settled constraint,
not a pending approval. Preserve native structural Y/Z coordinates; continue only with authorized
X transforms and necessary subdivision. The separate approved central vertical railing-post
exception remains valid: original width, centred position, and consistent treatment across LODs.
Neither the rejected depth proposal nor the rejected boundary-strip experiments may enter runtime.

The displacement audit also distinguishes the two candidate methods. At the documented wall point,
the bounded candidate coefficient `0.5639580944590741` exactly matches the retained high-detail
coefficient in the diagnostic's binary64 calculations. Its post coefficient is 1 apart from
floating-point interpolation error. The face-normal candidate changes the wall coefficient slightly
because normal projection shifts the source X position, but the occlusion already exists in the
bounded candidate without that shift. Correcting normal-projection X drift alone therefore cannot
repair the demonstrated bounded-candidate overlap. This comparison is not the contract's separate
binary32 generated-bridge width acceptance audit.

There is currently no accepted footing mapping satisfying the preserved depth, part transforms,
material appearance and complete LOD requirements. This does not authorize publishing a partial
bridge, retaining a fixed-width far model, hiding the post or overriding the user's depth decision.

## Latest task decision: defer this defect and continue Draw01

The user explicitly instructed bypassing this issue to continue Draw01, while pausing other
movable styles. The far-footing findings above remain unresolved; they no longer block integration
work. No Z correction is permitted. The temporary runtime path and its limitations are recorded
in [Draw01 generator integration](Draw01-Generator-Integration.md). This task decision does not
convert rejected geometry candidates into verified general implementation guidance.
