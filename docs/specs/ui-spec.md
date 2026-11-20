# Native interface specification

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="repository-native-game-ui-windows"></a>
<a id="project-native-game-ui-windows"></a>

## Native game UI windows

All UI windows must be created through Cities: Skylines II native UI APIs and components.
This includes panels, modal dialogs, confirmation/message windows and file/directory pickers.
Do not substitute custom HTML/CSS window shells or operating-system dialogs for native game
windows. Mod-specific content, localized labels and bindings may be composed inside native
containers; styling alone does not make a custom window native.

Use the native window lifecycle, focus, input handling and modal backdrop. File or directory
selection must use the game's native picker interface rather than only a text field. Inspect
the installed game's API and existing native usage before choosing an interface; do not invent
unsupported calls. For example, the export flow uses `cs2/ui.ConfirmationDialog`, native input
and button components, and `DirectoryBrowserPanel` with mod-owned `WidgetBindings` and
`EditorWidgetListRenderer` inside the native directory-picker panel/backdrop theme,
with native `FocusRoot`, `AutoNavigationScope` and `BackConsumer`. Do not put the directory
page view in a message dialog text slot; it needs the native picker layout constraints. The options-only directory browser
does not update while the options screen is closed; do not use it for gameplay dialogs.
Keep dialogs out of non-interactive parents that suppress pointer events. Verify clicking,
text entry, selection, cancellation and confirmation in game before claiming interaction works;
compilation alone is insufficient. The computer-use prohibition still applies.

<a id="generation-creation-picker-ordering-and-categories"></a>

## Creation picker ordering and categories

Order the upper network, lower network and bridge-style cards by their displayed names using the
active game language's dictionary order. Use the stable ID as the ordinal tie-breaker. Refresh
ordering on a language change. A network card and its type filter use the same native network
category, regardless of the provider. Keep Road Builder provenance for identity, custom labels and
import. The bridge-type filter uses explicit structural families keyed by stable style ID. Combine
the selected family with search and the existing single/double-deck eligibility rules. Build the
family options from the installed styles that match deck mode and search, before applying the
selected family. Always offer All, including when no styles match. If the selected family disappears,
reset it to All. An admitted style contributes its family in every compatible mode automatically.
Extradosed styles retain their display names and share the cable-stayed filter. Filtering
the visible list must not change the selected asset or the player's custom bridge name.

## Dropdown dimensions and interaction

Every mod-owned filter dropdown, including network type, bridge family and management mode,
availability and style, shows exactly seven complete options at its maximum height when at least
seven options exist. With fewer options, it fits precisely the available count without blank rows.
All counts include All where present. No eighth partial option or clipped seventh option may be
visible at either scroll boundary. Use uniform border-box row heights; count padding, borders,
inter-row spacing and scrollbar space explicitly. Popup padding and borders belong outside the
option viewport. Row and viewport dimensions must use the same layout/scaling coordinate system.

Longer lists retain native vertical scrolling and access to every option. Both scroll limits
align to complete rows. Preserve this behavior at fractional UI scales and with translated labels.
The popup shares the trigger's width, keeps its full visible rows inside the screen and opens above
the trigger when the lower edge cannot accommodate it. Position the popup within the viewport
rather than clipping rows to resolve an edge collision. Selection updates only the corresponding
filter and closes the menu; leaving its focus scope or pressing Escape closes it. The trigger
exposes its expanded state and the list exposes its selected option. Long option text retains
access to its full localized label.

<a id="generation-full-published-source-mod-names"></a>

## Full published source mod names

Display the style's [ordered required-source array](cross-environment-spec.md#dlc-and-mod-compatibility)
in its declared order: direct source first, normally a source mod before its required DLC. Show
all registered sources, not only the direct source; an empty array represents base-game content.
Catalogue admission and preview/creation eligibility use the same all-sources decision.

Every user-visible source-mod label must use the full official title published on Paradox Mods.
This applies to all bridge styles and source assets, including native-network and non-Network
prototypes, catalogue cards, details/settings, saved-bridge source presentation and alternate/error
paths. Never substitute a numeric mod ID, package/folder/prefab name, technical alias or shortened
name. Preserve an author's abbreviation when it is part of the published full title; do not invent
its expansion. Localize the surrounding Mod/source prefix in every supported language, not the
publisher's title. Preserve user-entered bridge names and existing native DLC localization.

Verify known source titles by web search on Paradox Mods and the official mod page/API. Record the
exact title, mod ID, URL and verification date as evidence; when search indexing fails, record that
fact and the successful official-page/API verification separately. Local SDK/cache metadata may
supply the published title at runtime, but is not a substitute for the required development-time
web verification. Do not claim a local name has been externally verified when it has not.

Keep IDs for ownership, prerequisites, binding and persistence; display names must never become
source-selection keys. Use the full official title from the matched native mod metadata, with
verified recorded titles when required metadata is unavailable. An unresolved title uses an explicit
localized name-unavailable label, not the ID or a guessed expansion, and does not add a generation
or availability gate. Preserve the complete title in existing tooltips or equivalent accessible
full-text presentation when the compact card truncates it. Audit all source-label producers and
consumers together; do not repair only one bridge's card or rewrite healthy assets merely to update
a display label. Follow the [UI source presentation design](ui-spec.md#generation-full-published-source-mod-names).

<a id="generation-registered-style-behavior-parity-for-testing"></a>

## Registered-style behavior parity for testing

All registered bridge styles must use identical UI and operation behavior regardless of implementation
completeness. This requirement applies to fixed and movable styles.

- Never display a notice, warning, tooltip, badge, placeholder or other message stating that a bridge
  style is unfinished, incomplete, not ready or still under development. Its messages and notification
  behavior must be identical to those of completed styles.
- Do not disable creation, create-and-build or preview for any registered style because its
  implementation is incomplete or its generator is not connected. Do not hide such styles or prevent
  their requests from reaching the generator router. Apply the same ordinary input, DLC, source and
  deck-mode requirements to every style; completion status must not introduce an additional gate.
- A creation or preview request for a registered style without a connected generator must explicitly
  fail with an **ERROR** identifying the requested style ID and the missing generator route. Surface
  it through the same operation-failure handling used for completed styles and record an ERROR-level
  diagnostic. A silent return, warning-only log, disabled control, substitute bridge or reported success
  is prohibited. The error describes the actual routing failure, not the style's completion status.
- Use the same request dispatch, progress, success and failure handling for all registered styles.
  Report success only when the requested operation actually succeeds. This rule does not authorize
  publishing invalid or partial assets, or claiming that an unverified generator is complete.

<a id="23-gpu-computation-and-parallel-bridge-generation"></a>

<a id="contract-index-style-registration-and-ui"></a>

## New style integration

New bridge styles must follow bridge generation contract section 22: immediately synchronize the UI and
connect the new bridge style to the generator router. Update the creation catalogue, preview,
both creation actions and management labels in the same implementation increment, and provide
style names in every supported UI language. Stable style IDs remain unlocalized. Verify translated
names after game-language changes and preserve user-entered bridge names. Backend generation alone
does not complete a new style without its corresponding working UI entry.

For testing, all registered styles follow the same interaction and notification behavior regardless
of implementation completeness. Never show an unfinished-style message or disable creation and
preview on that basis. Dispatch requests normally; a missing generator route must produce an explicit
ERROR with the style ID through ordinary operation-failure handling. See contract section 22.

## Search, filters and text-free preview

Remove the legacy operation-status subscription and its props in both views; retain preview status only to stop the spinner on failure. Removing the footer is structural, not CSS hiding. Existing bridge assets are unaffected; deployment checks do not replace human acceptance after restart.

- Each upper/lower network picker has independent name/ID search and a stable network-type filter. The backend classifies RoadPrefab by HighwayRules / PublicTransport, PathwayPrefab as pedestrian, and the existing track catalogue by train/subway/tram. It does not infer a network type from a localized name or confuse RoadBuilder provenance with a transport type.
- The creation lists sort by localized display name and use the stable ID to break ties. Bridge-style search matches the name or ID within the selected deck mode. The family filter offers only families represented in those results and returns to All if its selection disappears. Low-tower cable-stayed styles belong to the cable-stayed family. Management combines name, UUID or style search with deck count, style and availability.
- All filter menus follow [Dropdown dimensions and interaction](#dropdown-dimensions-and-interaction).
- Filtering changes the visible list only. It preserves selected IDs and edited names, does not publish preview requests or generate/delete assets, and resets only the list scroll position. Upper/lower picker state is independent. List layout follows [List layout and refresh](#list-layout-and-refresh).
- The preview frame contains no text in idle, loading, success or failure states. A CSS spinner uses an explicit backend loading flag rather than comparing localized strings. Decode errors or failed assembly stop it; existing error dialogs and logging are retained. The create/manage panel error footer and its preview-error callback are permanently removed; do not restore inline error text below the action buttons. Image alt text is empty to prevent a broken-image text fallback; the frame retains an accessibility label.
- Search/filter labels, options and empty results use the same 12-language dictionary and stable, nonlocalized filter IDs.

## Panel content and navigation

The toolbar opens and closes the Bridge Builder interface and indicates its open state. The header
provides Create and Manage tabs and an accessible close control. Keep the native window lifecycle
specified above. Two bounded content regions preserve access to the game toolbars and allow map
interaction through their intervening gap. They use game-scaled dimensions; long localized header
labels must not displace the tabs or close control and retain full tooltips.

Creation presents road selection on the left and preview, deck mode, bridge selection, name and
actions on the right. Single-deck mode shows one road picker; double-deck mode shows independent
upper and lower pickers and the direction control. The Custom road action preserves the current
creation draft across the Road Builder handoff. Management presents the searchable saved list on
the left and the selected bridge's preview, editable name and fixed action row on the right.
Selection is keyed by stable identity, never by visible position or translated text.

Search uses every whitespace-separated term against the documented name/identity fields, ignores
case, and Escape clears only that search. Distinguish an empty catalogue from a search/filter with
no matches using the corresponding localized empty state. Keep the preview, naming, source,
image and action rules in their dedicated sections below rather than adding separate per-list rules.

## List layout and refresh

Upper roads, lower roads, bridge styles and managed bridges share exactly three equal card cells
per row. A partial final row retains the same column widths and positions with empty cell space.
Do not switch to two columns after loading, scrolling, resizing or toggling scrollbar visibility.
Reserve the same scrollbar gutter permanently: the track may hide, but its space and the content
width remain unchanged. Native wheel and thumb interaction remain available; list wheel travel is
twice the native sensitivity-scaled delta. Bound each list to its panel slot rather than allowing
content to enlarge the panel. Preserve these rules across resolution, DPI and game UI scaling.

A background catalogue refresh retains published content while its replacement is pending. It must
not replace populated lists with a loading spinner, empty data or recreated cards. The first load
may show the localized loading indicator. Real deletion, a committed empty result or a filter with
no matches takes effect immediately; retaining content while loading does not authorize stale rows.

Use stable asset identities for cards independently of row boundaries. Insertion, deletion,
sorting, new binding objects and unchanged periodic refreshes preserve surviving card and image
nodes, focus, selection and list scroll. Unchanged image URLs must not be cache-busted or reloaded.
A changed icon source or actual image error still takes the documented update/fallback path.
Only a user change to that list's search, filter or deck mode resets its scroll to the top; ordinary
refresh does not. Do not remount a list to implement scroll reset. Real removal may transfer focus
away from the removed element without disturbing surviving controls.

## Card names and animation

All four card lists use the same title treatment and retain a full-name tooltip, including custom
names and translated names. The label viewport has two outer fade bands, each 8% of its width,
and an unobscured central text region occupying the remaining 84%. Calculate fit against that
central region, not the entire viewport. Short and exactly fitting titles are centered in it.
Long titles begin at its left boundary, with the entire left fade band reserved before the first
glyph. The resting first glyph and the final glyph at the opposite endpoint must remain outside
the faded bands; fade only text passing through an outer band during motion.

New cards have their correct static alignment on their first visible frame. Locale, font loading,
resizing and transitions between short and long text update alignment without exposing a clipped
left prefix. Static placement must not depend on a late effect, stale measured width or a zero-size
first measurement. Keep the clipping viewport independent of the intrinsic text width; measuring
text must not enlarge its container or feed back into card/grid width. Use layout coordinates
consistently rather than applying screen-scaled rectangles as CSS pixel widths or translations.

Hovering or selecting an overflowing title starts smooth travel immediately toward the endpoint
where its last glyph meets the right boundary of the central region. Pause for three seconds at
each arrival, then reverse. Continue while either hovered or selected; short titles never animate.
When both states end, clear all animation displacement and restore the appropriate centered or
left-aligned baseline. Unchanged bindings, same-size observer events and background refresh must
not restart motion. Genuine text/font/size changes recompute overflow. Cancel animation frames,
observers and listeners on replacement or unmount; do not introduce permanent idle polling merely
to establish static alignment. Preserve access to the complete name through the tooltip even if
animation is unavailable.

## Export and directory dialogs

Export acts on the selected saved bridge and disables repeated submission while busy. The native
confirmation dialog contains the editable destination and top-level folder name, a native browse
action and localized validation. Reject empty/invalid destination or folder input; preserve the
existing folder-name length, reserved-device-name and invalid-character restrictions. Native folder
browsing uses the focus/navigation hierarchy described above and returns to the export form without
losing its draft. Cancellation does not export. Successful export displays the actual path with
Open folder and Close actions; long paths wrap within the native dialog. Directory options publish
the title of the interacted field, without overwriting unrelated native picker titles.

## Candidate, saved asset and map names

```mermaid
flowchart TD
A[Selected style and decks supply localized suggestion] --> B{Player edits draft?}
B -->|Yes| C[Preserve manual draft]
B -->|No| D[Use common suggested-name formula]
C --> E[Preview and creation consume current draft]
D --> E
E --> F[Save submitted asset display name through native presentation]
G[Placed map naming] --> H[Apply separate native proper-name localization and preserve player map renames]
```

All bridge styles use the same suggested Prefab display-name formula, with no fixed, movable,
landmark or prototype exceptions:

- Single deck: `{localized prototype}·{localized road}`.
- Double deck: `{localized prototype}·{localized upper road}·{localized lower road}·{localized same/opposite direction}`.

Use the selected prototype/style's localized display label, the actual selected network display
names and the existing localized direction label. Preserve Road Builder and player-authored network
display names. The middle-dot separator is `·`; do not omit the prototype, a selected deck or the
double-deck direction, and do not substitute a forced landmark-only or road-only name.

The player can edit the suggestion. Preview, Create and Create-and-build use the same current
draft, and permanent saving preserves the submitted display name through the existing native
locale/presentation path. Do not replace a manual draft with a suggested formula or map name.
Later rename and language behavior follow the common rules below; stable internal Prefab IDs
and owner UUIDs remain independent of display text.

A prototype proper name applies only to the placed bridge's native map/street name. This includes
localized landmark or bridge names such as Golden Gate Bridge or Grand Bridge, and the
[Nanpu and Lupu map-name rule](non-network-spec.md#non-network-prototype-proper-name-exception).
Do not propagate that map-name choice into candidate text, saved asset display names or internal
Prefab identities. Map naming uses the native localization lifecycle and preserves player map
renames; it does not authorize hardcoded language-specific suffixes.

Acceptance checks the complete single-deck and double-deck formulas for every style, edited-name
preservation through preview and both creation actions, saved/renamed asset names and localized
map names as separate outcomes. No named prototype may bypass the common asset-naming path.

## Creation and placement actions

```mermaid
flowchart TD
A[Submit current selections and name] --> B{Requested action}
B -->|Create only| C[Create and register formal bridge; retain panel and tool]
B -->|Create and build| D[Carry BuildAfterCreate intent through queue]
D --> E[Complete native initialization and display-name registration]
E --> F[Activate formal construction prefab]
G[Manage Build existing UUID] --> F
F --> H{Activation succeeds?}
H -->|Yes| I[Close panel idempotently and clear preview]
H -->|No| J[Report failure and retain UI]
```

- Management retains prefab UUIDs as opaque action/selection keys only. The detail
  panel and create/rename success messages no longer display them.
- The name field submits RenameBridge on every nonblank change. Clearing the input
  leaves the saved name intact. Adjacent pending renames for the same identity are
  coalesced; other requests remain ordering barriers. Saved acknowledgements do not
  reset the active draft. 

- `CreateBridge` creates and registers the formal bridge only; the panel and current tool remain unchanged. `CreateAndBuildBridge` carries explicit `BuildAfterCreate` intent through the queue and native publication callback. Both use the same selected networks, style and custom display name.
- Create-and-build activates the formal prefab only after native initialization and display-name registration succeed. Successful native tool activation closes the panel idempotently and clears its preview. Validation, publication or tool activation failure does not dismiss the UI. Create-only retains the existing translated `CreatedManage` status.
- Management has a fixed Build/Export/Delete action row outside the details scroller. Rename
  is submitted from the nonblank name field without a separate Rename button. Build activates the selected existing UUID directly, without generating another bridge or temporary copy. Unavailable assets disable Build. Successful activation follows the same close/preview-cleanup path.
- Creation buttons occupy a separate row below the name field so longer translations do not squeeze the name input. The new Create label is translated into all 12 languages.
- Offline checks exercise both UI handlers, missing selections, existing-UUID activation, fixed action placement and request intent. Native tool switching and panel layout still require human verification after restarting the game.

## Catalogue image dimensions

Catalogue card images use a fixed appropriate display height and an explicit width calculated
as height multiplied by the source image's intrinsic width/height ratio. Resolve the intrinsic
dimensions from the actual native image resource or vector dimensions/viewBox in the backend,
accepting SVG files with an XML declaration, UTF-8 BOM and standard DOCTYPE such as Road Builder
thumbnails. Ignore the DTD when reading dimensions and disable external resolution; reading the
canvas must not fetch a DTD or expand declared entities. Preserve the original source URI and bytes,
and send the ratio with the icon URI. Set both numeric display dimensions; do not rely on Cohtml
`width:auto`, `object-fit`, browser `onLoad` events or `naturalWidth`/`naturalHeight` behavior.
Apply the same rule to network, bridge-style and management cards, including fallback images and
subsequent source changes. Do not gate visibility or assign
zero width while waiting for JavaScript image-load events or intrinsic-size properties;
cached images and Cohtml event differences must not leave every card blank. Do not clamp
width independently of the fixed height or stretch images into a fixed rectangular box.
Preserve original image content and transparent margins: a wide drawing inside a square image
does not authorize cropping or changing the canvas ratio. If source dimensions cannot be resolved,
show the existing fallback image at its known ratio rather than guessing the source's dimensions.
Preserve the error path. Tests must include square, wide and tall resources and rendering with no
load callback at all; offline
JavaScript checks do not establish actual Cohtml layout acceptance.

## Double-deck direction and locale behavior

The runtime create/manage panels use the same game locale as the existing settings and bridge-style translations. `Settings/RuntimeUiText.cs` supplies the runtime UI dictionary in 12 languages: English, Simplified Chinese, Traditional Chinese, German, Spanish, French, Italian, Japanese, Korean, Polish, Brazilian Portuguese and Russian.

Both creation actions and preview pass an immutable recipe through `BridgeRecipeReader`. `UiStringCatalog.Resolve` canonicalizes locale IDs, supports language/region fallback and distinguishes Chinese scripts. `BridgeBuilderUISystem` publishes the current dictionary. Operation status stores a translation key and original arguments; preview status stores its key. The catalogue selects and saves stable internal IDs.

Every double-deck style exposes the localized `LowerDeckOpposite` toggle, enabled by default and chosen per request. Preview identity includes direction; a late image for the previous direction must not replace the current one. Existing bridges retain their stored `AuxiliaryNets` arrangement without regeneration or editing.

Unsupported languages fall back to English. Language changes refresh asset labels and translate existing status messages without requesting another preview or rebuilding geometry. Preserve Road Builder names, custom bridge display names, deck IDs and prefab UUIDs. Translate panel labels, empty states, accessibility labels and tooltips. The JavaScript English fallback must match the C# table. Bound long header labels and expose full text in tooltips. Content-source label translation changes presentation only, not prerequisites or ownership.

## Native field and filter presentation

The implementation uses `game-ui/common/input/text/text-input.tsx` (`TextInput`), `editor-item.module.scss` and `FOCUS_DISABLED`. Explicit widths provide the full-width creation name field in Cohtml. The shared `SearchPlaceholder` expresses "Search" followed by an ellipsis in each supported language. The three packaged SVG masks replace font-dependent filter triangles and supply filter/search symbols.

Search/name fields use native game components and theme instead of browser-default borders. Keep the creation name field full-width. Search fields retain contextual accessibility labels and the shared 12-language `SearchPlaceholder`. Each filter has a white outline funnel and filled downward triangle on its left; each search has a left-side magnifier. Use the packaged independent SVG masks, with no Find It dependency or cache reference.

## Verification references

Follow the [runtime UI verification procedure](../guides/runtime-ui-verification.md). Retained [historical check results](../archive/audits/runtime-ui-verification-history.md) have limited provenance.

Management validation evidence is retained in [bridge management verification](../archive/audits/bridge-management-verification.md).
