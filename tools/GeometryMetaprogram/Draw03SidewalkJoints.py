"""Identify Draw03's four stepped road/sidewalk joint strips for layout adaptation.
Usage: python Draw03SidewalkJoints.py RESEARCH_DIRECTORY OUTPUT_JSON
This is offline identification. Do not classify native faces spatially at runtime.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

r, output = map(Path, sys.argv[1:])
levels = []
for lod, identities in enumerate(((705,1479,2176,2948),(745,1519,2264,3036))):
    name = 'DrawBridge03'+('_LOD1' if lod else '')+' Mesh.Geometry'
    digest = hashlib.sha256((r/'movable-skin'/name).read_bytes()).hexdigest()
    assert digest == ('f7ecc824ad55745e791e40b18fd12f5def20657ed8ea56d373429d5a1337588f',
                      '15b8419da13d6e99e1dc0591f12f4019f0be09c3b220a481d24720daf7b57e12')[lod]
    data = np.load(r/f'draw03-main-{lod}.npz')
    v, f = data['positions'], data['indices'].reshape(-1,3)
    parts = json.loads((r/f'draw03-parts-{lod}.json').read_text())
    groups = []
    for identity, bone in zip(identities,(1,0,2,0)):
        ids = np.array(parts[identity]['vertices'])
        incident = np.any(np.isin(f,ids),axis=1)
        complete = np.all(np.isin(f,ids),axis=1)
        assert np.array_equal(incident,complete)
        faces = np.flatnonzero(complete)
        assert len(faces) == (40,38)[lod]
        assert set(data['bones'][faces]) == {bone}
        points = v[f[faces]]
        left = points[:,:,0].max(1) <= -9.389892578125
        right = points[:,:,0].min(1) >= 9.389892578125
        centre = ~(left|right)
        assert left.sum() == right.sum() and np.all(points[centre,:,1] <= 10.009033203125)
        assert np.all(points[left|right,:,1] <= 10.208984375)
        # The top reaches the inner step; its lower bevel extends 0.01806640625 m.
        assert np.abs(points[centre,:,0]).max() == 9.407958984375
        assert not np.intersect1d(faces,np.load(r/f'draw03-road-replacement-{lod}.npz')['removed']).size
        groups.append(dict(part=identity,bone=bone,vertices=ids.tolist(),
                           leftFaces=faces[left].tolist(),rightFaces=faces[right].tolist(),
                           carriagewayFaces=faces[centre].tolist(),
                           minimum=v[ids].min(0).tolist(),maximum=v[ids].max(0).tolist()))
        print(f'LOD{lod} part {identity}: bone {bone}; left/carriageway/right faces {left.sum()}/{centre.sum()}/{right.sum()}; exclusive source vertices.')
    levels.append(dict(lod=lod,sha256=digest,groups=groups))
output.write_text(json.dumps(dict(nativeSidewalkWidth=3.5,outer=12.5,innerStep=9.389892578125,
                                 levels=levels),indent=2),encoding='utf-8')
