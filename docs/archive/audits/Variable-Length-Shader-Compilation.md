# Variable-length shader compilation

## Cause and implementation

`MovableCustomShaderProgram.TryCompilePackage` previously required compiled bytecode to have exactly the seed byte count, then copied it into the bundle in place. This was an implementation restriction, not a D3D or Unity shader requirement. Compiler environments and options can change the length.

The compiler now gathers validated replacement programs before calling `MovableShaderContainer`. It reads the actual UnityFS 8 directory and uncompressed streaming blocks, serialized-file 22 object/type tables, Shader type-tree fields, literal-LZ4 payload and version-202012090 program records. It reconstructs native program byte arrays and their four-byte alignment, record offsets/sizes, LZ4 literal lengths, Shader segment and blob lengths, serialized object sizes/eight-byte offsets, file size and UnityFS directory/block/file sizes. Original shader pass/reflection fields and unrelated records remain unchanged. No truncation, fixed capacity or old-bytecode fallback is used.

Existing BBS1 versions 1 and 2, including owner-local persisted packages, use the same rebuilding path. No seed, HLSL, saved owner or persistence envelope is rewritten. Seed-byte identity, overlapping/out-of-range locations, native format boundaries and actual compiler failures remain checked. This supports the recorded authoring container format; it is not a claim to parse arbitrary Unity versions or graphics backends.

## Executed verification

Both checked-in packages were tested with the production C# compiler/container sources:

| Program | Seed bundle bytes | Non-optimized compiled bundle bytes | Production recompilation of larger seed | Declared occurrences | Native program records |
| --- | ---: | ---: | ---: | ---: | ---: |
| BoneDecal | 243,835 | 273,763 | 243,835 | 10 | 55 |
| WorldLayeredPbr | 3,650,467 | 3,682,883 | 3,650,467 | 12 | 194 |

Real D3DCompile outputs therefore exercise growth and shrinkage. Synthetic shorter, longer and mixed-size payloads separately exercise container relocation; these synthetic bytecode bodies are not GPU-valid shader tests. Identity reconstruction reproduces the original bundle bytes exactly. Rebuilding is deterministic. Corrupt offsets, overlap, truncation, mismatched seed bytes and invalid DXBC lengths are rejected.

The independent UnityPy verifier decodes every resulting container and LZ4 record table, compares all native reflection/pass and AssetBundle metadata, and checks unchanged program prefixes/suffixes and unrelated records. The isolated Unity 2022.3.62f2 D3D11 check loads eight actual-compiler bundles, unloads between cases and verifies one supported Shader per bundle: one pass for BoneDecal and six for WorldLayeredPbr. Production build and twelve-language JavaScript/C# checks pass; four existing nullable build warnings remain.

Reproducible checks are `tools/ShaderContainerCheck`, `tools/MaterialShaders/VerifyVariableShaderContainer.py` and `tools/MaterialShaders/VariableShaderLoadCheck.Editor.cs`. Results, native logs, bundles and deployment receipt are retained in `artifacts/shader-variable/`. These checks establish compilation, structural reconstruction and native bundle/shader loading, not live city rendering, movement or saved-city cold-load acceptance. No game or launcher was started.
