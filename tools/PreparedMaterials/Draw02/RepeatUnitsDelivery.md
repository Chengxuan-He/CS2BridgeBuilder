# Draw02 prepared repeat-unit delivery

## Production source library

`assets/Materials/Draw02` contains 112 files / **6,017,894 bytes**, including 39 Surface descriptors and 17 Texture payloads. This is below the current 10,000,000-byte per-style working target; it is not the final SDK package size or generated-bridge footprint. No full-atlas authoring inputs are packaged.

Five production sampler keys resolve through `Draw02RepeatUnits.cs`: moving sidewalk, fixed road, fixed sidewalk, and opaque/cutout variants of the moving carriageway. The carriageway variants share all four texture payloads. All production local-repeat cutout calls use template 14; the opaque carriageway uses template 0. Both retain `_Snow=0` and the native packed-normal interpretation. Cutout variants retain AlphaTest, DoubleSided and DisableSSRTransparent.

Three template-1 descriptors totaling 639 bytes are now active structural-region bindings through `Draw02RepeatRegions.cs`. They preserve the original structural template and share all pixels with the road-composition template-14 variants. They must not be removed as obsolete; see `StructureDelivery.md` for the actual production region coverage and remaining limitations.

## Repeated units and density

| Unit | Texture dimensions | Complete mip levels | Physical period |
| --- | --- | --- | --- |
| Moving sidewalk | 600 by 828 | 10 | approximately 2.9000245 by 4 metres |
| Fixed road | 602 by 752 | 10 | approximately 3 by 4 metres |
| Fixed sidewalk | 545 by 752 | 10 | approximately 2.9000245 by 4 metres |
| Three-band moving carriageway | 311 by 827 | 10 | approximately 1.5 by 4 metres |

All four channels, including real authored Control, retain the corrected full-mip source extraction. The VTSurface tail correction is `498 + high`. Original source chains contain 13 levels; each smaller unit retains its complete 10-level chain. No mip-zero-only chain regeneration was used.

The carriageway's three bands exactly match the measured original recipe and continuously cover normalized x from zero to one. The maximum required normalized-unit source span is 310.9127673744429 by 827 pixels; 311 by 827 therefore retains at least the original density (ratios 1.00028056945 and 1). Its physical texel density is approximately 207.33334 by 206.75 pixels per metre.

The shared trial manifest incorrectly retained the sidewalk's physical width of 2.9000245 metres. The retained Draw02 manifest uses the measured carriageway width 1.49999995529652 metres. Physical-size metadata does not participate in the producer's span, pixel key or sampling function; correcting it requires no pixel preparation. Runtime UVs continue to use the measured carriageway projection.

## Verification and retained evidence

`repeat-units-v2/composite/` contains the corrected manifest, original shared producer report, independent density verification, both descriptor aliases and all five combined production aliases. `CompositeNativeCheck.Editor.cs` uses the installed game's native readers and uploads the existing texture data, checks templates 0/14, Snow and keywords, exact four-channel sharing, native Repeat/Trilinear filtering and reads all 40 channel/mip levels through GPU readback. Unity exited zero. No pixel encoder was invoked by this integration.

The other three units retain their 120 source/output channel/mip comparisons under `trial-result-corrected/verification.json`; maximum pixel-center error is within 0.005 RGBA8 tolerance. Their texture bytes did not change when caller descriptor semantics were restored. `descriptor-semantics/` contains the native template/Snow/keyword validation evidence.

`RepeatBindingsCheck` compiles the actual dispatcher and Draw02 partial implementation: all five production keys resolve with the expected native template and Snow setting; unknown keys remain unmapped. `NativeClosureCheck` validates the official source directory with 17 texture payloads, one physical file per CID, no unresolved dependencies and no orphan textures. All 100 files present before carriageway integration remain byte-identical. No new UI text or localization behavior was introduced.

## Remaining acceptance

The measured five legacy road-repeat calls are now covered. This does not complete all Draw02 material generation: arbitrary structural charts are not genuine repeat units, and out-of-chart structural receiver coverage remains unfinished. Original materials are used where actual receivers permit them. The 953-key structural catalogue is source-use evidence, not authorization to reinstate per-triangle baked resources.

Original-atlas versus isolated-repeat filtered boundary differences remain explicit appearance observations rather than an independent exact-pixel gate. Visual pattern integrity still needs integrated near/far/motion inspection. Preview/create/regeneration/export, native cold loading, per-owner dependency persistence and uninstall independence remain integrated acceptance work. The shared pipeline owns final mod compilation and the complete official SDK package measurement.
