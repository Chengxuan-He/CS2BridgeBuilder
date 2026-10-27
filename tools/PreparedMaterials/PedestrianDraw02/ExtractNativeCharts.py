"""Extract exact shared-edge native UV charts; do not replace runtime geometry."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct

import numpy as np

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('repository', type=Path)
p.add_argument('fixture', type=Path)
p.add_argument('output', type=Path)
a = p.parse_args()
root = a.repository.resolve()
code = (root/'src/BridgeBuilder/Bridges/Types/PedestrianDraw02/PedestrianDraw02SourceRenders.cs').read_text()
bindings = {name: re.findall(r'"([0-9a-f]+)"', values) for name, values in
            re.findall(r'Create\("([^"]+)", "[0-9a-f]+", new\[\] \{ ([^}]+) \}', code)}
samplers = json.loads((root/'artifacts/prepared-materials/PedestrianDraw02/source-consolidation/source-sampling.json').read_text())
source_graph = {s['cid']: s for s in samplers['sources']}
records, payload = [], {}
with a.fixture.open('rb') as f:
    def ints(n):
        return struct.unpack('<'+'i'*n, f.read(4*n))
    version, count = ints(2)
    assert version == 2
    for _ in range(count):
        def text():
            return f.read(ints(1)[0]).decode()
        name, implementation = text(), text()
        family, lod, mesh, vertices, indices = ints(5)
        positions = np.frombuffer(f.read(vertices*12),dtype='<f4').reshape(-1,3).copy()
        uv = np.frombuffer(f.read(vertices*8),dtype='<f4').reshape(-1,2).copy()
        faces = np.frombuffer(f.read(indices*4),dtype='<i4').reshape(-1,3).copy()
        if not name.startswith('PedestrianDrawBridge02'):
            continue
        surface = bindings[name][mesh]
        parent = list(range(len(faces)))
        def find(i):
            while parent[i] != i:
                parent[i] = parent[parent[i]]
                i = parent[i]
            return i
        # Weld only exact position+UV endpoints. Equal UV alone would merge
        # disconnected objects; equal position alone would erase authored seams.
        vertex_keys = [tuple(float(x) for x in (*pos,*q)) for pos,q in zip(positions,uv)]
        edges = {}
        for fi, face in enumerate(faces):
            for j in range(3):
                v, w = int(face[j]), int(face[(j+1)%3])
                key = tuple(sorted((vertex_keys[v],vertex_keys[w])))
                edges.setdefault(key,[]).append((fi,v,w))
        for uses in edges.values():
            # Nonmanifold edges are retained as boundaries for later inspection.
            if len(uses) == 2 and uses[0][0] != uses[1][0]:
                parent[find(uses[1][0])] = find(uses[0][0])
        groups = {}
        for fi in range(len(faces)):
            groups.setdefault(find(fi),[]).append(fi)
        face_chart = np.empty(len(faces),dtype='<i4')
        charts = []
        for ci, members in enumerate(groups.values()):
            face_chart[members] = ci
            q = uv[faces[members]].astype(float)
            cross = (q[:,1,0]-q[:,0,0])*(q[:,2,1]-q[:,0,1])-(q[:,1,1]-q[:,0,1])*(q[:,2,0]-q[:,0,0])
            charts.append(dict(id=ci,faces=members,bounds=[*q.reshape(-1,2).min(0),*q.reshape(-1,2).max(0)],
                uvArea=float(abs(cross).sum()/2),positiveFaces=int((cross>0).sum()),
                negativeFaces=int((cross<0).sum()),degenerateFaces=int((cross==0).sum())))
        boundaries = []
        for uses in edges.values():
            if len(uses) != 2:
                boundaries.extend([int(face_chart[fi]),fi,v,w] for fi,v,w in uses)
        prefix = f'mesh{len(records)}'
        payload.update({prefix+'_positions':positions,prefix+'_uv':uv,prefix+'_faces':faces,
                        prefix+'_face_chart':face_chart,prefix+'_boundary_edges':np.array(boundaries,dtype='<i4').reshape(-1,4)})
        records.append(dict(name=name,implementation=implementation,family=family,lod=lod,mesh=mesh,
            sourceSurfaceCid=surface,sourceGraph=source_graph.get(surface),arrayPrefix=prefix,
            vertexCount=len(positions),faceCount=len(faces),chartCount=len(charts),boundaryEdges=len(boundaries),charts=charts))
    assert f.read() == b''
a.output.mkdir(parents=True,exist_ok=True)
np.savez_compressed(a.output/'native-charts.npz',**payload)
report = dict(style='PedestrianDraw02',fixtureSHA256=hashlib.sha256(a.fixture.read_bytes()).hexdigest(),
              sourceBindingsSHA256=hashlib.sha256(code.encode()).hexdigest(),records=records,
              sharedSourceChannelImages=samplers['sourceChannelImages'],
              summary=dict(meshStreams=len(records),faces=sum(r['faceCount'] for r in records),charts=sum(r['chartCount'] for r in records)),
              limits=['Exact source chart adjacency and boundaries only; no chart has been declared repeatable from its bounding box.',
                      'Original face indices, UVs and positions are retained; no subdivision or geometry replacement is performed.',
                      'Mapped stretched receivers must be checked against actual chart triangles, not the rectangle bounds alone.',
                      'Native source mip/filter/normal and emitted output still require implementation verification.'])
(a.output/'native-charts.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report['summary']))
