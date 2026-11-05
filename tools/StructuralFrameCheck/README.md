# Measured structural frame verification

`MovableStructureModel.Plan.frameTransforms` is an optional array of final Plan
vertex index / direction Jacobian pairs. Coordinates are exactly those of the
plan's `SourcePositions` and `Positions`; this is not a world-space transform.
The style supplies the corresponding measured position deformation. A shear
`x' = x + s(y - anchor)` uses an identity matrix with `m01 = s`, without translation.
Finite signed and collapsed Jacobians are accepted. A collapsed transform retains
the source frame when its direction cannot be recovered; a reflection adjusts tangent
handedness. Membership
must be unique and within `Plan.Positions`, including appended edge/interior
vertices. Constructed channels interpolate from the original source bytes first;
then their explicitly supplied Jacobians apply. They do not implicitly inherit
endpoint frame corrections. Coincident vertices may have distinct frames.

Build decodes native octahedral normals/tangents, applies inverse-transpose to N
and the forward Jacobian to T, orthogonalizes T against normalized N, preserves
handedness, and uses the game's native encoders. All unrelated channel bytes and
unlisted vertex frames remain unchanged. Compact output remaps final Plan indices.
An additional UV-frame rotation on the same vertex is explicitly rejected;
measured source-UV-preserving domains retain the corrected frame through Retile.

Copy `StructuralFrameCheck.Editor.cs` into the existing isolated Unity project's
Editor directory, then run `-executeMethod StructuralFrameCheck.Run` with
`-gameManaged <Cities2_Data/Managed>` and `-modAssembly <candidate DLL>`.
The production Plan/Build/native-upload path is invoked by reflection, with native
packed channel inputs. Twenty-four combinations cover original/constructed frames,
compact/noncompact output, positive/zero/negative shear and both handedness signs.
Coincident constructed twins use independent shear/identity frames. Checks compare native
encoded output, unchanged positions/UV4/color/bones and unlisted N/T, and reject
nonfinite, malformed, duplicate and out-of-range declarations. Signed and zero
scale are positive cases; they do not gate generation.

This controlled fixture tests the public mechanism. It does not replace each
style's real source membership, full Retile, final geometry or in-game acceptance.

## Exact source copies

Plan's optional `int[] sourceCopies` explicitly lists original source indices.
Copies follow original, edge and interior vertices in final Plan index order.
`constructedX` supplies each independent X position; the source Y/Z are retained.
Frames use the final index and may differ between original and copy. Copy provenance
must be an original vertex of each fragment's source triangle; copies cannot refer
to another constructed vertex. No source-edge interpolation endpoints are faked.

Raw attributes copy before frame correction. Source-only duplication retains the
original UV format, including Float16 UV4, instead of triggering interpolation
promotion. If a plan also has genuine interpolated vertices, its existing UV
promotion applies uniformly without losing source values. Compact output and
Retile use the explicit original provenance. Plan wrappers must forward SourceCopies
alongside existing constructed positions, frames and UV domains.

The expanded native suite has36 cases, including source-copy positions, independent
frames, compact index remapping and original Float16 UV4 bytes. These are controlled
API checks; style membership and actual downstream Retile remain separate evidence.

## Prepared-material tangent projection

`PreparedTangentCheck.Run` accepts `-gameManaged` and `-modAssembly` in the same
isolated Unity environment. It invokes actual Plan/TryBuild, structural Retile and
native upload with a controlled measured-domain fixture and native packed frames.
Eighteen cases cover source, edge-interpolated and exact-copy vertices, existing
geometry shear, omitted projection, nontrivial projected UV and singular UV.

PreparedDomain's optional TangentProjections map uses final Plan indices and
ProjectedUvFrame U/V gradients in final object-local coordinates. Retile decodes
the already transformed native normal, derives the new tangent/handedness and packs
it using the native codec. Normals and unrelated channels remain byte-identical;
an omitted or singular projection retains the original tangent bytes. The test
compares native encoded tangents independently and retains position, normal,
color, bone and UVzw. It does not assert real-game shader appearance or replace
the style's actual-source mixed-material coverage tests.
