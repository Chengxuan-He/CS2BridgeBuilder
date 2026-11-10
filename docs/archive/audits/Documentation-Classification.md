# Documentation classification inventory

This is a historical documentation-organization inventory, not a project contract or a new design. Counts describe the inventory before this record was moved into the archive. The review covered every Markdown deliverable recursively under this chat output directory, every Markdown path under project `docs`, root Markdown files, and Markdown files directly in `work`. Large artifact trees, raw logs, images and binary receipts were not recursively inventoried. Existing domain and archive placement was retained.

## Design documents

| Delivery file | Classification | Canonical project location |
| --- | --- | --- |
| `bridge-deletion-design.md` | Authoritative deletion design former delivery copy | `docs/agent-contract/BRIDGE_DELETION.md` |
| `resource-loading-order.md` | Observed loading-order reference former delivery copy | `docs/design/persistence/Resource-Loading-Order.md` |

Neither contains unique design content requiring another project document. The redundant output copies and their interim pointers were removed after verifying the complete canonical project documents.

## Other delivery documents

| File | Classification and disposition |
| --- | --- |
| `BackendComparison.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `BridgeTextureReport92.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `material-documentation-conclusion.md` | Documentation or workspace maintenance delivery report; not a design |
| `migration-e-repair-verification.md` | Documentation or workspace maintenance delivery report; not a design |
| `native127-deck-diagnosis.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native129-material-conversion.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native130-compatible-materials.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native131-source-validation-audit.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native132-assembly-timing.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native133-elevated-approach.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native135-road-lod-gpu-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native136-double-gpu-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native137-native-layer-capability-audit.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native137-native-layer-capability-summary.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native137-uv-coordinate-only-review.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native137-uv-density-investigation.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native138-dynamic-shader-audit.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native138-dynamic-shader-results.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native139-source-decals-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native140-moving-wear-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native141-mechanism-end-wear-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native142-marking-coverage-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native143-marking-preview-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native144-marking-minification-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native145-deletion-investigation.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native145-rigid-native-decals-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `native146-deletion-audit-and-preview-repair.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `native147-shared-resource-deletion-report.md` | Build-specific implementation / validation / deployment report; not an independent maintained design |
| `PrototypeTextureReport93.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `road-section-lod-audit.md` | Audit / investigation / measurement; retain as evidence, not normative design |
| `source-marking-alpha-and-duplicate-investigation.md` | Audit / investigation / measurement; retain as evidence, not normative design |

## Project inventory

- 257 Markdown documents under `docs`, classified by the existing directories below.
- Root Markdown: `AGENTS.md` and `README.md`; both retain their instruction/navigation role.
- No Markdown files directly in `work`; no loose root-level design was found.
- No new audit or single-style reference was introduced into any contract.

| Area | Markdown files |
| --- | --- |
| `README.md` | 1 |
| `agent-contract` | 10 |
| `archive` | 209 |
| `design` | 14 |
| `guides` | 3 |
| `publishing` | 20 |

## Changes and limits

Updated the canonical deletion design with external-helper startup, process identity/handle waiting, shutdown-hook limits, singleton coordination and explicit implementation choices. The immediate-restart exclusion mechanism is still a required unverified implementation safeguard; a one-time process check is insufficient. No directory relocation was authorized. Updated the design index. The interim output pointers were subsequently removed; the canonical project documents remain the only design texts. This inventory was moved into the documentation audit archive. No production code, tests, build or deployment changed.

Existing self-check deletion wording in root `AGENTS.md`, runtime `AGENTS.md` and the generation contract remains a separately identified synchronization item; this focused organization task does not claim that those older rules or runtime behavior have already been migrated.
