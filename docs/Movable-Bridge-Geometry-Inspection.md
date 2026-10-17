# Movable bridge geometry inspection

## Purpose

`tools/GeometryMetaprogram` supports `--skin-report` for inspecting actual native `.Geometry`
files before identifying replacement road triangles and authored structural parts. It runs
offline and does not start the game, register prefabs, construct bridges or modify game assets.

This command is measurement tooling. Its bone groups are not runtime widening profiles and do
not establish road/structure membership, correct bind poses, animation or visual acceptance.

## Run the inspection

Build the existing tool project:

```powershell
dotnet build tools/GeometryMetaprogram/GeometryMetaprogram.csproj -c Release
```

Extract the required geometry bytes from their original game package into a scratch directory.
Use the exact CID/package entry from the resource audit and verify its SHA-256 against the audit
before decoding. Include every object mesh and every referenced LOD; do not stop at the first mesh
or treat a main-mesh report as coverage of its support meshes.

Run from the installed game's `Cities2_Data/Plugins/x86_64` directory so that the native
`PipelinePlugin.dll` decompressor can be resolved. Pass absolute paths for the tool and input/output:

```text
dotnet <repository>/tools/GeometryMetaprogram/bin/Release/net8.0/GeometryMetaprogram.dll --skin-report <scratch>/<source>.Geometry <scratch>/<report>.md
```

The output is a Markdown report and one `.mesh-N.obj` inspection file for each skinned mesh.
Keep OBJ files in scratch storage. User-facing reports belong in the task's output directory and
must remain Markdown. Each report records the source filename and SHA-256, vertex/triangle counts,
raw bone groups, per-group bounds and mixed-bone triangle counts.

## Native format evidence

The reader follows the installed `GeometryAsset.DecompressIndexData` and
`GeometryAsset.DecompressAttributeData` implementations:

- File flag 1 selects meshopt; file flag 2 selects ZSTD. When both are present, ZSTD is removed first.
- Mesh flag 1 selects 32-bit indices; otherwise indices are 16-bit. Vertex count does not select this.
- Positions in this inspection path must be `Float32x3`.
- The opt-in skin inspection requires the audited rigid `BlendIndices:UInt32x1` layout.
- A geometry mesh without skin data is reported as such; it does not receive an invented bone.
- Unsupported skin layouts are rejected explicitly, rather than interpreted as the audited layout.

The ordinary position/index analysis commands do not opt into the rigid-skin layout check.
The new report validates actual decoded position finiteness and triangle index ranges. It is not
a synthetic visual-generation unit test.

## Interpret the results

Geometry bone indices, procedural connection IDs and attachment parent identifiers are different.
Join geometry indices to the exact corresponding render prefab's `ProceduralAnimationProperties`
array before interpreting motion. Compare the hierarchy, bind poses and procedural types as well.

A bone group can contain road, railings and structural members simultaneously. A triangle with
vertices assigned to different native bones can be intentionally authored structure. Preserve it;
do not delete it just to make the report show zero mixed-bone triangles. The generated road's
rigid seam constraints are a separate requirement.

For widening, identify complete authored logical parts using the highest-detail evidence and
transfer that classification to every LOD. Native bone membership alone is not a widening rule.
The project contract's `x = 0` rule remains unchanged.

## Recorded execution

On 2026-10-16 the command decoded 88 actual geometry assets covering the ten audited native
movable owners, their object meshes and referenced LODs. All source files matched the hashes in
the resource audit. Reports and raw inspection meshes were retained outside game asset roots.
The tool built with zero warnings and zero errors. No visual-generation unit tests or game UI
automation were performed. See [implementation status](Movable-Bridges-Implementation.md).
