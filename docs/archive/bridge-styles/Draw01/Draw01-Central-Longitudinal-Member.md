# Draw01 central longitudinal member width

The user's 2026-10-17 native/generated comparison identifies an independently authored
central longitudinal member incorrectly widened into a plate. This correction keeps
that member at its native width and centred position, while transverse beams and X braces
continue to adapt to bridge width. It is a specific correction to the earlier universal
centre-crossing interpretation, not permission to freeze other spanning structures.

Native game 1.6.2f1 evidence: near-mesh authored groups begin at faces 3763 and 11657;
corresponding middle-LOD groups begin at 2126 and 6825. Both span X approximately
-0.40000153 to +0.40000153 m. Their 480 near and 472 middle vertices are not shared
with other authored members. The far native mesh has no independent representation
of this member and its existing merged representation remains unchanged.

The previous displacement formula used each member's own half-span, adding the whole
bridge width increment to this 0.8 m member. Remove only these measured vertex memberships
from the widening tables. Remove the associated width-collapse rejection, which no longer
applies to this fixed-width member. All other displacement coefficients remain unchanged.
Do not change Y/Z, normals, UVs, materials, bones or the restored road undersides.

Checks compare the original and updated actual membership tables: exactly 480/472/0
memberships change at LOD0/1/2, with no other coefficient change. Preview and persistence
share this assembly path. Build and deployment are separate from human visual acceptance;
verify the centre member in open/closed views and during LOD transitions on a newly generated bridge.
