# PedestrianDraw02 source texture and sampler mapping

## Scope and reproducible inventory

`PrepareSourceInputs.py` extracts complete byte-identical native inputs from the
original archives, independent of removed generated materials. Run:

```powershell
python tools/PreparedMaterials/PedestrianDraw02/PrepareSourceInputs.py . GAME_DATA artifacts/agent-material-env/PedestrianDraw02/source-input
```

The output is an authoring manifest, not the public runtime/preparation schema.
It covers seven renders, nine mesh streams and five Surfaces: 13 native files,
42,758,491 bytes, including eight physical pixel payloads. The main mechanism's
second Surface uses template 2 and four ordinary Texture resources, including its
interior atlas. The structural sources below use template 0. Original MidMips cache
locations/CIDs are retained as shared read-only inputs; per-source cache membership
and native channel interpretation remain public-validator requirements. Missing
original authoring Texture bindings in VT descriptors are explicitly reported,
not fabricated. Existing archive bytes are never resampled or re-encoded.

The structural input is `artifacts/uv-density/structure-tessellated-sources.json`,
SHA256 `dc72be70e2058c6fa79f8c459ae3c8fe3b2215110edd8b04decc559f58fad92b`:
2,231 structure sampling records plus one reachable fixed road repeat.
`ExportSourceSampling.py` is an obsolete-library audit tool and requires removed
per-sampler assets. Do not invoke it to rebuild those assets. Its retained source
identity evidence remains separate from the new preparation inputs.

## Measured source sharing

| Native Surface CID | Structural samplers | Native source |
| --- | ---: | --- |
| `a1b3ca16b946cc34fc031e376ba03d71` | 1,785 | Main mechanism, 4096 脳 4096 |
| `5a4524b9e015bf4860c6e66e37e08bb7` | 364 | Pillar, same descriptor bytes as main mechanism |
| `a8a24e8457ee76f1b026fc7679670937` | 32 | Pillar base, same descriptor bytes as main mechanism |
| `4daae9cd460c780875604a3e3e127e16` | 50 | Pillar LOD2, 512 脳 512 |

There are 1,852 triangular and 379 rectangular structural sampling records plus
one fixed road repeat. The three high-detail Surface descriptors have identical
SHA256 `5c3a1f839c0bca829fcb1fd4f923697f43b59cb464a494a2c64562d2f7b92219`.
Their existing CIDs and distinct sampler parameters are retained in the report.
They reference these same original 13-mip VT textures:

| Channel | Native VTTexture CID | Serialized bytes |
| --- | --- | ---: |
| Base color | `42620b429f731b291c062f7c43f92d82` | 9,698,478 |
| Normal | `428ef0a22449e912f24d3fd15b38fe21` | 15,212,606 |
| Mask | `a88118c6c3173d437a9fc1ed4ac6b8cf` | 8,979,612 |

The LOD2 source uses three layers of VTSurface
`2ef5ede53d04e07bd39508c629484d91` (389,806 bytes). Its three channels remain
distinct; counting one container does not assert that its channels are one texture.
The complete grouping is six channel images in four native payload files totaling
34,280,502 bytes. These are existing built-in references, not new packaged copies.
Native payload SHA256 values and source archive entries are recorded without
resampling, dropping mips, altering normal encoding or changing color space.

The old generated per-sampler material library has been removed under the user's
explicit instruction. Do not restore or rebuild it. Native input byte counts are
not SDK package sizes: compatible built-in references need no distributed pixel
copies. The complete ten-style SDK ZIP must remain at or below 55,000,000 bytes.

## Public integration requirements and verification limits

The legacy phase/region counts below describe their recorded diagnostic, not the
current production Retile path. `MovablePlanUvCheck` constructs parent Units
without Charts and rebuilds chart Units without ExactCoordinates, ExactPoints,
ExactSourceUvs or original domain provenance. Its source-reuse and region totals
must be replaced by the common production observer before selecting new units or
claiming current structural coverage. The historical source inventory remains
valid provenance; it is not a current production failure count.

### Whole-unit source-period implementation

`PedestrianDraw02NativeSampling.TrySourcePeriod` evaluates the final `Unit.Points`
coordinates after `MovableParentUv.Select`. It does not restore that method's
earlier `OffsetX/OffsetY`, apply modulo at individual vertices, or repeat the phase
conditioning. An already conditioned unit may occupy period zero. The whole unit
must fit one integer period and the unreflected triangle half where applicable;
the existing two-axis Float32 gradient check is then applied in absolute atlas UV.
This helper is an offline experiment under this tools directory. It is excluded
from the production source tree and has no runtime integration patch.

`RunPhaseCoverage.py` compiles a private copy of the production-plan audit and
links the actual selector and production UV implementations. With
`artifacts/uv-density/layout-plans.bin` and the current compiled mod, it evaluates
9 PedestrianDraw02 mesh streams at six width deltas (-1.25, -0.25, 0, 2, 8, 24).
The 54 cases contain 66,359 changed chart instances and 11,969 instances already
eligible for original-material reuse. **The proposed integer-phase extension
adds zero eligible instances.** This is not a resource reduction and is not a
reason to integrate the helper. The arithmetic tests separately exercise
18 checks, including negative phases, reflected/cross-period rejection and
already-conditioned period-zero coordinates. These are not native mip tests.

### Exact native chart input

`ExtractNativeCharts.py` reads the same actual source fixture and the recorded
source Surface bindings. It produces `native-charts.npz` and `native-charts.json`
under `artifacts/prepared-materials/PedestrianDraw02/source-consolidation/`.
The data covers all 9 source mesh streams, 51,639 original faces and 5,686 connected
charts. It retains exact source positions, UVs, triangle indices, per-face chart
ownership and boundary edges. Only complete shared edges with identical position
and UV endpoints are connected; UV seams and nonmanifold boundaries remain
explicit. No vertices or faces are cut, removed, or inserted.

Each chart points to its native Surface and shared channel graph. Its bounds are
an index only, not a declaration that the entire rectangle contains matching
texture content or is repeatable. Public integration can use these source charts
to validate complete mapped receiver coverage before native binding, keeping
unsupported cross-region receivers explicit. The old per-face fold recipe is not
treated as proof of the prototype's intended pattern. These data do not themselves
change live rendering or justify deleting prepared assets.

### Embedded chart adapter

`GenerateUvCharts.py` converts the reliable source boundaries into the formal
`PedestrianDraw02UvCharts.For(renderName, meshIndex, domain)` adapter. The domain
argument is the original `MovableUvDomains.Items` index, not a newly numbered
chart or a material key. Only charts containing that domain's recorded original
faces are candidates. If any required chart has unreliable boundaries, that
domain returns an empty candidate set and retains the existing generation path.

The generated `assets/HardcodedNumbers/PedestrianDraw02/PedestrianDraw02UvCharts.bin`
contains 116,406 Int32 and 94,180 Float64 values in checksum-validated BBN1 format,
1,219,128 bytes total. It holds 5,614 shared charts, 5,684 closed loops and 47,090
points. Outer rings and holes use even/odd containment; the repeated closing point
is omitted. All 5,614 chart records are referenced. No pixel payload is added.
The adapter lazily loads these values and shares each `MovableUvChart` instance.

The 9 mesh streams retain 47,983 original domain indices; 45,806 have candidate
charts. Rejected charts comprise 13 degenerate/folded UV cases, 10 folded adjacent
boundaries, 37 intersecting/touching loops, and 11 absent/nonmanifold boundaries.
Validation requires consistent interior edge orientation, simple non-touching
closed rings, matching original UV triangle area, and source-face centroids inside
the even/odd region. The generated arrays pass exact BBN numeric round-trip and
complete-reference checks. This verifies authoring data, not native rendering.

Public integration must call this adapter and accept a source reference only when
the complete mapped receiver lies inside the candidate chart. It must preserve
native Surface/texture/mip bindings and normal behavior. Do not infer rectangle
repeatability, integrate the zero-benefit phase helper, or infer native acceptance from
these coverage counts alone. Full game and SDK-package acceptance remain pending.

The report exposes `sourceChannelImages` separately from `samplers`. Each image
lists all source Surface CIDs and sampler keys that consume it. A sampler retains
its original bounds, triangle coordinates, repeat parameters and current prepared
texture bindings. The exporter verifies every listed prepared texture reference
resolves inside this style. Source identity is established from authoritative
native payload bytes, layer, channel meaning and format, not output names.

The shared implementation must express local rectangle repetition and triangular
fold sampling against these shared source texels through supported native rendering.
Ordinary affine UV transformation alone is not sufficient; the main agent owns
that common capability investigation and runtime/baker/loader changes. No
per-sampler bake is introduced as a workaround. Old generated libraries must not be restored as a fallback.

This change implements the style-specific inventory and validation tool only.
It does not claim native visual equivalence, an installed runtime migration,
source-density acceptance across mips, or completion of the total package limit.
Previously completed zero-control tests were not rerun. No Unity process, game
operation, deployment or Git publication was started. Geometry, bones, LODs,
owner-local persistence and localization remain unchanged; the only added text is
English authoring documentation and command-line diagnostics.
