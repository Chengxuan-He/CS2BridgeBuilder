# Bridge Builder — publishing information entry

## Current release inputs

User-authorized version: **26.11.20**. Use the current officially postprocessed candidate prepared by the release owner; retain its exact DLL/package hashes and SDK receipts in the release record. Do not reuse a historical candidate directory or treat local build output as the published download.

## Metadata and content

Required reading: the complete [metadata specification](../specs/metadata-spec.md).

- Metadata name and description heading: exactly `Bridge Builder`.
- Existing Paradox Mods listing: `160320`; update it rather than creating a new listing.
- Version: `26.11.20`; verify the final assembly and uploaded package match.
- Description: [maintained English, Simplified Chinese and Traditional Chinese text](mod-description.md), including current source-mod requirements and exact official titles in every mod link.
- Changelog: [26.11.20 upload text](Changelog-26.11.20.txt).
- Cover: `Thumbnail.png`; retain the established image unless the user requests replacement.
- Supported game version: `1.6.*`, derived from the current native game log version `1.6.4f1 (1097.158a) [6360.18886]`; retain that range in the draft and verify the final online field. Verify final visibility through official publishing configuration.
- Road Builder and other optional integrations are not global required dependencies. Each bridge style checks its own registered source content.
- Source: [GitHub](https://github.com/Chengxuan-He/CS2BridgeBuilder).
- Authenticate through official tools; never store credentials in repository content.

Follow the [release procedure](../guides/development-release-procedures.md#repository-release-workflow): verify source publication to all existing branches, actual Paradox Mods publication, then download and install the published version. Record completion only after verification.

Deleting a generated bridge also removes its placed instances. Advise players to back up their city; a missing-bridge repair must not overwrite their original save automatically. Historical candidate details remain in their release records and original-file backups, not current upload instructions.
