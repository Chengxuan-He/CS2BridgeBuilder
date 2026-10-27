# Offline native cable material preparation

Run from the repository root:

```powershell
dotnet run --project tools/PrepareCable -- '<game>/Cities2_Data' tools/PrepareCable/NativeTemplate.json assets/Materials
```

The tool reads the native Powerline Cables Surface by CID from the game's content archive,
checks its source digest and resolves its appearance against the measured CurvedTransparent
template. `NativeTemplate.json` is the extracted native template 30 from game 1.6.2f1; its digest
is checked before use. Do not replace the evidence or bypass the check when updating the game:
review native defaults and the object-conversion recipe first.

Output is native Surface v1 and Texture v5, with deterministic source/recipe/content identities,
in each movable style's `Cable/` folder. Only the finished native payloads and CID sidecars ship
with the mod. The source template and this tool are offline authoring inputs. No game scene or
generated bridge is required, and no original game file is changed.

Run `PreparedMaterialCheck`, `PreparedCableCheck` and `Test-PreparedNative.ps1` after preparation.
See [prepared materials](../../docs/Prepared-Materials.md) for lifetime, packaging, coverage and
remaining migration work. Successful binary checks do not establish in-game visual acceptance.
