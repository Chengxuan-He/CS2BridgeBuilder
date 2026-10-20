"""Create Draw03 runtime sidewalk check fixtures from the reviewed joint audit.
Usage: python Draw03SidewalkCheckInputs.py RESEARCH_DIRECTORY
Requires Draw03SidewalkJoints.py output and decoded native mesh caches.
"""
from pathlib import Path
import json
import sys
import numpy as np

r = Path(sys.argv[1])
audit = json.loads((r / "draw03-sidewalk-joint-map.json").read_text())
for level in audit["levels"]:
    lod = level["lod"]
    data = np.load(r / f"draw03-main-{lod}.npz")
    vertices = data["positions"].astype("<f4")
    indices = data["indices"].astype("<i4")
    groups = level["groups"]
    arrays = [
        [i for group in groups for i in group[key]]
        for key in ("leftFaces", "rightFaces", "carriagewayFaces", "vertices")
    ]
    for values in arrays:
        assert len(values) == len(set(values))
    assert not set(arrays[0]).intersection(arrays[2])
    assert not set(arrays[1]).intersection(arrays[2])
    with (r / f"draw03-sidewalk-check-{lod}.bin").open("wb") as output:
        output.write(np.array([len(vertices), indices.size], dtype="<i4").tobytes())
        output.write(vertices.tobytes())
        output.write(indices.tobytes())
        for values in arrays:
            output.write(np.array([len(values)], dtype="<i4").tobytes())
            output.write(np.array(values, dtype="<i4").tobytes())
    print(f"LOD{lod}: fixture with {len(arrays[3])} measured joint vertices")
