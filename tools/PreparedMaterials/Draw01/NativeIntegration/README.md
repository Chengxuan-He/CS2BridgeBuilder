# Draw01 native material binding implementation

## Delivered files

- `Sources.json`: 13 verified native Surface identities, their eight distinct content
  hashes and shared native texture resources. Compatible originals are retained;
  no native descriptor or pixel payload is generated.
- `Sampling.json`: 1,029 sampling records separate from source content, preserving
  measured Float32 bits and source identities. Legacy derived folding is explicitly
  distinguished from original atlas semantics.
- `Draw01NativeMaterialBinding.cs`: compiled binding/domain implementation. Native
  mapping uses the production `SourceUv` operation without vertex modulo. Unknown
  source content and unsupported receiver domains fail explicitly.
- `MovableStructureTiles.patch`: minimal shared-layer patch for the parent to review
  and apply. It preserves the existing compatible original-Surface path and rejects
  Draw01's per-sampler baked fallback. It does not edit another style's path.
- `Unsupported.json`: every production sampler key still has at least one unproven
  receiver scenario. These are explicit unresolved coverage, not a claim that every
  receiver of every sampler is incompatible.

The shared patch passes `git apply --check`. Before applying it, the parent must copy
`Draw01NativeMaterialBinding.cs` into `src/BridgeBuilder/Bridges/Types/Draw01/`.
The patch is a **partial integration**, not a complete migration: applying it now will
reject Draw01 generations that require the unresolved domain cases. It is supplied
for the parent to combine with the actual authored-domain solution, not to label
the bridge ready or silently regress generation. No production code was changed here
because the only selection and UV-binding integration point is shared code.

## Executable verification

The isolated `Check` project compiled with zero errors and zero warnings using the
game's Unity types and the actual production `MovableStructurePacking` implementation.
It replayed 58,568 previously recorded receiver observations:

- 3,625 compatible observations retained native-source mapping.
- 12,207 mapped UV points matched the original path bit-for-bit.
- 54,943 unsupported observations were rejected explicitly.
- 13 source identities and all 1,029 source/sampler links resolved consistently.
- Texture writes: zero. The original Surface and its native texture/mip graph remain
  the binding target in the proposed patch; no source image is decoded or reconstructed.

The result is saved at
`artifacts/prepared-materials/Draw01/source-sharing/native-binding-check.json`.
These are compiled producer/domain checks, not native all-mip rendered acceptance.
Existing native source evidence and measured resource bytes remain in the adjacent
`source-sharing/README.md` report; no repeated Unity probe was needed.

## Replay

Run `BuildNativeBindings.py` to recreate the source catalogue and binding code,
then `BuildNativeIntegrationPatch.py` to recreate the reviewable shared patch and
unresolved list. Both tools read the already verified source-sharing map.

```powershell
dotnet build tools/PreparedMaterials/Draw01/NativeIntegration/Check/Check.csproj -p:GameManagedDir="D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Managed" -p:BaseOutputPath=C:/Users/admin/Documents/Codex/CS2BridgeBuilder/artifacts/agent-material-env/Draw01/native-binding-check/bin/ -p:BaseIntermediateOutputPath=C:/Users/admin/Documents/Codex/CS2BridgeBuilder/artifacts/agent-material-env/Draw01/native-binding-check/obj/
dotnet artifacts/agent-material-env/Draw01/native-binding-check/bin/Debug/net8.0/Check.dll C:/Users/admin/Documents/Codex/CS2BridgeBuilder C:/Users/admin/Documents/Codex/CS2BridgeBuilder/artifacts/prepared-materials/Draw01/source-sharing/native-binding-check.json
git apply --check tools/PreparedMaterials/Draw01/NativeIntegration/MovableStructureTiles.patch
```

## Remaining implementation and acceptance

The legacy triangle fold is an old generator operation, not automatically an original
material requirement. For receivers leaving the measured authored atlas domain, the
remaining implementation must establish a source-faithful authored region mapping
instead of preserving that artificial fold by assumption or letting UVs wander into
unrelated atlas content. Native filtering, both density directions, normal interpretation
and all mip levels must be validated for that representation.

Production shared integration, complete part/LOD coverage, preview/create/regenerate/export,
native cold loading, independent owner snapshots/deletion/uninstall and the complete
official SDK package at or below 50,000,000 bytes remain pending. No package reduction
is claimed. Existing structural resources were not removed or changed, and no deployment,
publication, environment cleanup or new bake occurred. There are no UI strings or
localization behavior changes.
