"""Record the original transverse moving-base supports, excluding the central girder.

The source contains eighteen independent supports below the moving road. Their
old own-span coefficients disagree with the rigidly displaced longitudinal sides.
The horizontal central closure panels are separate authored components and retain
their existing inner anchors. No source faces or attributes are changed here.
"""
from pathlib import Path
import hashlib
import json
import struct
import numpy as np

R = Path(__file__).resolve().parents[2]
O = R / 'artifacts/material-remap/Draw02/base56'
O.mkdir(parents=True, exist_ok=True)
ints = [0] * 6
reports = []
for lod in range(3):
    path = R / f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz'
    data = np.load(path)
    v, f = data['mesh0_positions'], data['mesh0_indices'].reshape(-1, 3)
    parent = np.arange(len(v))
    def root(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a
    def join(a, b):
        parent[root(int(a))] = root(int(b))
    for a, b, c in f:
        join(a, b)
        join(a, c)
    coincident = {}
    for i, p in enumerate(v):
        key = tuple(p)
        if key in coincident:
            join(i, coincident[key])
        else:
            coincident[key] = i
    groups = {}
    for i in range(len(v)):
        groups.setdefault(int(root(i)), []).append(i)
    members = []
    for ids in groups.values():
        lo, hi = v[ids].min(0), v[ids].max(0)
        # Offline identification of the measured transverse diagonal support,
        # distinct from the horizontal base strips at Y=9.699951171875.
        if not (lo[0] < 0 < hi[0] and .49 < hi[2] - lo[2] < .51 and
                hi[1] == np.float32(9.60009765625)):
            continue
        station = round(float((lo[2] + hi[2]) / 2))
        assert abs(station) in range(4, 69, 8)
        assert np.max(np.abs(v[ids, 0])) <= np.float32(8.304443359375)
        faces = np.flatnonzero(np.isin(f, ids).any(1))
        assert np.isin(f[faces], ids).all()
        members.append(dict(station=station, vertices=ids, faces=faces.tolist(),
                            minimum=lo.tolist(), maximum=hi.tolist()))
    assert len(members) == (18 if lod < 2 else 0)
    ids = sorted(i for m in members for i in m['vertices'])
    assert len(ids) == len(set(ids))
    for delta in (-.1, 0, 2, 4, 5.75, 8):
        p = v.copy()
        p[ids, 0] += np.sign(p[ids, 0]) * np.float32(delta)
        assert np.array_equal(p[:, 1:], v[:, 1:])
        for member in members:
            triangles = f[member['faces']]
            a, b = v[triangles].astype(float), p[triangles].astype(float)
            na = np.cross(a[:, 1] - a[:, 0], a[:, 2] - a[:, 0])
            nb = np.cross(b[:, 1] - b[:, 0], b[:, 2] - b[:, 0])
            active = np.sum(na * na, axis=1) != 0
            assert np.all(np.sum(na[active] * nb[active], axis=1) > 0)
    center = json.loads((R / f'artifacts/material-remap/Draw02/underside52/lod{lod}-center-members.json').read_text())
    assert not set(ids).intersection(center['vertices'])
    if lod < 2:
        joins = json.loads((R / f'artifacts/material-remap/Draw02/edge55/lod{lod}-center-joins.json').read_text())
        assert not set(ids).intersection(joins['vertices'])
        ints[lod * 3:lod * 3 + 3] = [len(v), len(ints), len(ids)]
        ints.extend(ids)
    report = dict(lod=lod, sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                  sourceVertices=len(v), members=members, vertices=ids,
                  farTreatment='Existing merged shell signed displacement; no new detail' if lod == 2 else None)
    (O / f'lod{lod}-base-members.json').write_text(json.dumps(report, indent=2))
    reports.append(dict(lod=lod, supports=len(members), vertices=len(ids)))
body = struct.pack('<8I', 0x314e4242, 1, len(ints), 0, 0, 0, 0, 0) + np.array(ints, dtype='<i4').tobytes()
body += hashlib.sha256(body).digest()
(R / 'assets/HardcodedNumbers/Draw02/Types_Draw02_MovingBaseMeasurements.bin').write_bytes(body)
print(json.dumps(reports))
