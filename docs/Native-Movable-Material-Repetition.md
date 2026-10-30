# Native movable material repetition

## 1. What the original bridges actually do

Original detailed movable bridges often repeat **a finite region inside an atlas by repeating its UV traversal on adjacent physical mesh panels**. The physical panels are distinct triangles at different positions. At their shared edge, duplicated vertices can have the same position and bone but different UVs. The next panel restarts the same atlas region. This is different from asking a texture sampler to repeat the complete atlas or to wrap an arbitrary atlas island.

The earlier description “fixed atlas UVs” was incomplete: an original mesh can retain finite authored UVs **and** reuse that finite region repeatedly along the bridge. The geometry/UV topology supplies the repetition. Some transverse mappings are finite, mirrored, folded or locally shortened; there is no single universal two-dimensional repeat rule.

BaseColor, Normal, Mask and, where present, Control are **aligned channels of one authored appearance**, not four independently positioned road layers. Original asphalt, paint and wear can already be encoded together in that appearance. The shader samples these channels with the same UV mapping, so a geometry UV reset repeats their correspondence together. A separate selected-road worldspace family blended into the base is a different mechanism, demonstrated by native network shader 581—not by the skinned Default shader.

## 2. Direct example: Draw01

Two original moving panels each cover 4 m. At their common Z=73.9609375 m edge, both use bone 2 and original Surface `97291ab7f7938fc35cc670b15f2bca36`. The first side ends at V=0.85693359375; the second starts at V=0.67822265625. U matches at both physical endpoints. The jump spans 732 texels of the 4096 atlas, corresponding to 183 texels/m over these measured panels.

Within each triangle the GPU interpolates the panel's UVs. It does not interpolate across the duplicated-vertex seam into a single bridge-length UV span. This is how that internal atlas region is reused without an island-specific shader wrap operation. It does not prove that an arbitrary extracted crop has seamless filtered edges.

## 3. Per-style original region evidence

This table summarizes **the sampled original regions**, not a complete global periodicity classification. Near means LOD0; middle means LOD1. Counts of matching triangle pairs or shared edges are not counts of repeat cells. Exact per-face arrays, physical bounds, UVs, bone ownership and source hashes remain in the per-style evidence.

| Style | Verified near/middle mechanism | Coarse LOD and limits |
| --- | --- | --- |
| Draw01 | Moving road panels have 4 m translated reuse and exact shared-edge V resets. The moving selections contain 204/184 UV-reset edges; source translations also include quantized deviations. | Separate far atlas; the tested membership has seams but no qualifying exact translated-triangle pairs. This does not prove no repetition anywhere in LOD2. |
| Draw02 | Native road/grating selection has 4 m translated reuse; 1,696/1,521 distinct selected physical triangles and 1,446/1,314 UV-discontinuous shared edges. These include more than longitudinal period boundaries. | Far selected set has 44 triangles. Each moving half-width strip traverses about 77.94 m with continuous piecewise UV across internal boundaries, rather than 4 m resets. Different triangulation or pixel repetition is not excluded. |
| Draw03 | Road/paint/wear and grating regions each have 96 triangles per near/middle LOD, 40 reset edges and measured 8 m reuse. The longitudinal island extent is 968 source texels; transverse spans differ between grating and road. | Different 512 atlas; selected long faces traverse different rectangles. The near 8 m recipe is not established for far geometry. |
| Lift01 | Near/middle floor selections contain 384/192 triangles and 176/88 UV seams. Adjacent 4 m moving panels on bone 4 reset V from 0.260009765625 back to 0.469970703125. Near has inner 2 m plus outer 1 m bands; middle merges each 3 m half-width. | Four upward-moving far faces form two roughly 80.18 m finite panels in a different 512 atlas. Near/middle transverse domains cannot be conflated. |
| Lift02 | Two **narrow 0.42156982421875 m roadbed strips**, each with 53 adjacent 4 m quads and 52 exact shared joins. Per side, 41 quads move and 12 are fixed. U resets; the transverse V span is finite. | Three finite broad quads, including a 163.9375 m moving center, use distinct far-atlas domains. The narrow-strip finding is not a claim about the whole road. |
| Lift03 | Upper main moving road repeats at measured 8 m intervals; auxiliary road at 4 m. Near/middle transverse half-width charts mirror about the center. Both sampled moving levels use bone 3. | Eight upper moving triangles form two roughly 79.96 m full-width panels using distinct rectangles; U continues across the center. The lower level has shell/underside geometry and must not be called entirely absent. |
| Lift04 | 192 oriented near/middle tiles in six strips; 268 unequal-UV shared edges and eight continuous-endpoint mirror folds. Common moving length is 3.806640625 m, with other native lengths; fixed upper panels are 4 m. | Three moving panels of about 53.233 m use different finite atlas rectangles. Partial near traversals and mirror folds prevent a blanket 4 m/global-affine rule. |
| Lift05 | Near/middle adjacent panels commonly span 4 m. Fixed, central moving and other moving strips traverse **625, 612 and 598** source texels respectively. Central U mirrors across the center. | Three selected moving charts each span 124.921875 m without longitudinal reset. End panels and middle simplifications remain distinct. |
| PedestrianDraw01 | Fixed panels show 4 m resets; moving panels are approximately 6.794 m with source quantization. The original channel carriers report Clamp, yet the internal atlas region repeats because the mesh resets UV. | Each moving leaf has two approximately 30.11 m finite charts in its separate 512 atlas. Near repeated charts must not be imposed on far geometry. |
| PedestrianDraw02 | Both original LODs have 32 moving repeated patches; 26 successive patch pairs share 52 verified physical edges, each with a 1,347-texel V reset. Lengths range 6.7939453125–6.794921875 m. | The fixed adjacent 4 m sections use different V intervals: a chosen 4 m crop is not proof of a fixed original geometric repeat. No separate third main LOD was present in the audited graph. |

The Clamp observation is particularly useful: **an atlas need not have whole-texture Repeat enabled for its internal region to recur along geometry**. Conversely, whole-texture Repeat does not prove that a particular atlas island can repeat by itself. Fresh native enum/constructor evidence confirms VT Repeat=0, Clamp=1, UDim=2 and default allocation Clamp; a specific active allocation still requires its actual registration path.

## 4. Why few material files are enough

The ten original main animated mesh/LOD graphs use three Surface CIDs each, except PedestrianDraw02 with two. These descriptors are about 591–1,091 bytes per style. Their textures are not correspondingly tiny: for example, Draw01 references 44,605,263 bytes of VTTexture carriers, 473,101 bytes of VTSurface carriers and four shared built-in window textures totalling 8,476,128 bytes. Original near/middle often share identities, and existing native pixel assets are reused rather than written anew for every placed bridge.

Native MidMips are shared allocations. Draw01's sampled original main graph uses 752,640 bytes of selected BC7 mid-mip blocks inside 7,318,080 bytes of touched shared tiles. Neither that touched-tile count nor the whole game cache is an exclusive per-bridge file size. Descriptor count, distinct texture CID count, physical entry size and generated-owner persisted cost are separate quantities.

The native visible moving deck is a procedurally animated object with its authored materials. Conditional network sections hide static render layers while retaining traffic connectivity. Fixed visible NetPieces retain the network shader/context. Original placement therefore does not demonstrate an operation that dynamically converts any selected road's independently sampled layers onto the moving object.

## 5. Native shader sampling evidence

The detailed static audit freshly re-extracted eight D3D11 programs from the installed
`resources.assets` and matched their bytecode to the inspected disassembly. It did not capture
which variant a running game frame selected.

- Shader 571 procedural vertex program `571-2170` transforms position, normal and tangent
  using native bone data, then forwards UV0 unchanged (lines 182–183).
- Default non-VT fragment `571-2701` computes `p = UV0 * scale + offset` and samples its
  aligned material channels using those coordinates (lines 103–115). Texture-global wrapping
  does not supply arbitrary atlas-island bounds.
- Default VT fragment `571-2683` computes derivatives before addressing, selects whole-texture
  `frac` for Repeat or boundary clamp for Clamp, transforms into the registered virtual atlas,
  and samples physical pages using explicit derivatives. Later page-address `frac` operations
  are not authored atlas-island repetition.
- Current native `VTAddressMode` is Repeat=0, Clamp=1, UDim=2; `VTAtlassingInfo` defaults to
  Clamp. `VTDatabase.GetTextureParamBlock` describes the complete registered texture allocation.
  A texture carrier's wrap field alone does not establish the active allocation mode.
- Network vertex `581-106` evaluates source V as `V >= 0 ? L * V : frac(V)` using its native
  composition mapping scalar, while retaining U. This is a vertex operation before interpolation.
  Fragment `581-133` samples a separate world-XZ family and blends color, transformed normals
  and corresponding mask channels using base-mask blue. This is not equivalent to compositing
  two already-lit meshes or substituting that shader on an arbitrary skinned object.

The 29 audited main/LOD Surface identities include non-VT window template 2/shader 550,
which has separate interior mapping semantics. All non-window body Surfaces use shader 571
with VT: template 1 for Draw01–03, Lift01–03 and PedestrianDraw01; template 0 for Lift04/05 and
PedestrianDraw02. Body UV scale/offset inherit native (1,1)/(0,0) defaults; no body Surface
UV transform override was found. Active body stacks are 4096 square, with separately authored
512-square far stacks where present. Base/normal/mask/control are aligned appearance channels,
not independent road layers.

UV resets prove authored mapping reuse, not seamless filtering. Mip and anisotropic footprints
can reach outside a chart even when sample centers are inside it. Full source mip content,
normal frames, padding and actual filtering must support any seam-equivalence claim. No new
period-detection tolerance, independent-layer rendering capability or generated-output acceptance
is established by this evidence.

## 6. Generation design is a separate assessment

The generation scheme reuses non-road originals, composes/precompose outside the games only prototype-supported
road appearance types, and retains original materials for unsupported road types. Its
[implementation assessment](Movable-Nonroad-Material-Reuse-Assessment.md) is separate from the
native findings above and does not establish implementation acceptance. The
[general source-mapping rule](agent-contract/BRIDGE_GENERATION_CONTRACT.md#preserve-original-uv-projection-and-mapping-mode)
applies to both fixed and movable bridges; the material contract supplements it.

## 7. Evidence and reproduction

The retained read-only evidence is under `artifacts/layer-decoupling/` (development artifacts,
not shipped material payloads). Per-style `outputs/native-repeat-mechanism/` directories hold
source hashes, exact face/UV/bone records, Python audits and English findings; Lift03's English
explanation and schematic are in `audit.json`. Shared outputs contain:

- `shared/outputs/native-archetype-audit/`: bounded graph inventories, native CIDs, carrier byte
  counts, descriptor hashes and selected MidMips block/tile accounting. The unrestricted graph
  includes UI/unlock references and must not be treated as a bridge footprint.
- `shared/outputs/native-repeat-mechanism/`: `programs.json`, current main/network Surface
  sampling records, selected DXBC/disassemblies, current native VT loader types and evidence hashes.
- `Draw01/outputs/native-repeat-mechanism/native-road-uv-reset.png`: a geometry/UV illustration,
  not a rendered material or filtering acceptance image.

These artifacts can be absent from a clean checkout; this document preserves the conclusions
and bounds without embedding large original assets. Reproduction requires the recorded original
game archives, native geometry decoder and Python dependencies; it does not require game
construction. Native variants, material assignments, all-LOD maps and filtered edges must be
rechecked if their recorded source hashes change. No production code or assets were changed by
this documentation integration. No player-facing text was added; runtime localization is unchanged.
