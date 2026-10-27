# Offline Worldspace Asphalt Material

Run from the repository root:

```powershell
dotnet run --project tools/PrepareRoad -- '<game>/Cities2_Data' assets/Materials
dotnet run --project tools/PrepareRoad -- '<game>/Cities2_Data' assets/Materials --check
```

The preparer reads the real Road Surface with CID `1d7dad9f64c9fe55a809f1080c39fb92`
from the installed Game content archive. Source SHA-256:
`99f48ff7b6296957b5170c7fdf14ce65912c1de8eb81b15ba7c38b5189be1572`.

Recipe `BridgeBuilder/SkinnedWorldspaceAsphalt/1` maps the three worldspace texture
bindings into native object template 0, adds a linear zero-control texture, disables
worldspace snow and enables native octahedral tangents. The original albedo, normal
and mask texture CIDs remain unchanged built-in references. No texture is resampled,
baked or compressed. The original worldspace UV scale remains a geometry parameter.

Each style's `Road/` graph contains a native Surface, a 1x1 Texture and their sidecars,
totaling 267 bytes. Prepared Surface CID: `88df662f6bc8058d3ecf8f7a5d7f2239`.
Run `tools/PrepareCable` first: the identical zero-control payload reuses its prepared CID,
with a byte-identical copy in every Road variant. It does not allocate a second control identity.
Output identities incorporate the source, recipe and payload digests. Check mode
recomputes expected bytes and compares each packaged file without modifying it.

`tools/PreparedRoadCheck` tests production binding and source leases with native API
doubles, including changed texture identities and invalid density/texture inputs.
`tools/PreparedMaterialCheck` verifies independent snapshots and built-in boundaries.
The Unity native reader probe separately checks actual serialization and texture upload.
These checks do not establish game appearance or cold-load acceptance.
