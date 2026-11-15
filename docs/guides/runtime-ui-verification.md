# Runtime UI verification

## Offline and human checks

For runtime UI or localization changes, run `node tools/CheckRuntimeLocalization.mjs` and `dotnet run --project tools/LocalizationCheck/LocalizationCheck.csproj -c Release`, followed by `tools/Build.ps1`. These check translation coverage, format placeholders, the real C# locale resolver and status handling, UI labels and track selection. They do not launch the game or generate bridge geometry. Documentation-only changes require a localization review but no build or deployment, as specified in AGENTS.md.

Human verification: open the create and manage panels, switch the game language, and confirm that labels and existing status messages change, custom names/UUIDs stay intact, and longer labels fit with scrolling and close controls still usable. Cohtml layout and native dialog appearance have not been visually verified by the offline checks.
