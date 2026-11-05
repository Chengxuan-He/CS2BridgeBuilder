"""Controlled placement of actual native asphalt and paving P/UV; no invented paving UV."""
from pathlib import Path
import json, struct
import numpy as np

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/material-remap/Lift02/underlay71'
cases = json.loads((root / 'artifacts/material-remap/shared/draw01-head42/sidewalk-compose-fixtures.json').read_text())['cases']

def clip(poly, axis, bound, sign):
    result = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        da, db = (a[axis] - bound) * sign, (b[axis] - bound) * sign
        if da >= 0:
            result.append(a)
        if (da < 0 < db) or (db < 0 < da):
            p = a + (b - a) * (da / (da - db))
            p[axis] = bound
            result.append(p)
    return result

for lod in range(3):
    records = []
    data = np.load(out / f'current-road-lod{lod}.npz')
    left, right = float(np.float32(-10.600006103515625)), float(np.float32(10.6))
    for face in data['faces']:
        poly = list(np.concatenate((data['positions'][face], data['uv0'][face, :2]), axis=1).astype(float))
        poly = clip(clip(poly, 0, left, 1), 0, right, -1)
        for i in range(1, len(poly) - 1):
            records.append((0, np.array([poly[0], poly[i], poly[i + 1]])))
    for xshift in [-11.75, 12.35]:
        for zshift in [-96, -32, 32, 96]:
            for case in cases:
                if case['occurrence'] != 0:
                    continue
                poly = [np.array(v['P'] + v['UV'][:2], dtype=float) for v in case['vertices']]
                for p in poly:
                    p[:3] += [xshift, 9.99366, zshift]
                    p[:3] = p[:3].astype(np.float32)
                poly = clip(clip(poly, 2, -106, 1), 2, 106, -1)
                for i in range(1, len(poly) - 1):
                    records.append((2, np.array([poly[0], poly[i], poly[i + 1]])))
    with (out / f'mixed_native-lod{lod}-receivers.bin').open('wb') as stream:
        stream.write(struct.pack('<if', len(records), 8.5))
        for material, triangle in records:
            stream.write(struct.pack('<i', material))
            stream.write(triangle.astype('<f4').tobytes())
    print(lod, len(records))
    one_side = [(material, triangle) for material, triangle in records
                if material == 2 and triangle[:, 0].mean() > 0]
    for face in data['faces']:
        poly = list(np.concatenate((data['positions'][face], data['uv0'][face, :2]), axis=1).astype(float))
        poly = clip(clip(poly, 0, -13.5, 1), 0, right, -1)
        for i in range(1, len(poly) - 1):
            one_side.append((0, np.array([poly[0], poly[i], poly[i + 1]])))
    with (out / f'one_sidewalk-lod{lod}-receivers.bin').open('wb') as stream:
        stream.write(struct.pack('<if', len(one_side), 8.5))
        for material, triangle in one_side:
            stream.write(struct.pack('<i', material))
            stream.write(triangle.astype('<f4').tobytes())
    print('one_sidewalk', lod, len(one_side))

(out / 'mixed-native-fixture.json').write_text(json.dumps(dict(
    scope='Actual native P/UV with controlled selected-road placement, not an ECS capture.',
    asphalt='Current generated 30.5 m asphalt receiver, clipped to the central lane region.',
    paving='Native Sidewalk occurrence 0, rigidly translated; original UV retained, clipped at Z +-106.',
    source='shared/draw01-head42/sidewalk-compose-fixtures.json',
    targetHalfDelta=8.5, sidewalkYTranslation=9.99366,
    oneSidewalk='Right paving only; left side remains actual asphalt P/UV, no mirrored paving.',
    xTranslations=[-11.75,12.35], zTranslations=[-96,-32,32,96]), indent=2))
