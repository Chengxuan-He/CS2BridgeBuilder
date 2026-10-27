"""Recover exact filled boundaries of physically connected native UV charts."""
from collections import Counter
from fractions import Fraction
import hashlib
import numpy as np
from ExportNativeCharts import normalized_triangle

def overlap(a, b):
    # Native UVs are dyadic rationals. Exact separating-axis predicates distinguish
    # touching edges from overlapping filled regions without area tolerances.
    def cross(a, b, c):
        return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    def separates(p, q):
        sign = 1 if cross(*p) > 0 else -1
        return any(all(sign*cross(x, y, v) <= 0 for v in q)
            for x, y in zip(p, p[1:]+p[:1]))
    return not separates(a, b) and not separates(b, a)

def recover(root, source, original, loops_for):
    charts = {c['key']: c for c in source['charts']}
    candidates = {}
    native = {}
    checked_occurrences = Counter()
    expected_hashes = {m['render']: m['nativeMeshSHA256'] for m in source['meshes']}
    for occurrence in source['occurrences']:
        key = occurrence['chartKey']
        if original[key]:
            continue
        render, mesh = occurrence['render'], occurrence['mesh']
        if render not in native:
            path = root/'artifacts/uv-density/native'/(render+'.npz')
            assert hashlib.sha256(path.read_bytes()).hexdigest() == expected_hashes[render]
            native[render] = np.load(path)
        data = native[render]
        faces = data[f'mesh{mesh}_faces']
        uv = data[f'mesh{mesh}_uv0'][:, :2]
        triangles = sorted(set(normalized_triangle(uv[faces[fi]]) for fi in occurrence['faces']))
        edges, area, degenerate = Counter(), 0., 0
        for tri in triangles:
            a, b, c = np.asarray(tri, dtype=np.float64)
            ab, ac = b-a, c-a
            cross = ab[0]*ac[1]-ab[1]*ac[0]
            if cross == 0:
                degenerate += 1
                continue
            area += abs(float(cross))*.5
            for a, b in ((tri[0], tri[1]), (tri[1], tri[2]), (tri[2], tri[0])):
                edges[tuple(sorted((a, b)))] += 1
        candidate = dict(charts[key], boundary=sorted(e for e, count in edges.items() if count == 1), summedTriangleArea=area)
        loops = loops_for(candidate)
        if loops:
            filled = [tuple(tuple(Fraction(x) for x in p) for p in t) for t in triangles
                if (t[1][0]-t[0][0])*(t[2][1]-t[0][1]) != (t[1][1]-t[0][1])*(t[2][0]-t[0][0])]
            assert all(count <= 2 for count in edges.values())
            assert not any(overlap(a, b) for i, a in enumerate(filled) for b in filled[i+1:]), 'Recovered filled triangles overlap.'
        checked_occurrences[key] += 1
        if key in candidates:
            assert candidates[key]['loops'] == loops, 'Shared chart recovery differs by physical occurrence.'
        else:
            candidates[key] = dict(loops=loops, degenerateTriangles=degenerate, render=render, mesh=mesh,
                sourceFaces=occurrence['faces'], retainedArea=area, originalArea=charts[key]['summedTriangleArea'])
        assert area == charts[key]['summedTriangleArea']
    recovered = {key: row for key, row in candidates.items() if row['loops']}
    for key, row in recovered.items():
        row['verifiedPhysicalOccurrences'] = checked_occurrences[key]
    return recovered, candidates
