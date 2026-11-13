# Preview continuations and card icon dimensions

## Corrected behavior

The private preview course now includes non-rendered continuation edges before native node selection. Presentation cuts therefore describe a continuing network rather than isolated dead ends. Native node geometry and structural attachments remain present. Lane meshes now use the same native course and elevation transform as the network composition, including native rejected-submesh flags, instead of a second nominal straight course.

Card dimensions come from backend image headers or SVG intrinsic dimensions. All three card consumers use a fixed 62 rem height and explicit proportional width, without browser load events, `naturalWidth`, CSS auto sizing or `object-fit`. Missing metadata displays a visible fallback. Native `assetdb://gameui` aliases and local UI hosts follow registered resource locations.

Original GO, Truss Viaduct and Art Deco icons have respective canvas sizes 256×256, 300×300 and 150×150 pixels. All display at 62×62 rem. GO's wide silhouette occupies only part of its square transparent canvas; cropping that padding would change the published image. Original bytes, canvas/alpha bounds and hashes are recorded in `artifacts/preview-caps-icons/source-icons.json`.

## Verification and remaining acceptance

The production build passes with four existing nullable warnings. Eleven intrinsic-dimension cases, malformed JPEG rejection, callback-free card sizing/fallback/source changes, 35 production object-placement cases, and JavaScript/C# localization checks for all twelve languages pass. The placement tests use synthetic curves; they do not execute the complete private native ECS course or Cohtml renderer.

[The Arc investigation](../bridge-styles/ArcPedestrian/Implementation.md#reported-preview-structure-investigation) verifies the real structural mesh and measured 22.5 m adaptation. Its thin arches and circular native pillar alternatives must not be replaced based on the catalogue icon. No recorded structural face explains the white panel. The shared lane/course inconsistency is corrected, but disappearance of the reported white panel and caps requires a new in-game preview; no live visual acceptance is claimed.

An optional one-shot `ModsData/BridgeBuilder/PreviewMaterialCapture.request` containing `Arc` targets a completed preview whose draw source includes that text. It records completed preview pixels, mesh/material bindings, native composition matrices and source labels under `PreviewMaterialCaptures`. It neither creates a bridge nor modifies saved assets. An empty request retains the earlier generated-shader capture behavior. Inspect the Arc preview with the same selected road, then compare the three square source icons and a rectangular native road icon. No game process is started by these checks.
