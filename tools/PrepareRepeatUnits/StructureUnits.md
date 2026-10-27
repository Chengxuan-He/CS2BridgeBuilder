# Structural semantic-unit inventory

Produce one complete grouping of each style's existing original domain inventory, then one batch
of evidenced units. Do not run an open-ended sequence of per-face texture probes. Existing sampler
keys and changed receiver plans may be read to establish coverage; they must not become unit IDs.

Run the bookkeeping check with:

```text
python tools/PrepareRepeatUnits/AuditStructureUnits.py <manifest.json> <audit.json>
```

The input manifest has this exact structure (hash placeholders must be replaced with real SHA256):

```json
{
  "schema": "BridgeBuilder/StructureUnits/1",
  "style": "Lift04",
  "inventory": {"path": "original-domains.json", "sha256": "<sha256>"},
  "units": [{
    "id": "authored-steel-pattern",
    "sourceDigest": "<original Surface sha256>",
    "semanticPattern": "Measured original steel grain, shared by the identified beam family",
    "bounds": [0.5107421875, 0.5068359375, 0.52197265625, 0.60546875],
    "physicalPeriod": [0.3348388671875, 3.256129356413507],
    "evidence": {"path": "pattern-evidence.json", "sha256": "<sha256>"},
    "mipEvidence": {"path": "channel-mip-boundaries.json", "sha256": "<sha256>"}
  }],
  "groups": [{
    "id": "original-crossbeam-steel",
    "kind": "REPEAT_BASE",
    "sourceDigest": "<original Surface sha256>",
    "unit": "authored-steel-pattern",
    "domains": ["LiftBridge04 Mesh|0|2"],
    "evidence": {"path": "part-membership.json", "sha256": "<sha256>"}
  }]
}
```

The numerical example illustrates the interface; use the bridge's authoritative measured physical
frame, not the example's decimal approximation. `sourceDigest` is the original serialized Surface
digest used by production dispatch. Unit bounds identify the pattern independently of receiver
triangles, widths, LODs and sampler variants. Unit evidence must identify the real part/material
pattern, its authoritative source, full channels and complete mip boundaries. Small stochastic
normal differences need quantified comparison with original detail; exact byte periodicity is
not required. A rectangle, arbitrary crop or visually similar but different material is insufficient.

`original-domains.json` contains:

```json
{
  "schema": "BridgeBuilder/StructureDomains/1",
  "style": "Lift04",
  "domains": [{
    "id": "LiftBridge04 Mesh|0|2",
    "sourceDigest": "<original Surface sha256>",
    "receivers": 2
  }]
}
```

Enumerate every original domain of the declared production inventory, across all LODs and material
branches, exactly once. If the inventory is only a finite set of widths, identify that limit in its
evidence; it cannot prove every future width. `receivers` counts the actual sampled receivers in
that inventory and is separate from domain count.

The four group kinds are:

- `ORIGINAL`: full original material reuse is valid; record the verified native mapping in `reason`.
- `REPEAT_BASE`: an evidenced repeating base material, such as the original steel grain.
- `REPEAT_DETAIL`: an evidenced authored repeated detail, with its real period and full channel data.
- `NONREPEATING`: a specific remaining nonperiodic part or mixed island; record the concrete conflict
  and smallest necessary implementation in `reason`, rather than repeatedly testing its faces.

Every group has source digest, evidence and original domain membership. The two repeat kinds
reference a unit. Original and nonrepeating kinds require `reason`. Every proposed unit must be
used. The audit verifies hashes, complete non-overlapping inventory assignment and these fields;
it does not certify the semantic evidence or claim complete runtime generation.

Prepare accepted units through `Prepare-RepeatUnits.ps1` and its existing `RepeatUnits/2` input.
Descriptor variants share the same pixels. Do not bake one unit per group when groups use the same
original pattern. Record original-geometry frame/density evidence separately from sampling aliases.

Production coverage supports both measured rectangles and nonrectangular original charts:

```csharp
new Region(bindingKey, minU, minV, maxU, maxV,
    coverageMinU, coverageMinV, coverageMaxU, coverageMaxV);
new Region(bindingKey, minU, minV, maxU, maxV, measuredOriginalChart);
```

The second overload uses `MovableUvChart` with full triangle containment, including holes, rather
than its bounding box. A measured chart selects original same-material domains; it does not itself
establish a periodic unit. Reuse existing measured chart data and genuine-unit bounds. Both forms
preserve continuous density-corrected UVs and old phase recovery; neither splits geometry nor
creates runtime textures. Return these entries from the bridge's `Regions` partial only for the
verified original source digest. Keep unrelated original atlas domains out of the coverage.
