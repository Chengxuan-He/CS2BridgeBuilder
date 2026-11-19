# Bridge Builder 26.11.19

## Status

Release candidate prepared on 2026-11-19. Paradox Mods publication and installation are pending; the official publisher reports no active cached login. The previous confirmed published release is 26.11.18, revision 22. The 26.11.18a preparation was never published.

## Changes

See the [English upload changelog](Changelog-26.11.19.txt). The complete [three-language description](mod-description.md) is retained unchanged; the title remains Bridge Builder and linked mod titles retain their original spelling.

## Candidate and verification

The current native Player.log reports game version `1.6.4f1 (1097.158a) [6360.18886]`; the declared supported range is `1.6.*`.

- Officially postprocessed DLL SHA-256: `D1E421DD2D1E7638AD91B35A8112FC93A62A331B3A4AFB5CAB2EB80F1EC14DA3`.
- Official SDK ZIP: 80,950,237 bytes, 953 files; SHA-256 `FB9654734BB16D576F25D38F032B8AD4AFB9B4E0DBEE4DB0A18BEDD33D3F756B`.
- Canonical Release build, native material/resource checks, official IL postprocessing and distributed-artifact portable-path checks passed.
- Independent Windows managed/compiler round: six compiled harnesses passed, with actual D3DCompile shader compilation. Independent Windows mapping and permissions round: 18 mapping and 21 permission cases passed.
- Raw evidence and candidate/source binding: `work/release-26.11.19/`. Managed harnesses are not native gameplay acceptance. AMD rendering, GeForce NOW and other unavailable hosts remain untested; no game or launcher was started by agents.

The existing three-stage custom-program registration is retained: OnLoad restoration, loading-complete restoration and generation/preview on demand. This release does not claim to have diagnosed or fixed the intermittent startup crash.