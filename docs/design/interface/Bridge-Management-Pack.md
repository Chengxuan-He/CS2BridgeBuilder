# Bridge management and native asset pack membership

- Management retains prefab UUIDs as opaque action/selection keys only. The detail
  panel and create/rename success messages no longer display them.
- The name field submits RenameBridge on every nonblank change. Clearing the input
  leaves the saved name intact. Adjacent pending renames for the same identity are
  coalesced; other requests remain ordering barriers. Saved acknowledgements do not
  reset the active draft. There is no Rename button.
- The shared persistent AssetPackPrefab is named BridgeBuilder Asset Pack, with
  localized asset title BridgeBuilder and a dedicated blue-bordered pack icon. The
  toolbar keeps its separate, tightly cropped white mask. Existing pack UIObject
  icon references are updated in place without reinitializing network entities.
  The pack is saved and
  registered before a newly created bridge references it.
- Every generated permanent NetGeometryPrefab receives exactly that AssetPackItem.
  Preview graphs never enter this persistence path. Prototype dependencies and
  ContentPrerequisite components are unchanged.
- On catalogue refresh, exact export-state/registration identities and their exact
  generated carried-deck names receive the same membership. Only loaded writable
  assets are changed. Unregistered UUID-looking names, source roads, archetypes,
  saves and mod caches are not modified.
- Live migration updates only AssetPackElement, matching the decompiled native
  AssetPackItem.LateInitialize behavior (clear buffer, append pack entity). It does
  not replace or reinitialize a live network prefab entity.

Validation: Release compilation; 12-language UI checks including onchange, hidden
identity and blank-input safety; request coalescing/action-order checks. Native
asset-pack menu display and existing-bridge persistence require verification after
restarting the game. No geometry-generation test was run.
