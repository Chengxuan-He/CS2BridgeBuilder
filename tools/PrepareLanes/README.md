# Offline Native Lane Materials

Run from the repository root:

```powershell
dotnet run --project tools/PrepareLanes -- '<game>/Cities2_Data' tools/PrepareLanes/NativeTemplates.json assets/Materials
dotnet run --project tools/PrepareLanes -- '<game>/Cities2_Data' tools/PrepareLanes/NativeTemplates.json assets/Materials --check
```

The preparer reads real installed curve Surface/Material descriptors and their CID sidecars in
the Game and BridgesAndPorts content archives. `NativeTemplates.json` contains offline-extracted
material defaults and stack layouts for source templates 16/31/34 and object templates 0/1/35.
Re-extract and review that evidence when the native shader contract changes.

Conversion preserves the VT header and texture references byte-for-byte, copies every source
override, materializes missing appearance defaults and adds the native octahedral-tangent keyword.
All referenced native payload identities must exist in the installed archives. No texture is baked,
resampled, recompressed or packaged as a duplicate built-in resource.

Complete source bytes identify variants, including sources with different CIDs but identical
descriptors. Output identity also includes the template evidence, recipe and resulting bytes.
Each populated style directory contains its own finished Surface and CID sidecar. The check mode
recomputes the expected descriptors without writing and compares every packaged variant.

The measured catalogue has 103 source descriptors, 26 unique variants and 14,128 bytes per style.
This establishes offline conversion coverage for these descriptors, not arbitrary custom sources
or game rendering acceptance. Runtime binding additionally checks resolved native defaults and
properties before sharing the immutable descriptor.
