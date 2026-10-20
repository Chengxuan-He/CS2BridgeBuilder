"""Audit Draw03 support panel boundaries before reconstructing coarse LOD seams.

Usage: python Draw03SupportProfiles.py RESEARCH_DIRECTORY
Writes a research JSON with original face/vertex identities and exact coordinates.
This is evidence extraction, not a runtime classifier or a coarse widening recipe.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

r = Path(sys.argv[1])
hashes = (
    ('77ebf81e11324571d8e552f53aa32a258866ddb67376d41d06bdc397bf5a4a5b',
     '5b10921bd36c6c587c42e8b6d76c90bdc61e7ad23de4e155e07a295d4df3db3e',
     '68d7c2c7987bfa843071fe9085e53c45b878483c423d0c05c3066df0805a634f'),
    ('e0deea7435d42761f8ac5bf8274617587c0c55f5568ba913c7be053908b8eaba',
     'b69766bacb96f51545bf0dccd1928a5d6e0d4ec0de931bd1c1ea006f68e38b42',
     '70ac56aa24827d71a47a21cbaf820259448199eb7b2715485c7bbeaad05218f8'))
result = []
for part in range(2):
    levels = []
    for lod in range(3):
        name = 'DrawBridge03Pillar' + ('Base' if part else '') + (f'_LOD{lod}' if lod else '') + ' Mesh.Geometry'
        digest = hashlib.sha256((r / 'movable-skin' / name).read_bytes()).hexdigest()
        assert digest == hashes[part][lod]
        d = np.load(r / f'draw03-support-{part}-{lod}.npz')
        v, faces = d['positions'], d['indices'].reshape(-1, 3)
        centre_values = (0.,) if part else (-9.5367431640625e-7, -1.9073486328125e-6)
        centre = np.isin(v[:, 0], centre_values)
        panel_faces = np.flatnonzero(centre[faces].any(1))
        boundary = set()
        segments = set()
        for ordinal in panel_faces:
            face = faces[ordinal]
            side = [int(i) for i in face if not centre[i]]
            boundary.update(side)
            if len(side) == 2:
                segments.add(tuple(sorted(side)))
        crossing = np.flatnonzero((v[faces, 0].min(1) < -1e-4) &
                                  (v[faces, 0].max(1) > 1e-4))
        if lod < 2:
            assert len(panel_faces) == ((176, 127), (112, 80))[part][lod]
            assert len(crossing) == 0
        else:
            assert not centre.any()
            assert len(crossing) > 0
        levels.append(dict(lod=lod, source=name, sha256=digest,
                           vertices=len(v), triangles=len(faces),
                           centre_vertices=np.flatnonzero(centre).tolist(),
                           panel_faces=panel_faces.tolist(),
                           boundary_vertices=[dict(index=i, position=v[i].tolist()) for i in sorted(boundary)],
                           boundary_segments=[list(edge) for edge in sorted(segments)],
                           centre_crossing_faces=[dict(index=int(i), vertices=faces[i].tolist(),
                                                       positions=v[faces[i]].tolist()) for i in crossing]))
        print(f'{"Footing" if part else "Column"} LOD{lod}: {len(panel_faces)} centre-panel faces; '
              f'{len(crossing)} full-span crossing faces; {len(boundary)} boundary vertex records')
    result.append(dict(part='footing' if part else 'column', levels=levels))
(r / 'draw03-support-panel-profiles.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
