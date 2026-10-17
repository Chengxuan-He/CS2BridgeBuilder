# Moving attachment lighting and elevation audit

## Confirmed lighting mismatch

Binding a selected-road streetlight as an ObjectSubObjectInfo child of the mechanism preserves its
closed transform and native effect references, but does not preserve native road lighting control.
This finding applies to the current generic child-binding representation, not to a published bridge.

The inspected native StreetLightSystem road overload visits only the road's direct SubObject entries.
Unlike its building overload, it does not recurse into each object's child buffer. Its road loop also
skips roads without RoadFlags.IsLit. SecondaryObjectSystem sets or clears IsLit and AlwaysLit from
its own collected streetlight candidates. Returning these booleans from an offline generator does
not initialize or maintain the flags on future placed roads after a cold start.

SubObjectSystem initializes child streetlight state from a building or watercraft top owner, but the
inspected child-creation paths have no equivalent road-state initialization. EffectControlData reads
LightsOff from the effect's real owner. Ordinary objects are their own real owners; its owner lookup
does not climb an arbitrary mechanism hierarchy to obtain road or parent streetlight state.

Consequently, retaining a native StreetLightObject component on a nested lamp does not by itself make
that lamp participate in the road's day/night, electricity and tunnel lighting behavior. Adding only
StreetLightObject to the mechanism also does not solve the cleared RoadFlags.IsLit producer or move
the nested effect's state lookup to that mechanism.

## Actual native effect records

The resource scan identifies StaticObjectPrefabs by their StreetLightObject component, not by names.
It found 81 such prefabs and 110 directly declared effect entries. Every inspected effect entry has
required flags 0, intensity flags 0 and forbidden flags 32768. The installed EffectConditionFlags
defines 32768 (0x8000) as LightsOff. These effects rely on the streetlight state; they do not encode
an independent Night requirement that would compensate for losing road-controlled state.

No selected static prefab or referenced effect failed decoding. Header decoding coverage is recorded
separately in the evidence file. These counts cover directly declared effects in the scanned native
resources, not arbitrary external mods or effects reachable only through nested children.

## Elevation is separate from position

SecondaryObjectSystem constructs Game.Objects.Elevation from the network owner's elevation and the
candidate's local elevation. It applies the native near-ground normalization, OnGround/Stacked flags,
and Lowered for eligible negative elevations. SubObjectSystem's ordinary child path computes its
own elevation and has no equivalent Lowered branch in the inspected creation method.

ObjectSubObjectInfo stores object, transform, parent mesh, group and probability. It cannot directly
serialize this instance elevation state. A correct closed-position transform therefore does not
establish equivalent instance behavior. ObjectUtils.GetCollisionMask uses Lowered to add overground
collision to negative-elevation objects; this flag is not merely cosmetic metadata.

## Implementation consequence

Subsequent implementation adds a [native lighting representation](Movable-Native-Lighting.md) using
private network lighting placeholders and root-owned bone-bound effects. The mismatch below describes
the previous unmodified child-effect representation and remains relevant to acceptance and unsupported
attachment schemas. Initial state and full in-game behavior are not yet verified.

The current node resolver and bone binder are not yet an equivalent persistent representation of
all selected-road attachments. Keep this constraint visible when wiring final generators. Do not
replace native LightsOff with unconditional or Night-only effects, retain duplicate visible stationary
lamps, add a mod-owned lighting controller, or declare a correct nighttime screenshot sufficient for
power-loss and cold-start acceptance.

A native representation must preserve both the road's lighting producer/state and the animated
effect's actual state owner. Native EffectSource exposes parent-mesh and animation fields, so a
root-owned animated effect representation is a candidate to investigate. It is not yet adopted:
source effect behavior, per-light grouping, root bridge effects, original fees, node producer behavior,
attachment transforms and cold-start initialization must be reconciled first. No new resource or
component conversion is justified merely by this candidate.

## Reproduction and status

Run audit_streetlight_effects.py from the chat workspace. Inspect the resulting
movable-streetlight-effect-evidence.json and the methods named above in the local 1.6.2f1 decompilation.
This audit changed no runtime code, game assets, shaders or material resources. No deployment or
bridge removal was needed. All ten final generators and in-game acceptance remain incomplete.

## Evidence hashes

- `movable-streetlight-effect-evidence.json`: SHA-256 `502c4ea1f8810cbd11e066aee95f1b8bf9d53d28d2ed8976c1a01023423d3d61`.
- `Game/Game.Simulation/StreetLightSystem.cs`: SHA-256 `915b34cfb796e57b82930173a18465ec7619791167bf006806752419f43a5262`.
- `Game/Game.Objects/SecondaryObjectSystem.cs`: SHA-256 `83b1cce860cc67d584f0c264dd951d477100de12fe465dea53ac3df18eef7990`.
- `Game/Game.Objects/SubObjectSystem.cs`: SHA-256 `891bea3bc68d1070d1ea725593047d725bb827577452f45b9282c2930eafcfcd`.
- `Game/Game.Effects/EffectControlData.cs`: SHA-256 `bc952f35adfb7982d307251bdc4128ba28d405415239667f58b7b7e6d902eaeb`.
- `Game/Game.Prefabs/EffectConditionFlags.cs`: SHA-256 `9b6b5cdb5863c8e3811224e7e52539028c4418a4872828bd767a26d8ab96bc92`.
- `Game/Game.Objects/ObjectUtils.cs`: SHA-256 `83cbfb3cf8e0b22ad3904ebd46a51f249363360bfabaa53f8b1b1f47f6270d73`.
