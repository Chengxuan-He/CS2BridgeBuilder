# Paradox Mods Package Size

## 1. Reporting scope

Report the complete compressed Paradox Mods content ZIP as the mod delivery size. Source files
selected for Git and one generated bridge's complete physical assets are separate metrics under
[bridge generation contract section 24](agent-contract/BRIDGE_GENERATION_CONTRACT.md#24-movable-bridge-asset-size).
An unpacked build directory, installed directory, isolated texture subtotal or offline baking
candidate is not the download package. A locally produced SDK package is not a published version.

The complete deliverable must be at most **50,000,000 bytes (50 MB)**, including every included
style and all archive overhead. Apply the material generation contract's total-package gate
before final delivery or readiness for human acceptance. Required supplemental downloads count
toward the same total. This limit does not permit reducing texture resolution or pattern scale.

## 2. Controlled numerical-storage comparison

Both candidates contain the same 673 relative file paths. Their source hashes differ only for
`BridgeBuilder.dll` and its matching `BridgeBuilder.pdb`; materials, native payloads and CID files,
Harmony, UI, icons and licenses are identical. All 569 numerical payloads contain the same measured
bits and retain demand initialization. The numerical storage encoding and its loader are the
only runtime differences. Symbols are included in both packages.

| Numerical resources inside the DLL | Complete SDK content package, bytes | MiB |
| --- | ---: | ---: |
| Independently gzip-compressed | 37,722,743 | 35.98 |
| Uncompressed BBN1, direct loading | 39,080,756 | 37.27 |

The extra gzip layer saves 1,358,013 bytes (1.30 MiB), or 3.47% of the package with raw BBN1.
These package sizes are similar for this comparison, so the production source and DLL use raw
BBN1 and direct on-demand reads. Package compression already removes most of the numerical
redundancy. Removing the extra layer also removes runtime decompression and temporary file I/O.
The source checkout and unpacked DLL are larger; they must not be reported as download size.
This decision changes neither numeric precision nor generated bridge materials or geometry.

The final package SHA-256 is
`BAA686530B4818133B7B37E6687B15E05B7644A5BF354FB19F087684E4E1E349`.
Local evidence is in `artifacts/package-comparison/comparison.json`, `final-package.json`,
`compressed-package.json` and each candidate's `PostProcess.log`. The artifacts are not source
delivery files. A changed build or changed publication file list requires a new measurement.

## 3. Native packaging and reproduction

The installed official `ModPostProcessor` successfully processed both assemblies, using the
configured Unity Mods project and game managed references. The package tool invokes the installed
publisher SDK's actual `PDX.SDK.Internal.Util.IO.ZippingService.CreateZip`, the function called by
`ModsPublishingService.ZipContent`. It does not substitute another ZIP implementation or level.
It runs on the publisher's .NET 6.0.36 runtime. The measured SDK SHA-256 is
`EF06CD53D9051B389ED18660CC79DDAB7116BB22E007F633A4003F4772F6B0D0`.

Prepare a clean publication content folder with the release file list from `tools/StageRelease.ps1`:
the current DLL and matching PDB, Harmony and its license, UI modules, all listed SVG icons,
payload-bearing `Materials/` directories and `LICENSE.txt`. Exclude stale assemblies, source data,
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

## 4. Current source-level diagnostic package

The integrated source-level rebuild is measured with the official ModPostProcessor and
publisher SDK at `artifacts/shared-material-pipeline/diagnostic-package/partial-content.zip`:
**22,174,932 bytes**, **631 files**, SHA-256
`452296B6E6A036FFEB9E2D3C119B0CC45DE9944AB3F9C9CF4ABC29632632D854`.
Every archived entry is compared byte-for-byte with its staged input. The SDK SHA-256 is
`EF06CD53D9051B389ED18660CC79DDAB7116BB22E007F633A4003F4772F6B0D0`, running on .NET 6.0.36.
`PostProcess.log`, `package.json` and `result.json` record processing and verification.
The stage uses the current standard build, matching PDB, Harmony, UI, icons, license and
the rebuilt ten-style material catalogue. No installed mod or user bridge was replaced.

This is a **partial, non-deliverable diagnostic package**. The verified byte length falls
below 50,000,000, but missing local-region repeat implementations prevent full generation.
It does not pass the complete deliverable acceptance gate and must not be deployed or used
as evidence that all bridge materials are finished. The exact baseline rejected package
was 1,625,470,821 bytes; comparison with this partial archive measures changed payload cost,
not equivalent completed functionality or a final reduction in the user's download size.

The source-level material directory contains 618 files and 180,345 bytes: 299 native Surface
files, ten style-local copies of one constant-control Texture, and their CID sidecars.
There are 38 distinct local payload identities. Already compatible native materials and
textures remain built-in references, so their original game-file bytes are not charged as
new packaged textures. These figures are material subtotals, not a substitute for the full
SDK archive length above. No single generated bridge footprint has been measured in this run.

All 299 descriptors pass the installed game's binary readers, including 270 VT descriptors.
All ten style graphs pass source-linked independent-owner snapshot and deletion checks.
Standard compilation, hardcoded-data validation and localization checks pass. Native parsing
and persistence tests do not establish correct region-repeat sampling or rendered appearance.
See [the source-level implementation and remaining constraint](Prepared-Materials.md#remaining-local-repeat-constraint).

The production code can bind real source-chart-contained mappings and converted native object
descriptors. Out-of-chart structural receivers still need a valid replacement; the deleted
per-sampler material collection is not restored as a fallback. Road local repetition is also
unresolved under the current verified native shader and subdivision constraints. Final sizing
requires those paths to be implemented and verified, followed by another clean SDK package.
