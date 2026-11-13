# GO 224m Viaduct Bridge: deferred implementation

GO 224m Viaduct Bridge is not offered for new Bridge Builder previews or generation in the
current source tree. This prepares a future release without that style; no release was made
and the version was not changed. Existing generated bridges and the original source mod are
not removed or rewritten by this change.

## Preserved originals

The original files reside under `backups/go-viaduct-deferred/originals/`, preserving their
repository-relative paths. The backup contains originals only. The audit inventory and hashes
are outside it, in `artifacts/go-viaduct-deferred/originals.json`.

| Original path | Disposition |
| --- | --- |
| `src/BridgeBuilder/Bridges/Types/GOViaduct/` | Generator moved to backup; no production compilation. |
| `assets/HardcodedNumbers/GOViaduct/` | All 73 BBN resources moved to backup; no DLL embedding. |
| `tools/GeometryMetaprogram/GOViaduct.py` and `GOViaductCheck.py` | Offline extraction and validation tools moved to backup. |
| `docs/archive/bridge-styles/GOViaduct/Implementation.md` | Original measured implementation evidence moved to backup. |
| `BridgeGeneratorRouter.cs`, `BridgeStyleCatalog.cs`, `BridgeStyleDefinitions.cs` under `src/BridgeBuilder/Bridges/` | Pre-change originals copied to backup before removing active registration. |
| `assets/HardcodedNumbers/inventory.json` | Original copied before removing the GO generator entry. |

The moved originals total 77 files and 6,936,967 bytes; four shared-file originals total
248,178 bytes. There were no GO-specific prepared Surface or Texture files under `assets/Materials`.
GO's original material inputs were resolved from its source resource graph and converted at
generation time. Source extraction/native evidence in `artifacts/go-viaduct/` remains evidence,
not build content. Existing temporary authoring directories are not release inputs and were
not removed or repurposed here.

## Compatibility retained

The generator route, active style definition, implemented-style entry and catalogue binding
are removed. Twelve-language historical style names, the `GOViaduct` family/deck metadata and
the verified official source-mod title remain for existing bridge presentation. Metadata decoding,
owner identity, native persisted geometry and dependency loading do not require an active generator.
No new startup migration, retirement or removal rule is introduced. A healthy owner's existing
icon and metadata do not need catalogue lookup or rewriting.

Shared recorded-render construction, VT/material conversion, shader packaging, preview placement,
icon handling and unlock/persistence fixes remain. The recorded native-length inventory and virtual
prototype contract also remain applicable to future implementations. Historical incident records
using GO as a reproduction case document shared fixes and are retained.

## Future restoration

Restore the style-specific files from the matching relative backup paths, then deliberately reapply
its catalogue/router/definition registration and numerical-inventory entry against the current
shared files. Do not overwrite evolved shared files wholesale with old backups. Recheck the measured
source, current generation contracts, native materials/length handling, all LODs and localization
before offering the style again. Preserve existing owner identities and independent resources.

The project and production build enumerate source and resources below `src`, `vendor` and `assets`;
they do not compile or embed the backup tree. Automated checks verify that the rebuilt DLL contains
neither the GO generator type name nor its BBN resource names. The output directory is a local build,
not a published Paradox Mods package. Live loading of an existing saved GO bridge remains a separate
manual acceptance check.
