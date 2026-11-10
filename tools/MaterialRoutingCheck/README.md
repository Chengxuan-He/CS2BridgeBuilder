# Material routing regression fixtures

Run these Editor entry points in the existing isolated Unity environment with the
installed game assemblies and the candidate mod DLL. They do not start the game.
Native inputs and extracted game payloads are intentionally not committed.

- `RoadRouteCheck.Run`: `-gameManaged`, `-modAssembly`, `-sourceSurface` (original
  RoadEU), `-surfaceFile` (prepared NativeAsphalt), `-baseSurface` (corresponding
  original-base object descriptor), and `-fixtures` (eight logged triangle cases).
  Exercises actual Compose, original/no-match with empty maps, prototype priority,
  source-frame projection, and the logged domain19 attributes. The base descriptor
  is injected into the owner cache: this does not assert runtime database saving
  or rendered equality of the network and object shaders.
- `NativeOriginalCheck.Run`: `-gameManaged`, `-modAssembly`, `-surfaceFile`
  (native template17 head descriptor), `-fixtures` (native geometry attribute JSONs).
  Exercises raw two-submesh preservation and 26 native topology/frame cases through
  the real importer upload. It does not emulate native ECS composition.

All required fixture paths are explicit arguments. Process exit 0 indicates the
listed checks passed; an exception exits 1. The logged coarse-mip world-layer
contributions remain an authorized appearance difference, not an equivalence claim.

## Optional selected-road appearance capture

`RoadRouteCheck` also runs Compose with the optional capture disabled and enabled.
It compares every output triangle and material slot, including empty/original LODs,
corresponding native appearances and prepared finite/repeating axes. Captured
receivers use Compose's road-local coordinates; the caller's RoadToObject pose is
required for structural coordinates. SourceReceiver retains the input panel, while
Receiver is the actual final output triangle (an original-atlas wrap can subdivide
one panel). No source family, road branch or material is reselected by capture.

The binding records both the route and its evidence, including unknown Original
receivers. A None receiver type is not proof that a surface is paint or a rail,
nor permission to replace it with asphalt. Finite atlas sampling bounds and source
sampler semantics still apply. Affine U/V fitted on a receiver plane do not define
an independent sidewall mapping or authorize extrapolation outside that domain.


## Material operation boundaries

`MaterialBoundary119Check.Editor.cs` invokes the compiled mod in an isolated Unity Editor
with the installed game assemblies. Pass `-gameManaged <Managed> -modAssembly <dll>`
and execute `MaterialBoundary119Check.Run`. Add `-baseline` for native118. It checks
setup failures through the common creation/preview entry, failing material graph inputs,
and source-read failure followed by a failing reporting callback. It verifies false
results, empty graph output, diagnostics and one-shot callback consumption. It creates
no saved bridge and does not start the game. This fixture does not establish complete
material conversion, rendering, or every internal API's non-throwing behavior.


## Lane and cable result APIs

`MaterialResult121Check.Editor.cs` executes `MovableLaneSurface.TryCreate` in the
compiled mod against real native SurfaceAsset objects. It verifies direct reuse under
unseen identities, explicit incomplete-adapter results for network templates, and a
real truncated native descriptor read. Pass `-gameManaged`, `-modAssembly`,
`-outputRoot` (an isolated directory), `-sourceSurface` (the native TiledRoad descriptor)
and `-surfaceFile` (a prepared object Surface), then execute `MaterialResult121Check.Run`.
It does not certify arbitrary network rendering or native VT registration.

The source-linked PreparedLaneCheck covers actual source descriptors, resolved native
template defaults, complete copied fields and injected read failures. Its native cache
registration API is doubled and must be checked independently. Run it with the Materials
root, `tools/PrepareLanes/NativeTemplates.json` and the installed Cities2_Data directory.
PreparedCableCheck covers all ten prepared cable descriptors, exact overrides, result
failures and released property leases. Its second argument is the installed
Content/Game/Blob_FreeUpdate02.cok archive, used to verify the original control dependency.
Injected native Save failures remain failures and retain new assets for caller rollback.

`MaterialResult125Check.Run` extends the compiled native catalogue fixture with every
truncation of a native serialized Surface payload, UV-frame/polar/projection failure results
and native structural preservation results. It requires the same arguments as
`MaterialResult124Check.Run`. It does not render a city or certify arbitrary network materials.

## Common conversion native129

`MaterialConversion129Check.Editor.cs` uses the production converter with an
explicit evaluated-object geometry contract. Copy it into an isolated Editor project
along with `Fixtures/Protocol129*.shader`; these diagnostic shaders are not mod
assets and are never packaged. Arguments: `-gameManaged`, `-modAssembly`,
`-outputRoot` (fresh isolated directory), and `-vtSurface` (an existing native
prepared VT Surface). Invoke `MaterialConversion129Check.Run`.

The native database proxy only supplies fixture CIDs because the game's Burst
hash initializer is incompatible with this Editor host. Native Surface parsing,
conversion, saving, property leases and deletion remain real. The suite covers
source overrides together with distinct template defaults, differing source IDs,
object reuse, geometry/renderer incompatibility, transactional failure cleanup and
byte-exact real VT descriptor reload. It does not assert GPU rendering or streaming
reservations. Historical OriginalShader127Check describes the withdrawn blind
shader experiment; its acceptance expectations are not current conversion rules.

## Compatibility output native130

`MaterialCompatibility130Check.Run` supersedes native129 compatibility-rejection
expectations. Use its Editor source with the same diagnostic shaders and arguments,
plus `-bikeTexture` and `-busTexture` pointing to isolated native texture fixtures.
The 31 cases cover processed template 17, transparent and projected descriptors,
unknown shaders without albedo across ten styles, original texture identity,
world-planar mappings, projection overlap/height/bones, resource failures and VT
descriptor serialization. Native Surface IO is real; protocol shaders and source
descriptors are constructed fixtures, with real bicycle/bus texture payloads.
This does not certify city visuals, original network blending, soft transparency
or native VT streaming. Compatibility output can select one source layer, use
cutout transparency and native defaults, with non-blocking difference diagnostics.

## Original decal source regression (native139)

`DecalSource139Check.Run` takes `-gameManaged`, `-modAssembly`, `-outputRoot`,
`-sourceRoot`, `-vtSurface`, `-bikeTexture` and `-busTexture`. The source directory
contains the native TramRails, TramRailsBase and LaneLine `.Material` descriptors
and their original BaseColor/Normal/MaskMap `.Texture2D` payloads. It verifies all
three source texture identities, unchanged source bytes, native hashed-alpha
Surface save/reload, full source texture-area coordinates, receiver clipping,
separate overlapping layers, bone/gap/height handling, and the real final
TryAttachMovablePaint mesh consumer. Native database APIs are used; a fixture CID
allocator substitutes the incompatible Editor Burst hash initializer. Protocol
shaders are fixture shaders, so this is not a game visual or shadow-pass test.
The separate MaterialCompatibility130Check covers real bus/bicycle color textures
and existing compatibility/failure cases. No player asset is read or modified.

## Moving-domain prototype wear rule

`Wear140Check.Run` extends the native139 arguments with `-wearSurface` for the
original native CarLane descriptor. It checks shader-identity classification
including renamed third-party template identities, fixed/moving mixed projection,
ancestor motion, and final native meshes with no wear bound to a moving bone.
Line/rail layers remain intact. Shader fixtures are protocol stand-ins, not game
visual acceptance; source descriptor and original rail texture payloads are real.
