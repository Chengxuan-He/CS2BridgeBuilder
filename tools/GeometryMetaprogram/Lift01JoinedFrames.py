"""Emit measured subdivision for U frames, roof grids and rounded end platforms.
Usage: python Lift01JoinedFrames.py RESEARCH_DIRECTORY OUTPUT_CS
The original inner upright surface separates rigid sides from crossing beams.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from StaticArrayChunks import split_static_arrays

r, output = map(Path, sys.argv[1:])
tables = []
for lod in range(2):
    d = np.load(r / f'lift01-main-{lod}.npz')
    v, faces = d['positions'], d['indices']
    groups = json.loads((r / f'lift01-crossing-{lod}.json').read_text())
    selected = [0, 2, 4, 7, 8, 10, 12, 14, 17, 18, 346, 348, 349, 352, 353,
                356, 357, 360, 361, 364, 365, 368, 369, 372, 373, 376, 377,
                380, 381, 384, 385, 494, 496, 497, 500, 501, 504, 505, 508,
                509, 512, 513, 516, 517, 520, 521, 524, 525, 528, 529, 532, 533, 29, 343, 20, 21]
    parts = [(i, groups[i]) for i in selected]
    assert sum(len(p['vertices']) for _, p in parts) == (28225 if lod == 0 else 18714)
    near = json.loads((r / 'lift01-crossing-0.json').read_text())
    for i, part in parts:
        assert part['minimum'] == near[i]['minimum']
        assert part['maximum'] == near[i]['maximum']
    weights, edges, extra, fragments = {}, [], [], []
    for ordinal, part in parts:
        ids = part['vertices']
        extent = 3.3919677734375 if ordinal in (377, 380, 381, 384, 385, 525, 528, 529, 532, 533) else 3.39190673828125
        points = v[ids]
        if ordinal in (20,21):
            # Innermost tangent ring of the rounded platform ends. All curved
            # perimeter rings start at this X or farther out; keep them rigid.
            # The near-only fan centre at +/-8.6328125 is triangulation inside
            # a flat cap, not a perimeter boundary shared with the middle LOD.
            left, right = -9.082763671875, 9.082763671875
            assert part['minimum'][0] == -16.7490234375
            assert part['maximum'][0] == 16.7490234375
            assert set(points[points[:,1] == 7.0107421875,0]).issuperset((left,right))
        elif ordinal in (29,343):
            left = -.8794708251953125 if ordinal == 29 else -.879486083984375
            right = .879486083984375
            assert points[points[:,0]<0,0].max() == left
            assert points[points[:,0]>0,0].min() == right
        else:
            assert np.min(abs(points[points[:, 1] > 10.2, 0])) == extent
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
    assert len(weights) == (28225 if lod == 0 else 18714)
    rigid = json.loads((r / f'lift01-side-{lod}.json').read_text())
    assert not set(weights).intersection(i for p in rigid for i in p['vertices'])
    for i in (3, 13, 487, 30, 31, 32, 33, 493, 640):
        assert not set(weights).intersection(groups[i]['vertices'])
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
    for delta in (-2.99, -2.75, -2, -.5, 0, 2, 8):
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
        assert np.isfinite(moved).all()
        if delta >= -.5:
            assert np.all(np.sum(before[valid]*after[valid], axis=1) > 0)
        else:
            print(f"Joined LOD{lod} delta={delta}: {np.count_nonzero(np.sum(before[valid]*after[valid], axis=1) <= 0)} collapsed/reversed faces retained by signed mapping")
    name = 'LiftBridge01' + ('_LOD1' if lod else '') + ' Mesh.Geometry'
    digest = hashlib.sha256((r / 'movable-skin' / name).read_bytes()).hexdigest()
    tables.append((weights, edges, fragments, digest))
    print(f'LOD{lod}: {len(weights)} original vertices, {len(edges)} edge vertices, '
          f'{len(fragments)} subdivided faces; source area and seven-width checks passed')

def f(x):
    return format(float(np.float32(x)), '.9g') + 'f'
lines = ['// Measured LiftBridge01 joined U-frame, roof-grid and end-platform profiles, game 1.6.2f1.',
         '// Generated by Lift01JoinedFrames.py; no runtime intersections.',
         'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
         'internal static class Lift01JoinedFrames', '{',
    
         '    internal static MovableStructureModel.Plan Complete(int lod, Vector3[] source,',
         '        Vector3[] positions, int[] indices, float delta)', '    {',
         '        var data = lod switch', '        {',
         '            0 => (Count: 472209, Indices: 1141434, Vertices: Vertices0, Edges: Edges0, Signs: Signs0, Fragments: Fragments0),',
         '            1 => (Count: 351968, Indices: 684762, Vertices: Vertices1, Edges: Edges1, Signs: Signs1, Fragments: Fragments1),',
         '            _ => throw new ArgumentOutOfRangeException(nameof(lod))', '        };',
         '        if (!float.IsFinite(delta) || source.Length != data.Count ||',
         '            positions.Length != data.Count || indices.Length != data.Indices)',
         '            throw new InvalidOperationException("Lift01 joined frames require their measured layout.");',
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
output.write_text(split_static_arrays('\n'.join(lines)), encoding='utf-8')
