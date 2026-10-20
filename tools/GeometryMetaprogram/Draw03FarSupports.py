"""Emit Draw03 native far support shells with measured rigid-side envelopes.
Usage: python Draw03FarSupports.py RESEARCH_DIRECTORY OUTPUT_CS
Detailed near geometry establishes the innermost column moulding/footing toe.
Coarse faces are subdivided at these envelopes, preserving original far surfaces.
"""
from pathlib import Path
import hashlib
import sys
import numpy as np
research, output = map(Path, sys.argv[1:])
tables = []
for part in range(2):
    near = np.load(research / f'draw03-support-{part}-0.npz')['positions']
    extent = (11.728759765625, 10.161376953125)[part]
    centre = np.isin(near[:,0], (0.,) if part else (-9.5367431640625e-7,-1.9073486328125e-6))
    assert float(np.min(abs(near[~centre,0]))) == extent
    # The envelopes are conservative: no detailed side-body vertex is scaled.
    # They are not the overall support bounds or the selected carriageway width.
    left, right = -extent, extent
    data = np.load(research / f'draw03-support-{part}-2.npz')
    v, faces = data['positions'], data['indices'].reshape(-1,3)
    assert (len(v),len(faces)) == ((854,800),(158,200))[part]
    name = 'DrawBridge03Pillar' + ('Base' if part else '') + '_LOD2 Mesh.Geometry'
    digest = hashlib.sha256((research/'movable-skin'/name).read_bytes()).hexdigest()
    assert digest == ('68d7c2c7987bfa843071fe9085e53c45b878483c423d0c05c3066df0805a634f',
                      '70ac56aa24827d71a47a21cbaf820259448199eb7b2715485c7bbeaad05218f8')[part]
    edges, fragments, appended = [], [], []

    def clip(poly, points, boundary, keep_left):
        result = []
        for a, b in zip(poly, poly[1:] + poly[:1]):
            xa, xb = float(a @ points[:, 0]), float(b @ points[:, 0])
            ina = xa <= boundary if keep_left else xa >= boundary
            inb = xb <= boundary if keep_left else xb >= boundary
            if ina:
                result.append(a)
            if ina != inb:
                result.append(a + (b - a) * ((boundary - xa) / (xb - xa)))
        return result

    def index_of(bary, face, points, local):
        nonzero = np.flatnonzero(abs(bary) > 1e-10)
        if len(nonzero) == 1:
            return int(face[nonzero[0]])
        assert len(nonzero) == 2, bary
        a, b = map(int, nonzero)
        fraction = np.float32(bary[b])
        key = (int(face[a]), int(face[b]), float(fraction))
        if key not in local:
            local[key] = len(v) + len(edges)
            edges.append(key)
            appended.append(points[a] + (points[b] - points[a]) * float(fraction))
        return local[key]

    for ordinal, face in enumerate(faces):
        points = v[face].astype(float)
        lo, hi = points[:, 0].min(), points[:, 0].max()
        if not any(lo < cut < hi for cut in (left, right)):
            continue
        polys = [[row for row in np.eye(3)]]
        for cut in (left, right):
            split = []
            for poly in polys:
                xs = [p @ points[:, 0] for p in poly]
                if min(xs) < cut < max(xs):
                    split.extend([clip(poly, points, cut, True), clip(poly, points, cut, False)])
                else:
                    split.append(poly)
            polys = split
        indices, local = [], {}
        for poly in polys:
            ids = [index_of(p, face, points, local) for p in poly]
            for i in range(1, len(ids) - 1):
                indices.extend((ids[0], ids[i], ids[i + 1]))
        fragments.append((ordinal, indices))

    all_v = np.concatenate((v.astype(float), np.array(appended).reshape(-1, 3)))
    weights = np.clip(2 * (all_v[:, 0] - left) / (right - left) - 1, -1, 1).astype(np.float32)
    # Roundoff in edge interpolation must not introduce a column-width change.
    for i in range(len(v), len(all_v)):
        nearest = min((left, right), key=lambda x: abs(x - all_v[i, 0]))
        assert abs(nearest - all_v[i, 0]) < 1e-6
        weights[i] = -1 if nearest == left else 1
    replacement = dict(fragments)
    all_faces = np.array([tri for i, face in enumerate(faces)
                         for tri in np.array(replacement.get(i, face)).reshape(-1, 3)])
    for ordinal, indices in fragments:
        p = v[faces[ordinal]].astype(float)
        original_area = np.linalg.norm(np.cross(p[1] - p[0], p[2] - p[0]))
        q = all_v[np.array(indices).reshape(-1, 3)]
        area = np.linalg.norm(np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0]), axis=1).sum()
        assert abs(area - original_area) < 2e-4
    for delta in (-16., -8., -.1, 0., 2., 8.):
        moved = all_v.copy()
        moved[:, 0] += weights * delta
        assert np.array_equal(moved[:, 1:], all_v[:, 1:])
        for side, mask in ((-1, all_v[:, 0] < left - 1e-6), (1, all_v[:, 0] > right + 1e-6)):
            assert np.all(weights[mask] == side)
            assert np.allclose(moved[mask] - all_v[mask], [side * delta, 0, 0])
        p, q = all_v[all_faces], moved[all_faces]
        before = np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0])
        after = np.cross(q[:, 1] - q[:, 0], q[:, 2] - q[:, 0])
        valid = np.linalg.norm(before, axis=1) > 1e-8
        if delta >= -.1: assert np.all(np.sum(before[valid] * after[valid], axis=1) > 0), delta

    tables.append((len(v),faces.size,digest,extent,weights,edges,fragments))
    np.savez(research/f'draw03-far-support-plan-{part}.npz',positions=all_v,indices=all_faces,weights=weights)
    print(f'Part {part}: {len(fragments)} split source faces, {len(edges)} added edge vertices, {len(all_faces)} triangles; source area, six signed widths, rigid-side and positive-span winding checks passed')

def literal(x):
    return format(float(np.float32(x)), '.9g')+'f'

lines = ['// Native DrawBridge03 far support shells; generated by Draw03FarSupports.py.',
         'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class Draw03FarSupports', '{',
         '    internal static MovableStructureModel.Plan Create(bool footing, Vector3[] source, int[] indices, float delta)',
         '    {',
         '        var data = footing',
         '            ? (Count: 158, Indices: 600, Minimum: 10.161376953125f, Weights: Weights1, Edges: Edges1, Fragments: Fragments1)',
         '            : (Count: 854, Indices: 2400, Minimum: 11.728759765625f, Weights: Weights0, Edges: Edges0, Fragments: Fragments0);',
         '        if (!float.IsFinite(delta) || source.Length != data.Count || indices.Length != data.Indices)',
         '            throw new InvalidOperationException("Draw03 far supports require their measured layout.");',
         '        var positions = (Vector3[])source.Clone();',
         '        for (var i = 0; i < positions.Length; i++) positions[i].x += data.Weights[i] * delta;',
         '        var x = new float[data.Edges.Length];',
         '        for (var i = 0; i < x.Length; i++)', '        {',
         '            var edge = data.Edges[i];',
         '            x[i] = Mathf.LerpUnclamped(source[edge.A].x, source[edge.B].x, edge.Fraction)',
         '                + data.Weights[source.Length + i] * delta;', '        }',
         '        return new MovableStructureModel.Plan(source, positions, new[] { indices },',
         '            new[] { new bool[indices.Length / 3] }, data.Edges, data.Fragments, constructedX: x);',
         '    }', '']
for part, (count,size,digest,extent,weights,edges,fragments) in enumerate(tables):
    lines += ['    // Geometry SHA-256: '+digest, f'    private static readonly float[] Weights{part} =', '    {']
    for start in range(0,len(weights),12):
        lines.append('        '+', '.join(map(literal,weights[start:start+12]))+',')
    lines += ['    };', f'    private static readonly MovableStructureModel.EdgeVertex[] Edges{part} =', '    {']
    lines += [f'        new({a}, {b}, {literal(t)}),' for a,b,t in edges]
    lines += ['    };', f'    private static readonly MovableStructureModel.Fragment[] Fragments{part} =', '    {']
    lines += ['        new(0, '+str(i)+', '+', '.join(map(str,ids))+'),' for i,ids in fragments]
    lines += ['    };', '']
lines += ['}', '']
output.write_text('\n'.join(lines),encoding='utf-8')
