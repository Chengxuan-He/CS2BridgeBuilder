"""Report physical vertical faces whose final selected-road UV area collapsed."""
from pathlib import Path
import json
import numpy as np

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/material-remap/Lift02/underlay71'
dtype = np.dtype([('face', '<i4'), ('material', '<i4'), ('vertices', '<f4', (3, 5))])
results = []
for label in ['asphalt', 'ballast', 'mixed', 'paving', 'unknown', 'mixed_native', 'one_sidewalk']:
    for lod in range(3):
        data = np.frombuffer((out / f'{label}-lod{lod}-triangles.bin').read_bytes(), dtype, offset=4)
        v = data['vertices'].astype(float)
        normal = np.cross(v[:, 1, :3] - v[:, 0, :3], v[:, 2, :3] - v[:, 0, :3])
        vertical = (normal[:, 1] == 0) & np.any(normal != 0, axis=1)
        a = v[:, 1, 3:] - v[:, 0, 3:]
        b = v[:, 2, 3:] - v[:, 0, 3:]
        collapsed = vertical & (a[:, 0] * b[:, 1] == a[:, 1] * b[:, 0])
        result = dict(input=label, lod=lod, verticalFaces=int(sum(vertical)),
                      zeroUvAreaVerticalFaces=int(sum(collapsed)),
                      affectedSourceFaces=sorted(map(int, set(data['face'][collapsed]))))
        results.append(result)
        print(label, lod, result['verticalFaces'], result['zeroUvAreaVerticalFaces'])
(out / 'vertical-sampling.json').write_text(json.dumps(results, indent=2))
assert all(r['zeroUvAreaVerticalFaces'] == 0 for r in results if r['input'] in ['asphalt', 'ballast'])
