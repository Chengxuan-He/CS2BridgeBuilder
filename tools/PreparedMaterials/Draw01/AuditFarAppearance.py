"""Measure finite Draw01 far-atlas mapping; never infer repeatability from a crop."""
import hashlib
import json
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "artifacts/material-remap/Draw01"

# Offline reviewed source top-face groups from the native far mesh. These are
# evidence groups, not runtime prototype-face intersections or construction cuts.
GROUPS = {
    "positive-left-main": list(range(16, 23)),
    "positive-right-main": list(range(25, 32)),
    "negative-left-main": list(range(344, 351)),
    "negative-right-main": list(range(353, 360)),
    "positive-left-terminal": [63, 64],
    "positive-right-terminal": [226, 227],
    "negative-left-terminal": [391, 392],
    "negative-right-terminal": [550, 551],
    "positive-left-fixed-sidewalk": [108, 109, 110],
    "positive-right-fixed-sidewalk": [278, 279, 280],
    "negative-left-fixed-sidewalk-chart-a": [436, 437],
    "negative-left-fixed-sidewalk-chart-b": [445],
    "negative-right-fixed-sidewalk": [602, 603, 604],
    "positive-left-fixed-carriageway": [114, 115],
    "positive-right-fixed-carriageway": [274, 275],
    "negative-left-fixed-carriageway": [440, 441],
    "negative-right-fixed-carriageway": [598, 599],
}


def main():
    source = ROOT / "artifacts/uv-density/native/DrawBridge01_LOD2 Mesh.npz"
    expected = "0cee92b3b3503ab5a403e755940bcf39ab2dbe41e4d843bd34bf3b7a8a3025c0"
    assert hashlib.sha256(source.read_bytes()).hexdigest() == expected
    data = np.load(source)
    positions, uv, faces = (data["mesh0_" + key] for key in ("positions", "uv0", "faces"))
    rows = []
    for name, ids in GROUPS.items():
        indices = np.unique(faces[ids])
        p, t = positions[indices].astype(float), uv[indices].astype(float)
        design = np.column_stack((p[:, 0], p[:, 2], np.ones(len(p))))
        fit, _, rank, _ = np.linalg.lstsq(design, t, rcond=None)
        assert rank == 3
        residual = (design @ fit - t) * 512
        rows.append(dict(group=name, sourceFaces=ids, positions=p.tolist(), uv=t.tolist(),
                         affineXz1ToUv=fit.T.tolist(),
                         maximumSourceTexelErrorByAxis=np.max(np.abs(residual), axis=0).tolist(),
                         uvBounds=[t.min(axis=0).tolist(), t.max(axis=0).tolist()],
                         sourceBounds=[p.min(axis=0).tolist(), p.max(axis=0).tolist()],
                         mergedCarriagewayAndSidewalk=name.endswith("main")))
    assert len({i for ids in GROUPS.values() for i in ids}) == 56
    result = dict(surface="7271f977513970fff2c709a721970e75", sourceSha256=expected,
                  dimensions=[512, 512], groups=rows, approvedRuntimeMapping=False,
                  limitations=["Least-squares fits are measurements, not approved mapping tolerances.",
                               "The 14 near appearance domains do not encode every far chart and finite seam.",
                               "Main proxies merge carriageway and sidewalk; appearance type requires separate source semantics.",
                               "No all-mip filtering or generated-output equivalence is established."])
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "far-appearance.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps([dict(group=r["group"], error=r["maximumSourceTexelErrorByAxis"]) for r in rows], indent=2))


if __name__ == "__main__":
    main()
