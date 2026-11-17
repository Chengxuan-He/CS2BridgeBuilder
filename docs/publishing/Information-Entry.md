# Bridge Builder — publishing information entry

## Current release inputs

User-authorized version: **26.11.18**. Use the current officially postprocessed candidate prepared by the release owner; retain its exact DLL/package hashes and SDK receipts in the release record. Do not reuse a historical candidate directory or treat local build output as the published download.

## Metadata and content

- Metadata name and description heading: exactly `Bridge Builder`.
- Existing Paradox Mods listing: `160320`; update it rather than creating a new listing.
- Version: `26.11.18`; verify the final assembly and uploaded package match.
- Description: [maintained English text](mod-description.md), including current source-mod requirements.
- Changelog: [26.11.18 upload text](Changelog-26.11.18.txt).
- Cover: `Thumbnail.png`; retain the established image unless the user requests replacement.
- Compatible game version and final visibility: verify against the official publishing configuration; the XML remains a draft.
- Road Builder and other optional integrations are not global required dependencies. Each bridge style checks its own registered source content.
- Source: [GitHub](https://github.com/Chengxuan-He/CS2BridgeBuilder).
- Authenticate through official tools; never store credentials in repository content.

Follow the [release procedure](../guides/development-release-procedures.md#repository-release-workflow): verify source publication to all existing branches, actual Paradox Mods publication, then download and install the published version. Record completion only after verification.

Deleting a generated bridge also removes its placed instances. Advise players to back up their city; a missing-bridge repair must not overwrite their original save automatically. Historical candidate details remain in their release records and original-file backups, not current upload instructions.
