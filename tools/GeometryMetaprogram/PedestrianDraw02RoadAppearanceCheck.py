"""Verify the native rectangles used by PedestrianDraw02RoadAppearance.
Usage: python PedestrianDraw02RoadAppearanceCheck.py RESEARCH_DIRECTORY
Checks source identity, complete rectangle coverage and UV gradients against the
native triangles. Does not establish rendered texture-seam or material acceptance.
"""
from pathlib import Path
import hashlib
import sys
import numpy as np

r = Path(sys.argv[1])
assert hashlib.sha256((r / 'movable-skin/PedestrianDrawBridge02 Mesh.Geometry').read_bytes()).hexdigest() == \
    '55b8fb530c4f5d80a211feecb2e82b6deb12cc8af5d76bdc3abef78b192494be'
data = np.load(r / 'pedestrian02-main-0.npz')
v, faces = data['positions'], data['indices']
uv = np.load(r / 'pedestrian02-lod0-uv-evidence.npz')['uv']
for name, ids, x0, x1, z0, z1, uv0, uv1, direction in (
    ('fixed', [6708, 6709], 2.6348876953125, 3.68646240234375, 68., 72.,
     [.81787109375, .150634765625], [.86865234375, .34423828125], 1),
    ('moving', [8556, 8557], 1.9114990234375, 3.68646240234375, 2.2608642578125, 9.05517578125,
     [.69775390625, .0830078125], [.78369140625, .411865234375], -1),
):
    p, tex = v[faces[ids]].astype(float), uv[faces[ids]].astype(float)
    assert np.all(p[:, :, 1] == 10.)
    assert set(map(tuple, p[:, :, [0, 2]].reshape(-1, 2))) == {(x0, z0), (x0, z1), (x1, z0), (x1, z1)}
    span = np.array([x1 - x0, z1 - z0])
    st = (p[:, :, [0, 2]] - [x0, z0]) / span
    if direction == -1:
        st = 1 - st
    expected = np.array(uv0) + st * (np.array(uv1) - uv0)
    assert np.max(abs(expected - tex)) < 1e-12
    areas = np.linalg.norm(np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0]), axis=1) / 2
    assert abs(areas.sum() - np.prod(span)) < 1e-10
    gradients = (np.array(uv1) - uv0) / span
    print(name, 'native UV/metre:', gradients.tolist(), 'complete rectangle passed')
