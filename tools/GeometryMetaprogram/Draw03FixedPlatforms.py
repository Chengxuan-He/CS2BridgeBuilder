"""Emit Draw03 fixed platforms with rigid side supports and the measured central wall span.
Usage: python Draw03FixedPlatforms.py RESEARCH_DIRECTORY OUTPUT_CS
Detailed central-wall panel boundaries are X=+/-10.37841796875 m.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

r, output = map(Path, sys.argv[1:])
tables = []
for lod in range(2):
    d = np.load(r / f'draw03-main-{lod}.npz')
    v, faces = d['positions'], d['indices']
    crossing = [p for p in json.loads((r / f'draw03-parts-{lod}.json').read_text()) if not p['side']]
    parts = [crossing[i] for i in (6, 94)]
    name = 'DrawBridge03' + ('_LOD1' if lod else '') + ' Mesh.Geometry'
    digest = hashlib.sha256((r / 'movable-skin' / name).read_bytes()).hexdigest()
    assert digest == ('f7ecc824ad55745e791e40b18fd12f5def20657ed8ea56d373429d5a1337588f',
                      '15b8419da13d6e99e1dc0591f12f4019f0be09c3b220a481d24720daf7b57e12')[lod]
    weights, edges, extra, fragments = {}, [], [], []
    for part in parts:
        ids = part['vertices']
        extent = 10.37841796875
        assert extent in v[ids, 0] and -extent in v[ids, 0]
        for i in ids:
            weights[i] = float(np.clip(v[i, 0] / extent, -1, 1))
        membership = np.isin(faces, ids)
        assert not np.any(membership.any(1) & ~membership.all(1))
        selected = np.flatnonzero(membership.all(1))
        assert set(d['bones'][selected]) == {0}
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
    assert (len(weights), len(edges), len(fragments)) == ((3236, 0, 0), (2951, 30, 20))[lod]
    all_v = np.concatenate((v.astype(float), np.array(extra).reshape(-1, 3)))
    replacement = dict(fragments)
    platform_faces = np.flatnonzero(np.isin(faces, list(weights)).all(1))
    checked_faces = np.array([tri for j in platform_faces
                             for tri in np.array(replacement.get(j, faces[j])).reshape(-1, 3)])
    for ordinal, indices in fragments:
        p = v[faces[ordinal]].astype(float)
        normal = np.cross(p[1] - p[0], p[2] - p[0])
        q = all_v[np.array(indices).reshape(-1, 3)]
        area = np.linalg.norm(np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0]), axis=1).sum()
        assert abs(area - np.linalg.norm(normal)) < 3e-5
    for delta in (-16, -8, -.1, 0, 2, 8):
        moved = all_v.copy()
        for i, w in weights.items():
            moved[i, 0] += w * delta
        for i, edge in enumerate(edges):
            moved[len(v)+i, 0] += edge[3] * delta
        assert np.array_equal(moved[:, 1:], all_v[:, 1:])
        before_points, after_points = all_v[checked_faces], moved[checked_faces]
        before_normal = np.cross(before_points[:,1]-before_points[:,0], before_points[:,2]-before_points[:,0])
        after_normal = np.cross(after_points[:,1]-after_points[:,0], after_points[:,2]-after_points[:,0])
        valid = np.linalg.norm(before_normal, axis=1) > 1e-8
        if delta >= -.1: assert np.all(np.sum(before_normal[valid]*after_normal[valid], axis=1) > 0)
        for i, w in weights.items():
            if abs(w) == 1:
                assert abs(moved[i, 0] - all_v[i, 0] - w * delta) < 1e-10
        for _, indices in fragments:
            p = all_v[np.array(indices).reshape(-1, 3)]
            q = moved[np.array(indices).reshape(-1, 3)]
            before = np.cross(p[:, 1]-p[:, 0], p[:, 2]-p[:, 0])
            after = np.cross(q[:, 1]-q[:, 0], q[:, 2]-q[:, 0])
            if delta >= -.1: assert np.all(np.sum(before*after, axis=1) >= 0)
    name = 'DrawBridge03' + ('_LOD1' if lod else '') + ' Mesh.Geometry'
    digest = hashlib.sha256((r / 'movable-skin' / name).read_bytes()).hexdigest()
    np.savez(r / f'draw03-platform-plan-{lod}.npz', positions=all_v,
             indices=np.array([tri for j, face in enumerate(faces) for tri in np.array(dict(fragments).get(j, face)).reshape(-1,3)]),
             weights=np.array([weights.get(i, edges[i-len(v)][3] if i>=len(v) else 0) for i in range(len(all_v))]))
    tables.append((weights, edges, fragments, digest))
    print(f'LOD{lod}: {len(weights)} original vertices, {len(edges)} edge vertices, '
          f'{len(fragments)} subdivided faces; source area and six signed-width checks passed')

def f(x):
    return format(float(np.float32(x)), '.9g') + 'f'
lines = ['// Measured DrawBridge03 fixed support platforms, game 1.6.2f1.',
         '// Generated by Draw03FixedPlatforms.py; no runtime intersections.',
         'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class Draw03FixedPlatforms', '{',
         '    internal static MovableStructureModel.Plan Complete(int lod, Vector3[] source,',
         '        Vector3[] positions, int[] indices, float delta)', '    {',
         '        var data = lod switch', '        {',
         '            0 => (Count: 186078, Indices: 381444, Vertices: Vertices0, Edges: Edges0, Signs: Signs0, Fragments: Fragments0),',
         '            1 => (Count: 146988, Indices: 229152, Vertices: Vertices1, Edges: Edges1, Signs: Signs1, Fragments: Fragments1),',
         '            _ => throw new ArgumentOutOfRangeException(nameof(lod))', '        };',
         '        if (!float.IsFinite(delta) || source.Length != data.Count ||',
         '            positions.Length != data.Count || indices.Length != data.Indices)',
         '            throw new InvalidOperationException("Draw03 fixed platforms require their measured layout.");',
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
