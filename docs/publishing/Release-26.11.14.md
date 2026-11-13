# Bridge Builder 26.11.14

This release adds Art Deco Viaduct, Truss Viaduct, Arc Pedestrian Bridge and Covered Pedestrian Bridge styles, grouping width variants by their structural design. GO 224m Viaduct Bridge remains deferred; saved-owner compatibility data is retained.

- Rebuild shader containers for the actual compiled bytecode length, including changed serialized program sizes and nested offsets.
- Correct preview composition packing, selected-road markings, adjacent span supports and native elevated-width pillar selection.
- Preserve card icon aspect ratios and load Road Builder SVG icons containing a document type declaration.
- Preserve migration metadata and native unlock behavior, and display complete published source-mod names.
- Remove obsolete internal validation and unused execution remnants while retaining external IO validation and native operation failures.

Public version: 26.11.14. CLR assembly/file version: 26.11.14.0.

Automated verification and official publication evidence are recorded below when completed. Recent preview changes have not yet received a post-update in-game capture; automated checks do not establish visual, motion or cold-load acceptance across every configuration.

Production build and official postprocessing passed. Automated checks covered both generated shader programs with real D3DCompile, shorter/longer/mixed replacements and repeated rebuilds; 71 native placement/packing cases; 28 measured pillar candidates; twelve-language UI/native localization; icon dimensions; publication/unlock behavior; metadata-preserving migration and self-check lifecycle. The obsolete validation test harness now exercises retained serialized dependency recovery. Dead-code cleanup removed unused preview and network helpers, historical spacing constants, the unreachable recovery loop and unused embedded standalone HLSL; complete program sources remain in the BBS1 packages.

The official SDK content ZIP contains 965 files, measures 77,894,042 bytes and has SHA-256 `D82FC496ADB119FEE8B996C391858F09A4DFA14E83FFAF3C622DFA65010F4BBA`. This is the local publication candidate until the remote revision and downloaded content are verified. Its assembly contains no GO Viaduct resources or generator. Existing saved bridge assets are unchanged. Backups and authoring intermediates are excluded from source publication; historically tracked `work/` files are removed from tracking while retained locally.
