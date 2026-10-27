# Embedded numerical arrays

## Storage

The bridge-generation contract section 2 defines the policy. Each measured source file owns one
resource under `assets/HardcodedNumbers/`. Both production build paths embed these resources as
`BridgeBuilder.Numbers.<source-key>.bin`. No loose data file is required at installation.

The owning generated holder initializes `HardcodedNumbers` once on first access. Its six readonly
array references contain Int32, UInt32, Int64, UInt64, Single and Double values. Large compound
tables are reconstructed with loops over those arrays. These arrays are immutable inputs by
convention; generation never writes them. The managed heap holds the arrays, not the thread stack.

Every binary uses little-endian encoding:

| Offset | Contents |
| --- | --- |
| 0 | UInt32 magic `0x314e4242` |
| 4 | Int32 format version, currently 1 |
| 8 | Six Int32 array lengths in the type order above |
| 32 | The six tightly packed numerical arrays, in the same order |
| End minus 32 | SHA-256 of all preceding bytes |

The loader verifies the header, checksum and exact total size before allocating typed arrays.
IEEE bits, signedness and ordering are preserved. Unary minus and other recipe operations remain
in the generated C# expressions. There is no runtime prototype lookup, geometry classification,
file-system data lookup or resource dependency in exported bridge assets.

## Regeneration

1. Run the existing measured-prototype extraction script for the affected file.
2. Build the repository once if this is a new checkout, so its compiler reference response file
   exists. The checked-in resource-backed version builds without any prototype.
3. Run `dotnet run --project tools/HardcodedDataPack -- REPOSITORY_ROOT`.
4. Run `dotnet run --project tools/HardcodedDataPack -- REPOSITORY_ROOT --check`.
5. Run `dotnet run --project tools/HardcodedNumbersCheck`, then affected geometry checks and the
   production build.

The packer reads C# through Roslyn, retaining the original literal type and the contextual
conversions required by byte/short/enum fields. Every type's numerical bit sequence is independently
compared with the original syntax traversal before writing. Already converted files are skipped.
Regenerated literal files are converted again, and their inventory entries are replaced.
Both build paths reject unconverted numerical source and missing, corrupt or orphaned resources.

`--refresh` is a migration-audit option: it replays originals in the local
`artifacts/backups/20261021-embedded-numeric-data/` backup. It is not the ordinary regeneration
command and must not be used to overwrite later implementation changes.

`inventory.json` records source paths, original source hashes, numerical counts and resource sizes.
Backups contain original files only.

## Scope and verification

Coverage includes every bridge-type recipe (movable and fixed), generated `.g.cs` bridge data,
and the shared tower, cable, width, style, specification and native-template data files.
Algorithmic constants outside those recipe files are unchanged. C# pattern labels, enum
declarations, attributes and optional parameter defaults retain their compile-time representation.
They are language/control-flow contracts, not large measured tables.

Source-linked offline check projects embed the same resources through `tools/Directory.Build.targets`.
`HardcodedNumbersCheck` tests all resources, exact decoding, integer limits, negative zero,
subnormals, and rejection of corrupt, missing, truncated or unsupported data.
The migration also runs the existing geometry checks; neither these nor a successful build
establishes native rendering acceptance.
