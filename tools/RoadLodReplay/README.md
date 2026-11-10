# Selected-road LOD reuse and replay

The production cache compares complete ordered triangles (all attributes/provenance), pose,
partition trees and occurrence/face selectors, native bone metadata and resolved Surface/mapping
values. It is local to one render graph. Structural LODs remain independent. Cached importer
buffers have one owner; named model views are read-only borrowers until native capture/save
has copied their data. Equality/store time and cache hit/miss counts are logged explicitly.

`Road134Check.Editor.cs` loads the actual candidate assembly into the retained native Unity
validation host. It checks identical/different inputs, seam clipping, model encoding and
publication lifetime. Its single-triangle fixture is synthetic correctness evidence, not a
performance result for a user road.

## One-shot actual-input capture

With the game exited, create `ModsData/BridgeBuilder/RoadReplay/capture.once` below the game
user-data directory. Its complete text is the desired stable style ID, for example `Draw01`.
The next **permanent creation** of that style consumes the marker. Previews never consume it.
The user must manually start the game and create the selected sample; this file is diagnostic
arming only, not a construction request or a way to start the game. Each unique computed
road/lane LOD is recorded under a new RoadReplay subdirectory. Reused LODs are identified in
the normal per-LOD reuse counters and do not repeat large input payloads. Captures are capped
at 512 MiB raw triangle input per graph; a limit/error produces a diagnostic warning and no
change to generation success. Capture IO/hash time is a separate diagnostic stage and must
be excluded from normal performance comparisons. Never overwrite existing captures.

## BRR1 binary format

Little endian: Int32 magic `0x42525231`, Int32 triangle stride, Int32 triangle count, raw
sequential triangle bytes. A vertex contains position3, normal3, tangent4, UV4, color4
Float32 fields; three vertices plus material/occurrence/sourceTriangle Int32 form a triangle.
Then pose position3/quaternion4 Float32; Int32 boneCount, surfaceCount, mappingCount;
for each mapping Boolean planar plus Float32 scale. Next Int32 partitionCount/rootIndex;
each partition has Int32 bone, Boolean isGap, Int32 negative/positive indices (-1 absent),
16 Float32 Matrix4x4 elements in native indexed order. Next Int32 occurrenceCount (-1 absent)
and node indices; Int32 faceMapCount (-1 absent) and occurrence/sourceTriangle/node triples.
Finally Int32 expected bound-triangle count and 32 bytes SHA256 of the complete ordered native
BoundTriangle array. Capture preserves source bits, not a JSON-rounded approximation.

CPU/GPU replay must use this same input, motion decisions and output order. Record clipping,
binding and encoding separately. GPU totals include allocation/upload, CPU prefix scheduling
if needed, dispatch synchronization and final readback. Hardware/compiler limitations or
precision mismatches disqualify equivalence; an experimental time is not production speedup.
