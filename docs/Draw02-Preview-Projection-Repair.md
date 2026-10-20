# Draw02 preview projection repair

## Report and diagnosis

The 2026-10-20 23:33:56 local BridgeBuilder log reports failure for
`Large Road Divided / Draw02 / opposite` after atlas production:
`Moving road LOD 0: Road triangle 13 has invalid attributes, material index or mixed tangent handedness.`
The persistent last-export report is older and describes a different operation.

Code inspection found that repeating projection eligibility tested only the
absolute determinant at each corner. Opposite determinant signs were admitted,
so the repeat writer could produce different tangent handedness at the triangle's
corners. The subsequent road binder explicitly rejects that representation.
The original combined error does not establish which predicate failed in the
reported triangle; a full native reproduction remains pending.

## Change

Repeat eligibility now requires the geometric normal and every vertex normal to
have nonzero, finite projection determinants with the same sign. Non-repeatable
receivers use the already existing planar composition path, before chart packing
and texture production. This preserves positions, normals, source provenance and
bone partitions without splitting receiver geometry or weakening binder checks.
Consistent mirrored projections remain supported. The policy applies to all
movable LODs through shared composition; no non-movable path changes.

Binding diagnostics now distinguish material-index errors, invalid vertex
attributes and mixed tangent handedness, including source triangle/occurrence
and material context. These are developer diagnostics, not new UI strings.

## Verification and deployment

MovableProjectionCheck links the production geometry helper and binder. It tests
ordinary and mirrored projections, opposite corners, singular/nonfinite frames,
continued rejection of mixed handedness, and planar receiver geometry/provenance
preservation. It passes. Release build and all twelve LocalizationCheck languages
pass. No source prototype, healthy saved asset or native material was modified.

The game was absent at deployment. Original installed DLL/PDB files are backed up
under `artifacts/backups/20261020-draw02-projection-installed`; edited source originals
are under `artifacts/backups/20261020-draw02-projection`. Deployment hashes match:

- DLL: `2A637D7DFF494752A2144EC11B9BBAF1D78DD8CE768055315D0D35C999EBFA1B`
- PDB: `8B203CA2787438937144814BBE9BF733993F6EB0BB76FBAE18FDF1B6A5079E29`

This replaces the binary fingerprint in the earlier batch handoff. Human
acceptance must retry Draw02 opposite preview and creation, all LODs and motion.
Neither the regression test nor deployment establishes that the complete reported
preview now succeeds.
