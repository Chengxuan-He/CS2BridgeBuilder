"""Compare final native-plan exports with the unchanged 55r3 baseline.

This test does not bless an expected image: exact source membership was measured
separately. It checks the requested transform, unchanged central assembly and all
unrelated vertices/indices, and preserves original side-support relative positions.
"""
from pathlib import Path
import json
import struct
import numpy as np

R = Path(__file__).resolve().parents[2]
O = R / 'artifacts/material-remap/Draw02/base56'

def read(path):
    b = path.read_bytes()
    nv, nf, nr = struct.unpack_from('<3i', b)
    p = np.frombuffer(b, dtype='<f4', offset=12, count=nv * 3).reshape(-1, 3)
    return p, b[12 + nv * 12:]

cases = []
connections = []

def sections(points, faces, z, x):
    values = []
    for face in faces:
        q = points[face].astype(float)
        edges = []
        for i, j in ((0, 1), (1, 2), (2, 0)):
            if min(q[i, 2], q[j, 2]) < z < max(q[i, 2], q[j, 2]):
                edges.append(q[i] + (q[j] - q[i]) * ((z - q[i, 2]) / (q[j, 2] - q[i, 2])))
        if len(edges) != 2:
            continue
        a, b = edges
        if a[0] != b[0] and min(a[0], b[0]) < x < max(a[0], b[0]):
            values.append((float(a[1] + (b[1] - a[1]) * (x - a[0]) / (b[0] - a[0])),
                           abs(float((b[1] - a[1]) / (b[0] - a[0])))))
    return sorted(values)

for lod in range(3):
    data = np.load(R / f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz')
    v, f = data['mesh0_positions'], data['mesh0_indices'].reshape(-1, 3)
    members = json.loads((O / f'lod{lod}-base-members.json').read_text())
    ids = np.array(members['vertices'], dtype=int)
    center = json.loads((R / f'artifacts/material-remap/Draw02/underside52/lod{lod}-center-members.json').read_text())['vertices']
    joins = json.loads((R / f'artifacts/material-remap/Draw02/edge55/lod{lod}-center-joins.json').read_text())['vertices'] if lod < 2 else []
    for delta in (-.1, 0, 2, 4, 5.75, 8):
        for scenario in range(6):
            suffix = f'-lod{lod}-delta{delta}-case{scenario}.bin'
            p, indices = read(O / ('after56' + suffix))
            old, oldindices = read(O / 'baseline55' / ('after55' + suffix))
            assert indices == oldindices, (lod, delta, scenario, 'topology changed')
            removed = np.frombuffer(indices, dtype='?', count=len(f))
            for member in members['members']:
                assert not removed[member['faces']].any(), 'required source base face missing'
            unchanged = np.ones(len(p), dtype=bool)
            unchanged[ids] = False
            assert np.array_equal(p[unchanged], old[unchanged]), (lod, delta, 'unrelated vertex changed')
            assert np.array_equal(p[:len(v), 1:], v[:, 1:])
            assert np.array_equal(p[center], v[center])
            assert np.array_equal(p[joins], old[joins])
            expected = v[ids].copy()
            expected[:, 0] = expected[:, 0] + np.sign(expected[:, 0]) * np.float32(delta)
            assert np.array_equal(p[ids], expected), (lod, delta, 'base signed displacement')
            if delta == 0:
                assert np.array_equal(p, old)
            # Native side support and longitudinal body are independently authored,
            # overlapping attachments rather than welded, identical vertex pairs.
            # Every half-support is now a rigid translation, preserving all original
            # same-side contacts/clearances with the already translated lateral body.
            for member in members['members']:
                local = np.array(member['vertices'])
                for side in (-1, 1):
                    side_ids = local[np.sign(v[local, 0]) == side]
                    # The same binary32 operation on original X is the independent
                    # bound; no observed geometry error is used to tune a tolerance.
                    reference = np.float32(v[side_ids, 0] + np.float32(side * delta))
                    assert np.array_equal(p[side_ids, 0], reference)
                triangles = f[member['faces']]
                a, b = v[triangles].astype(float), p[triangles].astype(float)
                n0 = np.cross(a[:, 1] - a[:, 0], a[:, 2] - a[:, 0])
                n1 = np.cross(b[:, 1] - b[:, 0], b[:, 2] - b[:, 0])
                active = np.sum(n0 * n0, axis=1) != 0
                assert np.all(np.sum(n0[active] * n1[active], axis=1) > 0), 'winding/degenerate support'
                if scenario == 1:
                    # Original longitudinal side-wall planes, measured from source
                    # faces (near 6728/6729, 6833/6834 and their per-LOD counterparts).
                    for wall in (-6.14599609375, -5.74609375, 5.74609375, 6.14599609375):
                        side = np.sign(wall)
                        z = member['station'] + .01
                        original = sections(v, triangles, z, wall)
                        moved_wall = float(np.float32(np.float32(wall) + np.float32(side * delta)))
                        generated = sections(p, triangles, z, moved_wall)
                        prior = sections(old, triangles, z, moved_wall)
                        assert len(original) == len(generated), 'source attachment section lost'
                        # A priori binary32 X rounding bound for each source/support
                        # coordinate and the translated wall. Y/Z are untouched.
                        ulp = max(float(abs(np.spacing(x))) for x in p[local, 0])
                        x_bound = ulp + abs(float(np.spacing(np.float32(moved_wall))))
                        for (y, slope), (gy, _) in zip(original, generated):
                            bound = slope * x_bound * 2 + np.finfo(float).eps * 128
                            assert abs(y - gy) <= bound, (lod, delta, member['station'], wall, y, gy, bound)
                        connections.append(dict(lod=lod, delta=delta, station=member['station'],
                            wall=wall, originalY=[x[0] for x in original],
                            generatedY=[x[0] for x in generated], oldY=[x[0] for x in prior]))
            old_error = float(np.max(np.abs(old[ids, 0] - expected[:, 0]))) if len(ids) else 0
            if lod < 2 and delta != 0:
                assert old_error > 0, 'old proportional route must fail this regression'
            cases.append(dict(lod=lod, delta=delta, sidewalkCase=scenario,
                              changedBaseVertices=len(ids), oldSignedMappingError=old_error))
assert len(cases) == 108
result = dict(pass_=True, cases=cases, centerAndClosurePanelsUnchanged=True,
              attachmentSections=connections,
              originalTopologyAndOtherVerticesUnchanged=True,
              limitation='Native upload/Retile receipt is separate; no in-game visual or motion acceptance.')
(O / 'geometry-result.json').write_text(json.dumps(result, indent=2))
print('PASS 108 final-plan cases; all source supports signed, central assembly and unrelated geometry unchanged.')
