# Native renderer group audit

Run from the repository root:

```powershell
dotnet run --project tools/NativeRenderGroupAudit -- producer.json report.json
```

An optional third argument is an immutable candidate DLL. This invokes its actual
planner by reflection; candidate 53 is supported for before/after comparison.
Without it the tool links the current production planner.

Supply the actual post-Retile/Compose append order, before native batch balancing:

```json
{
  "style": "Lift01",
  "scope": "controlled road fixture; not a saved game owner",
  "provenance": "absolute path to source-native producer report",
  "stableSupplementKeys": false,
  "levels": [{
    "roadStart": 2,
    "laneStart": 4,
    "slots": [
      {"surface":"actual-CID-A", "role":"structure", "triangles":1, "partIds":["tower-core"]},
      {"surface":"actual-CID-B", "role":"structure", "triangles":1, "partIds":["glass"]},
      {"surface":"actual-CID-C", "role":"road", "triangles":1, "partIds":["road-left"]},
      {"surface":"actual-CID-D", "role":"road", "triangles":1, "partIds":["road-right"]}
    ]
  }]
}
```

The example is schema only, not measured geometry. Include every real LOD.
`roadStart` is the first appended road slot; `laneStart` is the first appended
lane slot (the slot count when no lanes follow). Surface identities must preserve
actual cross-LOD identity, never names or visual similarity. Distinct descriptor
instances must remain distinct if the production caller treats them separately.
`stableSupplementKeys` must match the production assembly: true only when all
supplements have complete unique, cross-LOD physical occurrence keys. Roles are
`structure`, `road`, `lanes`, or those exact supplement keys.

`partIds` identify measured physical source-face coverage. A tower in the near
main mesh and its far supplement use the same coverage identity, even if their
slot numbers and Surface identities differ. Do not label near glass and a far
tower as the same part just because both occupy slot 1. Finer roles may be needed
to distinguish geographically separate faces sharing a material. Whole-part
presence does not establish that every face is present.

For a detail explicitly omitted by the original prototype's lower LOD, supply
`"expectedPresenceByLod":{"native-detail-glass":[true,true,false]}` with source
evidence in `provenance`. The tool checks that exact expectation and stable renderer
ownership among required levels. Do not use this field to excuse generated loss
of a part required by the source. Parts without an override are required in all LODs.

The report checks exact slot ownership, native 16-batch capacity, nonempty LODs,
and each supplied part's renderer membership across independent LOD choices.
Exit 0 means those supplied checks passed; 1 means missing/unstable membership;
2 means evidence is incomplete (no physical labels, or the legacy path needs
post-balancing native stream evidence). It does not validate texture loading,
alpha, bones, source-face accounting, or actual game visibility. Source-only,
controlled-road, and saved-owner inputs must be distinguished in `scope`.

Production selection is in `MovableRoadAssets.TryCreate`. The tool follows its
complete structure/road split, protected structural prefix, explicit supplement
roles, then legacy grouping. It refuses to invent physical labels for native
stream slicing performed by the legacy balancing stage.
