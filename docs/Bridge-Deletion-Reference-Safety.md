# Deleted bridge prefab reference failure (2026-09-19)

## Evidence

The 09:38:54 FileSystem log starts with a failed CID resolution, not invalid JSON:

- Owner: deleted `r6b302c6f-6f65-4b66-97a1-cf38fb5ec431` GoldenGate bridge.
- `GoldenGateBridgeBase01`, `GoldenGateBridgePillar01` and the custom-lighting pillar still
  contain `SpawnableObject.m_Placeholders -> CID:92d38105937e4889d02e49e3130a6715`.
- Its `GoldenGateBridgePylon01` replacement references missing placeholder
  `CID:02c811df8dc57b6686c976ac78932c1d`.
- `PrefabReferenceResolver.TryResolveReference` fails. Odin subsequently reports a layout mismatch
  and `Was not in array when exiting array`. SceneFlow then reports four null SpawnableObject references.

The old remover walked only outward from the road. Replacement objects point **to** their placeholder,
not the other way around, so the placeholder was deleted while its replacements remained loadable assets.
Changing Odin parsing, removing a component, or suppressing the exception would not repair that graph.

## Change

`BridgePrefabRemoval` handles the explicit user delete operation only. No startup cleanup or per-frame
audit was added. It captures the graph before deletion, includes owned reverse-linked replacements and
their dependencies, and protects references from every other loaded writable user asset, including
unregistered assets. A shared placeholder protects its replacement objects too.

Ownership is restricted to the UUID-bearing bridge dependencies and the configured generated dependency
namespace. Ambiguous legacy dependencies are retained. Shared/read-only/built-in/source assets are not
deleted. Referrers are deleted before dependencies; a failed delete keeps its downstream references.
Cycles are retained, not partially broken. State/icon/registration removal only follows successful
root deletion. Live geometry remains allocated while its render prefabs are registered.

## Existing damage and validation

The exact old UUID has 8 prefab files, 10 geometry files and 18 CID sidecars (36 files, 1,652,648 bytes)
in ImportedData and BridgeBuilder. No other ImportedData prefab references those CIDs. The user explicitly
authorized permanent deletion of this deleted bridge's leftovers. The execution environment rejected
the deletion command **before execution**, so these files have not been deleted by this repair.
A separate, default-read-only manual cleanup script was supplied in the task output folder. It checks
the fixed UUID, fixed directories, inventory, registry, incoming references and stopped-game condition;
it never targets mod caches or saves. It was not executed to bypass the deletion restriction.

Eleven reference/persistence regression checks use the production removal class and shared reference
walker. They cover reverse replacements, shared placeholders/meshes, unregistered referrers, source
protection, disabled cleanup, failed root/dependency deletion and cycles. No geometry tests are run.
Release compilation passed. The DLL and five companion files were installed and hash-verified; previous
files are backed up under `outputs/bridge-before-reference-removal-20260919-095759` in the task workspace.
Game restart verification remains manual, and still requires handling the existing orphan files first.
