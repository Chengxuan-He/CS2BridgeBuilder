# Bridge Builder 26.11.19

## Status

Published on 2026-11-19 as Paradox Mods revision 23, version 26.11.19. The official SDK confirmed availability with Updating=false, downloaded the published revision and installed it after confirming that the game was absent. All 953 payload files matched the reviewed candidate by SHA-256. The previous published release was 26.11.18, revision 22; the 26.11.18a preparation was never published. Source commit db1ac96 was fast-forwarded atomically to all 18 remote GitHub branches and verified by remote readback.

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
## Publication and installation evidence

The live title is Bridge Builder, the game range is 1.6.*, and the English changelog matches the candidate. The English, Simplified Chinese and Traditional Chinese description and original mod-link titles match after normalizing server Markdown escaping, HTML entities and whitespace.

The SDK download initially hit its cross-volume move restriction when targeting E:. Downloading to a C: work directory succeeded; no production code was changed. Original installed files from revisions 17, 18 and 20 were backed up and hash-verified under E:/unused/release-26.11.19-installed-originals. Revision 22 was already absent at installation time. Revision 23 was installed by the official SDK and every candidate payload hash was verified again. No old installation directory was deleted.
