# Preview composition packing and boundary objects

The November 14 preview report selected Road Builder's 2+2 highway and Truss Viaduct.
Its missing inner markings and unequal structural ends had two independent shared causes.

## Native composition packing

`BridgePreviewComposition` called `CalculateCompositionData` before handing its pieces to
`BridgePreviewObjectCourse`. The latter sent those already packed records into native
`InitializeCompositionJob`, which called the same packing function again. Native packing adds
section widths to the existing offsets; it is not idempotent.

The November 14 00:28:08 capture `520de5647baf4a989431293ad3f61c69` used the same selected
Road Builder road with GO Viaduct, not the reported Truss preview. Its four-metre lane centers
were -14.5, -6.5, 6.5 and 14.5 metres. Native secondary-lane selection consequently saw gaps
between lanes, selecting boundary markings instead of the adjacency-dependent inner lines.
The road's actual section widths are 2, 4, 4, 2.5, 4, 4 and 2 metres. One native packing gives
lane centers -7.25, -3.25, 3.25 and 7.25 metres. The offline native-method projection reproduces
both the captured incorrect values and the corrected adjacent spacing.

The preview now retains pre-pack records for native initialization. After initialization,
road meshes, lanes and object rules all consume the resulting composition. No marking prefab,
color, UV, lane requirement or generated bridge asset is replaced to conceal the error.

## Continuous structural endpoints

The original Truss Viaduct support contains seven meshes. Its principal support's original
Y extent is ±10 metres; native X-axis rotation makes that its longitudinal extent. It is
not an authored one-sided structure. For a 64-metre edge, native non-even 14-metre spacing
places five occurrences at 0, 12.8, 25.6, 38.4 and 51.2 metres. The adjacent edge supplies
the next occurrence at 64 metres. Previously, preview context edges participated in native
geometry but all their object occurrences were discarded. Ordinary straight continuations
do not satisfy the prototype's dead-end/intersection node-object requirements.

The preview now admits native context-edge occurrences whose anchors belong to the visible
course, including boundary anchors. It keeps whole objects and their outward structural
extensions. Context road meshes remain hidden; no artificial dead end, mirrored support,
triangle clipping or blanket node-object removal is introduced.

## Verification and limits

`tools/PreviewPieceObjectCheck` exercises production native placement enumeration and the
production visible-domain selector. Its packing fixture projects the installed game's exact
`CalculateCompositionPieceOffsets` method, changing only visibility and container types to
managed arrays/dictionaries. The reproducible extraction script and native source are under
`artifacts/preview-markings`. The checks cover single packing, the old double-pack failure,
four-metre adjacency, forward/reversed visible intervals, adjacent visible spans, unique
boundary occurrences and complete support extents. They do not execute a live ECS world or
render an in-game image. Runtime cancellation and world disposal paths are unchanged.

The production build and automated checks are recorded beside the evidence. Live confirmation
of the corrected Truss preview, native line appearance, endpoint geometry and other road types
remains a separate player check; the game was not launched or stopped for this repair.
