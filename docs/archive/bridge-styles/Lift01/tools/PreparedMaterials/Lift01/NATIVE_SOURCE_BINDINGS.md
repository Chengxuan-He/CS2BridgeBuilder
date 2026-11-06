# Lift01 native source binding delivery

`ExportNativeBindings.py` exports the existing 2,353 production structure sampler
records into `artifacts/prepared-materials/Lift01/native-bindings/native-source-bindings.json`.
It separates nine original Surface identities and 18 channel views (12 physical native
backing files) from each source-domain mapping. The export retains source CIDs,
digests, bounds and triangle coordinates. It makes no texture or material descriptor.
The generated manifest is not currently consumed by production; it must not be
reported as a completed rendering migration.

The existing production-compatible branch in `MovableStructureTiles.Build` already
binds the original Surface and `Unit.SourceUv`, preserving the native graph and its
mips. `MovableParentUv.Result.Select` supplies unconditioned source coordinates when
`Unit.CanUseSource` accepts them. Rigid portions already retain their original UVs
and Surface. No change is needed to these working direct-reference branches.

## Shared implementation request

The descriptor-only conversion, source binding adapter and native verification are
implemented separately in [NATIVE_OBJECT_DELIVERY.md](NATIVE_OBJECT_DELIVERY.md).
Two new object Surface descriptors preserve the original six network textures.
The public generation path must integrate the adapter while retaining original UVs;
this document does not propose disabling an existing generation branch as a substitute.

The remaining shared implementation must resolve original source-domain UVs before
`Result.Select` conditions their integer phase and sets `RepeatOnly`. Unsupported
out-of-domain mappings must not use adjacent atlas islands, vertex modulo or clamping.
An in-domain mapping must still preserve the Float32 gradient. The old bake's triangle
fold is not itself proof of prototype semantics and is not required by this manifest.

139 records use native network template 33; their descriptor-only object equivalents
have passed native reader verification and await shared rendering integration.
The other 2,214 records use templates 0/1; their actual direct
reuse eligibility still depends on requested geometry and cannot be inferred from
the inventory alone. A template conversion must retain native texture identities,
not decode and rebake the images. Exact original built-in graph references create
no package texture payload; non-built-in resources require full original-CID
owner-local copies through the existing persistence path.

## Verification and limits

The exporter verified 6,741 triangle/rectangle corner mappings using Float32 source
values. This is data validation, not native filtering, boundary, normal or all-mip
rendering acceptance. No earlier affine-versus-fold probe was rerun or used as a
requirement for the new path. No production assets were replaced or deleted.

The binding inventory is `artifacts/prepared-materials/Lift01/native-bindings/native-source-bindings.json`.
Descriptor integration evidence is in `artifacts/prepared-materials/Lift01/native-object-candidates/`.
Native all-part/LOD sampling, preview/create/regenerate/export parity, cold loading,
uninstall independence and the aggregate official SDK package limit remain unverified
for the unresolved path. The old validated structure resources remain until an
actual replacement passes those checks. No UI text was changed; the proposed
diagnostic exception retains the existing localized generation-failure flow.
