# Bridge Builder 26.9.30

Changes since the 26.9.29 source release (`cff7f37109f9eb3d3ca7fb6c6d0e82e78fd937b6`).
The double-deck railway seam fix received user in-game acceptance on 2026-09-30.
This record accompanies GitHub branch synchronization; it does not indicate a Paradox Mods upload.

## English

1. Fixed unwanted crossovers at matching, two-edge lower railway seams on double-deck bridges, including the resulting gaps in trusses and railings. Genuine junctions and mismatched track layouts are excluded; network ownership and deck separation are unchanged.
2. Added optional Node Controller support for this repair. It follows Node Controller's replacement geometry pipeline without disabling the mod or overwriting its node settings; the native game pipeline is also supported.
3. Hardened automatic cleanup: wait for catalogue/publication work, revalidate current damage on separate frames, and do not treat an old error message or a failed inspection as permission to delete a bridge. Manual deletion and generation are coordinated with cleanup.
4. Corrected network-deletion timing to run before native topology/render maintenance. Preserve surviving shared junctions and their children, respect secondary lane/object lifetimes and current ownership, and wait for placed references to retire before finalizing asset cleanup.
5. Added an early registration guard for null sections and auxiliary-network references, including overhead/underground sections. Invalid Bridge Builder networks are quarantined before native initialization, preventing the covered NetInitializeSystem null-reference path; recovered networks can be registered again.
6. Added on-disk checks for mismatched bridge UUID identities, duplicate prefab names/CIDs and missing geometry references, including collision losers hidden by native registration. Retired prefab files and sidecars are hash-checked and moved to recoverable inactive storage; live prefab indices and rendering resources are retained.
7. Added four-sample preview supersampling, combined with the existing 2x render resolution. Linear colour and transparency are accumulated together to improve thin cables and silhouettes without dark edge fringes.
8. Reworked preview lighting: directional key and fill lights, a front/top key direction, no cast shadows, fixed exposure and an isolated neutral environment independent of map day/night lighting. City lighting and authored bridge lights are not modified.
9. Removed the preview's mixed Point/ProjectorBox light setup associated with a potential HDRP light-index ordering hazard. Added preview lifecycle/readback diagnostics; this is not a claim that every player-side HDRP IndexOutOfRangeException has been resolved.
10. Isolated prototype mesh reads into private buffers, including LODs, so bridge generation no longer releases shared prototype streaming resources. Preserve surface-slot order and validate total submesh/surface alignment before exporting.
11. Added read-only prototype material audits and bounded railway seam diagnostics covering backend execution, topology, lane connections and repair hits; failures retain native geometry instead of partially applying a repair.
12. Bundled the pinned self-contained Harmony runtime and license in build/install/staging, added cleanup and network-validation regression tooling, and synchronized assembly, UI and draft publishing metadata to 26.9.30.

Back up important saves. Recreate bridges affected by old generated geometry when needed. Damage detection covers the documented structural/identity/reference faults, not every possible malformed mesh. The player's previously reported HDRP exception still requires a fresh matching log to establish its exact cause.
