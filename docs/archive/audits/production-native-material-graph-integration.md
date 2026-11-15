# Production native material graph integration verification

## Scope

The production importer and owner persistence path were exercised with actual game assemblies in
an isolated Unity 2022.3.62f2 Editor project. The tested Release DLL embeds the complete native
AssetDatabaseResources/MaterialLibrary/Material/Shader graphs. No game session was started.

## Implementation inspected

- Runtime configuration changes only generated native hash, queue and bundle/CAB identity fields.
- Registry publishes the deserialized MaterialDescription without reconstructing its Material.
- Owner persistence stores the entire configured native graph as a FileAsset with its CID.
- Restoration imports saved owner graph bytes without source-template lookup or a JSON recipe.
- Cross-owner conversion copies those graph bytes unchanged and records owner-local dependencies.
- Native DefaultAssetFactory.MapSupportedExtension<FileAsset> registers the standard `.bundle`
  suffix before creation/discovery. The default FileAsset extension list excludes it; the first
  integration test exposed that missing registration, and the implementation was corrected.

## Completed checks

| Check | Observed result |
| --- | --- |
| ShaderContainerCheck | Both programs, queues 1999/2000/2007/2473: deterministic distinct identities; all bytes outside native scalar/identity fields preserved |
| Release build | Passed, including material authoring fingerprints, pinned source manifest, 1004 numeric resources, prepared assets and release path checks |
| Real native registry | Both programs at queues 2000/2007 load simultaneously with actual shaders and bone parameters |
| Repeated import | Reuses the same description and template identity without adding duplicate rows |
| Second MaterialLibrary | Reuses the immutable loaded native graph; no duplicate AssetBundle load |
| Foreign hash collision | Rejected without replacing the foreign entry |
| FileAsset persistence | Configured queue-2007 graph saved and read byte-identically with native CID |
| Fresh-process owner-only import | Saved BoneDecal graph restores queue 2007, hash 1283804602 and BridgeBuilder/Generated/BoneDecal without a source bundle input |

The registry test also observed WorldLayeredPbr hashes 1817382405 (queue 2000) and 1699939072
(queue 2007), and BoneDecal hash 1199916507 (queue 2000). These values identify these exact inputs;
they are not general constants or runtime requirements.

## Evidence and limits

- Harness: `tools/MaterialShaders/Custom160RegistryCheck.Editor.cs`.
- Scalar regression: `tools/ShaderContainerCheck/Program.cs`.
- Logs: `artifacts/native-shader-format/production-build.log`, `production-registry.log` and
  `production-owner-cold.log`.
- Isolated saved owner: `artifacts/native-shader-format/production-owner-configured`.
- The offline producer separately records source MaterialDescription/Material field fidelity and
  unchanged compiled Shader bytes in its production graph evidence.

These checks establish native deserialization, registration and file persistence under the isolated
Editor/game-assembly environment. They do not establish actual Cities: Skylines II rendering,
DOTS/ordinary draw equivalence, GPU-driver coverage, full game database cold discovery or a complete
bridge-generation transaction. No visual acceptance or deployment is claimed by this audit.
