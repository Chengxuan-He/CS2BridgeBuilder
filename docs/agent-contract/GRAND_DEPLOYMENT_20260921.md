# Authorized Grand Bridge reference repair

The user explicitly authorized the measured 7 m exception on 2026-09-21,
including deployment. This is a one-time exception to the 1 m audit limit.

The bridge/grand source change is integrated into dev without replacing the
other current runtime/UI changes. Native Grand pylon and pillar reference road
widths are 19 m (4 * 3 m carriageways + 2 * 3.5 m sidewalks), not 12 m.
BXP's separate reference is unchanged.

Retained current sample b9992141f-7caf-4a09-b588-069b1cbe7bde:
source cable x bounds [-9.55542, 9.55542], generated bounds
[-16.05542, 16.05542]; complete spans 19.11084 and 32.11084 m.
Road widths 19 and 25 m. Binary32 audit delta is -7 m (0xC0E00000).
Evidence is retained in C:/Users/admin/Downloads/BridgeBuilder-grand-current-20260921.

The corrected ordinary width delta for this 25 m road is 6 m instead of 13 m.
No runtime audit correction is added. Golden centre-support immutable maps
are integrated alongside this change; the accepted full sidewalk railing gap
remains unchanged. Compilation is not human in-game near/far acceptance.
