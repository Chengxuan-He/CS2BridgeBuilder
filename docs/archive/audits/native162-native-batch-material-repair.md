# Native batch material submission repair

This is diagnostic evidence, not a new material contract. The repair applies to the
shared generated layered-road and current-bone decal programs used by movable bridges.

## Reported owner and actual inputs

The inspected Draw03 road owner was `b3ecf2943-756e-4c85-b3a4-7a5722e4ebeb`.
Its saved `Mechanism Appearance Group 1` has five ordered material domains:

| Slot | Consumer | LOD0 triangles |
| --- | --- | ---: |
| 0 | Original selected-road median/curbs | 530 |
| 1 | Prototype full-period walking grating | 100 |
| 2 | Prototype derived road background | 356 |
| 3 | Prototype full-period road cross-section | 1,036 |
| 4 | Current-bone source decal volumes | 560 |

The Original surface retains source RoadEU base/normal/mask and world-layer
albedo/normal/mask textures. Its actual Geometry has four-component UV0, including
the evaluated source coordinates. The source descriptor, stored texture payloads
and native material bindings were present. The decal slot retains the original
LaneLine artwork and three native PBR textures. This investigation did not replace
either material with a white proxy or change its UV density.

The captured Original and decal attribute/index streams are byte-identical across
the three saved LODs. Other prototype submeshes are not assumed identical.
The ten bridge-family review found a common Original preparation route rather
than a bridge-specific omission.

## Reproduced defects

An immediate non-DOTS draw was insufficient to validate the generated programs.
The original installed program containers failed actual Unity
`BatchRendererGroup` submissions with:

`UnityPerMaterial CBuffer inconsistent size inside a SubShader`

The layered container mixed 1,056-, 1,040- and 624-byte declarations in referenced
parameter records. The decal container mixed 240-, 256- and 624-byte declarations.
The authoring scripts had treated the second parameter-record integer as a fixed
record-kind discriminator and updated only a few observed values. That skipped
real DOTS records. Common declarations alone did not repair those records.

The decal also substituted a non-DOTS fragment program/header for a DOTS variant,
retaining obsolete `UnityPerDraw` projection declarations. After correcting the
first defect, native submission exposed a second error:

`Trying to render a BatchRendererGroup (or Entities Graphics) batch with wrong cbuffer setup. Missing DOTS_INSTANCING_ON variant?`

The corrected authoring path updates every referenced material declaration,
preserves the native DOTS fragment program identity and removes the fragment
PerDraw inputs already replaced by current-bone vertex varyings. The decal bone
parameter is appended after the complete original 256-byte material block.
Original fragment field offsets, artwork, alpha blending and sampling operations
remain unchanged. Renaming a buffer containing built-in fields was tested and
rejected by Unity; it is not the final implementation.

## Actual GPU verification

The isolated Unity 2022.3.62f2 host used a real BatchRendererGroup, raw instance
buffer, Unity metadata bindings and SRP renderer-list submission. It did not use
the prior diagnostic aliases for DOTS buffers. Native HDRP global matrices had to
be published by the host before meaningful pixel comparisons were possible.
The game was not launched.

| Actual saved input | Old native batch | Corrected native batch |
| --- | --- | --- |
| Original mesh and six original BC7 textures | Rejected; zero pixels | 21,648 nonzero pixels |
| Decal volumes and three original BC7 textures | Rejected; zero pixels | 55,560 nonzero pixels |

For both corrected draws, all four floating-point render-target SHA256 hashes
match the corresponding immediate non-DOTS reference exactly. This compares the
complete GBuffer/DBuffer outputs, not just a mean color. The corrected decal image
contains the source solid and dashed lines. The corrected Original image contains
the textured median and curbs.

The same successful batch replay was repeated using containers compiled by the
current BridgeBuilder DLL's runtime compiler, after native program-envelope
serialization and readback. Malformed/truncated envelopes retained explicit
failure diagnostics and no partial result. The final postprocessed candidate also
passed runtime compilation and envelope roundtrip checks.
The final packaged-container check covered 126 referenced material declarations
and 77 DOTS variants, including their program headers and absence of obsolete
DOTS `UnityPerDraw` inputs. All eight complete float-target hashes matched.

Replay source: `tools/MaterialShaders/Material162BatchReplay.Editor.cs`.
Set `BB_MATERIAL_REPLAY` to the captured replay directory and run its public
methods in an isolated Editor assembly with unsafe code enabled. Evidence under
`work/material162` includes capture provenance, all-LOD byte comparisons, old
failure logs, runtime-compiled GPU logs and the four-target hash files.

## Build and installation

Main and frozen-source builds, the engine-free test suite, all twelve localization
languages, local documentation links and `git diff --check` passed. Resource
guards verified 351 Surfaces, 103 unique Textures, no packaged non-road edges,
907 typed resources and 181 AoS programs. Existing compiler warnings were retained.
No UI strings changed.

Local native162 was installed with Cities2 stopped. The receipt verifies 964
installed files: only DLL/PDB changed, two original files were backed up, and no
files were added or removed. No saved owner was rewritten or deleted.

DLL SHA256: `0A4576087FDF1F8300A176CC75979E374C78743FA1DFD7BA585C9CD11E17E69F`.

Receipt: `artifacts/material-remap/shared/native162/deployment-native162.json`.
Official SDK archive: 77,903,093 bytes, SHA256
`D8B7DEDFCD064B59FCFADC650E16B02B0E9F248996E87FEC981E4579D91AA935`.
Nothing was
pushed or published.

Automatic approval review rejected the final cleanup command for task-created
diagnostic-host files and redundant test build outputs in the frozen source copy
with `blocked by policy`. It was not retried or bypassed. Those temporary files
remain; they are not included in the installed mod or SDK package. The retained
test sources and evidence are available for reproduction.

## Acceptance limits

These tests reproduce and repair native batch submission defects. They do not
prove that every pale area in the supplied screenshot had the same cause: the
carriageway also contains distinct prepared prototype materials. Full game HDRP
lighting, all VT residency cases, moving poses and the complete scene remain
manual acceptance items. No visual acceptance is claimed from compilation alone.

Newly generated previews/bridges use the corrected program packages. Existing
owners retain their saved program payloads; this repair does not silently rewrite
them. Test a newly generated bridge before judging the repaired route. Bridge
Builder must remain enabled for generated custom-program registration under the
previously authorized temporary deferral of mod-disabled cold loading.
