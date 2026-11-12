# Bridge Builder 26.11.12a

- Isolate prepared materials from automatic startup discovery and register selected resources on demand.
- Check native content availability before accessing Bridges & Ports materials, addressing startup VT failures when that content is absent.
- Preserve saved bridges whose required content is unavailable and defer inspection without an ineffective recovery-restart notice.

Public version: 26.11.12a; CLR assembly/file version: 26.11.12.1. The numeric mapping follows previous letter-suffix releases. Package size is reported without a hard ceiling.

Offline discovery, dependency and lifecycle checks do not certify every player configuration or in-game rendering path. Lifecycle checks, all twelve localization suites and prepared-resource discovery checks passed. Official postprocessing succeeded. The official SDK ZIP contains 965 payload files and measures 77,900,940 bytes; SHA-256 `67AF33EC54D98E280854D125E8F493C3B9856C97577343D132D5AC6988186A70`. Published as public Paradox Mods revision **19**, with `UserModVersion=26.11.12a` and `Updating=False` verified through the official SDK. The English, Simplified Chinese and Traditional Chinese changelog matched after server bullet/entity normalization. Existing description, seven screenshots, cover image bytes and suggested game version `1.6.*` were preserved.

Release commit `db18800331946d35abb61fa2936b0e3b9be85449` was verified on all 18 existing GitHub branches before upload. The official SDK downloaded revision 19; all 965 payload hashes matched the uploaded SDK ZIP. With the game absent, the downloaded files replaced both existing native cache locations (`160320_17` and `160320_18`), preserving their native metadata. These folder names are native cache history, not claims of a new native cache registration. Installed DLL version is `26.11.12.1`, SHA-256 `6099C9918737B12E988C433B61B26ACE4911339DABBE5CD7542B62049F275708`.

Original-only backup: `backups/official-26.11.12a-20261112-233452`. Verification evidence is retained outside publication inputs under `work/release-26.11.12a/verified`. No game was launched or closed and no user bridge assets were changed.
