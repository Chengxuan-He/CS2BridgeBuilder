# Nanpu tower placement and collision investigation

## Observed generated owner

On 2026-11-17, player screenshots showed the cable fans without their pylon and a shipping
lane crossing the bridge without the intended structural obstruction. The inspected owner is
`b06e71822-ee5d-4c45-ab55-8f523c71594d`. Inspection was read-only; no saved player asset changed.

The saved core NetPieceObjects entry referenced NanpuPylon, with probability 100, phase
[0.5,0.5], zero spacing, Edge requirement and a Y-axis half-turn. NanpuPylon had only
UnderwaterObject.AllowDryland and one mesh reference. All three original Surface references
and all LOD links existed. Native decoding of the actual saved Geometry files produced:

| LOD | Vertices | Triangles |
| --- | ---: | ---: |
| 0 | 4548 | 1516 |
| 1 | 4536 | 1512 |
| 2 | 396 | 132 |

Each geometry bound is X=[-19.902588,19.902588], Y=[-48,107],
Z=[-6.812744,6.8599854]. This excludes empty tower geometry and absent LOD payloads;
it does not establish a live spawned entity, renderer readiness or visible material.

## Historical initial correction attempt

The installed native `8LaneCableStayedBridgePillar` object (resources path ID 120666)
has PillarObject (component 7293), type Standalone, anchor offset zero and vertical offset
range [-1,1]. The corresponding placeholder also declares PillarObject. In ObjectInitializeSystem,
PillarData gives non-horizontal pillars BaseCollision, OccupyZone, CanSubmerge and
OptionalAttach. A plain generated StaticObjectPrefab instead falls into the Overridable
and Brushable branch. Thus the prior generated tower lacked the native structural role.
UnderwaterObject alone changes water placement eligibility; it is not pillar collision.

The initial correction declared PillarType.None. This still enters the non-horizontal PillarData collision
branch, while avoiding SubObjectSystem's Standalone/Vertical AnchorTop and OnGround
adjustments. Those adjustments would shift the measured Y=-48..107 tower away from its
cable-foot datum. No collider dimensions or source geometry are fabricated.

StandingObject preserves the opening below the first crossbeam instead of treating the
whole H-shaped tower as a solid box. Across all LODs, the measured symmetric lower-leg
envelope has width 11.963134765625 m, height 39.48828125 m, depth 13.6727294921875 m,
and clear reference gap 14.81201171875 m. The gap changes by the requested road-width
delta while each leg's size stays fixed. This is a conservative native rectangular
envelope of the slightly asymmetric, sloping source legs and foundations, not an exact
triangle collider. The upper body begins at the measured first crossbeam.

StandingObject's height is measured from its object origin. ObjectMeshInfo therefore
translates the unchanged tower render by (0,48,-0.02362060546875); the root placement
uses (0,-48,-0.02362060546875) with the existing Y half-turn. These transforms cancel
exactly in world space. All three LODs at half-width deltas -6,0,8,20 m retain every
vertex bit-for-bit, and their lower vertices remain within the two leg envelopes.
The earlier regression was included in NanpuCheck.py. The reference empty passage remains
14.81201171875 m wide, rather than becoming collision across the whole pylon width.

The tower moves from secondary NetPieceObjects placement to root NetSubObjects with
EdgeMiddleFixedSegment and fixed index 1. NetInitializeSystem maps this to EdgePlacement,
MiddlePlacement and FixedPlacement, sets ParentIndex=1 and, with PillarData, MiddlePillars.
CourseSplitSystem assigns zero-based fixed indices to the sequence; index 1 is the positive
13.719970703125 m core. SubObjectSystem tests the owner's Fixed index before generating the
midpoint object, excluding both flank indices 0 and 2. The old secondary entry is removed,
so the two routes cannot duplicate the tower. The rotation remains the source-frame Y half-turn.

Zero spacing was not a proven cause: SecondaryObjectSystem starts with one edge object and
only derives repeated counts when spacing exceeds 0.1. The inspected persisted chain alone
cannot identify which live secondary-object initialization or selection condition suppressed
the old tower. The correction uses the native structural placement and collision path;
it is not evidence that the screenshot fault has already passed game acceptance.

## Subsequent overlap investigation and current correction

A later screenshot showed a 584 m placement with the right pylon and its owning cable edge red.
A red owner edge does not identify a cable collider: native validation propagates object errors
to their owner. The exact live ErrorData entity pair was unavailable, so the screenshot's collision
partner is not established by these files alone.

Two concrete representation defects were established from native consumers. SubObjectSystem
adds root local Y to owner elevation and creates object Elevation when OnGround is absent.
ObjectUtils.GetCollisionMask(geometry,elevation) then selects Underground when that result is
negative. The previous -48 m root / +48 m mesh compensation preserved every visible vertex but
could classify an elevated tower as underground. Visual transform equivalence did not preserve
native behavior. All current root object Y offsets are zero.

Object ValidationHelpers checks Standing legs, then treats the entire remaining upper bounds
as solid. The initial Standing representation consequently filled the H opening from the first
crossbeam upward. It is replaced with eight source-derived roles: two lower columns/foundations,
the lower crossbeam, two sloping middle columns, the upper crossbeam with lettering, and two
upper columns. Measured deck-relative division levels are -48, -8.51171875, -2, 55.966796875,
64.40234375 and 107 m. Offline necessary triangle subdivision retains full original face coverage.
The highest-detail column axes determine the object frames for every LOD; vertices and frame
attributes are transformed inversely so native object rotation restores the original structure.
No Standing aggregate, synthetic invisible solid, global collision bypass or missing foundation
is used. Each object's native box bounds its real measured member.

All objects retain PillarType.None, avoiding Standalone terrain/top relocation while preserving
structural identity. UnderwaterAllowDryland continues to permit water placement. The authorized
single-course recipe places all eight objects on the same EdgeMiddle owner, without extra network
nodes. At four width deltas and each LOD, offline fixtures retain real lower/upper beam and both
foundation collisions while leaving road-level, upper-H and below-span centre openings empty.
Native game collision and shipping-lane acceptance remain pending.

## Historical verification boundary

Original source measurements, geometry recipes, all three LODs, width frames, cable-core
boundaries and source material references are unchanged. Offline geometry checks continue
to apply. Shared numeric conversion and compilation are integrated separately. Native game
acceptance must verify a newly generated tower, one tower per core, near/far appearance,
shipping-lane overlap rejection where a foundation obstructs it, unobstructed crossings,
and persistence. Existing saved owners are preserved and are not silently regenerated.
