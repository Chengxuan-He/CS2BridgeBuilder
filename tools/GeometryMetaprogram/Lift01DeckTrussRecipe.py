"""Compile reviewed deck domains and original physical-boundary subdivisions.

Reads the candidate's measured source faces and the original production Plan's
edge provenance. All classification occurs offline. No texture-period cuts or
additional triangles are introduced; centre/edge copies retain source provenance.
"""
from pathlib import Path
import json, struct, hashlib, sys
import numpy as np

research, repo = map(Path, sys.argv[1:])
out = repo / 'artifacts/material-remap/Lift01/truss62'
half = 3.39190673828125
receipts = []
for lod in (0, 1):
    raw = np.load(research / f'lift01-main-{lod}.npz')
    v, faces = raw['positions'], raw['indices']
    n = len(v)
    domains = json.loads((out / f'deck-domains-lod{lod}.json').read_text())
    plan = json.loads((out / f'original-plan-lod{lod}.json').read_text())
    existing = {f['face']: f['indices'] for f in plan['fragments']}
    added = np.array([v[e['a']].astype(float) +
                      (v[e['b']].astype(float)-v[e['a']])*e['t'] for e in plan['edges']])
    points = np.concatenate((v, added))
    center_faces, edge_faces = set(domains['centerFaces']), set(domains['edgeFaces'])
    parent_polys, cv, ev = {}, set(), set()
    parent_plane = {}
    higher = set()
    for member in domains['parts']:
        plane = 3.3919677734375 if member['ordinal'] in (377,380,381,384,385,525,528,529,532,533) and member['family']=='crossing' else half
        for parent in member['centerFaces'] + member['edgeFaces']:
            parent_plane[parent] = plane
    for parent in sorted(center_faces | edge_faces):
        ix = existing.get(parent, faces[parent].tolist())
        triangles = np.array(ix).reshape(-1, 3)
        regions = []
        for tri in triangles:
            if parent in existing:
                # Existing Plan subdivisions lie on the measured inner upright
                # planes. The central polygon includes X=0 or interior X; outer
                # polygons are entirely beyond the upright's inner surface.
                x = points[tri, 0]
                central = abs(float(x.mean())) < parent_plane[parent]
            else:
                central = parent in center_faces
            (cv if central else ev).update(map(int, tri))
            if central and parent_plane[parent] != half: higher.update(map(int,tri))
            regions.append((central, tri.tolist()))
        parent_polys[parent] = regions
    shared = sorted((cv & ev) & set(range(n)))
    duplicates = sorted(i-n for i in cv if i >= n)
    copy_index = {i:k for k,i in enumerate(shared)}
    duplicate_index = {i:k for k,i in enumerate(duplicates)}
    fragments = []
    for parent, regions in parent_polys.items():
        ix = []
        for central, tri in regions:
            for i in tri:
                if central and i in copy_index:
                    ix.append(-1-copy_index[i])
                elif central and i >= n:
                    ix.append(-1-len(shared)-duplicate_index[i-n])
                else:
                    ix.append(i)
        if parent in existing or ix != faces[parent].tolist():
            fragments.append((parent, ix))
    pure = sorted((cv-ev) & set(range(n)))
    outer = sorted(ev & set(range(n)))
    y_members = [m for m in domains['parts'] if m['family']=='crossing' and m['maximum'][0] == 3.62957763671875]
    y_ids = sorted(set(int(i) for m in y_members for face in m['centerFaces'] for i in faces[face]))
    assert set(y_ids) <= set(pure)
    end_ids = sorted(set(int(i) for m in domains['parts'] if m['family']=='crossing' and m['ordinal'] in (492,639) for face in m['centerFaces'] for i in faces[face]))
    ints = [len(pure),len(outer),len(shared),len(duplicates),len(fragments),len(y_ids),len(higher),len(end_ids)]
    ints += pure + outer + shared + duplicates
    for parent, ix in fragments:
        ints += [parent,len(ix)]+ix
    ints += y_ids + sorted(higher) + end_ids
    payload = struct.pack('<8i',0x314e4242,1,len(ints),0,0,0,4,0)
    payload += struct.pack('<'+'i'*len(ints),*ints)+struct.pack('<4f',half,3.56268310546875,3.3919677734375,5.593505859375)
    payload += hashlib.sha256(payload).digest()
    name = f'Types_Lift01_DeckTruss{lod}'
    (repo/f'assets/HardcodedNumbers/Lift01/{name}.bin').write_bytes(payload)
    receipt = dict(lod=lod, pureCenterVertices=len(pure), edgeVertices=len(outer),
                   sourceCopies=len(shared), duplicateEdges=len(duplicates),
                   replacementParents=len(fragments), halfSpan=half,
                   selectedSourceFaces=len(parent_polys),
                   finalSelectedFaces=sum(len(x)//3 for _,x in fragments)+len(parent_polys)-len(fragments),
                   originalSelectedFaces=sum(len(existing.get(p,faces[p].tolist()))//3 for p in parent_polys))
    assert receipt['finalSelectedFaces'] == receipt['originalSelectedFaces']
    receipts.append(receipt)
(out/'deck-recipe.json').write_text(json.dumps(receipts,indent=2),encoding='utf-8')
print(receipts)
