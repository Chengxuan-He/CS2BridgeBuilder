"""Create native mesh fixtures for tools/MovableSidewalkCheck.
Usage: python Draw02SidewalkCheckInputs.py RESEARCH_DIRECTORY
The measured slab bounds are reviewed in Draw02FixedSidewalks.py. Never classify
runtime bridge geometry using these offline fixture identification bounds.
"""
from pathlib import Path
import sys
import numpy as np

research = Path(sys.argv[1])
for lod in range(3):
    data = np.load(research / f"draw02-main-lod{lod}-uv.npz")
    vertices = data["positions"].astype("<f4")
    faces = data["indices"].reshape(-1, 3).astype("<i4")
    points = vertices[faces]
    low, high = points.min(1), points.max(1)
    ends = ((low[:, 2] >= 78.74) & (high[:, 2] <= 80.02)) | (
        (low[:, 2] >= -80.02) & (high[:, 2] <= -78.74))
    height = (low[:, 1] >= 9.56) & (high[:, 1] <= 10)
    groups = []
    for side in (-1, 1):
        lateral = ((low[:, 0] >= 6.69) & (high[:, 0] <= 9.5)) if side > 0 else (
            (low[:, 0] >= -9.5) & (high[:, 0] <= -6.69))
        selected = np.flatnonzero(ends & height & lateral).astype("<i4")
        assert len(selected) == (22, 20, 0)[lod]
        owned = np.unique(faces[selected])
        incident = np.flatnonzero(np.isin(faces, owned).any(1))
        assert np.array_equal(incident, selected)
        groups.append(selected)
    with (research / f"draw02-sidewalk-check-{lod}.bin").open("wb") as output:
        output.write(np.array([len(vertices), faces.size], dtype="<i4").tobytes())
        output.write(vertices.tobytes())
        output.write(faces.tobytes())
        for selected in groups:
            output.write(np.array([len(selected)], dtype="<i4").tobytes())
            output.write(selected.tobytes())
    print(f"LOD{lod}: native mesh fixture with {sum(map(len, groups))} exclusive sidewalk faces")
