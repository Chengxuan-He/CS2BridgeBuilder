# PedestrianDraw02 verified non-tiling mapping

## Native mapping evidence

`BuildUvSampling.py` verifies the original Surface archive bytes and CIDs against the retained source manifest for all nine mesh streams. The three descriptor families remain distinct. Fresh native template 0 (`Default`, shader 571, `BH/SG_DefaultShader`) selects UV0 with identity scale and zero offset; source descriptors contain no UV overrides. Current template verification, shader evidence hashes, native channel identities, source part/LOD and domain provenance are recorded in `artifacts/prepared-materials/PedestrianDraw02/non-tiling/classification.json`. Template 2 uses Glass550 procedural interior sampling and remains unresolved.

Every admitted authored triangle interpolates finite UV0 inside the native atlas. Identity native shader sampling introduces no procedural period. Source coordinates, face memberships and island bounds are retained in evidence. Mirrored, overlapping, constant and line UVs preserve their authored mappings. They do not establish tiling, and they need no polygon clipping to retain their original source UVs. Both U and V are admitted together; mixed or unresolved cases retain density behavior.

The earlier 2,177 unresolved domains failed polygon-boundary clipping predicates, not a test demonstrating repeated sampling. Reviewing the native mapping admits 1,956 of those domains. The other 221 intersect the existing genuine road-repeat rectangle and remain density mappings. The clipping limitations are retained as diagnostic evidence on each native island; they do not gate source UV retention. No accepted degenerate chart samples the interior of the known repeat rectangle.

The genuine fixed-road repeat rectangle is read from the existing RoadAppearance BBN and excluded on positive source triangle overlap. Selected-road appearance is not classified. Original source unions and the existing chart BBN are preserved. Pixel periodicity is not used as a proxy for source mapping.

## Output and reproduction

Run `BuildUvSampling.py` with a Python environment containing NumPy. It requires no Unity session and creates no pixels. Standard `GenerateUvCharts.py` invokes it after both source-union integration steps.

The lazy runtime adapter is `PedestrianDraw02UvSampling.For(render, mesh, domain)`. Unknown renders return false; invalid mesh/domain indices for known renders raise internal errors. The independently loaded BBN contains schema 1, nine streams, and original-domain flags. Schema-3 domain recipes are decoded without changing face order.

- 47,224 domains retain authored native UV0 under the prototype-only stretching exception.
- 745 domains overlap the genuine road-repeat region and retain density.
- 14 template-2 domains remain unresolved.
- The BBN contains 192,040 uncompressed bytes, with no texture additions.

Repeated generation produced byte-identical sampling data and left the final source-union chart resource unchanged. Geometry, bones, source material identities, and localization behavior are unchanged. All new runtime strings are internal invariant diagnostics. Native rendering, complete persisted-owner material bytes and final package measurement remain integration acceptance gates.
