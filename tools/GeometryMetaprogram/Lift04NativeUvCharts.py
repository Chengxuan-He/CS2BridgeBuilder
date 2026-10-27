"""Extract Lift04 native UV chart boundaries without creating sampled images.

Existing UV triangles, exact shared edges and duplicate atlas aliases define the
charts. A bounding rectangle alone never proves a usable source region.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import numpy as np


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def uvkey(values):
    return tuple(struct.pack('<f', float(v)).hex() for v in np.asarray(values).flat)


def cross2(a, b):
    return float(a[0] * b[1] - a[1] * b[0])


def packed_charts(meshes):
    integers = [-41, len(meshes)]
    values = []
    for mesh in meshes:
        integers.extend((len(mesh['uvPoints']), mesh['sourceFaces'], len(mesh['charts'])))
        integers.extend(mesh['faceChart'])
        values.extend(v for point in mesh['uvPoints'] for v in point)
        for chart in mesh['charts']:
            integers.extend((int(chart['exactRectangle']), int(chart['singleBoundaryLoop']),
                             len(chart['faces']), len(chart['sourceTriangles']), len(chart['boundaryEdges'])))
            integers.extend(chart['faces'])
            integers.extend(v for face in chart['sourceTriangles'] for v in face)
            integers.extend(v for edge in chart['boundaryEdges'] for v in edge)
    payload = b'BBN1' + struct.pack('<7i', 1, len(integers), 0, 0, 0, len(values), 0)
    payload += struct.pack('<' + 'i' * len(integers), *integers)
    payload += struct.pack('<' + 'f' * len(values), *values)
    return payload + hashlib.sha256(payload).digest()


def extract(uv, faces):
    points, remap = np.unique(np.asarray(uv[:, :2], dtype=np.float32), axis=0, return_inverse=True)
    triangles, aliases, known, degenerate = [], [], {}, []
    for face, corners in enumerate(remap[faces]):
        ids = tuple(sorted(map(int, corners)))
        a, b, c = points[list(ids)]
        cross = cross2((b-a).astype(np.float64), (c-a).astype(np.float64))
        if len(set(ids)) < 3 or cross == 0:
            degenerate.append(face)
            continue
        if ids in known:
            aliases[known[ids]].append(face)
            continue
        known[ids] = len(triangles)
        triangles.append(ids if cross > 0 else (ids[0], ids[2], ids[1]))
        aliases.append([face])
    edges = {}
    for face, ids in enumerate(triangles):
        for a, b in zip(ids, ids[1:] + ids[:1]):
            edges.setdefault(tuple(sorted((a, b))), []).append((face, a, b))
    parent = list(range(len(triangles)))

    def root(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for uses in edges.values():
        if len(uses) == 2 and uses[0][1:] == uses[1][1:][::-1]:
            parent[root(uses[1][0])] = root(uses[0][0])
    groups = {}
    for i in range(len(triangles)):
        groups.setdefault(root(i), []).append(i)
    charts, face_chart = [], np.full(len(faces), -1, dtype=np.int32)
    for group in groups.values():
        members = sorted(f for t in group for f in aliases[t])
        local_edges = {}
        area = 0.0
        for t in group:
            ids = triangles[t]
            a, b, c = points[list(ids)].astype(np.float64)
            area += cross2(b-a, c-a) / 2
            for a, b in zip(ids, ids[1:] + ids[:1]):
                local_edges.setdefault(tuple(sorted((a, b))), []).append((a, b))
        boundary = [uses[0] for uses in local_edges.values() if len(uses) == 1]
        boundary_points = points[sorted({i for edge in boundary for i in edge})]
        low, high = boundary_points.min(0), boundary_points.max(0)
        next_vertex = {a: b for a, b in boundary}
        simple = len(next_vertex) == len(boundary) and len({b for _, b in boundary}) == len(boundary)
        loop = []
        if simple and boundary:
            current = boundary[0][0]
            while current not in loop:
                loop.append(current)
                current = next_vertex.get(current)
                if current is None:
                    break
            simple = current == loop[0] and len(loop) == len(boundary)
        rectangle = False
        if simple:
            # Boundary cancellation plus one closed non-overlapping side cycle
            # supplies coverage; equal area alone would not.
            on_sides = all((points[a][0] == points[b][0] and points[a][0] in (low[0], high[0])) or
                           (points[a][1] == points[b][1] and points[a][1] in (low[1], high[1])) for a, b in boundary)
            rectangle = bool(on_sides and area == float(np.prod((high-low).astype(np.float64))))
        chart_id = len(charts)
        face_chart[members] = chart_id
        charts.append(dict(id=chart_id, faces=members, sourceTriangles=[list(triangles[t]) for t in group],
                           boundaryEdges=[list(e) for e in boundary], bounds=[*map(float, low), *map(float, high)],
                           exactRectangle=rectangle, singleBoundaryLoop=simple,
                           pixelSampling='OriginalNativeSurface', uvArea=area))
    if sorted([f for c in charts for f in c['faces']] + degenerate) != list(range(len(faces))):
        raise ValueError('Native chart coverage is incomplete')
    return points, charts, face_chart, degenerate


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    catalog = json.loads((args.repo / 'artifacts/prepared-materials/Lift04/shared-sampling/native-bindings.json').read_text())
    sampler_map = json.loads((args.repo / 'artifacts/prepared-materials/Lift04/shared-sampling/map.json').read_text())
    meshes, candidates = [], {}
    for render in catalog['renderBindings']:
        path = args.repo / 'artifacts/uv-density/native' / (render['render'] + '.npz')
        with np.load(path) as native:
            for slot, source in enumerate(render['originalSurfaceCids']):
                uv, faces = native[f'mesh{slot}_uv0'], native[f'mesh{slot}_faces']
                points, charts, face_chart, degenerate = extract(uv, faces)
                index = len(meshes)
                meshes.append(dict(render=render['render'], mesh=slot, sourceSurfaceCid=source,
                                   sourceNPZSHA256=sha(path), sourceFaces=len(faces), uvPoints=points.tolist(),
                                   charts=charts, faceChart=face_chart.tolist(), degenerateFaces=degenerate))
                for chart in charts:
                    for triangle in chart['sourceTriangles']:
                        signature = tuple(sorted(uvkey(points[i]) for i in triangle))
                        candidates.setdefault((source, signature), set()).add((index, chart['id']))
    sampling = []
    for sampler in sampler_map['samplers']:
        signature = tuple(sorted(uvkey(sampler[k]) for k in ('a', 'b', 'c')))
        matches = sorted(candidates.get((sampler['sourceCid'], signature), set()))
        sampling.append(dict(samplingKey=sampler['key'], sourceSurfaceCid=sampler['sourceCid'],
                             candidates=[dict(mesh=m, chart=c) for m, c in matches],
                             status='MappedOriginalTriangle' if matches else 'NeedsExactDomainMembership'))
    report = dict(schema='Lift04/NativeUvCharts/1', style='Lift04', nativeBindings=catalog,
                  meshes=meshes, sampling=sampling,
                  summary=dict(meshes=len(meshes), sourceFaces=sum(m['sourceFaces'] for m in meshes),
                               charts=sum(len(m['charts']) for m in meshes),
                               exactRectangles=sum(c['exactRectangle'] for m in meshes for c in m['charts']),
                               degenerateFaces=sum(len(m['degenerateFaces']) for m in meshes),
                               samplingRecords=len(sampling), mappedSamplingRecords=sum(bool(s['candidates']) for s in sampling),
                               newTextureFiles=0, newPixelBytes=0))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, separators=(',', ':')) + '\n')
    binary = packed_charts(meshes)
    args.output.with_suffix('.bin').write_bytes(binary)
    print('BBN1 bytes=' + str(len(binary)))
    print(json.dumps(report['summary'], indent=2))


if __name__ == '__main__':
    main()
