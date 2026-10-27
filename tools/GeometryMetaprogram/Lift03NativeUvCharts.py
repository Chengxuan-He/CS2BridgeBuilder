"""Extract exact Lift03 prototype UV charts and source-native sampler bindings.

UV topology is identified offline. This does not infer texture periodicity from
pixel similarity, bake images, modify geometry, or publish candidate resources.
"""
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/PreparedMaterials/Lift03'))
from ExportRecipes import numbers

OUT = ROOT / 'artifacts/prepared-materials/Lift03/native-charts'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def text(value):
    raw = value.encode()
    size = len(raw)
    prefix = bytearray()
    while size >= 128:
        prefix.append((size & 127) | 128)
        size >>= 7
    return prefix + bytes([size]) + raw


def source_records():
    result = {}
    for name in ('Lift03SourceRenders.cs', 'Lift03PillarSources.cs'):
        code = (ROOT / 'src/BridgeBuilder/Bridges/Types/Lift03' / name).read_text()
        for render, geometry, surfaces in re.findall(
                r'Create\("([^"]+)", "([a-f0-9]{32})", new\[\] \{ ([^}]+) \}', code):
            result[render] = (geometry, re.findall('"([a-f0-9]{32})"', surfaces))
    return result


def meshes(payload):
    version, count = struct.unpack_from('<ii', payload)
    assert version == 2
    offset = 8
    def string():
        nonlocal offset
        count = struct.unpack_from('<i', payload, offset)[0]
        offset += 4
        value = payload[offset:offset+count].decode()
        offset += count
        return value
    seen = set()
    for _ in range(count):
        name, implementation = string(), string()
        family, lod, mesh, vertices, indices = struct.unpack_from('<5i', payload, offset)
        offset += 20
        positions = np.frombuffer(payload, '<f4', vertices*3, offset).reshape(-1, 3)
        uv = np.frombuffer(payload, '<f4', vertices*2, offset+12*vertices).reshape(-1, 2)
        faces = np.frombuffer(payload, '<i4', indices, offset+20*vertices).reshape(-1, 3)
        offset += vertices*20 + indices*4
        if implementation.startswith('Lift03') and (name, mesh) not in seen:
            seen.add((name, mesh))
            yield name, lod, mesh, uv, faces, positions
    assert offset == len(payload)


def charts(uv, faces, positions):
    points, vertex_map = np.unique(uv, axis=0, return_inverse=True)
    triangles, inverse = np.unique(np.sort(vertex_map[faces], axis=1), axis=0, return_inverse=True)
    q = points[triangles].astype(np.float64)
    cross = (q[:, 1, 0]-q[:, 0, 0])*(q[:, 2, 1]-q[:, 0, 1]) - (q[:, 1, 1]-q[:, 0, 1])*(q[:, 2, 0]-q[:, 0, 0])
    valid = np.isfinite(cross) & (cross != 0)
    triangles = triangles.copy()
    triangles[cross < 0] = triangles[cross < 0][:, (0, 2, 1)]
    # A source chart is connected through real source XYZ+UV edges. Coincident
    # atlas edges on separate physical parts do not establish adjacency.
    native, native_map = np.unique(np.concatenate((positions,uv),axis=1),axis=0,return_inverse=True)
    physical, first, physical_inverse = np.unique(np.sort(native_map[faces],axis=1),axis=0,return_index=True,return_inverse=True)
    physical_uv = native[physical,3:].astype(np.float64)
    pcross = ((physical_uv[:,1,0]-physical_uv[:,0,0])*(physical_uv[:,2,1]-physical_uv[:,0,1])
             -(physical_uv[:,1,1]-physical_uv[:,0,1])*(physical_uv[:,2,0]-physical_uv[:,0,0]))
    physical=physical.copy();physical[pcross<0]=physical[pcross<0][:,(0,2,1)]
    edges=defaultdict(list)
    for i in np.flatnonzero(pcross!=0):
        a,b,c=map(int,physical[i])
        for x,y in ((a,b),(b,c),(c,a)):
            edges[min(x,y),max(x,y)].append((int(i),x,y))
    parent=list(range(len(physical)))
    def root(i):
        while parent[i]!=i:
            parent[i]=parent[parent[i]];i=parent[i]
        return i
    for uses in edges.values():
        if len(uses)==2 and uses[0][1:]==uses[1][1:][::-1]:
            parent[root(uses[1][0])]=root(uses[0][0])
    physical_groups=defaultdict(list)
    for i in np.flatnonzero(pcross!=0):physical_groups[root(int(i))].append(int(i))
    # Complete identical chart regions may share metadata, without enlarging
    # any part's region by joining unrelated neighboring UV triangles.
    groups=defaultdict(list)
    for group in physical_groups.values():
        members=tuple(sorted(set(int(inverse[first[i]]) for i in group)))
        groups[members].extend(group)
    result=[];physical_chart=np.full(len(physical),-1,dtype=np.int32)
    for members,physical_members in sorted(groups.items()):
        members=list(members)
        cid=len(result);physical_chart[physical_members]=cid
        local = defaultdict(list)
        for i in members:
            a, b, c = map(int, triangles[i])
            for x, y in ((a, b), (b, c), (c, a)):
                local[min(x, y), max(x, y)].append((x, y))
        boundary = []
        valid_edges = True
        for uses in local.values():
            if len(uses) == 1:
                boundary.append(uses[0])
            elif len(uses) != 2 or uses[0] != uses[1][::-1]:
                valid_edges = False
                oriented = Counter(uses)
                for edge, count in oriented.items():
                    boundary.extend([edge] * max(0, count-oriented[edge[::-1]]))
        selected = points[np.unique(triangles[members])]
        low, high = selected.min(0), selected.max(0)
        rectangle = valid_edges and bool(np.all(high > low))
        sides = [[], [], [], []]
        for a, b in boundary:
            p, q = points[a], points[b]
            if p[0] == q[0] and p[0] in (low[0], high[0]):
                sides[int(p[0] == high[0])].append(sorted((float(p[1]), float(q[1]))))
            elif p[1] == q[1] and p[1] in (low[1], high[1]):
                sides[2+int(p[1] == high[1])].append(sorted((float(p[0]), float(q[0]))))
            else:
                rectangle = False
        for i, intervals in enumerate(sides):
            intervals.sort()
            axis = 1 if i < 2 else 0
            if not intervals or intervals[0][0] != low[axis] or intervals[-1][1] != high[axis]:
                rectangle = False
            if any(a[1] != b[0] for a, b in zip(intervals, intervals[1:])):
                rectangle = False
        result.append(dict(id=cid, triangles=members, boundary=boundary,
                           bounds=[*map(float, low), *map(float, high)],
                           exactRectangle=rectangle, periodicityProven=False,
                           topologyEdgesPaired=valid_edges))
    return points, triangles, physical_chart[physical_inverse], result, int((~valid[inverse]).sum())


def sampler_domains(render, mesh, uv, face_charts, surface, digest):
    path = ROOT / 'assets/HardcodedNumbers/Lift03' / ('UvDomain_'+render.replace(' ', '_')+'_'+str(mesh)+'.bin')
    n = numbers(path)
    from MovableUvDomainFormat import decode_arrays
    data, coords = decode_arrays(n['I32'], n['F32'])
    assert data[0] == -2
    at, coordinate = 2, 0
    result = []
    for index in range(data[1]):
        a, b, c, count = data[at:at+4]
        at += 4
        triangle = count < 0
        members = data[at:at+abs(count)]
        at += abs(count)
        if triangle:
            raw = uv[[a, b, c]].astype('<f4').tobytes()
        else:
            raw = struct.pack('<4f', *coords[coordinate:coordinate+4])
            coordinate += 4
        key = sha(text('BridgeBuilder/StructuralRepeat/1')+text(digest)+bytes([triangle])+raw) if digest else None
        result.append(dict(domain=index, sourceCid=surface, productionUnitKey=key,
                           charts=sorted(set(int(face_charts[f]) for f in members if face_charts[f] >= 0)),
                           degenerateFaces=sum(int(face_charts[f] < 0) for f in members),
                           sourceFaceCount=len(members)))
    assert at == len(data) and coordinate == len(coords)
    return result


def bbn(points, triangles, regions):
    # Version, vertex count, triangle count, chart count; chart offset/count pairs;
    # triangle indices and exact oriented boundary indices; original Float32 UVs.
    integers = [1, len(points), len(triangles), len(regions)]
    records = []
    for chart in regions:
        flags = int(chart['exactRectangle']) | (int(chart['topologyEdgesPaired']) << 1)
        records.extend([len(chart['triangles']), len(chart['boundary']), flags])
        records.extend(chart['triangles'])
        records.extend(v for edge in chart['boundary'] for v in edge)
    integers += triangles.reshape(-1).tolist() + records
    raw = bytearray(b'BBN1') + struct.pack('<7i', 1, len(integers), 0, 0, 0, points.size, 0)
    raw += np.asarray(integers, dtype='<i4').tobytes() + points.astype('<f4').tobytes()
    return raw + hashlib.sha256(raw).digest()


def main():
    mapping = json.loads((ROOT/'artifacts/prepared-materials/Lift03/shared-source-map/mapping.json').read_text())
    fixture = ROOT/'artifacts/uv-density/layout-plans.bin'
    data = fixture.read_bytes()
    assert sha(data) == 'cc253167a7916408d6471aebeb08d797c9821610ff843fecf582373c333811e2'
    sources = source_records()
    OUT.mkdir(parents=True, exist_ok=True)
    streams, key_uses = [], defaultdict(list)
    for render, lod, mesh, uv, faces, positions in meshes(data):
        geometry, surfaces = sources[render]
        surface = surfaces[mesh]
        points, triangles, face_charts, regions, degenerates = charts(uv, faces, positions)
        name = render.replace(' ', '_')+'_'+str(mesh)
        payload = bbn(points, triangles, regions)
        (OUT/(name+'.bin')).write_bytes(payload)
        domains = sampler_domains(render, mesh, uv, face_charts, surface,
                                  mapping['sources'][surface]['sourceDigest'] if surface in mapping['sources'] else None)
        for domain in domains:
            if domain['productionUnitKey']:
                key_uses[domain['productionUnitKey']].append(dict(stream=name, domain=domain['domain'], charts=domain['charts']))
        row = dict(name=render, stream=name, lod=lod, mesh=mesh, geometryCid=geometry, sourceCid=surface,
                   nativeUvDigest=sha(uv.tobytes()), nativeIndicesDigest=sha(faces.tobytes()),
                   adjacency="exact-source-XYZ-and-UV-edge", nativePositionDigest=sha(positions.tobytes()),
                   rawFaces=len(faces), uvDegenerateFaces=degenerates,
                   chartCount=len(regions), uniqueUvTriangles=len(triangles),
                   rectangles=sum(c['exactRectangle'] for c in regions),
                   bbnBytes=len(payload), bbnSha256=sha(payload),
                   charts=regions, domains=domains)
        (OUT/(name+'.json')).write_text(json.dumps(row, separators=(',',':'))+'\n')
        streams.append({k:v for k,v in row.items() if k not in ('charts','domains')})
        print(name, 'charts', len(regions), 'rectangles', row['rectangles'], 'bytes', len(payload), flush=True)
    bindings = [dict(key=s['productionUnitKey'], sourceCid=s['sourceCid'], uses=key_uses[s['productionUnitKey']])
                for s in mapping['samplers']]
    missing = [x['key'] for x in bindings if not x['uses']]
    (OUT/'sampler-chart-bindings.json').write_text(json.dumps(bindings, separators=(',',':'))+'\n')
    report = dict(style='Lift03', fixtureSha256=sha(data), sourceInventorySha256=mapping['inventorySha256'],
                  streams=streams, samplerCount=len(bindings), mappedSamplers=len(bindings)-len(missing), unmapped=missing,
                  nativePixelsCreated=0, formalAssetsChanged=False,
                  nativeTextureTreatment='Original native Surface and complete builtin VT graph remain direct references.',
                  periodicity='Exact chart rectangles prove source region coverage, not authored image periodicity.',
                  runtimeRequirement='Use actual receiver UV inside measured chart union; verify gradient, seams, normals and all mips. '
                                     'The runtime branch does not infer repetition or split receivers.')
    (OUT/'report.json').write_text(json.dumps(report, indent=2)+'\n')
    print('Samplers mapped:', len(bindings)-len(missing), '/', len(bindings))


if __name__ == '__main__':
    main()
