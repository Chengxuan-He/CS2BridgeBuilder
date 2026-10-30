"""Measure native road UV-reset slabs; no mesh or material generation."""
import ast
import hashlib
import itertools
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/Draw01/native-atlas-road'
OUT.mkdir(parents=True, exist_ok=True)
# Reuse the audited rounding-cell solver without executing its report writer.
tree = ast.parse((Path(__file__).with_name('AuditFarPrecision.py')).read_text())
fn = next(n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name == 'feasible')
exec(compile(ast.Module(body=[fn], type_ignores=[]), '<rounding-cell-solver>', 'exec'))
domains = [
    # Middle simplification pushes endpoint V outside the ordinary repeated range.
    ('asphalt', 9.800048828125, [.012, .201, .65, .89], 'Asphalt'),
    ('grating', 10, [.240, .410, .65, .89], 'Sidewalk'),
    ('fixed-sidewalk', 10, [.485, .676, .012, .274], 'Sidewalk'),
    ('fixed-carriageway', 9.800048828125, [.726, .950, .012, .274], 'Asphalt'),
]
reports = []
for lod, file in enumerate(['DrawBridge01 Mesh.npz', 'DrawBridge01_LOD1 Mesh.npz']):
    path = ROOT / 'artifacts/uv-density/native' / file
    data = np.load(path)
    p = data['mesh0_positions'].astype(float)
    uv = data['mesh0_uv0'].astype(float)
    faces = data['mesh0_faces']
    pp, tt = p[faces], uv[faces]
    rows = []
    for name, y, bounds, supported in domains:
        mask = (np.max(abs(pp[:, :, 1] - y), axis=1) < 1e-5)
        mask &= (tt[:, :, 0].min(1) >= bounds[0]) & (tt[:, :, 0].max(1) <= bounds[1])
        mask &= (tt[:, :, 1].min(1) >= bounds[2]) & (tt[:, :, 1].max(1) <= bounds[3])
        for right in [False, True]:
            for positive in [False, True]:
                side = pp[:, :, 0].min(1) >= 0 if right else pp[:, :, 0].max(1) <= 0
                leaf = pp[:, :, 2].min(1) > 0 if positive else pp[:, :, 2].max(1) < 0
                selected = np.flatnonzero(mask & side & leaf)
                groups = {}
                for i in selected:
                    key = tuple(sorted([float(abs(pp[i, :, 2]).min()), float(abs(pp[i, :, 2]).max())]))
                    groups.setdefault(key, []).append(int(i))
                cells = []
                for zr, ids in sorted(groups.items()):
                    vertices = np.unique(faces[ids])
                    xyz, tex = p[vertices], uv[vertices]
                    axes = feasible(xyz, tex)
                    cells.append(dict(zRange=list(zr), faces=ids, positions=xyz.tolist(), uv=tex.tolist(),
                                      uvMin=tex.min(0).tolist(), uvMax=tex.max(0).tolist(),
                                      affineAxes=axes, sourceSpanMetres=zr[1]-zr[0]))
                if cells:
                    seams = [c['zRange'][1] for c in cells[:-1]]
                    continuous = all(a['zRange'][1] == b['zRange'][0] for a, b in zip(cells, cells[1:]))
                    rows.append(dict(domain=name, supportedType=supported, right=right, positive=positive,
                                     sourceFaces=len(selected), cells=cells, resetAbsZ=seams,
                                     continuous=continuous, repeatAxis='V', finiteAxis='U',
                                     normalFrame='Native source U/V orientation; no texture-channel rotation.'))
    reports.append(dict(lod=lod, sourceFile=file, sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                        sourceSurfaceCid='97291ab7f7938fc35cc670b15f2bca36',
                        wholeMeshTriangles=sum(len(data[k]) for k in data.files if k.endswith('_faces') and 'metric' not in k),
                        domains=rows))
report = dict(status='MEASURED_RECIPE_NOT_RUNTIME_ACCEPTANCE',
              source='Actual decoded native prototype geometry and binary16 UVs.',
              geometryPolicy='Only actual UV-reset slabs and finite terminal boundaries; no prototype triangle Boolean.',
              selectedFixture='No captured evaluated selected-road fixture available; no real generation count asserted.',
              texturePolicy='Original finite atlas and authored mip chain; never extract a periodic texture unit.',
              lods=reports)
(OUT/'recipe.json').write_text(json.dumps(report, indent=2)+'\n')
for lod in reports:
    print('LOD', lod['lod'], 'native triangles', lod['wholeMeshTriangles'])
    for row in lod['domains']:
        bad = [i for i,c in enumerate(row['cells']) if not all(a['feasible'] and a['float32CoefficientsInsideRoundingCells'] and a['float32EvaluationInsideRoundingCells'] for a in c['affineAxes'])]
        print(row['domain'], row['right'], row['positive'], 'faces', row['sourceFaces'], 'cells', len(row['cells']), 'rounding failures', bad, 'continuous', row['continuous'])
