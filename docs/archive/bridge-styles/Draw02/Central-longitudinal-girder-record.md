# Draw02 central longitudinal girder

The measured narrow central longitudinal girder beneath each Draw02 moving leaf retains
its original transverse coordinates and section width. Its two walls and connecting
faces form one centred longitudinal member; do not split them by the sign of X or
stretch its section by the deck-width increment. Use exact offline membership in the
near and middle meshes. The far native merged underside has no independent narrow
girder; retain that simplified representation without inventing an extra near-detail part.
This exception does not change hinge axles or other independently classified mechanism
parts. The separately measured central hinge plate below each axle is one of three
matching thin mechanism plates on that leaf. Preserve its original section and central
position; its transverse thickness is not a width-spanning assembly reach. The two
lateral plates retain their rigid signed translations, and the complete transverse
axle retains its own measured span mapping. Apply the exact near/middle memberships;
the native far mesh has no independent plates to add. This rule is unconditional,
not a narrow-road clamp or permission to hold other centre-crossing members fixed.
Preserve original Y/Z, UVs, packed frames and the leaf bone. The measured transverse supports directly beneath the moving deck belong to
the moving base and use `x -> x + sign(x) * d`, matching its longitudinal side blocks,
not own-span proportional widening. Apply their exact near/middle source membership;
the far merged base already uses signed displacement. Do not change the central girder
to make that mapping uniform across the whole mesh. Preserve original Y/Z, materials,
raw attributes and bone ownership.

The separate original horizontal closure panels adjoining that girder retain their shared inner-edge
coordinates at the girder. Their opposite edges follow the measured rigid longitudinal
ribs, allowing the existing transverse panel span to extend without widening those ribs.
Record the exact adjoining panel vertices offline, including the original leaf-seam tips;
do not leave a gap by translating their inner edge away from the fixed girder. Preserve
the original faces, Y/Z, UV projection mode and motion binding. This does not add a panel
or a narrow girder to the native far merged representation. These panels are distinct
from the transverse diagonal base supports: translating their inner boundary would
reopen the joint to the deliberately unmoved central girder. Retain their recorded
inner-anchor/outer-rib mapping while applying signed displacement to the base supports.
