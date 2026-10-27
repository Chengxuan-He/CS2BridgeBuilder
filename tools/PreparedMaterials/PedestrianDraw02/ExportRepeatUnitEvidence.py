"""Record measured prototype units without promoting unverified crops to repeat units.

The output is review evidence, not an executable bake manifest. Only a native
boundary/full-mip check can approve its candidate for the shared RepeatUnits tool.
"""
import argparse
import json
from pathlib import Path
import struct

import numpy as np

from ExportSourceSampling import INVENTORY_SHA, STYLE, digest


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("repository", type=Path)
    p.add_argument("source_inputs", type=Path)
    p.add_argument("output", type=Path)
    args = p.parse_args()
    root = args.repository.resolve()
    repeat_path = root / "artifacts/prepared-materials/PedestrianDraw02/repeat-inventory.json"
    repeat = json.loads(repeat_path.read_text(encoding="utf-8-sig"))
    assert len(repeat["units"]) == 1
    unit = repeat["units"][0]
    inventory_path = root / "artifacts/uv-density/structure-tessellated-sources.json"
    assert digest(inventory_path.read_bytes()) == INVENTORY_SHA
    inventory = json.loads(inventory_path.read_text(encoding="utf-8-sig"))
    structures = [u for u in inventory["units"] if u["style"] == STYLE]
    assert len(structures) == 2231
    source_inputs = json.loads(args.source_inputs.read_text())
    source = next(s for s in source_inputs["sources"] if s["cid"] == unit["sourceCid"])
    assert source["sha256"] == unit["sourceDigest"]
    assets = {a["cid"]: a for a in source_inputs["assets"]}
    for a in assets.values():
        assert digest(Path(a["file"]).read_bytes()) == a["sha256"]
    charts_path = root / "artifacts/prepared-materials/PedestrianDraw02/source-consolidation/native-charts.npz"
    charts = np.load(charts_path)
    faces = charts["mesh0_faces"][[6708, 6709]]
    positions = charts["mesh0_positions"][faces].astype(np.float64)
    uv = charts["mesh0_uv"][faces].astype(np.float64)
    low, high = np.array(unit["min"]), np.array(unit["max"])
    assert np.array_equal(uv.reshape(-1, 2).min(0), low)
    assert np.array_equal(uv.reshape(-1, 2).max(0), high)
    physical_low = positions.reshape(-1, 3).min(0)
    physical_high = positions.reshape(-1, 3).max(0)
    size = (physical_high - physical_low)[[0, 2]]
    assert np.all(positions[:, :, 1] == 10)
    expected = low + (positions[:, :, [0, 2]] - physical_low[[0, 2]]) / size * (high - low)
    assert np.max(abs(expected-uv)) < 1e-12
    area = np.linalg.norm(np.cross(positions[:, 1]-positions[:, 0], positions[:, 2]-positions[:, 0]), axis=1).sum()/2
    assert abs(area-np.prod(size)) < 1e-10
    candidate_id = "PedestrianDraw02.FixedPrototypeRoad"
    mappings = [{"family": "Repeat", "key": unit["key"], "candidate": candidate_id,
                 "status": "exact-measured-prototype-region-awaits-tileability"}]
    for u in structures:
        b = u["bounds"]
        contained = (u["sourceCid"] == unit["sourceCid"] and b[0] >= low[0] and b[1] >= low[1]
                     and b[2] <= high[0] and b[3] <= high[1])
        mappings.append({"family": "Structure", "key": u["key"], "candidate": candidate_id if contained else None,
                         "status": "region-contained-not-repeat-equivalence" if contained else "no-evidenced-unit"})
    channels = []
    for backing in source["vtTextureCids"]:
        asset = assets[backing["cid"]]
        raw = Path(asset["file"]).read_bytes()
        assert raw[29] == 13 and struct.unpack_from("<2H", raw, 23) == (4096, 4096)
        channels.append(dict(backing, sha256=asset["sha256"], bytes=asset["bytes"],
                             locations=asset["locations"], nativePayloadBytesPreserved=True,
                             completeMipSamplingVerified=False))
    candidate = {"id": candidate_id, "sourceCid": unit["sourceCid"], "sourceSHA256": unit["sourceDigest"],
        "min": low.tolist(), "max": high.tolist(), "physicalSizeMetres": size.tolist(),
        "atlasUvPerMetre": ((high-low)/size).tolist(),
        "pixelsPerMetreAtMip0": ((high-low)*4096/size).tolist(), "sourcePixelFootprint": ((high-low)*4096).tolist(),
        "prototype": {"render": "PedestrianDrawBridge02 Mesh", "mesh": 0, "faces": [6708, 6709],
            "indices": faces.tolist(), "positions": positions.tolist(), "uv": uv.tolist()},
        "regions": unit["regions"], "cutout": unit["cutout"], "rows": unit["rows"], "bands": unit["bands"],
        "boundaryEvidence": {"twoTrianglesExactlyCoverRectangle": True, "affineDensityMatchesPrototype": True,
            "periodicImageBoundaryVerified": False, "filteredSeamsVerified": False, "normalSeamsVerified": False},
        "mipEvidence": {"representation": "native VTTexture plus native MidMips cache", "channels": channels,
            "independentRepeatableTextureAvailable": False, "midMipInputs": source_inputs["midMipInputs"],
            "midMipCacheMembershipVerified": False, "allMipBoundarySamplingVerified": False},
        "approvedForBake": False, "reason": "Geometric rectangle evidence does not prove a genuine repeating pattern or full-mip boundary compatibility."}
    report = {"schema": "BridgeBuilder/PedestrianDraw02/RepeatUnitEvidence/1", "style": STYLE,
        "sourceInputsSHA256": digest(args.source_inputs.read_bytes()), "nativeChartsSHA256": digest(charts_path.read_bytes()),
        "structureInventorySHA256": INVENTORY_SHA, "repeatInventorySHA256": digest(repeat_path.read_bytes()),
        "candidates": [candidate], "requests": mappings,
        "summary": {"candidateCount": 1, "approvedUnitCount": 0, "repeatRequests": 1,
                    "structureRequests": len(structures), "containedStructureRequests": sum(m["family"] == "Structure" and m["candidate"] is not None for m in mappings)},
        "inactive": repeat["evidence"]["inactivePatches"],
        "publicRequirements": ["Resolve actual VT MidMips membership and complete mip payloads before extraction.",
            "Verify source pattern and filter/normal seams; do not approve from UV rectangle alone.",
            "Share one proven unit payload across receiver sampling; do not restore per-triangle baking."]}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(report["summary"], indent=2))


if __name__ == "__main__":
    main()
