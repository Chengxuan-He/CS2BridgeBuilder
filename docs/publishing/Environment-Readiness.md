# Paradox Mods publishing environment

Updated: 2026-09-23. This is the historical readiness record for that release candidate.

## Configured toolchain

- Unity 2022.3.62f2; its license and UnityModsProject initialization were verified on 2026-09-20.
- Burst 1.8.23, Collections 2.5.7 and Entities 1.3.10.
- Private .NET Runtime 6.0.36 for the official ModPostProcessor and ModPublisher.
- Official Burst tarball: `C:/Users/admin/Downloads/BridgeBuilder-Burst-1.8.23/com.unity.burst-1.8.23.tgz`. Do not delete it.
- Tool paths come from local `CSII_*` user environment variables. Do not store account credentials in the repository.

## Release candidate

Version **26.9.23** includes the list-loading animation, the gray double-deck bridge upper-width argument fix,
and the A-pylon double-deck bridge central cable-anchor fix. The latest two fixes were deployed;
final in-game near/far acceptance had not been explicitly recorded.
Candidate directory: `C:/Users/admin/Downloads/BridgeBuilder-release-26.9.23-20260923-r2`, replacing the earlier candidate from that day.

Run official postprocessing again for every package; do not reuse the old 0.3.6 DLL. Exact exit codes
and file hashes are recorded in the candidate's `PostProcess.log`, `ProcessedContent.json` and
`Readiness.json`. Use `publish-content` for upload only after postprocessing succeeds. In-game UI
acceptance of the new animation remained pending.

At the time of this record, uploading to Paradox Mods, creating a listing or publishing publicly had
not been authorized. See `Information-Entry.md` for metadata entry.
