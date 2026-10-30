"""Identify native finite VT terminal faces independently of generated receivers."""
import hashlib
import json
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, domains, read_bbn
from shapely.geometry import Polygon
from shapely.ops import unary_union


def evidence():
    paths = [ROOT / 'artifacts/uv-density/native' / (name + '.npz')
             for name in ('DrawBridge01 Mesh', 'DrawBridge01_LOD1 Mesh')]
    near, middle = [np.load(p) for p in paths]
    baseline = json.loads((ROOT / 'artifacts/prepared-materials/Draw01/non-tiling/classification.json').read_text())
    for p in paths:
        record = next(r for r in baseline['meshes'] if r['render'] == p.stem and r['mesh'] == 0)
        assert hashlib.sha256(p.read_bytes()).hexdigest() == record['nativeSha256']
    near_positions = near['mesh0_positions'][near['mesh0_faces']]
    positions = middle['mesh0_positions'][middle['mesh0_faces']]
    uv = middle['mesh0_uv0'][middle['mesh0_faces'], :2]
    # Inspect every original face, not the finite test suite's remaining receivers.
    # This is ordinary object VT UV0, never the network shader's signed-V encoding.
    selected = np.flatnonzero(np.any(uv[:, :, 1] < 0, axis=1)).tolist()
    assert selected == [953, 1375, 1376]
    correction = ROOT / 'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01UndersideUv_cs.bin'
    correction_indices = read_bbn(correction)[0][178:336]
    assert not set(middle['mesh0_faces'][selected].reshape(-1)).intersection(correction_indices)
    assert not np.any(near['mesh0_uv0'][:, :2] < 0)
    records = []
    for face in selected:
        triangle = positions[face]
        assert np.all(triangle[:, 1] == triangle[0, 1])
        assert np.all((uv[face, :, 0] >= 0) & (uv[face, :, 0] <= 1))
        footprint = Polygon(triangle[:, [0, 2]])
        matches = [(i, Polygon(t[:, [0, 2]])) for i, t in enumerate(near_positions)
                   if np.all(t[:, 1] == triangle[0, 1]) and
                   Polygon(t[:, [0, 2]]).intersection(footprint).area > 0]
        assert matches and unary_union([p for _, p in matches]).covers(footprint)
        records.append(dict(face=face, vertices=middle['mesh0_faces'][face].tolist(),
            positions=triangle.tolist(), originalUv=uv[face].tolist(),
            nearFaces=[dict(face=i, positions=near_positions[i].tolist(),
                originalUv=near['mesh0_uv0'][near['mesh0_faces'][i], :2].tolist()) for i, _ in matches],
            interpretation='Finite authored underside terminal; native object VT clamps at atlas V=0. No native periodic remapping is encoded on this face. Near/middle triangulation and authored UVs differ.'))
    recipe = ROOT / 'assets/HardcodedNumbers/Draw01/UvDomain_DrawBridge01_LOD1_Mesh_0.bin'
    measured = domains(recipe)
    approved = [d['index'] for d in measured if set(d['faces']).issubset(selected)]
    assert approved == [757, 1028, 1029]
    assert sorted(f for d in measured if d['index'] in approved for f in d['faces']) == selected
    return dict(render='DrawBridge01_LOD1 Mesh', mesh=0, domains=approved,
        sourceSurface='97291ab7f7938fc35cc670b15f2bca36', template=1, nativeVT=True,
        nativeSources=[dict(path=str(p.relative_to(ROOT)), sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in paths],
        domainSha256=hashlib.sha256(recipe.read_bytes()).hexdigest(), faces=records,
        undersideCorrectionSha256=hashlib.sha256(correction.read_bytes()).hexdigest(),
        correctedVerticesOnSelectedFaces=0,
        sampling='Retain actual source UV0 without normalization or region expansion; original native VT Clamp preserves the authored finite edge footprint and mip graph. No extracted repeat unit is assigned.',
        limits='Source mapping/physical correspondence evidence, not rendered acceptance. Native LOD1 is intentionally retained, not repaired to match near-detail UVs.')


if __name__ == '__main__':
    out = ROOT / 'artifacts/material-resume/Draw01'
    out.mkdir(parents=True, exist_ok=True)
    result = evidence()
    (out / 'finite-terminal-faces.json').write_text(json.dumps(result, indent=2) + '\n')
    print('Verified finite terminal domains:', result['domains'])
