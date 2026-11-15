# Draw02 producer authored-control regression

## Result

PASS using the updated repository `Prepare-Repeats.ps1` with
`-ReuseTexturesOnly`, the Draw02 private Unity project, a fresh output/product
directory and the successful Draw02 pipeline report plus read-only product store.

The producer processed five units: zero created products, 25 reused products,
zero baked textures and zero borrowed controls. Draw02's authored control
textures therefore remain in use; the absent-control optimization did not
substitute this bridge's ZeroControl.

The updated native verifier passed five Surface descriptors and sixteen unique
native textures, including one equivalent descriptor variant. It uploaded
8,670,272 mip bytes unchanged and checked source-density dimensions. Both Unity
commands exited successfully. No game scene was created.

All 42 output files (Repeat descriptors and referenced Shared textures, including
CID sidecars), totaling 3,757,870 bytes, match the corresponding packaged Draw02
files byte-for-byte by SHA-256. No assets or public source files were changed.

## Evidence

The retained evidence root is
`artifacts/prepared-materials/Draw02/producer-authored-control/`:

- `bake.json`: validated reuse-compatible pipeline identity and producer counters.
- `executed-source-hashes.json`: actual current source snapshot hashes used by Unity;
  the compatible identity in `bake.json` intentionally retains old product keys.
- `bake.log`: successful producer execution.
- `native.log`: successful updated native reader verification.
- `packaged-comparison.json`: every relative path, size and both SHA-256 values.
- `output/Draw02/`: the independently generated graph, identical to packaged files.

The successful prior pipeline report was
`artifacts/agent-material-env/Draw02/reports/bake.json`; the read-only product
store was `artifacts/agent-material-env/Draw02/products`. Both commands used
`artifacts/agent-material-env/Draw02/unity` exclusively. Runtime text and
localization behavior are unchanged.
