# Four fixed styles integration

Implemented ArtDecoViaduct, TrussViaduct, ArcPedestrian and CoveredPedestrian as four single-deck families. Width aliases share a family, and the pedestrian native/BXP aliases do not duplicate styles. The existing BXP duplicate preference remains unchanged.

The recorded construction path resolves permitted native resource anchors without requiring the authoring network prefab. It derives owned geometry, preserves raw channels and material references, recomputes indexed bounds, and executes measured displacement and frame preparation through the shared background scheduler. Per-style recipes reconstruct native placement and unlock behavior. All twelve UI languages include the new names and viaduct family label.

## Verification

- Official `tools/Build.ps1` passed with four pre-existing nullable warnings in movable road/material code.
- Numerical packing verified 931 resources and 26,747,509 typed values, including exact bits and corruption rejection.
- Localization passed all twelve languages, aliases and stale-preview handling.
- Native structural frame verification passed 108 cases, including 18 fixed-factory comparisons with signed and zero scales.
- Generation scheduling passed 59 cases in isolated Unity Mono.
- Affine execution passed 181 programs, 161 AoS cases and 420,123 coordinate comparisons.
- Recorded variants passed 88 cases; source provenance passed 32 cases.
- Parameter lifetime and structural arithmetic guards passed.

Native frame and scheduling logs are retained in `artifacts/four-new-styles/shared/`. Each style has separate measured geometry evidence under its archive directory.

## Local deployment

The last loaded UI module and enabled subscribed package identify `160320_19` as the active installation. With Cities2 absent, only BridgeBuilder.dll, BridgeBuilder.pdb and BridgeBuilder.mjs were replaced. Original files are preserved under `backups/four-new-styles/installed/160320_19/`; original and replacement hashes are recorded outside backups in `artifacts/four-new-styles/shared/deployment.json`.

Product version remains 26.11.12a. DLL size is 126,562,304 bytes; SHA256 is `8B1048BE0629040D5FD3E5870A1EB7B1F621C08ACB754236401B736FC566EC67`. No package was published and no healthy generated assets were removed.

Game preview, creation, near/far rendering, carrier placement on curves, extreme signed widths and cold loading remain unverified in the game. The game was not started or stopped by this task.
