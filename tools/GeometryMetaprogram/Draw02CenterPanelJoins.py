"""Record the original under-road panel edges adjoining the fixed centre girder."""
from pathlib import Path
import json
import numpy as np

R = Path(__file__).resolve().parents[2]
O = R / 'artifacts/material-remap/Draw02/edge55'
O.mkdir(exist_ok=True)
for lod in range(2):
    d = np.load(R / f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz')
    v, f = d['mesh0_positions'], d['mesh0_indices'].reshape(-1, 3)
    p = v[f]
    normal = np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0])
    # The four authored downward panel strips, two per leaf, between the
    # centre longitudinal girder and its nearest narrow longitudinal rib.
    faces = np.flatnonzero((p[:, :, 1] == np.float32(9.699951171875)).all(1)
        & (abs(p[:, :, 0]).max(1) <= np.float32(1.29998779296875))
        & (normal[:, 1] < 0))
    members = np.unique(f[faces])
    vertices = members[abs(v[members, 0]) < .201]
    assert len(faces) == 160 and len(vertices) == 160
    center = json.loads((R / f'artifacts/material-remap/Draw02/underside52/lod{lod}-center-members.json').read_text())
    center_positions = {tuple(q) for q in v[center['vertices']]}
    matched = [int(i) for i in vertices if tuple(v[i]) in center_positions]
    # Four panel tips extend to the native leaf seam, beyond the girder ends.
    tips = sorted(set(map(int, vertices)) - set(matched))
    assert len(matched) == 156 and len(tips) == 4
    assert np.all(abs(v[tips, 2]) == np.float32(.029998779296875))
    assert not set(map(int, vertices)).intersection(center['vertices'])
    (O / f'lod{lod}-center-joins.json').write_text(json.dumps(dict(
        vertices=vertices.tolist(), faces=faces.tolist(), exactGirderPositionMatches=matched,
        nativeLeafTips=tips, invariant='Retain native inner edge X; outer rib edge follows its rigid displacement.'), indent=2))
