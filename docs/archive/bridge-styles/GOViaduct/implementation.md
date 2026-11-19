# GO 224 m Arch Viaduct implementation evidence

## Evidence scope

This record is restored for the user-requested GOViaduct support restoration. The measurements, fixture results and deployment descriptions below retain their original November 2026 development scope; they do not establish verification or deployment of the newly restored build. Current material-program ownership and directory-mapping requirements remain unchanged. Historical release records are not amended by restoring this evidence.

## Initial restoration: offline geometry verification

The current restoration returns one generator, 73 numerical resources and two geometry tools. The offline check against the installed `166012_1` native source package passed across all three LODs. It covers disjoint structural face coverage, original road-face exclusion, source provenance, coincident seams and rigid endpoint dimensions at lengths 109.2, 182, 254.8 and 400 m, and widths 22.39990234375, 8, 32, 64 and -4 m. The [current geometry receipt](../../../../work/go-restoration-geometry-check.json) records the actual per-LOD results.

This is offline source/recipe geometry evidence. Native gameplay, preview appearance and independent cold loading were not tested in this restoration check. Deployment descriptions in sections 4 and 6 remain historical and do not describe this restored build. Current programs remain mod-owned; the owner-local dependency snapshots described below concern ordinary native asset dependencies, not persisted shader programs.

## 1. Source and dimensional meaning

Source: GO 224m Viaduct Bridge, Paradox mod `166012`, installed package `166012_1`.
The [catalogue URL](https://mods.paradoxplaza.com/mods/166012/Windows) was not retrievable
by the web tool. The following description was supplied directly by the user:

> The asset dimensions are 224m long, 32m width ~24m Lane Width, 164m span

The immutable input is `GO-224m-ViaductBridge_64a34723e1f227e119d6dc6cfe7653f6.cok`.
Its SHA256 is `ea2fae6e6ceecd355dda35d9e9685e998d2fd2c6ee58321f8b69b6fd02b9c9dd`.
The root is a StaticObjectPrefab with one identity-transformed render and two external LODs.
It supplies neither a network nor fees, lanes or unlocks.

| Parameter | Description | Measured source meaning |
| --- | --- | --- |
| Overall length | 224 m | Source X from -112 to +112, including masonry ends |
| Clear span | 164 m | Between inner foundation faces X=-82 and +82 |
| Overall width | 32 m | Outer foundation envelope Z=-16 to +16 |
| Usable reference width | Approximately 24 m | Actual full-detail running sheet Z=±11.199951171875; width 22.39990234375 m |
| Steel-deck length | Not separately described | X=-91 to +91; 182 m |
| End-cap extension | Not separately described | 21 m beyond each steel-deck joint |
| Running-sheet elevation | Not separately described | Source Y=9.5; target deck datum zero |

The approximate description is not silently rounded into a geometry measurement. The 224/164
values describe the reference source, not an invariant length imposed on every player-drawn edge.
The source-to-construction frame is `(x,y,z)=(sourceZ,sourceY-9.5,-sourceX)`.

## 2. Immutable construction and native length adaptation

`GOViaduct` is a single structural style in the Arch family, localized in all twelve languages.
Width variants do not create separate styles. Road, track and pedestrian selections enter the
existing selected-network generation path; the style adds no road-kind filter.

Offline extraction emits ordinary BBN recipes consumed directly by the existing generator.
There is no intermediate network file, virtual prefab, registration, runtime prototype factory,
or additional virtual-conversion pass. The final bridge owns real native network components.

The November 16 proportional placement repair uses three native NetPieces: the 182 m
steel-deck reference and two 21 m approach references. Their node offsets are zero. The Bridge
reference is 224 m and its ordered fixed segments are all positive: 21 m Front, 182 m middle,
21 m Back, each with count range (1,1). The middle clears Front/Back and its piece excludes
those states; each approach requires its corresponding state. The selected road spans every
role. No movable Opening state is introduced.

The native reference is the all-positive bridge family, not a dam: TrussArchBridge01/02/03
use 128 m Front + 128 m Back, and SuspensionBridge01 uses 88 + 76 + 76 + 88 m.
NetInitializeSystem gives all-positive segments proportional 0.6–1.4 length ranges;
CourseSplitSystem distributes length consistently across their ordered roles. At scale s,
the source steel joints are at ±91s and approach outer edges at ±112s. The original
0.099609375 m lip in the detailed approach geometry remains present, with native deformation.
Drawing beyond the native sequence range can compress or repeat the sequence; this is not
an arbitrary-length single-span claim.

The five-part, all-LOD partition uses complete authored source-index islands, without new
cuts or omitted structural faces. Exclusive source-vertex ranges are:

| LOD | Both approach bodies | Both rigid foundation assemblies |
| --- | --- | --- |
| Full | [0,432) | [41748,41896) |
| Middle | [184,552) | [552,700) |
| Far | [0,76), [770,782) | [76,156) |

The remaining non-road faces belong to the steelwork. Each positive/negative approach is
centered at its measured source station ±101.5 m and uses native longitudinal normalization.
Each foundation is a continuous shaped block with source extent |X|=82–108 m, including
faces spanning the previously inspected 82/88/93/108 m stations. It is not several independent
footings. The complete block stays rigid longitudinally relative to its measured steel-joint
anchor ±91 m; its native object follows that joint at center-course phase 0 or 1. Width mapping
remains the recorded measured mapping. At short scales a rigid foundation can project beyond
the scaled approach; its physical dimensions are not silently compressed to hide this.

The three network parts share one request-local converted Surface set per source LOD.
The two foundation objects retain the original object materials and native UnderwaterObject
with m_AllowDryland=true. Native initialization sets OnGround|Underwater, allowing these
structural objects on land and in water without suppressing unrelated placement errors.

A second water blocker was found in the network composition: the historical -49.501953 m
structural HeightRange minimum participated in native road-water validation despite the
Overhead section role. All three structural NetPieces now contribute minimum zero to that
placement envelope. Their maxima preserve measured above-road clearance: 30.651367 m for
the steelwork and 1.406982421875 m for approaches. Their full actual render bounds, including
deep structure, remain unchanged. The generator preserves the selected network's existing
Bridge elevation-on-water value or the native Bridge default instead of assigning the
historical 49.501953 m structural-depth-derived value. The selected road's own negative
height envelope and native validation remain effective.

The preceding restored graph had NodeOffset=21, Bridge reference length 224 and no fixed
segments. GeometrySystem.CalculateCornerOffset does not reserve 21 m unconditionally at a
free end, so its steel filled the road course and its combined rigid ends projected outside.
An intermediate repair used 21 + 0 + 21, copying a dam's mixed-zero allocation; it pinned
approach lengths and was superseded by the user-authorized proportional bridge allocation.
Neither earlier graph is the current implementation.

The earlier proportional-model check passed all three LODs, exactly-once structural face coverage,
source channels, recorded width mappings, nominal source identity, whole-road approach
bounds and steel joints at scales 0.6/0.8/1/1.2/1.4, and unchanged rigid foundation dimensions.
That check assumed an incorrect native longitudinal sign; its symmetric bounds and face counts
did not establish correct endpoint ownership. Its historical receipt is [the proportional geometry check](../../../../work/go-placement-20261116/proportional-check.json).
Native source evidence is retained under `work/go-placement-20261116/native/` and
`work/native-bridge-lengths/roots.json`. This does not establish in-game placement, preview,
traffic or cold-load acceptance of the repaired graph. No game was started or closed.

## Native orientation repair: exterior closures and foundation frames

The reported missing abutment face and transverse strip at the arch/approach joint were
compared against actual generated owner `bc2f1368b-838f-4e54-8096-bd09ee485876`
(export report 2026-11-16T15:14:57Z), using its fifteen native Geometry payloads and serialized
section/object references. At the actual 22.5 m selected-road width, every generated position
matched its recorded recipe exactly. All face winding was retained: nine center-piece triangles
were only cyclically reordered by native compression. The apparent missing faces were not
lost by the split or writer, so adding caps or removing the visible strip was not justified.

Native `BatchMeshHelpers.GenerateCompositionMesh` reverses local X and Z before longitudinal
normalization. Its actual straight-course parameter is `t = 0.5 - localZ / referenceLength`.
The earlier implementation assumed the opposite direction. It assigned positive-source-X
approach part 1 to Front and negative-source-X part 2 to Back, putting the source outer
closure faces at inner joints. The corrected assignments are part 2 Front and part 1 Back.
All original closure faces and the complete original triangles remain unchanged at every LOD.

The object path has a different frame: `SecondaryObjectSystem` samples increasing curve
phase and uses the forward tangent in LookRotation, without the network mesh's automatic
X/Z reversal. Foundation part 3 (positive source X) therefore belongs at center phase 1,
and part 4 at phase 0; both use the exact Y half-turn quaternion `(0,1,0,0)` in their native
piece-object rule. Mesh-local transforms stay identity. This reproduces the network frame
without stretching the foundation or altering its source attributes.

The independent straight-course oracle is now every-vertex correspondence:
`world = (-sourceZ, sourceY - 9.5, sourceX * scale)` for network structure. Rigid foundations
use the matching half-turn and their scaled joint station with unchanged local dimensions.
At scales 0.6/0.8/1/1.2/1.4, all three LODs pass that oracle; source exterior closure faces
remain at ±112s rather than ±91s, and the old reversed role assignment explicitly fails.
The [orientation check receipt](../../../../work/go-placement-20261116/orientation-check.json)
records this stronger fixture. Native evidence is in
`work/go-placement-20261116/native/Game.Rendering.BatchMeshHelpers.decompiled.cs` and
`work/go-unlock-20261116/SecondaryObjectSystem.cs`.

No numerical recipes, topology, material, double-sided rendering, water-placement rules,
proportional length allocation, unlock persistence or native naming were changed by this
orientation repair. It establishes native field/frame correspondence and offline geometry
behavior, not yet human acceptance of a newly generated near/far in-game sample.

## Native unlock persistence and placed naming repair

The initial GO override returned success without converting the selected network's gate.
Although this retained its source component, a fresh owner could still have
`Unlockable.m_IgnoreDependencies=false`; the existing startup migration then classified it
as needing conversion. The generator now declares `SelectedRoadUnlocks=true`. Permanent
composition captures the initialized original selected network's effective native rule through
`BridgeUnlockSnapshot` and writes it through `BridgeNativeUnlock` before the first save.
It does not snapshot the unregistered clone, substitute the static source, flatten the rule
to its current locked state, or merely flip the flag. Shared owner handling applies current
native gates to owned auxiliary networks without modifying shared inputs. The historical
self-check path remains unchanged.

GO also previously retained the selected road's `m_AggregateType`, because recorded construction
bypasses the generic donor `CopyAggregate` path. It now resolves the native
`AggregateNetPrefab("Bridge")`, following the existing ArtDecoViaduct and TrussViaduct
construction paths. Native `GenerateAggregates` supplies the aggregate PrefabRef, and
`NameSystem.GetId` reads its localization/random-localization name category. This changes
native placed-network naming, independently of catalogue LocaleAsset titles; no suffix or
runtime label replacement is introduced.

These two changes leave the proportional geometry, water envelopes, materials and recorded
numerical resources unchanged. Compilation/fixture results are separate from pending manual
cold-load unlock behavior, road/highway placed labels, locale changes, save/load and player
rename acceptance.

## 3. Geometry and appearance coverage before the crossbar repair

| LOD | Source vertices | Source triangles | Replaced road triangles | Retained structural triangles |
| --- | ---: | ---: | ---: | ---: |
| Full | 71,114 | 58,048 | 8 | 58,040 |
| Middle | 19,722 | 13,012 | 12 | 13,000 |
| Far | 782 | 548 | 6 | 542 |

In that earlier recipe, only the measured Y=9.5 running-sheet faces were omitted. All other source faces occurred exactly
once across steel and masonry; the following repair supersedes that exclusion set. Source indices and attributes remain traceable. Attribute vertices
are split where distinct measured affine differentials require separate normals/tangents; no
triangles are added. Rigid side assemblies translate laterally. Roof ties adapt between their
measured transverse connection stations while preserving the centre longitudinal chord.
Selected-network sidewalks and surface materials remain governed by the normal generation path.

Steel requires the native NetCompositionMeshLit vertex protocol. Its source Default Surface
cannot be used unchanged on normalized network vertices. The derived Surface preserves common
native appearance values and the measured fixed white/brown/white colour palette. Its five
source BC7 channels are reconstructed without pixel decoding, resampling, recompression or
mipmap regeneration, including all source MidMips and mip tails. The original object shader's
single-light metadata multiplies an entirely black emissive layer at every source LOD;
full-resolution pixel inspection found zero nonzero RGB samples.

Fifteen complete ordinary Texture payloads total 120,586,910 bytes. This is a derived texture
payload measurement, not a measured complete generated-owner size or SDK package size. The
original compressed mod archive is 70,378,286 bytes. No structural textures are added to the
mod package. Runtime-owned derivatives have new identities; unchanged external resources use
the existing owner-local dependency snapshot mechanism. Original files and CIDs are untouched.

## 4. Verification and remaining acceptance

The restored local build passed the all-LOD geometry fixture and embedded-number checks,
including byte-identical restoration of all 73 numerical resources and the three generator/tool
inputs. The twelve-locale regression now expects GO to be an active creation style.
After the user closed the game, the build was deployed to the active `160320_20` cache.
All 964 runtime payloads matched the build; only the DLL and PDB differed and were backed up
under `backups/go-viaduct-restored-20261114/160320_20`. Native Paradox metadata was preserved.
The deployment receipt is `artifacts/go-viaduct-restoration/deployment.json`.
No game was started and no release was published; in-game acceptance remains pending.

## 5. Source color capability

The installed `166012_1` archive retains the SHA256 recorded above; its mesh Prefab is
byte-identical to the source-study evidence. Its active `ColorProperties` contains one
three-channel variation: white, `(0.54, 0.396180034, 0.3564, 0.5)`, white. Bindings
for channel IDs 0, 1 and 2 all set `m_CanBeModifiedByExternal=false`, and
`m_ExternalColorSource=0`. The source Surface includes `_ControlMask`.
This establishes authored color channels, but does not establish an exposed player-facing
recoloring interaction. In particular the native bindings do not opt into external modification.
An editor or third-party tool overriding these values is a separate capability requiring testing.

The restored generator passes the measured fixed palette into
`TowerFactory.CreateRecordedNetworkSurfacesAsync`; the conversion writes `_ColorMask0`,
`_ColorMask1` and `_ColorMask2` onto generated network Surfaces. It does not add a user-selected
palette or an editable recoloring control. Source-object color metadata must not be presented as
proof that the generated network supports interactive custom colors. No recoloring system was
added and no in-game color interaction was tested.

## 6. Historical verification detail

Reproducible tools are `tools/GeometryMetaprogram/GOViaduct.py`, `GOViaductCheck.py`,
`tools/NativeVtMipCheck`, its independent `CompareSource.py`, and `tools/PreviewPieceObjectCheck`.
Retained logs and measurements are under `artifacts/go-viaduct/`.

Automated checks cover all-LOD disjoint face coverage, exact channel provenance, coincident seams,
source-width identity, widths 8/32/64/-4 m, affine normal/tangent differentials, and rigid support
sections at steel lengths 109.2/182/254.8/400 m. Thirty-five placement checks use the production
native object enumerator; endpoint joins also agree with actual native BatchDataHelpers matrices.
All fifteen complete Texture files match an independent offline reconstruction byte for byte.
The shared native mesh build/upload frame fixture passed 108 cases, including remapped attributes.
Production compilation, typed-number inventory, twelve-language checks, UI checks and icon
regression pass. Deployment hashes and original backups are recorded outside the backup directory.

These are not claims of live game acceptance. Near/far appearance, full native ECS-generated
224 m reference geometry, all selected network kinds, traffic, sloped/intersection cases and an
independent cold load require manual game verification. No game or launcher was started or closed,
no healthy generated bridge was removed, and no version or publication was changed.


## November 17 crossbar and railing-atlas repair (incomplete grate exclusion)

This addresses the subsequent three user screenshots, independently of the earlier orientation
repair. Source package and all three Geometry identities remain those recorded above. The latest
reported owner b03e92284-89be-411c-9c49-6ee9f17878e5 had already been deleted by the user;
no retained Geometry/Surface payload was available for a latest-owner UV comparison. The evidence
below compares actual decoded source data with the current emitted recipe and the inspected native
consumer. It is not a claimed dump of that deleted owner or a new in-game rendering result.

The following grate exclusion was incomplete: it removed the upper sheets but missed the
separate lower rods. The November 18 investigation below supersedes its full-detail exclusion
and triangle total. The outer-fascia and railing-atlas observations remain applicable.

The first transverse member is a recessed original roadway grate: source |X|=88.1640625..91,
Y=9.449951171875..9.5, Z=+-11.199951171875. Its relief lies below the exact planar-road
predicate, explaining why the earlier complete-face test retained it. LOD0 source faces
47924..48069 and 49980..50125 (inclusive; 146 per end) are now omitted. LOD1 represents
this area as planar road faces 3432/3433 and 3704/3705, already excluded; LOD2 merges it
into the already excluded planar running sheet. No neighboring lower steel supports are removed.

The second member is the outer roadway fascia at X=+-112, Y=8..9.5, crossing between the
side rails. Its exact faces are LOD0 42/44/204/205 and LOD1 158/160/288/289. These are
omitted while the longitudinal rails, side returns and deeper end wall remain. LOD2 merges
that fascia into end-wall faces 14/17/22/25 extending to Y=-40.001953125. Those four faces
are clipped offline at the full-detail measured Y=8 boundary, retaining six lower fragments
with original winding and barycentrically interpolated native attributes. Runtime consumes
recorded source triangle indices/weights and explicit face recipes; it performs no classification
or clipping. No cap or double-sided material is introduced. Positive-end wall area changes
from 1327.999878 to 1293.799805 square metres at LOD0, 1327.996704 to 1293.796631 at LOD1,
and 1277.121948 to 1243.689819 at LOD2; source LOD simplification accounts for their differences.

The resulting triangle totals are 57,744 / 12,996 / 544. The far total includes two additional
triangles needed to preserve the deep wall while excluding its merged upper fascia. Every
unaffected source face retains exactly one representation. The native proportional 21+182+21
sequence, orientation, rigid foundations, water envelope, selected unlocks and Bridge aggregate
are unchanged.

The striped inner rail maps to source LOD0 faces 81/82 and 141/142 (positive approach),
X=91..112, Y=9.5..10.5, Z=+-11.10009765625. Their source V endpoints are 1 and
0.9951171875. Native BatchMeshHelpers with DisableTextureTiling subtracts ceil(max V);
the inspected shader recovers negative V with frac while zero stays zero. Thus the V=1
vertex becomes zero while the adjacent vertex remains near 0.995, interpolating across the
atlas instead of the intended narrow rail strip. This is not a source texture replacement issue.

The explicit offline UV arrays substitute only network-part V==1 with 0.99951171875, the
binary16 predecessor of one. All other UV values are retained; object foundation parts 3/4
retain their exact original UV values. LOD1/2 have no exact-one endpoints, so their source UV
mapping remains unchanged apart from necessary far-wall fragment interpolation. The maximum
endpoint quantization change is 0.00048828125 normalized V. Texture channels, resolution,
source Surfaces and the existing conversion remain unchanged. The shared writer preserves the
native UV format and any extra UV components. A passing shader-coordinate fixture establishes
sampling-coordinate continuity, not pixel-perfect source filtering or live visual acceptance.

The updated GOViaductCheck passes all three LODs, exact unaffected-face coverage, excluded
member identities, far-wall winding/depth, explicit UV provenance, source-width identity,
five signed/ordinary widths and native-oriented source correspondence at scales
0.6/0.8/1/1.2/1.4. Records are [the recipe check](../../../../work/go-placement-20261116/crossbars-check.json),
[semantic measurements](../../../../work/go-placement-20261116/crossbars-semantic-evidence.json),
[all-LOD end-wall comparison](../../../../work/go-placement-20261116/crossbars-endwalls.png), and
[both grate comparison](../../../../work/go-placement-20261116/crossbars-grates.png).
These are decoded-source/offline output plots, not game screenshots. Near/far appearance of a
newly generated owner and cold loading still require user-performed native game acceptance.
Original changed files were preserved under E:/unused/CS2BridgeBuilder-go-crossbars-20261117/originals;
checksum evidence is outside that backup subtree. No game or launcher was started or closed.

## November 18 omitted lower-grate rods

The newly reported joint grille was verified against actual owner
`bef93defc-5630-456c-9450-356253dd82f7`, written on November 18 at 01:45:10 local time.
Its full-detail center Geometry had 90,635 vertices and 57,336 triangles. Every decoded
position matched the preceding recorded recipe exactly, at width half-increment
2.300048828125 m (maximum error zero). This establishes a current recipe omission,
not a presumed stale owner or an LOD substitution.

The retained lower rods are source LOD0 faces 48812..49315 and 50868..51371 inclusive,
504 per end. Their measured source envelopes are X=-90.59765625..-88.59765625 and
X=88.59765625..90.59765625 respectively, Y=9.340087890625..9.449951171875,
Z=-10.800048828125..10.800048828125. Actual owner triangles corresponding to all
1,008 source faces were present. These rods sit below the previously excluded upper
sheets; a test derived only from the old exclusion list could not detect that incomplete
semantic selection.

`GOViaduct.py` now excludes those exact additional source face IDs offline, regenerating
the recorded full-detail center recipe. No runtime geometric predicate, transparency,
LOD adjustment or source-material change is used. Exactly 1,008 triangles and their
1,272 exclusive attribute vertices are removed: center counts become 56,328 triangles
and 89,363 vertices. Whole-style triangle totals are now 56,736 / 12,996 / 544.
All other source faces remain represented. All retained full-detail center face corners
have bit-identical source-vertex identity, position, displacement, differential frame
and UV values. Nine center resources change; the other 134 numerical resources remain
byte-identical, including every approach, foundation and reduced-LOD recipe.

The regression independently identifies the complete source assembly within measured
|X|=88.1..91, Y=9.3..9.5 and |Z|<11.21, then requires both ends' 146 upper-sheet plus
504 lower-rod faces to be absent. That domain is an offline test oracle, not a runtime
selection rule. Middle-LOD faces 3432/3433 and 3704/3705 and the far-LOD planar running
sheet remain absent. All-LOD source provenance, signed width mappings, orientation,
outer-wall clipping, source UV preservation and rigid-foundation checks still pass.
The unchanged retained corner data also preserves the side rails; no broad transverse
slab or abutment has been removed.

Receipts are [actual owner correspondence](../../../../work/go-grates-20261118/actual-owner-comparison.json),
[exact retained-face comparison](../../../../work/go-grates-20261118/retained-face-proof.json),
[resource hashes and byte counts](../../../../work/go-grates-20261118/resource-changes.json),
[the all-LOD check](../../../../work/go-grates-20261118/regression.json), and
[both-joint before/after source-space views](../../../../work/go-grates-20261118/joint-before-after.png).
The plot uses decoded source faces and the owner-verified old recipe; it is not a game
render. Original tools, changed resources and this record are preserved under
`E:/unused/CS2BridgeBuilder-go-grates-20261118/originals`. No player owner was edited,
no game was started or closed, and no in-game acceptance of a newly generated owner is
claimed. The proportional graph, water and collision components, native material conversion,
unlock finalization and aggregate naming are unchanged.
