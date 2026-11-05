# Lift04 internal longitudinal rib distribution

The original disconnected rib sections inherited RigidParts sign(x)*delta.
At actual half-width delta 10.25, this translates every internal member outward
by 10.25 m and leaves the centre empty. DeckBase is not the internal rib family.

Offline source-identity checked membership includes all 252 connected surface
components in each detailed LOD: 1840 vertices / 920 faces in LOD0 and 1629 /
731 in LOD1. The selection includes short and long sections, bottom flanges,
webs and end pieces over the full original Z extent. No face crosses membership.
LOD2 contains no matching detailed ribs; no substitute geometry is fabricated.

Four complete section envelopes are placed with equal clear gaps between the
unchanged outer support inner edges x=+-3.78802490234375, expanded by signed delta.
Each section moves rigidly; section width and original Y/Z remain unchanged.
The original centres are not equally spaced, so delta zero also redistributes
them, as authorized. At delta 10.25 each clear gap is 5.366497802734375 m.
Negative gaps on extreme narrow requests are not runtime rejection conditions.

The producer emits only measured IDs, offsets and delta coefficients in BBN1.
Runtime performs no geometric classification, changes no attributes, triangles,
material assignment or skeleton, and runs after original rigid-part transforms.
Unselected external structures and transverse beam mappings are untouched.

Measurement tests pass six signed deltas (-4,-1,0,2,10.25,24) and all three LODs.
Actual Plan baseline captured 18 cases directly from installed candidate71r9 DLL SHA256 FF8EA98201C44DAB67A87B7B1B232EF3D64D4D47ACEC33D76969204640640CF3. Candidate72r2 actual Plan passes all 18 cases: unselected positions match installed71r9 exactly, member Y/Z stay unchanged, and complete member triangle shape stays within 5e-6 m. Production UV frames pass 9906 member-face checks without phase change. Each detailed LOD retains all 184 original top support contacts for deltas -1,0,2,10.25,24 (boundary-face-box test at exact shared height, not general collision detection). Extreme delta -4 has overlaps and reduced contacts, reported without rejection. Native Unity6000.6.2f1 passes 30 TryBuild/upload/raw normal/tangent/bone/exact UV promotion/index cases and 30 actual finite/glass Retile cases. Evidence is longitudinal72/native/full-retile-result.txt, full-retile-slots.tsv and native.log. This header-prewarmed harness does not establish full GameDB, GPU pixels or persisted game visual acceptance. No user-visible strings or localization changed.

Candidate DLL SHA256: ED33FFBB44AB37DD60B38CC74D64E5B10ADCAB8AB35ED2B8C9922F935CC16C79. Measured resource: Types_Lift04_LongitudinalRibMeasurements.bin (41,716 bytes). Runtime receives the ordinary selected total-width delta; it introduces no left/right-road special branch. Independent asymmetric selected-road end-to-end generation was not exercised by this style harness.
