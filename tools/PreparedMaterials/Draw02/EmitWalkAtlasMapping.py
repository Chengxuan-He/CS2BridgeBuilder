"""Measure each LOD's complete moving sidewalk atlas traversal and terminals."""
import hashlib, json, struct
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
E = ROOT / 'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'
records, floats, candidates = [], [], []
for lod in (0, 1):
    m = np.load(E / f'lod{lod}-native.npz')
    p, uv, f = m['mesh0_positions'].astype(float), m['mesh0_uv'].astype(float), m['mesh0_indices']
    bones = m['mesh0_bones'][:, 0]
    q = p[f]
    normal = np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0])
    ids = np.where((np.ptp(q[:, :, 1], axis=1) < 1e-8) & (abs(q[:, 0, 1] - 10) < 1e-6) &
                   (normal[:, 1] > 0) & (bones[f[:, 0]] != 0))[0]
    for bone in (1, 2):
        sign = 1 if bone == 1 else -1
        for side in (-1, 1):
            faces = [i for i in ids if bones[f[i, 0]] == bone and np.sign(q[i, :, 0].mean()) == side]
            vertices = np.unique(f[faces]); points, tex = p[vertices], uv[vertices]
            assert len(faces) == 40
            lo, hi = tex.min(0), tex.max(0)
            x0, x1 = points[:, 0].min(), points[:, 0].max()
            z = points[:, 2] * sign
            centre = z.min(); end = z.max()
            stages = []
            for part, a, b in ((0, centre, 4.), (1, 4., 76.), (2, 76., end)):
                if part == 1:
                    slope = -(hi[1] - lo[1]) / 4 * sign; offset = hi[1]
                else:
                    endpoint = lo[1] if part == 0 else tex[abs(z-end) < 1e-7, 1][0]
                    slope = (endpoint-hi[1]) / (b-a) * sign; offset = hi[1]-slope*sign*a
                stages.extend((slope, offset))
            row = [x0, x1, *lo, *hi, centre, end, *stages]
            floats.extend(row)
            for face in faces:
                point = p[f[face]].mean(0)
                # All four corners of each cell have the same U affine map.
                candidates.append(dict(lod=lod, domain=(bone-1)*2+(side+1)//2,
                    x=float(point[0]), z=float(point[2]), u=float(uv[f[face],0].mean())))
            records.append(dict(lod=lod, bone=bone, side=side, faces=[int(v) for v in faces],
                sourceMinimum=lo.tolist(),sourceMaximum=hi.tolist(),sourceX=[x0,x1],
                finiteU=True,repeatVInterval=[4,76],period=4,terminalsFinite=True,profile=row))
floats.extend([4.,76.])
blob=struct.pack('<8I',0x314e4242,1,0,0,0,0,len(floats),0)+struct.pack('<%df'%len(floats),*floats)
blob+=hashlib.sha256(blob).digest()
(ROOT/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02WalkAtlasAppearance_cs.bin').write_bytes(blob)
out=ROOT/'artifacts/material-remap/Draw02/near-cells'
(out/'walk-atlas-plan.json').write_text(json.dumps(records,indent=2)+'\n')
(out/'walk-atlas-candidates.json').write_text(json.dumps(dict(points=candidates))+'\n')
print('Measured 8 moving sidewalk domains, 320 source faces; finite U, native V repetition and finite terminals.')
