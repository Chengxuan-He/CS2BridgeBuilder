# Runtime UI verification

## Offline and human checks

For runtime UI or localization changes, run `node tools/CheckRuntimeLocalization.mjs` and `dotnet run --project tools/LocalizationCheck/LocalizationCheck.csproj -c Release`, followed by `tools/Build.ps1`. These check translation coverage, format placeholders, the real C# locale resolver and status handling, UI labels and track selection. They do not launch the game or generate bridge geometry. Documentation-only changes require a localization review but no build or deployment, as specified in AGENTS.md.

Human verification: open the create and manage panels, switch the game language, and confirm that labels and existing status messages change, custom names/UUIDs stay intact, and longer labels fit with scrolling and close controls still usable. Cohtml layout and native dialog appearance have not been visually verified by the offline checks.

## Lists, titles and dropdown acceptance

Inventory every shared consumer before changing card or scroll behavior: upper and lower network
pickers, creation styles, management cards and all FilterBox menus. Compare frontend bindings and
backend loading publication so a refresh is not mistaken for a committed empty result.

Run `tools/CheckUiRefresh.mjs` with the task's React/ReactDOM/jsdom package manifest and
`tools/CheckUiLayout.mjs` with a permitted headless browser executable, that package's node_modules,
a task work directory and the installed game's UI stylesheet. These are offline fixtures, not native
Cohtml acceptance. Keep generated profiles and evidence in the task work directory.

Exercise insertion, reorder, deletion, search, filter, selected identity, focused controls, icon
failure/source replacement and repeated loading notifications. Check surviving DOM/image identity
and scroll offsets. Render short, exact-fit and long localized/custom titles at fractional sizes and
scales, with zero initial dimensions and delayed font/layout changes. Measure both fade boundaries,
the actual animation endpoints and three-second pauses, inactive reset and observer/frame cleanup.
Verify unchanged binding/size events do not reset animation progress.

Render dynamic short-to-long-to-empty-to-long lists with the scrollbar visible and hidden. Confirm
three cells per row, a stable content width/gutter and unchanged first-paint item/title coordinates.
For every dropdown, test fewer than seven, exactly seven and more than seven options, full rows at
both scroll boundaries, native scrolling, translated long text, fractional scaling and bottom-edge
placement. Record actual measured boxes rather than treating jsdom as a layout engine.

After build and installation under the [deployment contract](../contracts/development-contract.md#generation-13-deploy-coherent-increments-and-preserve-healthy-assets),
ask the user to perform the same interactions in game. Follow the constitution's
[manual-start boundary](../constitution.md#repository-game-and-launcher-startup-is-manual) and
[computer-use prohibition](../constitution.md#repository-computer-use-tools-are-prohibited).
Report offline results and user-observed Cohtml acceptance separately.
