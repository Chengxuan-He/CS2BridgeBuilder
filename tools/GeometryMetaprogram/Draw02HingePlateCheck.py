"""Check exact original hinge-plate membership against final native Plan exports.

This is not a narrow-road redesign or a global mesh-quality acceptance test.
Unchanged rib overlap is reported separately, following the user's explicit scope.
"""
from pathlib import Path
import hashlib
import json
import struct
import numpy as np

R = Path(__file__).resolve().parents[2]
O = R / 'artifacts/material-remap/Draw02/track63'
members = json.loads((O / 'hinge-members.json').read_text())

def read(path):
    b = path.read_bytes()
    nv, nf, nr = struct.unpack_from('<3i', b)
    p = np.frombuffer(b, '<f4', nv * 3, 12).reshape(-1, 3)
    mask = np.frombuffer(b, '?', nf, 12 + nv * 12)
    return p, mask, b[12 + nv * 12:]

results = []
for lod in range(3):
    source = R / f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz'
    d = np.load(source)
    v, faces = d['mesh0_positions'], d['mesh0_indices'].reshape(-1, 3)
    plates = [m for m in members[lod] if m['min'][1] == float(np.float32(2.07904052734375))
              and m['max'][1] == float(np.float32(4.20556640625))]
    assert len(plates) == (6 if lod < 2 else 0)
    center = [m for m in plates if m['min'][0] < 0 < m['max'][0]]
    assert sorted(len(m['ids']) for m in center) == ([212, 212] if lod == 0 else [73, 73] if lod == 1 else [])
    central_ids = sorted(i for m in center for i in m['ids'])
    # The same Y/Z shape and leaf bone appear in all three plate positions.
    for central in center:
        signature = np.unique(v[central['ids'], 1:], axis=0)
        same_leaf = [m for m in plates if m['bones'] == central['bones']]
        assert len(same_leaf) == 3
        for m in same_leaf:
            if len(m['ids']) == len(central['ids']):
                assert np.array_equal(np.unique(v[m['ids'], 1:], axis=0), signature)
    for path in sorted(O.glob(f'*/after61-lod{lod}-delta*-case*.bin')):
        delta = np.float32(path.name.split('-delta')[1].split('-case')[0])
        p, mask, index_bytes = read(path)
        for m in plates:
            ids = np.array(m['ids'])
            side = -1 if m['max'][0] < 0 else 1 if m['min'][0] > 0 else 0
            expected = v[ids].copy()
            expected[:, 0] += np.float32(side) * delta
            assert np.array_equal(p[ids], expected), (path.name, 'plate rigid section')
            assert not mask[m['faces']].any(), (path.name, 'plate face omitted')
            f = faces[m['faces']]
            old, new = v[f].astype(float), p[f].astype(float)
            a = np.cross(old[:, 1] - old[:, 0], old[:, 2] - old[:, 0])
            b = np.cross(new[:, 1] - new[:, 0], new[:, 2] - new[:, 0])
            assert ((a * b).sum(1) > 0).all(), (path.name, 'plate winding/degeneracy')
            assert np.array_equal(np.unique(d['mesh0_bones'][ids]), m['bones'])
        for m in center:
            axle = next(a for a in members[lod] if len(a['ids']) == (720 if lod == 0 else 296)
                        and a['bones'] == m['bones'])
            # Original central axial collar straddles the complete plate section.
            # This is an axial contact/containment check, not a claim that the
            # independently authored parts share welded vertices.
            original = v[axle['ids'], 0]
            ring = min(x for x in original if x > 0)
            ring_ids = np.array(axle['ids'])[original == ring]
            assert p[ring_ids, 0].min() > p[m['ids'], 0].max(), (path.name, 'central collar no longer spans plate')
        old_path = R / 'artifacts/material-remap/Draw02/gap61' / path.name
        if old_path.exists():
            old, _, old_indices = read(old_path)
            unchanged = np.ones(len(p), dtype=bool)
            unchanged[central_ids] = False
            assert np.array_equal(p[unchanged], old[unchanged]), (path.name, 'unrelated position changed')
            assert index_bytes == old_indices, (path.name, 'topology changed')
        results.append(dict(case=str(path.relative_to(O)), lod=lod, delta=float(delta),
                            sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                            plateVertices=sum(len(m['ids']) for m in plates),
                            plateFaces=sum(len(m['faces']) for m in plates),
                            nativePlan=True, unchangedBaseline=old_path.exists()))
assert results, 'Run final candidate native consumer first.'
(O / 'hinge-plate-final-check.json').write_text(json.dumps(dict(cases=results, passed=True), indent=2))
print(f'PASS {len(results)} final native Plan cases: exact plate sections, original faces/bones, winding and unchanged baseline.')
