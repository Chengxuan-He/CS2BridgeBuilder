# Source mod title verification and presentation

## 1. Official title evidence

The titles below were verified on 2026-11-13 through Paradox Mods web search and direct HTTP
retrieval of the official listing. The listing's `title` and `og:title` agree; only the website
suffix ` - Paradox Mods` is removed. Exact retrieval times and search outcomes are retained in
[official-titles.json](../../../artifacts/source-mod-names/official-titles.json).

| Mod ID | Complete published title | Official listing |
| --- | --- | --- |
| 166012 | GO 224m Viaduct Bridge | [Paradox Mods](https://mods.paradoxplaza.com/mods/166012/Windows) |
| 92245 | Bridge Expansion Pack | [Paradox Mods](https://mods.paradoxplaza.com/mods/92245/Windows) |
| 124160 | Bridge Expansion Pack: B&P | [Paradox Mods](https://mods.paradoxplaza.com/mods/124160/Windows) |

GO's ID/title search did not return an indexed matching listing. Direct official-page HTTP metadata
established its full title; local cache text was corroboration, not the external verification.
The two Bridge Expansion Pack titles were also found in an official Paradox Mods playset before
their individual pages were retrieved. `GO` and `B&P` are publisher-authored parts of these complete
titles and must remain unchanged; expanding either would invent a different title.

## 2. Source-label audit and implementation

The displayed numeric GO source came from `BridgePrototypeSource.ModDisplayName`, whose default
returned the platform ID. Other source branches also used `ModRequirement.m_ModId`, a non-built-in
pack's internal name, or a package directory. These paths now share the full-title resolution:
matching active Paradox Mods SDK metadata, matching owner `SourceMeta.displayName`, verified known
source titles, then a localized name-unavailable label. The native `ParadoxModsDataSource.GetMeta`
implementation assigns `Mod.displayName` to `SourceMeta.displayName`; it is not a prefab display name.

The audit covered subscribed donors, explicit ModRequirement content, external asset packs,
non-subscribed fallback sources, catalogue bindings, settings/status text, generation reports,
management and saved asset metadata. Technical IDs/source keys and prerequisite checks remain
unchanged. Settings source text now resolves the current localized label instead of freezing an
initial unlocalized label. Every catalogue card exposes the full subtitle in its existing button
and subtitle tooltips, including long titles. The compact subtitle retains its existing layout.

Saved bridge presentation stores the user's bridge display name and stable style/network identities;
it does not persist a source-mod label to migrate. Management cards show the bridge type rather than
a publisher field. No healthy saved assets are rewritten, and custom bridge names remain untouched.
Unknown titles do not block generation or source availability. No runtime HTTP query is introduced.
The surrounding source prefix and new name-unavailable text are covered in all twelve languages;
publisher titles are not translated or abbreviated by Bridge Builder.

## 3. Verification and delivery

- 43 source-policy checks compile the actual `BridgePrototypeSource`: official GO fallback,
  generic native owner titles, changed cached titles, prerequisite/pack/folder branches, numeric
  metadata rejection, unchanged technical source keys and name failure independent of availability.
- JavaScript and actual C# localization checks pass for all twelve supported languages.
- Card rendering checks expose complete long source titles through both tooltips, without image-load
  callbacks; existing image sizing and fallback checks still pass. JavaScript syntax passes.
- Production `Build.ps1` passes with the four existing unrelated nullable warnings. SourceMeta is
  a native value type; the source-policy test double now preserves that representation.
- Whitespace checks pass. Native tooltip appearance remains a human in-game check.

The [build log](../../../artifacts/source-mod-names/build.log) and
[deployment receipt](../../../artifacts/source-mod-names/deployment.json) retain delivery evidence.
No release or version bump is performed. Game startup/shutdown remains manual.
