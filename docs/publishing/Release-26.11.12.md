# Bridge Builder 26.11.12

Published on 2026-11-12 as public Paradox Mods revision **18**:
[Bridge Builder](https://mods.paradoxplaza.com/mods/160320/Windows).
The official SDK returned version 26.11.12 with `Updating=False` before download and installation.
See [the changelog](Changelog-26.11.12.txt) and [the maintained English listing](Mod-Description.md).

## Package and verification

The user explicitly waived the 55,000,000-byte package cap for this release only.
The complete official SDK ZIP is **77,898,073 bytes**, containing **965 payload files**.
No resource quality reduction or omitted dependency was used to meet a size target.
Package SHA-256: `54822457402B3E159AACF09226C2E186AFAA364E9D42BCB03CADD667AFC178BB`.

The build, resource/shader validation, all 12 localization checks and 59 generation scheduler
checks passed. Both DLLs passed official postprocessing. Four existing nullable warnings remained.
Release source commit `51d145554c0c9a72c48bd0cc665b709e67599190` was synchronized to all 18
existing GitHub branches before publication. The subsequent listing revision was synchronized
in commit `281208ad08943eda50f9af596d9dde3114fe6d85` before its metadata-only update.

## Listing and gallery

The live English, Simplified Chinese and Traditional Chinese descriptions were checked against
the prepared text, allowing only the server's Markdown bullet and HTML entity normalization.
The introduction has no subtitle. Each language groups four individually linked optional mods
under Compatibility, including Bridge Expansion Pack and Bridge Expansion Pack: B&P separately.
Prerequisites and caveats are under Notes. The published changelog also matched.

The seven approved images remain in the requested order: suspension gallery, custom draw/lift
bridges, road above/rail below, single-deck creation, double-deck creation, custom movable road,
and bridge management. The original PNG/SVG deliverables were preserved. Paradox rejected PNGs
larger than its 2.1 MB image limit, so upload-only JPEG derivatives retain 3840 by 2160 resolution,
composition and color treatment at 1,903,589–2,040,876 bytes each. All seven gallery files retrieved
by the official SDK matched the upload hashes and order. Direct CDN URL requests returned 404;
media verification used the SDK download, not an assumed successful public-browser render.

## Downloaded installation

All 965 files downloaded from published revision 18 matched the upload payload hashes.
With Cities2 absent, the existing active `pdx_mods/160320_17` installation was backed up in full,
obsolete material payloads were moved out, and the downloaded files replaced the installed payload.
Every installed payload hash matched the published download; the DLL assembly version is
26.11.12.0 and the UI reports 26.11.12. The existing native `.metadata` and `.cpatch` state was
preserved: the cache directory name remains 160320_17 until native SDK synchronization updates it.
No global playset synchronization was performed.

Original-only backup: `backups/release-26.11.12-original-installed-160320_17`.
Ignored local audit evidence is under `work/release-26.11.12/verified`, including payload hashes,
listing/gallery verification and installation hashes. No in-game visual acceptance is claimed;
the game was neither launched nor closed by this release workflow.
