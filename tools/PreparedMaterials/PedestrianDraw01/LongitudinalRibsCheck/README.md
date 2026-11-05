# PedestrianDraw01 underside rib verification

The native detailed and middle mechanisms each have seven independent longitudinal
ribs per leaf, centred at X = -3, -2, -1, 0, 1, 2, 3 metres. Each member has 120
vertices and 60 triangles. Exact component membership and original Geometry hashes
are recorded by `tools/GeometryMetaprogram/PedestrianDraw01LongitudinalRibs.py`.
The producer verifies the NPZ against the native OBJ indices/positions and original
leaf bones before emitting the checksum-protected embedded resource.

Each original member moves rigidly by `centre / 4 * halfWidthDelta`. Internal
centre intervals become `1 + halfWidthDelta / 4`; the central member stays fixed.
This preserves its approximately 0.20836 metre section. The original outer support
inner edge is at approximately 3.68646 metres, not 4 metres. The mapping preserves
the original unequal edge offset instead of claiming equal boundary clearance.
No runtime coordinate classification is introduced.

LOD2 has a simplified merged shell and no independent internal ribs: interior
moving-shell source vertices have no X coordinate inside +/-3.68 metres. Its
existing representation is retained without adding near-detail members.

Generate the fixture and resource using Python with NumPy:

```text
python tools/GeometryMetaprogram/PedestrianDraw01LongitudinalRibs.py RESEARCH ROOT OUTPUT
```

Run the actual compiled production plan (not a duplicate implementation):

```text
dotnet run --project tools/PreparedMaterials/PedestrianDraw01/LongitudinalRibsCheck -p:GameManagedDir=MANAGED -- MOD_DLL MANAGED OUTPUT/fixture.json
```

The test covers three LODs and half-width deltas -2, 0, 2, 8, 24 metres. It verifies
exact rigid positions, unchanged Y/Z, original rib section widths, retained face
indices and absence of replacement fragments/frame transforms on the ribs.
The native80 baseline fails at LOD0 delta -2 vertex1527, exposing the prior central
rib sign-X widening. Full native mesh upload, UV material dispatch, visual motion
and cold-load acceptance remain separate tests.

No localization text or native persistence layout changes are introduced. Existing
saved bridges retain their serialized geometry. Both preview and new creation use
the same changed structure-plan path.
