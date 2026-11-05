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
