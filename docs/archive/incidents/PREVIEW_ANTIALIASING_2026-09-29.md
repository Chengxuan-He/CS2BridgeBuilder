# Preview supersampling

The private transparent preview already rasterized at 3072x1536 and box-resolved
to 1536x768. HDRP postprocessing is intentionally disabled, and the native HDR
colour capture occurs before postprocessing, so simply enabling camera FXAA/TAA
would not antialias the published colour/coverage pair.

The preview now captures four quarter-render-pixel projection offsets on separate
submitted frames. Together with the existing 2x spatial resolution these form a
regular 4x4 sampling grid per output pixel. HDCamera.UpdateAllViewConstants in the
installed HDRP reads camera.projectionMatrix; both native shading and the coverage
renderer list therefore use the same offset. Projection reset before every sample
prevents drift. Bridge geometry, framing, lights/materials, city cameras and game
graphics settings are unchanged.

Premultiplied linear RGB and coverage accumulate at output resolution. Final
unpremultiplication, exposure, colour mapping and PNG encoding occur only after
all four samples. This preserves transparent silhouettes without blending clear
black into the cables. The existing texture warmup applies before the first
sample; the extra samples add three camera renders and readbacks. HDR target
dimensions are unchanged; the output-resolution accumulator uses about 18 MiB.
Cancellation/disposal releases the accumulator and unsubscribes the render callback.

Release build and diff checks passed. No visual unit tests were used; actual
antialiasing quality, fine-cable visibility, absence of fringes, framing stability
and preview latency require human verification in the creation and management UI.

Local deployment backup:
`C:\Users\admin\Downloads\BridgeBuilder-preview-aa-20260929`.
Required pre-deployment cleanup backed up and removed generated test assets/state;
the preserved corruption laboratory was not modified or reinstalled. Save files
were not edited. No GitHub or Paradox Mods publication was requested or performed.
