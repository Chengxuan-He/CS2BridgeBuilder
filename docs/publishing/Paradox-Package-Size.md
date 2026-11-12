# Paradox Mods Package Size

## 1. Reporting scope

Report the complete compressed Paradox Mods content ZIP as the mod delivery size. Source files
selected for Git and one generated bridge's complete physical assets are separate metrics under
[bridge generation contract section 24](../agent-contract/BRIDGE_GENERATION_CONTRACT.md#24-movable-bridge-asset-size).
An unpacked build directory, installed directory, isolated texture subtotal or offline baking
candidate is not the download package. A locally produced SDK package is not a published version.

Report all included styles, dependencies, archive overhead and required supplemental downloads.
There is no project-defined hard byte ceiling. Size reduction must preserve texture resolution,
pattern scale, complete dependencies and all other quality requirements.

## 2. Native packaging and reproduction

The installed official `ModPostProcessor` successfully processed both assemblies, using the
configured Unity Mods project and game managed references. The package tool invokes the installed
publisher SDK's actual `PDX.SDK.Internal.Util.IO.ZippingService.CreateZip`, the function called by
`ModsPublishingService.ZipContent`. It does not substitute another ZIP implementation or level.
It runs on the publisher's .NET 6.0.36 runtime. The measured SDK SHA-256 is
`EF06CD53D9051B389ED18660CC79DDAB7116BB22E007F633A4003F4772F6B0D0`.

Prepare a clean publication content folder with the release file list from `tools/StageRelease.ps1`:
the current DLL and matching PDB, Harmony and its license, UI modules, all listed SVG icons,
payload-bearing `~Materials/` directories and `LICENSE.txt`. Exclude stale assemblies, source data,
backups, logs and build intermediates. Postprocess the isolated assembly with the official tool
before packaging; pass each game managed DLL as a separate `-r` argument. Do not modify the
installed game DLL to obtain a measurement. Record postprocessing success before proceeding.

Build `tools/ParadoxPackage`, then invoke its DLL with the installed ModPublisher directory,
the postprocessed content directory and a new output ZIP path. Redirect its JSON output to a
report outside the content folder. The helper refuses an existing destination and verifies every
ZIP entry against its source bytes. It only constructs the local SDK service container and calls
the archive function; it never initializes the account, logs in, publishes or uploads. If the SDK
changes its internal API, fail and inspect it rather than silently falling back to another packer.

Use `tools/Measure-DeliverySize.py --package <zip> --output <report.json>` for the three delivery
scopes. Supply `--game-data` and `--owner` only when measuring a real persisted bridge; no bridge
sample means its footprint remains unmeasured. Listing covers and screenshots are uploaded
separately by ModPublisher and are not included in the installed content package.


Recorded inputs, measurements and their acceptance limits are preserved in [the historical evidence](../archive/audits/Package-size-measurements.md).
