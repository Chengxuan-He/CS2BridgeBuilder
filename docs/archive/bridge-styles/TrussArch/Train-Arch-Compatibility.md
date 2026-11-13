# Train Arch Bridge compatibility

## Source and structural identity

The installed Train Arch Bridge package is Paradox Mods `155730`, package revision `1`.
Its `TrussArchBridge02.cok` contains one `TrackPrefab`, named `TrussArchBridge02`,
localization and package metadata. It contains no custom geometry or material payloads.
The extracted Odin JSON source is retained at
`artifacts/train-arch-source/TrussArchBridge02.Prefab` with SHA256
`59A2FCEF5A0634D4A6FC733541FDFA1C214A202EBEA8492759E241E37EB9BBE9`.
It can be reproduced by opening the COK as ZIP and extracting the same-named Prefab entry.

The current installed game's Unity resource map resolves the source's structural references:

| Role | Source UnityGUID | Native resource |
| --- | --- | --- |
| Overhead section | `6ad42519c2f37ae4da100e2479f3a6d5` | `2-Lane Truss Arch Bridge` |
| Pier | `ced73e2efb5276742bdc77ef702bbd4e` | `2LaneTrussArchBridgePillar Placeholder` |

These are the existing arch-below `TrussArch` family, not the white DLC
`TrussArch02` family. Archived Unity path IDs are not stable across game versions;
the compatibility conclusion uses the current resource map, not historical path IDs.

## Name collision and integration

The mod's root name collides with the later native `TrussArchBridge02` name.
Name-only matching selects the wrong family, and name-only recorded width lookup
can also borrow the white bridge's dimensions. Both consumers require the same
source disambiguation. The mod's track deck and source-specific placement settings
do not constitute a new structural bridge style.

The existing `TrussArchBridgeGenerator` uses the shared fixed lifecycle, which
explicitly measures selected `TrackPrefab` inputs and supports railway decks.
The measured tower catalogue already includes the exact native pillar above.
Keep one `TrussArch` style and retain the existing native-source preference; a
second generator, duplicate localized style or new mesh recipe is unnecessary.
Do not substitute the original road variant's deck width for the mod's track width.

## Evidence limits

The mod source has a 128 m segment, 20 m water elevation, no curves and no fixed
segments. Its native references establish the shared structure, not equality of
every network setting with another variant. Native preview, train placement,
near/far appearance and cold loading remain separate human acceptance checks.
This investigation did not launch or close the game or modify source assets.
