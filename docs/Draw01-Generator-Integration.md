# Draw01 generator integration

## Scope and current status

The user authorized deferring the known far-footing problem and continuing Draw01 on
2026-10-16. Other movable styles are paused for this work. This is a task-specific deferral,
not a contract change or an acceptance of the remaining geometry defects.

Draw01 now has a connected generator route. The integrated build has passed compilation and
has been installed. Actual preview, creation, native movement and cold-start persistence have
not yet been demonstrated in a human-created game sample. No movable style is claimed accepted.

## Connected construction path

1. Resolve the selected road's initialized surface width and original native render sources.
2. Create the private closed-road calculation with measured mechanism metadata.
3. Prepare all three selected-road render levels, including surfaces, markings and partitions.
4. Build the moving render graph and all LODs of its stacked column and footing.
5. Assemble the native mechanism, structural attachments and selected-road furniture.
6. Build the independent ordinary network pillar and its nine render levels.
7. Convert the cloned selected-road sections and install the native bridge placements/states.
8. Return to the existing shared preview or owned-prefab persistence/publication transaction.

The source resolver excludes other bridges' ImportedData prefabs before reading their contents.
Generated assets remain collected by TowerFactory; no separate persistence store was introduced.
The existing UI entry and localization now reach the Draw01 route. Other registered styles retain
normal error handling when their own routes are absent.

## Width basis

The native elevated surface comprises two 3 m driving pieces and two 3.5 m sidewalks: 13 m.
The two 0.5 m side sections lie outside this surface-width basis. The generator uses the
selected initialized surface width against that recorded basis, not the old 42 m catalogue
measurement, which counts alternative section entries. See the native section evidence in
`Draw01-section-role-evidence.md`. A real generated-sample width audit remains outstanding.

## Deferred footing behavior

The temporary footing path uses the retained offline vertex-coefficient candidate at every LOD,
with original topology, materials and structural Y/Z unchanged. Near and middle geometry include
the approved centred, rigid vertical railing posts. The merged far mesh still has unresolved part
mapping, central-post representation and visibility defects. It is widened, not replaced with an
unscaled native mesh. No depth correction, replacement high-detail-derived LOD, or custom shader
was added. The rejected subdivision/seam candidates were not silently admitted as validated data.

This temporary path is solely for progressing the user-authorized integration. It is not the
verified generic method requested for subsequent styles. That document must follow actual
verification and must identify any remaining limitations accurately.

## Build, installation and cleanup

- Style implementation commit: `dcb4a26` on `bridge/draw-01`, integrated as `09907e6` on `dev`.
- Shared changes: virtual generator dispatch, original-source lookup and the Draw01 route.
- Release build succeeded without changing the user-defined version.
- Installed-mod backup: `artifacts/backups/draw01-generator-route-20261016-123909`.
- Backup contains 18 original files; 12 installed payload files were hash-verified.
- Affected style: `Draw01` only. Existing export-state records contain no Draw01 UUIDs;
  no existing bridge was removed and unrelated styles were preserved.
- The game was stopped before editing and deployment. No visual-generation unit tests or
  automated game construction were performed.

## Required human verification

Restart the game, select a regular two-lane road and Bascule Bridge 01, then request preview
and Create. Retain the resulting export report and bridge identity. Inspect near/far closed
geometry, construct a supported native waterway crossing, and observe a complete opening/closing
cycle with traffic and vessels. Test saving and cold-start loading, including manager absence,
after the first creation succeeds. Failures must retain their actual stage and exception.

Human creation is required by bridge generation contract sections 6 and 14; compilation does not replace it.

## Post-integration call-chain review

Read-only review after deployment confirmed the following source connections:

| Boundary | Inspected implementation | Finding |
| --- | --- | --- |
| Dispatch | BridgeGeneratorRouter and virtual BridgeGeneratorBase.Apply | Draw01 reaches its own override; fixed-style implementations retain the base path |
| Sampling ownership | Draw01ClosedRoad.Dispose | Disposes the private network and then the sampling mechanism in a finally block |
| Surface coordinates | MovableRoadAssets and MovableSurfaceEvaluation | Appearance is evaluated in the selected-road frame before RoadToObject binding; the supplied translation matches the sampling road origin |
| Generated dependency collection | TowerFactory.Movable and TryBuildBridge | Generated renders, marking objects, mechanism, pillar and converted sections enter the ordinary owned-node list |
| Preview | TryBuildBridge finally block | Adopts partial cloned/factory graphs even when composition fails |
| Permanent output | TryBuildBridge | Saves owned prefabs, snapshots external dependencies, validates native presentation, then schedules publication |
| Operation success | Native publication callback | Export success is reported only after the native publication callback succeeds |

These are source-level findings, not evidence that native jobs or asset import have executed
successfully for Draw01. The current game log ends at 2026-10-16 02:01:06, before the new deployment;
its Draw01 catalogue entry and Draw03 preview error are historical. The last export report is
from 2026-10-14. There is no running Cities2 process and no newly created Draw01 sample available
to inspect. The next acceptance step therefore requires the human game operation described above.

## Node-composition lane-buffer failure (2026-10-16)

The human Medium Road / Draw01 preview at 13:03:51 reached closed-road compilation and failed
in MovableNetworkLanes.LanePieces. The actual exception was a DynamicBuffer index read, not a
missing generator route or a footing conversion error.

Native Game.Prefabs.NetGeometryPrefab.Initialize constructs the node composition archetype
before adding NetCompositionLane for the edge archetype. NetCompositionSystem accordingly
initializes composition lanes only when that buffer is present. Draw01 samples start/end-node
surfaces, so reading NetCompositionLane on those compositions is invalid. The installed release
Entities assembly omits collection safety checks, which explains the unhelpful null-reference
exception rather than a missing-buffer diagnostic.

Repository-wide reference inspection established that LanePieces and LaneOccurrences had no
consumer: the latter was only assigned to the compiled result. Removed that unused derivation,
its intermediate surface field and its result field/constructor parameter. This is not a
missing-buffer fallback. Real lane extraction still executes LaneVisuals(laneOwners, origin)
and MovableLaneGeometry.Compile; Draw01 road/marking partitions and source-face correspondence
remain intact. The straight-road compiler's named sourceFaces argument remains compatible.

The change is shared sampling infrastructure on dev; the only currently connected movable
route is Draw01. Fixed bridges do not call this path. No Draw01 UUID was found in export state,
so no generated bridges were removed. Other movable implementations remain paused.

Release build passed and the installation was replaced after forced game shutdown. The backup
is artifacts/backups/draw01-node-lane-buffer-20261016-130804, containing 18 originals only;
12 installed files were hash-verified. No visual-generation unit tests were run. A new human
Medium Road / Draw01 preview is needed to establish the next runtime result; this repair does
not prove full creation, movement or persistence acceptance.

## Small Road structural-layer and attachment repair (2026-10-16)

The 13:38:40 human Small Road preview passed closed-road compilation and reached partition
selection. It failed on Elevated Side Piece 0.5, geometry e0632e804955bbb9da5988a6b0bd7ffa:
the sampler incorrectly submitted ordinary road structure as selected-road surface geometry.

Draw01 now explicitly supplies Bottom | Side as the native layers replaced by its bridge
structure. The shared sampler accepts that caller-owned mask, defaulting to no exclusions for
other callers. Filtering uses the native piece-layer enum, not names or spatial inference.
Occurrence indices and source-face arrays remain aligned; Surface-layer curbs, medians,
carriageway and pavement remain in conversion. Top is not silently discarded. Native traffic
and lane visuals remain separate. The existing Draw01 mechanism supplies the replacement
structural geometry; no arbitrary carriageway role was assigned to the rejected side mesh.

The same runtime log reports Small Road's elevated surface as two Car Drive Piece 3 and two
Sidewalk Piece 3.5 occurrences, totaling 13 m. Inspection of retained native road-graph records
120790 and 120789 confirms that Sidewalk Piece 3.5 and its NoTram wrapper share geometry. Their
object producers differ: the ordinary wrapper uses traffic-sign rule 0 and streetlight rules
4/5, while NoTram uses 0 and 2/3. Added the exact ordinary producer identities with the existing
geometry identity check. Native positions, rotations and selection conditions are preserved;
this does not claim tram-pole compatibility.

Style commit 10d2649 was integrated as 165b1d1. Shared sampler and source-role changes were made
on dev. Both increments passed Release build and deployment. Final backup:
artifacts/backups/draw01-small-road-producers-20261016-134050 (18 original files only).
All 12 installed payload files matched their build output. No Draw01 UUID exists in export state,
so cleanup removed no bridges. Fixed styles and other movable callers retain their previous paths.

The reported missing structural-piece role is corrected in code. A new human preview is still
needed to establish subsequent runtime behavior; full creation and animation remain unverified.

## Native underground lane representation repair (2026-10-16)

At 13:49:26 the Small Road preview reached lane partition selection and rejected Water Pipe
Lane 1. The sampler had included underground-view pipe meshes in the visible moving-road batch.

Read the installed native resources directly: Water Pipe Lane 1 Mesh (extraction ID 120303)
and Ground Cable Lane 1 Mesh (120359) both carry UndergroundMesh with IsSubPipeline=true,
IsPipeline=false and IsTunnel=false. Native MeshSystem transfers those flags to Pipeline and
SubPipeline render layers; RequiredBatchesSystem routes such meshes away from the default
road layer. These extraction IDs are evidence only, not runtime selectors.

MovableLaneVisuals now excludes native Pipeline/SubPipeline render parts before geometry,
material and bone conversion. It checks the actual UndergroundMesh component rather than lane
names, coordinates or a blanket UtilityLane classification. This applies to both extraction
entry points. Fences, catenary, normal lane meshes, decals and tunnel-only meshes are not
excluded by this rule. Source ownership is checked first. Native utility lane entities,
connectivity and selected-road sections are unchanged; the filter affects only the generated
visible road representation. No exception is swallowed and no default motion role is invented.

Release build succeeded. Backup: artifacts/backups/draw01-native-pipeline-visibility-20261016-135102,
with 18 originals; 12 installed files were hash-verified. No Draw01 UUID was present in export
state and no existing bridge was removed. Draw01 is the only connected consumer affected by
this shared conversion change; fixed bridges do not use it. Other movable styles remain paused.
A new human preview is required to verify subsequent execution; successful compilation does
not establish creation, animation or cold-start persistence acceptance.

## Curved deterioration decal and sampling repair (2026-10-16)

The 14:03:21 failure for tmp429098c9-5192-4776-812f-10f04524fbe4 was caused by
CarLane_e92cf085d2bacee448d09b477491aaaf, native template 19
(CurvedDecalDeterioration). The pixel reader admitted templates 20 and 32 only.
This was not an infoview-only mesh: installed Car Lane 3 Mesh (resource ID 120463)
has DecalProperties, CurveProperties and LodProperties, without DilationProperties.
It must not be removed from the visible moving road.

The reader now accepts template 19 for newly composed road lanes. Installed native
BatchDataSystem initializes CurveDeterioration to zero when no lane wear is supplied.
The private generation context creates fresh lanes; it does not import an aged city lane.
Native shader 578, fragment program 35, samples the ordinary texture coordinates and
coordinates shifted by 0.5 in U, then blends by CurveDeterioration. At zero wear this
selects the first sample, using the existing projected texture area and material channels.
Evidence is retained in work/bridge-research/template-19.json, shader-578-parsed.json
and shader-programs/578-35.asm in the chat workspace. No custom shader was created.
This establishes the fresh-road conversion only; dynamic aging on the moving object
has not been verified and is not claimed by this change.

Two avoidable CPU costs were removed:

- Compute the road page's normal matrix and orientation once per page, instead of
  in every texel evaluation. The frame and the evaluated formulas are unchanged.
- Treat the decal page size as a ceiling. Choose each receiver's power-of-two page
  from its span and requested texels per metre, with the existing padding and ceiling.
  Previously even tiny receiver faces allocated and scanned a full 2048-square page.
  Texture density is retained; sampling does not move structural vertices or change Z.

Compatibility review covered both lane extraction entry points, the decal pixel reader,
receiver sampling, page-size consumers, material writer and road atlas evaluator. Draw01
is the active generator using this path; fixed bridge generation does not call it.
Other movable styles remain paused. No existing movable bridge UUID was present in
export-state.tsv, so no existing bridge was removed.

Release build succeeded and deployment backed up 18 original files under
artifacts/backups/draw01-deterioration-decal-20261016-141112. All 12 installed payload
files matched the build hashes. The game was confirmed stopped before editing and deployment.
No visual unit test or automated game construction was run. Actual preview timing,
subsequent generation, appearance, animation and cold-start persistence still require
human in-game validation. The log does not isolate the full duration of the reported stall;
no measured speedup or complete bridge acceptance is claimed.

## GPU pixel processing and native lamp stations (2026-10-16)

The next human attempt completed all three road-appearance LOD passes before failing at
14:24:06: StreetlightSingle03 at object-space (4, 10, 0) lay in the measured central
road gap. The log recorded appearance milestones at 14:20:58, 14:23:27 and 14:24:06.
Those timestamps include intervening work and do not isolate GPU or CPU stage timing.

The user's explicit GPU request is implemented with generation-only compute kernels:

- Road atlas barycentrics, texture filtering, world-space material blending and normal encoding.
- Curved decal projection, receiver clipping, material sampling, opacity/end fading, normal
  frame conversion and final native albedo/normal/mask channel encoding.
- Native serialization remains CPU/engine work. Uploads, GPU readback, geometry partitioning,
  arrays passed to native asset writers, compression and saving remain synchronous.

MovableGpuBake dispatches the Road and Decal kernels. There is no automatic CPU fallback.
GPU support or bundle/kernel failures surface through the existing failure path. The previous
C# reference equations remain available for inspection but are no longer called by the
production atlas/receiver pixel loops. The shader source and reproducible Unity bundle build
script are under tools/GpuBake. The bundle is embedded in BridgeBuilder.dll; generated
bridges retain native Default/DefaultCutout/DefaultDecal surfaces and never reference the
manager's compute program. This changes the construction calculation, not engine shaders,
animation or the saved bridge's runtime dependencies.

Resource ownership is bounded per evaluator/page. Owned upload textures and compute instances
are destroyed immediately on disposal, including failures, rather than accumulating until
this synchronous generation frame ends. Compute buffers are disposed explicitly. Logs now
identify the GPU/API and report each road/decal pass's combined bake/readback/native-save time.
These are combined wall-clock timings, not isolated GPU profiling results.

The user separately approved native longitudinal streetlight mounting stations. Draw01 now
places selected-road lamp rows at the archetype's recorded z=-60, -20, 20, 60 stations,
retaining the selected model, lateral position, height, rotation, group and probability.
Repeated incident-node candidates for the same source producer and lateral row retain the
first selected variant in native order. Other attachments retain their native selection path.
Source-rule correspondence is carried into the existing measured bone partitioner; no gap
assertion is removed. Structural mesh positions, including Z, are unchanged. This is a
specific approved furniture placement change, not a general depth-correction exception.
The style change was authored on bridge/draw-01 (c05b967) and integrated into dev (3b6363a).

Only Draw01 currently routes through this implementation; other movable styles remain paused.
No movable bridge UUID was present in export state and no existing bridge was removed.
Human preview, measured speedup, open/closed appearance, traffic, shipping and cold-start
acceptance remain outstanding. Successful shader/C# builds are not bridge acceptance.

Final verification: both kernels loaded and reported supported on the local NVIDIA GeForce
RTX 5060 / Direct3D11 in the Unity editor, with 8x8x1 thread groups. The resource check did not
create a bridge or establish visual equivalence. The installed game's Player.log reports
Unity 2022.3.71f1; the available build editor is 2022.3.62f2. Loading in the game remains part
of the next human retry. The build project explicitly includes the built-in AssetBundle module;
the first resource check caught that module's omission and the bundle was rebuilt before final
installation. Shader compilation uses StrictMode.

Final Release deployment: artifacts/backups/draw01-gpu-verified-20261016-143920,
18 original files backed up and 12 installed payload files hash-verified. The embedded GPU
bundle was also checked byte-for-byte against assets/bridgebake. No visual unit tests or game
construction automation were run. Restart the game and retry Small Road / Draw01; inspect the
new per-pass timing and any subsequent failure before claiming complete support.

## Performance and footprint revision (2026-10-16)

The subsequent optimization replaces the earlier synchronous, per-LOD/per-triangle bake path.
Identical road appearances now share resources across LODs; same-bone coplanar marking faces
share tiled pages. Sources remain on GPU, half encoding and constant reduction run on GPU,
and bounded readback queues overlap evaluation with native saving. Constant textures are
shared within the bridge, and road atlases group actual source surfaces and crop unused space.
Native asset mutation remains thread-affine. No LOD or structural Z modification is introduced.

See [performance and size evidence](Movable-Bridge-Performance.md) for the measured old 132.134 s
bake total, implementation details, final build/deployment verification and the unresolved
complete dependency-size comparison. A successful pre-change preview was observed in the log;
new-build timing, full permanent creation and size acceptance still need a human-created sample.
Bridge generation contract section 24 now requires comparable total size including dependencies.
