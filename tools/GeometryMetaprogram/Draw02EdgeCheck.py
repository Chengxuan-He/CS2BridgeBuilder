"""Verify candidate55 final plans, source joins and exact native attributes separately."""
from pathlib import Path
import json, struct
import numpy as np

R = Path(__file__).resolve().parents[2]
O = R / 'artifacts/material-remap/Draw02/edge55'
S = R / 'artifacts/material-remap/Draw02/underside52'

def read(path):
    b = path.read_bytes()
    nv, nf, nr = struct.unpack_from('<3i', b)
    p = np.frombuffer(b, dtype='<f4', offset=12, count=nv * 3).reshape(-1, 3)
    at = 12 + nv * 12
    mask = np.frombuffer(b, dtype='?', offset=at, count=nf)
    at += nf
    fragments = {}
    for _ in range(nr):
        face, count = struct.unpack_from('<2i', b, at)
        at += 8
        fragments[face] = np.frombuffer(b, dtype='<i4', offset=at, count=count).tolist()
        at += count * 4
    return p, mask, fragments

cases = 0
for lod in range(3):
    d = np.load(R / f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz')
    v, f = d['mesh0_positions'], d['mesh0_indices'].reshape(-1, 3)
    center = json.loads((S / f'lod{lod}-center-members.json').read_text())
    original_joint = {}
    for vertex in center['vertices']:
        original_joint.setdefault(tuple(v[vertex]), []).append(vertex)
    walks = json.loads((S / f'lod{lod}-walk-members.json').read_text())
    outer = set(json.loads((O / f'lod{lod}-outer-members.json').read_text())['removedFaces'])
    joins = json.loads((O / f'lod{lod}-center-joins.json').read_text()) if lod < 2 else dict(vertices=[], faces=[])
    for delta in (0, 2, 4, 5.75, 8):
        for scenario in range(6):
            p, mask, fragments = read(O / f'after55-lod{lod}-delta{delta}-case{scenario}.bin')
            old, oldmask, oldfragments = read(O / f'baseline/before-lod{lod}-delta{delta}-case{scenario}.bin')
            assert np.array_equal(p[:len(v), 1:], v[:, 1:])
            assert fragments == {k: a for k, a in oldfragments.items() if k not in outer}
            assert not any(mask[list(outer)])
            assert np.array_equal(p[center['vertices']], v[center['vertices']])
            if lod == 2:
                assert np.array_equal(p, old) and np.array_equal(mask, oldmask)
            else:
                ids, faces = joins['vertices'], joins['faces']
                assert np.array_equal(p[ids], v[ids]) and not any(mask[faces])
                # Shared original panel/girder coordinates remain bit-identical.
                for i in joins['exactGirderPositionMatches']:
                    matches = original_joint.get(tuple(v[i]), [])
                    assert matches and all(np.array_equal(p[i], p[j]) for j in matches)
                a, b = v[f[faces]].astype(float), p[f[faces]].astype(float)
                n0 = np.cross(a[:, 1] - a[:, 0], a[:, 2] - a[:, 0])
                n1 = np.cross(b[:, 1] - b[:, 0], b[:, 2] - b[:, 0])
                assert np.all(np.einsum('ij,ij->i', n0, n1) > 0)
                # The original rib-side edge remains at the old rigid position.
                outside = np.setdiff1d(np.unique(f[faces]), ids)
                assert np.array_equal(p[outside], old[outside])
                if delta:
                    assert not np.array_equal(old[ids], v[ids])  # Reject the old open joint.
                if delta == 0:
                    assert np.array_equal(p[np.unique(f[faces])], v[np.unique(f[faces])])
                ov = np.unique(f[sorted(outer)])
                assert np.array_equal(p[ov, 0], v[ov, 0] + np.sign(v[ov, 0]) * np.float32(delta))
            spans = [((-9.5-delta,-6-delta),(6+delta,9.5+delta)),((0,0),(0,0)),
                ((-12,-7),(0,0)),((0,0),(8,12)),((-13,-8),(9,11)),((-13.5,-10.5),(10.5,13.5))][scenario]
            for walk, span in zip(walks, spans):
                ids, faces = walk['vertices'], walk['faces']
                present = span[1] > span[0]
                assert np.all(mask[faces] == (not present))
                if not ids or not present:
                    continue
                minimum = np.float32(-9.5 if walk['side'] < 0 else 6)
                fraction = (v[ids, 0] - minimum) / np.float32(3.5)
                origin = v[ids, 0] + (np.float32(span[0]) - minimum)
                factor = np.float32(span[1] - span[0] - 3.5)
                expected = (origin.astype(float) + float(factor) * fraction.astype(float)).astype(np.float32)
                assert np.array_equal(p[ids, 0], expected)
            cases += 1
report = dict(cases=cases, originalYZ=True, centerSectionUnchanged=True,
    originalPanelGirderSharedPositionsExact=True, nativeLeafTipsAnchored=True,
    ribEdgesUnchangedFromRigidPlan=True, originalWindingPreserved=True,
    originalWidthPanelUnchanged=True, farMergedShellUnchanged=True,
    outerSupportTurnsRestored=True, supersededFragmentsRemoved=True,
    absentSidewalksNotInvented=True, oldOpenJointNegativeControl=True,
    actual54InputCase='delta5.75/case1: 4m Car lanes, 2m asphalt shoulders with pedestrian lanes; no Sidewalk sections')
(O / 'geometry-result.json').write_text(json.dumps(report, indent=2))
print(report)
