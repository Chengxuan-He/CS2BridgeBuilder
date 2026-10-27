# Hardcoded Numerical Resources

## 1. Source layout and encoding

The authoritative requirement is [bridge generation contract section 2](agent-contract/BRIDGE_GENERATION_CONTRACT.md#numeric-storage-in-embedded-typed-arrays).
Store one uncompressed BBN1 payload per resource under
`assets/HardcodedNumbers/<BridgeType>/<resource>.bin`. `Shared/` contains common catalogue
and sizing parameters; measured style-specific data and UV domains belong to that style's
directory. Do not retain duplicate gzip resources. The complete Paradox Mods content package
provides transport compression.

BBN1 contains its version, six typed-array counts, ordered numerical bytes and SHA-256 checksum.
Preserve integer width, floating-point bits, array order and geometric rules. `inventory.json`
records `payloadBytes`; it is authoring metadata, not a runtime dependency. Both build routes
embed these files as `BridgeBuilder.Numbers.<resource>.bin` with culture inference disabled.

## 2. On-demand lifetime

Each numeric holder has an explicit static constructor, preventing `BeforeFieldInit` execution
before that holder is needed. The structural UV catalogue resolves only the requested key and
retains its decoded domain arrays. Catalogue enumeration does not load every bridge.

`HardcodedNumbers.Load` opens one embedded stream, validates its complete checksum and BBN1
layout, and reads the six typed arrays. The owning class retains those arrays for subsequent
use. No decompression, extraction file, loose installed data, persistent extraction cache or
generated-bridge dependency is needed. Any development decompression must use temporary files
and delete them on success or failure. Never read another bridge's files to obtain these values.

## 3. Regeneration and verification

`tools/HardcodedDataPack` writes BBN1 resources into bridge-type folders and emits explicit static
constructors. Its `--check` mode verifies checksums, counts, resource coverage, classification
paths and demand initialization. The UV recipe generator and Draw03 texture check use the same format.

`tools/HardcodedNumbersCheck` verifies every embedded resource's typed bits, optional original
baseline bytes, malformed-payload rejection, concurrent reading and production DLL initialization
flags. `tools/GpuBake/HardcodedNumbersNativeCheck.Editor.cs` executes the production loader under
Unity Mono and compares array bytes against the original source. The numerical resource set
contains 569 payloads and 14,571,352 values, totaling 58,321,824 bytes before package compression.
These checks do not establish visual acceptance of material or geometry changes.

## 4. Delivery measurement

Use `tools/Measure-DeliverySize.py --package <content.zip>` to report source delivery, the complete
Paradox Mods content package and, when supplied, one generated bridge's owned physical files.
Use official postprocessing and `tools/ParadoxPackage` for local SDK packaging; retain the log,
file manifest and toolchain identity. This local operation does not publish or upload anything.
An unpacked DLL/build-directory total is not the mod's package size.

The paired official-SDK comparison is documented in
[Paradox package size](Paradox-Package-Size.md). Evaluate additional numerical compression using
the entire package with identical other content. Similar package sizes do not justify adding
runtime decompression and temporary extraction to numerical loading.
