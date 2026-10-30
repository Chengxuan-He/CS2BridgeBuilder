"""Report continuous affine phase residuals against original Float16 UV intervals."""
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'artifacts/material-remap/Lift04'
groups = json.loads((BASE / 'near-domain-groups.json').read_text())
tiles = json.loads((BASE / 'near-domain-audit.json').read_text())
original = json.loads((ROOT / 'artifacts/layer-decoupling/Lift04/outputs/native-repeat-mechanism/audit.json').read_text())
lo = np.array([.4697265625, .8251953125])
span = np.array([.15576171875, .15625])
records = []
for level in groups['levels']:
    lod = level['lod']
    faces = {f['face']: f for f in original['lods'][lod]['faces']}
    for index, group in enumerate(level['domains']):
        members = sorted([t for t in tiles['levels'][lod]['domains']
                          if set(t['faces']).issubset(group['faces'])], key=lambda t: t['positionMin'][2])
        error = np.zeros(2)
        minimum_radius = np.full(2, np.inf)
        budget = np.zeros(2)
        for phase, tile in enumerate(members):
            sign = np.sign(tile['affineXZ'][1][0])
            for face in tile['faces']:
                f = faces[face]
                p = np.array(f['positions'], dtype=np.float32)
                x = np.c_[p[:, [0, 2]], np.ones(3, dtype=np.float32)]
                q = np.c_[x @ np.array(group['u'], dtype=np.float32),
                          x @ np.array(group['v'], dtype=np.float32)].astype(float)
                q[:, 0] -= sign * phase
                sampled = lo + q * span
                stored = np.array(f['uv0'], dtype=np.float16)
                lower = (stored.astype(float) + np.nextafter(stored, np.float16(-np.inf)).astype(float)) / 2
                upper = (stored.astype(float) + np.nextafter(stored, np.float16(np.inf)).astype(float)) / 2
                delta = np.abs(sampled - stored.astype(float))
                radius = np.where(sampled >= stored, upper - stored, stored - lower)
                error = np.maximum(error, np.max(delta * 4096, axis=0))
                minimum_radius = np.minimum(minimum_radius, np.min(radius * 4096, axis=0))
                budget = np.maximum(budget, np.max(delta / radius, axis=0))
        records.append(dict(lod=lod, domain=index, cells=len(members),
            sourcePixelResidual=error.tolist(), minimumHalfUlpPixels=minimum_radius.tolist(),
            maximumIntervalFraction=budget.tolist(), insideSourceQuantization=bool(np.all(budget < 1)),
            min=group['min'], max=group['max'], tangent=group['tangent'], bone=group['bone']))
out = BASE / 'offline-road-unit/continuous-phase-quantization.json'
out.write_text(json.dumps(dict(domains=records,
    interpretation='UV quantization feasibility only. Original atlas reset footprints and derived PBR/mip sampling require independent validation; no physical seam movement or new receiver cuts authorized.'), indent=2))
assert all(r['insideSourceQuantization'] for r in records)
print(f'{len(records)} domains fit original UV quantization; maximum interval fraction '
      f'{max(max(r["maximumIntervalFraction"]) for r in records)}')
