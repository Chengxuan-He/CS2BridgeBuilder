# Golden tower ornament XY mapping

## Requested exception

The 2026-09-22 user request explicitly replaces width-only deformation for the
complete fan ornament with uniform XY scaling about the top of its central spoke.
This exception does not change tower legs, the upper crossbeam, other bridge styles,
or the previously revised below-deck truss and railing rules. Z is preserved.

## Native evidence

Source: `SuspensionBridge03PillarTop_ed7f8a1cd838affcd9f77db0866d94fa.Geometry`
from the retained native prototype export. Full-detail mesh: 8168 vertices.
Welded components 101–109 contain the central spoke and eight fan ribs. The
curved lower chord is welded into component 0; its recorded vertex membership
includes both faces and depth surfaces. Membership is computed offline only.

The central spoke top is `(0, 111.63672)` in XY. Ornament left/right extents
are `-11.492676` and `11.492676`; full span is `22.985352` metres.

For the existing width increment `extra`, the mapping is:

    s = (22.985352 + extra) / 22.985352
    x' = s * x
    y' = 111.63672 + s * (y - 111.63672)
    z' = z

The original source positions are used directly, not the already widened mesh.
An unchanged width preserves native coordinates exactly. An invalid or collapsed
scale stops geometry publication. The previous central-spoke rectangularization
call is removed from this bridge path.

Full-detail membership is inherited by LOD1 and LOD2 through offline nearest-native
vertex correspondence: 1932/8168, 1736/6352 and 228/608 vertices respectively.
Runtime consumes exact mesh names and immutable vertex masks; it performs no
height-band, connected-component or nearest-vertex classification.

`asset-anatomy.txt` identifies `SuspensionBridge03NetPillar` child entries 16–33
as the eighteen ornament-mounted lamps, all parent mesh 0. Their positions follow
the same mapping; the shared lamp geometry, rotation, effect, parent and probability
remain unchanged. Entries 16 and 25 retain the previously requested exact x=0
centering. Other mounted objects retain their existing rigid displacement.

## Verification boundary

Compilation is a loadability check only. The prototype is real; a new generated
sample and human near/far/day/night visual acceptance remain outstanding. In
particular inspect the curved chord's attachment to the columns after XY scaling.
No game operation or bridge-construction API was invoked.
