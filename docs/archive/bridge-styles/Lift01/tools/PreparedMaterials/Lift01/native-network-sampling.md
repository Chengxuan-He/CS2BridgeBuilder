# Lift01 fixed approach native sampling

## Representation and source evidence

`Lift01ApproachAssembly` builds one fixed `NetPiecePrefab` graph from the exact
three declarations in `Lift01ApproachSources.g.cs`. The two LOD children are
`RenderPrefab` instances, but remain children of that native network root, not
independent object renders. Each Level uses the fixed-structure constructor,
without selected-road triangles or a procedural skeleton. `Lift01ApproachLayout`
inserts the generated root into the recorded native overhead section.

| Render / mesh | Original Surface CID | Template |
| --- | --- | ---: |
| `LiftBridge01Net Mesh` / 0 | `368eb4e7fa7b81f6e28ee321c76dd9f2` | 33 |
| `LiftBridge01Net_LOD1 Mesh` / 0 | `879f0a8882e3069d3d8b890a5ec15140` | 33 |
| `LiftBridge01Net_LOD2 Mesh` / 0 | `0a9990f567fc079703b199f1fcb0c53c` | 33 |

`Lift01NativeMaterials.IsNativeNetwork` identifies only these exact triples.
The shared caller must additionally establish the complete fixed NetPiece graph,
absence of object/selected-road motion, and original network Surface template.
A matching name alone, a different material slot, another LOD's Surface, or a
network Surface attached to a mechanism object does not qualify.

These streams contain all 86,489 source domains excluded as positive-V native
network mappings by `BuildUvSampling.py`. Their whole-domain finite-retention
flags remain false. Native shader 581 evaluates the positive V rate using its
native longitudinal distance and phase. The original U, rate, remaining UV
components, native channel encoding, material and composition context must reach
that renderer intact. No ordinary object-atlas polar frame substitutes for this
evaluation, and no rate is multiplied a second time.

The root's recorded `NetPieceTiling.m_DisableTextureTiling = true` remains part of
the native layout. This field controls composition grouping, not permission to
classify the source V as finite. The shared prefab writer copies source
components and NetPiece fields and remaps LOD references to generated children.
It must preserve that component in preview and saved output. Existing measured
width/part construction remains unchanged; constructed vertices retain the
existing source-channel interpolation.

Original Surface identity preserves native worldspace shading and texture/mip
bindings without proving an unnecessary template-33-to-0 conversion equivalent.
The built-in Surface graph stays native references, with zero new descriptors
or texture payloads. The object-only `Lift01NativeMaterials.Resolve` no longer
replaces these sources with prepared template-0 descriptors. It rejects network
templates 17/33 outside the native network route and returns the identical original
Surface for object sources. Retaining a network CID does not make its shader
compatible with a skinned object. The moving road, main mechanism, pillars, windows, authored
floor repeat and all object-source retention rules remain outside this predicate.

## Verification and integration boundary

Run the source-linked identity and provenance check:

```powershell
dotnet run --project tools/PreparedMaterials/Lift01/NativeNetworkCheck/NativeNetworkCheck.csproj -- <repository> <report.json>
```

The check reads all 16 original Surface records from their game COK archives and
verifies bytes, CIDs and templates against the retained source graph. It tests all
23 source render/slot uses, cross-source substitutions, unmeasured slots, modified
names and mechanism-object controls against the actual production predicate.
Its native Surface stub is never loaded and is not renderer evidence.

The result passes 483 predicate assertions. The checker additionally uses header
values from all 16 byte-verified original Surface records to verify object-only
dispatch: object sources retain the exact reference, the three network sources
are rejected, and no replacement descriptor loader is called. This managed
dispatch test does not establish native renderer behavior. Against the exact prior
`actual-final2` receiver capture, all 16,184 unresolved units and all 27,488
density-mapped Lift01 units belong to these three native approach streams. The
report at `artifacts/material-resume/Lift01/native-network-identity.json` retains
source, implementation and capture hashes and per-render attribution. These are
prior-capture attribution counts, not a substitute for the new shared observer's
actual native-context route and final upload checks.

Shared integration must exercise original-model/Surface retention, complete
native channel and interpolated-fragment preservation, LOD remapping and graph
capability isolation before installation. Preview, saved native rendering and
long/short-span appearance still require the agreed human acceptance stage.
Generated-owner bytes are unmeasured until a human creates the corresponding
sample; the built-in-only approach dependency branch does not establish the
entire bridge's material footprint. No user-facing string or localization table
changed; existing localized operation-failure handling is retained.
