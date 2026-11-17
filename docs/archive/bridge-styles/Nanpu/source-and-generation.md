# Nanpu Bridge source and implementation evidence

## Source identity and scope

Inspection date: 2026-11-16. Direct provider: Paradox Mods 162995, installed revision 1.
The source listing is [mod 162995](https://mods.paradoxplaza.com/mods/162995/Any).
The official SDK title, verified by the integration owner, is the literal source record
`上海南浦大桥` (Shanghai Nanpu Bridge). The same literal is the native StaticObjectPrefab
name. The player-facing proper name is Nanpu Bridge, localized independently of that source title.
The user confirmed that the authors supply no dimensional parameter record; the values below
are measurements of the installed geometry, not claims about its real-world counterpart.

The archive is `nanpudaqiao_4b1e2538fca4146f3ea68204c062ba6d.cok`, SHA-256
`7d2dc6a610141291d5146effd2d899bc8d32d95adfadf99241f035f69aa69e11`.
Its root references one render at identity transform, `nanpudaqiao Mesh`, CID
`08f664cdd47e916faeba066d2d34bc36`. Its LOD chain contains `nanpudaqiaoLOD1 Mesh`
and `nanpudaqiaoLOD2 Mesh`, with zero bias and shadow bias. The native icon CID is
`5967dab8848da280bfb480e2ad302717`. The complete source component inventory is
SubObjectDefaultProbability, RenterObject, EditorAssetCategoryOverride, UIObject and
ObsoleteIdentifiers; no nested objects, road network, fee or unlock graph exists.
The high render contains EmissiveProperties, LodProperties and ObsoleteIdentifiers.
Derived render creation retains applicable source render components and original appearance.
The generated style uses the packaged `coui://bridgebuilderui/BridgeNanpu.svg` icon;
the source icon identity above remains provenance.

## Source material observations

Inspection on 2026-11-17 found valid template-0 Surface descriptors at all three LODs.
Template 0 is the native Default material family, not a source-mod custom shader.
The archived native template record identifies `BH/SG_DefaultShader` (shader path ID 571).
The installed archive contains no shader or program bundle.
All declare virtual-texture metadata and `_TANGENTSPACE_OCTO`; that metadata does not
imply a packed VTSurface payload exists.

| Source level | Surface CID | Packed VTSurface | Direct texture properties |
| --- | --- | --- | --- |
| Near | `34e2e98c2c62afe9d3c9f9f2ecc8ff60` | No | BaseColor, Normal, Mask, ControlMask, Emissive |
| Middle | `98c54b8f5c400a363f308d8b639d634e` | No | Same five source identities as near |
| Far | `bfdeb60c101ac5fc6a371d61dc0d7135` | Yes | BaseColor, Mask, Emissive |

The direct properties above are serialized authoring bindings. Native VT-stack properties
use their VTTexture/VTSurface payloads instead; a direct original texture identity is not
necessarily a separate payload dependency. The installed archive contains five VTTexture
files, one VTSurface and one MidMips cache. The shared dependency writer parses Surface
references and copies required external payloads byte-for-byte with their CIDs into each
owner, retaining game-native programs as references. This is a traced persistence path,
not a completed Nanpu owner-snapshot or absent-source cold-load acceptance test.

The original network-material conversion incorrectly required packed VTSurface input,
rejecting the valid near and middle sources. The authorized non-Network native-template
conversion applies to the single whole-course cable piece at all three LODs. The eight
rigid pylon roles retain their original Surface.
The source Default object protocol is converted to native NetCompositionMeshLit for the
network-composed cable geometry; this is not a Bridge Builder custom shader. No shader
program is copied into an owner. The selected road retains its own native materials.

The shared material implementation must handle both the observed standalone VTTexture
and packed VTSurface forms while preserving channel identities, native headers, mip/cache
relationships and appearance. Changed descriptors have generated identities; required
unchanged external resources remain byte-identical independent owner snapshots. These
source observations and caller routing do not certify the shared conversion implementation
or in-game appearance. Ordinary preview, native placement and cold-load acceptance remain
separate from offline geometry and descriptor checks.

## Measured construction roles

This source is a single H-shaped pylon, two foundations, raised lettering and four cable fans
(two sides, each extending in both longitudinal directions). It contains no road deck, approaches,
sidewalks or road paint to remove. The selected native network supplies all roadway behavior.
No invented approach sequence or substitute deck mesh is introduced.

Coordinates are metres, X transverse, Y vertical, Z longitudinal. The cable-foot plane is Y=48;
the virtual construction frame subtracts 48 from source Y and retains X/Z. Total high-detail
bounds are X=[-20.32763671875,20.32763671875], Y=[approximately 0,155],
Z=[-203.89453125,203.89453125]. The cable envelope itself is Z=±203.70703125;
only near anchorage shoes extend the extra 0.1875 m. The reference normalized piece length is
407.4140625 m. This is a one-pylon fan module, not a documented bearing-to-bearing main span.

The virtual road reference is the measured clear envelope between the cable-foot inner boundaries,
X=[-11.67041015625,11.6796875], width 23.35009765625 m. The complete structural width is
40.6552734375 m. No authored pavement exists, so this envelope must not be described as a
published roadway width. The small source asymmetry is retained rather than silently centered.

The central pylon, footings and lettering remain rigid in longitudinal dimensions. Columns and
side cables translate transversely. The upper and lower crossing beams use their measured inner
column interfaces; their source contact vertices remain shared with the column displacement.
Membership is identified offline and emitted as exact source face/vertex indices, including all
LOD representations. There is no runtime geometry classification or intermediate Network asset.

| LOD | Original vertices | Original triangles | Cable/anchor triangles | Pylon/foundation/lettering triangles |
| --- | ---: | ---: | ---: | ---: |
| 0 | 6292 | 4300 | 2784 | 1516 |
| 1 | 3462 | 2164 | 652 | 1512 |
| 2 | 1348 | 486 | 354 | 132 |

Every original face is retained with full oriented area coverage. The current cable piece retains its original faces without the former core cuts.
Tower faces are subdivided only at the measured member interfaces needed for native collision. Each resulting vertex records
its original triangle and barycentric weights; channel interpolation precedes the recorded affine
frame transform. Original corners retain their source channel values. Packed normal/tangent
channels require decoding and re-encoding, rather than interpolation of packed integers. The
geometry CIDs, hashes, bounds and output triangle counts are in [measurements](measurements.json).

## Current single-course representation

On 2026-11-17 the user authorized one native edge per whole module, with relocated cable
attachments, to avoid additional internal nodes affecting lane-change simulation. The complete
positive and negative cable fans, including their road anchor shoes, translate by
-3.316650390625 m and +3.316650390625 m respectively. Every LOD has the same source upper-cap
range, absolute Z=3.2225341796875..3.4107666015625; its centre therefore moves exactly to Z=0.
Rigid translation preserves each fan's shape, normals, UVs and all source faces. This intentional
attachment adjustment supersedes the earlier source-coordinate identity only for these fans.

The new lower-anchor reference interval is 400.78076171875 m; original anchor-shoe extensions
remain. One Edge network piece uses that reference and ordinary native normalization. Bridge
SegmentLength is the same whole interval, FixedSegments is empty, and there are no Front/Back
role requirements. Native long-course repetition remains possible, but there is no artificial
fixed tower-core edge or extra node inside each module. Eight pylon objects share EdgeMiddle of
the same edge; they add no network nodes. Native root placement uses t=0.5 and yaw-only
GetNodeRotation, preserving the vertical pylon on a graded road.

The [collision investigation](tower-placement-and-collision.md) records the measured eight-role
pylon representation and why all root object Y positions remain zero. Each visible role retains
PillarType.None and UnderwaterObject.AllowDryland, its actual oriented geometry bound, and its
source material. Neither collision filtering nor the selected road's water rules are disabled.

The [current offline check](single-course-geometry-check.json) reads the actual emitted resource
files and original source meshes. All LODs retain oriented source-face coverage; reconstruction
error is below 0.00001 m. At length factors 0.6,1,1.4 and grades -0.2,0,0.2, all relocated upper-cap
vertices remain inside the actual source tower triangles, including their nonzero cap thickness.
These straight-grade sample calculations are not native curve execution or visual acceptance;
independent native integration and player placement/cold-load checks remain separate.

## Historical core/flank representation and acceptance

The following records the superseded development representation and its original measurements.
It is retained as evidence, not the current generation recipe.

The native consumer rules are recorded in the
[native length audit](../../audits/native-bridge-length-handling-audit.md). Ordinary whole-piece
normalization was rejected: upper cable anchors at Z approximately ±3.23..3.269 would move
outside the rigid pylon faces at Z=-3.316650390625/+3.3638916015625 when stretched. The
implementation instead uses a measured stationary tower core and two variable cable flanks.
This is a tower joint, not an approach; no approach exists in the source.

The rigid pylon footprint extends to Z=6.8599853515625. The symmetrical core therefore has
length 13.719970703125 m; each reference flank is 196.8470458984375 m. Cable geometry is
clipped at the two core boundaries for every LOD. Core cable stubs and the pylon retain their
longitudinal coordinates, while the outer fans normalize between the core and their outer tips.
Front, core and back each occur once. The native mixed fixed-segment sequence records zero
length for the two variable entries and the measured positive core length. The bridge nominal
segment length is one flank reference, with minimal-length mode disabled. Normal initialized
flanks range from 0.6 to 1.4 times that reference, while the core stays fixed. This claim excludes
invalid undersized native courses where validation can relax segment limits.

Native composition reverses local X/Z. The positive source-Z fan receives Front; the negative
fan receives Back. Core geometry requires neither state. The pylon uses one root NetSubObjects entry with EdgeMiddleFixedSegment placement and
fixed index 1, the measured core in the three-entry sequence. A Y-axis half-turn matches
the native network frame. The earlier core NetPieceObjects attachment is removed; unrestricted
root EdgeMiddle placement would repeat on the flanks. PillarObject with type None gives the
rigid source-derived tower structural collision semantics without applying native Standalone
top/ground alignment, which would change its measured deck datum. Structural placement
permits water and dry land; the selected road's own water rules remain unchanged. The selected
network's unlock graph is normalized by the shared first-save path. Fixed proper-name
presentation is integrated centrally.

[Offline checks](geometry-check.json) establish all-LOD source-face oriented area coverage,
clipped vertex provenance, reference reconstruction, affine frame consistency and native
straight-course alignment for four widths and three flank length factors. Maximum coordinate
error is below 0.000008 m. The core boundaries and upper anchors remain stationary relative to
the rigid pylon; each flank boundary meets its corresponding core boundary. These numerical
checks do not establish native material rendering, placement, lane continuity, sloped course
behavior, proper-name map labels or cold loading. Game acceptance remains separate. Existing
saved bridges have not been changed or removed.

## Reproduction

Run `tools/GeometryMetaprogram/Nanpu.py <installed-archive> <Cities2_Data> <output>` to regenerate
the 245 numerical resources and measurements. Run
`tools/GeometryMetaprogram/NanpuCheck.py <installed-archive> <Cities2_Data> <recipes> <report>`
for independent source/index/affine checks. Supply installation paths explicitly; no developer
absolute path is encoded in shipped source or numerical data. Native decoding uses the installed
PipelinePlugin and temporary files removed on exit. Neither command starts the game.

The [tower placement and collision investigation](tower-placement-and-collision.md) distinguishes the earlier StandingObject attempt from the current independent member shapes and road-datum origins.
