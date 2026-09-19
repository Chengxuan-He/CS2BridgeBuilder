# Runtime UI localization

The runtime create/manage panels use the same game locale as the existing settings and bridge-style translations. `Settings/RuntimeUiText.cs` contains 98 runtime UI keys in 12 languages: English, Simplified Chinese, Traditional Chinese, German, Spanish, French, Italian, Japanese, Korean, Polish, Brazilian Portuguese and Russian.

## Double-deck direction

Every double-deck style exposes the localized `LowerDeckOpposite` toggle. It defaults to on, matching the previous runtime behavior, but is now a per-request choice rather than a hardcoded value. Both creation actions and preview send an immutable recipe object through `BridgeRecipeReader`. Preview identity includes direction, so a late image for the previous direction cannot replace the current one. Existing bridges still render their stored `AuxiliaryNets` arrangement without regeneration or editing.

- `UiStringCatalog.Resolve` canonicalizes locale IDs, supports language/region fallback and distinguishes Chinese scripts. Unsupported languages fall back to English.
- `BridgeBuilderUISystem` publishes the current text dictionary and refreshes asset labels on a game-language change. Road Builder names, custom bridge display names, deck IDs and prefab UUIDs are preserved.
- Operation status retains a translation key and its original arguments. Preview status retains its key. Language changes translate existing messages without requesting another preview or rebuilding geometry.
- The JavaScript panel uses translated labels, empty states, accessibility labels and tooltips. Its English fallback must match the C# table. Long header labels are bounded, with full text in tooltips.
- Content-source labels are translated only for presentation; prerequisite checks and ownership rules are unchanged. Names of third-party content follow the available game dictionary.

## Checks

Run `node tools/CheckRuntimeLocalization.mjs` and `dotnet run --project tools/LocalizationCheck/LocalizationCheck.csproj -c Release`, followed by `tools/Build.ps1`. These check translation coverage, format placeholders, the real C# locale resolver and status handling, UI labels and track selection. They do not launch the game or generate bridge geometry.

Human verification: open the create and manage panels, switch the game language, and confirm that labels and existing status messages change, custom names/UUIDs stay intact, and longer labels fit with scrolling and close controls still usable. Cohtml layout and native dialog appearance have not been visually verified by the offline checks.

## Search, filters and text-free preview (2026-09-19)

- Each upper/lower network picker has independent name/ID search and a stable network-type filter. The backend classifies RoadPrefab by HighwayRules / PublicTransport, PathwayPrefab as pedestrian, and the existing track catalogue by train/subway/tram. It does not infer a network type from a localized name or confuse RoadBuilder provenance with a transport type.
- Bridge-style search filters the currently selected single/double-deck catalogue by display name or ID. Management searches display names, UUIDs and style labels, combined with deck count, style and availability filters.
- Filtering changes the visible list only. It preserves selected IDs and edited names, does not publish preview requests or generate/delete assets, and resets only the list scroll position. Upper/lower picker state is independent. All lists keep three explicit card cells per row.
- The preview frame contains no text in idle, loading, success or failure states. A CSS spinner uses an explicit backend loading flag rather than comparing localized strings. Decode errors or failed assembly stop it and report through the panel status bar outside the frame; existing CRITICAL reporting is retained. Image alt text is empty to prevent a broken-image text fallback; the frame retains an accessibility label.
- Card buttons and block-level name labels explicitly center their text. Search/filter labels, options and empty results use the same 12-language dictionary and stable, nonlocalized filter IDs.

Release compilation, JavaScript syntax, and nonvisual localization/filter/selection-state checks passed. No geometry tests or game control were performed. In-game verification remains necessary for spinner animation, centred card labels, dropdown focus/scroll behavior, long translations and double-deck picker sizing.

## Creation and placement actions (2026-09-19)

- `CreateBridge` creates and registers the formal bridge only; the panel and current tool remain unchanged. `CreateAndBuildBridge` carries explicit `BuildAfterCreate` intent through the queue and native publication callback. Both use the same selected networks, style and custom display name.
- Create-and-build activates the formal prefab only after native initialization and display-name registration succeed. Successful native tool activation closes the panel idempotently and clears its preview. Validation, publication or tool activation failure does not dismiss the UI. Create-only retains the existing translated `CreatedManage` status.
- Management has a fixed Build/Rename/Delete action row outside the details scroller. Build activates the selected existing UUID directly, without generating another bridge or temporary copy. Unavailable assets disable Build. Successful activation follows the same close/preview-cleanup path.
- Creation buttons occupy a separate row below the name field so longer translations do not squeeze the name input. The new Create label is translated into all 12 languages.
- Offline checks exercise both UI handlers, missing selections, existing-UUID activation, fixed action placement and request intent. Native tool switching and panel layout still require human verification after restarting the game.

## Native fields and vector icons (2026-09-19)

Find It's installed UI module uses `game-ui/common/input/text/text-input.tsx` (`TextInput`), the
`editor-item.module.scss` input class and `FOCUS_DISABLED`. All BridgeBuilder search/name fields
now use these same native game components/theme, not raw HTML inputs with browser-default borders.
Explicit widths keep the creation name field full-width in Cohtml. Search fields use the shared
12-language `SearchPlaceholder` (Simplified Chinese: 搜索……), retaining contextual accessibility labels.

The font-dependent dropdown triangle is removed. Each filter's left side contains a white outline
funnel and a filled downward triangle; every search has a left-side magnifier. These are small,
independently drawn SVG masks packaged with BridgeBuilder, inspired by Find It's icon presentation;
there is no Find It dependency or reference to its cache. Build output includes all three assets.
Offline checks cover field properties, translations and icon order/presence. Actual native focus,
typing, placeholder display and layout remain a human in-game check after restart.
