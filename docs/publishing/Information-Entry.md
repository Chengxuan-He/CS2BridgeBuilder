# Bridge Builder — publishing information entry

Updated: 2026-09-23. Release candidate: **26.9.23**; assembly version: **26.9.23.0**.

This candidate includes the list-loading animation and two subsequent double-deck fixes. Earlier
acceptance does not cover those later changes; the latest near/far and UI acceptance still needs an explicit record.

## Files prepared for publication

Candidate directory: `C:/Users/admin/Downloads/BridgeBuilder-release-26.9.23-20260923-r2`, replacing the earlier candidate from that day.

Use `publish-content` for a subsequent upload, not `content`, `postprocessed` or `bin/Release`.
The package's `PostProcess.log` and `ProcessedContent.json` establish the official postprocessing result.
See `Readiness.json` for the source commit and content hashes. This step prepares files only; it does not upload to Paradox Mods.

## Fields to enter or confirm

- Name: Bridge Builder; version: 26.9.23.
- Compatible game version: requires confirmation; keep `CONFIRM_BEFORE_PUBLISH` in the draft.
- Visibility: Private in the draft; confirm before public publication.
- ModId: leave empty for the first publication, then retain the assigned ID after success.
- Description: `Mod-Description.md`, maintained in English under the documentation policy.
- Cover: `Thumbnail.png`, the 800×800 Rajdhani version with a solid background.
- Screenshots: use the final game version; see `Image-Brief.md`.
- Road Builder is optional compatibility, not a mandatory dependency.
- Source: https://github.com/Chengxuan-He/CS2BridgeBuilder .
- The user signs in through official tools. Do not put passwords or tokens in the repository.

Deleting a generated bridge removes its corresponding placed instances. Missing-bridge save repair
does not automatically overwrite the original on-disk save; advise players to back up and save separately.
