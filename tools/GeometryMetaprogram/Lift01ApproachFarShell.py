"""Emit measured subdivision for the fixed-span far shell.
Usage: python Lift01ApproachFarShell.py RESEARCH_DIRECTORY OUTPUT_CS
The native underside width separates rigid side profiles from crossing panels.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

r, output = map(Path, sys.argv[1:])
tables = []
for lod in (2,):
    d = np.load(r / f'lift01-approach-{lod}.npz')
    v, faces = d['positions'], d['indices']
    groups = json.loads((r / f'lift01-approach-{lod}-parts.json').read_text())
    parts = [(0, groups[0])]
    assert len(parts[0][1]['vertices']) == 704
    weights, edges, extra, fragments = {}, [], [], []
    for ordinal, part in parts:
        ids = part['vertices']
        extent = 2.12420654296875
        points = v[ids]
        assert np.min(abs(points[:, 0])) == extent
        left, right = -extent, extent
        member = np.isin(faces, ids)
        assert not np.any(member.any(1) & ~member.all(1))
        assert not set(weights).intersection(ids)
        assert left in v[ids, 0] and right in v[ids, 0]
        for i in ids:
            weights[i] = float(np.clip((2 * float(v[i, 0]) - left - right) / (right - left), -1, 1))
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
            if not any(points[:, 0].min() < cut < points[:, 0].max() for cut in (left, right)):
                continue
            polys = [list(np.eye(3))]
            for cut in (left, right):
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
                            assert min(abs(p[0]-left), abs(p[0]-right)) < 1e-6
                            edges.append((*key, weight))
                            extra.append(p)
                        polygon.append(local[key])
                for i in range(1, len(polygon) - 1):
                    indices += [polygon[0], polygon[i], polygon[i+1]]
            fragments.append((int(ordinal), indices))
    assert len(weights) == 704
    reserved = set(i for ordinal, part in enumerate(groups) if ordinal not in {part_id for part_id, _ in parts} for i in part['vertices'])
    assert not set(weights).intersection(reserved)
    assert set(weights).union(reserved) == set(range(len(v)))
    assert len(reserved) == 780
    # All original shell vertices lie on the two rigid profiles. Only the
    # introduced end-panel boundary points delimit the central stretch span.
    assert all(w == np.sign(v[i, 0]) for i, w in weights.items())
    # Every original face touching a frame is wholly owned by it. Validate both
    # untouched original faces and the replacement fragments, not fragments alone.
    replaced = {ordinal for ordinal, _ in fragments}
    frame_faces = np.flatnonzero(np.isin(faces, list(weights)).all(1))
    retained = [faces[i] for i in frame_faces if i not in replaced]
    complete_faces = np.array([list(f) for f in retained] +
        [ids[i:i+3] for _, ids in fragments for i in range(0, len(ids), 3)])
    all_v = np.concatenate((v.astype(float), np.array(extra)))
    for ordinal, indices in fragments:
        p = v[faces[ordinal]].astype(float)
        normal = np.cross(p[1] - p[0], p[2] - p[0])
        q = all_v[np.array(indices).reshape(-1, 3)]
        area = np.linalg.norm(np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0]), axis=1).sum()
        assert abs(area - np.linalg.norm(normal)) < 3e-5
    for delta in (-4, -2.75, -2, -.5, 0, 2, 8):
        moved = all_v.copy()
        for i, w in weights.items():
            moved[i, 0] += w * delta
        for i, edge in enumerate(edges):
            moved[len(v)+i, 0] += edge[3] * delta
        assert np.array_equal(moved[:, 1:], all_v[:, 1:])
        for i, w in weights.items():
            if abs(w) == 1:
                assert abs(moved[i, 0] - all_v[i, 0] - w * delta) < 1e-10
        p, q = all_v[complete_faces], moved[complete_faces]
        before = np.cross(p[:, 1]-p[:, 0], p[:, 2]-p[:, 0])
        after = np.cross(q[:, 1]-q[:, 0], q[:, 2]-q[:, 0])
        valid = np.linalg.norm(before, axis=1) > 1e-8
        if delta >= -1: assert np.all(np.sum(before[valid]*after[valid], axis=1) > 0)
    name = 'LiftBridge01Net' + '_LOD2' + ' Mesh.Geometry'
    digest = hashlib.sha256((r / 'lift01-approaches' / name).read_bytes()).hexdigest()
    assert digest == '4c393e78ac9580d7a2ceda0f7417ebfdd1b2908af5f61347278ddc064ac567d6'
    tables.append((weights, edges, fragments, digest))
    assert len(fragments) == 8 and len(edges) == 24
    assert len(faces) - len(fragments) + sum(len(ids) // 3 for _, ids in fragments) == 1044
    print(f'LOD{lod}: {len(weights)} original vertices, {len(edges)} edge vertices, '
          f'{len(fragments)} subdivided faces; source area and seven signed-width checks passed')

def f(x):
    return format(float(np.float32(x)), '.9g') + 'f'
lines = ['// Measured LiftBridge01Net far shell profile, game 1.6.2f1.',
         '// Generated by Lift01ApproachFarShell.py; no runtime intersections.',
         'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class Lift01ApproachFarShell', '{',
         '    internal static MovableStructureModel.Plan Complete(Vector3[] source,',
         '        Vector3[] positions, int[] indices, float delta)', '    {',
         '        var data = (Vertices: Vertices0, Edges: Edges0, Signs: Signs0, Fragments: Fragments0);',
         '        if (!float.IsFinite(delta) || source.Length != 1484 ||',
         '            positions.Length != 1484 || indices.Length != 3060)',
         '            throw new InvalidOperationException("Lift01 far approach requires its measured layout.");',
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
