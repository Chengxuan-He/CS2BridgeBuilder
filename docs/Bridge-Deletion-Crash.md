# Bridge deletion native crash (2026-09-19)

## Evidence and limits

The latest available game log ends at 09:43:46, before the later 09:58 and 10:33
deployments. BridgeBuilder reports successful deletion at 09:43:43 and 09:43:46.
FileSystem then repeatedly reports missing user SourceMeta entries
`180ad89a3b868f8975629c7b9b16ea14` and `ebbc0b05f2705fa6cec00c20ab093e08`.
Player.log ends in a native crash with an empty managed stack. This is evidence of
the failing session, not a crash reproduction of the new DLL.

Inspection of the installed Game.dll establishes an unsafe deletion path in our code:

- `CompositionSelectSystem.CreateComposition` creates composition entities and assigns
  `PrefabRef(roadPrefab)` to them, just like placed roads.
- The old `TryMarkPlacedInstancesDeleted` queried **only** `PrefabRef` and marked every
  matching entity `Deleted`. It therefore deleted cached compositions along with roads,
  while registered prefabs, tool previews and rendering could still reference them.
- It also changed ECS structure while enumerating a `SubObject` DynamicBuffer, and deleted
  on-disk assets before native cleanup of the marked entities had run.
- `AssetDatabase.DeleteAsset` removes database metadata, not PrefabSystem registration.
  Leaving the disk handle on the retained prefab makes `isReadOnly -> isPackaged -> GetMeta`
  query a removed entry. Keeping geometry allocated alone did not prevent this.

These are confirmed code defects consistent with the crash, not a symbolicated proof of the
native instruction which failed.

## Repair

`BridgeInstanceRemoval` collects real `Edge`/`Node` topology first. It explicitly excludes
prefabs, composition data and tool `Temp` entities. The main deck and owned auxiliary deck
prefabs participate in the same deletion. Orphan endpoints are removed; shared junctions
and surviving roads are retained and marked `Updated`. Native systems clean up child
objects, subnets and lanes. No structural change occurs during buffer enumeration.

The placement tool is deactivated if it is using this bridge. Asset deletion waits until
the marked network entities no longer exist, not merely until they have `Deleted` tags.
If a surviving network entity still references a root (for example a shared junction not
yet reassigned by the game), deletion of the backing assets is refused and the registry
is retained. The operation reports incomplete; it never removes the other road to force success.

Disk deletion retains the prior ownership/reference-graph protections. A deleted prefab's
asset handle is detached before database notifications; a failed delete restores the handle.
Prefab entities, cached compositions, native geometry and meshes are not unregistered,
destroyed or unloaded by this operation. They remain allocated for this world lifetime.
Successfully deleted roots are removed from UI category buffers and open build menus are refreshed.

This code runs only for an explicit delete request. No startup sweep, generated-bridge cleanup,
save modification, cache cleanup, or automatic deletion of existing orphan files was performed.

## Verification

- `tools/RemovalCheck`: 13 production reference-removal checks, including stale-handle
  detachment and restoration on failure.
- `tools/InstanceRemovalCheck`: 9 production topology/lifetime checks, including a composition
  with the exact same PrefabRef, tool previews, double decks, shared junctions and cleanup gating.
- `tools/PublicationCheck`: existing publication/menu-order regressions.
- Release compilation against the installed managed assemblies.

These are nonvisual tests of deletion/lifetime boundaries, not a game-engine or native-renderer
simulation. The game was not launched. Human restart validation is still required: create and
delete an unused bridge, delete a placed single/double-deck bridge with its placement tool active,
then delete a bridge connected to another road. The unrelated road must remain; any still-referenced
asset must be retained with an incomplete-deletion result rather than causing a crash.
