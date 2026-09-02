# How Cities: Skylines II models a bridge

Everything here was read out of `Game.dll` by reflection and out of installed `.Prefab` assets, not
from documentation. It is written down because the design of this mod depends on it and because the
first assumption a reader is likely to make about it is wrong.

## Two things that are easy to conflate

`BridgeBuildStyle` is **not** what a player means by "bridge style":

```
enum BridgeBuildStyle { Elevated, Raised, Quay }
```

That enum only says how a bridge meets the ground — a deck on pillars, an embankment, a shore-hugging
quay. It has three values and always will.

Suspension, extradosed and truss arch bridges are a different thing entirely: **ordinary net prefabs**
that the game does ship, in the Bridges & Ports DLC. Read out of that DLC's own locale file:

| Prefab | Name in game |
|---|---|
| `SuspensionBridge01` … `04` | Two-Lane Suspension Bridge; Three-Lane Highway **Double-Decked** Suspension Bridge; … |
| `ExtradosedBridge02` … `04` | Four-Lane **Double-Decked** Extradosed Bridge; … |
| `TrussArchBridge02`, `03` | Four-Lane Truss Arch Bridge; … |
| `DrawBridge01` … `03`, `LiftBridge01` … `05` | moveable bridges, carrying `MoveableBridge` |
| `PedestrianDrawBridge01`, `02`, `PedestrianBridgeCoveredWood` | pedestrian bridges |

Their parts follow the game's net conventions — `SuspensionBridge01NetPylon`,
`…NetPillarTop`, `…EndNet`, `…MiddleNet`, `DrawBridge01Bottom300cm`, `LiftBridge02Side30cm` — which
is what confirms these are networks you draw, not objects you place. Asset packs such as the Bridge
Expansion Pack add more of the same kind.

Two consequences the design leans on:

- The style list is **discovered at runtime**, never hard coded, so it covers the DLC, any asset pack,
  and anything installed later, and it never offers a style that is not present.
- **Double-decked bridges exist natively** (`SuspensionBridge02`, `ExtradosedBridge02`), which is what
  establishes that the `AuxiliaryNets` route below is the game's own way of doing it rather than a
  trick.

## What a bridge prefab is made of

A bridge in an asset pack is an ordinary `RoadPrefab` (so, a `NetGeometryPrefab`) that additionally
carries:

| Component | Field | What it contributes |
|---|---|---|
| `Bridge` | `m_SegmentLength`, `m_Hanging`, `m_ElevationOnWater`, `m_CanCurve`, `m_AllowMinimalLength`, `m_WaterFlow`, `m_BuildStyle`, `m_FixedSegments` | Span behaviour: how long a span is, how far it sags, whether it may curve, how fixed-length main spans are laid out |
| `OverheadNetSections` | `m_Sections : NetSectionInfo[]` | The sections drawn **above** the deck — towers, cables, trusses |
| `NetSubObjects` | `m_SubObjects : NetSubObjectInfo[]` | Props anchored along the deck — pylons, portals — each with a position, a spacing and a `m_RequireElevated` gate |
| its own `m_Sections` | entries whose `m_RequireAll`/`m_RequireAny` contain `Elevated` or `Raised` | The deck edges: railings, the underside a bridge shows and a ground road does not |
| `AuxiliaryNets` | `m_AuxiliaryNets : AuxiliaryNetInfo[]`, `m_LinkEndOffsets` | Other nets created alongside this one at a fixed offset |

`NetSectionInfo` and `NetSubObjectInfo` both carry a `float3` offset/position, which is the hook this
mod uses to fit a donor's structure to a different road width.

So, operationally: **a bridge style is the `Bridge` component plus `OverheadNetSections` plus
`NetSubObjects` plus the elevated-only entries of `m_Sections`.** Copy those four onto a road prefab
and the road becomes that style of bridge.

## Double deck

`AuxiliaryNets` is the mechanism, and it is a stock game component, not a pack invention:

```
class AuxiliaryNetInfo {
    NetPrefab     m_Prefab;      // the net hung alongside
    float3        m_Position;    // where, relative to the carrier
    NetInvertMode m_InvertWhen;  // Never | LefthandTraffic | RighthandTraffic | Always
}
```

`m_InvertWhen = Always` is exactly "the lower deck runs the other way". `m_Position = (0, -spacing, 0)`
puts it below. `m_LinkEndOffsets = true` keeps the two decks' ends together at the abutments.

The Bridge Expansion Pack's own double-deck bridges are built this way: a placeable upper prefab
(`Bridge`, `PlaceableNet`, `UIObject`) that references a second, deliberately non-placeable prefab
(no `PlaceableNet`, no `UIObject`) for the lower deck.

The limitation that makes this feature experimental here is inherent to the mechanism: an auxiliary
net is created and destroyed with its carrier and cannot be selected or edited on its own. A lower
deck can therefore be placed, but connecting it to a separately built rail or subway line is not
something this mod can guarantee.

## Naming, and why grouping matters

The two sources that ship bridges do not agree on how to name variants:

- the game numbers them — `SuspensionBridge01`, `SuspensionBridge02`, `SuspensionBridge03`;
- packs spell them out — `BXP Suspension Bridge - Highway Twoway - 6 Lanes`.

Taken literally, either convention produces one "style" per variant, each with a single width, which
also disables the width fitting below — it can only choose when the variants sit in one family. So
discovery strips a one- or two-digit variant suffix and everything from the first ` - ` onwards, and
asks the game's own dictionary for the family's display name (`Assets.NAME[SuspensionBridge01]` →
"Two-Lane Suspension Bridge") rather than showing the raw identifier.

## Width adaptation, and its honest limit

Bridge sources author one prefab per lane count. A Road Builder road can be any width, so this mod:

1. measures the road by summing its sections' widths (`NetPiecePrefab.m_Width`, taking the widest
   piece per section because pieces at the same position are layers, not neighbours);
2. picks the donor variant authored closest to that width;
3. scales the **lateral** offsets only — `m_Offset.x` on sections, `m_Position.x` on sub-objects —
   leaving height and longitudinal position alone, since those describe the structure itself.

Meshes are not stretched, because they cannot be. Beyond roughly a factor of two the result stops
resembling the style, so the scale is clamped to 0.5×–2× and the report says so when it clamps.

## Dependency, deliberately

Donor geometry is **referenced, not copied**. An exported bridge keeps needing whatever its style came
from — the Bridges & Ports DLC for the built-in styles, or the asset pack for the rest. Copying the
meshes would make the export self-contained but would also mean redistributing content this mod has
no right to redistribute. The options page names the source for the selected style so the trade is
visible before the export, not after.
