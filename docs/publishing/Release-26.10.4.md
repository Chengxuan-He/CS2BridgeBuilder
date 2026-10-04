# Bridge Builder 26.10.4

## Changelog

- Fixed deleted Road Builder roads remaining in the selection list.
- Added atomic bridge registration saves and backups to prevent record loss.
- Improved recovery from interrupted bridge creation, diagnostics and maintainability.

## Acceptance and release scope

The user confirmed in-game acceptance on 2026-10-04 and authorized publication to every existing
GitHub remote branch and the existing Paradox Mods entry (160320). The project, UI and release
configuration use version 26.10.4. This updates the existing 26.10.4 release with the accepted
registration/recovery fixes; no geometry changes were made during release preparation.

Validation: seven non-visual C# check projects and six JavaScript checks passed. The local deployment's
12 payload files matched the build by SHA-256. Shared source is now checked in with its SHA-256
manifest. These checks supplement the user's acceptance; they do not establish universal game or
save compatibility.
