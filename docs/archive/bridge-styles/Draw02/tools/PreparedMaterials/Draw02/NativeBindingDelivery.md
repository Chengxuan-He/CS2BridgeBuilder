# Draw02 native binding implementation delivery

## Implemented files

`src/BridgeBuilder/Bridges/Types/Draw02/Draw02NativeSourceBindings.cs` implements
the bridge-specific binding boundary. It accepts the existing native Surface
and actual receiver sampling Unit, checks its recorded built-in CID and content
digest, and returns the same Surface instance with the Unit's source-UV mapping
when `CanUseSource` proves receiver containment and Float32 gradient preservation.
It never creates or clones a Surface, Texture, VTTexture, VTSurface or MidMips.
Native texture-global settings and original dependency references remain intact.

`Draw02NativeSourceCatalog.g.cs` separates fourteen verified native material
identities from sampling coordinates. It contains identity strings only, with
no duplicate numeric sampler tables. `EmitNativeBindingData.py` generates it
from the existing source map and emits all 953 exact sampling call records to
`artifacts/prepared-materials/Draw02/shared-source-migration/native-binding-calls.json`.
The numeric records retain the inventory's Float32 bits. They are integration
evidence, not an invented runtime sampler implementation.

Unsupported receivers return a specific requirement with the source sampler key.
The implementation does not assume the old generated triangle-fold pattern is
necessarily the prototype requirement. Such receivers require their original
UV mapping to be derived and validated before native binding; the helper neither
forces UVs outside their source region nor applies vertex modulo or new baking.

## Common integration handoff

`NativeBindings.integration.patch` is an unapplied minimal common-layer patch for
the main agent. It creates one Draw02 binding context per `MovableStructureTiles`
instance, uses the returned original Surface/mapping, and explicitly fails an
unsupported Draw02 receiver instead of selecting another per-sampler baked file.
It leaves other bridge styles unchanged. The patch passes:

```powershell
git apply --check --ignore-whitespace tools/PreparedMaterials/Draw02/NativeBindings.integration.patch
```

Whitespace tolerance is needed for the current CRLF common source. The sub-agent
did not apply this patch or edit common code. Until the main agent integrates it,
the new bridge-specific binder has no active common caller. Applying it before
the remaining receivers obtain a supported mapping deliberately reports those
unsupported branches; it is not a claim that all Draw02 bridge generation works.

## Verification

The boundary executable links the actual new Draw02 binding/catalogue files and
uses fourteen byte-identical native Surface descriptors extracted from installed
game archives. All fourteen pass these checks: same Surface instance retained,
same sampling object retained, descriptor read once per binding context,
unsupported mapping reported, modified content rejected, and non-built-in source
rejected. Unknown sources are not read. Its report is
`artifacts/prepared-materials/Draw02/shared-source-migration/native-binding-check.json`.
These are deliberately boundary test doubles, not a native rendering claim.

`NativeBindingCheck/NativeApiCompile.csproj` also compiled the new binder against
the installed game's actual SurfaceAsset/AssetDatabase and Unity assemblies,
with zero errors. Its sampling Unit/Slot doubles only supply the common API shape;
the main agent remains responsible for the full mod build. The probe build has
three non-production warnings: its custom intermediate directory property and
two unassigned compile-only stub fields. No runtime source warnings were reported.

The previous representative native mip-zero/interior evidence is retained and
was not rerun. No affine-failure, permutation, transposition, compression or
zero-control experiment was repeated. No material resource was removed or
overwritten. Added texture payloads: zero; proven official SDK package saving:
zero until safe replacement and packaging are complete.

## Exact remaining acceptance

The main agent must integrate the common caller and determine each unresolved
receiver's applicable original UV/native mapping. The 953 legacy sampling records
are coverage inputs, not automatic evidence that every old fold operation is
required. Full part/LOD sampling coverage, original full-mip and normal behavior,
native region boundaries, preview/create/regenerate/export consistency, cold
loading, deletion and uninstall independence, no runtime constant baking, the
complete SDK ZIP at or below 55,000,000 bytes, and human visual acceptance remain
unverified for this migration. Existing resources remain until those replacements
are verified. This finite implementation handoff stops here.

The new exceptions/requirements are technical diagnostics passed to the existing
generation error path; no runtime labels, dialogs, settings or other localized UI
text were added or changed. No build deployment, publication or environment cleanup
was performed by this sub-agent.
