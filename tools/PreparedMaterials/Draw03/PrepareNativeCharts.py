"""Extract native Draw03 chart membership, without inventing per-face repeat units.

Run from any directory. Outputs evidence/candidate arrays, never changes assets.
Physical edges join only when both position and UV match exactly. The chart keeps
the source triangles and boundary vertex indices; an arbitrary bounding box is not
advertised as a repeat region. Per-face density frames remain separate.
"""
from pathlib import Path
import collections
import hashlib
import json
import re
import struct
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/prepared-materials/Draw03/native-charts'


def charts(positions, uv, faces):
    # Matching attribute values, rather than mesh indices, joins authored normal
    # splits while retaining genuine UV seams and disconnected physical copies.
    _, welded = np.unique(np.column_stack((positions, uv)), axis=0, return_inverse=True)
    # Never weld a UV chart across disconnected authored topology, even if two
    # touching parts happen to have identical endpoint positions and UVs.
    vertex_parents=list(range(len(positions)))
    def vertex_root(a):
        while vertex_parents[a]!=a:
            vertex_parents[a]=vertex_parents[vertex_parents[a]]
            a=vertex_parents[a]
        return a
    for face in faces:
        a=vertex_root(int(face[0]))
        for b in face[1:]:vertex_parents[vertex_root(int(b))]=a
    parents = list(range(len(faces)))
    def root(a):
        while parents[a] != a:
            parents[a] = parents[parents[a]]
            a = parents[a]
        return a
    edges = collections.defaultdict(list)
    for fi, face in enumerate(faces):
        for i in range(3):
            a, b = int(face[i]), int(face[(i+1) % 3])
            x, y = int(welded[a]), int(welded[b])
            if x != y:
                edges[vertex_root(a),min(x,y),max(x,y)].append((fi, a, b, x, y))
    ambiguous = 0
    for uses in edges.values():
        if len(uses) == 2 and uses[0][3:] == uses[1][3:][::-1]:
            parents[root(uses[1][0])] = root(uses[0][0])
        elif len(uses) > 2:
            ambiguous += 1
    groups = collections.defaultdict(list)
    for fi in range(len(faces)):
        groups[root(fi)].append(fi)
    ordered = sorted(groups.values(), key=lambda fs: fs[0])
    face_chart = np.empty(len(faces), dtype='<i4')
    for ci, group in enumerate(ordered):
        face_chart[group] = ci
    boundaries = [[] for _ in ordered]
    for uses in edges.values():
        internal = len(uses) == 2 and uses[0][3:] == uses[1][3:][::-1]
        if not internal:
            for fi, a, b, _, _ in uses:
                boundaries[int(face_chart[fi])].extend((a,b))
    records = []
    for ci, group in enumerate(ordered):
        q = uv[faces[group]].astype(np.float64)
        low, high = q.min((0,1)), q.max((0,1))
        span = high-low
        e,f = q[:,1]-q[:,0],q[:,2]-q[:,0]
        cross = e[:,0]*f[:,1]-e[:,1]*f[:,0]
        area = np.abs(cross).sum() / 2
        boundary = boundaries[ci]
        rectangle = bool(np.all(span > 0) and boundary)
        # All boundary segments must belong to the actual box perimeter. Check
        # interval coverage on each side, not just area/bounding-box equality.
        sides = [[] for _ in range(4)]
        if rectangle:
            for a,b in np.asarray(boundary).reshape(-1,2):
                edge = uv[[a,b]].astype(np.float64)
                matched = False
                for axis in range(2):
                    for end in range(2):
                        if np.all(edge[:,axis] == (low,high)[end][axis]):
                            sides[axis*2+end].append(sorted(edge[:,1-axis].tolist()))
                            matched = True
                            break
                    if matched: break
                if not matched:
                    rectangle = False
                    break
        if rectangle:
            for side, intervals in enumerate(sides):
                axis = 1-side//2
                current = low[axis]
                for a,b in sorted(intervals):
                    if a != current or b <= a:
                        rectangle = False
                        break
                    current = b
                if current != high[axis]: rectangle = False
            rectangle &= abs(area-np.prod(span)) <= max(1e-14,np.prod(span)*1e-10)
        records.append({'faces': group, 'boundary': boundary,
                        'bounds': np.r_[low,high].astype('<f4'),
                        'rectangle': bool(rectangle), 'zeroUvArea': bool(area == 0)})
    return face_chart, records, ambiguous


def run():
    OUT.mkdir(parents=True, exist_ok=True)
    refs = {}
    for file in (ROOT/'src/BridgeBuilder/Bridges/Types/Draw03').glob('*.cs'):
        for name,cid,body in re.findall(r'Create(?:<[^>]+>)?\("(DrawBridge03[^\"]*)", "([a-f0-9]{32})", new\[\] \{ ([^}]+) \}',file.read_text(encoding='utf-8-sig')):
            refs[name] = cid,re.findall(r'"([a-f0-9]{32})"',body)
    report = {'schema':'Draw03NativeCharts/1','meshes':[],'newTexturePayloads':0,
              'materialMode':'original Surface with its unchanged complete native mip dependency graph',
              'nativeSamplingVerified':False}
    for name,(geometry_cid,surfaces) in sorted(refs.items()):
        path = ROOT/'artifacts/uv-density/native'/(name+'.npz')
        if not path.exists(): path = ROOT/'artifacts/uv-density/native-additional'/(name+'.npz')
        data = np.load(path)
        for mi,surface_cid in enumerate(surfaces):
            prefix = f'mesh{mi}_'
            p,f = data[prefix+'positions'],data[prefix+'faces']
            if prefix+'uv0' not in data: continue
            uv = data[prefix+'uv0'][:,:2]
            fc,records,ambiguous = charts(p,uv,f)
            # BBN1: schema, face count, chart count; face->chart; each chart's
            # flags, boundary edge count, original vertex index pairs. Bounds
            # are independent F32 arrays. Source face geometry is not duplicated.
            ints = [1,len(fc),len(records),*fc.tolist()]
            floats = []
            for r in records:
                ints.extend([int(r['rectangle']) | (int(r['zeroUvArea']) << 1),len(r['boundary'])//2,*r['boundary']])
                floats.extend(r['bounds'])
            blob = struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,len(floats),0)
            blob += np.asarray(ints,dtype='<i4').tobytes()+np.asarray(floats,dtype='<f4').tobytes()
            blob += hashlib.sha256(blob).digest()
            filename = name.replace(' ','_')+f'_{mi}.bin'
            (OUT/filename).write_bytes(blob)
            # Independently check serialized membership, flag and boundary offsets.
            counts = struct.unpack_from('<6i',blob,8)
            loaded = np.frombuffer(blob,'<i4',counts[0],32)
            assert np.array_equal(loaded[3:3+len(fc)],fc)
            at = 3+len(fc)
            for r in records:
                count = int(loaded[at+1])*2
                assert np.array_equal(loaded[at+2:at+2+count],r['boundary'])
                at += 2+count
            assert at == len(loaded) and hashlib.sha256(blob[:-32]).digest() == blob[-32:]
            row = {'render':name,'geometryCid':geometry_cid,'mesh':mi,'surfaceCid':surface_cid,
                   'sourceNpZSha256':hashlib.sha256(path.read_bytes()).hexdigest(),
                   'sourceFaces':len(f),'charts':len(records),'rectangleCharts':sum(r['rectangle'] for r in records),
                   'rectangleFaces':sum(len(r['faces']) for r in records if r['rectangle']),
                   'zeroUvCharts':sum(r['zeroUvArea'] for r in records),
                   'ambiguousPhysicalEdges':ambiguous,'boundaryEdges':sum(len(r['boundary'])//2 for r in records),
                   'file':filename,'bytes':len(blob),'sha256':hashlib.sha256(blob).hexdigest()}
            report['meshes'].append(row)
            print(name,mi,len(f),len(records),row['rectangleCharts'],flush=True)
    report['totals'] = {k:sum(r[k] for r in report['meshes']) for k in ('sourceFaces','charts','rectangleCharts','rectangleFaces','zeroUvCharts','ambiguousPhysicalEdges','boundaryEdges','bytes')}
    report['totals']['meshes'] = len(report['meshes'])
    (OUT/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report['totals']))


if __name__ == '__main__': run()
