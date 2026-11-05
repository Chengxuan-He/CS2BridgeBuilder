"""Audit final candidate57 native plans against the actual candidate56 plans."""
from pathlib import Path
import json
import struct
import numpy as np

R = Path(__file__).resolve().parents[2]
O = R / 'artifacts/material-remap/Draw02/underbase57'

def read(path):
    data = path.read_bytes()
    nv, nf, nr = struct.unpack_from('<3i', data)
    p = np.frombuffer(data, dtype='<f4', offset=12, count=nv * 3).reshape(-1, 3)
    at = 12 + nv * 12
    mask = np.frombuffer(data, dtype='?', offset=at, count=nf)
    return p, mask, data[at + nf:]

reports = []
for lod in range(3):
    d = np.load(R / f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz')
    v, f = d['mesh0_positions'], d['mesh0_indices'].reshape(-1, 3)
    record = json.loads((O / f'lod{lod}-rib-members.json').read_text())
    ids = np.array(record['vertices'], dtype=int)
    coefficients = np.array(record['coefficients'], dtype=np.float32)
    walks = json.loads((R / f'artifacts/material-remap/Draw02/underside52/lod{lod}-walk-members.json').read_text())
    center = json.loads((R / f'artifacts/material-remap/Draw02/underside52/lod{lod}-center-members.json').read_text())
    base = json.loads((R / f'artifacts/material-remap/Draw02/base56/lod{lod}-base-members.json').read_text())
    groups = {}
    for i in np.unique(f[record['componentFaces']]):
        groups.setdefault(tuple(v[i]), []).append(i)
    for delta in (-.1, 0, 2, 4, 5.75, 8):
        for scenario in range(6):
            suffix = f'-lod{lod}-delta{delta}-case{scenario}.bin'
            p, mask, fragments = read(O / ('after57' + suffix))
            old, oldmask, oldfragments = read(R / 'artifacts/material-remap/Draw02/base56' / ('after56' + suffix))
            assert fragments == oldfragments, 'unrelated fragments changed'
            fragment_faces, cursor = set(), 0
            while cursor < len(fragments):
                face, count = struct.unpack_from('<2i', fragments, cursor)
                fragment_faces.add(face)
                cursor += 8 + count * 4
            expected = v[ids].copy()
            expected[:, 0] = expected[:, 0] + coefficients * np.float32(delta)
            assert np.array_equal(p[ids], expected)
            remaining = np.ones(len(p), dtype=bool)
            remaining[ids] = False
            assert np.array_equal(p[remaining], old[remaining]), 'unrelated position changed'
            assert np.array_equal(p[center['vertices']], old[center['vertices']])
            assert np.array_equal(p[base['vertices']], old[base['vertices']]), '56 transverse map regressed'
            expected_mask = oldmask.copy()
            restored = 0
            for w in walks:
                assert not fragment_faces.intersection(w['faces']), 'restored face overlaps obsolete fragment'
                expected_mask[w['faces']] = False
                restored += int(oldmask[w['faces']].sum())
                # Source evidence: neither retaining these faces nor restoring
                # them on a shoulder can introduce an upward sidewalk surface.
                q = v[f[w['faces']]].astype(float)
                normals = np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0])
                assert (normals[:, 1] <= 0).all()
            assert np.array_equal(mask, expected_mask)
            assert not mask[record['componentFaces']].any(), 'original rib or panel face lost'
            for coincident in groups.values():
                assert np.all(p[coincident] == p[coincident[0]]), 'original panel/rib joint separated'
            centers = [0.]
            old_centers = [0.]
            for rib in record['ribs']:
                if not rib['vertices']:
                    continue
                q = p[rib['vertices'], 0]
                mid = float((float(q.min()) + float(q.max())) / 2)
                centers.append(mid)
                prior = old[rib['vertices'], 0]
                old_centers.append(float((float(prior.min()) + float(prior.max())) / 2))
                spacing = max(abs(float(np.spacing(x))) for x in q)
                assert abs(float(q.max()) - float(q.min()) - rib['originalWidth']) <= 2 * spacing
                assert abs(mid - (rib['center'] + rib['coefficient'] * float(np.float32(delta)))) <= 2 * spacing
                a, b = v[f[rib['faces']]].astype(float), p[f[rib['faces']]].astype(float)
                na = np.cross(a[:, 1] - a[:, 0], a[:, 2] - a[:, 0])
                nb = np.cross(b[:, 1] - b[:, 0], b[:, 2] - b[:, 0])
                active = np.sum(na * na, axis=1) != 0
                assert (np.sum(na[active] * nb[active], axis=1) > 0).all()
            if lod < 2:
                intervals = np.diff(sorted(centers))
                budget = max(abs(float(np.spacing(np.float32(x)))) for x in centers) * 4
                assert np.max(abs(intervals - (1.5 + float(np.float32(delta)) / 4))) <= budget
                if delta != 0:
                    assert np.ptp(np.diff(sorted(old_centers))) > .01, 'old crowding negative control missing'
            else:
                assert np.array_equal(p, old) and np.array_equal(mask, oldmask)
                bottom = json.loads((O / 'far-original-bottom-faces.json').read_text())
                assert len(bottom) == 64 and not mask[bottom].any(), 'native far merged bottom missing'
            reports.append(dict(lod=lod, delta=delta, sidewalkCase=scenario,
                restoredOriginalClosureFaces=restored, centres=sorted(centers),
                oldCentres=sorted(old_centers)))
assert len(reports) == 108
(O / 'geometry-result.json').write_text(json.dumps(dict(pass_=True, cases=reports,
    sharedPanelRibBoundaries=True, transverse56MapUnchanged=True,
    centralGirderUnchanged=True, noNewTopology=True), indent=2))
print('PASS 108 final-plan cases: uniform internal centres, original profiles/joints, outer closure faces and unchanged 56 transverse supports.')
