"""Audit the remaining native Lift01 tower proxy panels before width mapping.
Usage: python Lift01FarTowerPanels.py RESEARCH_DIRECTORY OUTPUT_JSON
This is offline evidence, not a runtime geometric classifier or deformation.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

research, output = map(Path, sys.argv[1:])
digest = hashlib.sha256((research / 'movable-skin' / 'LiftBridge01_LOD2 Mesh.Geometry').read_bytes()).hexdigest()
assert digest == 'a634d30d56089487f0a4fbd1d6f7dfbe3644c99f5b447b4a6658e4db282fd925'
data = np.load(research / 'lift01-main-2.npz')
vertices, faces = data['positions'], data['indices']
uv = np.load(research / 'lift01-far-uv.npy')
assert vertices.shape == (1110, 3) and faces.shape == (776, 3) and uv.shape == (1110, 2)
groups = json.loads((research / 'lift01-far-fixed-parts.json').read_text())
roles = ('upper_end', 'upper_bottom', 'upper_top', 'lower_outer_end', 'lower_inner_end')
starts = ((4, 6, 8, 14, 16), (392, 394, 396, 402, 404))
lateral = [0, 1, 2, 3, 10, 11, 12, 13, 388, 389, 390, 391, 398, 399, 400, 401]
used = set()
panels = []
for side, offsets in zip((1, -1), starts):
    for role, first in zip(roles, offsets):
        triangle_ids = [first, first + 1]
        ids = np.unique(faces[triangle_ids])
        assert len(ids) == 4 and not used.intersection(ids)
        used.update(map(int, ids))
        member = np.isin(faces, ids)
        assert np.array_equal(np.flatnonzero(member.all(1)), triangle_ids)
        assert not np.any(member.any(1) & ~member.all(1))
        p = vertices[ids]
        assert p[:, 0].min() < 0 < p[:, 0].max()
        assert np.all(side * p[:, 2] >= -1e-10)
        panels.append(dict(side=side, role=role, triangles=triangle_ids,
            vertices=ids.tolist(), positions=p.tolist(), uv=uv[ids].tolist(),
            minimum=p.min(0).tolist(), maximum=p.max(0).tolist()))
assert len(used) == 40
assert not used.intersection(faces[lateral].flat)
assert used.union(map(int, faces[lateral].flat)) == set(groups[0]['vertices'])

# Keep the detailed part measurements beside the proxy evidence. Matching a
# coarse quad to one of these is not implied merely by overlapping bounds.
near = json.loads((research / 'lift01-crossing-0.json').read_text())
references = {str(i): {key: near[i][key] for key in ('minimum', 'maximum')}
              for i in (23, 28, 29, 171, 265, 280, 287, 308, 315, 334, 335, 343)}
result = dict(geometry_sha256=digest, panels=panels, detailed_parts=references,
    unresolved='Transverse proxies combine baked columns, crossbars and roof details. '
               'Their original UV mappings must be preserved at separately transformed features; '
               'whole-panel stretching is not an accepted width recipe.')
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print('Verified ten two-triangle proxy panels: 40 unique vertices, disjoint from 32 lateral vertices.')
print('Panel coordinates and native UVs written for correspondence analysis; no deformation emitted.')
