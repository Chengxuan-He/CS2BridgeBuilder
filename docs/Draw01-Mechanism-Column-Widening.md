# Draw01 mechanism column widening

## Native structure

This mapping is for `DrawBridge01Pillar`, the mechanism's stacked middle render part.
It is not `DrawBridge01NetPillar`, the independent ordinary-network support, or
`DrawBridge01PillarBase`, the lower footing with protective rails. Native source files
were decoded through the installed PipelinePlugin geometry codecs. All three levels
contain one mesh and no bone-index channel.

| LOD | Vertices | Triangles | Displacement coefficients |
| --- | ---: | ---: | ---: |
| 0 | 288 | 144 | 3 |
| 1 | 259 | 129 | 3 |
| 2 | 131 | 88 | 9 |

The high-detail shape contains the two end-wall assemblies. The front central panel's
half-span is 3.20001220703125 m; the rear central panel's half-span is 4 m. Their side
portions translate rigidly, and the crossing panels stretch against their own spans.
The original detailed vertices occupy centers/endpoints for this mapping, producing
-1, 0 and +1 coefficients without scaling side portions. This is not whole-column scaling.

## LOD correspondence

LOD1 retains the same displacement stations. LOD2 changes the geometry and includes
nonzero interior X coordinates; applying only sign(X) would incorrectly move these
interior panel points as side parts. Each was projected offline onto the corresponding
high-detail surface, and its interpolated displacement was inspected against that
panel's measured span. Runtime stores only reviewed coefficients and vertex ordinals.

Examples: far vertices 13/17/19/22 at X=-0.23682403564453125 belong to the front wall,
with coefficient -0.07400722801685333. Vertices 40/129 at X=0.80438232421875 belong to
the rear wall, with coefficient 0.2010955810546875. The negative-end counterparts are
recorded independently. The largest native LOD2-to-high-surface projection distance is
0.27947408216308495 m. This is native simplification evidence, not a runtime tolerance.
Only the displacement coefficient transfers: original Y/Z and X residuals are retained.

The offline record includes every source vertex, corresponding high face, barycentric
coordinates, projection distance and coefficient. Equal-distance source projections
were checked for displacement disagreement. Native three-dimensional geometry and the
original/+8 m full-width plan views were inspected across all three LODs. These are
actual native geometry inspections, not synthetic vertex tests or game acceptance.

## Construction boundary

`Draw01ColumnWideningData.CreatePlan` copies the original position array, applies the
recorded X displacements and retains every triangle. The common structural builder
preserves native non-position channels, materials and submesh order. Original geometry
hashes are embedded in the mapping; the future caller must resolve that source identity.
Source array counts alone are not identity proof.

This unskinned render requires a structural build with zero procedural bones. Do not add
a fake skeleton or bone channel to satisfy an exporter precondition. Its original stack,
LOD and prerequisite components are configured by `Draw01RenderData.Part.Column`.
The footing, ordinary support, final assembly, native traffic/shipping and cold-start
acceptance remain separate unfinished work. No final generator calls this plan yet.
