# Road Builder card icon fallback

## Evidence and cause

The player's screenshot shows Road Builder road cards using the shared fallback bridge icon while native tracks and bridge styles still show their own icons. The current BridgeBuilder.log reports `DTD is prohibited` for each `coui://roadbuilderthumbnails/*.svg` dimension read at 00:28:20. URI resolution had reached XML parsing; this is not evidence of a broken host map.

`BridgeIconDimensions` used `DtdProcessing.Prohibit`. Road Builder SVGs declare the standard SVG 1.1 PUBLIC DOCTYPE. The parser therefore failed before reaching width/height or viewBox. `BridgeCardIcon.Ratio` returned unresolved zero, and the shared card intentionally displayed the known-ratio fallback. The fixed-height layout did not itself stretch or replace those files.

A retained local Road Builder export reproduces the exact exception in the production parser: `ModsData/RoadPrefabExporter/Icons/fd2ef5c7918e0962cdfb815d2f324063.svg`. It has an XML declaration, SVG 1.1 DOCTYPE, percentage width/height and a square `0 0 219.502 219.502` viewBox. Its SHA256 is `A8E52190B951D891FBF5D01D9797E82A6A768A444D4188D871D48FFB628A4645`. A byte-identical evidence copy and before/after test output are in `artifacts/roadbuilder-icons/`. This retained exported file is not claimed to be the temporary thumbnail from the screenshot.

## Change and verification

The production change switches only the SVG dimension reader from prohibiting DTD declarations to ignoring them, retaining `XmlResolver = null`. No DTD download or declared-entity expansion is needed to read canvas dimensions. Source URIs, SVG bytes, host resolution, exporter behavior, saved assets and the 62rem explicit ratio-based layout remain unchanged.

Fourteen dimension cases plus malformed-frame handling pass, including the real file, BOM plus DOCTYPE, landscape and portrait SVGs, an unavailable external DTD and non-expansion of internal entities. Card tests cover explicit widths, fallback, source changes and absence of image-load/natural-size dependencies. Their obsolete Infinity expectation was removed to match trusted internal data policy. Runtime UI and twelve-language C# checks pass; no new visible strings were introduced. Production Build.ps1 and whitespace checks pass.

With Cities2 absent, the build was deployed to `160320_19`, corroborated by the current native Modding.log registration path. Original installed files are in `backups/roadbuilder-icons/installed/160320_19`; hashes and unchanged version are recorded in `artifacts/roadbuilder-icons/deployment.json`. No game was launched or stopped, and no bridge/save was modified. Actual Cohtml presentation after the player's next launch remains a manual acceptance check.
