# Draw01 native network and finite terminal sampling

## Native fixed-base context

`Draw01FixedBase.Apply` constructs all five fixed-base families as native
`NetPiecePrefab` graphs. `Piece` carries the original near render and its complete
`LodProperties` chain into `CreateMovableRenderGraph`. The reduced levels are
`RenderPrefab` instances; their native network capability comes from the owning
graph, not their individual runtime type. These plans have no converted road or
skeleton. The native composition still supplies longitudinal distance and phase.

`EmitNetworkSampling.py` follows the recorded source graph and verifies the current
archive bytes for all 13 render/Surface slots and their Geometry assets. Every
Surface uses native template 17. It emits the exact render, mesh and Surface-CID
predicate in `Draw01NativeMaterials.IsNativeNetwork`. The shared caller must also
check fixed native network graph capability and the loaded Surface template before
retaining encoded channels and the original Surface. The predicate cannot authorize
the moving mechanism or an arbitrary object using the same texture.

The earlier observer recorded 702 unresolved uses across three near renders:
416 `DrawBridge01Bottom300cm Mesh`, 96 `DrawBridge01BottomNode300cm Mesh`, and
190 `DrawBridge01BottomIntersectionMiddle200cm Mesh`, all mesh 0. The gate also
covers both undeformed outer families and every reduced LOD, even though those
slots were absent from that changed-unit capture. The exact Surface and Geometry
identities and native components are in
`artifacts/material-resume/Draw01/native-network-sources.json`.

## Finite terminal faces

`FiniteTerminalEvidence.py` examines the complete original near and middle meshes,
independently of generated receiver failures. The only negative-V source faces in
the middle structural mesh are 953, 1375 and 1376. They are the finite terminal
underside footprint, represented by domains 757, 1028 and 1029. Original near
triangles cover those physical footprints; the middle mesh combines or changes
their authored UV mapping. None of these vertices belongs to the underside UV
correction table.

These coordinates belong to the ordinary template-1 object VT atlas, not template
17's signed-V encoding. Their finite mapping reaches the atlas-clamped V=0 edge;
expanding an extracted repeat region would replace that original behavior. Keep
their actual authored UVs and original Surface, including the source LOD's UV
differences. No source image, crop, mip, topology or position is modified.

The sampling producer now enables source retention for exactly these three domains.
The binary remains 91,928 bytes. `flag-transition.json` records exactly three 0-to-1
changes; the other 817 repeat-classified domains retain their previous flags.
`finite-terminal-faces.json` records source UVs, physical triangles, near
correspondence and input hashes. This is physical/source sampling evidence, not a
claim that an arbitrary atlas overlap establishes non-tiling behavior.

## Verification and integration limits

Native asset identities must be compared using `SurfaceAsset.id.guid.ToString()`.
`Identifier.ToString()` appends ` [uri]` when an asset has a URI; it is a diagnostic
representation and cannot match a bare CID. A GUID-only fixture hid this difference
until the actual Fixed Base 0 preview rejected a registered original Surface and
requested the obsolete Structure payload
`021869598a9e3a57c97e51c649ad25da40e8855bd0483f42583b2c91cb9dd757`.
The Draw01 object-descriptor resolver now uses the GUID for both source and
converted identity checks. The shared native-network consumer needs the same
identity rule; exact render, slot, graph and template checks remain mandatory.

`PrepareNativeNetworkCheck.py` verifies and extracts all 13 source Surface headers
and geometries from the current original COK. Its `inputs.json` records source
URIs, family/LOD assignments and hashes. Each `.mesh` fixture stores little-endian
vertex/index counts, float32 positions, untouched half UV0 and int32 indices.
Native integration must construct identifiers with non-null URIs and exercise
the actual plan/build/append/Unity mesh path; a GUID-only predicate test is
insufficient. Fixtures for the preview repair are under
`artifacts/draw01-fixedbase-fix/Draw01/outputs/native-fixture/`.

The archive identity checks, exact three-flag transition and deterministic
regeneration pass. The native gate and finite retention introduce no new material
or texture asset. Existing geometry, correction resources, repeat units and UI
localization are unchanged.

The 702 network and 63 finite observations identify the expected affected set;
they are not a replacement for replaying the current production path. Shared
integration must verify native graph capability through every LOD, final channel
preservation, original Surface binding, and the changed-unit observer. Preview,
saved rendering, motion, cold loading and generated-owner material bytes remain
separate acceptance measurements. No game sample was constructed or installation
changed by this bridge-specific work.

Reproduce with the project's Python dependencies (NumPy, Shapely and
texture2ddecoder):

```powershell
python tools/PreparedMaterials/Draw01/EmitNetworkSampling.py
python tools/PreparedMaterials/Draw01/FiniteTerminalEvidence.py
python tools/PreparedMaterials/Draw01/BuildUvSampling.py --output artifacts/material-resume/Draw01/non-tiling
```
