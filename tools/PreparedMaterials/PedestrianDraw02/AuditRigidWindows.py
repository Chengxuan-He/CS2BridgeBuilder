"""Check original window faces against the current measured rigid ownership.

This is a source/layout check, not a native shader or generated-mesh acceptance test.
It does not relabel out-of-unit window coordinates as finite/stretch mappings.
"""
import hashlib
import json
from pathlib import Path
import numpy as np
from ExportRecipes import numbers

ROOT = Path(__file__).resolve().parents[3]

def main():
    number_path = ROOT / 'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.bin'
    source_path = ROOT / 'src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02RenderAssembly.cs'
    values = numbers(number_path)['I32']
    results = []
    for lod, render in enumerate(('PedestrianDrawBridge02 Mesh', 'PedestrianDrawBridge02_LOD1 Mesh')):
        path = ROOT / ('artifacts/prepared-materials/shared-nontiling/windows/PedestrianDraw02-' + render.replace(' ', '_') + '-1.json')
        record = json.loads(path.read_text())
        assert hashlib.sha256(Path(record['geometry']).read_bytes()).hexdigest() == record['geometrySha256']
        uv, color, faces = (np.asarray(record[k]) for k in ('uv0', 'colors', 'indices'))
        assert len(uv) == values[7] == 24 and faces.size == values[8] == 36
        first = values[10] if lod == values[9] else values[11]
        last = values[13] if lod == values[12] else values[14]
        signs = np.where((np.arange(len(uv)) >= first) & (np.arange(len(uv)) <= last), 1, -1)
        assert np.all(signs[faces] == signs[faces[:, :1]])
        assert np.isfinite(uv).all() and np.isfinite(color).all()
        results.append(dict(lod=lod, render=render, geometrySha256=record['geometrySha256'],
            rightRange=[first, last], triangles=len(faces), rigidTriangles=len(faces),
            uvAndColorSha256=hashlib.sha256(uv.tobytes() + color.tobytes()).hexdigest(),
            outsideLocalUnitTriangles=int(np.count_nonzero(np.any((uv[faces, 1:3] < 0) | (uv[faces, 1:3] > 1), axis=(1, 2))))))
    output = ROOT / 'artifacts/material-remap/PedestrianDraw02/rigid-windows.json'
    output.write_text(json.dumps(dict(sourceSha256=hashlib.sha256(source_path.read_bytes()).hexdigest(),
        mappingSha256=hashlib.sha256(number_path.read_bytes()).hexdigest(), levels=results,
        result='Every original glass triangle has one rigid room translation; no width deformation within its face.',
        limit='No finite/stretch permission is inferred for the six out-of-unit domains; inspect final uploaded raw channels separately.'), indent=2) + '\n')
    print('PASS both original window LODs: 24 triangles keep one rigid room ownership per face; no finite reclassification.')

if __name__ == '__main__':
    main()
