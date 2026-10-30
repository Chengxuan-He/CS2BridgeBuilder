"""Export original finite panels and one genuine moving period for offline sampling.

These triangles are authoring evidence only, never receiver topology or runtime
part classifiers. No generated bridge length participates in this recipe.
"""
import hashlib
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/PedestrianDraw02'
CHARTS = ROOT / 'artifacts/prepared-materials/PedestrianDraw02/source-consolidation'
AUDIT = ROOT / 'artifacts/layer-decoupling/PedestrianDraw02/outputs/native-repeat-mechanism/evidence.json'
SHA = 'bae5f075a29125203c1b0cf1ee8f426ac81412ed4b3a30d86266c890c53b1170'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export():
    assert digest(CHARTS / 'native-charts.npz') == SHA
    evidence = json.loads(AUDIT.read_text())
    metadata = json.loads((CHARTS / 'native-charts.json').read_text())
    arrays = np.load(CHARTS / 'native-charts.npz')
    prior = json.loads((ROOT / 'artifacts/prepared-materials/PedestrianDraw02/repeat-units-v2/trial-v1/manifest.json').read_text())['units'][0]
    for channel in prior['channels']:
        assert digest(Path(channel['path'])) == channel['sha256']
    result = dict(schema='PedestrianDraw02/NativePanel/1', sourceCid=prior['sourceCid'],
                  descriptor=prior['descriptor'], channels=prior['channels'],
                  nativeChartsSha256=SHA, repetitionEvidenceSha256=digest(AUDIT), levels=[])
    for level in evidence['levels']:
        assert digest(Path(level['geometry'])) == level['sha256']
        record = next(x for x in metadata['records'] if x['lod'] == level['lod'] and x['mesh'] == 0 and x['family'] == 0)
        assert record['sourceSurfaceCid'] == result['sourceCid']
        prefix = record['arrayPrefix']
        positions, uv, faces = (arrays[prefix + '_' + n] for n in ('positions', 'uv', 'faces'))
        panels = []
        for component in level['uvContinuousComponents']:
            xyz = positions[faces[component['faces']]].astype(np.float64)
            tex = uv[faces[component['faces']]].astype(np.float64)
            low, high = xyz.min(axis=(0, 1))[[0, 2]], xyz.max(axis=(0, 1))[[0, 2]]
            repeated = component['bones'] != [0] and component['uvMin'] == [.69775390625, .0830078125] and component['uvMax'] == [.8759765625, .411865234375]
            # Original station coordinates are retained; small source cracks are
            # reported rather than welded or promoted to a different period.
            local = (xyz[:, :, [0, 2]] - low) / (high - low)
            triangles = []
            residual = 0.
            area = 0.
            for face, point, coords, original in zip(component['faces'], local, tex, xyz):
                matrix = np.c_[point, np.ones(3)]
                determinant = np.linalg.det(matrix)
                assert abs(determinant) > 1e-16
                affine = np.linalg.solve(matrix, coords)
                error = float(abs(matrix @ affine - coords).max())
                residual = max(residual, error)
                area += abs(determinant) / 2
                # The Jacobian is retained for tangent-frame conversion review;
                # mirrored packed normals cannot be assumed equivalent.
                triangles.append(dict(face=int(face), position=original.tolist(),
                    unit=point.tolist(), sourceUv=coords.tolist(),
                    sourceUvAffine=affine.tolist(), uvJacobianDeterminant=float(np.linalg.det(affine[:2]))))
            assert residual * 4096 < 1e-8
            panels.append(dict(firstFace=int(component['faces'][0]), bone=component['bones'][0],
                repeatedV=repeated, repeatU=False, physicalMin=low.tolist(), physicalMax=high.tolist(),
                physicalSize=(high-low).tolist(), areaFraction=area,
                sourceUvMin=component['uvMin'], sourceUvMax=component['uvMax'],
                maxVertexUvResidualPixels=residual*4096, triangles=triangles))
        assert len(panels) == 52 and sum(p['repeatedV'] for p in panels) == 32
        # First positive moving traversal is the same exact bounded physical
        # interval on both halves; retain full finite transverse charts.
        unit = [p for p in panels if p['bone'] == 1 and p['repeatedV'] and p['physicalMin'][1] == 2.2608642578125]
        assert len(unit) == 2 and all(p['physicalMax'][1] == 9.05517578125 for p in unit)
        # The original centre endpoints differ by one micrometre. Do not call
        # that exact rectangular coverage or silently weld the source vertices.
        assert all(0.9999998 < p['areaFraction'] < 1 for p in unit)
        result['levels'].append(dict(lod=level['lod'], geometry=level['geometry'],
            geometrySha256=level['sha256'], panels=panels,
            movingUnitFirstFaces=[p['firstFace'] for p in unit],
            movingPhysicalPeriod=6.7943115234375,
            movingUnitUncoveredAreaFraction=max(1-p['areaFraction'] for p in unit),
            maxVertexUvResidualPixels=max(p['maxVertexUvResidualPixels'] for p in panels)))
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / 'native-panel-recipe.json'
    path.write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(dict(path=str(path), levels=len(result['levels']), panels=104,
        triangles=sum(len(p['triangles']) for l in result['levels'] for p in l['panels']),
        period=result['levels'][0]['movingPhysicalPeriod'],
        vertexResidualPixels=max(l['maxVertexUvResidualPixels'] for l in result['levels']))))


if __name__ == '__main__':
    export()
