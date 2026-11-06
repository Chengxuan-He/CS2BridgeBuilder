# Runtime bridge menu publication

## Evidence

In the retained game logs from 2026-09-19, a bridge was created at 01:28:43.
At 01:28:45 the native UI first reported that focus key `67446:33` was already
registered. The duplicate-key and mismatching-unregister errors continued when
the buildable asset list was reopened.

Inspection of the installed Game.dll established this path:

- `PrefabSystem.OnUpdate` already runs the entire `PrefabUpdate` phase.
- `PrefabInitializeSystem` initializes every prefab with `Created`, including
  its `UIObject.LateInitialize`.
- `UIObject.LateInitialize` calls `UIGroupPrefab.AddElement`, which unconditionally
  appends both `UIGroupElement` and `UnlockRequirement` entries.
- `ToolbarUISystem.BindAssets` enumerates that buffer without deduplicating it.
- The previous bridge publisher called `PrefabSystem.Update` recursively from
  `PrefabUpdate`, then called `PrefabInitializeSystem.Update` again. Initializing
  an entity twice therefore produced two entries with exactly the same entity/focus key.

This is not a CSS/layout or translated-name identity problem. Renaming UUIDs or
filtering duplicate buttons in the frontend would leave the native category corrupt.

## Change

Bridge generation is scheduled **before** the native prefab phase (including asset
loading). It only registers new/updated prefabs. `BridgePublicationSystem` runs
**after** that phase and completes the operation once. Neither path calls a game
system's `Update` manually. An existing prefab update waits until the outer
`PrefabSystem` has replaced its entity on the next pass.

Before activating placement or reporting success, publication verifies the live
entities, network instance archetypes, initialized section buffer, and exactly one
category membership per menu-visible generated prefab. Registration targets are
deduplicated by object identity. An incomplete result is retained but not activated.
The native toolbar's cached bindings and optional editor panel are refreshed without
rerunning prefab initialization. The general shared exporter helper is unchanged;
BridgeBuilder no longer uses its synchronous publication path.

Temporary previews still never enter the publisher. No generated asset, save, or mod
cache is removed or migrated. Existing menu buffers are rebuilt by normal game startup.

## Verification

`tools/PublicationCheck` exercises the actual publication class with a small fake
prefab registry/ECS and the native append-only menu behavior. It checks one-time
completion, multiple creations in one category, dependency deduplication, queued
updates, and failed initialization/duplicate-menu rejection. It does not generate
geometry or claim to reproduce the game renderer.

Manual acceptance after restarting the game: create two bridges from the same road
with the native road category open, reopen that category, rename/build one
through the manager, and confirm there is only one entry per bridge with
ordinary roads unchanged. Also check single/double-deck and track bridge categories.
No duplicate focus-key errors should be added to UI.log. This in-game check remains
for the user; the agent does not launch or control the game.
