# Bridge generator code organization contract

This is a normative supplement routed by the general bridge generation contract.
It applies to every fixed and movable bridge generator, including future styles.

## Inheritance and responsibilities

- Every bridge generator must inherit `BridgeGeneratorBase`, directly or through an appropriate
  shared base. Movable generators inherit `MovableBridgeGeneratorBase<TSample>`, which itself
  inherits `BridgeGeneratorBase`. The sample type represents disposable style-specific sampling state.
- Each concrete bridge generator is defined in one independent file,
  `Bridges/Types/<Style>/<Style>BridgeGenerator.cs`. Do not split a concrete generator into partial
  files, combine several style generators in one file, or place another style's generator there.
- A concrete generator contains only its bridge's unique logic: stable style identity, measured
  parameters, native source selection, sampling arrangement, motion/part rules and assembly hooks.
  Reusable generation orchestration belongs in the common base, not copied into each generator.
- `BridgeGeneratorBase` owns shared bridge generation behavior. The movable base owns the common
  movable lifecycle: variant/road validation, width delta calculation, factory initialization,
  content and selected-road resolution, sampling lifetime, material lifetime, native LOD input
  ordering, stage timing and successful ownership transfer. Its sealed `Apply` invokes style hooks;
  movable generators must not bypass or duplicate that lifecycle.
- Keep focused reusable algorithms and resource services in `Bridges/Common` and
  `Bridges/Common/Movable`, called by the shared bases and their hooks. This does not require moving
  mesh, GPU, persistence or network services into one oversized base class. Common lifecycle logic
  must still reside in the base rather than in parallel helper pipelines.
- Style-specific immutable data and focused geometry/assembly helpers may remain separate files
  under that style's directory. The single-file rule concerns the concrete generator, not all of
  its measured data and algorithms. Shared bases must not depend on a concrete style or contain
  style-ID switches, that style's dimensions, or assumptions borrowed from one prototype.

## Routing and compatibility

`BridgeGeneratorRouter` remains the explicit stable-ID entry point and returns
`BridgeGeneratorBase`. Do not add a separate movable dispatcher or registration/persistence store.
Existing fixed generators retain their shared base pipeline; movable fixed sections still obey
the general bridge generation contract.

Refactoring must preserve generated names, native references, stage order, LOD inputs, material
ownership, cleanup on failure, preview behavior and persistence. Trace both preview and formal
generation callers and build every registered generator. Do not claim visual or cold-load
acceptance from a successful build. New styles supply their own measured rules; this shared base
does not imply that unimplemented styles are supported.

## Implementation policy

Follow the current bridge generation policy. Retain style-specific hardcoded data derived from real
prototypes and the existing shared lifecycle; do not require players to own identical prototype
files. Compact measured data is sufficient where equivalent to vertex tables. Per-style branches,
complete audits and human feedback are not prerequisites for integrating executable stages.
The policy does not permit empty generators or false completion claims.
