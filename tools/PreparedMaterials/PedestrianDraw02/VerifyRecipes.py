"""Verify emitted repeat rectangles against actual measured native triangles."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np

from ExportRecipes import STYLE, key


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("research", "inventory", "report"):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    inventory = json.loads(args.inventory.read_text(encoding="utf-8-sig"))
    mesh = np.load(args.research / "pedestrian02-main-0.npz")
    uv = np.load(args.research / "pedestrian02-lod0-uv-evidence.npz")["uv"].astype(np.float64)
    faces, vertices = mesh["indices"], mesh["positions"].astype(np.float64)
    active, inactive = inventory["units"], inventory["evidence"]["inactivePatches"]
    assert len(active) == len(inactive) == 1
    assert active[0]["purpose"] == "Fixed" and inactive[0]["purpose"] == "Moving"
    checked = []
    for unit, pair in ((active[0], (6708, 6709)), (inactive[0], (8556, 8557))):
        assert unit["style"] == STYLE and unit["cutout"] and not unit["rows"] and unit["bands"] is None
        assert unit["key"] == key(unit["sourceDigest"], unit["min"], unit["max"])
        triangles = faces[list(pair)]
        points, source_uv = vertices[triangles], uv[triangles]
        minimum, maximum = source_uv.reshape(-1, 2).min(axis=0), source_uv.reshape(-1, 2).max(axis=0)
        assert np.array_equal(minimum, unit["min"]) and np.array_equal(maximum, unit["max"])
        span = np.ptp(points.reshape(-1, 3), axis=0)[[0, 2]]
        area = np.linalg.norm(np.cross(points[:, 1] - points[:, 0], points[:, 2] - points[:, 0]), axis=1).sum() / 2
        assert abs(area - np.prod(span)) < 1e-10
        checked.append(dict(purpose=unit["purpose"], selectedForBake=unit is active[0], faces=pair,
                            physicalUnitMetres=span.tolist(), atlasUvPerMetre=((maximum-minimum)/span).tolist()))
    report = dict(style=STYLE, activeRepeatRecipes=1, inactiveMeasuredRecipes=1, nativeFacesVerified=4,
                  inventorySHA256=hashlib.sha256(args.inventory.read_bytes()).hexdigest(), regions=checked,
                  limits="Source UV rectangles only; native baked payloads and in-game appearance require separate verification.")
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("PASS PedestrianDraw02: 4 actual native faces, exact source UV rectangles, 1 active repeat recipe and 1 inactive measured patch.")


if __name__ == "__main__":
    main()
