# Custom shader authoring and loading inspection

Inspected on 2026-11-18. This records the checked-in producers and loaders, not a new
requirement or proof of native game visual acceptance. Paths below are repository-relative;
producer inputs and generated artifacts retain their distinct roles.

## Program-specific authoring inputs and outputs

The common compilation, final storage and embedding rules are defined in
[offline precompilation and storage](../../specs/custom-shader-spec.md#offline-precompilation-and-storage). The following table
records the distinct producers and intermediate artifacts; final paths appear only in the
[program inventory](../../specs/custom-shader-spec.md#custom-shader-table).

| Program | Shader producer and actual inputs | Intermediate output | Complete-graph producer |
| --- | --- | --- | --- |
| `BoneDecal` | `tools/MaterialShaders/BuildNativeDecal.py`: `--object-shader`, `--decal-shader`, `--container-seed`; adjacent `NativeDecalVertex.hlsl`, `NativeDecalTranscription.py`, `NativeInstanceSkeleton.hlsl`, `MaterialFunctions.hlsl`. | `<shader-output>/BoneDecal.bundle` and `BoneDecal.manifest.json`, with generated compiler inputs/diagnostics. | `BuildNativeMaterialGraph.py` reads the intermediate through `--program-dir`. |
| `WorldLayeredPbr` | `tools/MaterialShaders/BuildNativeLayered.py`: `--object-shader`, `--layered-shader`, `--container-seed`; adjacent `NativeNetworkVertex.hlsl`, `NativeInstanceSkeleton.hlsl`, `MaterialFunctions.hlsl`. | `<shader-output>/WorldLayeredPbr.bundle` and `WorldLayeredPbr.manifest.json`; generated HLSL/DXBC and probe bundles remain offline. | `BuildNativeMaterialGraph.py` reads the intermediate through `--program-dir`. |
| `NetworkPalette` | `tools/MaterialShaders/BuildNativePalette.py`: `--network-shader`, `--container-seed`; adjacent `NativePaletteTranscription.py` and its native transcription support. | `<shader-output>/NetworkPalette.bundle`, `NetworkPalette.manifest.json` and `report.json`, plus per-program ASM/HLSL/DXBC. Diagnostic `--limit` never produces a release bundle. | `BuildNativePaletteGraph.py` reads the intermediate through `--program-dir`. |

The graph producers also emit offline `layout.json` and generated offset source:
`BuildNativeMaterialGraph.py` produces `BridgeNativeProgramLayout.g.cs`, retained at
`src/BridgeBuilder/Runtime/BridgeNativeProgramLayout.g.cs`; `BuildNativePaletteGraph.py`
produces `BridgeNativePaletteLayout.g.cs`, retained at
`src/BridgeBuilder/Runtime/BridgeNativePaletteLayout.g.cs`. Keep these generated constants
matched to the complete graphs. They permit only the identity/queue specialization defined above,
not runtime shader compilation or graph reconstruction.

`tools/MaterialShaders/BuildNativeLayered.py` accepts extracted native object and
layered Shader type trees, a compatible Unity container, and an output directory.
UnityPy is an offline authoring dependency. The tool preserves the source layered
pixel programs and their pass states. It replaces the evaluated-geometry vertex
protocol for six native passes, with separate DOTS/non-DOTS variants. The selected
native network program consumes packed normals and tangents in every inspected
keyword variant, including variants without `_TANGENTSPACE_OCTO`. Preserve that
input protocol for the encoded road meshes; do not invent an unpacked interpretation
from the absence of that keyword. It retains the original UnityPerMaterial property offsets and
appends the current bone-range field; competing partial constant buffers are not
an interchangeable representation.

The palette graph producer retains the native network template defaults and adds the three
palette properties without altering the skeletal graphs.

## Inspected runtime boundaries

`src/BridgeBuilder/Bridges/Common/Movable/MovableCustomShaderAsset.cs` reads
`BridgeBuilder.MaterialProgram.<name>.bundle` from the assembly, configures the requested
family/queue, and contains preparation exceptions in `TryPrepare`. `TryRegisterCurrent`
at this inspection iterates default families NetworkPalette, 17 and 18; it is not evidence
that every persisted nondefault queue has already been registered.

`src/BridgeBuilder/Runtime/BridgeCustomShaderRegistry.cs` reuses an existing entry for the
same library/identity and retains the owning bundle with its native description. For another
library it can reuse that graph, but rejects occupied template hashes. New graphs load through
`AssetBundle.LoadFromMemory` and publish the serialized native description without reconstructing
Material or Shader. Failure removes only pending publication and releases an unpublished bundle.
These observations do not establish prerequisite gating at every caller or native cold-start
acceptance; the [required loading workflow](../../specs/custom-shader-spec.md#custom-shader-loading-workflow)
defines the complete obligation.
