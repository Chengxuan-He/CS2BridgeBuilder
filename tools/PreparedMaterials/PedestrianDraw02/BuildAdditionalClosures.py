"""Record finite side-strip closures excluded by the former road-crop overlap test.

The reviewed source face IDs identify walls/lips, not a runtime position classifier.
No source geometry, texture, UV or existing road membership is modified.
"""
import hashlib
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'artifacts/prepared-materials/PedestrianDraw02/source-consolidation'
FACES = (
    (7435, 7436, 7511, 7512, 11380, 11381, 12229, 12230,
     22467, 22468, 22543, 22544, 26412, 26413, 27261, 27262),
    (5498, 5499, 5572, 5573, 7210, 7211, 7642,
     14465, 14466, 14540, 14541, 16207, 16208, 16618),
)

def main():
    original = json.loads(Path(__file__).with_name('ClosureUvParts.json').read_text())
    payload = BASE / 'native-charts.npz'
    assert hashlib.sha256(payload.read_bytes()).hexdigest() == original['nativeChartsSha256']
    metadata = json.loads((BASE / 'native-charts.json').read_text())
    arrays = np.load(payload)
    result = dict(schema=1, style='PedestrianDraw02', nativeChartsSha256=original['nativeChartsSha256'],
        rule='Reviewed finite non-road end-wall and underside-lip side strips. '
             'Their atlas overlap with a top-road crop is not repetition evidence.', meshes=[])
    for lod, face_ids in enumerate(FACES):
        source = next(x for x in original['meshes'] if x['lod'] == lod)
        record = next(x for x in metadata['records'] if x['name'] == source['render'] and x['mesh'] == 0)
        assert record['sourceSurfaceCid'] == source['sourceSurfaceCid']
        assert hashlib.sha256(Path(source['geometry']).read_bytes()).hexdigest() == source['geometrySha256']
        prefix = record['arrayPrefix']
        p, uv, faces = (arrays[prefix + '_' + key] for key in ('positions', 'uv', 'faces'))
        old_faces = {f for part in source['parts'] for f in part['sourceFaces']}
        assert not set(face_ids).intersection(source['roadFaces'])
        assert not set(face_ids).intersection(old_faces)
        samples = []
        for face in face_ids:
            xyz, tex = p[faces[face]], uv[faces[face]]
            normal = np.cross(xyz[1] - xyz[0], xyz[2] - xyz[0])
            assert np.linalg.norm(normal) > 0 and normal[1] <= 0
            assert np.isfinite(tex).all() and (tex >= 0).all() and (tex <= 1).all()
            assert (xyz[:, 0] > 0).all() or (xyz[:, 0] < 0).all()
            samples.append(dict(face=face, positions=xyz.tolist(), uv=tex.tolist(),
                geometricNormal=(normal / np.linalg.norm(normal)).tolist()))
        result['meshes'].append(dict(render=source['render'], mesh=0, lod=lod,
            geometrySha256=source['geometrySha256'], sourceSurfaceCid=source['sourceSurfaceCid'],
            roadFaces=source['roadFaces'], samples=samples))
    Path(__file__).with_name('AdditionalClosureUv.json').write_text(json.dumps(result, indent=2) + '\n')
    print('PASS 30 reviewed side-strip closure faces; both original Geometry hashes, road exclusion and native UVs verified.')

if __name__ == '__main__':
    main()
