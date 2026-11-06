# Native movable-bridge signal ownership and persistence

## Finding

Saving a selected traffic-light prefab as an ObjectSubObjectInfo child of the animated mechanism
is not equivalent to preserving a native node-owned traffic light. The current generic attachment
binding path does not establish this equivalence. Computing correct TrafficSignNeeds alone is
insufficient; ownership, instance masks and native update participation are separate requirements.

## Native evidence (game 1.6.2f1)

| Native source | Observed behavior | Consequence |
| --- | --- | --- |
| SecondaryObjectSystem.CreateSecondaryObject | Creates an entity with Owner(owner), then assigns TrafficLight.m_GroupMask0 from vehicle needs and m_GroupMask1 from the union of pedestrian masks | Masks belong to the instantiated secondary object and are computed from its node context |
| TrafficLightObject | Declares the TrafficLight instance component and initializes prefab TrafficLightData type/reach fields | This prefab component contains no explicit persisted instance group-mask configuration |
| ObjectSubObjectInfo | Serializes object reference, position, rotation, parent mesh, group index and probability | Its group index is not a traffic signal group mask; neither instance mask is present |
| TrafficLightSystem.UpdateTrafficLightObjects | Iterates the current node's direct SubObject buffer, updates entries with TrafficLight, and does not recurse into a mechanism's child buffer | A traffic light nested beneath a mechanism is not automatically updated by this traversal |
| TrafficLightInitializationSystem | Assigns lane signal groups and detects MoveableBridgeData through the native node/subnode relationship | Recognition of the movable bridge does not initialize arbitrary nested visual traffic lights |
| Game.Objects.TrafficLight | Serializes state and both group masks in a game save | Savegame instance serialization does not establish prefab-only cold-start construction |

The native simulation updates the bridge's PointOfInterest separately from its direct light-object
iteration. Do not interpret that bridge update as a recursive update of nested traffic-light children.
The inspected SubObjectSystem does not contain an equivalent traffic-mask assignment. This is a
finding about these observed paths, not proof that every native system has been exhaustively searched.

## Effect on current implementation

MovableRoadObjectSelection.TryResolve returns ObjectSubObjectInfo after selecting a concrete prefab.
MovableRoadObjects.Bind attaches that object to a mechanism connection. Those operations do not
persist the native traffic masks or preserve node ownership. The previous MovableSectionObjects
implementation removed all NetPieceObjects from hidden moving-node pieces, so retaining no network
producer and copying only a signal child would lose the native signal relationship.

No complete generator currently calls this path, so this finding has not identified a newly published
broken signal asset. It does identify an integration constraint that must be resolved before routing.
Do not add a mod-owned signal timer, patch the game simulation, assign every light an all-groups mask,
or serialize a temporary world's entity/group IDs as if they were stable prefab identities.

## Implemented producer split and remaining integration

The shared section conversion now retains original NetPieceObjectInfo entries whose object carries
TrafficLightObject. The native audit includes signal-capable placeholders with zero light fields:
their concrete selection still belongs to native node context. The other object rules are removed
from moving-node pieces for separate visual conversion. Mixed pieces receive a private component
array; all-signal pieces remain unchanged. Requirements, positions and ordering of retained entries
are preserved. The selected-road placement resolver skips these signal rules, and the generic
attachment selector raises an explicit error if called with one. No duplicate signal child is baked.

This narrow component-based split does not claim arbitrary placeholder compatibility, moving-signal
placement or final generator integration. TrafficSignObject alone is deliberately not a retention
criterion because native road arrows and lane/path marking placeholders carry it as well.

Separate objects that native nodes must own from visual attachments that can be bound to the mechanism.
For network-owned signals, preserve their native producer, conditions and node placement; avoid also
baking a duplicate mechanism child. Verify actual closed-span placement before claiming that any
retained object is outside the moving region. If a selected feature requires a moving signal object,
prove a native relationship supporting that behavior rather than silently leaving it stationary.

TrafficSignObject and LaneDirectionObject placeholders also depend on node context and must be traced
alongside TrafficLightObject before adopting a shared ownership classification. Prefab display names
are not classification evidence. Utility anchors and signal effects remain separate native paths.
The present report does not authorize a blanket keep-all-objects workaround.

## Reproduction and status

The source list and hashes are recorded in
`work/bridge-research/movable-signal-ownership-evidence.json` in the chat workspace. Read the listed
methods together with the mod's selected-object resolver, binder and hidden-section object stripping.
The initial review changed no runtime code. The subsequent producer split was built and deployed
with 18 original backup files and 12 installed payloads verified at
`artifacts/backups/movable-native-signal-producers-20261016-062949`.
No final routed generator calls the changed helpers, so no existing bridge output changed and no
bridges were removed. Engine code, shaders and native source assets remain unchanged. Final native
signal ownership integration and in-game/cold-start acceptance remain incomplete.
