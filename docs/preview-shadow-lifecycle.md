# Preview shadow lifecycle (2026-09-21)

## Follow-up after reproduction on Golden Gate preview

The lifecycle-only change did not resolve the user's reproduction. Further shipped
IL inspection found that `HDRPDotsInputs.FillVisibleLights` merges Unity and ECS
lights. Sort keys put Point before ProjectorBox, but
`HDGpuLightsBuilder.CalculateAllLightDataTextureInfo` processes only
`min(sortedLightCounts, HDRPDotsInputs.s_NumUnityLights)` entries of that sorted
list. A preview box shadow owner can fall beyond that prefix after ECS points are
merged, leaving its reservation unpopulated. This explains a concrete route to
the null request; the exact runtime list was not captured.

The preview shadow owner now uses a Unity Point light: Unity source indices precede
the appended ECS point indices within that type's sort key. Box fills do not cast
shadows. The point is placed remotely relative to the model bounds, with centre
illuminance maintained using candela = lux * distance squared. Shadow range and
fade include that position. No city-light global switch, exception suppression or
shadow-manager patch is used. Build/install passed; Golden Gate preview and its
lighting still require in-game acceptance.

The reported `HDShadowManager.PrepareGPUShadowDatas` exception points to the
request-array read beginning at IL_0035. The shipped method dereferences
`m_ShadowRequests[i].isInCachedAtlas` at IL_003d after asserting the slot is non-null.
This is an unpopulated shadow request, not evidence of a missing surface material.

The preview previously changed every private light's enabled state in
`beginCameraRendering`. The shipped HDRP collects camera culling results in
`TryCull` before executing render requests. Per-camera light mutation therefore
does not guarantee that the light state seen during request execution matches
the state during culling. This is a diagnosed lifecycle risk; the supplied stack
alone cannot establish which light produced the empty request.

The stage now enables its finite box lights once at initialization and leaves
them stable across camera callbacks. Completion hides the stage on a later engine
frame. Isolation still uses a private scene, layer 31, light layer 128 and a stage
100 km below the city. The independent key retains punctual shadows; city sun
settings and the global HDRP shadow manager are not modified.

Build and local installation passed. No visual unit tests were run. In-game
verification remains required: repeatedly open/switch/close previews while the city
camera renders, confirm independent shadows and no new shadow-manager exceptions.
If the error persists, retain the full log and the triggering action; do not treat
this lifecycle change as proof that every cause of an empty shadow request is fixed.
