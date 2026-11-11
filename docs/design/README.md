# Maintained design and reference

Use the [project contracts](../agent-contract/README.md) for requirements. These documents explain shared implementation and source interpretation.

## Generation and geometry

- [Native bridge pricing and prototype unlocks](generation/Bridge-Economy-And-Unlocks.md)
- [Bridge generation workflow design](generation/Bridge-Generation-Workflow.md)
- [How Cities: Skylines II models a bridge](generation/BridgeModel.md)
- [Hardcoded Numerical Resources](generation/Hardcoded-Numerical-Resources.md)
- [Movable network geometry context](generation/Movable-Network-Geometry.md)
- [Road width calculation](generation/road-width-calculation.md)

## Materials

- [Prepared Bridge Materials](materials/Prepared-Materials.md): current source-preserving
  decal workflow, compatibility differences and the authorized generated-program work.
- [Generated material programs](materials/Generated-Material-Programs.md#shader-definition-and-scope):
  custom shader = native shader + bone binding; native pass
  packaging, generation-time compilation, owner dependencies and registration boundaries.
  Implementation is in progress; startup registration requires Bridge Builder.

## Persistence and recovery

- [Bridge deletion lifecycle](../agent-contract/BRIDGE_DELETION.md): authoritative single/bulk deletion design, durable tombstones, in-game unregistration and post-exit physical cleanup. Implementation status must be checked separately.

- [Observed resource loading order](persistence/Resource-Loading-Order.md): historical startup observations, dependency readiness and independent-copy semantics; not a guaranteed global schedule.
- [Permanent bridge identities](persistence/Bridge-Identity-Migration.md)
- [Dependency persistence and self-check implementation](persistence/Dependency-Persistence-Implementation.md)
- [Missing bridge save repair](persistence/missing-bridge-save-repair.md)

## Interface

- [Bridge management and native asset pack membership](interface/Bridge-Management-Pack.md)
- [Runtime UI localization](interface/Runtime-UI-Localization.md)

## Canonical locations and delivery links

Maintained shared designs belong in this directory's domain subdirectories. Normative
requirements belong in `docs/agent-contract/`; link to them rather than maintaining a
second design with conflicting rules. Observed loading order is a persistence reference,
not an engine scheduling guarantee. Audit measurements, version-specific repair reports,
benchmarks and single-style evidence remain research or historical records, not contracts.

The deletion design has one canonical location: `docs/agent-contract/BRIDGE_DELETION.md`.
The loading-order reference explains historical observations and does not duplicate that
policy. Project navigation points directly to these documents; chat output duplicates
and pointer documents are not maintained. Raw logs, images and deployment receipts are
not design documents.
