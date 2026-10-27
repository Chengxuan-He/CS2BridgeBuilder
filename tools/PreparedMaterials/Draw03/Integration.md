# Draw03 repeat material-domain integration

## Contract scope

The movable contract's section 20 and its preservation rule allow required
physical and appearance-domain partitions. They prohibit receiver subdivision at
paint outlines or texture-repeat periods. A boundary between different lane/gap
source units, or between lane samples with different phases, is an actual
appearance boundary. It is not the gap unit's 32-pixel repeat boundary.

The earlier task-level request to investigate a solution without any additional
cuts was narrower than the contract. Native shader inspection found no compatible
general per-fragment selector under that restriction. The implemented solution
uses the contract's existing appearance-domain mechanism; it does not change the
contract, introduce a custom shader, or add coplanar wear overlays.

## Production implementation

`Draw03RoadPreparation` evaluates each selected-road LOD once for this generation,
collects their actual lateral bounds, and passes those same evaluated inputs to
the existing common preparation overload. Consequently a far-LOD vertex outside
the LOD0 layout is included in the wear domain. These are invocation-local inputs,
not cached generated bridge results.

`Draw03RoadAppearance.Patches` accepts that complete span. `Draw03RoadWear` retains
the measured lane centers, narrow-lane crop limits, native density and gap phases.
`Draw03RoadWearDomains.Apply` expands only banded leaves of the appearance tree:

1. Each real lane or gap interval receives an unbanded immutable native unit from
   `Draw03RoadWearUnits.ForBand`.
2. The plane at each interior band endpoint is `dot(V, position) + V.w - end = 0`.
   A balanced binary tree passes these actual appearance boundaries to the existing
   road clipper. It never inserts planes for individual texture repeats.
3. The clipper preserves interpolated position, normal, tangent, original UV/color
   and source provenance. The original physical partitions remain responsible for
   the native skeleton; appearance indices are not physical bone indices.
4. Each fragment already owns its band, including shared boundary vertices. Its
   full immutable unit and affine map therefore preserve that band's phase without
   trying to infer ownership again from an inclusive edge.

For lane samples, full-unit V is the measured cropped offset plus the band's local
coordinate multiplied by its cropped fraction. For gap samples it is the local
coordinate multiplied by `RepeatY`. U and source Surface remain unchanged. Both
opaque and cutout maps resolve the existing prepared recipe keys; road width and
receiver coordinates are not new material keys.

The entire road layout is not a repeated pattern. Its first and last appearance
domains extend the outer gap's affine map beyond their bounds. This retains the
gap's density and local phase through endpoint roundoff, rather than wrapping all
lanes at normalized V=0/1. The collected actual source span covers all LOD vertices;
physical clipping and subsequent interpolation cannot create a new lateral extremum
except for float rounding. Sidewalk patches and no-car-lane inputs remain unchanged.

The common `Compose` call to conservative `TryReduce` remains compatible. Unknown
crop families, crossing triangles and unrecognized band shapes return false.
`ForBand` is the strict appearance-domain entry point and rejects an invalid native
recipe. Normal Draw03 domain fragments have no `Bands`, so they do not request the
full-layout dynamic repeat page.

## Verification

`WearCheck` source-links the production mapper, domain builder and common clipper.
Its .NET mode verifies seven prepared keys, 7,196 float32 algebra samples, 84
conservative reductions, 3,780 projected samples and 24 rejected crossings. Maximum
projected error is 0.0028076171875 source pixels.

`DomainNativeCheck.Editor.cs` loads the netstandard checker assembly inside the
independent Unity project, exercising the real native matrix validation and the
unchanged production clipper. Eighteen scenarios combine 2/3/6-lane layouts,
20/40/80-metre spans and reflected/translated source frames. The completed check
reported 1,074 fragments and 22,449 interior samples, with maximum error
0.00048828125 source pixels. Its 840 inclusive/both-side boundary phase probes
had maximum error 0.00024602280518593034 source pixels. All domain materials matched
existing opaque/cutout prepared keys. The fragment bound depends on real appearance
domains, not the number of texture repetitions.

Checks cover total area, source provenance, original UV/color interpolation and
physical bone ownership. Five float-rounding slivers below 0.000001 square metres
were excluded from bone and sampling assertions: the common clipper can form a
nearly degenerate coplanar fragment, whose repeated physical assignment follows
its existing positive-side tie rule. This is recorded, not reported as an exact
all-fragment bone proof. No common clipping code was changed.

Evidence: `artifacts/prepared-materials/Draw03/wear-domain-check.txt` and `.log`
(Unity exit code 0), plus `wear-integration-check.json`. No texture was rebaked.
The native checker uses the formal source-linked `Draw03RoadWearDomains` with its
13 values loaded from `Types_Draw03_Draw03RoadWearDomains_cs.bin` through the normal
embedded-array reader. The existing tools resource glob includes this file without
a separate loading path. `wear-domain-inputs.json` identifies the tested production
source, BBN payload and checker assembly by SHA256; its results match those above.
The reference calculation in the latter report demonstrates why an unpartitioned
two-lane page would grow to 296x2420, 296x4840 and 296x9680; it is not a claim that
the newly partitioned path still emits those pages.

## Remaining acceptance

The production entry change passes the full mod build, 571-resource bit-exact and
lazy-loading checks, all registered generator routes and localization checks. The
installed DLL and PDB match the build; all 878 prepared payload/CID files match
the build paths and bytes. Build and deployment receipts are
`artifacts/draw03-wear-domain-build.log` and `artifacts/draw03-wear-domain-deployment.json`.
The installed DLL SHA-256 is
`5ED6E83F12D71424C221DBD3679A10F88F31E35B9973CA695EDF303761EECFBD`.
The generated-road integration run remains unverified. Synthetic native clipping checks are not game
visual acceptance. Verify selected-road masks, mirrored leaves, motion, near/far
LODs and independent persistence on generated assets. Genuinely mixed native
surface masks still use the common composition path; this work does not establish
that every such composite is immutable or exempt from the material contract.
The common composer documents that actual appearance partitions occur upstream;
it does not introduce texture-period subdivision.

All added runtime failures are internal diagnostics. No player-facing strings,
localization keys, generated display names or translations were changed.
