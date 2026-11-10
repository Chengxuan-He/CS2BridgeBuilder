# Dynamic persistent shader feasibility — 2026-11-08

## Status and tested scope

**Conclusion: Not implementable under current requirements.** The complete custom-shader solution is unavailable for the current game version when the saved bridge must cold-load with Bridge Builder disabled or uninstalled. The experiment is concluded, not an active production rollout. This does not revoke the user's permission to investigate a future evidenced native loading path.

Dynamic HLSL compilation, Unity shader-container construction/loading and real GPU execution succeeded in an isolated standalone Unity Player. Independent native cold loading of a newly generated template did not: the saved native Surface retains only a template hash, and native Prefab references to Material/Shader use the built-in global UnityObjectsMap. No production shader implementation, bridge mutation, build or installation was made. Native136 remains installed.

The user permits dynamically generated shaders saved with their bridge, but still requires operation with Bridge Builder disabled/uninstalled. Independent loading is a non-negotiable project constraint. A startup loader/registration service is a disqualified counterexample, not an optional solution or a candidate for deployment. No game or launcher was started or stopped. All executable probes were our separately built diagnostic Player or the retained offline Unity Editor project.

## Dynamic compilation and container execution

- Host: Windows, NVIDIA GeForce RTX 5060, Direct3D11, Unity 2022.3.62f2 Mono standalone Player (`Application.isEditor=false`). The game is 2022.3.71f1; the Player probe is not an in-game test.
- Generated HLSL is compiled inside the Player by `D3DCompile`, then its DXBC replaces a program in an uncompressed Unity Shader/AssetBundle serialization container. `AssetBundle.LoadFromMemory` loads the actual changed resource. No Editor compilation API is called by the Player.
- A reusable serialization/reflection skeleton was initially produced with the Editor. The probe supports an unchanged binding/reflection layout and equal-length program replacement; it is not yet a general ShaderLab compiler or arbitrary-layout serializer. The offline Python container builder demonstrated a variable-length 212-to-420-byte program replacement too.
- Two generated source strings produced independent programs and GPU readbacks: `(0.217,0.613,0.359,1)` and `(0.317,0.713,0.459,1)`. This is not a fixed precompiled shader with altered material uniforms.
- SHA-256 values and byte counts are recorded in `work/dynamic138/hashes.json`. Sources, DXBC, containers, probe C# and logs are retained under that work directory and the retained isolated project.

## Original six-texture layered sampling

A second Player probe generates a 2,092-byte pixel program from the source coordinate/material recipe. It samples the original six BC7 texture payloads with their complete mip chains and original dimensions: three 1024x4096 base maps and three 1024x1024 world maps. No texture is rebaked or downsampled. A 256x128 diagnostic coordinate texture carries 32,768 barycentrically evaluated points from the actual saved mesh `work/uv137/output-0-2.npz`, over X=[-8,8], Z=[10,18]. This texture is a test input, not a proposed production bridge resource.

The material function uses evaluated base UV.xy and world XZ*0.4000000059604645, with base Mask.B as the blend weight. It emits linear color, the blended tangent-space normal before final normalization, and smoothness/metallic/AO plus blend weight. Normal decoding follows the inspected native R*A/G encoding. The test is a horizontal-plane material-function slice; it does not implement all HDRP lighting, weather, decals, native moving bones, VT streaming, passes or arbitrary shader protocols.

After correcting the diagnostic Player's initial gamma-project configuration to Linear, comparison against a separate CPU BC7 decoder/bilinear/sRGB reference gives maximum absolute differences of 0.001143173 for linear color, 0.001060606 for normal components, and 0.000430644 for PBR/weight. These are measured differences, not bit-equivalence or complete native visual acceptance. The final row order is the raw Unity texture upload/readback order; an extra Y flip is incorrect. The test retains the actual independent coordinate fields instead of multiplying base UV by 3.2.

The single measured layered run took 65.834 ms: compilation 8.246 ms, container read/patch/save 3.805 ms, bundle loading 3.394 ms, original texture file reads/uploads 34.696 ms, and mesh draw/three synchronous readbacks/file writes 15.161 ms. This is not a whole bridge generation benchmark, a cold disk measurement or a prediction for a complete HDRP program.

## Minimal-program timing

Nine iterations in one standalone Player process: first process iteration versus median of eight warm iterations. The OS/driver cache was not flushed. The program is a 420-byte constant-color pixel shader and reads back an 8x8 float target. `cleanupMs` includes result validation/report IO plus explicit cleanup; destruction scheduling is not a native memory-release benchmark. GPU time includes synchronous CPU readback, not pure GPU instruction time.

| Stage | First iteration ms | Warm median ms |
| --- | ---: | ---: |
| compileMs | 3.976 | 0.905 |
| containerReadMs | 1.087 | 0.216 |
| containerPatchMs | 0.270 | 0.144 |
| saveSourceAndPayloadMs | 1.444 | 1.094 |
| bundleLoadMs | 2.928 | 0.534 |
| materialTargetMs | 1.200 | 0.014 |
| gpuDispatchReadbackMs | 6.156 | 0.402 |
| cleanupMs | 3.956 | 0.211 |
| totalMs | 21.019 | 3.498 |

Do not extrapolate these figures to complete dynamic PBR compilation, all pass/keyword variants, native VT preparation, or bridge creation. The sampled simple compilation cost is small; complete production performance is unmeasured.

## Native persistence and reference counterexamples

The actual shipped `Colossal.IO.AssetDatabase.dll` was loaded in the isolated Editor host. Native database read/write calls were exercised with explicit fixture CIDs and a private fixture directory. No player asset was touched. Tests were repeated in a fresh process.

1. Native `SurfaceAsset.SaveData` writes the template hash and property dictionaries, not the Unity Shader or Material object (`work/dynamic138/SurfaceAsset.cs:725`). `GetTemplateMaterial` queries the global `MaterialLibrary` (`:666`). `Load` logs a missing template and uses the fallback material when lookup fails (`:681-685`).
2. An owner-local Surface and dynamically generated bundle exist. Loading the bundle alone still leaves `GetTemplateMaterial` null. Explicitly adding its MaterialDescription to the in-memory MaterialLibrary makes lookup succeed. Clearing the library loses the template again. The fresh-process test repeats the same result. Evidence: `surface/warm-result.txt`, `surface/cold-result.txt`, `surface-map-cold.log`.
3. Actual `PrefabAsset.PrefabReferenceResolver.CanReference` serializes both generated Shader and Material references as `UnityObjectsMap:<guid>` (`work/dynamic138/PrefabAsset.cs:224-228`), not embedded Unity asset bytes. They resolve while the live map holds them. After clearing the map, actual `TryResolveReference` returns false/null even with the bundle still loaded. Thus putting a Material/Shader reference into a native Prefab does not independently restore its object.
4. `AssetDatabase.Initialize` obtains `AssetDatabaseResources` through `Resources.Load("AssetDatabase/AssetDatabaseResources")` (`AssetDatabase.cs:314`). The map and material library are serialized game resource references, not automatically loaded owner-local files (`AssetDatabaseResources.cs:37-51`).
5. `PackageAsset` packages recognized native assets in .cok/.cop/.zip; it is not a Unity AssetBundle importer. The shipped native asset type inventory contains no ShaderAsset or AssetBundleAsset. `UnityObjectsMap.TryGetGuidOrAdd` mutates the live map; `OnBeforeSerialize` records references, not shader payload serialization or owner-local registration.
6. The shipped AssetPipeline importer inventory has no ShaderImporter/ShaderCompiler class. The Player `Material(string)` constructor is obsolete and does not pass its source string to its native call (`UnityMaterial.cs:226-230`). This did not prevent the separate tested D3DCompile/container route, but those APIs do not establish native cold loading.

## Remaining boundary

The demonstrated experimental route would need a creation-time compiler plus a loader which restores bundle and template/map registration at each process startup. This is recorded solely as a noncompliant counterexample, not a proposed alternative. The latter need not run each frame, but still violates the user's disabled/uninstalled-mod independence requirement and is not implemented in production. Modifying the game's shared libraries/resources, installing a replacement loader, or smuggling executable bootstrapping into an asset is not an authorized workaround.

These tests cover Surface hash lookup, native Prefab external-object reference resolution, MaterialLibrary/UnityObjectsMap, built-in resource initialization and package/importer roles. They do not constitute a proof that every undocumented native plugin/import extension is impossible. No documented or inspected owner-local native shader/template cold-discovery path has been found. A complete new native loading protocol supplied by the game would change that conclusion; an Editor-only importer or file bundle by itself does not.

The complete solution is **not implementable under current requirements**, because independent cold loading is not supplied by the inspected native asset paths. The dynamic shader execution investigation succeeded in a bounded scope, but the bridge density bug is not fixed and no ineffective build was deployed.
