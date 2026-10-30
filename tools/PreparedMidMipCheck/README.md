# Prepared VT mid-mip regression

`PreparedMidMipCheck` links the production preflight, Surface reference reader and
native MidMips membership reader. Supply a candidate style directory, an unchanged
broken style fixture whose parent has no common cache root, and original VTTexture
fixture carriers with CID sidecars. It checks the 21 converted lanes and paint,
the zero-level SubwayRails control, four empty lanes, missing-cache rejection and
malformed native cache rejection. Test build/output directories must belong to the
assigned independent environment.

`../Test-NativeMidMips.ps1` runs `GpuBake/NativeMidMipCheck.Editor.cs` in an independent
Unity project. The fixture calls the installed engine's actual
`AtlasMaterialsDatabase.PreRegisterToVT`, `SurfaceAsset.LoadTexturesVT`,
`VTTextureAsset.Load` and `TexturesAsyncLoader.StartStreaming`. A file-only database
interface supplies immutable original carriers; material grouping, native atlas
reservation, header validation, native memory allocation and asynchronous reads
use the engine implementations. Original Texture IDs are provenance-only dummies,
as allowed by the native reader. No game construction or generated-owner API runs.

The missing-cache control passes zero preprocessed levels and must reproduce the
reported native mismatch. The positive case resolves the level count through
native cache membership and reads the mip tail asynchronously, comparing every
loaded byte with its original carrier. A legitimate zero-level source stays zero.
BC7 middle-mip and trilinear block equality is independently checked by
`../PrepareMidMips/PrepareMidMips.py` and its source fixtures.

These checks establish native cache reservation and texture-loading behavior.
They do not claim full GPU rendering, game startup discovery, motion, visual
appearance or a human-generated owner's material footprint.
