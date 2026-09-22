# Elevated footway and golden truss investigation

Status: incomplete. Shared source-component measurement has been changed and compiled;
the golden support geometry and Grand railing placement have NOT been repaired or accepted.

## Retained evidence

The game was stopped and generated assets were backed up before cleanup to
`C:/Users/admin/Downloads/BridgeBuilder-railing-audit-20260921`.
No save was edited. The backup includes the exported prefabs, geometry and registry.

The current sample road is `r14c34175-192b-431d-9627-aba567946d30-76561199197854251`
(Custom Eight-Lane Divided Road). The runtime log records 36 m elevated surface width:
two selected 3.5 m sidewalk pieces, eight 3 m drive pieces, and a 5 m median.
Its ground default is 40 m. Two 0.5 m side extensions are measured separately.
Both Grand factory calls received targetDeck=36, sourceRoad=12. This rules out passing
the 40 m ground width in that sample; it does not validate the Grand prototype reference
or the final structure width.

## Shared input changes on dev

- Removed the bridge-family gate around the outermost-sidewalk scan.
- Preserve individual selected elevated component slots in SourceSections, rather than
  assigning an entire parent section's summed width to its name.
- Overlapping render layers remain one slot. The widest selected layer supplies its width;
  at equal width a sidewalk identity takes precedence over its bottom layer.
- Retain per-side footway boundary diagnostics at the composer/factory boundary.

These changes do not add Grand to a runtime geometric railing heuristic. Grand's railing
consumer still requires an archetype-specific, immutable mapping. Final in-game verification
is outstanding.

## Golden support evidence

Generated sample: `ba17e0bce-eb88-4dfa-8f6c-506d0507671f`.
The native BridgesAndPorts Blob.cok contains the actual source geometry; the matching files
were extracted without changing game assets into the backup's `prototype-golden` directory.

The full-detail `SuspensionBridge03SupportEnd` source and generated section Piece both have
82394 vertices. Comparison of the below-deck transverse faces shows unequal displacements
across heights and distorted triangles. The source/derived projection is retained as
`golden-base-comparison.png`; raw positions and indices are retained as JSON. The endpoint
pylon top was also compared and is not the support mesh in the marked area.

The existing support path uses WidenParts, a height-profile transform. Replacing this must
identify complete authored transverse members in the full-detail support and carry that
classification to SupportEnd and SupportMiddle LOD1/LOD2. Do not repair it by changing the
road width, scaling the whole tower, adding runtime coordinate thresholds, or applying a
blind sign mapping to every central vertex. No such replacement has been made yet.

The isolated `bridge/suspension-golden` worktree is at
`C:/Users/admin/Documents/Codex/CS2BridgeSuspensionGolden`. Its metaprogram has a
`--golden-inspect input.Geometry output.json` diagnostic export; runtime generation there
has not been modified. Its older baseline must not be installed over the current dev build.
