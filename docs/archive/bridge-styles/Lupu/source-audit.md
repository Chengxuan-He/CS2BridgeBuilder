# Lupu source and construction audit

## Scope and source

Measured on 2026-11-16 from installed Paradox Mods source `151335`, revision `2`.
The literal source title and BuildingPrefab identifier are `卢浦大桥` (Lupu Bridge).
The official source is [Paradox Mods 151335](https://mods.paradoxplaza.com/mods/151335/Windows).
The user confirmed that no author dimensions are available; the following values come from
actual installed geometry, not the real-world bridge or a dimension inferred from its name.
The installed COK SHA-256 is `32b2bf0e9ffcf46b679218d3c403906de2e062940e9d13244af3f66e434ca310`.

The root contains one ObjectMeshInfo, identity translation and rotation
`(0.707106769, 0, 0, -0.707106769)`. Its render is `lupudaqiao Mesh`, CID
`105b204f71b6078ad4315793356fe1e2`, followed by `lupudaqiaoLOD1 Mesh`
(`7ef3ddd6c891a985fe2c8fe86a1cd9d4`) and `lupudaqiaoLOD2 Mesh`
(`47910622d170582b14f1375d222d0091`). LOD and shadow bias are both zero.
The near render carries EmissiveProperties; derivation retains its component and original
source channels. Root building economy, signature-building and lot-area components are not
bridge-network behavior and are not copied to the generated selected network.

## Measured construction frame

The native decoded mesh uses X longitudinally, Y transversely and Z vertically. The source root
rotation maps these to an upright world bridge. All structural roles now share the frame
`(sourceY - 0.04015827178955078, sourceZ - 37.2265625, sourceX - 0.01171875)`.
The vertical origin is the bottom of the lower inward terminal slab. The current recipe fixes
that bottom and all support cap tops at construction Y=-0.5 through offline coordinates,
without sampling or moving the road. The complete balcony assembly is lowered by 0.5 m.
This is right handed. Native composition reverses its local X/Z; rigid support placement uses
Y-axis half-turn rotation to match that native world orientation.

| Quantity | Measured value and interpretation |
| --- | --- |
| Source complete envelope | X `[-280.6484375, 280.671875]`, Y `[-21.4541015625, 21.580078125]`, Z `[-0.0012107789516448975, 77.560546875]` |
| Full authored length | `561.3203125` m, including terminal ornament overhangs |
| Reference course length | `540.984375` m between terminal tower centers `-270.48046875` and `270.50390625` |
| Selected-road outer interface | `22.22540283203125` m between actual main-arch outer section edges Y `-11.072543144226074` and `11.152859687805176` at source Z `37.2265625` |
| Historical balcony-tip measurement | `15.35693359375` m between Y `-7.6153564453125` and `7.7415771484375`; no longer the road-width reference |
| Historical hanger-toe measurement | `18.772216796875` m between hanger-toe inner boundaries Y `-9.330078125` and `9.442138671875`; no longer the road-width reference |
| Full structural width | `43.0341796875` m, both outer envelope boundaries included |
| Historical hanger datum | Z `34.65625`; not the current construction origin |
| Unified road-interface datum | Z `37.2265625`, bottom of the lower inward balcony slab; its top remains at source Z `38.7255859375` |
| Spandrel contact datum | Z `34.6201171875`, inward column-cap edge; the outer cap edge is up to `0.1259765625` m higher |
| Arch-foot centers | X `-206.033203125` and `206.060546875`, measured footprint midpoints |
| Foot separation | `412.09375` m between those centers; not a published clear span |

The source contains no running-surface mesh, sidewalk paving or operational lanes. These are
supplied entirely by the selected elevated network. The running-space measurement records
an adaptation reference, not an imposed road type or a source traffic configuration.

## All-LOD ownership and adaptation

The source continuous side arches include the terminal regions and main arch; their native
network normalization applies the same longitudinal ratio to each region. Four independently
placed object domains retain the measured terminal towers, one per side at each end. One finite native
course avoids repeating the terminal towers as ordinary repeated span attachments. Native
composition and piece-object placement follow the consumers recorded in the
[native length audit](../../audits/native-bridge-length-handling-audit.md).

| LOD | Source slots | Continuous structure faces | Negative terminal | Positive terminal | Total faces |
| --- | ---: | ---: | ---: | ---: | ---: |
| Near | 2 | 5,081 | 20,854 | 20,854 | 46,789 |
| Middle | 2 | 4,822 | 5,158 | 5,158 | 15,138 |
| Far | 1 | 420 | 541 | 549 | 1,510 |

The terminal columns above combine the two sides for comparison with the original pair records.
Individual tower face counts are 10,427 each near, 2,579 each middle, and 276/275/265/274 far
in part order 1/2/3/4 (negative end/positive end on the negative side, then the positive side).

All source faces are retained exactly once. Far source simplification already removes hangers
and transverse ties; the recipe retains that authored LOD rather than manufacturing detail.
Terminal window material occupies only slot 1 in near/middle sources. Explicit recorded slot
selection excludes empty slots from continuous structure, without mixing Surfaces.
Source original UVs and exact attribute-index provenance remain attached to their source corners.
Normal/tangent affine frames follow each measured displacement differential.

Every source vertex uses `x + sign(x) * halfWidthDifference` in the unified construction frame,
including transverse upper connectors. Center-line vertices stay fixed. Shared original positions
receive identical coefficients; original triangles and all material/UV corners remain intact.
The connector rule supersedes the former independent-span scaling and follows the explicit
user correction. Triangle affine differentials transport normal/tangent frames.
Terminal towers and their decoration keep rigid transverse and longitudinal
sections. The selected network's native pricing and water rules remain intact. Structural objects
receive UnderwaterObject with dry-land placement enabled; structure-only piece height minima do
not add an InWater blocker. No custom shader is persisted by this style.

## Reproduction and evidence limits

### Terminal structural collision identity

The reported owner `b5ba37073-b7a7-46b9-867f-c7cc9fa7dfb5` saved terminal StaticObjectPrefabs
with only UnderwaterObject. Native ObjectInitializeSystem classifies that ordinary static-object
branch as Overridable and Brushable. Native network validation lowers overlaps with such objects
to Override rather than an ordinary blocking structural collision. UnderwaterObject only sets
placement flags; it does not establish structural collision identity.

The generator now adds PillarObject with PillarType.None. The native initialization branch gives
it BaseCollision, CanSubmerge, OccupyZone and OptionalAttach without the ordinary Overridable
classification. Native AlignSystem and SubObjectSystem only perform the special terrain/top
anchoring for their explicit Vertical/Standalone/Base cases, so None retains the measured pose.
The two towers at each end are emitted separately: the native overlap consumer tests object
boxes or cylinders, not individual render triangles, so one box enclosing both towers would
incorrectly fill the opening between them. All source faces, original material slots and LODs
remain assigned exactly once. Offline reconstruction still has zero reference-world error.

### Unadopted arch-root collision experiment

The four terminal towers have the collision repair above. Localized below-deck collision for
the continuous arch roots remains unresolved. No whole-span negative HeightRange, guessed
padding or undocumented slope restriction was added.

An offline experiment duplicated 1,272 original below-deck near-LOD faces into Outline-only
StaticObjectPrefabs with PillarType.None. Native CulDeSacLarge01 and CulDeSacMedium01 use
ObjectState.Outline (22); ObjectInitializeSystem includes their mesh bounds in collision while
BatchInstanceSystem limits rendering to Outline. Existing custom preview skips nonzero states.
This proves the mechanism, not its suitability for the deforming arch.

An initial 50 arbitrary 15 m bins was reduced to 20 source-structural domains: twelve inner
arch/root/outer arch side groups separated by measured wedge footprints, plus eight actual
cross-deck tie islands. The extra recipes were about 102 KB, 161 resources and 1,272 triangles.
The retained [experimental fixture](collision-check.json) verifies source-face provenance,
exactly-once duplicate coverage and horizontal containment at half-width deltas -4/0/8 and
length ratios 0.6/1/1.4/1.54. Native positive fixed length is 0.6 to 1.4; validation expands the
upper bound by 1.1. HalfLength is not used by this style. Conservative carrier-free central
intervals were 122.470/256.588/390.707/437.649 m. At ratio 0.6 the actual source-triangle
bounds left about 170.51 m: the proposed all-length bounds added approximately 24.02 m of
longitudinal exclusion at each inner edge, beyond ordinary native triangle-to-box approximation.

The experiment was rejected before deployment. Native attached-object placement rotates the
whole local box along the tangent, whereas network positions retain a world-vertical height
offset. Horizontal containment therefore does not establish nonzero-grade or vertical-curve
coverage. Disabling curved courses alone is not proof that all vertical grade transitions are
excluded. Native slope validation checks endpoint height differences, has exemption paths,
and cannot justify universal padding from MaxSlopeSteepness. Selection outlines would also
retain reference local geometry instead of the stretched visible arch. This is a known
geometry mismatch, not merely missing live-game acceptance.

Exclusive experimental code and resources were moved to
`E:/unused/lupu-unadopted-root-collision-20261117`, with the earlier draft separately retained
in `E:/unused/lupu-collision-domain-draft-20261117`. Production retains the four independent
towers and 201 resources. The experiment must not be reported as a completed root-collision
fix. Native placement still needs to confirm deployed tower collision, legitimate navigation
and remaining root overlap. No player asset or installed package was changed by this audit.

### Source Surface inspection and preview failure

The 2026-11-17 preview log records `Recorded native network material input is unavailable.`
inside `TowerFactory.CreateRecordedNetworkSurfacesAsync`, before the later `PreviewBuildFailed`
wrapper. The reported opposite-direction selection is not evidence of a geometry or road-direction
failure: conversion rejects the source Surface before constructing that geometry, and Nanpu reaches
the same shared rejection.

Direct decoding of the installed COK's version-1 Surface records distinguishes VT-capable
materials from the optional packed VTSurface reference:

| Source Surface | Bytes | Template | VT flag | Packed VTSurface reference |
| --- | ---: | ---: | --- | --- |
| `lupudaqiao_ee166e204f291f3b83f406cd7a5bb994.Surface` | 483 | 1 | true | absent |
| `lupudaqiaoLOD1_56dbb3e2cecea82e54931efab8cd071f.Surface` | 483 | 1 | true | absent |
| `lupudaqiaoLOD2_fcca2e56ec03f5f6f7875dd4ac1b9068.Surface` | 499 | 0 | true | present |
| `lupudaqiaoGls_4001fd3dce6035caae3e53997aaac458.Surface` | 56 | 11 | false | absent |
| `lupudaqiaoLOD1Gls_d7b57391d0806cddfe45f22dc78f1a81.Surface` | 56 | 11 | false | absent |

Each structural Surface records `_BaseColorMap`, `_ControlMask`, `_EmissiveColorMap`,
`_MaskMap` and `_NormalMap` original TextureAsset identities. Near and middle also carry five
nonzero preprocessed VTTexture references in their two-stack VT headers. The native loader uses
those VTTexture payloads when no packed VTSurface is present; the original texture identities are
not additional required payloads for these VT-managed channels. Far instead uses the packed
VTSurface and has zero preprocessed VTTexture references. Native `SurfaceAsset.ReadVTHeader`
reads the VT flag independently of the optional payload reference, and `LoadTexturesVT` supports
both native storage forms. Therefore the absent near/middle packed payload is valid source data.

The inspected native material-library records map template 0 to `Default`, template 1 to
`DefaultCutout` (both `BH/SG_DefaultShader`), and template 11 to `DefaultGlassDoubleSided`
(`BH/GlsShader`). These are native game programs. External source Surface and VT payloads follow
the shared unchanged-byte, unchanged-CID owner-snapshot path; retaining their references does not
authorize depending on the source mod's installed files after generation. No custom program
payload or registration recipe belongs in that snapshot.

The installed COK contains all five referenced VTTexture payloads and their identity sidecars,
the far VTSurface, and `TS512_MB3.MidMips`. Texture filenames' GUID-shaped suffixes differ from
their actual sidecar CIDs. The verified VTTexture CIDs are
`f3c20b22cb47fc08c743171d739792ab` (base color),
`7c5a14b953787a82591c256343be6596` (normal),
`99a5d1cffb91389b85b08852dacdb828` (mask),
`032a5cc25bc154e4ee9605df96c682c7` (control), and
`8968ac673c5922f430ec881236475b2e` (emissive).
The far VTSurface CID is `12ddad3b66cfd5dac671bd7f39624104`.
The shared binary Surface reader follows these serialized payload references; material cache
resolution adds required native cache membership. These source-file and code checks do not
replace a generated-owner snapshot or source-disabled cold-load test.

Under the authorized [non-Network native-template conversion](../../../specs/non-network-spec.md#native-material-representation-for-network-rendering),
Lupu converts continuous-structure slot 0 at every LOD to the native `NetCompositionMeshLit`
template. Its native vertex protocol supplies the placed network's composition deformation;
the source `Default` and `DefaultCutout` templates use the object protocol instead.
The converter must consume actual standalone VTTexture channels at near/middle detail and
the packed VTSurface at far detail, preserving their appearance and required mip data without
attempting to load absent original Texture payloads. Terminal objects retain their original
Surfaces, including glass slot 1 at near/middle detail. Generated Surface and channel resources
use the existing owner persistence path; unchanged external dependencies retain exact-byte,
exact-CID snapshots. No custom shader or program payload is introduced.

The source-preserving, per-placed-course object-geometry alternative was not adopted; no
dedicated files were created for that alternative. That alternative did not change the geometry or native network
placement recipe; the later road-interface correction below is independent. Shared conversion repair, successful live preview, near/far
appearance and cold loading require their own evidence; this source inspection does not establish
those outcomes.

### Geometry reproduction

Run `tools/GeometryMetaprogram/Lupu.py` with explicit COK path, game data directory, numerical
output directory and isolated evidence directory. It uses the game's PipelinePlugin decoder,
records all-LOD ownership and generates the checked-in existing-format numerical resources.
Run `tools/GeometryMetaprogram/LupuCheck.py <resources> <evidence>` against the resulting source
snapshots. No live game process or source mutation is involved.

Offline checks cover every face once, corner/source-channel provenance, exact measured-reference
vertex reconstruction, fixed terminal dimensions at course ratios 0.6/1/1.6 and signed width
mapping. The reference reconstruction error is zero at all LODs. A 16 m width increment gives
exactly 16 m complete-envelope growth; source near and far envelope differences are retained.
These results do not establish game rendering, source Surface conversion, traffic, native placed
names, water placement, cold loading or the live generated-asset width audit.

The shallow arch-root wedges are integral welded members of the continuous arch shell: near
and middle source each have 44 wedge faces at each end, height 2.604736328125 m. They are
not independent source objects or vertical pier columns. Separating them into rigid objects
was rejected after measuring their finite shared interface: uniform arch normalization would
separate that interface at non-reference lengths. Their final representation stays in the same
network piece as the arch, retaining width/height sections and the source faces. The shared
interface is checked at width half-deltas -4/0/8 m and length ratios 0.6/1/1.4/1.6. All matching
source positions remain coincident. This source-specific span-local base classification does
not authorize stretching unrelated rigid piers or terminal towers.


### Superseded first road-interface correction (2026-11-17)

The first correction used separate hanger and balcony-top frames, width 15.356934 m,
and 207 numerical resources. Its generator SHA-256 was
`D7A950FE980601598C23CCFF16A1EEFAE695C42222A4F1E3B18184C30E074883`.
The deployed output really applied its recorded shifts: two fresh owners matched X to within
0.000001 m and all 24 column Y shifts were -2 m. This was not a stale-asset failure.

Its [432 flat-bottom and 144 Top-only checks](geometry-check.json),
[section illustration](interface-before-after.png) and
[column overlap sampling](support-contact-check.json) describe that superseded recipe only.
They did not model the actual sloping outer Bottom piece and do not establish game acceptance.
The source `RoadElevatedBottomEdge2` outer edge is -1 m, whereas its bounding minimum -2 m
occurs inward. Using the latter as the whole contact plane caused the reported gap.
Originals and the full 207-resource recipe are backed up under
`E:/unused/lupu-full-interface-20261117`.

### Superseded profile-contact correction (2026-11-17)

This 213-resource attempt is historical. A later actual-owner inspection disproved its
claimed running-surface offset: the source layer was misidentified. The following geometry
checks describe the earlier intended fixture and do not verify the actual game output.
The common-plane correction below supersedes its separate cap/balcony heights.

The current user-defined references are the lower inward slab **bottom** and the main arch
outer section edges at that plane, as measured above. All parts use that common frame.
The actual source running surface in the reported road is -0.20000076293945312 m; inclusive
queries at the two selected-road outer boundaries provide the generated vertical offset.
The continuous structure uses their mean and each rigid terminal side uses its own boundary.
No median top, global HeightRange or universal thickness substitutes for the selected geometry.

The shared selected-road context measures original native Top/Bottom longitudinal-center
cross-sections with actual composition offsets and inversion. `SupportProfile` supplies the
piecewise lower support boundary, preferring Bottom only where Bottom exists and otherwise
using Top. For each original column's measured cap upper envelope, the caller compares every
intersecting pair of segments at both ends and takes the minimum road-minus-cap height.
This creates contact without penetrating the road; it does not flatten the authored sloping
cap or claim its entire area conforms to a different road underside. No matching surface leaves
the source cap unchanged. No road width or placement is rejected.

Raising whole source columns would disconnect some feet from the arches. Instead, the original
bottom vertices remain fixed and measured cap vertices move together. Existing source triangles
connect those domains without new cuts or topology. Offline face gradients describe that exact
piecewise affine displacement; the runtime normal/tangent Jacobian is composed as
`widthJacobian * columnJacobian * sourceFrame`. Source UVs, materials and vertex attributes stay
attached to their original corners. Near and middle each retain 24 independent source-column domains plus four integral terminal
bearing domains. The far source omits the independent columns but retains all four terminal
bearings; these receive the same contact adaptation without fabricating detail. The former inward column offsets
are removed. Four independently placed terminal collision objects remain unchanged in role.

[Historical profile verification](profile-geometry-check.json) covers 240 column and terminal-bearing contacts at widths
22.225403, 22.5, 30.5 and 40 m using the actual measured sloping native edge profile, with zero
minimum contact gap and no cap penetration. All 46,789 / 15,138 / 1,510 source faces remain
exactly once, unified reference-frame error is zero and maximum tested triangle-Jacobian
residual is 0.00005228 m. Foot vertices stay exactly fixed under the column correction.
[Historical profile illustration](actual-profile-contact.png) shows actual source/generated
sections and the measured native road profile at widths 22.5 and 30.5 m. It is an offline section
plot, not a game screenshot. The original slab bottom follows the road skin; column inner caps
contact the native underside while preserving their source slope.

There are 213 numerical resources: the six additions record cap segments and affine support
weights/gradients. This does not restore the rejected navigation-collision carrier experiment,
change selected-road water rules, create custom shaders or modify any player asset. Native
preview, placed grades/vertical transitions, near/far appearance and cold loading still require
separate in-game acceptance. These checks replace the earlier flat-plane acceptance claim;
they do not by themselves prove the player's reported appearance is resolved.

The first unified-frame review exposed an omitted bearing class: the blocks below each terminal
tower belong to the continuous arch shell (near components 43/45, middle/far 1/3), not to the
rigid tower objects or their decoration. Their authored tops are also at source Z=34.6201171875.
Four measured cap vertex sets and their incident original triangles now receive the same support
profile correction in every LOD. All non-cap vertices remain fixed, preserving the shell joint;
no separate collision object or artificial geometry is introduced. The updated section plot at
source longitudinal station 270.49 m shows those caps contacting the road underside. Station
185 m does not intersect a discrete source support, so its empty section is not a missing member.
The 213-resource count and runtime caller are unchanged; the additional domains are immutable
entries in the existing support tables. Originals before this incremental correction are retained
under `E:/unused/lupu-before-terminal-support-20261117`.

### Unadopted road-derived common-plane attempt (2026-11-17)

This intermediate 219-resource implementation was not installed. The user subsequently
required a hardcoded plane instead of runtime height sampling, superseding this entire route.
Its source and numerical originals are preserved under
`E:/unused/lupu-unadopted-runtime-plane-20261117`. The tests below characterize that rejected
intermediate, not the current fixed-plane implementation.

The user explicitly requested raising the supports and lowering the inward balcony assemblies
until the support tops and balcony bottoms share one horizontal joining plane. This supersedes
retaining the source sloping caps and independently aligning balcony bottoms to road skin.
The structural reference frame and signed transverse width rule remain unchanged.

Actual owner `b5221d87f-a745-4138-80d2-deef1bb71f7e`, created at
`2026-11-17T03:26:17.5750140Z`, used assembly MVID
`57e340d0-4a7e-4e39-95fa-2b2dec336da8`. Its decoded geometry showed zero global
running-surface offset, not the intended -0.2000007629 m. The actual native descriptor uses
`NetPieceLayer.Surface` (0) for running surfaces; the old shared collector incorrectly selected
`Top` (2). Its fixture repeated that wrong enum assignment. The corrected collector reads
Surface and Bottom (1), preserving native packed offsets and transverse inversion.
[Actual owner diagnosis](common-plane-owner-diagnosis.json) and
[decoded owner comparison](common-plane-previous-owner.json) distinguish that observed defect
from the new offline geometry checks. Shared production-method tests against actual decoded
native triangles record 50 passing assertions; they use fake IO and are not a native session.

Generation queries `SupportAt` at both actual selected-road outer boundaries and uses the lower
available value as one common horizontal joining plane. If only one boundary is available it
uses that measured value; if neither exists it uses the measured road-surface reference.
For the reported road, both actual outer Bottom boundaries are -1 m. Its running skin remains
-0.20000076293945312 m. No hardcoded -1 m road thickness enters production. The older
interval-profile minimum is no longer used. No new width or geometry rejection gate is added.

All 28 near/middle support caps and four far integral terminal caps are flattened and raised
to that common plane. The source cap upper-edge variation of up to 0.1259765625 m is deliberately
removed by this explicit user request. Foot vertices remain unchanged; existing source faces
connect the fixed feet and moved caps. Offline static flattening gradients and parameterized
height gradients produce the exact normal/tangent Jacobian. Column caps may enter the thicker
inner part of the native road slab: this is deliberate solid contact, not the former isolated
point contact on its sloping underside. No additional triangles are introduced.

The four inward balcony assemblies include their lower slab, intermediate rails and upper slab.
Move each complete assembly down by common-plane minus its original road-skin reference;
do not move the tower body, windows or unrelated ornaments. Near and middle source lower slabs
have 18 faces each; their source bottom is Z=37.2265625. The far source omits lower slabs and
rails and retains the 16-face upper slabs. Those surviving far parts receive the same assembly
translation; no lower slab is fabricated and no missing far bottom is falsely reported as tested.
The original tower wall still overlaps the rear of the translated assembly.

[Common-plane checks](common-plane-geometry-check.json) cover 240 cap-plane cases across all
LODs and four widths, with zero measured cap-plane error. They additionally check all present
lower balcony bottoms, unchanged tower vertices, rigid complete balcony translation, unchanged
feet, exact original face/corner provenance and triangle-wise position derivatives.
[Measured section plots](common-plane-contact.png) show both terminal stations and an ordinary
column at widths 22.5 and 30.5 m. Caps and lower slab bottoms now meet at -1 m in the actual
native road cross-section. These are offline plots, not screenshots from a generated candidate.

There are 219 numerical resources. Twelve immutable balcony membership arrays replace six
obsolete profile-cap/interval arrays; support records now carry both static flattening and
height-dependent gradients. The retired arrays and original files are under
`E:/unused/lupu-before-common-plane-20261117`. This change preserves original materials,
all source faces/LODs, four terminal collision identities, selected-road rules, the signed
width mapping and native persistence. No player asset was modified. New-candidate native
preview, placed near/far and sloped-course appearance, and cold loading remain unverified.

### Superseded fixed construction plane Y=0 (2026-11-17)

The subsequent user adjustment lowers the common plane by exactly 0.5 m. The zero-plane
results below retain their historical scope and do not describe the current coordinates.

The latest explicit user instruction replaces all runtime road-height lookup with one hardcoded
horizontal plane, Y=0. The source lower balcony slab bottom Z=37.2265625 maps directly to it.
All measured independent-column and integral-terminal cap vertices are baked offline onto the
same plane. The original foot vertices remain unchanged and original incident faces connect
the feet to the higher, now flat caps. No extra faces, generated supports or runtime height
adjustment are introduced. This is the user's scoped geometry adaptation, not a new generic
permission to change tower height.

The tower body and complete inward balcony assemblies retain their original geometry in that
source frame. Because the lower slab bottom already equals zero, no additional whole-tower or
local balcony shift is necessary in the final recipe. The far source lacks lower slabs and
ordinary columns; preserve its surviving upper balcony slabs and four integral terminal bearing
caps without inventing missing detail. The signed X displacement remains identical at every LOD.

The runtime consumes the baked Origins and three static support-frame arrays. Their per-face
Jacobians transform normals/tangents consistently with the recorded cap-height change; unchanged
faces carry identity. There is no `BridgeRoadGeneration.Find`, `RoadTopAt`, `SupportAt`, profile
query, geometry scan or selected-road height cache in this style's height path. The abandoned
profile intervals, dynamic support memberships and balcony translation arrays are removed.
The final source contains 204 numerical resources, down from the unadopted intermediate's 219.

[Fixed-plane checks](zero-plane-geometry-check.json) verify 240 cap-plane cases at widths
22.225403, 22.5, 30.5 and 40 m, with zero cap-plane error, all present lower balcony bottoms at
zero, exact original face/corner coverage, unchanged foot/tower Y coordinates and triangle-wise
normal-frame derivatives. [Fixed-plane section plots](zero-plane-contact.png) show both ends and
an ordinary column at widths 22.5 and 30.5 m. Source/output face totals remain
46,789 / 15,138 / 1,510. The known native road skin at -0.2000007629 m is drawn for reference;
the requested zero plane is 0.2 m above it. These checks establish structural coplanarity, not
arbitrary selected-road surface conformity or absence of road penetration.

The earlier real-owner diagnosis remains useful evidence of the superseded route; it is not a
new-candidate acceptance result. This revision has no new player-created owner yet. Native near/far
appearance, placed slopes and cold loading remain unverified. Source materials, four independent
tower collision objects, water-placement policy, UUID ownership and existing assets are unchanged.

### Current fixed construction plane Y=-0.5 (2026-11-17)

The user's latest height adjustment sets the shared plane to exactly Y=-0.5 m. This is baked
into the immutable geometry, not obtained from road meshes or calculated as a runtime correction.
Every measured cap top is flat at -0.5 m. Feet remain fixed; the original connecting faces and
static affine normal/tangent frames are regenerated from the source coordinates.

Each complete inward balcony assembly (lower slab, intermediate rails and upper slab where
present) is translated down exactly 0.5 m. Its lower slab bottom therefore also equals -0.5 m.
The rest of each tower, its windows, ornaments, original materials and collision identity remain
unchanged. Far LOD retains its authored upper slab and omits the same source lower slab/rails;
the surviving assembly receives the same rigid translation. No source faces are added or removed.

[Updated checks](half-plane-geometry-check.json) verify 240 all-LOD cap-plane cases, all existing
lower balcony bottoms, unchanged feet and unrelated tower coordinates, complete assembly rigid
translation and source-derived triangle Jacobians. The maximum cap-plane error is zero.
[Updated section plots](half-plane-contact.png) show both ends and an ordinary column for widths
22.5 and 30.5 m. The actual road profile is reference context only; this verifies the specified
structural plane and does not claim adaptation to arbitrary road surfaces or game acceptance.

Resource count remains 204. The runtime generator changes only its explanatory plane comment;
all positions and static frame changes come from the metaprogram output. The original zero-plane
files are preserved under `E:/unused/lupu-zero-plane-original-20261117`. No player assets were
modified and the agent did not launch, close or deploy the game. New-candidate native near/far,
sloped placement and cold-load acceptance remain pending.

### Incomplete first hanger correction (2026-11-17)

The later player screenshot shows hanger rods and shoes inside the road after the width
reference changed to the main-arch exterior. Near and middle source meshes each contain
52 ordinary hanger assemblies of 36 original faces, plus two 44-face assemblies omitted
by this first correction. Its tests enumerated only the selected 52 and did not independently
prove source-role completeness. The far authoring mesh omits these assemblies;
no far detail is invented. Each assembly has a lower shoe with sixteen distinct source
positions and four upper attachment positions. The source shoe bottom lies around
Z34.66, below the current source datum Z37.2265625. The upper attachments follow the
inward-leaning arch and therefore lie inward of the complete road's horizontal projection.

The current offline recipe places each lower shoe at the selected road's complete outer
edge, including shoulders and sidewalks, using the existing signed half-width displacement.
Its bottom meets the already requested fixed plane Y=-0.5. The shoe's original inward wall
leans slightly; the measured two end rings define a small transverse shear that makes that
wall vertical against the road edge. The shoe rises by 2.065430–2.070313 m; its top remains
approximately Y0.989258–0.999023. These are recorded source-specific adjustments, not runtime
road-height measurements. The original long rod faces connect the adapted shoe to unchanged
upper attachment vertices. No face subdivision, additional bracket or new support is introduced.
The upper rod necessarily inclines inward above the lower region to retain its arch connection;
this does not claim that its entire high-level projection is outside the road.

The static frame resource now records six floats per output vertex: the construction X and Y
Jacobian increments. The common factory composes these with the source and width frames.
Coordinates remain offline Origins followed by the ordinary shared width executor. There is
no per-style runtime coordinate correction, part discovery, road-height query or new cache.
Source UVs, materials, glass, source corners and every triangle retain their provenance.
The four terminal towers, support feet, signed upper crossbeams and Y=-0.5 cap/balcony plane
remain unchanged from the preceding increment. Resource count remains 204.

[Complete geometry checks](hanger-geometry-check.json) cover source-face ownership, unchanged
upper hanger attachment positions, original unrelated coordinates, all cap/balcony contacts
and analytical triangle Jacobians. [Independent contact checks](hanger-contact-check.json)
exercise 416 hanger/width cases at 12, 22.5, 30.5 and 40 m using actual recorded-output triangles.
Every tested member of that incomplete 52-hanger selection remains outside the road
through construction Y=1 m; this did not cover the two omitted assemblies. Shoe-wall contact at
Y=-0.2, 0 and 0.8 has a maximum measured gap of 0.0000476837 m. Upper attachments have zero
displacement relative to their source-plus-width position. Original face totals remain
46,789 / 15,138 / 1,510. [Output sections](hanger-contact.png) show the short and central
hangers and their road-edge contact; the observed road skin is reference context only.

These are offline source/output checks. They do not establish native near/far appearance,
vehicle clearance above the checked lower region, sloped placement or cold-load acceptance.
Existing player assets are unchanged. The preceding originals are backed up in
`E:/unused/lupu-before-hangers-20261117`. This increment requires a newly generated sample;
no claim is made that previous persisted geometry is rewritten automatically.

### Complete hanger inventory correction (2026-11-17)

The player's two detached shoes are source near components 590 and 655, corresponding to
middle components 34 and 99. Both are on the positive transverse side, at longitudinal
stations approximately -107.1 and +107.1 m. Each has 44 original triangles instead of 36.
Its lower shoe has twenty distinct source positions instead of sixteen: four additional
vertices subdivide the upper shoe ring. The four upper attachment positions retain the
same role. These are authored topology variations, not separate brackets or missing assets.

The earlier metaprogram's `len(fi)==36` condition excluded these two entire assemblies.
Actual-owner comparison in `work/lupu-detached-20261117/actual-recipe.json` establishes that
all three decoded LODs matched the deployed recipe at width 22.5 m with zero coordinate
error. The report was therefore caused by the recipe, not stale installation or an old owner.

Role selection now uses measured source placement and anchor extents without a face-count
condition. Shoe-wall fitting takes both longitudinal ends of each measured ring; extra
interior ring vertices cannot replace a wall endpoint. All original corners and faces remain.
An independent welded-component inventory in LupuCheck enumerates every source island
starting at the shoe level and crossing the deck datum, before examining recorded role lists.
It verifies exact face-set equality: 54 hangers in each near/middle LOD, 27 per side,
52 ordinary and two subdivided assemblies; the authored far LOD has zero. This new inventory
fails against the old 52-member recipe and passes against the regenerated resources.

[Complete inventory geometry checks](hanger-inventory-geometry-check.json) verify 540
hanger/width cases and 300 support-cap cases, original upper anchors, face provenance and
triangle Jacobians. [Independent contacts](hanger-inventory-contact-check.json) cover 432
cases at widths 12, 22.5, 30.5 and 40 m. All tested hanger triangles through construction
Y=1 m remain outside the complete road, lower shoes reach Y=-0.5, and maximum measured
wall-contact discrepancy is 0.000047684 m. [Sections of the two repaired members](hanger-inventory-contact.png)
show their upper connections and lower road-edge contacts. This is an offline construction
view; the road skin is reference context, not runtime height sampling.

[Before/after face comparison](hanger-inventory-changed-faces.json) confirms only 84 faces
per near/middle LOD changed coordinates, all within the two original 44-face assemblies.
Their remaining four triangles retain their original positions. The other 52 hangers,
all unrelated structure and the entire far LOD retain exactly the preceding coordinates.
Source/output totals remain 46,789 / 15,138 / 1,510 faces. Runtime generator code and resource
count (204) are unchanged; only offline selection, checks and recorded numerical data change.
Originals are preserved under `E:/unused/lupu-before-hanger-inventory-20261117`.

These checks do not establish native appearance, grades or cold loading for newly generated
owners. Existing player assets are untouched. New generation and in-game visual acceptance
remain separate from these source/output proofs.
