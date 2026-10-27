"""Measure proven source UV rectangle consolidation without changing runtime resources."""
import json
import sys
from pathlib import Path
import numpy as np
from MovableUvRecipes import rectangle_pairs, tessellated_rectangles, underside_uv

source, repository, report = map(Path, sys.argv[1:4])
rows = []
for path in sorted(source.glob('*.npz')):
    native = np.load(path)
    for key in native.files:
        if not key.endswith('_faces'):
            continue
        prefix = key[:-len('faces')]
        if prefix+'uv0' not in native:
            continue
        uv = underside_uv(path, prefix, native[prefix+'uv0'][:, :2].astype(float), repository)
        faces = native[key]
        before = rectangle_pairs(faces, uv)
        measured = tessellated_rectangles(faces, uv)
        after = dict(before)
        after.update(measured)
        unique = lambda patches: len(set(tuple(bounds) for bounds in patches.values()))
        rows.append(dict(source=path.name, stream=prefix, faces=len(faces),
            beforeFaces=len(before), afterFaces=len(after), beforeRectangles=unique(before),
            afterRectangles=unique(after), changedFaces=sum(
                face not in before or not np.array_equal(before[face], bounds)
                for face, bounds in after.items())))
    print('Measured '+path.name, flush=True)
report.write_text(json.dumps(dict(resourcesModified=False, meshes=len(rows),
    beforeRectangles=sum(r['beforeRectangles'] for r in rows),
    afterRectangles=sum(r['afterRectangles'] for r in rows),
    changedFaces=sum(r['changedFaces'] for r in rows), rows=rows), indent=2), encoding='utf-8')
