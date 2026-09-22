# Golden truss YZ return alignment

## Real evidence

Generated sample `b8a64b1df-9a79-478e-9a39-f321f931c2b1`, 33 m deck,
was retained by ownership-scoped cleanup in
`C:/Users/admin/Downloads/BridgeBuilder-yoz-clean-20260922-223418`.
Its `Piece 1.Geometry` is the 198956-vertex Middle support mesh.

Native Middle vertices 5676–5679 all have x=8.963135. The generated lower
vertices have x=12.702166 while the upper vertices have x=12.963135.
Y and Z match the original. Thus the defect is a sheared originally vertical
return surface, not a missing Z displacement or an ornament issue.

The previous barycentric donor mapping gave the lower endpoints coefficient
0.9347576 and the upper endpoints 1. With half-width increment 4, that creates
approximately 0.261 m of horizontal misalignment.

## Change

Offline membership now carries the entire outer return with its authored side
upright: coefficient +1 on the right, -1 on the left. Centre-side endpoints
retain the previously requested identity mapping. Runtime mapping is unchanged;
it consumes regenerated immutable data. Both support variants and both LODs
inherit the full-detail decision. Y/Z, topology and other mesh channels remain
unchanged. The pending tower ornament work is separate and was preserved.

## Real-prototype mapping inspection

Before: End had 84 and Middle had 164 approximately constant-X vertical YZ
triangles with inconsistent displacement coefficients. After regeneration:

| Mesh | Inspected vertical YZ triangles | Inconsistent displacement |
| --- | ---: | ---: |
| End | 6596 | 0 |
| End LOD1 | 1860 | 0 |
| End LOD2 | 780 | 0 |
| Middle | 12808 | 0 |
| Middle LOD1 | 3429 | 0 |
| Middle LOD2 | 1508 | 0 |

Inspection uses the real native mesh indices and positions (below-deck triangles,
X spread <=0.001 m, Y spread >=0.05 m). These are offline diagnostic criteria,
not runtime geometry-selection rules and not synthetic unit tests. Native small
coordinate differences are retained, not snapped away.

Compilation does not establish visual acceptance. Installation and human near/far
inspection of a newly generated bridge remain outstanding.
