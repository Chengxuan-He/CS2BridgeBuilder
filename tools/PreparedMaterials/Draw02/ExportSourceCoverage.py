"""Export every measured near/middle road source face for production routing audit."""
import hashlib
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
NEAR = ROOT/'artifacts/material-remap/Draw02/near-cells'
SOURCE = ROOT/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'
groups = []
for level in json.loads((NEAR/'appearance-domain-measurements.json').read_text()):
    for domain in level['domains']:
        groups.append(dict(lod=level['lod'], role=0, mode=0, faces=domain['faces'], kind='moving-car'))
for name, role, mode, kind in [('walk-atlas-plan.json', 1, 2, 'moving-walk'), ('fixed-walk-atlas-plan.json', 1, 2, 'fixed-walk')]:
    for row in json.loads((NEAR/name).read_text()): groups.append(dict(lod=row['lod'], role=role, mode=mode, faces=row['faces'], kind=kind))
for row in json.loads((ROOT/'artifacts/material-remap/Draw02/fixed-car/mapping-plan.json').read_text())['rows']:
    groups.append(dict(lod=row['lod'], role=0, mode=1, faces=row['faces'], kind='fixed-car'))
records = []; seen = set()
for lod in (0, 1):
    mesh = np.load(SOURCE/f'lod{lod}-native.npz'); p = mesh['mesh0_positions']; faces = mesh['mesh0_indices']
    for group in (g for g in groups if g['lod'] == lod):
        for face in group['faces']:
            key = (lod, group['role'], face)
            assert key not in seen, key
            seen.add(key)
            records.append(dict(lod=lod, role=group['role'], mode=group['mode'], kind=group['kind'], face=face,
                positions=p[faces[face]].astype(float).reshape(-1).tolist()))
out = ROOT/'artifacts/material-remap/Draw02/source-face-coverage-input.json'
out.write_text(json.dumps(dict(faces=records), separators=(',', ':'))+'\n')
print('Exported', len(records), 'unique measured source faces; SHA256', hashlib.sha256(out.read_bytes()).hexdigest())
