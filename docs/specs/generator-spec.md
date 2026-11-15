# Generator Spec

Required companion: [Generator Contract](../contracts/generator-contract.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](../contracts/reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="generator-shared-structure-coordinate-execution"></a>

## Shared structure coordinate execution

All styles use the general contract's [common data-driven SIMD pipeline](generation-spec.md#generation-cpu-affine-geometry-execution).
Style-specific measured binary parameters, memberships and topology recipes remain data.
The shared executor owns operation dispatch, packed arithmetic, explicit rounding stages,
ordered writes and output publication. Offline producers encode interleaved XYZ stages
and activity masks; the executor must not recognize arithmetic expressions or gather
coordinates into axis columns. Native mesh construction consumes the owned packed
coordinate target directly. Do not replace scalar style loops with a growing
set of per-style SIMD wrappers or retain alternate style-owned coordinate executors.
Offline producers lower their measurements to the shared program representation; native
geometry/material realization retains its existing ownership and thread boundaries.

