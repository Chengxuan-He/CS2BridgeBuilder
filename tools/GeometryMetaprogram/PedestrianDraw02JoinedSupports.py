"""Emit measured subdivision for the two end caps and two hinge cradles.
Usage: python PedestrianDraw02JoinedSupports.py RESEARCH_DIRECTORY OUTPUT_CS
Cap boundary is its innermost column bevel; cradle boundary is its inner arm flange.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

r, output = map(Path, sys.argv[1:])
tables = []
for lod in range(2):
    d = np.load(r / f'pedestrian02-main-{lod}.npz')
    v, faces = d['positions'], d['indices']
    parts = [p for p in json.loads((r / f'pedestrian02-main-{lod}-parts.json').read_text())
             if p['crossing'] and len(p['vertices']) in (127, 151)]
    assert len(parts) == 4
    weights, edges, extra, fragments = {}, [], [], []
    for part in parts:
        ids = part['vertices']
        extent = 3.63031005859375 if len(ids) == 127 else 2.03131103515625
        assert extent in v[ids, 0] and -extent in v[ids, 0]
        for i in ids:
            weights[i] = float(np.clip(v[i, 0] / extent, -1, 1))
        selected = np.flatnonzero(np.isin(faces, ids).all(1))
        def clip(poly, points, boundary, left):
            result = []
            for a, b in zip(poly, poly[1:] + poly[:1]):
                xa, xb = a @ points[:, 0], b @ points[:, 0]
                ina = xa <= boundary if left else xa >= boundary
                inb = xb <= boundary if left else xb >= boundary
                if ina:
                    result.append(a)
                if ina != inb:
                    result.append(a + (b - a) * ((boundary - xa) / (xb - xa)))
            clean = []
            for p in result:
                if not clean or np.max(abs(p - clean[-1])) > 1e-10:
                    clean.append(p)
            if len(clean) > 1 and np.max(abs(clean[0] - clean[-1])) <= 1e-10:
                clean.pop()
            return clean
        for ordinal in selected:
            face = faces[ordinal]
            points = v[face].astype(float)
            if not any(points[:, 0].min() < cut < points[:, 0].max() for cut in (-extent, extent)):
                continue
            polys = [list(np.eye(3))]
            for cut in (-extent, extent):
                result = []
                for poly in polys:
                    xs = [p @ points[:, 0] for p in poly]
                    if min(xs) < cut < max(xs):
                        result += [clip(poly, points, cut, True), clip(poly, points, cut, False)]
                    else:
                        result.append(poly)
                polys = result
            indices, local = [], {}
            for poly in polys:
                polygon = []
                for bary in poly:
                    nonzero = np.flatnonzero(abs(bary) > 1e-10)
                    if len(nonzero) == 1:
                        polygon.append(int(face[nonzero[0]]))
                    else:
                        assert len(nonzero) == 2
                        a, b = map(int, nonzero)
                        fraction = float(np.float32(bary[b]))
                        key = (int(face[a]), int(face[b]), fraction)
                        if key not in local:
                            local[key] = len(v) + len(edges)
                            p = points[a] + (points[b] - points[a]) * fraction
                            weight = -1 if p[0] < 0 else 1
                            assert abs(abs(p[0]) - extent) < 1e-6
                            edges.append((*key, weight))
                            extra.append(p)
                        polygon.append(local[key])
                for i in range(1, len(polygon) - 1):
                    indices += [polygon[0], polygon[i], polygon[i+1]]
            fragments.append((int(ordinal), indices))
    all_v = np.concatenate((v.astype(float), np.array(extra)))
    for ordinal, indices in fragments:
        p = v[faces[ordinal]].astype(float)
        normal = np.cross(p[1] - p[0], p[2] - p[0])
        q = all_v[np.array(indices).reshape(-1, 3)]
        area = np.linalg.norm(np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0]), axis=1).sum()
        assert abs(area - np.linalg.norm(normal)) < 3e-5
    for delta in (-4., -2.03131103515625, -.01, 0, 2, 8):
        moved = all_v.copy()
        for i, w in weights.items():
            moved[i, 0] += w * delta
        for i, edge in enumerate(edges):
            moved[len(v)+i, 0] += edge[3] * delta
        assert np.array_equal(moved[:, 1:], all_v[:, 1:])
        for i, w in weights.items():
            if abs(w) == 1:
                assert abs(moved[i, 0] - all_v[i, 0] - w * delta) < 1e-10
        inverted = 0
        for _, indices in fragments:
            p = all_v[np.array(indices).reshape(-1, 3)]
            q = moved[np.array(indices).reshape(-1, 3)]
            before = np.cross(p[:, 1]-p[:, 0], p[:, 2]-p[:, 0])
            after = np.cross(q[:, 1]-q[:, 0], q[:, 2]-q[:, 0])
            inverted += int(np.count_nonzero(np.sum(before*after, axis=1) < 0))
        assert np.isfinite(moved).all()
        if delta >= -.01:
            assert inverted == 0
        print(f'LOD{lod} delta={delta}: {inverted} inverted fragment triangles; signed mapping retained')
    name = 'PedestrianDrawBridge02' + ('_LOD1' if lod else '') + ' Mesh.Geometry'
    digest = hashlib.sha256((r / 'movable-skin' / name).read_bytes()).hexdigest()
    tables.append((weights, edges, fragments, digest))
    print(f'LOD{lod}: {len(weights)} original vertices, {len(edges)} edge vertices, '
          f'{len(fragments)} subdivided faces; source area and six-width checks passed')

def f(x):
    return format(float(np.float32(x)), '.9g') + 'f'
lines = ['// Measured PedestrianDrawBridge02 joined cap/cradle profiles, game 1.6.2f1.',
         '// Generated by PedestrianDraw02JoinedSupports.py; no runtime intersections.',
         'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class PedestrianDraw02JoinedSupports', '{',
         '    internal static MovableStructureModel.Plan Complete(int lod, Vector3[] source,',
         '        Vector3[] positions, int[] indices, float delta)', '    {',
         '        var data = lod switch', '        {',
         '            0 => (Count: 40168, Indices: 90192, Vertices: Vertices0, Edges: Edges0, Signs: Signs0, Fragments: Fragments0),',
         '            1 => (Count: 32137, Indices: 54075, Vertices: Vertices1, Edges: Edges1, Signs: Signs1, Fragments: Fragments1),',
         '            _ => throw new ArgumentOutOfRangeException(nameof(lod))', '        };',
         '        if (!float.IsFinite(delta) || source.Length != data.Count ||',
         '            positions.Length != data.Count || indices.Length != data.Indices)',
         '            throw new InvalidOperationException("PedestrianDraw02 joined supports require their measured layout.");',
         '        foreach (var vertex in data.Vertices)',
         '            positions[vertex.Index].x = source[vertex.Index].x + vertex.Weight * delta;',
         '        var x = new float[data.Edges.Length];',
         '        for (var i = 0; i < x.Length; i++)', '        {',
         '            var edge = data.Edges[i];',
         '            x[i] = Mathf.LerpUnclamped(source[edge.A].x, source[edge.B].x, edge.Fraction) + data.Signs[i] * delta;',
         '        }',
         '        return new MovableStructureModel.Plan(source, positions, new[] { indices },',
         '            new[] { new bool[indices.Length / 3] }, data.Edges, data.Fragments, constructedX: x);',
         '    }', '']
for lod, (weights, edges, fragments, digest) in enumerate(tables):
    lines += ['    // Geometry SHA-256: ' + digest,
              f'    private static readonly (int Index, float Weight)[] Vertices{lod} =', '    {']
    lines += [f'        ({i}, {f(w)}),' for i, w in sorted(weights.items())]
    lines += ['    };', f'    private static readonly MovableStructureModel.EdgeVertex[] Edges{lod} =', '    {']
    lines += [f'        new({a}, {b}, {f(t)}),' for a, b, t, _ in edges]
    lines += ['    };', f'    private static readonly float[] Signs{lod} =', '    {',
              '        ' + ', '.join(f(w) for _, _, _, w in edges), '    };',
              f'    private static readonly MovableStructureModel.Fragment[] Fragments{lod} =', '    {']
    lines += ['        new(0, ' + str(i) + ', ' + ', '.join(map(str, ids)) + '),' for i, ids in fragments]
    lines += ['    };', '']
lines += ['}', '']
output.write_text('\n'.join(lines), encoding='utf-8')
