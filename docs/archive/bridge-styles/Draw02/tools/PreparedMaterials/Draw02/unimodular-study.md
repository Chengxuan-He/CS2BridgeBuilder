# Draw02 integer repeat-basis feasibility

## Conclusion

An integer unimodular basis change can reduce the conservative local pixel
allocation for actual Draw02 triangle samplers. It preserves the ideal periodic
sampling function when the complete old function is composed with the matrix.
It is not an output-equivalent replacement using the current native bilinear
sampler, axis-aligned mip generator and unchanged Float32 UV storage. The examined
cases offer a reason for further investigation, not authorization to replace
existing textures or reduce their resolution.

No textures were encoded, no GPU bake was run, and no production geometry,
materials or common code was changed. The result includes concrete counterexamples
to naive triangle remapping, unchanged mip filtering and unsafe Float32 evaluation.

## Actual inputs and reproducible evidence

The authoritative input is
`artifacts/uv-density/structure-tessellated-sources.json`, containing 953 Draw02
sampler units, of which 763 are triangular. The analysis selects the three largest
triangle allocations and the three highest-condition-number triangles. These are
current inputs, not unused historical seam candidates.

`AnalyzeUnimodular.py` reads the corresponding existing native Surface references
and Texture headers from
`artifacts/prepared-current-structures-canonical/Draw02`. All six measured pixel
dimensions agree with the current `MovableTextureFootprint` calculation. Source
channel dimensions come from the real game material dependency headers, recorded
in `artifacts/prepared-materials/Draw02/unimodular-native-inputs.json`.

The numerical results, full keys, source coordinates, matrices, native channel
identities and inventory SHA256 are in
`artifacts/prepared-materials/Draw02/unimodular-analysis.json`. The analysis uses
integer matrices with coefficients from -8 through 8 plus small changes around a
Gauss-reduced basis. It does not claim a global minimum over every integer matrix.

## Why these allocations are large

For source atlas coordinates A, B and C, let J have columns B-A and C-A. Scaling
each row by its source channel's pixel dimensions gives the source-pixel Jacobian
D. The current allocator uses both column lengths multiplied by
`sqrt(1 + abs(cos(angle)))`, then rounds each result upward to a BC7 block boundary.
This conservatively bounds all directional gradients, including oblique axes.

Nearly parallel columns increase that bound, while their parallelogram area stays
small. The largest Draw02 sample uses a 4096-square source and source points
A=(0.81396484375, 0.71923828125), B=(0.9560546875, 0.72119140625),
C=(0.9560546875, 0.8427734375). Its native repeat occupies 776x1028 pixels. Its
source-pixel parallelogram area is 289,836, versus 797,728 allocated pixels. A
reduced basis can nearly remove the oblique-axis allocation overhead.

The triangle repeat contains the original triangle plus its reflected partner;
half of its domain is not unused padding that can simply be discarded. Overall
Draw02 structure storage also contains hundreds of distinct triangle samplers and
multiple channels/full mip chains. Improving six units alone does not establish
a whole-mod or complete-generated-bridge size reduction.

| Current sampler key prefix | Native source size | Current unit | Candidate local-density unit | Pixel reduction | Integer matrix M, rows |
| --- | --- | --- | --- | --- | --- |
| a693a81f02de | 4096x4096 | 776x1028 | 504x588 | 62.85% | (-1,-1); (1,0) |
| b5b5e3db2489 | 4096x4096 | 776x1028 | 500x592 | 62.89% | (-1,-1); (1,0) |
| e7afb4ab5c22 | 4096x4096 | 776x1028 | 500x592 | 62.89% | (-1,-1); (1,0) |
| 2d65a01a8e7b | 512x512 | 12x308 | 12x208 | 32.47% | (-1,16); (0,-1) |
| 224e4dd60e16 | 4096x4096 | 24x688 | 20x524 | 36.53% | (-1,12); (0,-1) |
| 35fc2722b432 | 4096x4096 | 24x688 | 20x524 | 36.53% | (-1,12); (0,-1) |

The candidate sizes apply the unchanged local gradient bound to D*M and round
upward. They preserve that local bound; they do not prove sufficient representation
of relocated discontinuities or unchanged filtered pixels. The quoted reduction
is pixel allocation only, not measured compressed bytes.

## Ideal function and triangle folding

Define p=frac(q) and F(p)=p for p.x+p.y<=1, otherwise F(p)=1-p. The old color
function is `T(q)=S(A+J*F(frac(q)))`, including the original source sampler S.
The normal channel additionally contains the original fold-dependent 180-degree
tangent-space rotation.

For an integer M with determinant +1 or -1, its inverse is integer. The function
`Tnew(q')=T(M*q')` is periodic on the unit square, and evaluating it at
`q'=inverse(M)*q` gives exactly T(q) in real arithmetic. More generally a phase
phi requires `Tnew(q')=T(M*q'+phi)` and `q'=inverse(M)*(q-phi)` together.
The phase is part of the sampling recipe, not a disposable vertex offset.

This does not permit replacing J by J*M and continuing to fold q' on its own
diagonal. Folding and integer shearing do not commute. On the first actual sampler,
at q'=(0.2,0.3), the selected M gives the correct source UV
(0.913427734375,0.744921875), while that naive triangle construction gives
(0.771337890625,0.74296875), a 582.05-source-pixel displacement. This is a concrete
source-coordinate counterexample, independent of texture colors.

The exact transformed implementation must apply frac and reflection to M*q',
retain the old half-pixel sampling convention and fold parity, and carry every
original wrap/fold boundary into the new domain. Some formerly external tile seams
become internal oblique seams. This is a new periodic parameterization, not the
previously rejected aliasing of different triangle vertex permutations.

## Filtering, normals and finite precision

Native bilinear reconstruction and mip filtering are not invariant under shear.
Old axis-aligned filter footprints become oblique footprints in q'. Baking a
regular new texture and then applying the same native filter instead uses a new
axis-aligned footprint. Original grid knot lines and periodic seam lines can become
oblique inside the new texture; a smaller axis-aligned grid cannot generally
represent that same piecewise reconstruction exactly.

A simple periodic-channel counterexample is `T(q)=cos(2*pi*q.x)`. It is compatible
with the triangle reflection because cosine is unchanged by q.x -> 1-q.x. Under
M=((1,-1),(0,1)), its frequency becomes (1,-1). For a 4x4 unit, the current centered
2x2 mip average attenuates the old channel by cos(pi/4)=0.70710678 but the transformed
channel by cos(pi/4)^2=0.5. No encoding error is needed to produce the difference.
This analytical channel is not claimed to be a measured Draw02 pixel pattern; it
disproves the general assertion that unchanged mip generation preserves T.

Normal maps must sample the complete old normal-valued function, including its
old fold-dependent rotation. The current runtime transports explicitly stored mesh
normal/tangent frames through `MovableUvFrame.Rotate`; a coordinate change alone
does not authorize rebuilding that frame from the new UV gradient. Keeping the old
frame and transforming only the lookup is one coherent interpretation. Rebuilding
the frame requires a separate basis conversion of every normal sample, handedness
review, and new mip evidence. A shear is not an orthogonal normal rotation.

Float32 storage breaks exact cancellation even for small integer matrices. The CPU
study deliberately stress-tests synthetic coordinates rather than claiming they
are measured bridge vertices. For the skinny 512-square-source sampler, the
coefficient-16 matrix maps q=(1,0.20000000298) back to
(0.99999976158,0.20000000298). That tiny error crosses the old periodic seam,
changes fold parity, and moves the source lookup by 148.39 pixels. The coefficient-12
case has a similar 344.57-pixel seam displacement at q=(8,0.20000000298).
Away from seams, the coefficient-1 maximum-size cases showed only about 0.00112
source-pixel displacement in this finite stress sample; that is not a universal
bound. The production shader's exact operation order and FMA behavior remain to
be tested separately.

## Evidence required before implementation

1. Define whether the target is the ideal continuous source function or the
   already filtered native texture result. An exact unchanged native sampling
   result generally rules out a generic shear with the current texture filter.
2. Record M, inverse M and any phase from actual sampler data. Prove the complete
   source function and fold/normal parity match analytically; never substitute a
   re-anchored triangle or independently permuted source vertices.
3. Check actual receiver UV values, Float32 storage, interpolation, all LODs and
   supported width changes. Bound errors at wrap and fold seams separately from
   ordinary local-gradient errors; do not hide seam crossings with a tolerance.
4. Compare original and transformed channel samples on both sides of every
   internal seam, at texel centers and fractional phases, including mirrored and
   negative-determinant cases. A local singular-value density bound is insufficient.
5. Establish an equivalent filter strategy and validate all native mip levels,
   anisotropic footprints, BC7 reconstruction, normal normalization and cutout
   coverage. Reusing the current axis-aligned mip kernel alone does not meet this.
6. Measure full retained material graphs and exported bridge dependencies only
   after those requirements pass. Keep current verified resources unchanged until
   new evidence supports replacement.

Signed axis permutations can preserve the rectangular sampling lattice with
appropriate texel-center phase, normal semantics and filtering checks, but merely
swap dimensions and do not provide the shear reductions listed above. The current
evidence therefore supports mathematical exploration of reduced bases, while
rejecting a direct production substitution as output-equivalent.
