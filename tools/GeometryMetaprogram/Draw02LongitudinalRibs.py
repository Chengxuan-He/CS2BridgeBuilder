"""Measure the original Draw02 longitudinal ribs and their adjoining panel edges.

The seven internal member centres have six equal intervals. Preserve original
sections and distribute the half-width increment in quarters (the fourth interval
continues to the original outer base). No faces or attributes are manufactured.
"""
from pathlib import Path
import hashlib
import json
import struct
import numpy as np

R = Path(__file__).resolve().parents[2]
O = R / 'artifacts/material-remap/Draw02/underbase57'
O.mkdir(parents=True, exist_ok=True)
ints, floats = [0] * 8, []
for lod in range(3):
    source = R / f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz'
    d = np.load(source)
    v, f = d['mesh0_positions'], d['mesh0_indices'].reshape(-1, 3)
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
    components = []
    for ids in groups.values():
        lo, hi = v[ids].min(0), v[ids].max(0)
        if (lo[0] == -np.float32(5.800048828125) and hi[0] == -lo[0]
                and hi[2] - lo[2] > 70 and (lo[2] > 0 or hi[2] < 0)):
            assert lo[1] == np.float32(4.20556640625)
            assert hi[1] == np.float32(9.699951171875)
            components.append(ids)
    assert sorted(map(len, components)) == ([2556, 2556] if lod == 0 else [2542, 2544] if lod == 1 else [])
    all_ids = sorted(i for c in components for i in c)
    ids, coefficients, members = [], [], []
    # Exact source rib flanges; finite panel endpoints share these same measured
    # coordinates, while centre-girder/closure anchors stay outside this mapping.
    spans = ((1.29998779296875, 1.70001220703125),
             (2.79998779296875, 3.20001220703125),
             (4.300048828125, 4.699951171875))
    for i in all_ids:
        x = float(v[i, 0])
        if abs(x) <= .201:
            continue
        group = next((j + 1 for j, (a, b) in enumerate(spans) if a <= abs(x) <= b), 4)
        if group == 4:
            assert abs(x) == np.float32(5.800048828125)
        ids.append(i)
        coefficients.append(float(np.sign(x)) * group / 4)
    for j, (a, b) in enumerate(spans):
        for side in (-1, 1):
            rib_ids = [i for i in all_ids if a <= float(v[i, 0]) * side <= b]
            faces = np.flatnonzero(np.isin(f, rib_ids).all(1))
            if lod < 2:
                assert len(faces) > 0
                assert np.ptp(v[rib_ids, 0]) == np.float32(b - a)
            members.append(dict(side=side, center=side * (j + 1) * 1.5,
                coefficient=side * (j + 1) / 4, vertices=rib_ids, faces=faces.tolist(),
                originalWidth=b - a))
    center = json.loads((R / f'artifacts/material-remap/Draw02/underside52/lod{lod}-center-members.json').read_text())
    assert not set(ids).intersection(center['vertices'])
    if lod < 2:
        joins = json.loads((R / f'artifacts/material-remap/Draw02/edge55/lod{lod}-center-joins.json').read_text())
        assert not set(ids).intersection(joins['vertices'])
        ints[lod * 4:lod * 4 + 4] = [len(v), len(ints), len(ids), len(floats)]
        ints.extend(ids)
        floats.extend(coefficients)
    report = dict(lod=lod, sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
        vertices=ids, coefficients=coefficients, ribs=members,
        componentFaces=np.flatnonzero(np.isin(f, all_ids).any(1)).tolist(),
        far='Retain native merged shell; no independent rib detail' if lod == 2 else None)
    (O / f'lod{lod}-rib-members.json').write_text(json.dumps(report, indent=2))
    print(lod, len(ids), len(report['componentFaces']))
body = struct.pack('<8I', 0x314e4242, 1, len(ints), 0, 0, 0, len(floats), 0)
body += np.array(ints, dtype='<i4').tobytes() + np.array(floats, dtype='<f4').tobytes()
body += hashlib.sha256(body).digest()
(R / 'assets/HardcodedNumbers/Draw02/Types_Draw02_LongitudinalRibMeasurements.bin').write_bytes(body)
