"""Inventory original complete parts before assigning assembly-wide truss maps.

Usage: python Lift01TrussInventory.py RESEARCH_DIRECTORY OUTPUT_DIRECTORY
This is offline evidence, not a runtime bounds classifier.
"""
import ast
import hashlib
import json
from pathlib import Path
import sys
import numpy as np

research, output = map(Path, sys.argv[1:])
output.mkdir(parents=True, exist_ok=True)
recipes = {}
for name in ('Bases', 'TransverseParts', 'Connectors', 'JoinedFrames',
             'TowerFrames', 'TowerBodies'):
    tree = ast.parse(Path(__file__).with_name('Lift01' + name + '.py').read_text())
    for node in ast.walk(tree):
        if (isinstance(node, ast.Assign) and isinstance(node.value, ast.List)
                and any(isinstance(t, ast.Name) and t.id == 'selected' for t in node.targets)):
            try:
                ids = ast.literal_eval(node.value)
            except (ValueError, TypeError):
                continue
            for index in ids:
                assert index not in recipes, (index, recipes.get(index), name)
                recipes[index] = name

for lod in (0, 1):
    data = np.load(research / f'lift01-main-{lod}.npz')
    positions, faces = data['positions'], data['indices']
    rows, owner = [], np.full(len(positions), -1, dtype=np.int32)
    for family in ('crossing', 'side'):
        parts = json.loads((research / f'lift01-{family}-{lod}.json').read_text())
        for ordinal, part in enumerate(parts):
            ids = np.array(part['vertices'], dtype=np.int32)
            assert np.all(owner[ids] == -1)
            owner[ids] = len(rows)
            rows.append(dict(family=family, ordinal=ordinal,
                             vertices=part['vertices'],
                             minimum=part['minimum'], maximum=part['maximum'],
                             currentRecipe=recipes.get(ordinal, 'Other')
                             if family == 'crossing' else 'RigidParts',
                             triangleCount=0))
    assert np.all(owner >= 0)
    face_owners = owner[faces]
    assert np.all(face_owners == face_owners[:, :1])
    counts = np.bincount(face_owners[:, 0], minlength=len(rows))
    for row, count in zip(rows, counts):
        row['triangleCount'] = int(count)
    source = research / 'movable-skin' / (
        'LiftBridge01' + ('_LOD1' if lod else '') + ' Mesh.Geometry')
    report = dict(lod=lod, sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                  sourceVertices=len(positions), sourceFaces=len(faces),
                  assignedFaces=int(counts.sum()), parts=rows)
    (output / f'original-members-lod{lod}.json').write_text(json.dumps(report), encoding='utf-8')
    print(f'LOD{lod}: {len(rows)} whole components, {int(counts.sum())} faces, exclusive coverage')
