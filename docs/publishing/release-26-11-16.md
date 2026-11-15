# Bridge Builder 26.11.16

## Release

Public version: **26.11.16**. CLR assembly/file version: **26.11.16.0**.

## Changelog

- Use mod-owned precompiled material programs with stable queue-specific Surface references; new bridges and exports contain no shader program payloads. Bridge Builder must remain enabled.
- Discontinue compatibility with legacy owner-persisted custom-shader bridges. Only the existing self-check flow identifies their obsolete protocol and performs owner-scoped backup/removal and restart handling; healthy noncustom bridges remain preserved.
- Correct mapped-directory handling for persistence, backup and removal while retaining ownership and permission boundaries.

## Verification and publication status

Status: **Published and installed from the official download**. Paradox Mods listing `160320` reports public, published revision **21**, version **26.11.16**, with `Updating=false`. The remote changelog matches the three entries above after normalization.

| Artifact | Identity |
| --- | --- |
| Postprocessed DLL | Assembly/file version 26.11.16.0 |
| DLL SHA-256 | `BB6BA09D12EB459308D1A9C43AF6FFDB709D1AADDF2D1CAD2AE33B795E60F131` |
| Official SDK ZIP | 77,788,081 bytes |
| ZIP SHA-256 | `A52EBF073D99D1F0470EEB94BEE0EA7499A34C41A5BA47342C9F1B9324AC6941` |

The official SDK `DownloadAndInstallMod` installed revision 21 into native cache directory `160320_21`. All **965 payload files** match the uploaded SDK ZIP by hash. Native local data confirms revision 21 is subscribed and enabled in the active playset. The game was absent during installation.

All **2,907 files** in the previous revision 17/18/20 cache directories remained unchanged. The original-only revision-20 backup contains **969 files**, totaling **275,413,534 bytes**, under `E:/unused/CS2BridgeBuilder-release-26.11.16/installed/160320_20`.

Release source commit `a53b10636ef21a4a09cbd383c2a205d4d4fc04b9` and changelog correction `ad9865e` were synchronized to all **18 existing GitHub branches**. Verification receipts are retained under `work/release-26.11.16/verified/` and `work/release-26.11.16/amendment-readback/`.

The [short changelog](Changelog-26.11.16.txt) is the upload text.
