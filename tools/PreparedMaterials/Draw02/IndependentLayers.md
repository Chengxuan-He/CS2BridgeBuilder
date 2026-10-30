# Draw02 independent layer integration inputs

Draw02 reaches `MovableSurfaceAtlas.Compose` through `Draw02RoadSampling` and
`MovableRoadPreparation`. Its `PrototypeRoad` flag is true. Moving network
receivers retain their selected-road projection while carrying a separate
prototype road patch; compatible object surfaces bypass this composition.
The shared composer owns layer selection, masks, worldspace sampling, period
evaluation, output materials and persistence. No Draw02-local texture compositor
exists to replace independently.

## Source fixture

`LayerSamplingAudit.py` reads the actual BridgesAndPorts archive and verifies both
mechanism Surface hashes against the measured catalogue. It decodes the native
descriptors and checksum-valid `Types_Draw02_Draw02RoadAppearance_cs.bin` resource,
including raw binary32 bits. Run it with an explicit output path:

```powershell
python tools/PreparedMaterials/Draw02/LayerSamplingAudit.py artifacts/layer-decoupling/Draw02/outputs/source-inputs.json
```

The near and middle mechanism use Surface `8a28ab8d9526e258efbcb81c3bbc936b`;
the far structure uses `436d1828c36d1336cf115c85def2c62c`. Both are native VT
template 1. Road preparation deliberately obtains its prototype appearance from
the near Surface for every road LOD. The far structural Surface is not a road
appearance substitute.

Four patch families produce sixteen side/leaf projection aliases: moving
carriageway, moving sidewalk, fixed road and fixed sidewalk. The moving
longitudinal rates are exactly -0.25 and +0.25 cycles per metre (4 m periods).
The carriageway transverse rate is binary32 -0.6666666865348816; the two
sidewalk rates are -0.3448246717453003 and +0.3448246717453003. Each sidewalk's
phase follows its actual selected-road outer edge independently.

## Required shared behavior

- Retain the three transverse carriageway bands, including grating holes and
  solid strips, with their measured patch coordinates and native cutout semantics.
- Keep the selected-road base, worldspace/prototype substitution and blend mask
  independently sampled. The selected-road descriptor is a runtime input and is
  not represented by the two prototype fixtures.
- Preserve source UVs, physical phase, normal frames, all channels and LODs.
  An unsupported mixed layer cannot trigger full-road rasterization or removal
  of that layer.
- Preserve the existing fixed NetPiece native-material route. Its network shader
  capability does not establish support on a skinned moving object.

The existing native shader inventory in `../Draw03/NativeLayers.md` records one
base/normal/mask/control family on Default object variants, while the network
worldspace-layer shader lacks the required procedural object-animation interface.
A new shared implementation must demonstrate the complete native object path;
changing a material template or preserving only textures is insufficient.

The source fixture validates input identity and numeric provenance. It does not
establish a replacement phase tolerance, native independent-layer rendering,
game appearance or cold loading. No style source, geometry, material payload,
localization string or installation is changed by this audit.
