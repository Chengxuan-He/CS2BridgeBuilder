# Exact UV-domain recipe storage

The common Python API is `tools/GeometryMetaprogram/MovableUvDomainFormat.py`.
`decode_arrays(ints, floats)` returns NumPy little-endian I32/F32 arrays in the
legacy schema-2 layout. Existing offline readers call it after BBN checksum
validation. Convert integer slices to ordinary lists where JSON or list semantics
require them. `encode_arrays` emits schema-3; `read_bbn` and `write_bbn` handle
the unchanged version-1 BBN1 container.

Schema-3 I32 begins `[-3, domainCount, faceCount, distinctBoundsCount]`. It then
contains four domain-sized columns: consecutive anchor-A increments (initially
relative to zero), B-A, C-A, and signed face counts. Positive counts describe
rectangular domains and negative counts describe triangles. Next comes one bounds
dictionary index per rectangular domain, then consecutive increments across the
complete ordered face stream, initially relative to zero. F32 contains exact
bounds quadruples in first-occurrence order. Dictionary identity is all sixteen
original bytes, including signed zero; no floating-point arithmetic reconstructs
bounds. Integer additions are checked and reconstructed values remain I32.

This is an uncompressed measured numerical recipe, not a compressed byte stream.
The runtime constructs only the demanded render/mesh domains. It keeps one ordered
face array and domain records; unused styles remain unloaded. Native geometry,
domain ordering, UV behavior, source identities and independently saved assets do
not change. Both schema versions remain readable during migration.

Stage every candidate into a new directory:

```powershell
python tools/GeometryMetaprogram/StageUvDomainRecipes.py assets/HardcodedNumbers artifacts/prepared-materials/shared-numeric-recipe/candidate
dotnet run --project tools/UvDomainRecipeCheck -p:GameManagedDir="<game Managed>" -- assets/HardcodedNumbers artifacts/prepared-materials/shared-numeric-recipe/candidate
```

The Python stage verifies complete legacy I32/F32 bytes and deterministic
regeneration. The C# check invokes the actual runtime constructor against old/new
resources and compares every anchor, flag, ordered face and Float32 bounds bit.
Current evidence covers 210 resources, 1,922,488 domains and 2,122,916 faces.
All 1,368 actual generation plan fingerprints also match the prior coherent build.

The isolated official SDK package in `artifacts/shared-numeric-package` contains
the identical 893-path file list and measures 47,521,306 bytes, compared with the
60,043,790-byte prior package. Only DLL/PDB bytes change; material payloads retain
their bytes. This establishes the isolated package-size result, not generated-owner
closure, structural source coverage or human acceptance. Formal resource migration
is complete after all direct/delegated consumers passed compatibility checks. The
coherent formal SDK package is 47,521,314 bytes; its measurement and hash are in
`artifacts/shared-numeric-formal-package/measurement.json`. Original-only backups
retain all 210 old resources. The formal build repeats the complete C# and 1,368-plan
checks. No runtime user-visible text changes; localization behavior is unchanged.

## Consumer inventory

All paths below are under `tools/`; unchanged delegated callers retain their shared
reader rather than duplicating the codec. Historical generated scripts inside
artifact environments are not maintained entry points; regenerate them from tools.

| Scope | Readers and producers |
| --- | --- |
| Common | GeometryMetaprogram/MovableUvRecipes.py producer; MovableUvDomainFormat.py codec; runtime MovableUvDomains; lazy MovableUvCatalogue |
| Draw01 | PreparedMaterials/Draw01/BuildUvCharts.py |
| Draw02 | ExportNativeCharts.py; CheckNativeCharts.py and GroupStructureUnits.py delegate to it |
| Draw03 | GenerateUvCharts.py and GroupStructureUnits.py decode only domain arrays; native chart arrays retain their separate schema |
| Lift01 | BuildUvCharts.py delegates numerical decoding and returns ordinary Python integer lists |
| Lift02 | ExportNativeCharts.py; delegated CheckNativeCharts.py and PrepareStructureUnits.py; ChartBindingCheck and RegionBindingCheck use actual MovableUvDomains.Items |
| Lift03 | GeometryMetaprogram/Lift03NativeUvCharts.py; C# receiver tools use actual MovableUvDomains |
| Lift04 | GeometryMetaprogram/Lift04UvChartBindings.py preserves tuple-of-int results; AuditStructureInventory.py and TestUvChartBindings.py delegate to it |
| Lift05 | BuildUvCharts.py |
| PedestrianDraw01 | ExportNativeCharts.py, AuditStructureGrouping.py, StageExactTriangleCharts.py |
| PedestrianDraw02 | GenerateUvCharts.py, StageTriangleCharts.py, StageUnionCharts.py |

Style-relative entries are inside `PreparedMaterials/<style>/`. The five remaining
styles' direct reader functions/compositions were exercised against both schemas
across 148 resources; delegated callers retain those exact functions. Both Lift02
C# projects compile without warnings. Separate owner evidence covers Lift04/Lift05
and both pedestrian styles, including private full PedestrianDraw02 regeneration
with byte-identical chart and override outputs. Common C# exact reconstruction
and formal plan fingerprints cover all 210 resources regardless of style.
