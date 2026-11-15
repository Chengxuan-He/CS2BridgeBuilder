# Preparing repeat materials

Use this procedure for eligible road content under the [source-atlas requirements](../specs/material-spec.md#repeat-unit-producer-format-and-sampling). Read the complete material specification and contract first. The specification owns eligibility, manifest fields, sampling, native formats and acceptance criteria; this guide owns command execution. A successful producer run is not in-game visual or cold-load acceptance.

## Prepare an isolated workspace

Use a private Unity project, source-output directory, prepared-material output and log. Follow the [coordination and environment obligations](../contracts/development-contract.md#project-implementation-requirements) for ownership and cleanup. Obtain the exact original Surface, complete native channel/mip inputs and reviewed periodic-boundary evidence identified by the manifest. Search the actual built-in Blob archives and VT archives before concluding an original Texture is unavailable.

## Extract VT-only sources

For a VT-only source, run the adapter before the repeat producer:

```powershell
python tools/PrepareRepeatUnits/ExtractVtSources.py <manifest.json> <private-source-output> <Cities2_Data>
```

Use its resulting `manifest.json` directly with the producer below. Keep reconstructed full-source Textures and source reports in the private workspace; they are offline inputs, not packaged prepared materials. Review `extraction.json` and source provenance against the specification's reconstruction and complete-mip requirements. Do not copy the game's large original MidMips caches into the prepared package.

## Run the repeat-unit producer

Run the shared isolated Unity entry point from the repository root:

```powershell
./tools/Prepare-RepeatUnits.ps1 -Style Lift01 -ProjectPath <private-unity-project> `
  -GameData <Cities2_Data> -Manifest <manifest.json> -Output <private-output-root> `
  -PreparedMaterials <prepared-Materials-root> -Report <report.json> -LogPath <unity.log>
```

Replace the style and paths with the actual reviewed input and private environment. Inspect the report and Unity log, then verify the emitted complete native resource graph under the [material verification procedure](../specs/material-spec.md#material-verification-requirements). Preserve source files and report unresolved failures instead of substituting lower resolution, incomplete mips or a full-span bake. Retain reproducible evidence outside temporary environments before their required cleanup.
