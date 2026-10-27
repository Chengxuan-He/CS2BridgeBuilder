"""Export native source faces for the actual C# float-frame regression executable.

Usage: python ExportUvFrameCases.py NPZ_DIRECTORY REPOSITORY OUTPUT_BIN
This exercises source frames and affine transforms, not game-generated geometry.
"""
from pathlib import Path
import json
import struct
import sys
import numpy as np
from UvDensityAudit import jacobians
from MovableUvRecipes import underside_uv

source, root, output = map(Path, sys.argv[1:])
records, sources = 0, []
with output.open('wb') as writer:
    writer.write(struct.pack('<ii', 1, 0))
    for path in sorted(source.glob('*.npz')):
        data = np.load(path)
        for key in data.files:
            if not key.endswith('_positions'):
                continue
            prefix = key[:-len('positions')]
            if prefix+'uv0' not in data:
                continue
            p = data[key]
            uv = underside_uv(path, prefix, data[prefix+'uv0'][:, :2].astype(float), root)
            faces = data[prefix+'faces']
            ids, j = jacobians(p, uv, faces)
            ids = ids[np.any(j != 0, axis=(1, 2))]
            index = len(sources)
            sources.append({'source': path.name, 'mesh': prefix, 'faces': len(ids)})
            dtype = np.dtype([('source', '<i4'), ('face', '<i4'), ('positions', '<f4', (9,)), ('uv', '<f4', (6,))])
            rows = np.empty(len(ids), dtype=dtype)
            rows['source'] = index
            rows['face'] = ids
            rows['positions'] = p[faces[ids]].reshape(-1, 9)
            rows['uv'] = uv[faces[ids]].reshape(-1, 6)
            writer.write(rows.tobytes())
            records += len(ids)
    writer.seek(4)
    writer.write(struct.pack('<i', records))
output.with_suffix('.json').write_text(json.dumps(sources, indent=2), encoding='utf-8')
print(f'Exported {records} nonconstant, nondegenerate native UV frames from {len(sources)} mesh streams.')
