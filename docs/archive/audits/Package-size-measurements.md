# Package size measurements

## Latest retained package evidence

The later fixed-base identity-repair package is recorded as **52,486,998 bytes**, with SHA-256
`B9CADFFC393440FDF965FEAA95DE8BBC9F0FCC8DFF44319F0134EF3080438254`.
Its package and deployment evidence is retained under
`artifacts/draw01-fixedbase-fix/shared/outputs/`; see the [shared handoff](../../../tools/PreparedMaterials/SharedCurrent.md).
This is a prior measured build, not a fresh measurement of documentation changes or proof of
compliance with the new material scheme. Historical package and library counts below belong to
their named artifacts. The complete generated-owner material measurement remains a separate gate.

## 2. Recorded numerical-storage comparison

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

The package in this recorded comparison has SHA-256
`BAA686530B4818133B7B37E6687B15E05B7644A5BF354FB19F087684E4E1E349`.
Local evidence is in `artifacts/package-comparison/comparison.json`, `final-package.json`,
`compressed-package.json` and each candidate's `PostProcess.log`. The artifacts are not source
delivery files. A changed build or changed publication file list requires a new measurement.

## 4. Recorded partial source-level diagnostic package

The retained partial source-level rebuild was measured with the official ModPostProcessor and
publisher SDK at `artifacts/shared-material-pipeline/diagnostic-package/partial-content.zip`:
**22,174,932 bytes**, **631 files**, SHA-256
`452296B6E6A036FFEB9E2D3C119B0CC45DE9944AB3F9C9CF4ABC29632632D854`.
Every archived entry is compared byte-for-byte with its staged input. The SDK SHA-256 is
`EF06CD53D9051B389ED18660CC79DDAB7116BB22E007F633A4003F4772F6B0D0`, running on .NET 6.0.36.
`PostProcess.log`, `package.json` and `result.json` record processing and verification.
That stage used its recorded standard build, matching PDB, Harmony, UI, icons, license and
the rebuilt ten-style material catalogue. No installed mod or user bridge was replaced.

This is a **partial, non-deliverable diagnostic package**. Missing local-region repeat implementations prevent full generation.
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
See [current requirements and retained implementation evidence](../../design/materials/Prepared-Materials.md#current-requirements-and-evidence-scope).

The production code can bind real source-chart-contained mappings and converted native object
descriptors. Out-of-chart structural receivers need a valid original-material mapping under the current contract; the deleted
per-sampler material collection is not restored as a fallback. Road local repetition is also
unresolved under the current verified native shader and subdivision constraints. Final sizing
requires those paths to be implemented and verified, followed by another clean SDK package.
