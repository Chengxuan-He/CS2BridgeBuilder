# Actual Retile region classification

Build with `dotnet build tools/RetileRegionCheck -p:GameManagedDir="<game>/Cities2_Data/Managed"`.
The default output is `tools/RetileRegionCheck/bin/Debug/net8.0/RetileRegionCheck.dll`.

Run that DLL with these arguments:

```text
<runtime DLL> <game Managed directory> <style.units.ndjson.gz> <source-map.json> <report.json>
```

The source map is a JSON array of `{style, render, mesh, sourceDigest}` records.
`sourceDigest` is the exact source Surface SHA-256 used by production region dispatch.
Optional provenance fields such as `sourceCid` are retained in the supplied map but
do not replace the digest. Missing or duplicate exact keys fail. The style owner must
derive the map from its verified original render/material graph.

The adapter restores Float32 vector keys with `GetSingle`, double coordinates with
`GetDouble`, null-versus-empty exact dictionaries, exact point arrays and original
chart loops. It calls the selected DLL's real `CanUseSource`, `Regions` and
`RegionMapping.Create`, including the production first-match order. Baseline direct
eligibility must reproduce the capture exactly. This checks the observed generation
state; changing the frame implementation requires a fresh observer capture.

`--current-charts` substitutes the selected DLL's current charts by original domain.
`--chart-overrides <file.json>` substitutes only explicitly listed domains. The override
is an array of `{style, render, mesh, originalDomain, charts}`; `charts` uses the capture
shape: chart array, each chart containing loops, each loop containing double `[u,v]`
points. Duplicate exact domain keys fail. Do not join different physical occurrences
by equal chart-boundary keys. Both options may be combined. The report hashes the
override and counts used/unused records and changed direct results. Without either
option, any direct parity change fails; with overrides, unaffected rows retain that
strict check. An override is a diagnostic candidate, not a production integration or
proof that its loops match native source coverage.

The report contains original/repeat/remaining counts, source dispatch/coverage/mapping
failure categories, direct rejection reasons, binding usage and per-scenario totals.
Direct rejection diagnosis calls actual source-UV, chart-triangle and gradient methods;
it does not relax their predicates. Up to twelve representatives per rejection class
are included. `<report>.classifications.ndjson.gz` retains every unit's input line,
scenario, original domain and source-face/fragment lineage, result and binding key.
The original capture remains the complete exact numeric evidence.

`repeatOnlyReasons` diagnoses the retained unconditioned coordinates for RepeatOnly
Units using the same actual predicates. It preserves exact-coordinate keys and
reports missing evidence or collisions explicitly; it does not change eligibility.

This is numerical/source-selection evidence only. It does not establish source-map
provenance, native filtered pixel equivalence, generated-owner bytes or human acceptance.
Baseline cross-checks against independently restored Lift04 and Lift05 results passed,
including zero direct-eligibility parity changes. No player-facing localization changes
are introduced by this offline tool.
